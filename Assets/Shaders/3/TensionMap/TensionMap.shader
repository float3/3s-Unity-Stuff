// Code by _3, Razgriz
// based on
// https://forum.unity.com/threads/beyond-wrinkle-maps-to-realtime-tension-maps-current-state-of-the-unity-possibilities.509473/#post-5202389
// https://github.com/ted10401/GeometryShaderCookbook
// https://github.com/Xiexe/Unity-Lit-Shader-Templates/tree/refactor

// Proof-of-concept tensionmap shader
// Based on method and code by MrArcher on the Unity Forums (Thanks!)
// Requires BakeEdgeLength.cs to bake edge length data

Shader "Razgriz/TensionMap"
{
Properties
{
_TriangleLengthBuffer           ("Baked Edge Length Data", 2D)                          = "white" {}
_CompressionTensionMultiplier   ("Multiplier", Range(-50.0, 50.0))                      = 1.005
_TensionBlendStrength           ("Tension Blend Strength", Range(0.0, 10.0))            = 1.0
_CompressionBlendStrength       ("Compression Blend Strength", Range(0.0, 10.0))        = 1.0        
_TensionBlendThreshold          ("Tension Blend Threshold", Range(-100, 100))           = 0.0
_CompressionBlendThreshold      ("Squish Blend Threshold", Range(-100, 100))            = 0.0
_TensionExponent                ("Tension Exponent", Range(0,10))                       = 1.0
_DistanceScale                  ("Distance Scale", Range(1, 200))                       = 50.0
_TensionScale                   ("Tension Scale", Range(0,10))                          = 1.0
_POTTexSize                     ("Tri Length Buffer Texture Size", int)                 = 64
}

SubShader
{
    Tags { "RenderType"="Opaque" }
    LOD 100

    Pass
    {
        CGPROGRAM
        #pragma vertex vert
        #pragma geometry geom
        #pragma fragment frag

        #include "UnityCG.cginc"
        #define lengthResolution 0.000001;



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
            float tension : TEXCOORD1;
        };



        sampler2D _TriangleLengthBuffer;
        float4 _TriangleLengthBuffer_ST;
        uniform float4 _TriangleLengthBuffer_TexelSize;
        float _POTTexSize;

        float _CompressionTensionMultiplier;
        float _TensionBlendStrength;
        float _CompressionBlendStrength;
        float _TensionBlendThreshold;
        float _CompressionBlendThreshold;
        float _TensionExponent;
        float _DistanceScale;
        float _TensionScale;



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

            // Tri Perimeter
            float length =  distance(input[0].vertex, input[1].vertex) + 
                            distance(input[1].vertex, input[2].vertex) + 
                            distance(input[2].vertex, input[0].vertex);

            // Index square texture based on index (starts at lower left)
            float x = fmod((float)fragID,_POTTexSize);
            float y = floor((float)fragID/_POTTexSize);
            float2 xy = (float2(x,y) + 0.5) / _POTTexSize;

            float4 oc = tex2Dlod(_TriangleLengthBuffer, float4(xy, 0, 0));
            float originalLength = (oc.r + oc.g*255.0 + oc.b*255.0*255.0 + oc.a*255.0*255.0*255.0)*255.0*lengthResolution;
            float diff = (length - originalLength * _CompressionTensionMultiplier);

            for(int i = 0; i < 3; i++)
            {
                o.vertex = UnityObjectToClipPos(input[i].vertex);
                o.uv = input[i].uv;

                if (diff > 0)
                    o.tension = pow(_TensionBlendStrength * (diff * _DistanceScale - _TensionBlendThreshold), _TensionExponent) * _TensionScale;
                else
                    o.tension = -pow(_CompressionBlendStrength * (abs(diff) * _DistanceScale + _CompressionBlendThreshold), _TensionExponent) * _TensionScale;
                
                outStream.Append(o);
            }

            outStream.RestartStrip();
        }

        float4 frag (g2f i) : SV_Target
        {
            float squash = saturate(-i.tension);
            float stretch = saturate(i.tension);
            return float4(squash, stretch, saturate(1 - squash - stretch), 1);// saturate(1 - squash - stretch), 1);
        }


        ENDCG
    }
} 
}
