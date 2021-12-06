#include "GTAO_Common.cginc"

BlurlInput vertBlurVertical(VertexInput v) 
{
	BlurlInput o;
	o.vertex = v.vertex;
	
	half2 uv = v.uv;
	
	o.uv[0] = uv;
	o.uv[1] = uv + float2(0.0, _AO_RT_TexelSize.y * 1.0) /** _BlurSize*/;
	o.uv[2] = uv - float2(0.0, _AO_RT_TexelSize.y * 1.0) /** _BlurSize*/;
	o.uv[3] = uv + float2(0.0, _AO_RT_TexelSize.y * 2.0) /** _BlurSize*/;
	o.uv[4] = uv - float2(0.0, _AO_RT_TexelSize.y * 2.0) /** _BlurSize*/;
			 
	return o;
}
		
BlurlInput vertBlurHorizontal(VertexInput v) 
{
	BlurlInput o;
	o.vertex = v.vertex;
	
	half2 uv = v.uv;
	
	o.uv[0] = uv;
	o.uv[1] = uv + float2(_AO_RT_TexelSize.x * 1.0, 0.0)/* * _BlurSize*/;
	o.uv[2] = uv - float2(_AO_RT_TexelSize.x * 1.0, 0.0)/* * _BlurSize*/;
	o.uv[3] = uv + float2(_AO_RT_TexelSize.x * 2.0, 0.0)/* * _BlurSize*/;
	o.uv[4] = uv - float2(_AO_RT_TexelSize.x * 2.0, 0.0)/* * _BlurSize*/;
			 
	return o;
}
half BlurGTAO_frag(BlurlInput i) : SV_Target 
{
	half weight[3] = {0.4026, 0.2442, 0.0545};
	
	half sum = SAMPLE_TEXTURE2D(_MainTex, my_linear_clamp_sampler, i.uv[0]).r * weight[0];
	
	for (int it = 1; it < 3; it++) {
		sum += SAMPLE_TEXTURE2D(_MainTex, my_linear_clamp_sampler, i.uv[it*2-1]).r * weight[it];
		sum += SAMPLE_TEXTURE2D(_MainTex, my_linear_clamp_sampler, i.uv[it*2]).r * weight[it];
	}
	
	return sum;
}