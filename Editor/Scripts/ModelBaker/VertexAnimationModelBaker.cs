using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace TAO.VertexAnimation.Editor
{
	[CreateAssetMenu(fileName = "new ModelBaker", menuName = "TAO/VertexAnimation/ModelBaker", order = 400)]
	public class VertexAnimationModelBaker : ScriptableObject
	{
#if UNITY_EDITOR
		// Input.
		public GameObject model;

		/// <summary> If true each child mesh will be processed separated to its own output file with name of the mesh transform name. </summary>
		[SerializeField] bool _batchMode;
		/// <summary> Child meshes inside this collection will not be processed. </summary>
		[SerializeField] List<string> _ignoredMeshs;

		[InlineEditor] public AnimationClipCollection animationClips;
		[Range(1, 60)]
		public int fps = 12;
		public int textureWidth = 512;
		public bool applyRootMotion = false;
		public bool includeInactive = false;

		public LODSettings lodSettings = new LODSettings();
		public bool applyAnimationBounds = true;
		public bool generateAnimationBook = true;
		public bool generatePrefab = true;
		public Shader materialShader = null;
		public bool useInterpolation = true;
		public bool useNormalA = true;

		// Output.
		public List<BakedModel> bakedModels = new List<BakedModel>();

		[System.Serializable]
		public class LODSettings
		{
			public LODSetting[] lodSettings = new LODSetting[3] { new LODSetting(1, .4f), new LODSetting(.6f, .15f), new LODSetting(.3f, .01f) };

			public float[] GetQualitySettings()
			{
				float[] q = new float[lodSettings.Length];
				for (int i = 0; i < lodSettings.Length; i++) q[i] = lodSettings[i].quality;
				return q;
			}

			public float[] GetTransitionSettings()
			{
				float[] t = new float[lodSettings.Length];
				for (int i = 0; i < lodSettings.Length; i++) t[i] = lodSettings[i].screenRelativeTransitionHeight;
				return t;
			}

			public int LODCount() => lodSettings.Length;
		}

		[System.Serializable]
		public struct LODSetting
		{
			[Range(1.0f, 0.0f)] public float quality;
			[Range(1.0f, 0.0f)] public float screenRelativeTransitionHeight;

			public LODSetting(float q, float t)
			{
				quality = q;
				screenRelativeTransitionHeight = t;
			}
		}

		private void OnValidate()
		{
			if (materialShader == null)
				materialShader = Shader.Find("TAO/Lit");
		}

		// ── Helpers ──────────────────────────────────────────────

		private string GetOutputFolder()
		{
			string bakerPath = AssetDatabase.GetAssetPath(this);
			string dir = Path.GetDirectoryName(bakerPath).Replace('\\', '/');
			return $"{dir}";
		}

		private void EnsureOutputFolder()
		{
			string folder = GetOutputFolder();
			if (AssetDatabase.IsValidFolder(folder)) return;
			string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
			string folderName = Path.GetFileName(folder);
			AssetDatabase.CreateFolder(parent, folderName);
		}

		// ── Bake ─────────────────────────────────────────────────

		public void Bake()
		{
			Debug.Log($"{name}.VertexAnimationModelBaker.Bake()");

			if (_batchMode)
			{
				var smrs = model.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);
				foreach (var smr in smrs)
				{
					if (_ignoredMeshs != null && _ignoredMeshs.Contains(smr.name)) continue;
					BakeSingle($"{name}_MultipleBakes/{smr.name}", smr.name);
				}
			}
			else
			{
				BakeSingle($"{name}_SingleBake", null);
			}
		}

		private void BakeSingle(string outputName, string onlyMeshName)
		{
			
			Material sourceMaterial = null;
			if (onlyMeshName != null)
			{
				foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive))
				{
					if (smr.name == onlyMeshName) { sourceMaterial = smr.sharedMaterial; break; }
				}
			}

			var target = Instantiate(model);
			target.name = model.name;

			RemoveIgnoredMeshes(target);
			if (onlyMeshName != null) RemoveAllMeshesExcept(target, onlyMeshName);

			target.ConbineAndConvertGameObject(includeInactive);
			AnimationBaker.BakedData bakedData = target.Bake(animationClips.Clips, applyRootMotion, fps, textureWidth);
			DestroyImmediate(target);

			SaveAssets(bakedData, outputName, sourceMaterial);
		}

		private void RemoveIgnoredMeshes(GameObject target)
		{
			if (_ignoredMeshs == null || _ignoredMeshs.Count == 0) return;
			foreach (var smr in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				if (_ignoredMeshs.Contains(smr.name)) DestroyImmediate(smr);
			}
		}

		private void RemoveAllMeshesExcept(GameObject target, string keepName)
		{
			foreach (var smr in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				if (smr.name != keepName) DestroyImmediate(smr);
			}
		}

		// ── Save ─────────────────────────────────────────────────

		private void SaveAssets(AnimationBaker.BakedData bakedData, string outputName, Material sourceMaterial = null)
		{
			EnsureOutputFolder();
			string folder = GetOutputFolder();
			string modelPath = $"{folder}/{outputName}.asset";
			string modelDir = Path.GetDirectoryName(modelPath).Replace('\\', '/');
			if (!AssetDatabase.IsValidFolder(modelDir))
			{
				string parent = Path.GetDirectoryName(modelDir).Replace('\\', '/');
				AssetDatabase.CreateFolder(parent, Path.GetFileName(modelDir));
			}

			// Load or create the BakedModel asset.
			var bakedModel = AssetDatabase.LoadAssetAtPath<BakedModel>(modelPath);
			if (bakedModel == null)
			{
				bakedModel = CreateInstance<BakedModel>();
				AssetDatabase.CreateAsset(bakedModel, modelPath);
			}

			// Clear old sub-assets.
			foreach (var a in AssetDatabase.LoadAllAssetsAtPath(modelPath))
			{
				if (a != bakedModel) AssetDatabase.RemoveObjectFromAsset(a);
			}

			var rawName = outputName.Split('/')[^1];

			// Position map.
			bakedModel.positionMap = Texture2DArrayUtils.CreateTextureArray(
				bakedData.positionMaps.ToArray(), false, true,
				TextureWrapMode.Repeat, FilterMode.Point, 1,
				$"{rawName}_PositionMap", true);
			AssetDatabase.AddObjectToAsset(bakedModel.positionMap, bakedModel);

			// Meshes.
			Bounds bounds = new() { max = bakedData.maxBounds, min = bakedData.minBounds };
			bakedModel.meshes = bakedData.mesh.GenerateLOD(lodSettings.LODCount(), lodSettings.GetQualitySettings());
			for (int i = 0; i < bakedModel.meshes.Length; i++)
			{
				if (applyAnimationBounds) bakedModel.meshes[i].bounds = bounds;
				bakedModel.meshes[i].Finalize();
				var s = new SerializedObject(bakedModel.meshes[i]);
				s.FindProperty("m_IsReadable").boolValue = true;
				AssetDatabase.AddObjectToAsset(bakedModel.meshes[i], bakedModel);
			}

			AssetDatabase.SaveAssets();

			if (generatePrefab) GeneratePrefab(bakedData, bakedModel, outputName, folder, sourceMaterial);
			if (generateAnimationBook) GenerateBook(bakedData, bakedModel, outputName);

			if (!bakedModels.Contains(bakedModel)) bakedModels.Add(bakedModel);

			EditorUtility.SetDirty(bakedModel);
			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private void GeneratePrefab(AnimationBaker.BakedData bakedData, BakedModel bakedModel, string outputName, string folder, Material sourceMaterial)
		{
			string path = $"{folder}/{outputName}.prefab";
			NamingConventionUtils.PositionMapInfo info = bakedData.GetPositionMap.name.GetTextureInfo();

			var matName = outputName.Split('/')[^1] + "_Material";
			if (bakedModel.material == null)
			{
				bakedModel.material = AnimationMaterial.Create(matName, materialShader, bakedModel.positionMap, useNormalA, useInterpolation, info.maxFrames);
				AssetDatabase.AddObjectToAsset(bakedModel.material, bakedModel);
			}
			else
			{
				bakedModel.material.Update(matName, materialShader, bakedModel.positionMap, useNormalA, useInterpolation, info.maxFrames);
			}

			if (sourceMaterial != null)
			{
				bakedModel.material.SetTexture( "_BaseMap", sourceMaterial.GetTexture( "_BaseMap" ) );
				bakedModel.material.SetColor( "_BaseColor", sourceMaterial.GetColor( "_BaseColor" ) );
				bakedModel.material.SetTexture( "_SpecGlossMap", sourceMaterial.GetTexture( "_SpecGlossMap" ) );
				bakedModel.material.SetColor( "_SpecColor", sourceMaterial.GetColor( "_SpecColor" ) );
				if (sourceMaterial.IsKeywordEnabled( "_EMISSION" ))
				{
					bakedModel.material.EnableKeyword( "_EMISSION" );
					bakedModel.material.SetTexture( "_EmissionMap", sourceMaterial.GetTexture( "_EmissionMap" ) );
					bakedModel.material.SetColor( "_EmissionColor", sourceMaterial.GetColor( "_EmissionColor" ) );
				}
			}

			bakedModel.prefab = AnimationPrefab.Create(path, outputName, bakedModel.meshes, bakedModel.material, lodSettings.GetTransitionSettings());
		}

		private void GenerateBook(AnimationBaker.BakedData bakedData, BakedModel bakedModel, string outputName)
		{
			string bookPath = AssetDatabase.GetAssetPath(this).Replace(".asset", "_Book.asset");
			string bookDir = Path.GetDirectoryName(bookPath).Replace('\\', '/');
			if (!AssetDatabase.IsValidFolder(bookDir))
			{
				string parent = Path.GetDirectoryName(bookDir).Replace('\\', '/');
				AssetDatabase.CreateFolder(parent, Path.GetFileName(bookDir));
			}

			var book = AssetDatabase.LoadAssetAtPath<AnimationBook>(bookPath);
			if (book == null)
			{
				book = CreateInstance<AnimationBook>();
				AssetDatabase.CreateAsset(book, bookPath);
			}

			// Clear old animation sub-assets.
			foreach (var a in AssetDatabase.LoadAllAssetsAtPath(bookPath))
			{
				if (a != book) AssetDatabase.RemoveObjectFromAsset(a);
			}

			bakedModel.book = book;
			book.animations = new List<Animation>();

			List<NamingConventionUtils.PositionMapInfo> info = new();
			foreach (var t in bakedData.positionMaps) info.Add(t.name.GetTextureInfo());
			
			var mat = bakedModel.material;
			if (mat.HasProperty("_MaxFrames") && info.Count > 0) mat.SetFloat("_MaxFrames", info[0].maxFrames);
			if (mat.HasProperty("_PositionMap")) mat.SetTexture("_PositionMap", bakedModel.positionMap);

			for (int i = 0; i < info.Count; i++)
			{
				string animationName = $"{outputName}_{info[i].name}";
				VA_AnimationData newData = new(animationName, info[i].frames, info[i].maxFrames, info[i].fps, i, -1);

				var animation = CreateInstance<Animation>();
				animation.name = animationName;
				AssetDatabase.AddObjectToAsset(animation, book);
				animation.SetData(newData);
				book.TryAddAnimation(animation);
			}

			EditorUtility.SetDirty(book);
		}

		private static bool TryGetAnimationFromBook(AnimationBook book, string animationName, out Animation animation)
		{
			foreach (var a in book.animations)
			{
				if (a != null && a.name == animationName) { animation = a; return true; }
			}
			animation = null;
			return false;
		}

		// ── Cleanup ───────────────────────────────────────────────

		public void DeleteSavedAssets()
		{
			string folder = GetOutputFolder();
			if (AssetDatabase.IsValidFolder(folder))
				AssetDatabase.DeleteAsset(folder);

			bakedModels.Clear();

			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		public void DeleteUnusedAnimations()
		{
			foreach (var bm in bakedModels)
			{
				if (bm == null || bm.book == null) continue;

				var bookPath = AssetDatabase.GetAssetPath(bm.book);
				var allAnims = AssetDatabase.LoadAllAssetsAtPath(bookPath);
				foreach (var a in allAnims)
				{
					if (a is Animation anim && !bm.book.animations.Contains(anim))
						AssetDatabase.RemoveObjectFromAsset(anim);
				}
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
#endif
	}
}
