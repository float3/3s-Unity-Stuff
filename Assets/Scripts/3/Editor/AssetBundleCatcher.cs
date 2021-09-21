using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using VRC.Core;

namespace _3.Editor
{
	public class AssetBundleCacher
	{
		public static AssetBundleCacher Instance;

		static AssetBundleCacher()
		{
			EditorApplication.playModeStateChanged += CacheAssetbundle;
		}

		private static void CacheAssetbundle(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingPlayMode)
			{
				string fileName = PlayerSettings.companyName;
				string sourcePath = Application.temporaryCachePath;


				PipelineManager pipelineManager = UnityEngine.Object.FindObjectOfType<PipelineManager>();

				string id = pipelineManager.blueprintId;

				int version =
					string targetPath = GetVRChatCacheFullLocation(id, int version);

				string sourceFile = System.IO.Path.Combine(sourcePath, fileName);
				string destFile = System.IO.Path.Combine(targetPath, fileName);

				System.IO.Directory.CreateDirectory(targetPath);

				System.IO.File.Copy(sourceFile, destFile, true);


				if (System.IO.Directory.Exists(sourcePath))
				{
					string[] files = System.IO.Directory.GetFiles(sourcePath);

					// Copy the files and overwrite destination files if they already exist.
					foreach (string s in files)
					{
						// Use static Path methods to extract only the file name from the path.
						fileName = System.IO.Path.GetFileName(s);
						destFile = System.IO.Path.Combine(targetPath, fileName);
						System.IO.File.Copy(s, destFile, true);
					}
				}
			}
		}

		public string GetAssetId(string id)
		{
			byte[] hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(id));
			StringBuilder idHex = new StringBuilder(hash.Length * 2);
			foreach (byte b in hash)
			{
				idHex.AppendFormat("{0:x2}", b);
			}

			return idHex.ToString().ToUpper().Substring(0, 16);
		}

		public string GetAssetVersion(int version)
		{
			byte[] bytes = BitConverter.GetBytes(version);
			string versionHex = String.Empty;
			foreach (byte b in bytes)
			{
				versionHex += b.ToString("X2");
			}

			return versionHex.PadLeft(32, '0');
		}

		public string GetVRChatCacheLocation()
		{
			var cachePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
			                @"Low\VRChat\VRChat\Cache-WindowsPlayer";

			return cachePath;
		}


		public string GetVRChatCacheFullLocation(string id, int version)
		{
			var cachePath = GetVRChatCacheLocation();
			var idHash = GetAssetId(id);
			var versionLocation = GetAssetVersion(version);


			return Path.Combine(cachePath, idHash, versionLocation);
		}
	}
}