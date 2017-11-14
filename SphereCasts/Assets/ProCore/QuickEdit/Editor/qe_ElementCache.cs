using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace QuickEdit
{
	[System.Serializable]
	internal class ElementCache : ScriptableObject
	{
		// The currently editing mesh.
		public qe_Mesh mesh;

		// Selected indices
		public List<int> indices 		{ get { return _indices; } }

		// Selected edges
		public List<qe_Edge> edges 		{ get { return _edges; } }
		
		// Selected faces
		public List<qe_Triangle> faces 	{ get { return _faces; } }

		// All vertices including hard edges that weren't explicitly selected
		public List<int> allIndices 	{ get { return _allIndices; } }

		// Unique (no hard vertex duplicates) and distinct.
		public List<int> userIndices 	{ get { return _userIndices; } }

		public Vector3[] verticesInWorldSpace;

		public Transform transform { get { return mesh != null ? mesh.transform : null; } }

		public int selectedUserVertexCount { get; private set; }

		[SerializeField] private List<int> _indices = new List<int>();
		[SerializeField] private List<qe_Edge> _edges = new List<qe_Edge>();
		[SerializeField] private List<qe_Triangle> _faces = new List<qe_Triangle>();
		[SerializeField] private List<int> _allIndices = new List<int>();
		[SerializeField] private List<int> _userIndices = new List<int>(); 

		/** 
		 * Set the selection using Faces.  Updates all elements below face (edge, tri).
		 */
		public void SetFaces(IList<qe_Triangle> faces)
		{
			this._faces = faces.ToList();
			this._edges = faces.SelectMany(x => x.GetEdges())
								.ToSharedIndex(mesh.triangleLookup)
								.Distinct()
								.ToTriangleIndex(mesh.sharedTriangles)
								.ToList();
			this._indices = faces.SelectMany(x => x.GetIndices() ).ToList();


			CacheIndices();
		}

		/**
		 * Set the selection using edges. Face selection will be cleared, and indices selection is set to all indices in edge array.
		 */
		public void SetEdges(IList<qe_Edge> edges)
		{
			this._faces.Clear();
			this._edges = edges.ToList();
			this._indices = edges.ToIndices().ToList();
			
			CacheIndices();
		}

		/** 
		 * Set the selection with triangles.  Face and edge arrays are cleared.
		 */
		public void SetIndices(IList<int> indices)
		{
			this._faces.Clear();
			this._edges.Clear();
			this._indices = indices.ToList();

			CacheIndices();
		}

		private void CacheIndices()
		{
			_allIndices = mesh.GetAllIndices(this.indices).Distinct().ToList();
			_userIndices = (List<int>) mesh.GetUserIndices(this.indices);
			selectedUserVertexCount = _userIndices.Count;
		}

		public void CacheMeshValues()
		{
			Vector3[] v = mesh.vertices;
			int vc = v.Length;

			verticesInWorldSpace = new Vector3[vc];
			Matrix4x4 matrix = mesh.transform.localToWorldMatrix;

			for(int i = 0; i < vc; i++)
				verticesInWorldSpace[i] = matrix.MultiplyPoint3x4(v[i]);
		}

		public void Clear()
		{
			indices.Clear();
			edges.Clear();
			faces.Clear();
			_allIndices.Clear();
			_userIndices.Clear();
			selectedUserVertexCount = 0;
		}

		/**
		 * Returns the average position of the selected vertices in model space.
		 */
		public HandleTransform GetHandleTransform()
		{
			Vector3 v = Vector3.zero;
			int len = _userIndices.Count;
			Vector3[] vertices = mesh.vertices;

			for(int i = 0; i < len; i++)
				v += vertices[_userIndices[i]];

			HandleTransform handle = new HandleTransform();
			if( len > 0 ) handle.position = v / (float)len;
			handle.rotation = Quaternion.identity;
			handle.scale = Vector3.one;

			return handle;
		}
	}
}