//referenced from https://github.com/lukis101/VRCUnityStuffs/blob/master/Scripts/Editor/MaterialCleaner.cs

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace _3.Editor
{
	public class MaterialMigrator : EditorWindow
	{
		private Material selectedMaterial;
		private SerializedObject serializedObject;
		private Shader yourShader;

		private void OnEnable()
		{
			GetSelectedMaterial();
		}

		private void OnGUI()
		{
			EditorGUIUtility.labelWidth = 200f;

			yourShader = (Shader) EditorGUILayout.ObjectField("Shader", yourShader, typeof(Shader), false);


			if (selectedMaterial == null)
			{
				EditorGUILayout.LabelField("No material selected");
			}
			else
			{
				serializedObject.Update();


				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Selected material:", selectedMaterial.name);
				if (GUILayout.Button("Migrate mats"))
					MigrateMultiple(yourShader);
			}


			EditorGUIUtility.labelWidth = 0;
		}

		private void OnProjectChange()
		{
			GetSelectedMaterial();
		}

		private void OnSelectionChange()
		{
			GetSelectedMaterial();
		}


		[MenuItem("Window/Material Migrator")]
		private static void Init()
		{
			GetWindow<MaterialMigrator>("Mat. Migrator");
		}

		[MenuItem("Tools/3/Migrate Materials to lit")]
		private void GetSelectedMaterial()
		{
			selectedMaterial = Selection.activeObject as Material;
			if (selectedMaterial != null) serializedObject = new SerializedObject(selectedMaterial);

			Repaint();
		}

		private static void MigrateMultiple(Shader p_shader)
		{
			foreach (Object obj in Selection.objects)
			{
				Material mat = obj as Material;
				if (mat != null)
					MigrateMaterial(mat, p_shader);
				//Debug.Log("debug");
			}
		}

		private static void MigrateMaterial(Material p_material, Shader p_shader)
		{
			int storedQueue = p_material.renderQueue;
			p_material.shader = p_shader;
			p_material.renderQueue = storedQueue;
		}
	}
}
#endif