using Unity.Collections;
using UnityEngine;

namespace TAO.VertexAnimation
{
    /// <summary>
    /// MonoBehaviour that test animations inside the book and feed material with animation data
    /// </summary>
    public class TestAnimationBook : MonoBehaviour
	{
		public AnimationBook _book;
		public bool _cycleThroughAllAnimations;
		public int _animIndex;
		[BlockEdit] public float _animRuntimeData;
		[BlockEdit] public int _lastAnimIndex = -1;

		int _propDataHash = Shader.PropertyToID( "_AnimationDataOne" );
		Material _material;

		void Awake()
		{
			var r = GetComponentInChildren<Renderer>( true );
			if( r != null ) _material = r.sharedMaterial;
		}

		void Update()
		{
			if( _book == null || _book.animations == null || _book.animations.Count == 0 ) return;
			if( _material == null ) return;

			_animIndex = Mathf.Clamp( _animIndex, 0, _book.animations.Count - 1 );

			if( _animIndex != _lastAnimIndex )
			{
				_animRuntimeData = 0f;
				_lastAnimIndex = _animIndex;
			}

			var animData = _book.animations[_animIndex].GetData();

			_animRuntimeData += Time.deltaTime * animData.frameTime;
			if( _animRuntimeData > animData.duration )
			{
				_animRuntimeData -= animData.duration;
				if( _cycleThroughAllAnimations )
					_animIndex = ( _animIndex + 1 ) % _book.animations.Count;
			}

			float nextTime = _animRuntimeData + ( 1f / animData.maxFrames );
			_material.SetVector( _propDataHash, new Vector4( _animRuntimeData, animData.animationMapIndex, nextTime, animData.animationMapIndex ) );
		}
	}

}
