#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Rendering;

// thank you Scruffy and z3y

namespace _3.Editor
{

	public class OnAvatarBuild : IVRCSDKPreprocessAvatarCallback
	{
		public int callbackOrder => 3;

		public bool OnPreprocessAvatars(Gameobject avatarGameobject)
		{
			Debug.Log("OnPreProcessAvatars" + avatarGameobject);
			return true;
		}
	}

	public class OnSceneBuild : IVRCSDKBuildRequestedCallback
	{
		public int callbackOrder => 6;

		public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
		{
			Debug.Log("OnBuildRequested" + requestedBuildType)
			if (requestedBuildType == VRCSDKRequestedBuildType.Avatar)
			{
#define AVATAR
				return true;
			}
			else
			{
#undef AVATAR
				return true;
			}
		}
	}



	public class PreprocessShaders : IPreprocessShaders
	{

		public PassType[] pts = {PassType.Deferred, PassType.LightPrePassBase, PassType.LightPrePassFinal, PassType.VertexLM, PassType.MotionVectors, PassType.ScriptableRenderPipeline, PassType.ScriptableRenderPipelineDefaultUnlit};

		public int callbackOrder => 9;

		public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
		{
			Debug.Log("OnProcessShader")
#if AVATAR
		Debug.Log("AVATAR");
		public PassType[] ptsfinal = {pts + PassType.Normal};
#else
		Debug.Log("ELSE");
		public PassType[] ptsfinal = pts;
#endif
#undef AVATAR

			if (ptsfinal.Contains(snippet.passType) )
			{
				//Debug.Log($"Consumed {shader.name} = {snippet.passType} = {snippet.passName}");
				data.Clear();
				return;
			}

			string shaderName = shader.name;
			shaderName = string.IsNullOrEmpty(shaderName) ? "Empty" : shaderName;
			if (shaderName.Contains("Hidden/PostProcessing"))
			{
				//Debug.Log($"Consumed {shader.name}");
				data.Clear();
			}
		}

	}
}

#endif