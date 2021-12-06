#include "GTAO_Common.cginc"

bool PixelSetAsNoMotionVectors(float4 inBuffer)
{
	return inBuffer.x > 1.0f;
}

void DecodeMotionVector(float4 inBuffer, out float2 motionVector)
{
    motionVector = PixelSetAsNoMotionVectors(inBuffer) ? 0.0f : inBuffer.xy;
}

half3 FindMinMaxAvgAO(half2 uv)
{
    float minAO = 2.0f;
    float maxAO = -2.0f;
    float avg = 0;
    for (int i = -1; i <= 1; ++i)
    {
        for (int j = -1; j <= 1; ++j)
        {
            half currAO = SAMPLE_TEXTURE2D(_GTAO_Spatial_Texture, my_point_clamp_sampler, uv + half2(i, j) * _AO_RT_TexelSize.xy).r;
            avg += currAO;
            minAO = min(minAO, currAO);
            maxAO = max(maxAO, currAO);
        }
    }

    return float3(minAO, maxAO, avg/9);
}
//////Temporal filter
half3 TemporalGTAO_frag(PixelInput IN) : SV_Target
{
	half2 uv =IN.uv;
    half2 currFrameData = SAMPLE_TEXTURE2D(_GTAO_Spatial_Texture, my_point_clamp_sampler, uv).rg;

    half currAO = currFrameData.x;
    half currDepth = currFrameData.y;


//#if HALF_RES
//    float2 closest = posInputs.positionSS * 2;
//#else
//    float2 closest = posInputs.positionSS;
//#endif
    float2 motionVector;
    DecodeMotionVector(SAMPLE_TEXTURE2D(_CameraMotionVectorsTexture, my_point_clamp_sampler, uv), motionVector);
    float motionVecLength = length(motionVector);
    float motionVecWeighting = saturate(motionVecLength * 100.0);

    //float2 uv = (dispatchThreadId.xy + 0.5) * _AOBufferSize.zw;
    //float2 prevFrameNDC = uv - motionVector;

    half3 prevData = SAMPLE_TEXTURE2D(_PrevRT, my_point_clamp_sampler, uv - motionVector).rgb;
    half prevAO = prevData.x;
    half prevDepth = prevData.y;
    half prevMotionVecLen = prevData.z;


    float velWeight = 1.0f - saturate((abs(prevMotionVecLen - motionVecWeighting)) * 3.0f);

    float3 minMax = FindMinMaxAvgAO(uv);
    float minAO = minMax.x;
    float maxAO = minMax.y;
    float avg = minMax.z;
    float nudge = lerp(_AOTemporalUpperNudgeLimit, _AOTemporalLowerNudgeLimit, motionVecWeighting) * abs(avg - currAO);
    minAO -= nudge;
    maxAO += nudge;

    float diff = abs(currAO - prevAO) / Max3(prevAO, currAO, 0.1f);
    float weight = 1.0 - diff;
    float feedback = lerp(0.85, 0.95, weight * weight);

    prevAO = clamp(prevAO, minAO, maxAO);

    float depth_similarity = saturate(pow(prevDepth / currDepth, 1) + 0.01);

    float newAO = (lerp(currAO, prevAO, feedback * depth_similarity * velWeight));

    return half3(newAO, currDepth, motionVecWeighting);
}