using UnityEngine;

namespace TAO.VertexAnimation
{
	public class MeshBakerDebugger : MonoBehaviour
	{
		[SerializeField] Vector3 _offset = Vector3.right;
		[SerializeField] float _nodeSize = .05f;
		[SerializeField] float _scale = 1;

		[SerializeField] Vector3 _referenceOffset = Vector3.forward;
		[SerializeField] float _referenceScale = 1;
		[SerializeField] Mesh[] _referenceMeshs = new Mesh[]{ };

		public static (Vector3[],int[])[] lastBake;

		void OnDrawGizmos()
		// void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawRay( transform.position, Vector3.up * 2 );
            DrawWireFrameMeshArray( transform.position, lastBake, _scale, Color.yellow, Color.blue );
		}

        void DrawWireFrameMeshArray( Vector3 origin, (Vector3[],int[])[] meshesArray, float scale, Color edgeColor, Color verticesColor )
        {
			if( meshesArray == null ) return;
			var bakedMeshesCount = meshesArray.Length;
			if( bakedMeshesCount == 0 ) return;

			for( int i = 0; i < bakedMeshesCount; i++ )
			{
				var meshData = meshesArray[i];
                DrawWireFrameMesh( origin + ( _offset * i ), meshData.Item1, meshData.Item2, scale, edgeColor, verticesColor );
			}
        }

        void DrawWireFrameMesh( Vector3 pos, Vector3[] verts, int[] tris, float scale, Color edgeColor, Color verticesColor )
        {
            if( tris == null ) return;
            if( verts == null ) return;

            var trisCount = tris.Length;

            Gizmos.color = edgeColor;
            for( int t = 2; t < trisCount; t += 3 )
            {
                var p0 = pos + verts[tris[t-2]] * scale;
                var p1 = pos + verts[tris[t-1]] * scale;
                var p2 = pos + verts[tris[t]] * scale;
                Gizmos.DrawLine( p0, p1 );
                Gizmos.DrawLine( p1, p2 );
                Gizmos.DrawLine( p2, p0 );
            }

            var vertsCount = verts.Length;
            Gizmos.color = verticesColor;
            for( int v = 0; v < vertsCount; v++ )
            {
                Gizmos.DrawSphere( pos + verts[v] * scale, _nodeSize );
            }
        }

		void OnDrawGizmosSelected()
		{
            if( _referenceMeshs == null ) return;
            var count = _referenceMeshs.Length;
            if( count == 0 ) return;
            
			var debug = new (Vector3[],int[])[_referenceMeshs.Length];
			for( int i = 0; i < _referenceMeshs.Length; i++ )
            {
                var lodMesh = _referenceMeshs[i];
                // lodMesh.
				debug[i] = ( lodMesh.vertices, lodMesh.triangles );
            }
            
            DrawWireFrameMeshArray( transform.position + _referenceOffset, debug, _referenceScale, Color.green, Color.red );
		}
	}
}