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
	[RequireComponent(typeof(Camera), typeof(MeshRenderer))]
	public class AudioRenderer :
			#if UDON
			UdonSharpBehaviour
		#else
	MonoBehaviour
		#endif
	{
		public AudioFilterSource audioFilterSource;
		public Texture2D texture;

		private Renderer renderer;
		private Rect rect;
		private Color[] buffer;
		private MaterialPropertyBlock matPropBlock;

		void OnEnable()
		{
			var width = texture.width;
			var height = texture.height;
			rect = new Rect(0, 0, width, height);
			buffer = new Color[width * height];
			audioFilterSource.buffer = buffer;
			audioFilterSource.offset = 0;

			renderer = GetComponent<MeshRenderer>();
			matPropBlock = new MaterialPropertyBlock();
			matPropBlock.SetInt("_SampleRate", audioFilterSource.sampleRate);
			matPropBlock.SetInt("_Offset", audioFilterSource.offset);
			renderer.SetPropertyBlock(matPropBlock);
		}

		void Start()
		{
			var rt = GetComponent<Camera>().targetTexture;
			rt.width = texture.width;
			rt.height = texture.height;
		}

		void OnDisable()
		{
			System.Array.Clear(buffer, 0, buffer.Length);
		}

		void OnPreRender()
		{
			matPropBlock.SetInt("_Offset", audioFilterSource.offset);
			renderer.SetPropertyBlock(matPropBlock);
		}

		void OnPostRender()
		{
			texture.ReadPixels(rect, 0, 0, false);
			System.Array.Copy(texture.GetPixels(), buffer, buffer.Length);
		}
	}
}