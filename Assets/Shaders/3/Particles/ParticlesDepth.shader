Shader "3/Particles/DepthParticle"
{
    Properties
    {
        _TintColor("Tint Color", Color) = (0.5,0.5,0.5,0.5)
        _MainTex("Particle Texture", 2D) = "white" {}
        _InvFade("Soft Particles Factor", Range(0.01,3.0)) = 1.0
        _FadeLength("FadeLength", Float) = 3
        _FadeOffset("FadeOffset", Float) = 10
        _FadeNear("FadeNear", Color) = (1,0,0,0)
        _FadeFar("FadeFar", Color) = (0,1,0,0)
    }
    Category
    {
        SubShader
        {
            Tags
            {
                "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            Lighting Off
            ZWrite Off
            ZTest LEqual

            Pass
            {

                CGPROGRAM
                #ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
					#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
                #endif

                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0
                #pragma multi_compile_instancing
                #pragma multi_compile_particles
                #pragma multi_compile_fog


                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    fixed4 color : COLOR;
                    float4 texcoord : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    fixed4 color : COLOR;
                    float4 texcoord : TEXCOORD0;
                    UNITY_FOG_COORDS(1)
                    #ifdef SOFTPARTICLES_ON
						float4 projPos : TEXCOORD2;
                    #endif
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                    float4 texcoord3 : TEXCOORD3;
                };


                #if UNITY_VERSION >= 560
					UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
                #else
                uniform sampler2D_float _CameraDepthTexture;
                #endif

                // uniform sampler2D_float _CameraDepthTexture;

                uniform sampler2D _MainTex;
                uniform fixed4 _TintColor;
                uniform float4 _MainTex_ST;
                uniform float _InvFade;
                uniform float _FadeLength;
                uniform float _FadeOffset;
                uniform float4 _FadeNear;
                uniform float4 _FadeFar;

                float3 HSVToRGB(float3 c)
                {
                    float4 k = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                    const float3 p = abs(frac(c.xxx + k.xyz) * 6.0 - k.www);
                    return c.z * lerp(k.xxx, saturate(p - k.xxx), c.y);
                }

                float3 RGBToHSV(float3 c)
                {
                    float4 k = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                    float4 p = lerp(float4(c.bg, k.wz), float4(c.gb, k.xy), step(c.b, c.g));
                    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                    const float d = q.x - min(q.w, q.y);
                    const float e = 1.0e-10;
                    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
                }


                v2f vert(appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    UNITY_TRANSFER_INSTANCE_ID(v, o);
                    const float3 customSurfaceDepth = v.vertex.xyz;
                    const float customEye = -UnityObjectToViewPos(customSurfaceDepth).z;
                    o.texcoord3.x = customEye;


                    o.texcoord3.yzw = 0;

                    v.vertex.xyz += float3(0, 0, 0);
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    #ifdef SOFTPARTICLES_ON
						o.projPos = ComputeScreenPos(o.vertex);
						COMPUTE_EYEDEPTH(o.projPos.z);
                    #endif
                    o.color = v.color;
                    o.texcoord = v.texcoord;
                    UNITY_TRANSFER_FOG(o, o.vertex);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    UNITY_SETUP_INSTANCE_ID(i);
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                    #ifdef SOFTPARTICLES_ON
						float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
						float partZ = i.projPos.z;
						float fade = saturate(_InvFade * (sceneZ - partZ));
						i.color.a *= fade;
                    #endif

                    const float customEye = i.texcoord3.x;
                    const float cameraDepthFade = ((customEye - _ProjectionParams.y - _FadeOffset) / _FadeLength);
                    float clampResult = clamp(cameraDepthFade, 0.0, 1.0);
                    const float2 appendResult = (float2((1.0 - clampResult), clampResult));
                    float2 weightedBlendVar = appendResult;
                    float4 weightedAvg = ((weightedBlendVar.x * _FadeNear + weightedBlendVar.y * _FadeFar) / (
                        weightedBlendVar.x + weightedBlendVar.y));
                    float3 hsvTorgb = RGBToHSV(weightedAvg.rgb);
                    float3 hsvTorgb1 = RGBToHSV(_FadeNear.rgb);
                    float3 hsvTorgb2 = RGBToHSV(_FadeFar.rgb);
                    float3 hsvTorgb3 = HSVToRGB(float3(hsvTorgb.x, ((hsvTorgb1.y + hsvTorgb2.y) / 2.0),
                                                       ((hsvTorgb1.z + hsvTorgb2.z) / 2.0)));
                    const float2 uv_MainTex = i.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
                    const float4 appendResult2 = (float4(hsvTorgb3, (i.color.a * tex2D(_MainTex, uv_MainTex).a)));


                    fixed4 col = appendResult2;
                    UNITY_APPLY_FOG(i.fogCoord, col);
                    return col;
                }
                ENDCG
            }
        }
    }
}