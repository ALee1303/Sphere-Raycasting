using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace QuickEdit
{
	internal static class qe_Editor_Utility
	{
#region NOTIFICATION MANAGER

		const float TIMER_DISPLAY_TIME = 1f;
		private static float notifTimer = 0f;
		private static EditorWindow notifWindow;
		private static bool notifDisplayed = false;

		/**
		 * Show a timed notification in the SceneView window.
		 */
		public static void ShowNotification(string notif)
		{
			SceneView scnview = SceneView.lastActiveSceneView;
			if (scnview == null)
				scnview = EditorWindow.GetWindow<SceneView>();

			ShowNotification(scnview, notif);
		}

		public static void ShowNotification(EditorWindow window, string notif)
		{
			window.ShowNotification(new GUIContent(notif, ""));
			window.Repaint();

			if (EditorApplication.update != NotifUpdate)
				EditorApplication.update += NotifUpdate;

			notifTimer = Time.realtimeSinceStartup + TIMER_DISPLAY_TIME;
			notifWindow = window;
			notifDisplayed = true;
		}

		public static void RemoveNotification(EditorWindow window)
		{
			EditorApplication.update -= NotifUpdate;

			window.RemoveNotification();
			window.Repaint();
		}

		private static void NotifUpdate()
		{
			if (notifDisplayed && Time.realtimeSinceStartup > notifTimer)
			{
				notifDisplayed = false;
				RemoveNotification(notifWindow);
			}
		}
#endregion

#region SceneView

		public static bool SceneViewInUse(Event e)
		{
			return e.alt
					|| Tools.current == Tool.View
					|| GUIUtility.hotControl > 0
					|| (e.isMouse ? e.button > 1 : false)
					|| Tools.viewTool == ViewTool.FPS
					|| Tools.viewTool == ViewTool.Orbit;
		}
#endregion

#region Raycast

		/**
		 * Describes different culling options.
		 */
		public enum Culling
		{
			Back = 0x1,
			Front = 0x2,
			FrontBack = 0x4
		}

		/**
		 * Find a triangle intersected by InRay on InMesh.  InRay is in world space.
		 * Returns the index in mesh.faces of the hit face, or -1.  Optionally can ignore
		 * backfaces.
		 */
		public static bool MeshRaycast(Ray InWorldRay, qe_Mesh mesh, out qe_RaycastHit hit)
		{
			return MeshRaycast(InWorldRay, mesh, out hit, Mathf.Infinity, Culling.Front);
		}

		/**
		 * Find the nearest triangle intersected by InWorldRay on this pb_Object.  InWorldRay is in world space.
		 * @hit contains information about the hit point.  @distance limits how far from @InWorldRay.origin the hit
		 * point may be.  @cullingMode determines what face orientations are tested (Culling.Front only tests front 
		 * faces, Culling.Back only tests back faces, and Culling.FrontBack tests both).
		 */
		public static bool MeshRaycast(Ray InWorldRay, qe_Mesh mesh, out qe_RaycastHit hit, float distance, Culling cullingMode)
		{
			/**
			 * Transform ray into model space
			 */

			InWorldRay.origin 		-= mesh.transform.position;  // Why doesn't worldToLocalMatrix apply translation?
			InWorldRay.origin 		= mesh.transform.worldToLocalMatrix * InWorldRay.origin;
			InWorldRay.direction 	= mesh.transform.worldToLocalMatrix * InWorldRay.direction;

			Vector3[] vertices = mesh.vertices;

			float dist = 0f;
			Vector3 point = Vector3.zero;

			float OutHitPoint = Mathf.Infinity;
			float dot; // vars used in loop
			Vector3 nrm;	// vars used in loop
			int OutHitFace = -1;
			Vector3 OutNrm = Vector3.zero;

			/**
			 * Iterate faces, testing for nearest hit to ray origin.  Optionally ignores backfaces.
			 */
			for(int CurFace = 0; CurFace < mesh.faces.Length; ++CurFace)
			{
				int[] Indices = mesh.faces[CurFace].indices;

				for(int CurTriangle = 0; CurTriangle < Indices.Length; CurTriangle += 3)
				{
					Vector3 a = vertices[Indices[CurTriangle+0]];
					Vector3 b = vertices[Indices[CurTriangle+1]];
					Vector3 c = vertices[Indices[CurTriangle+2]];

					nrm = Vector3.Cross(b-a, c-a);
					dot = Vector3.Dot(InWorldRay.direction, nrm);

					bool ignore = false;

					switch(cullingMode)
					{
						case Culling.Front:
							if(dot > 0f) ignore = true;
							break;

						case Culling.Back:
							if(dot < 0f) ignore = true;
							break;
					}

					if(!ignore && qe_Math.RayIntersectsTriangle(InWorldRay, a, b, c, out dist, out point))
					{
						if(dist > OutHitPoint || dist > distance)
							continue;

						OutNrm = nrm;
						OutHitFace = CurFace;
						OutHitPoint = dist;

						continue;
					}
				}
			}

			hit = new qe_RaycastHit(OutHitPoint,
									InWorldRay.GetPoint(OutHitPoint),
									OutNrm,
									OutHitFace);

			return OutHitFace > -1;
		}

		/**
		 * Find the all triangles intersected by InWorldRay on this pb_Object.  InWorldRay is in world space.
		 * @hit contains information about the hit point.  @distance limits how far from @InWorldRay.origin the hit
		 * point may be.  @cullingMode determines what face orientations are tested (Culling.Front only tests front 
		 * faces, Culling.Back only tests back faces, and Culling.FrontBack tests both).
		 */
		public static bool MeshRaycast(Ray InWorldRay, qe_Mesh qe, out List<qe_RaycastHit> hits, float distance, Culling cullingMode)
		{
			/**
			 * Transform ray into model space
			 */
			InWorldRay.origin -= qe.transform.position;  // Why doesn't worldToLocalMatrix apply translation?
			
			InWorldRay.origin 		= qe.transform.worldToLocalMatrix * InWorldRay.origin;
			InWorldRay.direction 	= qe.transform.worldToLocalMatrix * InWorldRay.direction;

			Vector3[] vertices = qe.vertices;

			float dist = 0f;
			Vector3 point = Vector3.zero;

			float dot; // vars used in loop
			Vector3 nrm;	// vars used in loop
			hits = new List<qe_RaycastHit>();

			/**
			 * Iterate faces, testing for nearest hit to ray origin.  Optionally ignores backfaces.
			 */
			for(int CurFace = 0; CurFace < qe.faces.Length; ++CurFace)
			{
				int[] Indices = qe.faces[CurFace].indices;

				for(int CurTriangle = 0; CurTriangle < Indices.Length; CurTriangle += 3)
				{
					Vector3 a = vertices[Indices[CurTriangle+0]];
					Vector3 b = vertices[Indices[CurTriangle+1]];
					Vector3 c = vertices[Indices[CurTriangle+2]];

					if(qe_Math.RayIntersectsTriangle(InWorldRay, a, b, c, out dist, out point))
					{
						nrm = Vector3.Cross(b-a, c-a);

						switch(cullingMode)
						{
							case Culling.Front:
								dot = Vector3.Dot(InWorldRay.direction, -nrm);

								if(dot > 0f)
									goto case Culling.FrontBack;
								break;

							case Culling.Back:
								dot = Vector3.Dot(InWorldRay.direction, nrm);

								if(dot > 0f)
									goto case Culling.FrontBack;
								break;

							case Culling.FrontBack:
								hits.Add( new qe_RaycastHit(dist,
															InWorldRay.GetPoint(dist),
															nrm,
															CurFace));
								break;
						}

						continue;
					}
				}
			}

			return hits.Count > 0;
		}

		const float MAX_EDGE_SELECT_DISTANCE = 20f;

		/**
		 * Checks if mouse is over an edge, and if so, returns true setting @edge.
		 */
		public static bool EdgeRaycast(Vector2 mousePosition, ElementCache selection, out qe_Edge edge)
		{
			qe_Mesh mesh = selection.mesh;

			Vector3 v0, v1;
			float bestDistance = Mathf.Infinity;
			float distance = 0f;
			edge = null;

			GameObject go = HandleUtility.PickGameObject(mousePosition, false);

			if( go == null || go != selection.transform.gameObject)
			{
				qe_Edge[] edges = mesh.userEdges;

				int width = Screen.width;
				int height = Screen.height;

				for(int i = 0; i < edges.Length; i++)
				{
					v0 = selection.verticesInWorldSpace[edges[i].x];
					v1 = selection.verticesInWorldSpace[edges[i].y];
					
					distance = HandleUtility.DistanceToLine(v0, v1);

					if ( distance < bestDistance && distance < MAX_EDGE_SELECT_DISTANCE )// && !PointIsOccluded(mesh, (v0+v1)*.5f) )
					{
						Vector3 vs0 = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(v0);
						
						// really simple frustum check (will fail on edges that have vertices outside the frustum but is visible)
						if( vs0.z <= 0 || vs0.x < 0 || vs0.y < 0 || vs0.x > width || vs0.y > height )
							continue;

						Vector3 vs1 = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(v1);

						if( vs1.z <= 0 || vs1.x < 0 || vs1.y < 0 || vs1.x > width || vs1.y > height )
							continue;


						bestDistance = distance;
						edge = edges[i];
					}
				}
			}
			else
			{
				// Test culling
				List<qe_RaycastHit> hits;
				Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

				if( MeshRaycast(ray, mesh, out hits, Mathf.Infinity, Culling.FrontBack) )
				{
					// Sort from nearest hit to farthest
					hits.Sort( (x, y) => x.Distance.CompareTo(y.Distance) );
					
					// Find the nearest edge in the hit faces
					Vector3[] v = mesh.vertices;

					for(int i = 0; i < hits.Count; i++)
					{
						if(  PointIsOccluded(mesh, mesh.transform.TransformPoint(hits[i].Point)) )
							continue;

						foreach(qe_Edge e in mesh.faces[hits[i].FaceIndex].GetEdges())
						{
							float d = HandleUtility.DistancePointLine(hits[i].Point, v[e.x], v[e.y]);

							if(d < bestDistance)
							{
								bestDistance = d;
								edge = e;
							}
						}

						if( Vector3.Dot(ray.direction, mesh.transform.TransformDirection(hits[i].Normal)) < 0f )
							break;
					}

					if(edge != null && HandleUtility.DistanceToLine(mesh.transform.TransformPoint(v[edge.x]), mesh.transform.TransformPoint(v[edge.y])) > MAX_EDGE_SELECT_DISTANCE)
					{
						edge = null;
					}
					else
					{
						edge.x = mesh.ToUserIndex(edge.x);
						edge.y = mesh.ToUserIndex(edge.y);
					}
				}
			}

			return edge != null;
		}

		public static bool VertexRaycast(Vector2 mousePosition, int rectSize, ElementCache selection, out int index)
		{
			qe_Mesh mesh = selection.mesh;

			float bestDistance = Mathf.Infinity;
			float distance = 0f;
			index = -1;

			GameObject go = HandleUtility.PickGameObject(mousePosition, false);

			if( go == null || go != selection.transform.gameObject )
			{
				Camera cam = SceneView.lastActiveSceneView.camera;
				int width = Screen.width;
				int height = Screen.height;

				Rect mouseRect = new Rect(mousePosition.x - (rectSize/2f), mousePosition.y - (rectSize/2f), rectSize, rectSize);
				List<int> user = (List<int>) selection.mesh.GetUserIndices();

				for(int i = 0; i < user.Count; i++)
				{
					if( mouseRect.Contains( HandleUtility.WorldToGUIPoint(selection.verticesInWorldSpace[user[i]]) ) )
					{				
						Vector3 v = cam.WorldToScreenPoint(selection.verticesInWorldSpace[user[i]]);

						distance = Vector2.Distance(mousePosition, v);

						if( distance < bestDistance )
						{
							if( v.z <= 0 || v.x < 0 || v.y < 0 || v.x > width || v.y > height )
								continue;

							if( PointIsOccluded(mesh, selection.verticesInWorldSpace[user[i]]) )
								continue;

							index = user[i];
							bestDistance = Vector2.Distance(v, mousePosition);
						}
					}
				}
			}
			else
			{
				// Test culling
				List<qe_RaycastHit> hits;
				Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

				if( MeshRaycast(ray, mesh, out hits, Mathf.Infinity, Culling.FrontBack) )
				{
					// Sort from nearest hit to farthest
					hits.Sort( (x, y) => x.Distance.CompareTo(y.Distance) );
					
					// Find the nearest edge in the hit faces
					Vector3[] v = mesh.vertices;

					for(int i = 0; i < hits.Count; i++)
					{
						if(  PointIsOccluded(mesh, mesh.transform.TransformPoint(hits[i].Point)) )
							continue;

						foreach(int tri in mesh.faces[hits[i].FaceIndex].indices)
						{
							float d = Vector3.Distance(hits[i].Point, v[tri]);

							if(d < bestDistance)
							{
								bestDistance = d;
								index = tri;
							}
						}

						if( Vector3.Dot(ray.direction, mesh.transform.TransformDirection(hits[i].Normal)) < 0f )
							break;
					}

					if(index > -1 && Vector2.Distance(mousePosition, HandleUtility.WorldToGUIPoint(selection.verticesInWorldSpace[index])) > rectSize * 1.3f)
					{
						index = -1;
					}
				}
			}

			if( index > -1 )
				index = mesh.ToUserIndex(index);

			return index > -1;
		}

		/**
		 * Returns true if this point in world space is occluded by a triangle on this object.
		 */
		public static bool PointIsOccluded(qe_Mesh mesh, Vector3 worldPoint)
		{
			Camera cam = SceneView.lastActiveSceneView.camera;
			Vector3 dir = (cam.transform.position - worldPoint).normalized;

			// move the point slightly towards the camera to avoid colliding with its own triangle
			Ray ray = new Ray(worldPoint + dir * .0001f, dir);
			
			qe_RaycastHit hit;

			return MeshRaycast(ray, mesh, out hit, Vector3.Distance(cam.transform.position, worldPoint), Culling.Back);
		}
#endregion

#region Editor

		/**
		 * Attempt to read user data attached to a mesh by qe_Editor.
		 */
		public static bool GetMeshUserData(AssetImporter assimp, out string originalImportedMeshPath, out string originalImportedMeshSubAssetName)
		{
			string[] data = assimp.userData.Split('?');

			if(data.Length == 2)
			{
				originalImportedMeshPath = data[0];
				originalImportedMeshSubAssetName = data[1];
				return true;
			}

			originalImportedMeshPath = "";
			originalImportedMeshSubAssetName = "";

			return false;
		}

		public static ModelSource GetMeshGUID(Mesh mesh, ref string guid)
		{
			string path = AssetDatabase.GetAssetPath(mesh);

			if(path != "")
			{
				AssetImporter assetImporter = AssetImporter.GetAtPath(path);

				if( assetImporter != null )
				{
					// Only imported model (e.g. FBX) assets use the ModelImporter,
					// where a saved asset will have an AssetImporter but *not* ModelImporter.
					// A procedural mesh (one only existing in a scene) will not have any.
					if (assetImporter is ModelImporter)
					{
						guid = AssetDatabase.AssetPathToGUID(path);
						return ModelSource.Imported;
					}
					else
					{
						guid = AssetDatabase.AssetPathToGUID(path);
						return ModelSource.Asset;
					}
				}
				else
				{
					return ModelSource.Scene;
				}
			}

			return ModelSource.Scene;
		}

		const int DIALOG_OK = 0;
		const int DIALOG_CANCEL = 1;
		const int DIALOG_ALT = 2;
		const string DO_NOT_SAVE = "DO_NOT_SAVE";

		/**
		 * Returns true if save was successfull, false if user-cancelled or otherwise failed.
		 */
		public static bool SaveMeshAssetIfNecessary(qe_Mesh mesh)
		{
			string save_path = DO_NOT_SAVE;

			switch( mesh.source )
			{
				case ModelSource.Asset:

					int saveChanges = EditorUtility.DisplayDialogComplex(
						"Save Changes",
						"Save changes to edited mesh?",
						"Save",				// DIALOG_OK
						"Cancel",			// DIALOG_CANCEL
						"Save As");			// DIALOG_ALT

					if( saveChanges == DIALOG_OK )
						save_path = AssetDatabase.GetAssetPath(mesh.originalMesh);
					else if( saveChanges == DIALOG_ALT )
						save_path = EditorUtility.SaveFilePanelInProject("Save Mesh As", mesh.cloneMesh.name + ".asset", "asset", "Save edited mesh to");
					else
						return false;

					break;

				case ModelSource.Imported:
				case ModelSource.Scene:
				default:
					// @todo make sure path is in Assets/
					save_path = EditorUtility.SaveFilePanelInProject("Save Mesh As", mesh.cloneMesh.name + ".asset", "asset", "Save edited mesh to");
				break;
			}

			if( !save_path.Equals(DO_NOT_SAVE) && !string.IsNullOrEmpty(save_path) )
			{
				Object existing = AssetDatabase.LoadMainAssetAtPath(save_path);

				if( existing != null )
				{
					qe_Mesh_Utility.Copy( (Mesh)existing, mesh.cloneMesh);
					GameObject.DestroyImmediate(mesh.cloneMesh);
				}
				else
				{
					AssetDatabase.CreateAsset(mesh.cloneMesh, save_path );
				}

				AssetDatabase.Refresh();

				MeshFilter mf = mesh.gameObject.GetComponent<MeshFilter>();
				SkinnedMeshRenderer mr = mesh.gameObject.GetComponent<SkinnedMeshRenderer>();

				if(mf != null)
					mf.sharedMesh = (Mesh)AssetDatabase.LoadAssetAtPath(save_path, typeof(Mesh));
				else if(mr != null)
					mr.sharedMesh = (Mesh)AssetDatabase.LoadAssetAtPath(save_path, typeof(Mesh));

				return true;
			}

			// Save was canceled
			return false;
		}
#endregion
	}
}
