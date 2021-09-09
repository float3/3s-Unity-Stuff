Shader "3/TensionMap"
{
    Properties
    {
        _TriangleLengthBuffer ("Baked Triangle Length Data", 2D) = "white" {}
        _SquashStretchOffset ("Squash Stretch Offset", Range(0.0, 2.0)) = 1.0
        _StretchBlendStrength ("Stretch Blend Strength", Range(0.0, 2.0)) = 1.0
        _SquashBlendStrength ("Squash Blend Strength", Range(0.0, 2.0)) = 1.0        
        _StretchBlendThreshold ("Stretch Blend Threshold", Range(-100, 100)) = 0.0
        _SquashBlendThreshold ("Squish Blend Threshold", Range(-100, 100)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct a2v
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2g
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 len : TEXCOORD1;
                float2 triLength : TEXCOORD2;
            };

            sampler2D _TriangleLengthBuffer;
            float4 _TriangleLengthBuffer_ST;
            float _TotalTriCount = 1376.0;

            float _SquashStretchOffset;
            float _StretchBlendStrength;
            float _SquashBlendStrength;
            float _StretchBlendThreshold;
            float _SquashBlendThreshold;

            v2g vert (a2v v)
            {
                v2g o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> outStream, uint fragID : SV_PrimitiveID)
            {
                g2f o;

                float l =   distance(input[0].vertex, input[1].vertex) + 
                            distance(input[1].vertex, input[2].vertex) + 
                            distance(input[2].vertex, input[0].vertex);
                o.len = l;

                float originalLength = tex2Dlod(_TriangleLengthBuffer, float4(((float)(fragID)) / _TotalTriCount, 0.5, 0, 0));
                float diff = (l - originalLength * _SquashStretchOffset);

                o.triLength = float2(0,0);


                for(int i = 0; i < 3; i++)
                {
                    o.vertex = UnityObjectToClipPos(input[i].vertex);
                    o.uv = input[i].uv;
                    outStream.Append(o);
                    
                    if (diff > 0)
                        o.triLength = fixed2(0, pow(_StretchBlendStrength * (diff * 50 - _StretchBlendThreshold), 3));
                    else
                        o.triLength = fixed2(-pow(_SquashBlendStrength * (diff * 50 + _SquashBlendThreshold), 3), 0);
                    }
                
                outStream.RestartStrip();
            }

            float4 frag (g2f i) : SV_Target
            {
                float squash = saturate(i.triLength.x);
                float stretch = saturate(i.triLength.y);
                return float4(squash, stretch, 1 - squash - stretch, 1);
            }
            ENDCG
        }
    }
}