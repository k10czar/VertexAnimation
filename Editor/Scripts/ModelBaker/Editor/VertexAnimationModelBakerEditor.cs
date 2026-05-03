using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TAO.VertexAnimation.Editor
{
	[CustomEditor(typeof(VertexAnimationModelBaker))]
	public class VertexAnimationModelBakerEditor : UnityEditor.Editor
	{
		private VertexAnimationModelBaker modelBaker = null;
		private bool _meshsFoldout = false;
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
		SerializedProperty _materialBaseMapOverrideProp;
		SerializedProperty _colorOverrideProp;
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
			_materialBaseMapOverrideProp = serializedObject.FindProperty("_materialBaseMapOverride");
			_colorOverrideProp = serializedObject.FindProperty("_colorOverride");
			_useNormalAProp = serializedObject.FindProperty("useNormalA");
			_useInterpolationProp = serializedObject.FindProperty("useInterpolation");
			
			_ignoredSet.Clear();
			for (int i = 0; i < _ignoredMeshsProp.arraySize; i++)
				_ignoredSet.Add(_ignoredMeshsProp.GetArrayElementAtIndex(i).stringValue);
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

		HashSet<string> _ignoredSet = new();
		HashSet<string> _ignoredSetFlipper = new();

		private void IgnoredMeshesGUI()
		{
			if (modelBaker.model == null) return;

			var smrs = modelBaker.model.GetComponentsInChildren<SkinnedMeshRenderer>(_includeInactiveProp.boolValue);
			if (smrs.Length == 0) return;

			int selected = smrs.Length - _ignoredMeshsProp.arraySize;
			_meshsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_meshsFoldout, $"Meshes {selected}/{smrs.Length}");
			if (_meshsFoldout)
			{
				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						if (GUILayout.Button("All", EditorStyles.miniButtonLeft))
						{
							_ignoredMeshsProp.ClearArray();
							_ignoredSet.Clear();
						}
						if (GUILayout.Button("None", EditorStyles.miniButtonMid))
						{
							_ignoredMeshsProp.ClearArray();
							for (int i = 0; i < smrs.Length; i++)
							{
								_ignoredMeshsProp.InsertArrayElementAtIndex(i);
								var smr = smrs[i];
								_ignoredMeshsProp.GetArrayElementAtIndex(i).stringValue = smr.name;
								_ignoredSet.Add( smr.name );
							}
						}
						if (GUILayout.Button("Flip", EditorStyles.miniButtonRight))
						{
							_ignoredSetFlipper.Clear();
							foreach (var smr in smrs)
							{
								if (_ignoredSet.Contains(smr.name)) 
								{
									RemoveFromList(_ignoredMeshsProp, smr.name);
								}
								else 
								{ 
									int idx = _ignoredMeshsProp.arraySize; 
									_ignoredMeshsProp.InsertArrayElementAtIndex(idx); 
									_ignoredMeshsProp.GetArrayElementAtIndex(idx).stringValue = smr.name; 
									_ignoredSetFlipper.Add( smr.name );
								}
							}
							_ignoredSet.Clear();
							var oldRef = _ignoredSet;
							_ignoredSet = _ignoredSetFlipper;
							_ignoredSetFlipper = oldRef;
						}
					}

					_ignoredMeshsScroll = EditorGUILayout.BeginScrollView(_ignoredMeshsScroll, GUILayout.MaxHeight(MESHES_MAX_HEIGHT));
					EditorGUI.indentLevel++;
					foreach (var smr in smrs)
					{
						bool isIgnored = _ignoredSet.Contains(smr.name);
						bool include = EditorGUILayout.ToggleLeft(smr.name, !isIgnored);

						if (!include && !isIgnored)
						{
							int idx = _ignoredMeshsProp.arraySize;
							_ignoredMeshsProp.InsertArrayElementAtIndex(idx);
							_ignoredMeshsProp.GetArrayElementAtIndex(idx).stringValue = smr.name;
							_ignoredSet.Add( smr.name );
						}
						else if (include && isIgnored)
						{
							RemoveFromList(_ignoredMeshsProp, smr.name);
							_ignoredSet.Remove( smr.name );
						}
					}
					EditorGUI.indentLevel--;
					EditorGUILayout.EndScrollView();
				}
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
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
			EditorGUILayout.PropertyField(_materialBaseMapOverrideProp, new GUIContent("Base Map Override"));
			EditorGUILayout.PropertyField(_colorOverrideProp, new GUIContent("Color Override"));

			if (GUILayout.Button("Bake", GUILayout.Height(32)))
			{
				modelBaker.Bake();
			}

			// using (new EditorGUILayout.HorizontalScope())
			// {
			// 	if (GUILayout.Button("Clear Cache", EditorStyles.miniButtonLeft))
			// 	{
			// 		// modelBaker.DeleteUnusedAnimations();
			// 	}
			// }
		}
	}
}
