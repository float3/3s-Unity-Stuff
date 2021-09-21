#if UNITY_EDITOR

#region

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

#endregion

// thank you Scruffy and z3y

namespace _3.Editor
{
	internal class AutoLockOnBuild : IPreprocessShaders
	{
		public PassType[] pts =
		{
			PassType.Deferred, PassType.LightPrePassBase, PassType.LightPrePassFinal, PassType.VertexLM, PassType.Meta,
			PassType.MotionVectors, PassType.ScriptableRenderPipeline, PassType.ScriptableRenderPipelineDefaultUnlit
		};

		public int callbackOrder => 3;

		public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
		{
			if (pts.Contains(snippet.passType))
			{
				//Debug.Log($"Consumed {shader.name} = {snippet.passType} = {snippet.passName}");
				data.Clear();
				return;
			}

			var shaderName = shader.name;
			shaderName = string.IsNullOrEmpty(shaderName) ? "Empty" : shaderName;
			if (shaderName.Contains("Hidden/PostProcessing"))
				//Debug.Log($"Consumed {shader.name}");
				data.Clear();
		}
	}
}

#endif