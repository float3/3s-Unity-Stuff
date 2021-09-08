Shader "3/NoteDetection"
{
    Properties
    {
        [NonModifiableTextureData] [HideInInspector] _AudioLink ("Texture", 2D) = "black" {}
        [NonModifiableTextureData] [HideInInspector] _NoteArray ("Note Array", 2DArray) = "" {} 
        }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include <Assets/AudioLink/Shaders/AudioLink.cginc>
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                nointerpolation float2 index: TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _AudioLink;
            sampler2D _DFTHistory;
            UNITY_DECLARE_TEX2DARRAY(_NoteArray);

            v2f vert (const appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                float local_max = 0;
                uint index = 0;

                for(int j=0; j<12; j++) 
                {
                    float amplitude = 0;
                    for(int i=0; i<10; i++)
                    {
                        amplitude +=  saturate(AudioLinkGetAmplitudeAtNote(i,j));
                    }
                    if (amplitude > local_max) {
                        local_max = amplitude;
                        index = j;
                    }
                }
                o.index = float2(index,local_max);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                const float index = i.index.x;

                const uint notearray[12] = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10,11,};
                uint note = notearray[index];
                return UNITY_SAMPLE_TEX2DARRAY (_NoteArray, float3(i.uv,note));
            }
            ENDCG
        }
    }
}