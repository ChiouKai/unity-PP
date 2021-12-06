#include "GTAO_Common.cginc"

inline float ApproximateConeConeIntersection(float ArcLength0, float ArcLength1, float AngleBetweenCones)
{
	float AngleDifference = abs(ArcLength0 - ArcLength1);

	float Intersection = smoothstep(0, 1, 1 - saturate((AngleBetweenCones - AngleDifference) / (ArcLength0 + ArcLength1 - AngleDifference)));

	return Intersection;
}

inline half ReflectionOcclusion(half3 BentNormal, half3 ReflectionVector, half Roughness, half OcclusionStrength)
{
	half BentNormalLength = length(BentNormal);
	half ReflectionConeAngle = max(Roughness, 0.1) * PI;
	half UnoccludedAngle = BentNormalLength * PI * OcclusionStrength;

	half AngleBetween = acos(dot(BentNormal, ReflectionVector) / max(BentNormalLength, 0.001));
	half ReflectionOcclusion = ApproximateConeConeIntersection(ReflectionConeAngle, UnoccludedAngle, AngleBetween);
	ReflectionOcclusion = lerp(0, ReflectionOcclusion, saturate((UnoccludedAngle - 0.1) / 0.2));
	return ReflectionOcclusion;
}

//inline half ReflectionOcclusion_Approch(half NoV, half Roughness, half AO)
//{
//	return saturate(pow(NoV + AO, Roughness * Roughness) - 1 + AO);
//}

real3 GTAOMultiBounce(real visibility, real3 albedo)
{
    real3 a =  2.0404 * albedo - 0.3324;
    real3 b = -4.7951 * albedo + 0.6417;
    real3 c =  2.7552 * albedo + 0.6903;

    real x = visibility;
    return max(x, ((x * a + b) * x + c) * x);
}




#define FLT_EPSILON     1.192092896e-07 // Smallest positive number, such that 1.0 + FLT_EPSILON != 1.0
float PositivePow2(float base, float power)
{
	return pow(max(abs(base), FLT_EPSILON), power);
}
float OutputFinalAO(float AO)
{
    return saturate(PositivePow2(AO, _AO_Intensity));
}


//////Combien Scene Color
half4 CombienGTAO_frag(PixelInput IN) : SV_Target
{
	half2 uv = IN.uv.xy;

	//////AO & MultiBounce
	half2 GT_Occlusion = SAMPLE_TEXTURE2D(_CurrRT, my_point_clamp_sampler, uv).rg;
	half3 GTAO = OutputFinalAO(GT_Occlusion.r);

	half GTRO = GT_Occlusion.g;

	#ifdef _AO_MultiBounce
		half3 Albedo = SAMPLE_TEXTURE2D(_CameraGBufferTexture0, my_linear_clamp_sampler, uv).rgb;
		GTAO = GTAOMultiBounce(GTAO.r, Albedo);
	#endif

	half3 RelfectionColor = SAMPLE_TEXTURE2D(_CameraReflectionsTexture, my_linear_clamp_sampler, uv).rgb;
	half3 SceneColor = GTAO * (SAMPLE_TEXTURE2D(_AO_Scene_Color, my_linear_clamp_sampler, uv).rgb - RelfectionColor);
	RelfectionColor *= GTRO;
	
	return half4(SceneColor + RelfectionColor, 1);
}

//////DeBug AO
half4 DeBugGTAO_frag(PixelInput IN) : SV_Target
{
	half2 uv = IN.uv.xy;

	//////AO & MultiBounce
	half3 GTAO = OutputFinalAO(SAMPLE_TEXTURE2D(_CurrRT, my_linear_clamp_sampler, uv).r);

	#ifdef _AO_MultiBounce
		half3 Albedo = SAMPLE_TEXTURE2D(_CameraGBufferTexture0, my_linear_clamp_sampler, uv).rgb;
		GTAO = GTAOMultiBounce(GTAO.r, Albedo);
	#endif
	
	return half4(GTAO, 1);
}

//////DeBug RO
half4 DeBugGTRO_frag(PixelInput IN) : SV_Target
{
	half2 uv = IN.uv.xy;

	//////AO & MultiBounce
	half2 GT_Occlusion = SAMPLE_TEXTURE2D(_CurrRT, my_linear_clamp_sampler, uv).rg;
	half GTRO = GT_Occlusion.g;
	
	return GTRO;
}

//////DeBug BentNormal
//half4 DeBugBentNormal_frag(PixelInput IN) : SV_Target
//{
//	half2 uv = IN.uv.xy;
//	return half4(SAMPLE_TEXTURE2D(_BentNormal_Texture, my_linear_clamp_sampler, uv).rgb * 0.5 + 0.5, 1);
//}




////////Combien Reflection Color
//half4 CombienGTRO_frag(PixelInput IN) : SV_Target
//{
//	half2 uv = IN.uv.xy;
//	half depth = tex2D(_CameraDepthTexture, uv).r;
//	half4 sceneColor = tex2D(_AO_Scene_Color, uv);
//	half4 specular = tex2D(_CameraGBufferTexture1, uv);
//	half roughness = 1 - specular.a;

//	half4 worldPos = mul(_Inverse_View_ProjectionMatrix, half4(half3(uv * 2 - 1, depth), 1));
//	worldPos.xyz /= worldPos.w;

//	half3 worldNormal = tex2D(_CameraGBufferTexture2, uv).rgb * 2 - 1;
//	half4 bentNormal = tex2D(_CurrRT, uv);

//	//////Reflection Occlusion
//	half3 viewVector = normalize(worldPos.xyz - _WorldSpaceCameraPos.rgb);
//	half3 reflectionDir = reflect(viewVector, worldNormal);

//	half4 relfectionColor = tex2D(_CameraReflectionsTexture, uv);
//	half groundTruth_RO = ReflectionOcclusion(bentNormal.rgb, reflectionDir, roughness, 0.5);

//	return relfectionColor * groundTruth_RO;
//}
