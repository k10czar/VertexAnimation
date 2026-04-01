using UnityEngine;

namespace TAO.VertexAnimation.Editor
{
	[CreateAssetMenu(fileName = nameof(AnimationClipCollection), menuName = "TAO/VertexAnimation/AnimationClipCollection", order = 400)]
    public class AnimationClipCollection : ScriptableObject
	{
		[SerializeField] private AnimationClip[] animationClips;

		public AnimationClip[] Clips => animationClips;
	}
}
