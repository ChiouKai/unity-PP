#include "GTAO_Common.cginc"

inline half3 GetPosition(half2 uv, out half depth)
{
	depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, my_point_clamp_sampler, uv).r;
	half viewDepth = LinearEyeDepth(depth, _ZBufferParams);
	return half3((uv * _AO_UVToView.xy + _AO_UVToView.zw) * viewDepth, viewDepth);
}
inline half3 GetPosition(half2 uv)
{
	half depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, my_point_clamp_sampler, uv).r;
	half viewDepth = LinearEyeDepth(depth, _ZBufferParams);
	return half3((uv * _AO_UVToView.xy + _AO_UVToView.zw) * viewDepth, viewDepth);
}

inline half3 GetNormal(half2 uv)
{
	half3 normal = 	SAMPLE_TEXTURE2D(_CameraGBufferTexture2, my_point_clamp_sampler, uv).rgb * 2 - 1; 
	half3 view_Normal = normalize(mul((half3x3) _WorldToCameraMatrix, normal));

	return half3(view_Normal.xy, -view_Normal.z);
}

inline half GTAO_Offsets(half2 uv)
{
	int2 position = (int2)(uv * _AO_RT_TexelSize.zw);
	return 0.25 * (half)((position.y - position.x) & 3);
}

inline half GTAO_Noise(half2 position)
{
	return frac(52.9829189 * frac(dot(position, half2( 0.06711056, 0.00583715))));
}

half IntegrateArc_UniformWeight(half2 h)
{
	half2 Arc = 1 - cos(h);
	return Arc.x + Arc.y;
}

half IntegrateArc_CosWeight(half2 h, half n)
{
    half2 Arc = -cos(2 * h - n) + cos(n) + 2 * h * sin(n);
    return 0.25 * (Arc.x + Arc.y);
}


half4 GTAO(half2 uv, out half Depth)
{
	half depth;
	half3 vPos = GetPosition(uv, depth);
	half3 viewNormal = GetNormal(uv);
	half3 viewDir = normalize(0 - vPos);

	half2 radius_thickness = half2(_AO_Radius, 1);
	half radius = radius_thickness.x;
	half thickness = radius_thickness.y;

	half stepRadius = max(min((radius * _AO_HalfProjScale) / vPos.b, 512), (half)_AO_SliceSampler);
	stepRadius /= ((half)_AO_SliceSampler + 1);

	half noiseOffset = GTAO_Offsets(uv);
	half noiseDirection = GTAO_Noise(uv * _AO_RT_TexelSize.zw) + _AO_TemporalDirections;

	half initialRayStep = frac(noiseOffset + _AO_TemporalOffsets);

	half Occlusion, angle, bentAngle, wallDarkeningCorrection, projLength, n, cos_n;
	half2 slideDir_TexelSize, h, H, falloff, uvOffset, dsdt, dsdtLength;
	half3 sliceDir, ds, dt, planeNormal, tangent, projectedNormal ,BentNormal;
	half4 uvSlice;
	Depth = vPos.z;
	if (depth <= 1e-7)
	{
		return 1;
	}

	UNITY_LOOP
	for (int i = 0; i < _AO_DirSampler; i++)
	{
		angle = (i + noiseDirection) * (PI / (half)_AO_DirSampler);
		sliceDir = half3(half2(cos(angle), sin(angle)), 0);
		slideDir_TexelSize = sliceDir.xy * _AO_RT_TexelSize.xy;
		h = -1;

		UNITY_LOOP
		for (int j = 0; j < _AO_SliceSampler; j++)
		{
			uvOffset = slideDir_TexelSize * max(stepRadius * (j + initialRayStep), 1 + j);
			uvSlice = uv.xyxy + float4(uvOffset, -uvOffset);

			ds = GetPosition(uvSlice.xy) - vPos;
			dt = GetPosition(uvSlice.zw) - vPos;

			dsdt = half2(dot(ds, ds), dot(dt, dt));
			dsdtLength = rsqrt(dsdt);

			falloff = saturate(dsdt.xy * (2 / (radius * radius)));

			H = half2(dot(ds, viewDir), dot(dt, viewDir)) * dsdtLength;
			h.xy = (H.xy > h.xy) ? lerp(H, h, falloff) : lerp(H.xy, h.xy, thickness);
		}

		planeNormal = normalize(cross(sliceDir, viewDir));
		tangent = cross(viewDir, planeNormal);
		projectedNormal = viewNormal - planeNormal * dot(viewNormal, planeNormal);
		projLength = length(projectedNormal);

		cos_n = clamp(dot(normalize(projectedNormal), viewDir), -1, 1);
		n = -sign(dot(projectedNormal, tangent)) * acos(cos_n);

		h = acos(clamp(h, -1, 1));
		h.x = n + max(-h.x - n, -HALF_PI);
		h.y = n + min(h.y - n, HALF_PI);

		bentAngle = (h.x + h.y) * 0.5;

		BentNormal += viewDir * cos(bentAngle) - tangent * sin(bentAngle);
		Occlusion += projLength * IntegrateArc_CosWeight(h, n); 			
		//Occlusion += projLength * IntegrateArc_UniformWeight(h);			
	}

	BentNormal = normalize(normalize(BentNormal) - viewDir * 0.5);
	Occlusion /= (half)_AO_DirSampler;


	return half4(BentNormal, Occlusion);
}




void ResolveGTAO_frag(PixelInput IN, out half2 AO : SV_Target0, out half3 BentNormal : SV_Target1)
{
	half2 uv = IN.uv.xy;

	half Depth = 0;
	half4 GT_Details = GTAO(uv, Depth);

	AO = half2(GT_Details.a, Depth);
	BentNormal = mul((half3x3)_CameraToWorldMatrix, half3(GT_Details.rg, -GT_Details.b));
} 