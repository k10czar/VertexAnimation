using UnityEngine;

namespace TAO.VertexAnimation
{
	public class BakedModel : ScriptableObject
	{
		public GameObject prefab;
		public Texture2DArray positionMap;
		public Material material;
		public Mesh[] meshes;
		public AnimationBook book;

		public bool IsValid => material != null && meshes != null && meshes.Length > 0 && meshes[0] != null;

		public int AnimationCount => book != null ? book.animations.Count : 0;
    	public VA_AnimationData GetAnimData( int index ) => book.animations[index].GetData();
    }
}
