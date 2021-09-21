Shader "Unlit/BILLBOARD"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;

                float3 camPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1));
                float3 baseZ = normalize(camPos);
                float3 baseY = float3(0, 1, 0);
                float3 baseX = cross(baseZ, baseY);
                float3x3 rot = transpose(float3x3(baseX, baseY, baseZ));
                v.vertex.xyz = mul(rot, v.vertex);
                o.vertex = UnityObjectToClipPos(v.vertex);

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return 1;
            }
            ENDCG
        }
    }
}
