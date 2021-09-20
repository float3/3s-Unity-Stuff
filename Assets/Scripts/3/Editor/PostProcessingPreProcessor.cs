#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;


namespace _3.Editor
{
	internal class PostProcessingPreProcessor : IPreprocessBuildWithReport
	{
		public int callbackOrder { get { return 0; } }
		public void OnPreprocessBuild(BuildReport report)
		{
			Debug.Log("try Packages/");
			try
			{
				Directory.Delete(Path.GetFullPath("Packages/com.unity.postprocessing@3.1.1/PostProcessing/Shaders"));
				Directory.Delete(Path.GetFullPath("Packages/com.unity.postprocessing@3.1.1/PostProcessing/Textures"));
			}
			catch(IOException e) 
			{
				Debug.Log("IOException");
				Debug.Log(e);
			}
			catch(UnauthorizedAccessException e)
			{
				Debug.Log("UnauthorizedAccessException");
				Debug.Log(e);
			}
			catch(DirectoryNotFoundException e)
			{
				Debug.Log("DirectoryNotFoundException");
				Debug.Log(e);
			}

			Debug.Log("try Library/PackageCache");
			try
			{
				Directory.Delete(Path.GetFullPath("Library/PackageCache/com.unity.postprocessing@3.1.1/PostProcessing/Shaders"));
				Directory.Delete(Path.GetFullPath("Library/PackageCache/com.unity.postprocessing@3.1.1/PostProcessing/Textures"));
			}
			catch(IOException e) 
			{
				Debug.Log("IOException");
				Debug.Log(e);
			}
			catch(UnauthorizedAccessException e)
			{
				Debug.Log("UnauthorizedAccessException");
				Debug.Log(e);
			}
			catch(DirectoryNotFoundException e)
			{
				Debug.Log("DirectoryNotFoundException");
				Debug.Log(e);
			}
	}	
}
#endif
