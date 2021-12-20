/*
Copyright 2021 lox9973

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

Shader "Polyrhythm/Audio"
{
	Properties
	{
		[ToggleUI] _hasRoot ( "_hasRoot", float) = 1.0
		[ToggleUI] _hasMinSecond ( "_hasMinSecond", float) = 0
		[ToggleUI] _hasMajSecond ( "_hasMajSecond", float) = 0
		[ToggleUI] _hasMinThird ( "_hasMinThird", float) = 0
		[ToggleUI] _hasMajThird ( "_hasMajThird", float) = 0
		[ToggleUI] _hasFourth ( "_hasFourth", float) = 0
		[ToggleUI] _hasTritone ( "_hasTritone", float) = 0
		[ToggleUI] _hasFifth ( "_hasFifth", float) = 0
		[ToggleUI] _hasMinSixth ( "_hasMinSixth", float) = 0
		[ToggleUI] _hasMajSixth ( "_hasMajSixth", float) = 0
		[ToggleUI] _hasMinSeventh( "_hasMinSeventh", float) = 0
		[ToggleUI] _hasMajSeventh( "_hasMajSeventh", float) = 0
		[ToggleUI] _hasOctave( "_hasOctave", float) = 0
		[ToggleUI] _Polyrhythm( "PolyRhythm", float) = 0
		_Volume ( "Volume", float) = 0
		_Root ( "Root", float) = 10
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
			#include <UnityCG.cginc>

			uint _SampleRate;
			uint _Offset;
			static float2 _BufferSize = _ScreenParams.xy; // use render target size

			struct FragInput
			{
				float2 tex : TEXCOORD0;
				float4 pos : SV_Position;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			void vert(appdata_base i, out FragInput o)
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.tex = i.texcoord;
				o.pos = float4(i.texcoord.xy * 2 - 1, UNITY_NEAR_CLIP_VALUE, 1);
				o.pos.y *= _ProjectionParams.x;
				if (unity_OrthoParams.w != 1)
					o.pos = 0;
			}


			#define mod(x,y) ((x)%(y))

			bool _hasRoot;
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

			// Created by inigo quilez - iq/2014
			// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
			// main instrument
			float instrument(float freq, float time)
			{
				// https://www.shadertoy.com/view/3l23Rc

				float tm = mod(time, 1/freq);
				float phase = mod(freq * time, 1);

				// TODO: add a toggle between polyrhythm mode and sine mode
				if (!_Polyrhythm)
				{
					float k = sin(phase * UNITY_TWO_PI);
					return k * _Volume;
				}

				float finetune = 1.8;

				// KICK

				// sine that drops to 0 freq
				float k = sin(80. * exp(-tm * finetune * 10.));
				//return vec2(k) * .5;

				// ramp up start, fixes glitch
				k *= min(1., tm * 500.) * max(0., 1. - tm);
				//return vec2(k) * .5;

				// fade out the end
				k *= exp(-tm * 10.);
				//return vec2(k) * .5;

				// add a little more bass complexity 
				k *= cos(120.0 * exp(-tm * 2.));
				//return vec2(k) * .5;

				// SUB

				// low freq
				float s = sin(tm * 380.);
				//return vec2(s) * .5;

				// fade out
				s *= exp(-tm * 1.5);
				//return vec2(s) * .5;

				// ramp up start (fixes glitch) and fade to 0
				s *= min(1., tm * 100.) * max(0., .5 - tm);
				//return vec2(s) * .5;

				// add kick
				s += k;
				//return vec2(s) * .5;

				// incrase volume at the end
				s *= lerp(1., 7., tm * 2.);
				//return vec2(s) * .5;

				// add more kick
				return (s + k) * _Volume;
			}


			//----------------------------------------------------------------------------------------
			// sound shader entrypoint
			//
			// input: time in seconds
			// ouput: stereo wave valuie at "time"
			//----------------------------------------------------------------------------------------

			float2 wave(float time)
			{
				float2 sound;
				sound = _hasRoot ? instrument(time, _Root) : float2(0.0, 0.0);
				sound += _hasMinSecond ? instrument(time, MINSECOND) : float2(0.0, 0.0);
				sound += _hasMajSecond ? instrument(time, MAJSECOND) : float2(0.0, 0.0);
				sound += _hasMinThird ? instrument(time, MINTHIRD) : float2(0.0, 0.0);
				sound += _hasMajThird ? instrument(time, MAJTHIRD) : float2(0.0, 0.0);
				sound += _hasFourth ? instrument(time, FOURTH) : float2(0.0, 0.0);
				sound += _hasTritone ? instrument(time, TRITONE) : float2(0.0, 0.0);
				sound += _hasFifth ? instrument(time, FIFTH) : float2(0.0, 0.0);
				sound += _hasMinSixth ? instrument(time, MINSIXTH) : float2(0.0, 0.0);
				sound += _hasMajSixth ? instrument(time, MAJSIXTH) : float2(0.0, 0.0);
				sound += _hasMinSeventh ? instrument(time, MINSEVENTH) : float2(0.0, 0.0);
				sound += _hasMajSeventh ? instrument(time, MAJSEVENTH) : float2(0.0, 0.0);
				sound += _hasOctave ? instrument(time, OCTAVE) : float2(0.0, 0.0);
				return sound;
			}

			float2 mainSound(in int samp, float time)
			{
				return wave(time);
			}


			float2 frag(FragInput i) : SV_Target
			{
				uint bufferLen = _BufferSize.x * _BufferSize.y;
				uint index = dot(floor(i.tex.xy * _BufferSize.xy), float2(1, _BufferSize.x));
				index += (_Offset / bufferLen + (_Offset % bufferLen > index)) * bufferLen;
				return mainSound(index, float(index / _SampleRate) + float(index % _SampleRate) / _SampleRate);
			}
			ENDCG
		}
	}
}