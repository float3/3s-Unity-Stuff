#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

// thank you Scruffy and z3y

namespace _3.Editor
{
	internal class ShaderPreProcessor : IPreprocessShaders
	{
		private readonly PassType[] pts =
		{
			PassType.Deferred, PassType.LightPrePassBase, PassType.LightPrePassFinal, PassType.VertexLM, PassType.Meta
		};

		public int callbackOrder { get { return 0; } }

		public void OnProcessShader(Shader p_shader, ShaderSnippetData p_snippet, IList<ShaderCompilerData> p_data)
		{
			if (pts.Contains(p_snippet.passType))
			{
				//Debug.Log($"Consumed {p_shader.name} = {p_snippet.passType} = {p_snippet.passName}");
				p_data.Clear();
				return;
			}

			var shaderName = p_shader.name;
			shaderName = string.IsNullOrEmpty(shaderName) ? "Empty" : shaderName;
			if (shaderName.Contains("Hidden/PostProcessing"))
				//Debug.Log($"Consumed {p_shader.name}");
				p_data.Clear();
		}
	}
}
#endif