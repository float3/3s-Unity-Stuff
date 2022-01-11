Shader "3/hmd-detect"
{
	Properties {}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Assets/Polyrhythm/Shader/FortyPixelPrint.cginc"

			// https://vr-compare.com
			// FOV should be stored as the unity_CameraProjection._m11 value instead of as the FOV value to avoid precision loss

			#define FORTE_VFX1 253/230


			#define OCULUS_RIFT_DK1 640/800


			#define OCULUS_RIFT_DK2 960/1080

			#define PLAYSTATION_VR 960/1080


			#define HTC_VIVE 1080/1200

			#define OCULUS_RIFT 1080/1200


			#define OCULUS_GO 1280/1440


			#define VALVE_INDEX 1440/1600

			#define OCULUS_QUEST 1440/1600


			#define OCULUS_RIFT_S 1648/1774 //roughly


			#define PIMAX_ARTISAN 1700/1440


			#define OCULUS_QUEST_2 1832/1920

			#define PICO_NEO_3 1832/1920


			#define HP_REVERB_G2 2160/2160


			#define PIMAX_5K_SUPER 2560/1440

			#define VRGINEERS_XTAL 2560/1440


			#define HTC_VIVE_PRO_2 2448/2448

			#define HTC_VIVE_FOCUS_3 2448/2448


			#define ARPARA_VR 2560/2560


			#define VARJO_AERO 2880/2720


			#define PIMAX_VISION_8K_PLUS 3840/2160

			#define PIMAX_VISION_8KX 3840/2160

			#define VRGINEERS_XTAL_8k 3840/2160

			#define PIMAX_REALITY_12K_QLED 5670/3240

			struct appdata
			{
				float4 vertex : POSITION;
				float4 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 uv : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};


			v2f vert(appdata v)
			{
				v2f o;
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.uv = v.uv;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}


			float4 frag(v2f i) : SV_Target
			{
				float2 uv6 = (i.uv.xy * 6);
				float print;
				int wholecount;
				int decimalcount;
				switch ((int)uv6.y)
				{
				case 5:
					print = _ScreenParams.x;
					wholecount = 4;
					decimalcount = 2;
					break;
				case 4:
					print = _ScreenParams.y;
					wholecount = 4;
					decimalcount = 2;
					break;
				case 3:
					print = _ScreenParams.z;
					wholecount = 1;
					decimalcount = 5;
					break;
				case 2:
					print = _ScreenParams.w;
					wholecount = 1;
					decimalcount = 5;
					break;
				case 1:
					print = _ScreenParams.x / _ScreenParams.y;
					decimalcount = 5;
					wholecount = 1;
					break;
				case 0:
					print = unity_CameraProjection._m11;
					wholecount = 1;
					decimalcount = 5;
					break;
				default:
					print = 0;
					wholecount = 1;
					decimalcount = 0;
					break;
				}
				return float4(print5x7float(print, float2(i.uv.x, uv6.y), wholecount, decimalcount), 1);
			}
			ENDCG
		}
	}
}