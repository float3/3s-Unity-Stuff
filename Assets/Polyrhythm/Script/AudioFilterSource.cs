/*
Copyright 2021 lox9973

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/


using UnityEngine;
#if UDON
using UdonSharp;
#endif

namespace ShaderAudio
{
	[RequireComponent(typeof(AudioSource))]
	public class AudioFilterSource :
			#if UDON
			UdonSharpBehaviour
		#else
			MonoBehaviour
		#endif
	{
		public int sampleRate = 48000;
		[System.NonSerialized] public Color[] buffer;
		[System.NonSerialized] public int offset = 0;

		void OnAudioFilterRead(float[] data, int channels)
		{
			// save to reduce data race
			var buf = buffer;
			var off = offset;
			if (buffer == null)
				return;
			// save for performance
			var nbuf = buf.Length;
			var ndata = data.Length;
			for (int i = 0; i < ndata; off++)
			{
				var color = buf[off % nbuf];
				data[i] = color.r;
				i++;
				data[i] = color.g;
				i++;
			}

			offset = off;
		}

		// don't implement Update() here! it'll crash the Udon stack
		private float[] onAudioFilterReadData;
		private int onAudioFilterReadChannels;

		public void _onAudioFilterRead()
		{
			// expose OnAudioFilterRead to UdonSharp
			OnAudioFilterRead(onAudioFilterReadData, onAudioFilterReadChannels);
		}
	}
}