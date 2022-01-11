Shader "Polyrhythm/LCM"
{
	Properties
	{
		[ToggleUI] _hasPrime ( "_hasPrime", float) = 1.0
		[ToggleUI] _hasMinSecond ( "_hasMinSecond", float) = 0
		[ToggleUI] _hasMajSecond ( "_hasMajSecond", float) = 0
		[ToggleUI] _hasMinThird ( "_hasMinThird", float) = 0
		[ToggleUI] _hasMajThird ( "_hasMajThird", float) = 1.0
		[ToggleUI] _hasFourth ( "_hasFourth", float) = 0
		[ToggleUI] _hasTritone ( "_hasTritone", float) = 0
		[ToggleUI] _hasFifth ( "_hasFifth", float) = 1.0
		[ToggleUI] _hasMinSixth ( "_hasMinSixth", float) = 0
		[ToggleUI] _hasMajSixth ( "_hasMajSixth", float) = 0
		[ToggleUI] _hasMinSeventh( "_hasMinSeventh", float) = 0
		[ToggleUI] _hasMajSeventh( "_hasMajSeventh", float) = 0
		[ToggleUI] _hasOctave( "_hasOctave", float) = 1.0
		[ToggleUI] _Polyrhythm( "PolyRhythm", float) = 0
		_Root ( "Root", float) = 440
	}
	SubShader
	{
		Tags
		{
			"RenderType"="Opaque"
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			#pragma warning (default : 3206)
			#pragma warning (default : 3569)
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
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			#define FOR 0
			#define WHILE 1
			#define LOOP 1
			#if LOOP == FOR

			uint GCD(uint a, uint b)
			{
				if (a == 0)
					return b;
				if (b == 0)
					return a;
				UNITY_LOOP
				for (int i = 0; i < 10; i++)
				{
					uint c = b;
					b = a % b;
					a = c;
					if (a == 0)
						return b;
					if (b == 0)
						return a;
				}
				return max(a, b);
			}

			#elif LOOP == WHILE

			uint GCD(uint a, uint b)
			{
				if (a == 0)
					return b;
				if (b == 0)
					return a;
				uint c;
				UNITY_LOOP
				while (!any(float2(a, b) == 0))
				{
					c = b;
					b = a % b;
					a = c;
					if (a == 0)
						return b;
					if (b == 0)
						return a;
				}
				return a < b ? a : b;
			}

			#endif

			// uint GCD(uint a, uint b)
			// {
			// 	if (a == 0)
			// 		return b;
			// 	if (b == 0)
			// 		return a;
			// 	return GCD(b, a % b);
			// }

			// uint GCD(uint a, uint b, uint c)
			// {
			// 	return GCD(a, GCD(b, c));
			// }
			//
			// uint GCD(uint a, uint b, uint c, uint d)
			// {
			// 	return GCD(a, GCD(b, c, d));
			// }

			uint LCM(uint a, uint b)
			{
				return (a / GCD(a, b)) * b;
			}

			bool _hasPrime;
			bool _hasMinSecond;
			bool _hasMajSecond;
			bool _hasMinThird;
			bool _hasMajThird;
			bool _hasFourth;
			bool _hasTritone;
			bool _hasFifth;
			bool _hasMinSixth;
			bool _hasMajSixth;
			bool _hasMinSeventh;
			bool _hasMajSeventh;
			bool _hasOctave;

			#define PRIME 1.0/1.0
			#define MINSECOND 16.0/15.0
			#define MAJSECOND 9.0/8.0
			#define MINTHIRD 6.0/5.0
			#define MAJTHIRD 5.0/4.0
			#define FOURTH 4.0/3.0
			#define TRITONE 45.0/32.0
			#define FIFTH 3.0/2.0
			#define MINSIXTH 8.0/5.0
			#define MAJSIXTH 5.0/3.0
			#define MINSEVENTH 7.0/4.0
			#define MAJSEVENTH 15.0/8.0
			#define OCTAVE 2.0/1.0

			#define PRIME_DENOMINATOR 1
			#define MINSECOND_DENOMINATOR 15
			#define MAJSECOND_DENOMINATOR 8
			#define MINTHIRD_DENOMINATOR 5
			#define MAJTHIRD_DENOMINATOR 4
			#define FOURTH_DENOMINATOR 3
			#define TRITONE_DENOMINATOR 32
			#define FIFTH_DENOMINATOR 2
			#define MINSIXTH_DENOMINATOR 5
			#define MAJSIXTH_DENOMINATOR 3
			#define MINSEVENTH_DENOMINATOR 4
			#define MAJSEVENTH_DENOMINATOR 8
			#define OCTAVE_DENOMINATOR 2

			float4 frag(v2f i) : SV_Target
			{
				//Compute LCM of Denominators of Fractions
				uint denomLCM = 1;

				if (_hasPrime)
				{
					denomLCM = LCM(denomLCM,PRIME_DENOMINATOR);
				}
				if (_hasMinSecond)
				{
					denomLCM = LCM(denomLCM,MINSECOND_DENOMINATOR);
				}
				if (_hasMajSecond)
				{
					denomLCM = LCM(denomLCM,MAJSECOND_DENOMINATOR);
				}
				if (_hasMinThird)
				{
					denomLCM = LCM(denomLCM,MINTHIRD_DENOMINATOR);
				}
				if (_hasMajThird)
				{
					denomLCM = LCM(denomLCM,MAJTHIRD_DENOMINATOR);
				}
				if (_hasFourth)
				{
					denomLCM = LCM(denomLCM,FOURTH_DENOMINATOR);
				}
				if (_hasTritone)
				{
					denomLCM = LCM(denomLCM,TRITONE_DENOMINATOR);
				}
				if (_hasFifth)
				{
					denomLCM = LCM(denomLCM,FIFTH_DENOMINATOR);
				}
				if (_hasMinSixth)
				{
					denomLCM = LCM(denomLCM,MINSIXTH_DENOMINATOR);
				}
				if (_hasMajSixth)
				{
					denomLCM = LCM(denomLCM,MAJSIXTH_DENOMINATOR);
				}
				if (_hasMinSeventh)
				{
					denomLCM = LCM(denomLCM,MINSEVENTH_DENOMINATOR);
				}
				if (_hasMajSeventh)
				{
					denomLCM = LCM(denomLCM,MAJSEVENTH_DENOMINATOR);
				}
				if (_hasOctave)
				{
					denomLCM = LCM(denomLCM,OCTAVE_DENOMINATOR);
				}

				uint lcm = 1;
				//Compute LCM of 
				if (_hasPrime)
				{
					lcm = LCM(lcm,PRIME * denomLCM);
				}
				if (_hasMinSecond)
				{
					lcm = LCM(lcm,MINSECOND * denomLCM);
				}
				if (_hasMajSecond)
				{
					lcm = LCM(lcm,MAJSECOND * denomLCM);
				}
				if (_hasMinThird)
				{
					lcm = LCM(lcm,MINTHIRD * denomLCM);
				}
				if (_hasMajThird)
				{
					lcm = LCM(lcm,MAJTHIRD * denomLCM);
				}
				if (_hasFourth)
				{
					lcm = LCM(lcm,FOURTH * denomLCM);
				}
				if (_hasTritone)
				{
					lcm = LCM(lcm,TRITONE * denomLCM);
				}
				if (_hasFifth)
				{
					lcm = LCM(lcm,FIFTH * denomLCM);
				}
				if (_hasMinSixth)
				{
					lcm = LCM(lcm,MINSIXTH * denomLCM);
				}
				if (_hasMajSixth)
				{
					lcm = LCM(lcm,MAJSIXTH * denomLCM);
				}
				if (_hasMinSeventh)
				{
					lcm = LCM(lcm,MINSEVENTH * denomLCM);
				}
				if (_hasMajSeventh)
				{
					lcm = LCM(lcm,MAJSEVENTH * denomLCM);
				}
				if (_hasOctave)
				{
					lcm = LCM(lcm,OCTAVE * denomLCM);
				}

				return float4(print5x7int(denomLCM, i.uv, 3, 0), 1);
			}
			ENDCG
		}
	}
}