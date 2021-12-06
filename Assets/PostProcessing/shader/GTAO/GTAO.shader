Shader "Hidden/GroundTruthAmbientOcclusion"
{
	HLSLINCLUDE
		#include "GTAO_Common.cginc"
		#pragma target 5.0
	ENDHLSL

	SubShader
	{
		ZTest Always
		Cull Off
		ZWrite Off

		Pass //0
		{ 
			Name "ResolveGTAO"
			HLSLPROGRAM 
				#include "GTAO_AO.hlsl"
				#pragma multi_compile _ TEMPORAL
				#pragma vertex vert
				#pragma fragment ResolveGTAO_frag
			ENDHLSL
		}
		
		Pass //1
		{ 
			Name "SpatialGTAO"
			HLSLPROGRAM 
				#include "GTAO_Spatial.hlsl"
				#pragma vertex vert
				#pragma fragment SpatialDenoise
			ENDHLSL
		}

		Pass //2
		{ 
			Name "TemporalGTAO"
			HLSLPROGRAM 
				#include "GTAO_Temporal.hlsl"
				#pragma vertex vert
				#pragma fragment TemporalGTAO_frag
			ENDHLSL
		}

		Pass //3
		{ 
			Name "BlurGTAO_V"
			HLSLPROGRAM 
				#include "GTAO_Blur.hlsl"
				#pragma vertex vertBlurVertical
				#pragma fragment BlurGTAO_frag
			ENDHLSL
		}		
	
		Pass //4
		{ 
			Name "BlurGTAO_H"
			HLSLPROGRAM 
				#include "GTAO_Blur.hlsl"
				#pragma vertex vertBlurHorizontal
				#pragma fragment BlurGTAO_frag
			ENDHLSL
		}




		Pass //5
		{ 
			Name "CombienGTAO"
			HLSLPROGRAM 
				#include "GTAO_Combien&Debug.hlsl"
				#pragma multi_compile _ _AO_MultiBounce
				#pragma vertex vert
				#pragma fragment CombienGTAO_frag
			ENDHLSL
		}


		Pass //6
		{ 
			Name "DeBugGTAO"
			HLSLPROGRAM 
				#include "GTAO_Combien&Debug.hlsl"
				#pragma multi_compile _ _AO_MultiBounce
				#pragma vertex vert
				#pragma fragment DeBugGTAO_frag
			ENDHLSL
		}

		Pass //7
		{ 
			Name "DeBugGTRO"
			HLSLPROGRAM 
				#include "GTAO_Combien&Debug.hlsl"
				#pragma vertex vert
				#pragma fragment DeBugGTRO_frag
			ENDHLSL
		}

		//Pass 
		//{ 
		//	Name"BentNormal"
		//	HLSLPROGRAM 
		//		#include "GTAO_Combien&Debug.hlsl"
		//		#pragma vertex vert
		//		#pragma fragment DeBugBentNormal_frag
		//	ENDHLSL
		//}

	}
}

