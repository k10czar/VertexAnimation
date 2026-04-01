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
	}
}
