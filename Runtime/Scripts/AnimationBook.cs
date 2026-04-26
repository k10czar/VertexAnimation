using System.Collections.Generic;
using UnityEngine;

namespace TAO.VertexAnimation
{
	[CreateAssetMenu(fileName = "new AnimationBook", menuName = "TAO/VertexAnimation/AnimationBook", order = 400)]
	public class AnimationBook : ScriptableObject
	{
		public int MaxFrames { get; private set; } = 0;

		public List<Animation> animations = new List<Animation>();

		public bool TryAddAnimation(Animation animation)
		{
			if (animations != null && animations.Count != 0)
			{
				if (!animations.Contains(animation))
				{
					animations.Add(animation);
					OnValidate();
					return true;
				}
			}
			else
			{
				// Add first animation.
				animations.Add(animation);
				// Set maxFrames for this animation book.
				OnValidate();

				return true;
			}

			return false;
		}

		private void UpdateMaxFrames()
		{
			if (animations != null && animations.Count != 0)
			{
				if (animations[0] != null)
				{
					MaxFrames = animations[0].Data.maxFrames;
				}
			}
		}

		private void OnValidate()
		{
			UpdateMaxFrames();

			if (animations != null)
			{
				foreach (var a in animations)
				{
					if (a != null)
					{
						if (a.Data.maxFrames != MaxFrames)
						{
							Debug.LogWarning(string.Format("{0} in {1} doesn't match maxFrames!", a.name, this.name));
						}
					}
				}
			}
		}
	}
}