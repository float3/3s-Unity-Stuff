#if UNITY_EDITOR

#region 

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;

#if VRC_SDK_VRCSDK3
#if !UDON
using VRC.SDK3.Avatars.Components;
#endif
using VRC.SDKBase.Editor.BuildPipeline;
#endif
#if !UDON && !VRC_SDK_VRCSDK3
using UnityEditor.Build.Reporting;
#endif

#endregion

namespace _3
{
	#region Callbacks

	#if VRC_SDK_VRCSDK3
	public class OnBuildRequest : IVRCSDKBuildRequestedCallback
	{
		public int callbackOrder => 6;


		public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
		{
			if (requestedBuildType == VRCSDKRequestedBuildType.Scene)
			{
				ShaderPreprocessor.ExcludedShaders.Clear();

				Scene scene = SceneManager.GetActiveScene();
				string[] shaderpaths = AssetDatabase.GetDependencies(scene.path, true).Where(x => x.EndsWith(".shader"))
					.ToArray();

				foreach (string shaderpath in shaderpaths)
				{
					if (shaderpath.Contains("Reflect-BumpVertexLit.shader"))
					{
						string[] excludedShaderPass = new string[3];

						excludedShaderPass[0] = "Reflective/Bumped Unlit";
						excludedShaderPass[1] = "BASE";
						excludedShaderPass[2] = "Legacy Shaders/Reflective/Bumped VertexLit";
					}

					if (shaderpath.Contains("unity_builtin_extra"))
						continue;

					string[] shader = File.ReadAllLines(shaderpath);

					foreach (string line in new CommentFreeIterator(shader))
					{
						if (line.Contains("UsePass"))
						{
							string shadernameRegex = "(?<=\\\")(.*)(?=\\/)";
							string passnameRegex = "(?<=\\/)([^\\/]*?)(?=\")";

							string[] excludedShaderPass = new string[3];

							excludedShaderPass[0] = Regex.Match(line, shadernameRegex).Value;
							excludedShaderPass[1] = Regex.Match(line, passnameRegex).Value;
							excludedShaderPass[2] = AssetDatabase.LoadAssetAtPath<Shader>(shaderpath).name;

							ShaderPreprocessor.ExcludedShaders.Add(excludedShaderPass);
						}
					}
				}
			}

			return true;
		}
	}
	#if !UDON
	public class OnAvatarBuild : IVRCSDKPreprocessAvatarCallback
	{
		public int callbackOrder => 3;

		public bool OnPreprocessAvatar(GameObject avatarGameObject)
		{
			ShaderPreprocessor.ExcludedShaders.Clear();

			List<Material> materials = avatarGameObject.GetComponentsInChildren<Renderer>(true)
				.SelectMany(r => r.sharedMaterials).ToList();

			VRCAvatarDescriptor descriptor = avatarGameObject.GetComponent<VRCAvatarDescriptor>();
			if (descriptor != null)
			{
				IEnumerable<AnimationClip> clips = descriptor.baseAnimationLayers.Select(l => l.animatorController)
					.Where(a => a != null).SelectMany(a => a.animationClips).Distinct();
				foreach (AnimationClip clip in clips)
				{
					IEnumerable<Material> clipMaterials = AnimationUtility.GetObjectReferenceCurveBindings(clip)
						.Where(b => b.isPPtrCurve && b.type.IsSubclassOf(typeof(Renderer)) &&
						            b.propertyName.StartsWith("m_Materials"))
						.SelectMany(b => AnimationUtility.GetObjectReferenceCurve(clip, b))
						.Select(r => r.value as Material);
					materials.AddRange(clipMaterials);
				}
			}

			foreach (Material material in materials)
			{
				if (material == null)
					continue;
				if (material.shader == null)
					continue;

				// hardcode this shader because
				if (material.shader.name == "Legacy Shaders/Reflective/Bumped VertexLit")
				{
					string[] excludedShaderPass = new string[3];

					excludedShaderPass[0] = "Reflective/Bumped Unlit";
					excludedShaderPass[1] = "BASE";
					excludedShaderPass[2] = "Legacy Shaders/Reflective/Bumped VertexLit";
				}

				if (AssetDatabase.GetAssetPath(material.shader).Contains("unity_builtin_extra")) continue;

				string[] shader = File.ReadAllLines(AssetDatabase.GetAssetPath(material.shader));

				foreach (string line in new CommentFreeIterator(shader))
				{
					if (line.Contains("UsePass"))
					{
						string shadernameRegex = "(?<=\")(.*)(?=\\/)";
						string passnameRegex = "(?<=\\/)([^\\/]*?)(?=\")";

						string[] excludedShaderPass = new string[3];

						excludedShaderPass[0] = Regex.Match(line, shadernameRegex).Value;
						excludedShaderPass[1] = Regex.Match(line, passnameRegex).Value;
						excludedShaderPass[2] = material.shader.name;

						ShaderPreprocessor.ExcludedShaders.Add(excludedShaderPass);
					}
				}
			}

			return true;
		}
	}
	#endif
	#endif
	#if !UDON && !VRC_SDK_VRCSDK3
	class OnBuild : IPreprocessBuildWithReport
	{
		public int callbackOrder => 3;

		public void OnPreprocessBuild(BuildReport report)
		{
			Scene scene = SceneManager.GetActiveScene();

			string[] shaderpaths =
				AssetDatabase.GetDependencies(scene.path, true).Where(x => x.EndsWith(".shader")).ToArray();

			ShaderPreprocessor.ExcludedShaders.Clear();

			foreach (string shaderpath in shaderpaths)
			{
				if (shaderpath.Contains("Reflect-BumpVertexLit.shader"))
				{
					string[] excludedShaderPass = new string[3];

					excludedShaderPass[0] = "Reflective/Bumped Unlit";
					excludedShaderPass[1] = "BASE";
					excludedShaderPass[2] = "Legacy Shaders/Reflective/Bumped VertexLit";
				}

				if (shaderpath.Contains("unity_builtin_extra")) continue;

				string[] shader = File.ReadAllLines(shaderpath);

				foreach (string line in new CommentFreeIterator(shader))
				{
					if (line.Contains("UsePass"))
					{
						string shadernameRegex = "(?<=\\\")(.*)(?=\\/)";
						string passnameRegex = "(?<=\\/)([^\\/]*?)(?=\")";

						string[] excludedShaderPass = new string[3];

						excludedShaderPass[0] = Regex.Match(line, shadernameRegex).Value;
						excludedShaderPass[1] = Regex.Match(line, passnameRegex).Value;
						excludedShaderPass[2] = AssetDatabase.LoadAssetAtPath<Shader>(shaderpath).name;

						ShaderPreprocessor.ExcludedShaders.Add(excludedShaderPass);
					}
				}
			}
		}
	}
	#endif

	#endregion Callbacks
	public class ShaderPreprocessor : IPreprocessShaders
	{
		public int callbackOrder => 3;

		public static List<string[]> ExcludedShaders = new List<string[]>();

		SettingsData s = DataManager.Load();

		//this is a precaution because those shaders use a Vertex Pass as a Workaround
		//so I want to make sure to never strip them
		private List<string> shaderBlackList = new List<string>(new[]
		{
			"Motion/GrabMotionDec",
			"AudioLink/Internal/AudioTextureExport"
		});

		public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
		{
			#if VRC_SDK_VRCSDK3
			if (s.UnusedHardwareTiers)
			{
				for (int i = data.Count - 1; i >= 0; --i)
				{
					if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
					{
						if (data[i].graphicsTier == GraphicsTier.Tier1 ||
						    data[i].graphicsTier == GraphicsTier.Tier3)
							data.RemoveAt(i);
					}
					else //if(EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
					{
						if  (data[i].graphicsTier == GraphicsTier.Tier1 ||
							data[i].graphicsTier == GraphicsTier.Tier2)
							data.RemoveAt(i);
					}
				}
			}
			#endif
			
			
			string shaderName = string.IsNullOrEmpty(shader.name) ? "Empty" : shader.name;
			
			if (shaderBlackList.Contains(shaderName))
				return;

			//If Unity does not find a matching Pass for the UsePass ShaderLab command, it shows the error material. https://docs.unity3d.com/Manual/SL-UsePass.html
			foreach (string[] excludedShader in ExcludedShaders)
			{
				if (excludedShader[0] == shaderName && excludedShader[1] == snippet.passName)
				{
					Debug.Log(
						$"Pass: {snippet.passName} of type {snippet.passType} in Shader: {shaderName} wasn't included because {excludedShader[2]} uses it in a UsePass");
					return;
				}
			}

			bool shouldStrip = ShouldStrip(shader, snippet);

			if (shouldStrip)
			{
				data.Clear();
				return;
			}

			#if VRC_SDK_VRCSDK3 && !UDON
			if (s.UnusedVariants)
			{
				for (int i = data.Count - 1; i >= 0; --i)
				{
					if (data[i].shaderKeywordSet.IsEnabled(new ShaderKeyword("DYNAMICLIGHTMAP_ON")) ||
					    data[i].shaderKeywordSet.IsEnabled(new ShaderKeyword("LIGHTMAP_ON")) ||
					    data[i].shaderKeywordSet.IsEnabled(new ShaderKeyword("LIGHTMAP_SHADOW_MIXING")) ||
					    data[i].shaderKeywordSet.IsEnabled(new ShaderKeyword("DIRLIGHTMAP_COMBINED")) ||
					    data[i].shaderKeywordSet.IsEnabled(new ShaderKeyword("SHADOWS_SHADOWMASK")))
						data.RemoveAt(i);
				}
			}
			#endif
		}

		private bool ShouldStrip(Shader shader, ShaderSnippetData snippet)
		{
			if (s.PostProcessing && shader.name.StartsWith("Hidden/PostProcessing/")) return true;
			if (s.Meta && snippet.passType == PassType.Meta) return true;
			if (s.Forward && (snippet.passType == PassType.ForwardBase || snippet.passType == PassType.ForwardAdd)) return true;
			if (s.DeferredLighting && (snippet.passType == PassType.LightPrePassBase || snippet.passType == PassType.LightPrePassFinal)) return true;
			if (s.DeferredShading && snippet.passType == PassType.Deferred) return true;
			if (s.Vertex && (snippet.passType == PassType.Vertex || snippet.passType == PassType.VertexLM)) return true;
			if (s.SRP && (snippet.passType == PassType.ScriptableRenderPipeline || snippet.passType == PassType.ScriptableRenderPipelineDefaultUnlit)) return true;
			if (s.MotionVectors && snippet.passType == PassType.MotionVectors) return true;
			return false;
		}
	}

	public class ShaderPreprocessorEditor : EditorWindow
	{
		[MenuItem("Tools/3/ShaderPreprocessor")]
		public static void ShowWindow()
		{
			GetWindow<ShaderPreprocessorEditor>("ShaderPreprocessor");
		}

		public SettingsData settingsData;
		bool firstTime = true;
		bool moreInfo = false;


		const string SRPInfo = ":\nShader Passes used in Scriptable Render Pipelines";

		const string MetaInfo = ":\nShader Passes used for Baking Lights and Realtime GI";

		#if VRC_SDK_VRCSDK3
		const string PostProcessingInfo =
			":\nVRChat replaces the Post Processing Shaders with it's own bundled ones \n" +
			"so you can safely excliude them from your build";

		const string VertexLitInfo = ":\nAudioLink/ShaderMotion use Vertex Passes \n" +
		                             "this but those cases are handled specifically \n" +
		                             "so you can turn this on even if you are using those";

		private const string unusedAvatarVariantsInfo =
			":\nThis will strip shader variants that are only used for different types of Lightmaps \n";
		
		
		//https://docs.unity3d.com/Manual/graphics-tiers.html
		//tier 1 = Android OpenGL ES 2
		//tier 2 = Android OpenGL ES 3
		//tier 3 = Desktop DX11
		private const string unusedHardwareTiersInfo =
			":\nUnity generates all Shader Variants for all 3 Hardware Tiers but VRC only uses Hardware Tier 2 for Quest 1/2\n" +
			"Hardware Tier 3 for Desktop, This will reduce shaders bloating";
		#else
		const string PostProcessingInfo = ":\nPost Processing Shaders";
		const string VertexLitInfo = ":\nVertexLit shader passes instead of PixeLit, usually used on mobile platforms"
		#endif


		private void OnGUI()
		{
			if (firstTime)
			{
				settingsData = DataManager.Load();
				firstTime = false;
			}
			
			Debug.Log(EditorUserBuildSettings.activeBuildTarget);

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("What Shaders should be excluded?", EditorStyles.boldLabel);
			EditorGUILayout.Space();
			if (GUILayout.Button("Auto Detect Settings"))
			{
				settingsData.Forward = false;
				settingsData.DeferredLighting = false;
				settingsData.DeferredShading = false;
				settingsData.Meta = false;


				BuildTargetGroup buildTargetGroup =
					BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
				TierSettings tierSettings =
					EditorGraphicsSettings.GetTierSettings(buildTargetGroup, GraphicsTier.Tier3);
				RenderingPath renderingPath = tierSettings.renderingPath;

				settingsData.Forward = renderingPath != RenderingPath.Forward;
				settingsData.DeferredLighting = renderingPath != RenderingPath.DeferredLighting;
				settingsData.DeferredShading = renderingPath != RenderingPath.DeferredShading;
				settingsData.Vertex = renderingPath != RenderingPath.VertexLit;

				if (GraphicsSettings.renderPipelineAsset != null)
				{
					string renderPipelineName = GraphicsSettings.renderPipelineAsset.ToString();
					if (renderPipelineName.Contains("Custom")) settingsData.SRP = false;
					else if (string.IsNullOrEmpty(renderPipelineName) ||
					         renderPipelineName.Contains("HDRenderPipeline") ||
					         renderPipelineName.Contains("LightWeight") ||
					         renderPipelineName.Contains("Universal")) settingsData.SRP = true;
				}
				else settingsData.SRP = true;

				#if VRC_SDK_VRCSDK3 && !UDON // Avatars
				settingsData.Meta = true;
				settingsData.UnusedVariants = true;
				#else
				settingsData.Meta = !Lightmapping.realtimeGI;
				#endif
				Camera[] cameras = FindObjectsOfType<Camera>();
				if (cameras.Any(camera => camera.actualRenderingPath == RenderingPath.DeferredLighting))
					settingsData.DeferredLighting = false;
				if (cameras.Any(camera => camera.actualRenderingPath == RenderingPath.DeferredShading))
					settingsData.DeferredShading = false;
				if (cameras.Any(camera => camera.depthTextureMode == DepthTextureMode.MotionVectors))
					settingsData.MotionVectors = false;
				if (cameras.Any(camera =>
					    camera.actualRenderingPath == RenderingPath.VertexLit &&
					    !camera.gameObject.name.Contains("AudioLink"))) settingsData.Vertex = false;

				#if VRC_SDK_VRCSDK3
				settingsData.SRP = true;
				settingsData.PostProcessing = true;
				settingsData.UnusedHardwareTiers = true;
				#endif

				//if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
				//settingsData.Vertex = false;

				settingsData.Vertex = false;
			}

			EditorGUILayout.Space();
			#if VRC_SDK_VRCSDK3
			#if  !UDON
			settingsData.UnusedVariants = GUILayout.Toggle(settingsData.UnusedVariants,"Unused Lightmap Variants" + (moreInfo ? " " + unusedAvatarVariantsInfo : ""));
			if (moreInfo) EditorGUILayout.Space();
			#endif
			settingsData.UnusedHardwareTiers = GUILayout.Toggle(settingsData.UnusedHardwareTiers,"Unused Hardware Tiers" + (moreInfo ? " " + unusedHardwareTiersInfo : ""));
			if (moreInfo) EditorGUILayout.Space();
			#endif
			settingsData.PostProcessing = GUILayout.Toggle(settingsData.PostProcessing, "Post Processing Shaders" + (moreInfo ? PostProcessingInfo : ""));
			EditorGUILayout.Space();
			settingsData.Forward = GUILayout.Toggle(settingsData.Forward, "Forward");
			if (moreInfo) EditorGUILayout.Space();
			settingsData.DeferredShading = GUILayout.Toggle(settingsData.DeferredShading, "Deferred Shading");
			if (moreInfo) EditorGUILayout.Space();
			settingsData.DeferredLighting = GUILayout.Toggle(settingsData.DeferredLighting, "Legacy Deferred Lighting");
			if (moreInfo) EditorGUILayout.Space();
			settingsData.Vertex = GUILayout.Toggle(settingsData.Vertex, "VertexLit" + (moreInfo ? VertexLitInfo : ""));
			if (moreInfo) EditorGUILayout.Space();
			settingsData.SRP = GUILayout.Toggle(settingsData.SRP, "SRP Passes" + (moreInfo ? SRPInfo : ""));
			if (moreInfo) EditorGUILayout.Space();
			settingsData.Meta = GUILayout.Toggle(settingsData.Meta, "Meta Passes" + (moreInfo ? MetaInfo : ""));
			if (moreInfo) EditorGUILayout.Space();
			settingsData.MotionVectors = GUILayout.Toggle(settingsData.MotionVectors, "Motion Vector Passes");
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			moreInfo = GUILayout.Toggle(moreInfo, "more Info");
			GUILayout.FlexibleSpace();
			if (EditorGUI.EndChangeCheck())
			{
				DataManager.Save(settingsData);
			}
		}
	}

	[Serializable]
	public class SettingsData
	{
		public bool Forward;
		public bool DeferredShading;
		public bool DeferredLighting;
		public bool Vertex;
		public bool SRP;
		public bool Meta;
		public bool PostProcessing;
		public bool MotionVectors;
		#if VRC_SDK_VRCSDK3
		public bool UnusedHardwareTiers;
		#if !UDON
		public bool UnusedVariants;
		#endif
		#endif
	}

	public static class DataManager
	{
		public static string fileName =
			Path.Combine(Application.dataPath, "../") + "ProjectSettings/3ShaderPreprocessorData.json";

		public static void Save(SettingsData data) => File.WriteAllText(fileName, JsonUtility.ToJson(data));
		public static SettingsData Load() => JsonUtility.FromJson<SettingsData>(File.ReadAllText(fileName));
	}

	[InitializeOnLoad]
	class Startup
	{
		static Startup()
		{
			if (!File.Exists(DataManager.fileName)) DataManager.Save(new SettingsData());
		}
	}

	public class CommentFreeIterator : IEnumerable<string>
	{
		private readonly IEnumerable<string> _sourceLines;

		public CommentFreeIterator(IEnumerable<string> sourceLines)
		{
			_sourceLines = sourceLines;
		}

		public IEnumerator<string> GetEnumerator()
		{
			int comment = 0;
			foreach (string xline in _sourceLines)
			{
				string line = ParserRemoveComments(xline, ref comment);
				yield return line;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public static string ParserRemoveComments(string line, ref int comment)
		{
			int lineSkip = 0;
			bool cisOpenQuote = false;


			while (true)
			{
				//Debug.Log ("Looking for comment " + lineNum);
				int openQuote = line.IndexOf("\"", lineSkip, StringComparison.CurrentCulture);
				if (cisOpenQuote)
				{
					if (openQuote == -1)
					{
						//Debug.Log("C-Open quote ignore " + lineSkip);
						break;
					}

					lineSkip = openQuote + 1;
					bool esc = false;
					int i = lineSkip - 1;
					while (i > 0 && line[i] == '\\')
					{
						esc = !esc;
						i--;
					}

					if (!esc)
					{
						cisOpenQuote = false;
					}

					//Debug.Log("C-Open quote end " + lineSkip);
					continue;
				}

				//Debug.Log ("Looking for comment " + lineSkip);
				int commentIdx;
				if (comment == 1)
				{
					commentIdx = line.IndexOf("*/", lineSkip, StringComparison.CurrentCulture);
					if (commentIdx != -1)
					{
						line = new string(' ', commentIdx + 2) + line.Substring(commentIdx + 2);
						lineSkip = commentIdx + 2;
						comment = 0;
					}
					else
					{
						line = "";
						break;
					}
				}

				commentIdx = line.IndexOf("//", lineSkip, StringComparison.CurrentCulture);
				int commentIdx2 = line.IndexOf("/*", lineSkip, StringComparison.CurrentCulture);
				if (commentIdx2 != -1 && (commentIdx == -1 || commentIdx > commentIdx2))
				{
					commentIdx = -1;
				}

				if (openQuote != -1 && (openQuote < commentIdx || commentIdx == -1) &&
				    (openQuote < commentIdx2 || commentIdx2 == -1))
				{
					cisOpenQuote = true;
					lineSkip = openQuote + 1;
					//Debug.Log("C-Open quote start " + lineSkip);
					continue;
				}

				if (commentIdx != -1)
				{
					line = line.Substring(0, commentIdx);
					break;
				}

				commentIdx = commentIdx2;
				if (commentIdx != -1)
				{
					int endCommentIdx = line.IndexOf("*/", lineSkip, StringComparison.CurrentCulture);
					if (endCommentIdx != -1)
					{
						line = line.Substring(0, commentIdx) + new string(' ', endCommentIdx + 2 - commentIdx) +
						       line.Substring(endCommentIdx + 2);
						lineSkip = endCommentIdx + 2;
					}
					else
					{
						line = line.Substring(0, commentIdx);
						comment = 1;
						break;
					}
				}
				else
				{
					break;
				}
			}

			return line;
		}
	}
}
#endif
