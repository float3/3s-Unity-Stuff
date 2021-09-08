using System.IO;
using UnityEditor;
using UnityEngine;

namespace _3.Editor
{
    public class WriteEdgeLengthTexture : EditorWindow
    {
        public static SkinnedMeshRenderer skinnedMeshRenderer;
        public static string meshName;
        private static readonly int TriangleLengthBuffer = Shader.PropertyToID("_TriangleLengthBuffer");
        private static readonly int TotalTriCount = Shader.PropertyToID("_TotalTriCount");

        [MenuItem("CONTEXT/SkinnedMeshRenderer/Bake Edge Length")]
        private static void Bake(MenuCommand p_command)
        {
            skinnedMeshRenderer = (SkinnedMeshRenderer) p_command.context;
            var mesh = skinnedMeshRenderer.sharedMesh;
            meshName = mesh.name;

            var verts = mesh.vertices;
            var triangles = mesh.GetTriangles(0);
            var triCount = triangles.Length / 3;
            var triangleLengthTexture = new Texture2D(triCount, 1, TextureFormat.ARGB32, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var texSize = Mathf.NextPowerOfTwo((int) Mathf.Sqrt(triCount));

            for (var x = 0; x < texSize; x++)
            for (var y = 0; y < texSize; y++)
            {
                var l =
                    (verts[triangles[x * 3]] - verts[triangles[x * 3 + 1]]).magnitude
                    + (verts[triangles[x * 3 + 1]] - verts[triangles[x * 3 + 2]]).magnitude
                    + (verts[triangles[x * 3 + 2]] - verts[triangles[x * 3]]).magnitude;
                triangleLengthTexture.SetPixel(x, y, Color.white * l);
                if ((y + 1) * texSize + x > triCount) triangleLengthTexture.SetPixel(x, y, Color.black);
            }

            triangleLengthTexture.Apply();
            skinnedMeshRenderer.material.SetTexture(TriangleLengthBuffer, triangleLengthTexture);
            skinnedMeshRenderer.material.SetFloat(TotalTriCount, triCount - 1);


            var path = $"Assets/Scripts/3/Editor/GeneratedAssets/t_{meshName}_edgeLengths.png";

            SaveTextureToFile(triangleLengthTexture, path, texSize);
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
            skinnedMeshRenderer.material.SetTexture(TriangleLengthBuffer, texture2D);
            AssetDatabase.Refresh();
        }
    }
}