using UnityEngine;
using UnityEditor;

namespace TAO.VertexAnimation.Editor
{
	[CustomEditor(typeof(VertexAnimationModelBaker))]
	public class VertexAnimationModelBakerEditor : UnityEditor.Editor
	{
		private VertexAnimationModelBaker modelBaker = null;
		private bool _ignoredMeshsFoldout = false;
		private Vector2 _ignoredMeshsScroll = Vector2.zero;
		private const float MESHES_MAX_HEIGHT = 240f;

		SerializedProperty _modelProp;
		SerializedProperty _batchModeProp;
		SerializedProperty _ignoredMeshsProp;
		SerializedProperty _animationClipsProp;
		SerializedProperty _fpsProp;
		SerializedProperty _textureWidthProp;
		SerializedProperty _applyRootMotionProp;
		SerializedProperty _includeInactiveProp;
		SerializedProperty _lodSettingsProp;
		SerializedProperty _applyAnimationBoundsProp;
		SerializedProperty _generateAnimationBookProp;
		SerializedProperty _generatePrefabProp;
		SerializedProperty _materialShaderProp;
		SerializedProperty _useNormalAProp;
		SerializedProperty _useInterpolationProp;

		void OnEnable()
		{
			modelBaker = target as VertexAnimationModelBaker;

			_modelProp = serializedObject.FindProperty("model");
			_batchModeProp = serializedObject.FindProperty("_batchMode");
			_ignoredMeshsProp = serializedObject.FindProperty("_ignoredMeshs");
			_animationClipsProp = serializedObject.FindProperty("animationClips");
			_fpsProp = serializedObject.FindProperty("fps");
			_textureWidthProp = serializedObject.FindProperty("textureWidth");
			_applyRootMotionProp = serializedObject.FindProperty("applyRootMotion");
			_includeInactiveProp = serializedObject.FindProperty("includeInactive");
			_lodSettingsProp = serializedObject.FindProperty("lodSettings").FindPropertyRelative("lodSettings");
			_applyAnimationBoundsProp = serializedObject.FindProperty("applyAnimationBounds");
			_generateAnimationBookProp = serializedObject.FindProperty("generateAnimationBook");
			_generatePrefabProp = serializedObject.FindProperty("generatePrefab");
			_materialShaderProp = serializedObject.FindProperty("materialShader");
			_useNormalAProp = serializedObject.FindProperty("useNormalA");
			_useInterpolationProp = serializedObject.FindProperty("useInterpolation");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			InputGUI();
			EditorGUILayoutUtils.HorizontalLine(color: Color.gray);
			BakeGUI();

			serializedObject.ApplyModifiedProperties();
		}

		private void InputGUI()
		{
			GUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(_modelProp);
			BatchModeGUI( GUILayout.Width(100) );
			GUILayout.EndHorizontal();
			IgnoredMeshesGUI();

			EditorGUILayout.PropertyField(_animationClipsProp);
			EditorGUILayout.PropertyField(_fpsProp);
			EditorGUILayout.PropertyField(_textureWidthProp);
			EditorGUILayout.PropertyField(_applyRootMotionProp);
			EditorGUILayout.PropertyField(_includeInactiveProp);
		}

		private void BatchModeGUI( params GUILayoutOption[] options )
		{
			string label = _batchModeProp.boolValue ? "Multiple Output" : "Single Output";
			if (GUILayout.Button(label, options))
				_batchModeProp.boolValue = !_batchModeProp.boolValue;
		}

		private void IgnoredMeshesGUI()
		{
			if (modelBaker.model == null) return;

			var smrs = modelBaker.model.GetComponentsInChildren<SkinnedMeshRenderer>(_includeInactiveProp.boolValue);
			if (smrs.Length == 0) return;

			int selected = smrs.Length - _ignoredMeshsProp.arraySize;
			_ignoredMeshsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_ignoredMeshsFoldout, $"Meshes {selected}/{smrs.Length}");
			if (_ignoredMeshsFoldout)
			{
				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						if (GUILayout.Button("All", EditorStyles.miniButtonLeft))
						{
							_ignoredMeshsProp.ClearArray();
						}
						if (GUILayout.Button("None", EditorStyles.miniButtonMid))
						{
							_ignoredMeshsProp.ClearArray();
							for (int i = 0; i < smrs.Length; i++)
							{
								_ignoredMeshsProp.InsertArrayElementAtIndex(i);
								_ignoredMeshsProp.GetArrayElementAtIndex(i).stringValue = smrs[i].name;
							}
						}
						if (GUILayout.Button("Flip", EditorStyles.miniButtonRight))
						{
							foreach (var smr in smrs)
							{
								if (ListContains(_ignoredMeshsProp, smr.name)) RemoveFromList(_ignoredMeshsProp, smr.name);
								else { int idx = _ignoredMeshsProp.arraySize; _ignoredMeshsProp.InsertArrayElementAtIndex(idx); _ignoredMeshsProp.GetArrayElementAtIndex(idx).stringValue = smr.name; }
							}
						}
					}

					_ignoredMeshsScroll = EditorGUILayout.BeginScrollView(_ignoredMeshsScroll, GUILayout.MaxHeight(MESHES_MAX_HEIGHT));
					EditorGUI.indentLevel++;
					foreach (var smr in smrs)
					{
						bool isIgnored = ListContains(_ignoredMeshsProp, smr.name);
						bool include = EditorGUILayout.ToggleLeft(smr.name, !isIgnored);

						if (!include && !isIgnored)
						{
							int idx = _ignoredMeshsProp.arraySize;
							_ignoredMeshsProp.InsertArrayElementAtIndex(idx);
							_ignoredMeshsProp.GetArrayElementAtIndex(idx).stringValue = smr.name;
						}
						else if (include && isIgnored)
						{
							RemoveFromList(_ignoredMeshsProp, smr.name);
						}
					}
					EditorGUI.indentLevel--;
					EditorGUILayout.EndScrollView();
				}
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		private static bool ListContains(SerializedProperty arrayProp, string value)
		{
			for (int i = 0; i < arrayProp.arraySize; i++)
			{
				if (arrayProp.GetArrayElementAtIndex(i).stringValue == value) return true;
			}
			return false;
		}

		private static void RemoveFromList(SerializedProperty arrayProp, string value)
		{
			for (int i = arrayProp.arraySize - 1; i >= 0; i--)
			{
				if (arrayProp.GetArrayElementAtIndex(i).stringValue == value)
				{
					arrayProp.DeleteArrayElementAtIndex(i);
					return;
				}
			}
		}

		private void BakeGUI()
		{
			EditorGUILayout.PropertyField(_lodSettingsProp);
			EditorGUILayout.PropertyField(_applyAnimationBoundsProp);
			EditorGUILayout.PropertyField(_generateAnimationBookProp);

			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.PropertyField(_generatePrefabProp);
				EditorGUILayout.PropertyField(_materialShaderProp, new GUIContent(""));
			}

			EditorGUILayout.PropertyField(_useNormalAProp, new GUIContent("Use Normal (A)"));
			EditorGUILayout.PropertyField(_useInterpolationProp);

			if (GUILayout.Button("Bake", GUILayout.Height(32)))
			{
				modelBaker.Bake();
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Delete Unused Animations", EditorStyles.miniButtonLeft))
				{
					if (EditorUtility.DisplayDialog("Delete Unused Animations", "Deleting assets will loose references within the project.", "Ok", "Cancel"))
					{
						modelBaker.DeleteUnusedAnimations();
					}
				}

				if (GUILayout.Button("Delete", EditorStyles.miniButtonRight))
				{
					if (EditorUtility.DisplayDialog("Delete Assets", "Deleting assets will loose references within the project.", "Ok", "Cancel"))
					{
						modelBaker.DeleteSavedAssets();
					}
				}
			}
		}
	}
}
