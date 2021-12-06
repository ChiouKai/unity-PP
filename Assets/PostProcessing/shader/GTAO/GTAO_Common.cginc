#ifndef _GTAO_Common
#define _GTAO_Common

#include "Assets/PostProcessing/shader/Copy/URP/ShaderLibrary/Core.hlsl"
#include "Filtter_Library.hlsl"

#define KERNEL_RADIUS 8


half4 _AOParams0;
#define _AO_Intensity _AOParams0.x
#define _AO_Radius _AOParams0.y
#define _AO_DirSampler _AOParams0.z
#define _AO_SliceSampler _AOParams0.w

half4 _AOParams1;
#define _AO_HalfProjScale _AOParams1.x
#define _AOTemporalUpperNudgeLimit _AOParams1.y
#define _AOTemporalLowerNudgeLimit _AOParams1.z
#define _AOSpatialBilateralAggressiveness _AOParams1.w

half4	_AO_UVToView, _AO_RT_TexelSize;

half2 _AOParams2;
#define _AO_TemporalDirections _AOParams2.x
#define _AO_TemporalOffsets _AOParams2.y



half4x4	_WorldToCameraMatrix, _CameraToWorldMatrix, _Inverse_View_ProjectionMatrix;


TEXTURE2D(_CameraGBufferTexture0);
TEXTURE2D(_CameraGBufferTexture1);
TEXTURE2D(_CameraGBufferTexture2);
TEXTURE2D(_CameraReflectionsTexture);
TEXTURE2D(_CameraMotionVectorsTexture);
TEXTURE2D(_CameraDepthNormalsTexture);
TEXTURE2D(_CameraDepthTexture);

TEXTURE2D(_MainTex);
TEXTURE2D(_AO_Scene_Color);
TEXTURE2D(_BentNormal_Texture);
TEXTURE2D(_GTAO_Texture);
TEXTURE2D(_GTAO_Spatial_Texture);
TEXTURE2D(_PrevRT);
TEXTURE2D(_CurrRT);
SAMPLER(my_point_clamp_sampler);
SAMPLER(my_linear_clamp_sampler); 


struct VertexInput
{
	half4 vertex : POSITION;
	half2 uv : TEXCOORD0;
};

struct PixelInput
{
	half4 vertex : SV_POSITION;
	half2 uv : TEXCOORD0;
};
struct BlurlInput
{
	half4 vertex : SV_POSITION;
	half2 uv[5]: TEXCOORD0;
};

PixelInput vert(VertexInput v)
{
	PixelInput o;
	o.vertex = v.vertex;
	o.uv = v.uv;
	return o;
}


#endif