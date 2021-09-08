Shader "Hidden/3/NoteHistory"
{
	Properties
	{
		[NonModifiableTextureData] [HideInInspector] _AudioLink ("Texture", 2D) = "black" {}
	}

	SubShader
	{
		Lighting Off
		Blend One Zero

		Pass
		{
			CGPROGRAM
			#include "Assets/AudioLink/Shaders/AudioLink.cginc"
			#define _SelfTexture2D _JunkTexture
			#include "UnityCustomRenderTexture.cginc"
			#undef _SelfTexture2D
			Texture2D<float4> _SelfTexture2D;

			#pragma vertex CustomRenderTextureVertexShader
			#pragma fragment frag


			float4 frag(const v2f_customrendertexture IN) : SV_Target
			{
				uint2 pixel = IN.localTexcoord*float2(12,240);
				float sum = 0;
                for (uint i = 0; i < 10; i++)
                {
                    sum += AudioLinkGetAmplitudeAtNote(i, pixel.x);
                    sum += AudioLinkGetAmplitudeAtNote(i, pixel.x + 0.5);
                }
                sum = sum/20;
				if (pixel.y < 2) return sum;
				return AudioLinkGetSelfPixelData(uint2(pixel.x,pixel.y-1));
			}
			ENDCG
		}
	}
}