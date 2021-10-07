#if UNITY_EDITOR && (VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3)

#region

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.SDKBase.Editor.BuildPipeline;

#endregion

// thank you Scruffy and z3y

namespace _3.Editor
{
	public class OnBuildRequest : IVRCSDKBuildRequestedCallback
	{
		public static VRCSDKRequestedBuildType requestedBuildTypeCallback;
		public int callbackOrder => 6;

		public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
		{
			if (requestedBuildType == VRCSDKRequestedBuildType.Avatar)
			{
				requestedBuildTypeCallback = requestedBuildType;
			}

			else if (requestedBuildType == VRCSDKRequestedBuildType.Scene)
			{
				requestedBuildTypeCallback = requestedBuildType;
			}

			return true;
		}
	}

	public class PreprocessShaders : IPreprocessShaders
	{
		private readonly PassType[] pts =
		{
			PassType.Deferred, PassType.LightPrePassBase, PassType.LightPrePassFinal, PassType.VertexLM,
			PassType.MotionVectors, PassType.ScriptableRenderPipeline, PassType.ScriptableRenderPipelineDefaultUnlit
		};

		public int callbackOrder => 9;

		public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
		{
			if (pts.Contains(snippet.passType) || (OnBuildRequest.requestedBuildTypeCallback == VRCSDKRequestedBuildType.Scene && !Lightmapping.realtimeGI && snippet.passType == PassType.Meta) || (OnBuildRequest.requestedBuildTypeCallback == VRCSDKRequestedBuildType.Avatar && snippet.passType == PassType.Meta))
			{
				data.Clear();
				return;
			}

			string shaderName = shader.name;
			shaderName = string.IsNullOrEmpty(shaderName) ? "Empty" : shaderName;
			if (shaderName.Contains("Hidden/PostProcessing"))
			{
				data.Clear();
			}
		}
	}
}
#endif