#region

using System;
using UnityEditor;
using UnityEngine;

#endregion

namespace _3.StencilInjector
{
	public class StencilInjector
	{
		public static readonly string[] Properties =
		{
			"[IntRange] _Stencil (\"Reference Value\", Range(0, 255)) = 0",
			"[IntRange] _StencilWriteMask (\"ReadMask\", Range(0, 255)) = 255",
			"[IntRange] _StencilReadMask (\"WriteMask\", Range(0, 255)) = 255",
			"[WideEnum(UnityEngine.Rendering.CompareFunction)] _StencilComp (\"Compare Function\", Int) = 8",
			"[WideEnum(UnityEngine.Rendering.StencilOp)] _StencilPass (\"Pass Op\", Int) = 0",
			"[WideEnum(UnityEngine.Rendering.StencilOp)] _StencilFail (\"Fail Op\", Int) = 0",
			"[WideEnum(UnityEngine.Rendering.StencilOp)] _StencilZFail (\"ZFail Op\", Int) = 0"
		};

		public static readonly string[] Pass =
		{
			"Stencil",
			"{",
			"Ref [_Stencil]",
			"ReadMask [_StencilReadMask]",
			"WriteMask [_StencilWriteMask]",
			"Comp [_StencilComp]",
			"Pass [_StencilPass]",
			"Fail [_StencilFail]",
			"ZFail [_StencilZFail]",
			/*"CompBack [_StencilCompBack]",
			"PassBack [_StencilPassBack]",
			"FailBack [_StencilFailBack]",
			"ZFailBack [_StencilZFailBack]",
			"CompFront [_StencilCompFront]",
			"PassFront [_StencilPassFront]",
			"FailFront [_StencilFailFront]",
			"ZFailFront [_StencilZFailFront]",*/
			"}"
		};

		[MenuItem("Assets/Inject Stencils")]
		private static void InjectStencils()
		{
			Shader s = Selection.activeObject as Shader;
			Shader newShader = ShaderEditor.ModifyShader(s, new StencilOperation());
			Shader newS = newShader;
			EditorGUIUtility.PingObject(newS);
		}

		public class StencilOperation : ShaderEditor.IShaderOperation
		{
			public string GetSuffix()
			{
				return "_stencil";
			}

			public bool ModifyShaderLines(ShaderEditor.ShaderState ss)
			{
				if (ss.EditShaderNameLineNum == -1)
				{
					EditorUtility.DisplayDialog("StencilInjector",
						"In " + ss.ShaderName + ": failed to find Shader \"...\" block.", "OK", "");
					// Failed to parse shader;
					return false;
				}

				if (ss.EndPropertiesLineNum == -1)
				{
					EditorUtility.DisplayDialog("StencilInjector",
						"In " + ss.ShaderName + ": failed to find end of Properties block.", "OK", "");
					// Failed to parse shader;
					return false;
				}

				if (ss.CgIncludeLineNum == -1)
				{
					EditorUtility.DisplayDialog("StencilInjector",
						"In " + ss.ShaderName + ": failed to find CGINCLUDE or appropriate insertion point.", "OK", "");
					// Failed to parse shader;
					return false;
				}

				int numSlashes = 0;
				if (!ss.Path.StartsWith("Assets/", StringComparison.CurrentCulture))
				{
					EditorUtility.DisplayDialog("StencilInjector",
						"Shader " + ss.ShaderName + " at path " + ss.Path + " must be in Assets!", "OK", "");
					return false;
				}

				string includePrefix = "";
				Debug.Log("path is " + ss.Path);
				foreach (char c in ss.Path.Substring(7))
				{
					if (c == '/')
					{
						numSlashes++;
						includePrefix += "../";
					}
				}

				if (ss.PassBlockInjectionLine != -1)
				{
					string passLine = ss.ShaderData[ss.PassBlockInjectionLine];
					string passAdd = "\n" +
					                 "       // Stencil Pass::\n" +
					                 "       Stencil\n" +
					                 "       {\n" +
					                 "       Ref [_Stencil]\n" +
					                 "       ReadMask [_StencilReadMask]\n" +
					                 "       WriteMask [_StencilWriteMask]\n" +
					                 "       Comp [_StencilComp]\n" +
					                 "       Pass [_StencilPass]\n" +
					                 "       Fail [_StencilFail]\n" +
					                 "       ZFail [_StencilZFail]\n" +
					                 "       }\n";
					passLine = passAdd;
					ss.ShaderData[ss.PassBlockInjectionLine] = passLine;
				}

				string epLine = ss.ShaderData[ss.BeginPropertiesLineNum];
				string propertiesAdd = "\n" +
				                       "        // Stencil Properties::\n" +
				                       "        [IntRange] _Stencil (\"Reference Value\", Range(0, 255)) = 0\n" +
				                       "        [IntRange] _StencilWriteMask (\"ReadMask\", Range(0, 255)) = 255\n" +
				                       "        [IntRange] _StencilReadMask (\"WriteMask\", Range(0, 255)) = 255\n" +
				                       "        [WideEnum(UnityEngine.Rendering.CompareFunction)] _StencilComp (\"Compare Function\", Int) = 8\n" +
				                       "        [WideEnum(UnityEngine.Rendering.StencilOp)] _StencilPass (\"Pass Op\", Int) = 0\n" +
				                       "        [WideEnum(UnityEngine.Rendering.StencilOp)] _StencilFail (\"Fail Op\", Int) = 0\n" +
				                       "        [WideEnum(UnityEngine.Rendering.StencilOp)] _StencilZFail (\"ZFail Op\", Int) = 0\n";
				epLine = epLine.Substring(0, ss.BeginPropertiesSkip) + propertiesAdd +
				         epLine.Substring(ss.BeginPropertiesSkip);
				ss.ShaderData[ss.BeginPropertiesLineNum] = epLine;

				string shaderLine = ss.ShaderData[ss.EditShaderNameLineNum];
				shaderLine = shaderLine.Substring(0, ss.EditShaderNameSkip) + ss.ShaderSuffix +
				             shaderLine.Substring(ss.EditShaderNameSkip);
				ss.ShaderData[ss.EditShaderNameLineNum] = shaderLine;
				string prepend = "// AUTOGENERATED by StencilInjector at " + DateTime.UtcNow.ToString("s") + "!\n";
				prepend += "// Original source file: " + ss.Path + "\n";
				prepend +=
					"// This shader will not update automatically. Please regenerate if you change the original.\n";
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