// Code by _3, Razgriz
// based on
// https://forum.unity.com/threads/beyond-wrinkle-maps-to-realtime-tension-maps-current-state-of-the-unity-possibilities.509473/#post-5202389
// https://github.com/ted10401/GeometryShaderCookbook
// https://github.com/Xiexe/Unity-Lit-Shader-Templates/tree/refactor

// Reads triangle perimeters of a skinned mesh and encodes to a png texture.
// Triangles are read in order of their primitive IDs.
//
// Perimeter lengths are encoded as an RGBA (Color32) value corresponding to 
//  the android color value of the length times a specified resolution:
//  https://developer.android.com/reference/android/graphics/Color
//
// Triangles are stored in the texture from bottom left to top right, as such:
/* 
+---+----+----+----+
| 9 | 10 | 11 | 12 |
| 5 |  6 |  7 |  8 |
| 1 |  2 |  3 |  4 |
+---+----+----+----+
*/
// To access these values in shader, use the semantic SV_PrimitiveID in a geometry shader,
// 
/* 
void geom(... uint fragID : SV_PrimitiveID)
    float x = fmod((float)fragID,_POTTexSize);
    float y = floor((float)fragID/_POTTexSize);
    float2 xy = (float2(x,y) + 0.5) / _POTTexSize;
    float4 oc = tex2Dlod(_TriangleLengthBuffer, float4(xy, 0, 0));
    float length = (oc.r + oc.g*255.0 + oc.b*255.0*255.0 + oc.a*255.0*255.0*255.0)*255.0*lengthResolution;
*/


using System.IO;
using UnityEditor;
using UnityEngine;

namespace _3.Editor
{
	public class WriteEdgeLengthTexture : EditorWindow
	{
		private static readonly float lengthResolution = 0.000001f; // Needs to match shader declare
		public static SkinnedMeshRenderer skinnedMeshRenderer;
		public static string meshName;
		private static readonly int TriangleLengthBuffer = Shader.PropertyToID("_TriangleLengthBuffer");
		private static readonly int POTTexSize = Shader.PropertyToID("_POTTexSize");
		private static float l;

		[MenuItem("CONTEXT/SkinnedMeshRenderer/Bake Edge Length")]
		private static void Bake(MenuCommand p_command)
		{
			skinnedMeshRenderer = (SkinnedMeshRenderer) p_command.context;
			var mesh = skinnedMeshRenderer.sharedMesh;
			meshName = mesh.name;

			var verts = mesh.vertices;
			var triangles = mesh.GetTriangles(0);
			var triCount = triangles.Length / 3;

			var texSize = Mathf.NextPowerOfTwo((int) Mathf.Sqrt(triCount));

			var triangleLengthTexture = new Texture2D(texSize, texSize, TextureFormat.ARGB32, true)
			{
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Clamp
			};

			for (var x = 0; x < texSize; x++)
			for (var y = 0; y < texSize; y++)
			{
				var t = y * texSize + x;

				if (t * 3 + 2 > triangles.Length)
				{
					triangleLengthTexture.SetPixel(x, y, Color.black);
				}
				else
				{
					l = (verts[triangles[t * 3]] - verts[triangles[t * 3 + 1]]).magnitude +
					    (verts[triangles[t * 3 + 1]] - verts[triangles[t * 3 + 2]]).magnitude +
					    (verts[triangles[t * 3 + 2]] - verts[triangles[t * 3]]).magnitude;

					triangleLengthTexture.SetPixel(x, y, EncodeDistanceToColor(l, lengthResolution));
				}
			}

			//triangleLengthTexture.SetPixel(texSize, texSize, EncodeDistanceToColor((float)(texSize), 1)); // Optional - write texture size to top right pixel

			triangleLengthTexture.Apply();
			skinnedMeshRenderer.sharedMaterial.SetTexture(TriangleLengthBuffer, triangleLengthTexture);
			skinnedMeshRenderer.sharedMaterial.SetFloat(POTTexSize, texSize);

			var path = $"Assets/Scripts/3/Editor/GeneratedAssets/t_{meshName}_edgeLengths.png";

			SaveTextureToFile(triangleLengthTexture, path, texSize);
		}

		private static Color32 EncodeDistanceToColor(float p_distance, float p_resolution)
		{
			var aCol = (int) (p_distance / p_resolution);
			var r = (byte) (aCol & 0xFF);
			var g = (byte) ((aCol >> 8) & 0xFF);
			var b = (byte) ((aCol >> 16) & 0xFF);
			var a = (byte) ((aCol >> 24) & 0xFF);

			var outColor = new Color32(r, g, b, a);

			Debug.Log(string.Format("Tri Edge: {0} Color: {1}", p_distance, outColor));

			return outColor;
		}

		private static void SaveTextureToFile(Texture2D p_texture, string p_filename, int p_size)
		{
			File.WriteAllBytes(p_filename, p_texture.EncodeToPNG());
			AssetDatabase.Refresh();
			var source = (TextureImporter) AssetImporter.GetAtPath(p_filename);
			source.maxTextureSize = p_size;
			source.filterMode = FilterMode.Point;
			source.textureCompression = TextureImporterCompression.Uncompressed;
			source.sRGBTexture = false;
			source.mipmapEnabled = false;
			AssetDatabase.Refresh();
			var texture2D = (Texture2D) AssetDatabase.LoadAssetAtPath(p_filename, typeof(Texture2D));
			skinnedMeshRenderer.sharedMaterial.SetTexture(TriangleLengthBuffer, texture2D);
			AssetDatabase.Refresh();
		}
	}
}