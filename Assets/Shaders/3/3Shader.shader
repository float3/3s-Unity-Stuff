Shader "3/3Shader"{
	Properties
	{
		_Maintex("Main tex", 2d) = {white}
		_Color("Color", Color) = (1,1,1,1)
		_MainTint("MainTint", Color) = (1,1,1,1)
		
		_Bumpmap("Bumpmap", 2D) = {bump}
		_BumpmapScale("BumpmapScale", Range(0,10)) = 1.0

		_Par

		_ZWrite ("ZWrite", Float)
	}
	SubShader
	{
		//Tags {"Queue" = }
		
		Pass 
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_instancing
			#pragma multi_compile_fog

			struct appdata 
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float4 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
				float4 uv0.xy : TEXCOORD0;
				float4 uv0.zw : TEXCOORD1;
				UNITY_FOG_COORDS(1)
				UNITY_VERETX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				return o;
			}

			float4 frag(v2f i)
			{
				float4 finalcolor = Tex2Dlod(_Maintex, _MainTex_ST) * _MainColor;
				UNITY_APPLY_FOG(i.fogCoord, finalcol);
				return finalcolor;
			}
			ENDCG
		}
	}
}