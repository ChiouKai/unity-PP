Shader "Hidden/PP/NaNKiller"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            CGPROGRAM
            #pragma exclude_renderers gles
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            bool IsNaN(float x)
            {
                return (asuint(x) & 0x7FFFFFFF) > 0x7F800000;
            }
            bool AnyNAN(float3 v)
            {
                return IsNaN(v.x) || IsNaN(v.y) || IsNaN(v.z);
            }
            bool IsInf(float x)
            {
                return (asuint(x) & 0x7FFFFFFF) == 0x7F800000;
            }
            bool AnyIsInf(float3 v)
            {
                return IsInf(v.x) || IsInf(v.y) || IsInf(v.z);
            }





            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {

                half3 col = tex2D(_MainTex, i.uv);

                if(AnyNAN(col) || AnyIsInf(col))
                    col = half3(0, 0, 0);

                return half4(col, 1.0);
            }
            ENDCG
        }
    }
}
