/*
Copyright 2018-2021 Lyuma
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

#if UNITY_EDITOR

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#endregion

namespace f3ShaderUtility
{
	public class float3ShaderUtility : EditorWindow
	{
		public static readonly HashSet<string> BlackList = new HashSet<string>(new[]
		{
			"BILLBOARD_FACE_CAMERA_POS",
			"EDITOR_VISUALIZATION",
			"ETC1_EXTERNAL_ALPHA",
			"FOG_EXP",
			"FOG_EXP2",
			"FOG_LINEAR",
			"LOD_FADE_CROSSFADE",
			"OUTLINE_ON",
			"SHADOWS_SHADOWMASK",
			"SOFTPARTICLES_ON",
			"STEREO_INSTANCING_ON",
			"STEREO_MULTIVIEW_ON",
			"UNITY_HDR_ON",
			"UNITY_SINGLE_PASS_STEREO",
			"_EMISSION",
			"VERTEXLIGHT_ON",
			"UNDERLAY_ON",
			"UNDERLAY_INNER",
			"APPLY_FORWARD_FOG",
			"AUTO_EXPOSURE",
			"BLOOM",
			"BLOOM_LOW",
			"CHROMATIC_ABERRATION",
			"CHROMATIC_ABERRATION_LOW",
			"COLOR_GRADING_HDR",
			"COLOR_GRADING_HDR_2D",
			"COLOR_GRADING_HDR_3D",
			"COLOR_GRADING_LDR_2D",
			"DISTORT",
			"FINALPASS",
			"FOG_EXP",
			"FOG_EXP2",
			"FOG_LINEAR",
			"FXAA",
			"FXAA_KEEP_ALPHA",
			"FXAA_LOW",
			"FXAA_NO_ALPHA",
			"GRAIN",
			"SOURCE_GBUFFER",
			"STEREO_DOUBLEWIDE_TARGET",
			"STEREO_INSTANCING_ENABLED",
			"TONEMAPPING_ACES",
			"TONEMAPPING_CUSTOM",
			"TONEMAPPING_NEUTRAL",
			"VIGNETTE",
		});

		public static bool IsInBlacklist(string inputStr)
		{
			foreach (string item in BlackList)
			{
				if (inputStr.Contains(item))
				{
					return true;
				}
			}

			return false;
		}


		[MenuItem("Tools/3/ReplaceFallBackDiffuse", false, 990)]
		private static void ReplaceFallBackDiffuse()
		{
			if (EditorUtility.DisplayDialog("Replace FallBack \"Diffuse\" ?",
				    "Are you sure you want to replace FallBack \"Diffuse\" with FallBack \"Standard\" in all shaders in your Project?",
				    "Yes", "No"))
			{
				ShaderInfo[] shaders = ShaderUtil.GetAllShaderInfo();

				foreach (ShaderInfo shaderinfo in shaders)
				{
					Shader shader = Shader.Find(shaderinfo.name);
					if (AssetDatabase.GetAssetPath(shader).StartsWith("Assets") ||
					    AssetDatabase.GetAssetPath(shader).StartsWith("Packages"))
					{
						if (FindFallBackDiffuse(shader))
						{
							ReplaceFallBack(shader);
						}
					}
				}

				AssetDatabase.Refresh();
			}
		}

		[MenuItem("Tools/3/Convert global Shader Keywords to locals", false, 990)]
		private static void ConvertKeywords()
		{
			if (EditorUtility.DisplayDialog("Replace global Shader Keywords ?",
				    "Are you sure you want to replace global Shader Features and Multi Compiles with local ones in all shaders in your Project? Use at your own Risk.",
				    "Yes", "No"))
			{
				ShaderInfo[] shaders = ShaderUtil.GetAllShaderInfo();

				foreach (ShaderInfo shaderinfo in shaders)
				{
					Shader shader = Shader.Find(shaderinfo.name);
					if (AssetDatabase.GetAssetPath(shader).StartsWith("Assets") ||
					    AssetDatabase.GetAssetPath(shader).StartsWith("Packages"))
					{
						if (FindGlobalShaderKeywords(shader))
						{
							ReplaceGlobals(shader);
						}
					}
				}

				AssetDatabase.Refresh();
			}
		}


		public static void ReplaceFallBack(Shader shader)
		{
			int fallbackline = FindDiffuse(shader);

			string path = AssetDatabase.GetAssetPath(shader);

			string[] lines = File.ReadAllLines(path);
			int lineNum = -1;
			string[] newLines = new string[lines.Length];
			foreach (string line in lines)
			{
				lineNum++;
				if (lineNum >= fallbackline &&
				    lineNum <= fallbackline)
				{
					newLines[lineNum] = lines[lineNum].Replace("Diffuse", "Standard");
				}
				else
				{
					newLines[lineNum] = lines[lineNum];
				}
			}

			File.WriteAllLines(path, newLines);
		}

		public static void ReplaceGlobals(Shader shader)
		{
			string path = AssetDatabase.GetAssetPath(shader);

			string[] lines = File.ReadAllLines(path);
			int lineNum = -1;
			string[] newLines = new string[lines.Length];
			foreach (string line in lines)
			{
				lineNum++;
				if (line.Contains("shader_feature ") && !IsInBlacklist(line))
				{
					newLines[lineNum] = lines[lineNum].Replace("shader_feature ", "shader_feature_local ");
				}
				else if (line.Contains("multi_compile ") && !IsInBlacklist(line))
				{
					newLines[lineNum] = lines[lineNum].Replace("multi_compile ", "multi_compile_local ");
				}
				else
				{
					newLines[lineNum] = lines[lineNum];
				}
			}

			File.WriteAllLines(path, newLines);
		}

		private static bool FindGlobalShaderKeywords(Shader shader)
		{
			if (shader.name.Contains("Hidden/PostProcessing")) return false;
			string filePath = AssetDatabase.GetAssetPath(shader);
			string[] shaderLines = File.ReadAllLines(filePath);
			bool fallThrough = true;
			foreach (string xline in new CommentFreeIterator(shaderLines))
			{
				string line = xline;
				int lineSkip = 0;

				while (fallThrough)
				{
					//Debug.Log("Looking for state " + state + " on line " + lineNum);
					fallThrough = false;
					lineSkip = 0; // ???
				}

				int global = line.IndexOf("shader_feature ", lineSkip, StringComparison.Ordinal);
				if (global == -1)
				{
					global = line.IndexOf("multi_compile ", lineSkip, StringComparison.Ordinal);
				}

				if (global != -1 && !IsInBlacklist(line))
				{
					return true;
				}
			}

			return false;
		}

		private static bool FindFallBackDiffuse(Shader shader)
		{
			string filePath = AssetDatabase.GetAssetPath(shader);
			string[] shaderLines = File.ReadAllLines(filePath);
			bool fallThrough = true;
			bool found = false;
			foreach (string xline in new CommentFreeIterator(shaderLines))
			{
				string line = xline;
				int lineSkip = 0;

				while (fallThrough)
				{
					//Debug.Log("Looking for state " + state + " on line " + lineNum);
					fallThrough = false;
					lineSkip = 0; // ???
				}

				if (found)
				{
					int diffuse = line.IndexOf("Diffuse", lineSkip, StringComparison.Ordinal);
					if (diffuse != -1)
					{
						return true;
					}
				}


				int fallBack = line.IndexOf("FallBack", lineSkip, StringComparison.Ordinal);
				if (fallBack != -1)
				{
					found = true;
					if (line.IndexOf("Diffuse", StringComparison.Ordinal) != -1) return true;
				}
			}

			return false;
		}

		private static int FindDiffuse(Shader shader)
		{
			string filePath = AssetDatabase.GetAssetPath(shader);
			int lineNum = -1;
			string[] shaderLines = File.ReadAllLines(filePath);
			bool fallThrough = true;
			bool found = false;
			foreach (string xline in new CommentFreeIterator(shaderLines))
			{
				string line = xline;
				lineNum++;
				int lineSkip = 0;

				while (fallThrough)
				{
					//Debug.Log("Looking for state " + state + " on line " + lineNum);
					fallThrough = false;
					lineSkip = 0; // ???
				}

				if (found)
				{
					int diffuse = line.IndexOf("Diffuse", lineSkip, StringComparison.Ordinal);
					if (diffuse != -1)
					{
						return lineNum;
					}
				}

				int fallBack = line.IndexOf("FallBack", lineSkip, StringComparison.Ordinal);
				if (fallBack != -1)
				{
					found = true;
					if (line.IndexOf("Diffuse", StringComparison.Ordinal) != -1) return lineNum;
				}
			}

			return -1;
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