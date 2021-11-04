//referenced from https://github.com/lukis101/VRCUnityStuffs/blob/master/Scripts/Editor/MaterialCleaner.cs
#if UNITY_EDITOR

#region

using UnityEditor;
using UnityEngine;

#endregion

// ReSharper disable once CheckNamespace
namespace _3.MaterialMigrator
{
	public class MaterialMigrator : EditorWindow
	{
		private Material _selectedMaterial;
		private SerializedObject _serializedObject;
		private Shader _yourShader;

		private void OnEnable()
		{
			GetSelectedMaterial();
		}

		private void OnGUI()
		{
			EditorGUIUtility.labelWidth = 200f;

			_yourShader = (Shader)EditorGUILayout.ObjectField("Shader", _yourShader, typeof(Shader), false);


			if (_selectedMaterial == null)
			{
				EditorGUILayout.LabelField("No material selected");
			}
			else
			{
				_serializedObject.Update();


				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Selected material:", _selectedMaterial.name);
				if (GUILayout.Button("Migrate mats"))
					MigrateMultiple(_yourShader);
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

		private  void GetSelectedMaterial()
		{
			_selectedMaterial = Selection.activeObject as Material;
			if (_selectedMaterial != null) _serializedObject = new SerializedObject(_selectedMaterial);

			Repaint();
		}

		private static void MigrateMultiple(Shader pShader)
		{
			foreach (Object obj in Selection.objects)
			{
				Material mat = obj as Material;
				if (mat != null)
					MigrateMaterial(mat, pShader);
				//Debug.Log("debug");
			}
		}

		private static void MigrateMaterial(Material pMaterial, Shader pShader)
		{
			int storedQueue = pMaterial.renderQueue;
			pMaterial.shader = pShader;
			pMaterial.renderQueue = storedQueue;
		}
	}
}
#endif