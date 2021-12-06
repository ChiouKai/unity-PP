#include "GTAO_Common.cginc"

#define BILATERAL_EPSILON 0.01
float BilateralWeight(float sampleDepth, float linearCentralDepth)
{
    float linearSample = LinearEyeDepth(sampleDepth, _ZBufferParams);
    float delta = abs(linearSample - linearCentralDepth);
    float w = saturate(1.0f - (_AOSpatialBilateralAggressiveness * delta + BILATERAL_EPSILON));

    return w;
}


void GatherAOData(float2 UV, out float4 AOs, out float4 depths)
{
    AOs = GATHER_TEXTURE2D(_GTAO_Texture, my_point_clamp_sampler, UV);
    depths = GATHER_GREEN_TEXTURE2D(_GTAO_Texture, my_point_clamp_sampler, UV);
}




half2 SpatialDenoise(PixelInput IN) : SV_Target
{
    float4 UnpackedAOs, UnpackedDepths;
    half2 uv = IN.uv;
    float2 UV = uv /*+ float2(-2.0, 0.0) * _AO_RT_TexelSize.xy*/;
    GatherAOData(UV, UnpackedAOs, UnpackedDepths);

    half centralDepth = UnpackedDepths.w;
    float linearCentralDepth = LinearEyeDepth(centralDepth, _ZBufferParams);

    float total = UnpackedAOs.w;
    float totalWeight = 1;

    // This manual unrolling is horrible looking, but I found it hard to please the PS4 compiler otherwise. TODO: Make this nicer.

    // First set of gathered data.
    float weight = BilateralWeight(UnpackedDepths.x, linearCentralDepth);
    total += weight * UnpackedAOs.x;
    totalWeight += weight;

    weight = BilateralWeight(UnpackedDepths.y, linearCentralDepth);
    total += weight * UnpackedAOs.y;
    totalWeight += weight;

    weight = BilateralWeight(UnpackedDepths.z, linearCentralDepth);
    total += weight * UnpackedAOs.z;
    totalWeight += weight;

    // Second set of gathered data.
    UV = uv + float2(-2.0, 0.0) * _AO_RT_TexelSize.xy;
    GatherAOData(UV, UnpackedAOs, UnpackedDepths);

    weight = BilateralWeight(UnpackedDepths.x, linearCentralDepth);
    total += weight * UnpackedAOs.x;
    totalWeight += weight;

    weight = BilateralWeight(UnpackedDepths.y, linearCentralDepth);
    total += weight * UnpackedAOs.y;
    totalWeight += weight;

    weight = BilateralWeight(UnpackedDepths.z, linearCentralDepth);
    total += weight * UnpackedAOs.z;
    totalWeight += weight;

    weight = BilateralWeight(UnpackedDepths.w, linearCentralDepth);
    total += weight * UnpackedAOs.w;
    totalWeight += weight;


    // Third set of gathered data.
    UV = uv + float2(0.0, -2.0) * _AO_RT_TexelSize.xy;
    GatherAOData(UV, UnpackedAOs, UnpackedDepths);

    weight = BilateralWeight(UnpackedDepths.x, linearCentralDepth);
    total += weight * UnpackedAOs.x;
    totalWeight += weight;

    weight = BilateralWeight(UnpackedDepths.y, linearCentralDepth);
    total += weight * UnpackedAOs.y;
    totalWeight += weight;

    weight = BilateralWeight(UnpackedDepths.z, linearCentralDepth);
    total += weight * UnpackedAOs.z;
    totalWeight += weight;

    weight = BilateralWeight(UnpackedDepths.w, linearCentralDepth);
    total += weight * UnpackedAOs.w;
    totalWeight += weight;

    // Fourth set of gathered data.
    UV = uv + float2(-2.0, -2.0) * _AO_RT_TexelSize.xy;
    GatherAOData(UV, UnpackedAOs, UnpackedDepths);

    weight = BilateralWeight(UnpackedDepths.x, linearCentralDepth);
    total += weight * UnpackedAOs.x;
    totalWeight += weight;

    weight = BilateralWeight(UnpackedDepths.y, linearCentralDepth);
    total += weight * UnpackedAOs.y;
    totalWeight += weight;

    weight = BilateralWeight(UnpackedDepths.z, linearCentralDepth);
    total += weight * UnpackedAOs.z;
    totalWeight += weight;

    weight = BilateralWeight(UnpackedDepths.w, linearCentralDepth);
    total += weight * UnpackedAOs.w;
    totalWeight += weight;


    total /= totalWeight;

    return half2(total, centralDepth);
}




//inline void FetchAoAndDepth(float2 uv, inout float ao, inout float depth) {
//	float2 aod = SAMPLE_TEXTURE2D(_GTAO_Texture, my_linear_clamp_sampler, uv).rg;
//	ao = aod.r;
//	depth = aod.g;
//}

//inline float CrossBilateralWeight(float r, float d, float d0) {
//	const float BlurSigma = (float)KERNEL_RADIUS * 0.5;
//	const float BlurFalloff = 1 / (2 * BlurSigma * BlurSigma);

//    float dz = (d0 - d) * _ProjectionParams.z * _AO_Sharpeness;
//	return exp2(-r * r * BlurFalloff - dz * dz);
//}

//inline void ProcessSample(float2 aoz, float r, float d0, inout float totalAO, inout float totalW) {
//	float w = CrossBilateralWeight(r, d0, aoz.y);
//	totalW += w;
//	totalAO += w * aoz.x;
//}

//inline void ProcessRadius(float2 uv0, float2 deltaUV, float d0, inout float totalAO, inout float totalW) {
//	float ao, z;
//	float2 uv;
//	float r = 1;

//	UNITY_UNROLL
//	for (; r <= KERNEL_RADIUS / 2; r += 1) {
//		uv = uv0 + r * deltaUV;
//		FetchAoAndDepth(uv, ao, z);
//		ProcessSample(float2(ao, z), r, d0, totalAO, totalW);
//	}

//	UNITY_UNROLL
//	for (; r <= KERNEL_RADIUS; r += 2) {
//		uv = uv0 + (r + 0.5) * deltaUV;
//		FetchAoAndDepth(uv, ao, z);
//		ProcessSample(float2(ao, z), r, d0, totalAO, totalW);
//	}
		
//}

//inline float2 BilateralBlur(float2 uv0, float2 deltaUV)
//{
//	float totalAO, depth;
//	FetchAoAndDepth(uv0, totalAO, depth);
//	float totalW = 1;
		
//	ProcessRadius(uv0, -deltaUV, depth, totalAO, totalW);
//	ProcessRadius(uv0, deltaUV, depth, totalAO, totalW);

//	totalAO /= totalW;
//	return float2(totalAO, depth);
//}


//half2 SpatialGTAO_X_frag(PixelInput IN) : SV_Target
//{
//	half2 uv = IN.uv.xy;
//	half2 AO = BilateralBlur(uv, half2(1 / _ScreenParams.x, 0));
//	return AO;
//} 

//half2 SpatialGTAO_Y_frag(PixelInput IN) : SV_Target
//{
//	half2 uv = IN.uv.xy;
//	half2 AO = BilateralBlur(uv, half2(0, 1 / _ScreenParams.y));


//	//////Reflection Occlusion
//	half3 bentNormal = SAMPLE_TEXTURE2D(_BentNormal_Texture, my_linear_clamp_sampler, uv).rgb;
//	half3 worldNormal = SAMPLE_TEXTURE2D(_CameraGBufferTexture2, my_linear_clamp_sampler, uv).rgb * 2 - 1;
//	half4 Specular = SAMPLE_TEXTURE2D(_CameraGBufferTexture1, my_linear_clamp_sampler, uv);
//	half Roughness = 1 - Specular.a;

//	half Depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, my_point_clamp_sampler, uv).r;
//	half4 worldPos = mul(_Inverse_View_ProjectionMatrix, half4(half3(uv * 2 - 1, Depth), 1));
//	worldPos.xyz /= worldPos.w;

//	half3 viewDir= normalize(worldPos.xyz - _WorldSpaceCameraPos.rgb);
//	half3 reflectionDir = reflect(viewDir, worldNormal);
//	half GTRO = ReflectionOcclusion(bentNormal, reflectionDir, Roughness, 0.5);

//	return lerp(1, half2(AO.r, GTRO), _AO_Intensity);
//} 

