#if UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5
#define UNITY_5_5_OR_LOWER
#endif

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace QuickEdit
{
	/**
	 * Editor vindow implementation.  Responsible for GUI and selection management.
	 */
	public class qe_Editor : ScriptableObject, ISerializationCallbackReceiver
	{
#region Const & Members

		// The diameter in pixels that a mouse click will register vertex hits.
		const int CLICK_RECT = 32;

		// The draggable bit of the sceneview window.
		readonly Rect QE_WINDOW_DRAG_RECT = new Rect(0,0,128,20);

#if DO_THE_DEBUG_DANCE
		Rect windowRect = new Rect(24, 24, 200, 260);
#else
		// Size of the GUI window.
		Rect windowRect = new Rect(24, 24, 105, 150);
#endif

		// Automatically set - used to clamp the GUI window to the sceneview window rect.
		Rect windowBounds = new Rect(0,0,0,0);

		// The rect the vertex / edge / face toolbar takes up.
		readonly Rect toolbarRect = new Rect(4,20,180,24);

		// The color that a mouse drag will tint the selected rect.
		readonly Color MOUSE_DRAG_RECT_COLOR = new Color(.313f, .8f, 1f, 1f);

		// What element type is being selected.
		[SerializeField] ElementMode elementMode = ElementMode.Face;

		// Stores information about the mesh currently being edited.
		[SerializeField] ElementCache selection;

		// Various GUI content fields.
		GUIContent[] SelectionIcons;
		GUIContent gc_RebuildNormals = new GUIContent("Normals", "Rebuild the normals for this mesh.");
		GUIContent gc_RebuildUV2 = new GUIContent("UV2", "Rebuild the UV2 (Lightmap) array for this mesh.");
		GUIContent gc_Facetize = new GUIContent("Tri", "Rebuild the mesh will all hard edges.  Also called Facetize, or Triangulate.");
		GUIContent gc_RebuildColliders = new GUIContent("Collider", "Rebuild the mesh collisions.");
		GUIContent gc_DeleteFaces = new GUIContent("Delete Faces", "Delete all selected faces from the mesh.");
		GUIContent gc_CenterPivot = new GUIContent("Center Pivot", "Make the pivot point of this mesh equal to the center of the mesh bounds.");

		public static qe_Editor instance { get { return singleton; } }
		[SerializeField] private static qe_Editor singleton;

		// A list of pre-formatted indices to be fed to qe_Mesh_Utility.CreateMesh() to generate
		// the preview element highlights.  Set using CacheIndicesForGraphics().
		[SerializeField] private List<int> selectedIndices = new List<int>();
		[SerializeField] private List<int> unselectedIndices = new List<int>();
#endregion

#region Initialization 

		/**
		 * ISerialization... requires both be implemented.
		 */
		public void OnBeforeSerialize() {}

		/**
		 * On deserialization, need to re-register sceneview and undo delegates.
		 */
		public void OnAfterDeserialize()
		{
			singleton = this;

			SceneView.onSceneGUIDelegate += OnSceneGUI;
			Undo.undoRedoPerformed += UndoRedoPerformed;
			SceneView.RepaintAll();
		} 

		[MenuItem("Tools/QuickEdit/Edit Selected Mesh &e", true, 0)]
		static bool InitCheck()
		{
			return 	Selection.transforms.Select(x => x.GetComponentInChildren<MeshFilter>()).FirstOrDefault() != null ||
					Selection.transforms.Select(x => x.GetComponentInChildren<SkinnedMeshRenderer>()).FirstOrDefault() != null;
		}
  
		[MenuItem("Tools/QuickEdit/Edit Selected Mesh &e", false, 0)]
		static void Init()
		{ 
			if( singleton != null )
				return;

			singleton = ScriptableObject.CreateInstance<qe_Editor>();
			singleton.hideFlags = HideFlags.DontSave;
			EditorApplication.delayCall += singleton.Initialize;
		}

		/**
		 * Initialize a n ew instance of Quick Edit, taking care of caching and setting up the appropriate stores.
		 */
		void Initialize()
		{
			SceneView.onSceneGUIDelegate += OnSceneGUI;
			Undo.undoRedoPerformed += UndoRedoPerformed;

			if(selection == null)
			{
				selection = ScriptableObject.CreateInstance<ElementCache>();
				selection.hideFlags = HideFlags.DontSave;
			} 

			if( selection.mesh == null )
			{
				MeshFilter mf = Selection.transforms.Select(x => x.GetComponentInChildren<MeshFilter>()).FirstOrDefault();
	 
				if( mf == null )
				{
					SkinnedMeshRenderer mr = Selection.transforms.Select(x => x.GetComponentInChildren<SkinnedMeshRenderer>()).FirstOrDefault();
					if(mr == null || mr.sharedMesh == null)
					{
						qe_Editor_Utility.ShowNotification("No Mesh Selected");
						EditorApplication.delayCall += Close;
					}
					else
					{
						SetSelection(mr.gameObject);
					}
				}
				else
				{
					SetSelection(mf.gameObject);
				}
			}
			else
			{
				SceneView.RepaintAll();
			}

			LoadIcons();
		}

		/**
		 * Load the toolbar icons.
		 */
		void LoadIcons()
		{
			bool isProSkin = true; // EditorGUIUtility.isProSkin;
			Texture2D VertexIcon = (Texture2D)(AssetDatabase.LoadAssetAtPath(isProSkin ? "Assets/ProCore/QuickEdit/Gizmos/VertexIcon_Pro.png" : "Assets/ProCore/QuickEdit/Gizmos/VertexIcon.png", typeof(Texture2D)));
			Texture2D EdgeIcon = (Texture2D)(AssetDatabase.LoadAssetAtPath(isProSkin ? "Assets/ProCore/QuickEdit/Gizmos/EdgeIcon_Pro.png" : "Assets/ProCore/QuickEdit/Gizmos/EdgeIcon.png", typeof(Texture2D)));
			Texture2D FaceIcon = (Texture2D)(AssetDatabase.LoadAssetAtPath(isProSkin ? "Assets/ProCore/QuickEdit/Gizmos/FaceIcon_Pro.png" : "Assets/ProCore/QuickEdit/Gizmos/FaceIcon.png", typeof(Texture2D)));

			if( VertexIcon != null && EdgeIcon != null && FaceIcon != null)
			{
				SelectionIcons = new GUIContent[3]
				{
					new GUIContent(VertexIcon, "Vertex Selection"),
					new GUIContent(EdgeIcon, "Edge Selection"),
					new GUIContent(FaceIcon, "Face Selection")
				};
			}
			else
			{
				SelectionIcons = new GUIContent[3]
				{
					new GUIContent("V", "Vertex Selection"),
					new GUIContent("E", "Edge Selection"),
					new GUIContent("F", "Face Selection")
				};
			}
		}

		/**
		 * On exiting QE, this determines how objects are cleaned up.  Save will prompt for a path, or save/save as.
		 * DoNotSave will ask 'are you sure', and NullValue just cleans up quietly.
		 */
		enum ExitStatus{
			Save,
			DoNotSave,
			NullValue
		};

		/**
		 * Finish editing and clean up temp objects.
		 */
		void Finish(ExitStatus status)
		{
			if(status == ExitStatus.DoNotSave || status == ExitStatus.NullValue) 
			{
				if(selection != null && selection.mesh != null)
				{
					if(status != ExitStatus.NullValue && selection.transform != null )
					{
						if( !EditorUtility.DisplayDialog("Cancel Mesh Modifications", "This will clear all changes to the mesh and exit QuickEdit.  Are you sure you wish to continue?", "Yes", "No") )
							return;

						selection.mesh.Revert();
					}
					else
					{
						// this is a hot exit, scorch the earth
						selection.mesh.Revert();
					}

					DestroyImmediate(selection.mesh);
				}
			}
			else
			{
				// If the source mesh was imported, save a new asset.
				// If source was an asset, ask to save over or save as.
				// If source was procedural, ask to replace or save as asset.
				// After save, applies the new saved asset to the selection (or cancels).
				// Safe to destroy selection after this returns.
				if( !qe_Editor_Utility.SaveMeshAssetIfNecessary(selection.mesh) )
				{ 
					return;
				}

				DestroyImmediate(selection.mesh); 
			} 
			
			Close();
		} 

		/**
		 * Clean up delegates and sceneview state changes.
		 */
		void Close()
		{
			Tools.hidden = false;

			GameObject.DestroyImmediate(selection);

			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			Undo.undoRedoPerformed -= UndoRedoPerformed;

			SceneView.RepaintAll();

			GameObject.DestroyImmediate(this);
		}
#endregion 

#region GameObject and Asset Management

		/**
		 * Sets the currently selected @selection to @go, and replaces @go's
		 * mesh with a new one for editing.
		 */
		void SetSelection(GameObject go)
		{
			selection.mesh = qe_Mesh.Create(go);
			selection.CacheMeshValues();
			SetElementMode(elementMode);
		}

		/**
		 * Returns all currently selected vertices in world space (used to override frame selection in handle renderer editor).
		 */
		internal static Vector3[] GetSelectedVerticesInWorldSpace()
		{
			return singleton != null ? qeUtil.ValuesWithIndices(singleton.selection.verticesInWorldSpace, singleton.selection.userIndices) : new Vector3[0];
		}

		/**
		 * Check that selection, mesh, and Selection are valid.
		 */
		private bool NullCheck()
		{
			return selection == null || selection.transform == null || selection.mesh.cloneMesh == null;
		}
#endregion

#region Scene GUI
 
		private int lastHotControl = -1;
		void OnSceneGUI(SceneView scene)
		{
			if( NullCheck() )
			{
				Finish( ExitStatus.NullValue );
				return;
			}

			Selection.activeTransform = selection.transform;

			Event currentEvent = Event.current;

			windowBounds.width = Screen.width; 
			windowBounds.height = Screen.height;

			windowRect = qe_Math.ClampRect( GUI.Window(0, windowRect, DrawWindow, "QuickEdit"), windowBounds );

			Tools.hidden = selection.mesh != null && selection.selectedUserVertexCount > 0;

			if( GUIUtility.hotControl != lastHotControl )
			{
				lastHotControl = GUIUtility.hotControl;

				if( lastHotControl < 1 )
				{
					if( movingVertices )
					{
						OnFinishVertexMovement();
					}
					else
					{
						//
						if( selection.transform.hasChanged )
						{
							selection.CacheMeshValues();
							CacheIndicesForGraphics();
							UpdateGraphics();
							selection.transform.hasChanged = true;
						}
					}
				}
			}

			if( Tools.hidden )
			{
				VertexHandle();
			}

			if( qe_Editor_Utility.SceneViewInUse(currentEvent) )
			{
				return;
			}

			if( !mouseDragInProgress )
				hovering.DrawHandles();

			int controlID = GUIUtility.GetControlID(FocusType.Passive);
			HandleUtility.AddDefaultControl(controlID);	

			switch( currentEvent.type )
			{
				case EventType.MouseDrag:
				{
					if(!mouseDragInProgress)
						OnBeginMouseDrag(currentEvent.mousePosition);

					mouseDragRect.x = (int) Mathf.Min(mouseDragOrigin.x, currentEvent.mousePosition.x);
					mouseDragRect.y = (int) Mathf.Min(mouseDragOrigin.y, currentEvent.mousePosition.y);
					mouseDragRect.width = Mathf.Abs(mouseDragOrigin.x - currentEvent.mousePosition.x);
					mouseDragRect.height = Mathf.Abs(mouseDragOrigin.y - currentEvent.mousePosition.y);

					SceneView.RepaintAll();
				}
				break;

				case EventType.Ignore:
				{
					OnFinishMouseDrag(currentEvent.mousePosition);
				}
				break;

				case EventType.MouseUp:
				{
					if( mouseDragInProgress )
					{
						OnFinishMouseDrag(currentEvent.mousePosition);
					}
					else
					{
						if( currentEvent.button == 0 &&
							GUIUtility.hotControl < 1)
						{
							OnMouseUp(currentEvent);
						}
					}
				}
				break;

				case EventType.MouseMove:
				{
					UpdateElementHover(currentEvent.mousePosition);
				}
				break;

				case EventType.KeyUp:
				{
					if( currentEvent.keyCode == KeyCode.Backspace && selection.faces.Count > 0 )
					{
						qeUtil.RecordMeshUndo(selection.mesh, "Delete Faces");

						if( qe_Mesh_Utility.DeleteTriangles( selection.mesh, selection.faces ) )
						{
							selection.Clear();
							selection.CacheMeshValues();
							CacheIndicesForGraphics();
							UpdateGraphics();
						}	
					}
				}
				break;
			}

			Handles.BeginGUI();

			if(mouseDragInProgress)
			{
				GUI.backgroundColor = MOUSE_DRAG_RECT_COLOR;
				GUI.Box(mouseDragRect, "");
				GUI.backgroundColor = Color.white;
			}

			Handles.EndGUI();
		
		}

		void DrawWindow(int id)
		{
			EditorGUI.BeginChangeCheck();

				elementMode = (ElementMode) GUI.Toolbar(toolbarRect, (int)elementMode, SelectionIcons, "Command");

			if(EditorGUI.EndChangeCheck())
			{
				SetElementMode( elementMode );
			}

			GUILayout.Space(24);

			GUILayout.BeginHorizontal();
				if( GUILayout.Button(gc_RebuildNormals, EditorStyles.miniButtonLeft) )
				{
					qeUtil.RecordMeshUndo(selection.mesh, "Rebuild Normals");
					qe_Mesh_Utility.RebuildNormals(selection.mesh);
				}

				if( GUILayout.Button(gc_RebuildUV2, EditorStyles.miniButtonRight))	
				{
					qeUtil.RecordMeshUndo(selection.mesh, "Rebuild UV2");
					qe_Mesh_Utility.RebuildUV2(selection.mesh);
				}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
				if( GUILayout.Button(gc_Facetize, EditorStyles.miniButtonLeft) )
				{
					qeUtil.RecordMeshUndo(selection.mesh, "Facetize" );

					qe_Mesh_Utility.Facetize(selection.mesh);
					selection.CacheMeshValues();
				}

				if( GUILayout.Button(gc_RebuildColliders, EditorStyles.miniButtonRight))	
				{
					qeUtil.RecordMeshUndo(selection.mesh, "Rebuild Collision Mesh");
					qe_Mesh_Utility.RebuildColliders(selection.mesh);
				}
			GUILayout.EndHorizontal();

			if(GUILayout.Button(gc_CenterPivot, EditorStyles.miniButton))
			{
				qeUtil.RecordMeshUndo(selection.mesh, "Center Pivot");
				Vector3 offset = qe_Mesh_Utility.CenterPivot(selection.mesh.cloneMesh);
				Undo.RecordObject(selection.transform, "Center Pivot");
				selection.transform.position += offset;
				selection.CacheMeshValues();
			}

			if(GUILayout.Button(gc_DeleteFaces, EditorStyles.miniButton) && selection.faces.Count > 0)
			{
				qeUtil.RecordMeshUndo(selection.mesh, "Delete Faces");

				if( qe_Mesh_Utility.DeleteTriangles( selection.mesh, selection.faces ) )
				{
					selection.Clear();
					selection.CacheMeshValues();
					CacheIndicesForGraphics();
					UpdateGraphics();
				}
			}


			if(GUILayout.Button("Cancel", EditorStyles.miniButton))
				Finish( ExitStatus.DoNotSave );

			if(GUILayout.Button("Save", EditorStyles.miniButton))
				Finish( ExitStatus.Save );


			GUI.DragWindow( QE_WINDOW_DRAG_RECT );

 #if DO_THE_DEBUG_DANCE

			scroll = GUILayout.BeginScrollView(scroll);
			if(selection.mesh != null)
			{
				GUILayout.Label("Source: " + selection.mesh.source);
				GUILayout.Label("GUID: " + selection.mesh.originalMeshGUID);
				GUILayout.Label("Clone: " + selection.mesh.cloneMesh.name);
				GUILayout.Label("Original: " + selection.mesh.originalMesh.name);

				GUILayout.Label("Vertex Count: " + selection.mesh.cloneMesh.vertexCount);
				GUILayout.Label("Triangle Count: " + selection.mesh.cloneMesh.triangles.Length);
			}
			else
			{
				GUILayout.Label("Source: NULL");
			}
			GUILayout.EndScrollView();
#endif

		}

		private Matrix4x4 _handleMatrix;

		void PushHandlesMatrix()
		{
			_handleMatrix = Handles.matrix;
		}

		void PopHandlesMatrix()
		{
			Handles.matrix = _handleMatrix;
		}

		void SetElementMode(ElementMode mode)
		{
			elementMode = mode;

			if( selection.mesh.handlesRenderer.material != null )
				DestroyImmediate(selection.mesh.handlesRenderer.material);

			selection.mesh.handlesRenderer.material = new Material( Shader.Find( mode == ElementMode.Vertex ? "Hidden/QuickEdit/VertexShader" : "Hidden/QuickEdit/FaceShader") );

			if(elementMode == ElementMode.Vertex)	
				selection.mesh.handlesRenderer.material.SetFloat("_Scale", 2f);

			selection.mesh.handlesRenderer.material.hideFlags = HideFlags.HideAndDontSave;

			CacheIndicesForGraphics();
			
			UpdateGraphics();
		}

		void CacheIndicesForGraphics()
		{
			switch(elementMode)
			{
				default:
					selectedIndices = selection.faces.SelectMany(x => x.indices).ToList();
					unselectedIndices.Clear();
					break;

				case ElementMode.Vertex:
				{
					List<int> selected = selection.userIndices;
					List<int> unselected = selection.mesh.GetUserIndices().ToList();
					HashSet<int> remove = new HashSet<int>(selected);
					unselected.RemoveAll(x => remove.Contains(x));

					selectedIndices = selected;
					unselectedIndices = unselected;
				}
				break;

				case ElementMode.Edge:
				{
					List<qe_Edge> sel = selection.edges.ToSharedIndex(selection.mesh.triangleLookup).ToList();

					sel = sel.ToTriangleIndex(selection.mesh.sharedTriangles).ToList();

					selectedIndices = sel.ToIndices().ToList();
					unselectedIndices = selection.mesh.userEdges.ToIndices().ToList();
				}
				break;
			}
		}

		void UpdateGraphics()
		{
			if( selection.mesh == null )
				return;

			switch(elementMode)
			{
				default:
					qe_Mesh_Utility.MakeFaceSelectionMesh(	ref selection.mesh.handlesRenderer.mesh, 
															qeUtil.ValuesWithIndices(selection.mesh.vertices,
																selectedIndices ));
					break;

				case ElementMode.Vertex:
				{
					qe_Mesh_Utility.MakeVertexSelectionMesh(
						ref selection.mesh.handlesRenderer.mesh,
						qeUtil.ValuesWithIndices(selection.mesh.vertices, unselectedIndices),
						qeUtil.ValuesWithIndices(selection.mesh.vertices, selectedIndices));
				}
				break;

				case ElementMode.Edge:
				{
					qe_Mesh_Utility.MakeEdgeSelectionMesh(
						ref selection.mesh.handlesRenderer.mesh,
						qeUtil.ValuesWithIndices(selection.mesh.vertices, unselectedIndices),
						qeUtil.ValuesWithIndices(selection.mesh.vertices, selectedIndices));
				}
				break;
			}

			SceneView.RepaintAll();
		}

		void ResetHandles()
		{
			// Do not apply object scale to Handles.matrix because it skews the gizmos.
			handlesTransformMatrix = Matrix4x4.TRS(selection.transform.position, selection.transform.rotation, Vector3.one); // selection.transform.localToWorldMatrix;
			originPivot = selection.GetHandleTransform();
			activePivot = originPivot;
			originPivotMatrix = originPivot.GetMatrix();
		}

		private class HoveringPreview
		{
			#if UNITY_5
			public Vector3[] vertices = new Vector3[3];
			#else
			public Vector3[] vertices = new Vector3[4];
			#endif

			public ElementMode mode;
			public bool valid = false;
			public int hashCode;

			readonly Color FACE_HIGHLIGHT_COLOR = new Color(.8f, .2f, .2f, .4f);
			readonly Color EDGE_HIGHLIGHT_COLOR = new Color(.8f, .2f, .2f, .8f);
			readonly Color VERTEX_HIGHLIGHT_COLOR = new Color(.8f, .2f, .2f, .8f);

			public void DrawHandles()
			{
				if( !valid )
					return;

				switch(mode)
				{
					case ElementMode.Vertex:
					{
						Handles.color = VERTEX_HIGHLIGHT_COLOR;
						#if UNITY_5_5_OR_LOWER
						Handles.DotCap(-1, vertices[0], Quaternion.identity, HandleUtility.GetHandleSize(vertices[0]) * .06f);
						#else
						Handles.DotHandleCap(-1, vertices[0], Quaternion.identity, HandleUtility.GetHandleSize(vertices[0]) * .06f, Event.current.type);
						#endif
					}
					break;

					case ElementMode.Face:
					{
						#if UNITY_5
						Handles.color = FACE_HIGHLIGHT_COLOR;
						Handles.DrawAAConvexPolygon(vertices);
						#else
						Handles.DrawSolidRectangleWithOutline(	vertices,
																FACE_HIGHLIGHT_COLOR,
																FACE_HIGHLIGHT_COLOR);
						#endif
					}
					break;

					case ElementMode.Edge:
					{
						Handles.color = EDGE_HIGHLIGHT_COLOR;
						Handles.DrawLine(vertices[0], vertices[1]);
					}
					break;
				}

				Handles.color = Color.white;
			}
		}

		HoveringPreview hovering = new HoveringPreview();

		void UpdateElementHover(Vector2 mousePosition)
		{
			hovering.mode = elementMode;
			int hash = hovering.hashCode;
			bool wasValid = hovering.valid;
			hovering.valid = false;

			switch( elementMode )
			{
				case ElementMode.Vertex:
				{
					int tri = -1;

					if( qe_Editor_Utility.VertexRaycast(mousePosition, CLICK_RECT, selection, out tri) )
					{
						hovering.valid = true;
						hovering.hashCode = tri.GetHashCode();
						hovering.vertices[0] = selection.verticesInWorldSpace[tri];
					}
				}
				break;

				case ElementMode.Face:
				{
					qe_RaycastHit hit;

					if( qe_Editor_Utility.MeshRaycast( HandleUtility.GUIPointToWorldRay(mousePosition), selection.mesh, out hit) )
					{
						qe_Triangle face = selection.mesh.faces[hit.FaceIndex];
						hovering.valid = true;

						if( hash != face.GetHashCode() )
						{
							hovering.hashCode = face.GetHashCode();

							hovering.vertices[0] = selection.verticesInWorldSpace[face.x];
							hovering.vertices[1] = selection.verticesInWorldSpace[face.y];
							hovering.vertices[2] = selection.verticesInWorldSpace[face.z];
							#if !UNITY_5
							hovering.vertices[3] = selection.verticesInWorldSpace[face.z];
							#endif
						}
					}
				}
				break;

				case ElementMode.Edge:
				{
					qe_Edge edge;

					if( qe_Editor_Utility.EdgeRaycast(mousePosition, selection, out edge) )
					{
						hovering.valid = true;

						if( hash != edge.GetHashCode() )
						{
							hovering.hashCode = edge.GetHashCode();

							hovering.vertices[0] = selection.verticesInWorldSpace[edge.x];
							hovering.vertices[1] = selection.verticesInWorldSpace[edge.y];
						}
					}
				}
				break;
			}

			if( hash != hovering.hashCode || hovering.valid != wasValid )
				SceneView.RepaintAll();
		}
#endregion

#region Vertex Handles

		// Transform delta of the current handle.
		[SerializeField] HandleTransform originPivot = new HandleTransform();
		[SerializeField] Matrix4x4 originPivotMatrix = Matrix4x4.identity;
		HandleTransform activePivot = new HandleTransform();
		[SerializeField] Vector3[] vertexCache = new Vector3[0];
		Matrix4x4 handlesTransformMatrix = Matrix4x4.identity;
		bool movingVertices = false;

		Vector3 DivideBy(Vector3 a, Vector3 b)
		{
			return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
		}

		void VertexHandle()
		{
			PushHandlesMatrix();

			Handles.matrix = handlesTransformMatrix;
			HandleTransform t_pivot = activePivot;
			
			// Have to apply scale here instead of setting the Handles.matrix to object matrix because
			// handle gizmos are skewed when rendered in a matrix with non-identity scale.
			t_pivot.position = Vector3.Scale(t_pivot.position, selection.transform.lossyScale);

			switch( Tools.current )
			{
				case Tool.Move:
					t_pivot.position = Handles.PositionHandle(t_pivot.position, t_pivot.rotation );
					break;

				case Tool.Rotate:
					t_pivot.rotation = Handles.RotationHandle(t_pivot.rotation, t_pivot.position);
					break;

				case Tool.Scale:
					t_pivot.scale = Handles.ScaleHandle(t_pivot.scale,
														t_pivot.position,
														t_pivot.rotation,
														HandleUtility.GetHandleSize(t_pivot.position));
					break;
			}

			t_pivot.position = DivideBy(t_pivot.position, selection.transform.lossyScale);

			if( t_pivot != activePivot )
			{
				if( !movingVertices )
				{
					qeUtil.RecordMeshUndo(selection.mesh, "Move vertices");
					OnBeginVertexMovement();
				}			

				Matrix4x4 delta = (t_pivot - originPivot).GetMatrix();
				activePivot = t_pivot;
				
				Vector3[] v = selection.mesh.vertices;

				for(int i = 0; i < selection.allIndices.Count; i++)
				{
					Vector3 inv = originPivotMatrix.inverse.MultiplyPoint3x4( vertexCache[selection.allIndices[i]] );
					inv = delta.MultiplyPoint3x4( inv );
					v[selection.allIndices[i]] = originPivotMatrix.MultiplyPoint3x4( inv );
				}
				
				selection.mesh.cloneMesh.vertices = v;

				UpdateGraphics();
			}

			PopHandlesMatrix();
		}
#endregion

#region Event Handling

		void OnMouseUp(Event e)
		{
			if(!e.shift)
			{
				Undo.RecordObject(selection, "Clear Selection");
				selection.Clear();
			}

			switch( elementMode )
			{
				case ElementMode.Vertex:
				{
					int index = -1;

					if( qe_Editor_Utility.VertexRaycast(e.mousePosition, CLICK_RECT, selection, out index) )
					{
						List<int> sel = selection.userIndices;
						int indexOf = sel.IndexOf(index);

						if(indexOf < 0)
							sel.Add(index);
						else
							sel.RemoveAt(indexOf);

						selection.SetIndices(sel);

						break;
					}
					else
					{
						goto default;
					}
				}

				case ElementMode.Edge:
				{
					qe_Edge edge;

					if( qe_Editor_Utility.EdgeRaycast(e.mousePosition, selection, out edge) )
					{
						List<qe_Edge> sel = selection.edges.Distinct().ToList();
						int index = sel.IndexOf(edge);

						if(index > -1)
							sel.RemoveAt(index);
						else
							sel.Add(edge);

						selection.SetEdges(sel);
					}
					else
					{
						goto case ElementMode.Face;
					}

					break;
				}

				case ElementMode.Face:
				default:
				{
					qe_RaycastHit hit;

					if( qe_Editor_Utility.MeshRaycast( HandleUtility.GUIPointToWorldRay(e.mousePosition), selection.mesh, out hit) )
					{
						List<qe_Triangle> sel = selection.faces;
						qe_Triangle tri = selection.mesh.faces[hit.FaceIndex];

						int index = sel.IndexOf(tri);

						if(index > -1)
							sel.RemoveAt(index);
						else
							sel.Add(tri);

						selection.SetFaces(sel);
					}	

					break;
				}
			}

			ResetHandles();
			CacheIndicesForGraphics();
			UpdateGraphics();

		}

		bool mouseDragInProgress = false;
		Vector2 mouseDragOrigin = Vector2.zero;
		Rect mouseDragRect = new Rect(0,0,0,0);

		void OnBeginMouseDrag(Vector2 mousePosition)
		{
			mouseDragInProgress = true;
			mouseDragOrigin = mousePosition;
		}

		void OnCancelMouseDrag()
		{
			mouseDragInProgress = false;
			SceneView.RepaintAll();
		}

		void OnFinishMouseDrag(Vector2 mousePosition)
		{
			mouseDragInProgress = false;

			if(!Event.current.shift)
			{
				selection.Clear();
			}

			switch( elementMode )
			{
				case ElementMode.Face:
				{
					HashSet<qe_Triangle> sel = new HashSet<qe_Triangle>(selection.faces);

					foreach(qe_Triangle tri in selection.mesh.faces)
					{
						if( mouseDragRect.Contains(HandleUtility.WorldToGUIPoint( selection.verticesInWorldSpace[tri.x] )) &&
							mouseDragRect.Contains(HandleUtility.WorldToGUIPoint( selection.verticesInWorldSpace[tri.y] )) &&
							mouseDragRect.Contains(HandleUtility.WorldToGUIPoint( selection.verticesInWorldSpace[tri.z] )) )
						{
							if( sel.Contains(tri) )	
								sel.Remove(tri);
							else
								sel.Add(tri);
						}
					}

					selection.SetFaces( sel.ToList() );
				}
				break;

				case ElementMode.Edge:
				{
					HashSet<qe_Edge> sel = new HashSet<qe_Edge>(selection.edges);

					foreach(qe_Edge edge in selection.mesh.userEdges)
					{
						if( mouseDragRect.Contains(HandleUtility.WorldToGUIPoint( selection.verticesInWorldSpace[edge.x])) && 
							mouseDragRect.Contains(HandleUtility.WorldToGUIPoint( selection.verticesInWorldSpace[edge.y])) )
						{
							if( sel.Contains(edge) )
								sel.Remove(edge);
							else
								sel.Add(edge);
						}
					}

					selection.SetEdges(sel.ToList());
				}
				break;

				case ElementMode.Vertex:
				{
					HashSet<int> sel = new HashSet<int>(selection.indices);

					for(int i = 0; i < selection.mesh.sharedTriangles.Count; i++)
					{
						int index = selection.mesh.sharedTriangles[i][0];

						Vector2 v = HandleUtility.WorldToGUIPoint( selection.verticesInWorldSpace[index] );

						if( mouseDragRect.Contains(v) )
						{
							if( sel.Contains(index) )
								sel.Remove(index);
							else
								sel.Add(index);
						}
					}

					selection.SetIndices(sel.ToList());
				}
				break;
			}

			ResetHandles();
			CacheIndicesForGraphics();
			UpdateGraphics();
		}

		void OnBeginVertexMovement()
		{
			qe_Lightmapping.PushGIWorkflowMode();
			
			int vertexCount = selection.mesh.cloneMesh.vertexCount;
			vertexCache = new Vector3[vertexCount];
			System.Array.Copy(selection.mesh.vertices, 0, vertexCache, 0, vertexCount);

			movingVertices = true;
		}

		void OnFinishVertexMovement()
		{		
			qe_Lightmapping.PopGIWorkflowMode();

			ResetHandles();
			movingVertices = false;
			
			selection.CacheMeshValues();
			UpdateGraphics();
			
			selection.mesh.cloneMesh.RecalculateBounds();
		}
#endregion

#region Delegate

		void UndoRedoPerformed()
		{
			if( NullCheck() )
			{
				Finish( ExitStatus.NullValue );
				return;
			}

			selection.mesh.cloneMesh.vertices = selection.mesh.cloneMesh.vertices;
			selection.mesh.Apply();

			if(	selection.mesh.vertexCount != selection.mesh.GetCachedVertexCount() || 
				selection.mesh.cloneMesh.triangles.Length != selection.mesh.GetCachedTriangleCount() )
				selection.mesh.CacheElements();

			selection.CacheMeshValues(); 
			CacheIndicesForGraphics();
			
			ResetHandles();
			UpdateGraphics();

			SceneView.RepaintAll();
		}
#endregion

#if DO_THE_DEBUG_DANCE

		Vector2 scroll = Vector2.zero;

		void DrawDebugHandles()
		{
			foreach(List<int> arr in selection.mesh.sharedTriangles)
			{
				Vector2 v = HandleUtility.WorldToGUIPoint( selection.mesh.transform.TransformPoint(selection.mesh.vertices[arr[0]]) );
				GUIContent gc = new GUIContent(arr.ToFormattedString(", "));
				DrawSceneLabel(v, gc);
			}
		}
		
		private static GUIStyle _splitStyle;
		private static GUIStyle SplitStyle
		{
			get
			{
				if(_splitStyle == null)
				{
					_splitStyle = new GUIStyle();
					_splitStyle.normal.background = EditorGUIUtility.whiteTexture;
					_splitStyle.margin = new RectOffset(6,6,0,0);
				}
				return _splitStyle;
			}
		}

		/**
		 * Draw a solid color block at rect.
		 */
		public static void DrawSolidColor(Rect rect, Color col)
		{
			Color old = UnityEngine.GUI.backgroundColor;
			UnityEngine.GUI.backgroundColor = col;

			UnityEngine.GUI.Box(rect, "", SplitStyle);

			UnityEngine.GUI.backgroundColor = old;
		}

		void DrawSceneLabel(Vector2 position, GUIContent content)
		{
			float width = EditorStyles.boldLabel.CalcSize(content).x;
			float height = EditorStyles.label.CalcHeight(content, width) + 4;

			DrawSolidColor( new Rect(position.x, position.y, width, height), Color.black);
			GUI.Label( new Rect(position.x, position.y, width, height), content, EditorStyles.boldLabel );
		}
#endif
	}
}
