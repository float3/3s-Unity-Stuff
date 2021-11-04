#region

using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

#endregion

namespace _3.StencilInjector
{
	public class ShaderEditor : ScriptableObject
	{
		private static string[] GETBuiltinShaderSource(string shaderName, out string shaderPath)
		{
			string[] zipAssets = AssetDatabase.FindAssets("builtin_shaders");
			string path = "";
			foreach (string guid in zipAssets)
			{
				path = AssetDatabase.GUIDToAssetPath(guid);
				if (path.EndsWith(".zip"))
				{
					break;
				}
			}

			using (ZipArchive archive = ZipFile.OpenRead(path))
			{
				foreach (ZipArchiveEntry entry in archive.Entries)
				{
					if (entry.FullName.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
					{
						using (StreamReader s = new StreamReader(entry.Open()))
						{
							string fileContents = s.ReadToEnd();
							if (fileContents.IndexOf("Shader \"" + shaderName + "\"", StringComparison.Ordinal) != -1)
							{
								shaderPath = Path.GetFileName(entry.FullName);
								return fileContents.Split('\n');
							}
						}
					}
				}
			}

			shaderPath = "";
			return new string[] { };
		}

		[MenuItem("CONTEXT/Material/Generate 2d waifu TEST (Lyuma Waifu2d)")]
		private static void Waifu2dMaterial(MenuCommand command)
		{
			Material m = command.context as Material;
			if (!(m is null))
			{
				Shader newShader = ModifyShader(m.shader, new Waifu2DOperation());
				if (newShader != null)
				{
					m.shader = newShader;
				}
			}
		}

		public static Shader ModifyShader(Shader s, IShaderOperation shOp)
		{
			string shaderName = s.name;
			string path = AssetDatabase.GetAssetPath(s);
			Debug.Log("Starting to work on shader " + shaderName);
			Debug.Log("Original path: " + path);
			string[] shaderLines;
			if (path.StartsWith("Resources/unity_builtin_extra", StringComparison.CurrentCulture))
			{
				string zipPath;
				shaderLines = GETBuiltinShaderSource(shaderName, out zipPath);
				string pathToGenerated = "Assets" + "/Generated";
				if (!Directory.Exists(pathToGenerated))
				{
					Directory.CreateDirectory(pathToGenerated);
				}

				path = pathToGenerated + "/" + zipPath;
			}
			else
			{
				shaderLines = File.ReadAllLines(path);
			}

			return ModifyShaderAtPath(path, shaderName, shaderLines, shOp);
		}

		private static Shader ModifyShaderAtPath(string path, string shaderName, string[] shaderLines,
			IShaderOperation shOp)
		{
			ShaderState ss = new ShaderState();
			ss.ShaderName = shaderName;
			ss.Path = path;
			ss.ShaderData = shaderLines;
			ss.ShaderSuffix = shOp.GetSuffix();
			int state = 0;
			int comment = 0;
			int braceLevel = 0;
			int lineNum = -1;
			bool isOpenQuote = false;
			bool cisOpenQuote = false;
			foreach (string xline in ss.ShaderData)
			{
				string line = xline;
				if (path.IndexOf(ss.ShaderSuffix + ".shader", StringComparison.CurrentCulture) != -1 &&
				    shaderName.EndsWith(ss.ShaderSuffix, StringComparison.CurrentCulture))
				{
					string origPath = path.Replace(ss.ShaderSuffix + ".shader", ".shader");
					string origShaderName = shaderName.Replace(ss.ShaderSuffix, "");
					if (File.Exists(origPath))
					{
						if (EditorUtility.DisplayDialog("Lyuma ShaderModifier",
							"Detected an existing shader: Regenrate from " + origShaderName + "?", "Regenerate",
							"Cancel"))
						{
							if (path.Equals(origPath) || shaderName.Equals(origShaderName))
							{
								EditorUtility.DisplayDialog("Lyuma ShaderModifier",
									"Unable to find name of original shader for " + shaderName, "OK", "");
								return null;
							}

							Shader origShader = Resources.Load<Shader>(origPath);
							if (origShader == null)
							{
								origShader = Shader.Find(origShaderName);
							}

							return ModifyShader(origShader, shOp);
						}

						return null;
					}
				}

				lineNum++;
				int lineSkip = 0;
				while (true)
				{
					//Debug.Log ("Looking for comment " + lineNum);
					int commentIdx;
					if (comment == 1)
					{
						commentIdx = line.IndexOf("*/", lineSkip, StringComparison.CurrentCulture);
						if (commentIdx != -1)
						{
							lineSkip = commentIdx + 2;
							comment = 0;
						}
						else
						{
							line = "";
							break;
						}
					}

					int openQuote = line.IndexOf("\"", lineSkip, StringComparison.CurrentCulture);
					if (cisOpenQuote)
					{
						if (openQuote == -1)
						{
							//Debug.Log("C-Open quote ignore " + lineSkip);
							break;
						}

						lineSkip = openQuote + 1;
						cisOpenQuote = false;
						//Debug.Log("C-Open quote end " + lineSkip);
						continue;
					}

					commentIdx = line.IndexOf("//", lineSkip, StringComparison.CurrentCulture);
					int commentIdx2 = line.IndexOf("/*", lineSkip, StringComparison.CurrentCulture);
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

				lineSkip = 0;
				bool fallThrough = true;
				while (fallThrough)
				{
					//Debug.Log ("Looking for state " + state + " on line " + lineNum);
					fallThrough = false;
					switch (state)
					{
						case 0:
						{
							int shaderOff = line.IndexOf("Shader", lineSkip, StringComparison.CurrentCulture);
							if (shaderOff != -1)
							{
								int firstQuote = line.IndexOf('\"', shaderOff);
								int secondQuote = line.IndexOf('\"', firstQuote + 1);
								if (firstQuote != -1 && secondQuote != -1)
								{
									ss.EditShaderNameLineNum = lineNum;
									ss.EditShaderNameSkip = secondQuote;
									state = 1;
								}
							}
						}
							break;
						case 1:
						{
							// Find beginning of Properties block
							int shaderOff = line.IndexOf("Properties", lineSkip, StringComparison.CurrentCulture);
							if (shaderOff != -1)
							{
								state = 2;
								lineSkip = shaderOff;
								fallThrough = true;
							}
						}
							break;
						case 2:
						{
							// Find end of Properties block
							while (lineSkip < line.Length)
							{
								int openQuote = line.IndexOf("\"", lineSkip, StringComparison.CurrentCulture);
								if (isOpenQuote)
								{
									if (openQuote == -1)
									{
										//Debug.Log("Open quote ignore " + lineSkip);
										break;
									}

									lineSkip = openQuote + 1;
									isOpenQuote = false;
									//Debug.Log("Open quote end " + lineSkip);
									continue;
								}

								int openBrace = line.IndexOf("{", lineSkip, StringComparison.CurrentCulture);
								int closeBrace = line.IndexOf("}", lineSkip, StringComparison.CurrentCulture);
								if (openQuote != -1 && (openQuote < openBrace || openBrace == -1) &&
								    (openQuote < closeBrace || closeBrace == -1))
								{
									isOpenQuote = true;
									lineSkip = openQuote + 1;
									//Debug.Log("Open quote start " + lineSkip);
									continue;
								}

								//Debug.Log ("Looking for braces state " + state + " on line " + lineNum + "/" + lineSkip + " {}" + braceLevel + " open:" + openBrace + "/ close:" + closeBrace + "/ quote:" + openQuote);
								if (closeBrace != -1 && (openBrace > closeBrace || openBrace == -1))
								{
									braceLevel--;
									if (braceLevel == 0)
									{
										ss.EndPropertiesLineNum = lineNum;
										state = 3;
										fallThrough = true;
									}

									lineSkip = closeBrace + 1;
								}
								else if (openBrace != -1 && (openBrace < closeBrace || closeBrace == -1))
								{
									if (braceLevel == 0)
									{
										ss.BeginPropertiesLineNum = lineNum;
										ss.BeginPropertiesSkip = openBrace + 1;
									}

									braceLevel++;
									lineSkip = openBrace + 1;
								}
								else
								{
									break;
								}
							}
						}
							break;
						case 3:
						{
							// Find beginning of CGINCLUDE block, or beginning of a Pass or CGPROGRAM
							int cgInclude = line.IndexOf("CGINCLUDE", lineSkip, StringComparison.CurrentCulture);
							int cgProgram = line.IndexOf("CGPROGRAM", lineSkip, StringComparison.CurrentCulture);
							int passBlock = line.IndexOf("GrabPass", lineSkip, StringComparison.CurrentCulture);
							int grabPassBlock = line.IndexOf("Pass", lineSkip, StringComparison.CurrentCulture);
							if (cgInclude != -1)
							{
								ss.FoundCgInclude = true;
							}
							else if (cgProgram != -1)
							{
								ss.FoundNoCgInclude = true;
							}
							else if (grabPassBlock != -1)
							{
								ss.FoundNoCgInclude = true;
								ss.FoundGrabPassBlock = true;
							}
							else if (passBlock != -1)
							{
								if (passBlock == lineSkip || char.IsWhiteSpace(line[passBlock - 1]))
								{
									if (passBlock + 4 == line.Length || char.IsWhiteSpace(line[passBlock + 4]))
									{
										ss.FoundPassBlock = true;
										ss.FoundNoCgInclude = true;
									}
								}
							}

							if (ss.FoundCgInclude)
							{
								state = 4;
								ss.CgIncludeLineNum = lineNum + 1;
								ss.CgIncludeSkip = 0;
							}
							else if (ss.FoundNoCgInclude)
							{
								state = 4;
								ss.CgIncludeLineNum = lineNum;
								ss.CgIncludeSkip = lineSkip;
							}

							if ((ss.FoundPassBlock || ss.FoundGrabPassBlock) && ss.PassBlockInjectionLine == -1)
							{
								ss.PassBlockInjectionLine = lineNum - 1;
							}
						}
							break;
						case 4:
							// Look for modified tag, or end of shader, or custom editor.
							break;
					}
				}
			}

			Debug.Log("Done with hard work");
			if (!shOp.ModifyShaderLines(ss))
			{
				return null;
			}

			string dest = ss.Path.Replace(".shader", ss.ShaderSuffix + ".txt");
			string finalDest = ss.Path.Replace(".shader", ss.ShaderSuffix + ".shader");
			if (dest.Equals(ss.Path))
			{
				EditorUtility.DisplayDialog("Lyuma ShaderModifier",
					"Shader " + ss.ShaderName + " at path " + ss.Path + " does not have .shader!", "OK", "");
				return null;
			}

			Debug.Log("Writing shader " + dest);
			Debug.Log("Shader name" + ss.ShaderName + ss.ShaderSuffix);
			Debug.Log("Original path " + ss.Path + " name " + ss.ShaderName);
			StreamWriter writer = new StreamWriter(dest, false);
			writer.NewLine = "\n";

			foreach (var t in ss.ShaderData)
			{
				writer.WriteLine(t);
			}

			writer.Close();
			FileUtil.ReplaceFile(dest, finalDest);
			try
			{
				FileUtil.DeleteFileOrDirectory(dest);
			}
			catch (Exception)
			{
				// ignored
			}

			//FileUtil.MoveFileOrDirectory (dest, finalDest);
			AssetDatabase.ImportAsset(finalDest);
			return (Shader)AssetDatabase.LoadAssetAtPath(finalDest, typeof(Shader));
		}

		public class ShaderState
		{
			public int BeginPropertiesLineNum = -1;
			public int BeginPropertiesSkip = -1;
			public int CgIncludeLineNum = -1;
			public int CgIncludeSkip = -1;
			public int EditShaderNameLineNum = -1;
			public int EditShaderNameSkip = -1;
			public int EndPropertiesLineNum = -1;
			public bool FoundCgInclude;
			public bool FoundGrabPassBlock;
			public bool FoundNoCgInclude;
			public bool FoundPassBlock;
			public int PassBlockInjectionLine = -1;
			public string Path;
			public string[] ShaderData;
			public string ShaderName;
			public string ShaderSuffix;
		}

		public interface IShaderOperation
		{
			string GetSuffix();
			bool ModifyShaderLines(ShaderState ss);
		}

		private class Waifu2DOperation : IShaderOperation
		{
			public string GetSuffix()
			{
				return "_2d";
			}

			public bool ModifyShaderLines(ShaderState ss)
			{
				if (ss.EditShaderNameLineNum == -1)
				{
					EditorUtility.DisplayDialog("Waifu2d",
						"In " + ss.ShaderName + ": failed to find Shader \"...\" block.", "OK", "");
					// Failed to parse shader;
					return false;
				}

				if (ss.EndPropertiesLineNum == -1)
				{
					EditorUtility.DisplayDialog("Waifu2d",
						"In " + ss.ShaderName + ": failed to find end of Properties block.", "OK", "");
					// Failed to parse shader;
					return false;
				}

				if (ss.CgIncludeLineNum == -1)
				{
					EditorUtility.DisplayDialog("Waifu2d",
						"In " + ss.ShaderName + ": failed to find CGINCLUDE or appropriate insertion point.", "OK", "");
					// Failed to parse shader;
					return false;
				}

				string[] shader2dassets = AssetDatabase.FindAssets("Waifu2d.cginc");
				string includePath = "LyumaShader/Waifu2d/Waifu2d.cginc";
				foreach (string guid in shader2dassets)
				{
					Debug.Log("testI: " + AssetDatabase.GUIDToAssetPath(guid));
					includePath = AssetDatabase.GUIDToAssetPath(guid);
					if (!includePath.Contains("Waifu2d.cginc"))
					{
						continue;
					}

					if (!includePath.StartsWith("Assets/", StringComparison.CurrentCulture))
					{
						EditorUtility.DisplayDialog("Waifu2d",
							"This script at path " + includePath + " must be in Assets!", "OK", "");
						return false;
					}

					includePath = includePath.Substring(7);
					break;
				}

				Debug.Log("Including code from " + includePath);
				string cgincCode = File.ReadAllText("Assets/" + includePath);
				if (!ss.Path.StartsWith("Assets/", StringComparison.CurrentCulture))
				{
					EditorUtility.DisplayDialog("Waifu2d",
						"Shader " + ss.ShaderName + " at path " + ss.Path + " must be in Assets!", "OK", "");
					return false;
				}

				Debug.Log("path is " + ss.Path);
				foreach (char c in ss.Path.Substring(7))
				{
					if (c == '/')
					{
					}
				}

				if (ss.FoundCgInclude)
				{
					string cgIncludeLine = ss.ShaderData[ss.CgIncludeLineNum];
					string cgIncludeAdd = "//Waifu2d Generated\n#define LYUMA2D_HOTPATCH\n";
					{
						cgIncludeAdd += cgincCode.Replace("\r\n", "\n");
					}
					ss.ShaderData[ss.CgIncludeLineNum] = cgIncludeAdd + cgIncludeLine;
				}
				else
				{
					string cgIncludeLine = ss.ShaderData[ss.CgIncludeLineNum];
					string cgIncludeAdd = "\nCGINCLUDE\n//Waifu2d Generated Block\n#define LYUMA2D_HOTPATCH\n";
					{
						cgIncludeAdd += cgincCode.Replace("\r\n", "\n");
					}
					cgIncludeAdd += "ENDCG\n";
					ss.ShaderData[ss.CgIncludeLineNum] = cgIncludeLine.Substring(0, ss.CgIncludeSkip) + cgIncludeAdd +
					                                     cgIncludeLine.Substring(ss.CgIncludeSkip);
				}

				string epLine = ss.ShaderData[ss.BeginPropertiesLineNum];
				string propertiesAdd = "\n" +
				                       "        // Waifu2d Properties::\n" +
				                       "        _2d_coef (\"Twodimensionalness\", Range(0, 1)) = 0.99\n" +
				                       "        _facing_coef (\"Face in Profile\", Range (-1, 1)) = 0.0\n" +
				                       "        _lock2daxis_coef (\"Lock 2d Axis\", Range (0, 1)) = " + "1.0" + "\n" +
				                       "        _zcorrect_coef (\"Squash Z (good=.975; 0=3d; 1=z-fight)\", Float) = " +
				                       "0.975" + "\n";
				epLine = epLine.Substring(0, ss.BeginPropertiesSkip) + propertiesAdd +
				         epLine.Substring(ss.BeginPropertiesSkip);
				ss.ShaderData[ss.BeginPropertiesLineNum] = epLine;

				string shaderLine = ss.ShaderData[ss.EditShaderNameLineNum];
				shaderLine = shaderLine.Substring(0, ss.EditShaderNameSkip) + ss.ShaderSuffix +
				             shaderLine.Substring(ss.EditShaderNameSkip);
				ss.ShaderData[ss.EditShaderNameLineNum] = shaderLine;
				string prepend = "// AUTOGENERATED by LyumaShader Waifu2DGenerator at " +
				                 DateTime.UtcNow.ToString("s") + "!\n";
				prepend += "// Original source file: " + ss.Path + "\n";
				prepend +=
					"// This shader will not update automatically. Please regenerate if you change the original.\n";
				prepend +=
					"// WARNING: this shader uses relative includes. Unity might not recompile if Waifu2d.cginc changes.\n";
				prepend +=
					"// If editing Waifu2d.cginc, force a recompile by adding a space in here or regenerating.\n";
				ss.ShaderData[0] = prepend + ss.ShaderData[0];
				for (int i = 0; i < ss.ShaderData.Length; i++)
				{
					if (ss.ShaderData[i].IndexOf("CustomEditor", StringComparison.CurrentCulture) != -1)
					{
						ss.ShaderData[i] = "//" + ss.ShaderData[i];
					}
				}

				return true;
			}
		}
	}
}