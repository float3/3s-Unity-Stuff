//#define DEBUG_PARSER 1

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using tinycpp;
using UnityEditor;
using UnityEngine;

#endregion

namespace _3.StencilInjector
{
	public class ParsedShader
	{
		private readonly string _filePath;

		private readonly ZipArchive _zipArchive = openBuiltinShaderZip();
		private readonly List<CgBlock> CgBlocks = new List<CgBlock>();
		public List<Block> Passes = new List<Block>();
		public string[] ShaderLines;
		public string ShaderName;

		public ParsedShader(Shader shader)
		{
			_filePath = GETPath(shader);
			Parse();
		}

		private static ZipArchive openBuiltinShaderZip()
		{
			string[] zipAssets = AssetDatabase.FindAssets("builtin_shaders");
			string path = "";
			foreach (string guid in zipAssets)
			{
				path = AssetDatabase.GUIDToAssetPath(guid);
				if (path.EndsWith(".zip"))
					break;
			}

			return ZipFile.OpenRead(path);
		}

		public static string[] GETBuiltinShaderSource(ZipArchive zipArchive, string shaderName, out string shaderPath)
		{
			foreach (ZipArchiveEntry entry in zipArchive.Entries)
				if (entry.FullName.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
					using (StreamReader s = new StreamReader(entry.Open()))
					{
						string fileContents = s.ReadToEnd();
						if (fileContents.IndexOf("Shader \"" + shaderName + "\"", StringComparison.Ordinal) != -1)
						{
							shaderPath = Path.GetFileName(entry.FullName);
							return fileContents.Split('\n');
						}
					}

			shaderPath = "";
			return new string[] { };
		}

		public static string GETZipCgincSource(ZipArchive zipArchive, string cgincPath)
		{
			foreach (ZipArchiveEntry entry in zipArchive.Entries)
				if (entry.FullName.Equals("CGIncludes/" + cgincPath, StringComparison.OrdinalIgnoreCase))
					using (StreamReader s = new StreamReader(entry.Open()))
					{
						string fileContents = s.ReadToEnd();
						return fileContents;
					}

			return "";
		}

		public string GETCgincSource(string fileContext, ref string fileName)
		{
			if (fileContext.Contains('/'))
			{
				string fsPath = Path.Combine(Path.GetDirectoryName(fileContext) ?? string.Empty, fileName);
				if (File.Exists(fsPath))
				{
					fileName = fsPath;
					return File.ReadAllText(fsPath);
				}
			}

			string ret = GETZipCgincSource(_zipArchive, fileName);
			if (ret.Length == 0)
				Debug.LogError("Failed to find include " + fileName + " from " + fileContext);
			return ret;
		}

		private void Parse()
		{
			ShaderLines = File.ReadAllLines(_filePath);
			ParseState state = ParseState.ShaderName;
			ParseState lastState = ParseState.PassBlock;
			int braceLevel = 0;
			int lineNum = -1;
			int beginBraceLineNum = -1;
			int beginCgLineNum = -1;
			bool isOpenQuote = false;
			// bool CisOpenQuote = false;
			Block.Type passType = Block.Type.None;
			Block.Type cgType = Block.Type.None;
			Regex programCgRegex = new Regex("\\b(CG|HLSL)PROGRAM\\b|\\b(CG|HLSL)INCLUDE\\b");
			Regex passCgRegex = new Regex("\\bGrabPass\\b|\\bPass\\b|\\b(CG|HLSL)PROGRAM\\b|\\b(CG|HLSL)INCLUDE\\b");
			foreach (string xline in new CommentFreeIterator(ShaderLines))
			{
				string line = xline;
				lineNum++;
				int lineSkip = 0;
				/*
				while (true) {
				    //Debug.Log ("Looking for comment " + lineNum);
				    int openQuote = line.IndexOf ("\"", lineSkip, StringComparison.CurrentCulture);
				    if (CisOpenQuote) {
				        if (openQuote == -1) {
				            //Debug.Log("C-Open quote ignore " + lineSkip);
				            break;
				        } else {
				            lineSkip = openQuote + 1;
				            CisOpenQuote = false;
				        }
				        //Debug.Log("C-Open quote end " + lineSkip);
				        continue;
				    }
				    if (openQuote != -1) {
				        CisOpenQuote = true;
				        lineSkip = openQuote + 1;
				        //Debug.Log("C-Open quote start " + lineSkip);
				        continue;
				    }
				}
				lineSkip = 0;
				*/
				bool fallThrough = true;

				while (fallThrough)
				{
					if (state != lastState)
					{
						#if DEBUG_PARSER
                        Debug.Log ("Line " + lineNum + ": state changed to " + state); }
						#endif
						lastState = state;
					}

					Debug.Log("Looking for state " + state + " on line " + lineNum);
					fallThrough = false;
					lineSkip = 0; // ???
					switch (state)
					{
						case ParseState.ShaderName:
						{
							int shaderOff = line.IndexOf("Shader", lineSkip, StringComparison.CurrentCulture);
							if (shaderOff != -1)
							{
								int firstQuote = line.IndexOf('\"', shaderOff);
								int secondQuote = line.IndexOf('\"', firstQuote + 1);
								if (firstQuote != -1 && secondQuote != -1)
								{
									ShaderName = line.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
									new Block(this, Block.Type.ShaderName, lineNum, lineNum);
									fallThrough = true;
									state = ParseState.Properties;
								}
							}
						}
							break;
						case ParseState.Properties:
						{
							// Find beginning of Properties block
							int shaderOff = line.IndexOf("Properties", lineSkip, StringComparison.CurrentCulture);
							if (shaderOff != -1)
							{
								state = ParseState.PropertiesBlock;
								passType = Block.Type.Properties;
								fallThrough = true;
							}
						}
							break;
						case ParseState.PropertiesBlock:
						case ParseState.PassBlock:
						{
							// Find end of Properties block
							int i = 0;
							while (lineSkip < line.Length && i < 10000)
							{
								i++;
								int openQuote = line.IndexOf("\"", lineSkip, StringComparison.CurrentCulture);
								if (isOpenQuote)
								{
									if (openQuote == -1)
									{
										Debug.Log("Open quote ignore " + lineSkip);
										break;
									}

									lineSkip = openQuote + 1;
									bool esc = false;
									int xi = lineSkip - 1;
									while (xi > 0 && line[xi] == '\\')
									{
										esc = !esc;
										xi--;
									}

									if (!esc)
										isOpenQuote = false;
									Debug.Log("Open quote end " + lineSkip);
									continue;
								}

								int openBrace = line.IndexOf("{", lineSkip, StringComparison.CurrentCulture);
								int closeBrace = line.IndexOf("}", lineSkip, StringComparison.CurrentCulture);
								if (openQuote != -1 && (openQuote < openBrace || openBrace == -1) &&
								    (openQuote < closeBrace || closeBrace == -1))
								{
									isOpenQuote = true;
									lineSkip = openQuote + 1;
									Debug.Log("Open quote start " + lineSkip);
									continue;
								}

								Match m = null;
								if (state == ParseState.PassBlock)
									m = programCgRegex.Match(line, lineSkip);
								Debug.Log("Looking for braces state " + state + " on line " + lineNum + "/" + lineSkip +
								          " {}" + braceLevel + " open:" + openBrace + "/ close:" + closeBrace +
								          " m.index " + (m == null ? -2 : m.Index));
								if (m != null && m.Success && (closeBrace == -1 || m.Index < closeBrace) &&
								    (openBrace == -1 || m.Index < openBrace))
								{
									string match = m.Value;
									#if DEBUG_PARSER
                                    Debug.Log ("Found " + match + " in Pass block line " + lineNum);
									#endif
									cgType = match.Equals("HLSLINCLUDE") || match.Equals("CGINCLUDE")
										? Block.Type.CgInclude
										: Block.Type.CgProgram;
									state = ParseState.PassCg;
									fallThrough = false;
									lineSkip = line.Length;
									beginCgLineNum = lineNum + 1;
									break;
								}

								if (closeBrace != -1 && (openBrace > closeBrace || openBrace == -1))
								{
									lineSkip = closeBrace + 1;
									braceLevel--;
									if (braceLevel == 0)
									{
										Block b = new Block(this, passType, beginBraceLineNum, lineNum);
										if (state == ParseState.PropertiesBlock)
										{
										}
										else if (state == ParseState.PassBlock)
											Passes.Add(b);

										state = ParseState.SubShader;
										fallThrough = true;
										break;
									}
								}
								else if (openBrace != -1 && (openBrace < closeBrace || closeBrace == -1))
								{
									if (braceLevel == 0)
									{
										beginBraceLineNum = lineNum;
									}

									braceLevel++;
									lineSkip = openBrace + 1;
								}
								else
								{
									break;
								}
							}

							if (i >= 9999)
								throw new Exception("Loop overflow " + i + "in braces search " + lineNum + "/" +
								                    lineSkip + ":" + braceLevel);
						}
							break;
						case ParseState.SubShader:
						{
							Match m;
							m = passCgRegex.Match(line, lineSkip);
							if (m != null && m.Success)
							{
								string match = m.Value;
								if (match.Equals("HLSLINCLUDE") || match.Equals("HLSLPROGRAM") ||
								    match.Equals("CGINCLUDE") || match.Equals("CGPROGRAM"))
								{
									cgType = match.Equals("HLSLINCLUDE") || match.Equals("CGINCLUDE")
										? Block.Type.CgInclude
										: Block.Type.CgProgram;
									#if DEBUG_PARSER
                                    Debug.Log ("Found " + match + " in SubShader line " + lineNum + ": " + cgType);
									#endif
									state = ParseState.SubShaderCg;
									fallThrough = true;
									beginCgLineNum = lineNum + 1;
									break;
								}

								if (match.Equals("GrabPass") || match.Equals("Pass"))
								{
									state = ParseState.PassBlock;
									fallThrough = true;
									passType = match.Equals("Pass") ? Block.Type.Pass : Block.Type.GrabPass;
								}
							}
						}
							break;
						case ParseState.SubShaderCg:
						case ParseState.PassCg:
							int endCg = line.IndexOf("ENDCG", lineSkip, StringComparison.CurrentCulture);
							if (endCg != -1)
							{
								#if DEBUG_PARSER
                            Debug.Log ("Ending cg:" + cgType + " lines " + beginCGLineNum + "-" + lineNum);
								#endif
								string buf = "";
								if (cgType == Block.Type.CgProgram)
								{
									int whichBlock = 0;
									for (int i = 0;
											i < beginCgLineNum;
											i++) // if (i == cgBlocks[whichBlock].beginLineNum) {
										//     buf += shaderLines[i].Substring(cgBlocks[whichBlock].beginSkip) + "\n";
										// } else
										if (whichBlock >= CgBlocks.Count)
											buf += "\n";
										else if (i >= CgBlocks[whichBlock].BeginLineNum &&
										         i < CgBlocks[whichBlock].endLineNum) buf += ShaderLines[i] + "\n";
										// } else if (i == cgBlocks[whichBlock].endLineNum) {
										//     buf += shaderLines[i].Substring(0, cgBlocks[whichBlock].endSkip) + "\n";
										//     whichBlock += 1;
										else
											buf += "\n";
									for (int i = beginCgLineNum; i < lineNum; i++)
										buf += ShaderLines[i] + "\n";
								}

								CgBlock b = new CgBlock(this, cgType, beginCgLineNum, lineNum, buf);
								Passes.Add(b);
								CgBlocks.Add(b);
								state = state == ParseState.SubShaderCg ? ParseState.SubShader : ParseState.PassBlock;
							}

							// Look for modified tag, or end of shader, or custom editor.
							break;
					}
				}
			}

			foreach (Block b in Passes)
			{
				CgBlock cgb = b as CgBlock;
				if (cgb != null || b.type == Block.Type.CgInclude || b.type == Block.Type.CgProgram)
					Debug.Log("Shader has a " + b.type + " on lines " + b.BeginLineNum + "-" + b.endLineNum +
					          " with vert:" + cgb.vertFunction + " geom:" + cgb.geomFunction + " surf:" +
					          cgb.surfFunction +
					          " | vert accepts input " + cgb.vertInputType + " output " + cgb.vertReturnType);
				else
					Debug.Log("Shader has " + b.type + " block on lines " + b.BeginLineNum + "-" + b.endLineNum);
			}
		}

		public static string GETPath(Shader shader)
		{
			if (shader == null)
				return null;
			string path = AssetDatabase.GetAssetPath(shader);
			if (path.StartsWith("Resources/unity_builtin_extra", StringComparison.CurrentCulture) &&
			    "Standard".Equals(shader.name))
			{
				string[] tmpassets = AssetDatabase.FindAssets("StandardSimple");
				foreach (string guid in tmpassets)
				{
					path = AssetDatabase.GUIDToAssetPath(guid);
					if (path.IndexOf(".shader", StringComparison.CurrentCulture) != -1)
						break;
				}
			}

			return path;
		}

		[MenuItem("CONTEXT/Shader/TestShaderParser")]
		private static void TestShaderParser(MenuCommand command)
		{
			Shader s = command.context as Shader;
			// ReSharper disable once ObjectCreationAsStatement
			new ParsedShader(s);
		}


		public class Block
		{
			public enum Type
			{
				None = -1,
				ShaderName = 0,
				Properties = 1,
				CgInclude = 2,
				CgProgram = 3,
				GrabPass = 4,
				Pass = 5
			}

			public int BeginLineNum;
			public int endLineNum;
			public ParsedShader shader;
			public Type type;

			public Block(ParsedShader shader, Type type, int beginLine, int endLine)
			{
				this.shader = shader;
				BeginLineNum = -1;
				this.type = type;
				BeginLineNum = beginLine;
				endLineNum = endLine;
			}
		}

		public class CgBlock : Block
		{
			public string geomFunction;
			public Dictionary<int, string> pragmas = new Dictionary<int, string>();
			public int shaderTarget;
			public string surfFunction;
			public string vertFunction;
			public string vertInputType;
			public string vertReturnType;

			public CgBlock(ParsedShader shader, Type type, int beginLine, int endLine, string cgProgramSource) : base(
				shader, type, beginLine, endLine)
			{
				if (type != Type.CgProgram)
					return;
				//for (int i = beginLine; i < endLine; i++) {
				Regex re = new Regex(
					"^\\s*(vertex|fragment|geometry|surface|domain|hull|target)\\s*(\\S+)\\s*.*(\\bvertex:(\\S+))?\\s*.*$");
				foreach (var pragmaLine in new PragmaIterator(
					shader.ShaderLines.Skip(beginLine).Take(endLine - beginLine), beginLine))
				{
					Match m = re.Match(pragmaLine.Key);
					#if DEBUG_PARSER
                    Debug.Log ("Found #pragma " + pragmaLine.Key + ": match " + m.Groups [1].Value + "," + m.Groups [2].Value + "," + m.Groups [3].Value + "," + m.Groups [4].Value);
					#endif
					if (m.Success)
					{
						string funcType = m.Groups[1].Value;
						if (funcType.Equals("surface"))
						{
							surfFunction = m.Groups[2].Value;
						}
						else if (funcType.Equals("vertex"))
						{
							vertFunction = m.Groups[2].Value;
						}
						else if (funcType.Equals("fragment"))
						{
						}
						else if (funcType.Equals("geometry"))
						{
							geomFunction = m.Groups[2].Value;
							if (shaderTarget <= 0)
								shaderTarget = -40;
						}
						else if (funcType.Equals("domain"))
						{
							if (shaderTarget <= 0)
								shaderTarget = -50;
						}
						else if (funcType.Equals("hull"))
						{
							if (shaderTarget <= 0)
								shaderTarget = -50;
						}
						else if (funcType.Equals("target"))
						{
							shaderTarget = Mathf.RoundToInt(float.Parse(m.Groups[2].Value) * 10.0f);
						}
					}

					pragmas.Add(pragmaLine.Value, pragmaLine.Key);
				}

				if (shaderTarget < 0)
					shaderTarget = -shaderTarget;
				if (shaderTarget == 0)
				{
					Debug.Log("Note: shader " + this.shader.ShaderName + " using old shader target " +
					          shaderTarget / 10 + "." + shaderTarget % 10);
					shaderTarget = 20;
				}

				Preproc pp = new Preproc();
				CgProgramOutputCollector cgpo = new CgProgramOutputCollector(shader);
				pp.set_output_interface(cgpo);
				pp.cpp_add_define("SHADER_API_D3D11 1");
				pp.cpp_add_define("SHADER_TARGET " + shaderTarget);
				pp.cpp_add_define("SHADER_TARGET_SURFACE_ANALYSIS 1"); // maybe?
				pp.cpp_add_define("UNITY_VERSION " + 2018420);
				pp.cpp_add_define(
					"UNITY_PASS_SHADOWCASTER 1"); // FIXME: this is wrong. WE need to get the LightMode from tags. so we should parse tags, too.
				pp.cpp_add_define("UNITY_INSTANCING_ENABLED 1");
				//pp.cpp_add_define("UNITY_STEREO_INSTANCING_ENABLED 1");
				pp.parse_file(shader._filePath, cgProgramSource);
				string code = cgpo.GetOutputCode();
				File.WriteAllText("output_code.txt", code);

				if (surfFunction != null)
				{
					vertReturnType = "v2f_" + surfFunction;
					vertInputType = "appdata_full";
					vertFunction = "vert_" + surfFunction; // will be autogenerated.
				}
				else
				{
					Regex vertRe = new Regex("\\b(\\S+)\\b\\s+" + vertFunction + "\\s*\\(\\s*(\\S*)\\b");
					foreach (string lin in new CommentFreeIterator(shader.ShaderLines.Skip(beginLine)
						.Take(endLine - beginLine)))
					{
						if (lin.IndexOf("// Surface shader code generated based on:",
							StringComparison.CurrentCulture) != -1)
						{
						}

						/*vertReturnType = "v2f_surf";
						    vertInputType = "appdata_full";
						    break;*/
						/*
						int vertIndex = lin.IndexOf (" " + vertFunction + " ", StringComparison.CurrentCulture);
						if (vertIndex != -1) {
						    vertReturnType = lin.Substring (0, vertIndex).Trim ();
						    int paren = lin.IndexOf ("(", vertIndex, StringComparison.CurrentCulture);
						    if (paren != -1) {
						        int space = lin.IndexOf (" ", paren);
						        if (space != -1) {
						            vertInputType = lin.Substring (paren + 1, space - paren - 1).Trim();
						        }
						    }
						}
						*/
						Match m = vertRe.Match(lin);
						if (m.Success)
						{
							vertReturnType = m.Groups[1].Value;
							vertInputType = m.Groups[2].Value;
						}
					}
				}
			}

			private class CgProgramOutputCollector : Preproc.IOutputInterface
			{
				private readonly StringBuilder _outputCode = new StringBuilder();
				private readonly ParsedShader _parsedShader;
				private bool _wasNewline;

				public CgProgramOutputCollector(ParsedShader ps)
				{
					_parsedShader = ps;
				}

				public void Emit(string s, string file, int line, int column)
				{
					if (_wasNewline && s.Trim() == "" && s.EndsWith("\n"))
						return;
					if (s.Trim() != "" || s.EndsWith("\n"))
						_wasNewline = s.EndsWith("\n");
					_outputCode.Append(s);
				}

				public void EmitError(string msg)
				{
					Debug.LogError(msg);
				}

				public void EmitWarning(string msg)
				{
					Debug.LogWarning(msg);
				}

				public string IncludeFile(string fileContext, ref string filename)
				{
					Debug.Log("Found a pound include " + fileContext + "," + filename);
					return _parsedShader.GETCgincSource(fileContext, ref filename);
				}

				public string GetOutputCode()
				{
					return _outputCode.ToString();
				}
			}
		}

		private enum ParseState
		{
			ShaderName = 0,
			Properties = 1,
			PropertiesBlock = 2,
			SubShader = 3,
			PassBlock = 4,
			SubShaderCg = 6,
			PassCg = 7
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
						if (openQuote == -1) //Debug.Log("C-Open quote ignore " + lineSkip);
							break;
						lineSkip = openQuote + 1;
						bool esc = false;
						int i = lineSkip - 1;
						while (i > 0 && line[i] == '\\')
						{
							esc = !esc;
							i--;
						}

						if (!esc)
							cisOpenQuote = false;
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
						commentIdx = -1;
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

		public class PragmaIterator : IEnumerable<KeyValuePair<string, int>>
		{
			private readonly IEnumerable<string> _sourceLines;
			private readonly int _startLine;

			public PragmaIterator(IEnumerable<string> sourceLines, int startLine)
			{
				_sourceLines = sourceLines;
				_startLine = startLine;
			}

			public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
			{
				Regex re = new Regex("^\\s*#\\s*pragma\\s+(.*)$");
				//Regex re = new Regex ("^\\s*#\\s*pragma\\s+geometry\\s+\(\\S*\)\\s*$");
				int ln = _startLine - 1;
				foreach (string xline in _sourceLines)
				{
					string line = xline;
					ln++;
					/*if (ln < startLine + 10) { Debug.Log ("Check line " + ln +"/" + line); }
					line = line.Trim ();
					if (line.StartsWith("#", StringComparison.CurrentCulture)) {
					    Debug.Log ("Check pragma " + ln + "/" + line);
					}*/
					if (re.IsMatch(line)) //Debug.Log ("Matched pragma " + line);
						yield return new KeyValuePair<string, int>(re.Replace(line, match => match.Groups[1].Value),
							ln);
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}
	}
}