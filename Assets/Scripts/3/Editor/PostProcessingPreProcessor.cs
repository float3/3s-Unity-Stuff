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
			try{
				Directory.Delete(Path.GetFullPath("Packges/com.unity.postprocessing@3.1.1/PostProcessing/Shaders"));
				Directory.Delete(Path.GetFullPath("Packages/com.unity.postprocessing@3.1.1/PostProcessing/Textures"));
				Directory.Delete(Path.GetFullPath("Library/PackageCache/com.unity.postprocessing@3.1.1/PostProcessing/Shaders"));
				Directory.Delete(Path.GetFullPath("Library/PackageCache/com.unity.postprocessing@3.1.1/PostProcessing/Textures"));
			}
			catch(){}
	}	
}
#endif
