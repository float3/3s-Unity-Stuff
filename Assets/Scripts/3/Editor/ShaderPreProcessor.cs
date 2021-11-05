#if UNITY_EDITOR && (VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3)

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;

#endregion

// thank you Scruffy and z3y

// ReSharper disable once CheckNamespace
namespace _3.ShaderPreProcessor
{
	public class OnBuildRequest : IVRCSDKBuildRequestedCallback
	{
		public static VRCSDKRequestedBuildType RequestedBuildTypeCallback;
		public int callbackOrder => 6;


		public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
		{
			if (requestedBuildType == VRCSDKRequestedBuildType.Avatar)
			{
				RequestedBuildTypeCallback = requestedBuildType;
			}

			else if (requestedBuildType == VRCSDKRequestedBuildType.Scene)
			{
				RequestedBuildTypeCallback = requestedBuildType;

				Scene scene = SceneManager.GetActiveScene();

				string[] shaderpaths = AssetDatabase.GetDependencies(scene.path).Where(x => x.EndsWith(".shader"))
					.ToArray();

				PreprocessShaders.ExcludedShaders.Clear();

				foreach (string shaderpath in shaderpaths)
				{
					string[] shader = File.ReadAllLines(shaderpath);

					foreach (string line in new CommentFreeIterator(shader))
					{
						if (line.Contains("UsePass"))
						{
							string shadernameRegex = "(?<=\\\")(.*)(?=\\/)";
							string passnameRegex = "(?<=\\/)(.*?)(?=\")";

							string[] excludedShaderPass = new string[3];

							excludedShaderPass[0] = Regex.Match(line, shadernameRegex).Value;
							excludedShaderPass[1] = Regex.Match(line, passnameRegex).Value;
							excludedShaderPass[2] = AssetDatabase.LoadAssetAtPath<Shader>(shaderpath).name;

							PreprocessShaders.ExcludedShaders.Add(excludedShaderPass);
						}
					}
				}
			}

			return true;
		}
	}

	public class OnAvatarBuild : IVRCSDKPreprocessAvatarCallback
	{
		public int callbackOrder => 3;

		public bool OnPreprocessAvatar(GameObject avatarGameObject)
		{
			Renderer[] renderers = avatarGameObject.GetComponentsInChildren<Renderer>(true);

			PreprocessShaders.ExcludedShaders.Clear();

			foreach (Renderer renderer in renderers)
			{
				foreach (Material material in renderer.sharedMaterials)
				{
					string[] shader = File.ReadAllLines(AssetDatabase.GetAssetPath(material.shader));

					foreach (string line in new CommentFreeIterator(shader))
					{
						if (line.Contains("UsePass"))
						{
							string shadernameRegex = "(?<=\\\")(.*)(?=\\/)";
							string passnameRegex = "(?<=\\/)(.*?)(?=\")";

							string[] excludedShaderPass = new string[3];

							excludedShaderPass[0] = Regex.Match(line, shadernameRegex).Value;
							excludedShaderPass[1] = Regex.Match(line, passnameRegex).Value;
							excludedShaderPass[2] = material.shader.name;

							PreprocessShaders.ExcludedShaders.Add(excludedShaderPass);
						}
					}
				}
			}

			return true;
		}
	}

	public class PreprocessShaders : IPreprocessShaders
	{
		public static List<string[]> ExcludedShaders = new List<string[]>();

		private readonly PassType[] _pts =
		{
			PassType.Deferred, PassType.LightPrePassBase, PassType.LightPrePassFinal, PassType.VertexLM,
			PassType.MotionVectors, PassType.ScriptableRenderPipeline, PassType.ScriptableRenderPipelineDefaultUnlit
		};

		public int callbackOrder => 9;

		public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
		{
			string shaderName = shader.name;
			shaderName = string.IsNullOrEmpty(shaderName) ? "Empty" : shaderName;
			if (shaderName.Contains("Hidden/PostProcessing"))
			{
				data.Clear();
				return;
			}

			foreach (string[] excludedShader in ExcludedShaders)
			{
				if (excludedShader[0] == shader.name && excludedShader[1] == snippet.passName)
				{
					Debug.Log(
						$"Pass: {snippet.passName} in Shader: {shader.name} was included because {excludedShader[2]} uses it in a UsePass");
					return;
				}
			}

			if (_pts.Contains(snippet.passType) ||
			    OnBuildRequest.RequestedBuildTypeCallback == VRCSDKRequestedBuildType.Scene &&
			    !Lightmapping.realtimeGI && snippet.passType == PassType.Meta ||
			    OnBuildRequest.RequestedBuildTypeCallback == VRCSDKRequestedBuildType.Avatar &&
			    snippet.passType == PassType.Meta)
			{
				data.Clear();
			}
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