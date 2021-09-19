#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;


namespace _3.Editor
{
	internal class AssetPreProcessor : IPreprocessBuildWithReport
	{
		public int callbackOrder { get { return 0; } }
		public void OnPreprocessBuild(BuildReport report)
		{
			public string path = string(Application.dataPath - @"/Assets");

			path += @"/Library/PackageCache/com.unity.postprocessing@3.1.1/PostProcessing";
			Directory.Delete(path + @"/Textures");
			Directory.Delete(path + @"/Shaders");
			Console.Log("path");
		}
	}	
}
#endif