using UnityEngine;

namespace TAO.VertexAnimation
{
	public static class MeshLodGenerator
	{
		public static Mesh[] GenerateLOD(this Mesh mesh, int lods, float[] quality)
		{
			Debug.Log( $"MeshLodGenerator.GenerateLOD( {mesh?.vertexCount.ToString() ?? "NULL" }[{lods}] )" );

			Mesh[] lodMeshes = new Mesh[lods];
			var meshName = mesh.name;

			var tris = mesh.triangles;
			var verts = mesh.vertices;

			var trisCount = tris.Length;
			for( int i = 1; i < trisCount; i++ )
			{
				var vId = tris[i];
				var oId = tris[i-1];
				Debug.DrawLine( verts[vId], verts[oId], Color.red, 20 );
			}

			Debug.Log( $"tris[{tris.Length}]: {{ {string.Join( ", ", tris )} }}" );
			Debug.Log( $"verts[{verts.Length}]: {{ {string.Join( ", ", verts )} }}" );

			var debug = new (Vector3[],int[])[lodMeshes.Length];
			
			for( int i = 0; i < lodMeshes.Length; i++ )
			{
				var lodMesh = mesh.Copy();
				// Only simplify when needed.

				var modelQuality = quality[i];
				if (modelQuality - 1.0f < -float.Epsilon)
				{
					lodMesh = lodMesh.Simplify(modelQuality);
				}

				Debug.Log( $"MeshLodGenerator.GenerateLOD[{i}]( v:{lodMesh.vertexCount}, t:{lodMesh.triangles.Length} )" );

				lodMesh.name = $"{meshName}_LOD{i}";
				lodMeshes[i] = lodMesh;
				debug[i] = ( (Vector3[]) lodMesh.vertices.Clone(), (int[])lodMesh.triangles.Clone() );
			}

			MeshBakerDebugger.lastBake = debug;

			return lodMeshes;
		}

		public static Mesh[] GenerateLOD(this Mesh mesh, int lods, AnimationCurve qualityCurve)
		{
			float[] quality = new float[lods];

			for (int q = 0; q < quality.Length; q++)
			{
				quality[q] = qualityCurve.Evaluate(1f / quality.Length * q);
			}

			return GenerateLOD(mesh, lods, quality);
		}
	}
}