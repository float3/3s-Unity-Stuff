Shader "Polyrhythm/Polyrhythm"
{
	Properties
	{
		_x ("X",int) = 3
		_y ("Y",int) = 4
	}
	SubShader
	{
		Tags
		{
			"PreviewType"="Plane"
		}
		Pass
		{
			Tags
			{
				"LightMode"="ForwardBase"
			}
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Assets/Polyrhythm/Shader/FortyPixelPrint.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			int _x;
			int _y;
			static float2 uvGrid;
			static float2 uvFloor;
			static float2 uvCeil;

			float3 frag(v2f i) : SV_Target
			{
				i.uv.y = 1.0 - i.uv.y;
				uvGrid = i.uv * float2(_x, _y);
				uvGrid += 1;


				if (any(abs(uvGrid - floor(uvGrid)) < 0.05) || any(abs(uvGrid - ceil(uvGrid)) < 0.05))
				{
					return float3(1, 1, 1);
				}

				float3 a = 1;

				//index = (int)uvGrid.x * (int)uvGrid.y;
				uint index = ((int)uvGrid.y - 1) * _x + (int)uvGrid.x;

				int places = 1;
				if ((int)uvGrid.x >= 10)
				{
					places = 2;
				}


				a = print5x7intzl((int)uvGrid.x, float2(uvGrid.x % 1, (1 - uvGrid.y) % 1) + float2(-0.075, 0.05), places);


				if ((int)uvGrid.x == 1 || index % _y == 1)
				{
					a *= float3(1, 0, 0);
				}

				return a;
			}
			ENDCG
		}
	}
}