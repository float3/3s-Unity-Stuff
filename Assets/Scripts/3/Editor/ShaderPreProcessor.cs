#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Rendering;

// thank you Scruffy and z3y

namespace _3.Editor
{
	public class PreprocessShaders : IPreprocessShaders
	{
	#if (VRC_SDK_VRCSDK3 && UDON) || VRC_SDK_SDK2
			public PassType[] pts = {PassType.Deferred, PassType.LightPrePassBase, PassType.LightPrePassFinal, PassType.VertexLM, PassType.MotionVectors, PassType.ScriptableRenderPipeline, PassType.ScriptableRenderPipelineDefaultUnlit};
	#elif VRC_SDK_VRCSDK3 && !UDON
			public PassType[] pts = {PassType.Deferred, PassType.LightPrePassBase, PassType.LightPrePassFinal, PassType.VertexLM, PassType.Meta, PassType.MotionVectors, PassType.ScriptableRenderPipeline, PassType.ScriptableRenderPipelineDefaultUnlit};
	#endif

		
		public int callbackOrder => 3;

		public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
		{

			Debug.Log("OnProcessShader"); //remember to test which callback happens first

			if (pts.Contains(snippet.passType))
			{
				//Debug.Log($"Consumed {shader.name} = {snippet.passType} = {snippet.passName}");
				data.Clear();
				return;
			}

			string shaderName = shader.name;
			shaderName = string.IsNullOrEmpty(shaderName) ? "Empty" : shaderName;
			if (shaderName.Contains("Hidden/PostProcessing"))
				//Debug.Log($"Consumed {shader.name}");
				data.Clear();
		}
	}

	public  class class VRCCallbackTest : IVRCSDKBuildRequestedCallback
	{
		public int callbackOrder => 3;

		bool IVRCSDKBuildRequestedCallback.OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
		{
			Debug.Log($"OnBuildRequest {requestedBuildType}")
			return true;
		}
	}
}

#endif