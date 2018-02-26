using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace QuickEdit
{
	[System.Serializable]
	public class qe_Mesh : ScriptableObject, ISerializationCallbackReceiver
	{
		public GameObject gameObject;				// Reference to source gameObject.
		public Transform transform;					// Reference to source gameObject transform.
		public Mesh cloneMesh;						// A clone of the source mesh that we can edit.
		public Mesh originalMesh;					// Used to revert in the case that this is a procedural mesh.
		public qe_HandleRenderer handlesRenderer;	// 
		public ModelSource source;					// The original mesh.
		public string originalMeshGUID;				// The original mesh GUID.
		public Dictionary<int, int> triangleLookup = new Dictionary<int, int>();	// Shared triangle lookup table.  Keys are indices in triangles array, Value is index in sharedTriangles array.
		public List<List<int>> sharedTriangles = new List<List<int>>();
 
		// Getters
		public int vertexCount { get { return cloneMesh.vertexCount; } }
		public Vector3[] vertices { get { return cloneMesh.vertices; } set { cloneMesh.vertices = value; } }
		public Vector2[] uvs { get { return cloneMesh.uv; } }
		public Vector3[] normals { get { return cloneMesh.normals; } }
		public int[] indices { get { return cloneMesh.triangles; } }
		public int[] GetIndices(int submesh) { return cloneMesh.GetIndices(submesh); }
		public void SetIndices(int submesh, int[] tris) { cloneMesh.SetIndices(tris, cloneMesh.GetTopology(submesh), submesh); }

		[SerializeField] private qe_Triangle[] _faces;
		[SerializeField] private qe_Edge[] _edges;
		[SerializeField] private qe_Edge[] _userEdges;	// Same as _edges, but with no duplicates and guaranteed to point to first index sharedTriangle array.
		[SerializeField] private int _vertexCount;		// used to determine if the cache needs rebuilt
		[SerializeField] private int _triangleCount;	// used to determine if the cache needs rebuilt

		public qe_Triangle[] faces { get { return _faces; } }
		public qe_Edge[] edges { get { return _edges; } }
		public qe_Edge[] userEdges { get { return _userEdges; } }
		public int GetCachedVertexCount() { return _vertexCount; }
		public int GetCachedTriangleCount() { return _triangleCount; }
		/** 
		 * Get an array of qe_Triangle from this mesh.
		 */
		public qe_Triangle[] GetFaces()
		{
			int[] tris = cloneMesh.triangles;

			qe_Triangle[] t = new qe_Triangle[ tris.Length / 3 ];
			int index = 0;

			for(int i = 0; i < tris.Length; i+=3)
			{
				t[index++] = new qe_Triangle(tris[i], tris[i+1], tris[i+2]);
			}

			return t;
		}

		/**
		 * Get an array of qe_Edge from this mesh (three per-triangle).
		 */
		public qe_Edge[] GetEdges()
		{
			int[] tris = cloneMesh.triangles;

			qe_Edge[] edges = new qe_Edge[tris.Length];

			for(int i = 0; i < tris.Length; i+=3)
			{
				edges[i+0] = new qe_Edge(tris[i+0], tris[i+1]);
				edges[i+1] = new qe_Edge(tris[i+1], tris[i+2]);
				edges[i+2] = new qe_Edge(tris[i+2], tris[i+0]);
			}

			return edges;
		}

		/**
		 * One triangle per user editable vertex.
		 */
		public IList<int> GetUserIndices()
		{
			List<int> arr = new List<int>();
			for(int i = 0; i < sharedTriangles.Count; i++)
				arr.Add(sharedTriangles[i][0]);
			return arr;
		}

		public IList<int> GetUserIndices(IList<int> indices)
		{
			List<int> user = new List<int>( indices );

			for(int i = 0; i < user.Count; i++)
				user[i] = triangleLookup[user[i]];

			user = user.Distinct().ToList();

			for(int i = 0; i < user.Count; i++)
				user[i] = sharedTriangles[user[i]][0];

			return user;
		}

		public IList<int> GetAllIndices(IList<int> indices)
		{
			List<int> _ind = new List<int>(indices);

			for(int i = 0; i < indices.Count; i++)
				_ind[i] = triangleLookup[indices[i]];

			_ind = _ind.Distinct().ToList();

			List<int> all = new List<int>(indices);

			for(int i = 0; i < _ind.Count; i++)
				all.AddRange( sharedTriangles[_ind[i]] );

			return all;
		}

		public int ToUserIndex(int triangle)
		{
			return sharedTriangles[triangleLookup[triangle]][0];
		}

		/**
		 * Initialize a new qe_Mesh with @InGameObject.  Must have a valid meshfilter and mesh.
		 * GameObject will have it's MeshFilter.sharedMesh property set to the clone mesh for editing.
		 * Call qe_Mesh.Revert() to undo this change (and destroy the cloned mesh in the process).
		 */
		public static qe_Mesh Create(GameObject InGameObject)
		{
			qe_Mesh qmesh = ScriptableObject.CreateInstance<qe_Mesh>();
			qmesh.hideFlags = HideFlags.DontSave;

			qmesh.gameObject = InGameObject;
			qmesh.transform = qmesh.gameObject.transform;

			MeshFilter mf = InGameObject.GetComponent<MeshFilter>();
			SkinnedMeshRenderer mr = InGameObject.GetComponent<SkinnedMeshRenderer>();

			Mesh og_mesh = mf != null ? mf.sharedMesh : (mr != null ? mr.sharedMesh : null);

			qmesh.originalMesh = og_mesh;

			qmesh.source = qe_Editor_Utility.GetMeshGUID( qmesh.originalMesh, ref qmesh.originalMeshGUID );

			// Copy mesh from InMesh.
			qmesh.cloneMesh = qe_Mesh_Utility.Clone( og_mesh );
			Undo.RegisterCreatedObjectUndo(qmesh.cloneMesh, "Open Quick Edit");

			Undo.RecordObject(qmesh, "Open Quick Edit");
			qmesh.Apply();

			qmesh.handlesRenderer = (qe_HandleRenderer) Undo.AddComponent(InGameObject, typeof(qe_HandleRenderer));
			qmesh.handlesRenderer.hideFlags = HideFlags.HideAndDontSave;

			qmesh.handlesRenderer.mesh = new Mesh();
			qmesh.handlesRenderer.mesh.hideFlags = HideFlags.HideAndDontSave;
			qmesh.handlesRenderer.material = null;

			qmesh.CacheElements();

			return qmesh;
		}

		public void CacheElements()
		{
			int vertexCount = cloneMesh.vertexCount;
			_vertexCount = vertexCount;
			_triangleCount = cloneMesh.triangles.Length;

			Vector3[] v = cloneMesh.vertices;

			bool[] assigned = qeUtil.FilledArray(false, vertexCount);

			sharedTriangles = new List<List<int>>();

			bool showProgressBar = vertexCount > 4000;
			
			for(int i = 0; i < vertexCount-1; i++)
			{
				if(assigned[i])
					continue;

				List<int> indices = new List<int>(1) {i};
				for(int n = i+1; n < vertexCount; n++)
				{
					if( v[i] == v[n] )
					{
						indices.Add(n);
						assigned[n] = true;
					}
				}

				if( showProgressBar && i % 100 == 0)
					EditorUtility.DisplayProgressBar("Optimize Mesh for Editing", "Caching elements...", i/(float)vertexCount);

				sharedTriangles.Add( indices );
			}

		 	if(!assigned[vertexCount-1])
				sharedTriangles.Add( new List<int>() {vertexCount-1} );

			triangleLookup = new Dictionary<int, int>();

			for(int i = 0; i < sharedTriangles.Count; i++)	
			{
				for(int n = 0; n < sharedTriangles[i].Count; n++)
					triangleLookup.Add(sharedTriangles[i][n], i);
			}

			_faces = GetFaces();
			_edges = GetEdges();
			_userEdges = _edges.ToSharedIndex(triangleLookup)
												.Distinct()
												.ToTriangleIndex(sharedTriangles)
												.ToArray();

			EditorUtility.ClearProgressBar();
		}

		/**
		 * Sets the MeshFilter sharedMesh to the cloned mesh.
		 */
		public void Apply()
		{
			if( gameObject != null )
			{
				MeshFilter mf = gameObject.GetComponent<MeshFilter>();

				if(mf != null)
				{
					mf.sharedMesh = cloneMesh;
				}
				else
				{
					SkinnedMeshRenderer mr = gameObject.GetComponent<SkinnedMeshRenderer>();
					if(mr != null)
						mr.sharedMesh = cloneMesh;
				}
			}
		}

		/**
		 * Sets the MeshFilter sharedMesh to the original mesh, and destroys the 
		 * cloned mesh.
		 */
		public void Revert()
		{
			if( cloneMesh != null )
				Undo.DestroyObjectImmediate( cloneMesh );

			if( originalMesh != null && gameObject != null )
			{
				MeshFilter mf = gameObject.GetComponent<MeshFilter>();

				if( mf != null )
				{
					mf.sharedMesh = originalMesh;
				}
				else
				{
					SkinnedMeshRenderer mr = gameObject.GetComponent<SkinnedMeshRenderer>();

					if(mr != null)
						mr.sharedMesh = originalMesh;
				}
			}
		}

		void OnDestroy()
		{
			if( handlesRenderer != null )
				DestroyImmediate( handlesRenderer );
		}

#region Serialization Override

		[SerializeField] List<int> lookup_keys = new List<int>();
		[SerializeField] List<int> lookup_values = new List<int>();
		[SerializeField] List<JaggedArrayContainer> shared_values = new List<JaggedArrayContainer>();

		[System.Serializable]
		class JaggedArrayContainer
		{
			public List<int> value;

			public JaggedArrayContainer(List<int> val)
			{
				value = val;
			}
		}

		public void OnBeforeSerialize()
		{
			lookup_keys.Clear();
			lookup_values.Clear();

			foreach(KeyValuePair<int, int> kvp in triangleLookup)
			{
				lookup_keys.Add(kvp.Key);
				lookup_values.Add(kvp.Value);
			}

			shared_values.Clear();

			for(int i = 0; i < sharedTriangles.Count; i++)
				shared_values.Add( new JaggedArrayContainer(sharedTriangles[i]) );
		}

		public void OnAfterDeserialize()
		{
			triangleLookup = new Dictionary<int, int>();
			sharedTriangles = new List<List<int>>();

			for(int i = 0; i < lookup_keys.Count; i++)
				triangleLookup.Add(lookup_keys[i], lookup_values[i]);

			for(int i = 0; i < shared_values.Count; i++)
				sharedTriangles.Add(shared_values[i].value);
		}
#endregion
	}
}
