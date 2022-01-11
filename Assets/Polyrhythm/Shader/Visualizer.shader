Shader "Polyrhythm/Visualizer"
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
		_Volume ( "Volume", float) = 0
		_Root ( "Root", float) = 440
	}
	SubShader
	{
		Tags
		{
			"PreviewType"="Plane"
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

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

			#define glsl_mod(x,y) (((x)-(y)*floor((x)/(y))))

			static v2f vertex_output;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			#define X_AXIS_SCALE 0.005
			#define Y_AXIS_SCALE 10.
			#define SIN_LIMIT 0.1
			#define LIMIT_2 0.005
			#define X_AXIS_LIMIT 0.1
			#define Y_AXIS_LIMIT 0.00005
			#define DEBUG false
			#define DEBUG_LIMIT 0.1

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
			bool _Polyrhythm;
			float _Volume;
			float _Root;

			#define MINSECOND 16.0/15.0 * _Root
			#define MAJSECOND 9.0/8.0 * _Root
			#define MINTHIRD 6.0/5.0 * _Root
			#define MAJTHIRD 5.0/4.0 * _Root
			#define FOURTH 4.0/3.0 * _Root
			#define TRITONE 45.0/32.0 * _Root
			#define FIFTH 3.0/2.0 * _Root
			#define MINSIXTH 8.0/5.0 * _Root
			#define MAJSIXTH 5.0/3.0 * _Root
			#define MINSEVENTH 7.0/4.0 * _Root
			#define MAJSEVENTH 15.0/8.0 * _Root
			#define OCTAVE 2.0 * _Root
			#define PI 3.1415927
			#define TWO_PI 6.2831855
			#define FOUR_PI 12.566371
			#define INV_PI 0.31830987
			#define INV_TWO_PI 0.15915494
			#define INV_FOUR_PI 0.07957747
			#define HALF_PI 1.5707964
			#define INV_HALF_PI 0.63661975

			float getAmplitude(float a)
			{
				float root = sin(_Root * a * TWO_PI);
				float minSecond = sin(MINSECOND * a * TWO_PI);
				float majSecond = sin(MAJSECOND * a * TWO_PI);
				float minThird = sin(MAJTHIRD * a * TWO_PI);
				float majThird = sin(MAJTHIRD * a * TWO_PI);
				float fourth = sin(FOURTH * a * TWO_PI);
				float tritone = sin(TRITONE * a * TWO_PI);
				float fifth = sin(FIFTH * a * TWO_PI);
				float minSixth = sin(MINSIXTH * a * TWO_PI);
				float majSixth = sin(MAJSIXTH * a * TWO_PI);
				float minSeventh = sin(MINSEVENTH * a * TWO_PI);
				float majSeventh = sin(MAJSEVENTH * a * TWO_PI);
				float octave = sin(OCTAVE * a * TWO_PI);
				float amplitude = 0;
				amplitude += _hasPrime ? root : 0.;
				amplitude += _hasMinSecond ? minSecond : 0.;
				amplitude += _hasMajSecond ? majSecond : 0.;
				amplitude += _hasMinThird ? minThird : 0.;
				amplitude += _hasMajThird ? majThird : 0.;
				amplitude += _hasFourth ? fourth : 0.;
				amplitude += _hasTritone ? tritone : 0.;
				amplitude += _hasFifth ? fifth : 0.;
				amplitude += _hasMinSixth ? minSixth : 0.;
				amplitude += _hasMajSixth ? majSixth : 0.;
				amplitude += _hasMinSeventh ? minSeventh : 0.;
				amplitude += _hasMajSeventh ? majSeventh : 0.;
				amplitude += _hasOctave ? octave : 0.;
				return amplitude;
			}

			bool isCycleEnd(float a)
			{
				float root = abs(sin(_Root * a * TWO_PI));
				float minSecond = abs(sin(MINSECOND * a * TWO_PI));
				float majSecond = abs(sin(MAJSECOND * a * TWO_PI));
				float minThird = abs(sin(MAJTHIRD * a * TWO_PI));
				float majThird = abs(sin(MAJTHIRD * a * TWO_PI));
				float fourth = abs(sin(FOURTH * a * TWO_PI));
				float tritone = abs(sin(TRITONE * a * TWO_PI));
				float fifth = abs(sin(FIFTH * a * TWO_PI));
				float minSixth = abs(sin(MINSIXTH * a * TWO_PI));
				float majSixth = abs(sin(MAJSIXTH * a * TWO_PI));
				float minSeventh = abs(sin(MINSEVENTH * a * TWO_PI));
				float majSeventh = abs(sin(MAJSEVENTH * a * TWO_PI));
				float octave = abs(sin(OCTAVE * a * TWO_PI));
				if (_hasPrime && root > DEBUG_LIMIT)
					return false;
				if (_hasMinSecond && minSecond > DEBUG_LIMIT)
					return false;
				if (_hasMajSecond && majSecond > DEBUG_LIMIT)
					return false;
				if (_hasMinThird && minThird > DEBUG_LIMIT)
					return false;
				if (_hasMajThird && majThird > DEBUG_LIMIT)
					return false;
				if (_hasFourth && fourth > DEBUG_LIMIT)
					return false;
				if (_hasTritone && tritone > DEBUG_LIMIT)
					return false;
				if (_hasFifth && fifth > DEBUG_LIMIT)
					return false;
				if (_hasMinSixth && minSixth > DEBUG_LIMIT)
					return false;
				if (_hasMajSixth && majSixth > DEBUG_LIMIT)
					return false;
				if (_hasMinSeventh && minSeventh > DEBUG_LIMIT)
					return false;
				if (_hasMajSeventh && majSeventh > DEBUG_LIMIT)
					return false;
				if (_hasOctave && octave > DEBUG_LIMIT)
					return false;

				return true;
			}

			float getCycle(float a)
			{
				float root = sin(_Root * a * TWO_PI);
				float minSecond = sin(MINSECOND * a * TWO_PI);
				float majSecond = sin(MAJSECOND * a * TWO_PI);
				float minThird = sin(MAJTHIRD * a * TWO_PI);
				float majThird = sin(MAJTHIRD * a * TWO_PI);
				float fourth = sin(FOURTH * a * TWO_PI);
				float tritone = sin(TRITONE * a * TWO_PI);
				float fifth = sin(FIFTH * a * TWO_PI);
				float minSixth = sin(MINSIXTH * a * TWO_PI);
				float majSixth = sin(MAJSIXTH * a * TWO_PI);
				float minSeventh = sin(MINSEVENTH * a * TWO_PI);
				float majSeventh = sin(MAJSEVENTH * a * TWO_PI);
				float octave = sin(OCTAVE * a * TWO_PI);
				float cycle = 1.;
				cycle *= _hasPrime ? root : 1.;
				cycle *= _hasMinSecond ? minSecond : 1.;
				cycle *= _hasMajSecond ? majSecond : 1.;
				cycle *= _hasMinThird ? minThird : 1.;
				cycle *= _hasMajThird ? majThird : 1.;
				cycle *= _hasFourth ? fourth : 1.;
				cycle *= _hasTritone ? tritone : 1.;
				cycle *= _hasFifth ? fifth : 1.;
				cycle *= _hasMinSixth ? minSixth : 1.;
				cycle *= _hasMajSixth ? majSixth : 1.;
				cycle *= _hasMinSeventh ? minSeventh : 1.;
				cycle *= _hasMajSeventh ? majSeventh : 1.;
				cycle *= _hasOctave ? octave : 1.;
				return cycle;
			}

			float2 kick(float time, float bps)
			{
				float tm = glsl_mod(time, 1./bps);
				float finetune = 1.8;
				float k = sin(80. * exp(-tm * finetune * 10.));
				k *= min(1., tm * 500.) * max(0., 1. - tm);
				k *= exp(-tm * 10.);
				k *= cos(120. * exp(-tm * 2.));
				float s = sin(tm * 380.);
				s *= exp(-tm * 1.5);
				s *= min(1., tm * 100.) * max(0., 0.5 - tm);
				s += k;
				s *= lerp(1., 7., tm * 2.);
				return ((float2)s + k) * 0.5;
			}

			float2 wave(float time)
			{
				float2 sound;
				sound = _hasPrime ? kick(time, _Root) : float2(0.0, 0.0);
				sound += _hasMinSecond ? kick(time, MINSECOND) : float2(0.0, 0.0);
				sound += _hasMajSecond ? kick(time, MAJSECOND) : float2(0.0, 0.0);
				sound += _hasMinThird ? kick(time, MINTHIRD) : float2(0.0, 0.0);
				sound += _hasMajThird ? kick(time, MAJTHIRD) : float2(0.0, 0.0);
				sound += _hasFourth ? kick(time, FOURTH) : float2(0.0, 0.0);
				sound += _hasTritone ? kick(time, TRITONE) : float2(0.0, 0.0);
				sound += _hasFifth ? kick(time, FIFTH) : float2(0.0, 0.0);
				sound += _hasMinSixth ? kick(time, MINSIXTH) : float2(0.0, 0.0);
				sound += _hasMajSixth ? kick(time, MAJSIXTH) : float2(0.0, 0.0);
				sound += _hasMinSeventh ? kick(time, MINSEVENTH) : float2(0.0, 0.0);
				sound += _hasMajSeventh ? kick(time, MAJSEVENTH) : float2(0.0, 0.0);
				sound += _hasOctave ? kick(time, OCTAVE) : float2(0.0, 0.0);
				return sound;
			}

			#define GREEN float4(0, 1, 0, 1)
			#define BLUE float4(0, 0, 1, 1)
			#define RED float4(1, 0, 0, 1)
			#define WHITE float4(1, 1, 1, 1)
			#define BLACK float4(0, 0, 0, 1)

			float3 frag(v2f __vertex_output) : SV_Target
			{
				vertex_output = __vertex_output;
				float2 fragCoord = vertex_output.uv * _ScreenParams.xy;
				float2 uv = (fragCoord / _ScreenParams.xy - 0.5) * float2(X_AXIS_SCALE, Y_AXIS_SCALE);
				float3 col = 0;

				if (abs(uv.x) <= Y_AXIS_LIMIT)
					col.b = 1;

				if (abs(uv.y) <= X_AXIS_LIMIT)
					col.g = 1;

				uv.x += _Time.y * 1 / _Root;
				float amplitudeSum = getAmplitude(uv.x) * _Volume;
				if (abs(amplitudeSum - uv.y) <= SIN_LIMIT)
				{
					col.xyz = float3(1,0,0); 
				}

				return col;
			}
			ENDCG
		}
	}
}