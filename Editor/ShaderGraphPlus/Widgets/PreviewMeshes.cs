namespace ShaderGraphPlus;

/// <summary>
/// Preview meshes shared by the Qt preview and the panel preview, so both show the same sphere.
/// </summary>
public static class PreviewMeshes
{
	/// <summary>
	/// The tessellated, UV-mapped sphere the preview shows by default in the Qt editor.
	/// </summary>
	public static Model CreateSphereModel()
	{
		return Model.Builder
			.AddMesh( CreateTessellatedSphere( 64, 64, 4.0f, 4.0f, 32.0f ) )
			.Create();
	}

	// We could do with a nice geometry API but this is tools code so fuck it!
	public static Mesh CreateTessellatedSphere( int uFacets, int vFacets, float maxU, float maxV, float radius )
	{
		float dU = 1.0f / uFacets;
		float dV = 1.0f / vFacets;

		var material = Material.Load( "materials/core/shader_editor.vmat" );
		var mesh = new Mesh( material );
		mesh.CreateVertexBuffer<Vertex>( (uFacets + 1) * (vFacets + 1) );
		mesh.CreateIndexBuffer( 2 * 3 * uFacets * vFacets );
		mesh.Bounds = BBox.FromPositionAndSize( 0, radius * 2 );

		mesh.LockVertexBuffer<Vertex>( ( vertices ) =>
		{
			float v = 0.5f;
			int i = 0;

			for ( int nV = 0; nV < (vFacets + 1); nV++ )
			{
				float u = 0.0f;

				for ( int nU = 0; nU < (uFacets + 1); nU++ )
				{
					float sinTheta = MathF.Sin( u * MathF.PI );
					float cosTheta = MathF.Cos( u * MathF.PI );
					float sinPhi = MathF.Sin( v * 2.0f * MathF.PI );
					float cosPhi = MathF.Cos( v * 2.0f * MathF.PI );

					var vertex = new Vertex();
					vertex.Position = radius * new Vector3( sinTheta * cosPhi, sinTheta * sinPhi, cosTheta );
					vertex.Normal = new Vector3( sinTheta * cosPhi, sinTheta * sinPhi, cosTheta ).Normal;
					vertex.Tangent = new Vector4( new Vector3( -sinPhi, cosPhi, 0.0f ).Normal, -1.0f );
					vertex.TexCoord0 = new Vector2( (v - 0.5f) * maxV, u * maxU );
					vertex.TexCoord1 = vertex.TexCoord0 * -1.0f;
					vertex.Color = Color.Lerp( Color.Red, Color.Green, (vertex.Position.z + radius) / (2 * radius) );

					vertices[i++] = vertex;

					u += dU;
				}

				v += dV;
			}
		} );

		mesh.LockIndexBuffer( ( indices ) =>
		{
			int i = 0;

			for ( int v = 0; v < vFacets; v++ )
			{
				for ( int u = 0; u < uFacets; u++ )
				{
					indices[i++] = v * (uFacets + 1) + u;
					indices[i++] = v * (uFacets + 1) + (u + 1);
					indices[i++] = (v + 1) * (uFacets + 1) + u;
					indices[i++] = v * (uFacets + 1) + (u + 1);
					indices[i++] = (v + 1) * (uFacets + 1) + (u + 1);
					indices[i++] = (v + 1) * (uFacets + 1) + u;
				}
			}
		} );

		return mesh;
	}
}
