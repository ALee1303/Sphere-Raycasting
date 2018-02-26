#if UNITY_WEBPLAYER
#pragma warning disable 0414	// DragDecalsImage
#endif

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using ProCore.Decals;
using System.Reflection;
using QD = ProCore.Decals;

public class qd_Editor : EditorWindow
{
#region CONST

	const string QD_DATABASE_PATH = "Assets/ProCore/QuickDecals/Database/QuickDecalsDatabase.asset";
	const int MIN_IMG_SIZE = 24;
	const int MAX_IMG_SIZE = 512;
	const int SCROLL_BAR_WIDTH = 18;

	const int LEFT_MOUSE_BUTTON = 0;
	const int RIGHT_MOUSE_BUTTON = 1;
	const bool OPEN_AS_UTILITY = true;

	const int SCROLL_MODIFIER = 5;	// how much the mouse scroll wheel affects the image size (in px)

	const float MIN_ROTATION = -180f;
	const float MAX_ROTATION = 180f;
	const float MIN_SCALE = .01f;
	const float MAX_SCALE = 10f;

	#if UNITY_4_3
	const int SETTINGS_HEIGHT = 110;
	#else
	const int SETTINGS_HEIGHT = 107; // 4_3 - 3
	#endif

	Color RowColorEven, RowColorOdd;
	Color ToolbarOnColor;
	Color BoldFontColor;
	Color HeaderBarColor;
	Color PackedRed = new Color(.8f, .1f, .1f, 1f);
	Texture2D DragDecalsImage;
#endregion

#region Members

	DecalView decalView = DecalView.Organizational;

	Event e;
	qd_Database database;
	List<DecalGroup> decalGroups;

	// Layout
	int settingsTrayHeight = 80;

	// Images
	int imageSize = 64;

	Dictionary<int, List<int>> selected = new Dictionary<int, List<int>>();
	int mouseOver_groupIndex = -1;
	int mouseOver_textureIndex = -1;

	int shaderIndex;	// because of the way shaders are set, use this to relay which group should get what shader

	Texture2D transparentImage;
	Texture2D DropZoneImage;

	// GUI
	bool isFloating = OPEN_AS_UTILITY;
	GUIStyle backgroundColorStyle;
	GUIStyle colorTextStyle, colorTextStyleBold;
	Color SelectedItemColor = new Color(0f, .8f, 1f, 1f);
	Color DragTargetColor = new Color(0f, .8f, 1f, .3f);
	Color SettingsBackgroundColor {
 		get {
 			return EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, .4f) : new Color(.7f, .7f, .7f, 1f);
 		}
	}

	// Decal Placement
	MethodInfo IntersectRayMesh = typeof(HandleUtility).GetMethod("IntersectRayMesh", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);
	public GameObject DecalParent
	{
		get
		{
			GameObject go = GameObject.Find("DecalParent");
			if(go == null)
			{
				go = new GameObject();
				go.name = "DecalParent";
			}
			return go;
		}
	}

#endregion

#region MEnu

	[MenuItem("Tools/QuickDecals/About", false, 0)]
	public static void MenuAbuot()
	{
		qd_AboutWindow.Init("Assets/ProCore/QuickDecals/About/pc_AboutEntry_QuickDecals.txt", true);
	}

	[MenuItem("Tools/QuickDecals/Decals Window")]
	public static void MenuOpenDecalsWindow()
	{
		bool openAsUtil = EditorPrefs.HasKey("qd_OpenAsUtility") ? EditorPrefs.GetBool("qd_OpenAsUtility") : true;
		EditorWindow.GetWindow<qd_Editor>(openAsUtil, "QuickDecals", false).isFloating = openAsUtil;
	}

	static void ContextOpenFloatingWindow()
	{
		EditorPrefs.SetBool("qd_OpenAsUtility", true);
		EditorWindow.GetWindow<qd_Editor>().Close();
		EditorWindow.GetWindow<qd_Editor>(true, "QuickDecals", true).isFloating = true;
	}

	static void ContextOpenDockableWindow()
	{
		EditorPrefs.SetBool("qd_OpenAsUtility", false);
		EditorWindow.GetWindow<qd_Editor>().Close();
		EditorWindow.GetWindow<qd_Editor>(false, "QuickDecals", false).isFloating = false;
	}
#endregion

#region Init

	void OnEnable()
	{
		dragging = false;

		database = (qd_Database)AssetDatabase.LoadAssetAtPath(QD_DATABASE_PATH, typeof(qd_Database));

		if(database == null)
			database = InitDatabase();

		if( database.LoadDecalGroups(decalView) )
			decalGroups = database.decalGroups;

		InitGUI();

		#if UNITY_4_3
			Undo.undoRedoPerformed += this.UndoRedoPerformed;
		#endif

		HookSceneView();

		this.minSize = new Vector2(365f, 275f);
	}

	void OnDisable()
	{
		#if UNITY_4_3
			Undo.undoRedoPerformed -= this.UndoRedoPerformed;
		#endif

		SceneView.onSceneGUIDelegate -= this.OnSceneGUI;

		if(transparentImage != null)
			DestroyImmediate(transparentImage);

		database.Save(decalView);
	}

	void InitGUI()
	{
	 	BoldFontColor = EditorGUIUtility.isProSkin ? new Color(.8f, .8f, .8f, .7f) : new Color(.65f, .65f, .65f, 1f);
	 	ToolbarOnColor = EditorGUIUtility.isProSkin ? new Color(.8f, .8f, .8f, 1f) : new Color(.8f, .8f, .8f, 1f);
	 	HeaderBarColor = EditorGUIUtility.isProSkin ? new Color(.06f, .06f, .06f, 1f) : new Color(.23f, .23f, .23f, 1f);

		colorTextStyle = new GUIStyle();
		colorTextStyleBold = new GUIStyle();
		colorTextStyleBold.fontStyle = FontStyle.Bold;
		colorTextStyle.normal.textColor = BoldFontColor;
		colorTextStyleBold.normal.textColor = BoldFontColor;

		backgroundColorStyle = new GUIStyle();
		backgroundColorStyle.normal.background = EditorGUIUtility.whiteTexture;

		RowColorOdd = EditorGUIUtility.isProSkin ? new Color(.83f, .83f, .83f, .06f) : new Color(.83f, .83f, .83f, 1f);
	 	RowColorEven = EditorGUIUtility.isProSkin ? new Color(.8f, .8f, .8f, .02f) : new Color(.8f, .8f, .8f, 1f);

		DragDecalsImage = (Texture2D)Resources.Load("DragDecalsImage", typeof(Texture2D));
		DropZoneImage = (Texture2D)Resources.Load("DropZoneImage", typeof(Texture2D));
	}

	void HookSceneView()
	{
		if(SceneView.onSceneGUIDelegate != this.OnSceneGUI)
		{
			SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
			SceneView.onSceneGUIDelegate += this.OnSceneGUI;
		}
	}
#endregion

#region GUI

	Texture2D dragObject;
	bool dragging = false;
	Vector2 mousePositionInGroupRect = Vector2.zero;
	string searchString = "";
	Rect toolbarRect = new Rect();
	Rect textureDisplayRect = new Rect();
	Rect settingsDisplayRect = new Rect();

	void OnGUI()
	{
		float screenWidth = this.position.width;
		float screenHeight = this.position.height;

		e = Event.current;

		// Listener methods will return true if it's necessary to repaint
		if( KeyListener() )
			Repaint();

		// Figure out layout stuff
		toolbarRect = new Rect(0, 0, screenWidth, 28);
		int trayHeight = (decalView == DecalView.Organizational ? SETTINGS_HEIGHT : SETTINGS_HEIGHT + 12);
		settingsTrayHeight = trayHeight - (isFloating ? (int)toolbarRect.height : (int)(toolbarRect.height+toolbarRect.y - 8));
		textureDisplayRect = new Rect(pad, toolbarRect.y+ (isFloating ? toolbarRect.height+pad : toolbarRect.height-12), screenWidth-4, screenHeight-settingsTrayHeight - toolbarRect.height+pad);

		settingsDisplayRect = new Rect(2, textureDisplayRect.y+textureDisplayRect.height+pad, screenWidth-4, settingsTrayHeight);

		mousePositionInGroupRect = e.mousePosition + scroll;
		mousePositionInGroupRect.y -= textureDisplayRect.y;

		if( DrawToolbar(toolbarRect) )
		{
			Repaint();
		}

		DrawGroups(textureDisplayRect, imageSize);

		// Mouse interaction
		MouseListener();

		if( ListenForDragAndDrop(textureDisplayRect) )
			return;

		DrawSettings(settingsDisplayRect);

		if(Event.current.type == EventType.ValidateCommand)
		{
			OnValidateCommand(Event.current.commandName);
		}

		Repaint();
	}

	/**
	 * The top toolbar with pane toggle and search bar.
	 */
	bool DrawToolbar(Rect r)
	{
		bool needsRepaint = false;

		GUI.BeginGroup(r);

			GUILayout.BeginHorizontal(EditorStyles.toolbar);
				Color col = GUI.backgroundColor;
				GUI.backgroundColor = decalView == DecalView.Organizational ? ToolbarOnColor : col;
				if(GUILayout.Button("Decals", EditorStyles.toolbarButton))
				{
					database.Save(decalView);
					decalView = DecalView.Organizational;
					selected.Clear();
					database.LoadDecalGroups(decalView);
					decalGroups = database.decalGroups;
					needsRepaint = true;
				}
				GUI.backgroundColor = decalView == DecalView.Atlas ? ToolbarOnColor : col;
				if( GUILayout.Button("Atlas", EditorStyles.toolbarButton) )
				{
					database.Save(decalView);
					decalView = DecalView.Atlas;
					selected.Clear();
					database.LoadDecalGroups(decalView);
					decalGroups = database.decalGroups;
					needsRepaint = true;
				}
				GUI.backgroundColor = col;
				GUILayout.FlexibleSpace();

				if(decalView != DecalView.Atlas)
				{
					GUI.SetNextControlName("DecalSearch");
					searchString = GUILayout.TextField(searchString, GUI.skin.FindStyle("ToolbarSeachTextField"), GUILayout.MinWidth(Mathf.Min(160, this.position.width/2f)));
					if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton")))
					{
					    // Remove focus if cleared
					    searchString = "";
					    GUIUtility.keyboardControl = 0;
					    GUI.FocusControl("");
					}
				}
			GUILayout.EndHorizontal();
		GUI.EndGroup();

		return needsRepaint;
	}

	Vector2 scroll = Vector2.zero;
	Rect[] groupRects = new Rect[0];
	Rect[][] decalRects = new Rect[0][];
	int pad = 2;
	void DrawGroups(Rect r, int maxImageHeight)
	{
		int groupLength = decalGroups == null ? 0 : decalGroups.Count;


#if UNITY_WEBPLAYER
		if(decalView == DecalView.Atlas || groupLength < 1)
#else
		if(groupLength < 1)
#endif
		{
			groupRects = new Rect[0];
			decalRects = new Rect[0][];

			int sz = (int)Mathf.Min(Mathf.Min(r.width, r.height)/1.3f, 256);
#if UNITY_WEBPLAYER
			if(decalView == DecalView.Atlas)
			{
				sz = 256;
				Rect helpRect = new Rect(12, (r.height/2-r.height/3f )+toolbarRect.height/2f, r.width-24,96);
				EditorGUI.HelpBox(helpRect, "Your project is currently targeting the 'Webplayer' platform. Due to Unity restrictions, you must switch to an non-Webplayer platform (File > Build Settings) to generate and control your Decal Atlases. Don't worry though- once generated, the Atlases will work just fine in your Webplayer game.", MessageType.Warning);
				if(GUI.Button(new Rect(r.width/2-sz/2, helpRect.y + helpRect.height + 3, sz, 32), new GUIContent("Target " + EditorUserBuildSettings.selectedStandaloneTarget, "Set the Build Target to this setting.  Alternatively, open up the Build Settings window (Super+Shift+B)")))
					EditorUserBuildSettings.SwitchActiveBuildTarget(EditorUserBuildSettings.selectedStandaloneTarget);
			}
			else
			{
				GUI.Label(new Rect(r.width/2-sz/2, (r.height/2-sz/2)+toolbarRect.height/2f, sz, sz), DragDecalsImage);
			}
#else
			GUI.Label(new Rect(r.width/2-sz/2, (r.height/2-sz/2)+toolbarRect.height/2f, sz, sz), DragDecalsImage);
#endif
			return;
		}

		// Get all GUI dimensions before drawing because we need to draw selected background colors and stuff first
		decalRects = new Rect[groupLength][];
		groupRects = new Rect[groupLength];
		int xpos = pad, ypos = pad;

		for(int i = 0; i < groupLength; i++)
		{
			decalRects[i] = new Rect[decalGroups[i].decals.Count];
			groupRects[i] = new Rect(xpos, ypos, r.width-pad*2, maxImageHeight);

			int skipped = 0;
			for(int j = 0; j < decalGroups[i].decals.Count; j++)
			{
				if(decalView != DecalView.Atlas && searchString != "" && !decalGroups[i].decals[j].texture.name.ToLower().Contains(searchString.ToLower()))
				{
					skipped++;
					continue;
				}

				if(dragging && selected.Contains(i, j)) continue;

				if(decalGroups[i].decals[j].texture == null)
				{
					decalGroups[i].decals.Remove(decalGroups[i].decals[j]);
					database.Save(decalView);
					return;
				}

				Texture2D img = decalGroups[i].decals[j].texture;
				int w = (int)((img.width/(float)img.height) * maxImageHeight);

				// when dragging, scooch decals to make a space for where the dragreferences
				// will be dropped
				if(dragging && mouseOver_groupIndex == i && mouseOver_textureIndex == j)
				{
					hovering.bounds = new Rect(xpos, ypos, imageSize, imageSize);
					hovering.groupIndex = i;
					hovering.textureIndex = j;
					xpos += imageSize;
				}

				if(xpos + w > r.width)
				{
					xpos = pad;
					ypos += maxImageHeight + pad;
					groupRects[i].height += maxImageHeight+pad;
				}

				decalRects[i][j] = new Rect(xpos, ypos, w, maxImageHeight);

				xpos += w + pad;
			}

			xpos = pad;

			if(decalView == DecalView.Organizational && skipped == decalGroups[i].decals.Count)
				groupRects[i].height = 0;
			else
				ypos += maxImageHeight + pad;
		}

		int viewHeight = (int)(groupRects[groupLength-1].y + groupRects[groupLength-1].height);

		viewHeight += maxImageHeight;

		// draw!
		scroll = GUI.BeginScrollView(r, scroll, new Rect(0, 0, viewHeight > r.height ? r.width - 16 : r.width, viewHeight));
		for(int i = 0; i < groupLength; i++)
		{
			if( (dragging && i == mouseOver_groupIndex && dragOriginIndex != i) || (decalView == DecalView.Atlas && selected.ContainsKey(i) && !dragging))
				GUI.backgroundColor = DragTargetColor;
			else
				GUI.backgroundColor = i % 2 == 0 ? RowColorEven : RowColorOdd;

			GUI.Box(groupRects[i], "", backgroundColorStyle);

			GUI.backgroundColor = Color.clear;
			for(int j = 0; j < decalRects[i].Length; j++)
			{
				Texture2D img = decalGroups[i].decals[j].texture;

				if(selected.Contains(i, j))
				{
					if(dragging)
						continue;
					else
						GUI.backgroundColor = SelectedItemColor;
				}

				if(decalView != DecalView.Atlas)
					GUI.Box(decalRects[i][j], "", backgroundColorStyle);
				GUI.backgroundColor = Color.clear;

				GUI.Label(decalRects[i][j], new GUIContent(img, img.name));
			}
		}

		int dropZoneWidth = (int)Mathf.Min(128, maxImageHeight);
		Rect dropZone = new Rect(
			r.x+(r.width/2f - dropZoneWidth/2f),
			groupRects[groupRects.Length-1].y+groupRects[groupRects.Length-1].height,//+dropZoneWidth/2f,
			dropZoneWidth,
			dropZoneWidth);

		GUI.Label(dropZone, DropZoneImage);

		GUI.EndScrollView();

		// Draw dragging texture last so it's always on top
		if(dragging && dragTextures != null)
		{
			for(int i = 0; i < dragTextures.Count; i++)
			{
				GUI.Label( new Rect(
					e.mousePosition.x + dragMouseOffset[i].x,
					e.mousePosition.y + dragMouseOffset[i].y,
					(int)((dragTextures[i].width/(float)dragTextures[i].height) * maxImageHeight), maxImageHeight),
					dragTextures[i]);
			}
		}
	}

	float defaultVal = 0f, minVal = 0f, maxVal = 0F;
	void DrawSettings(Rect r)
	{
		List<string> selected_textures = new List<string>();
		foreach(KeyValuePair<int, List<int>> kvp in selected)
			foreach(int n in kvp.Value)
				selected_textures.Add(decalGroups[kvp.Key].decals[n].texture.name);

		GUI.backgroundColor = SettingsBackgroundColor;
		GUI.Box(r, "", backgroundColorStyle);

		GUI.backgroundColor = HeaderBarColor;
		GUI.Box(new Rect(r.x, r.y, r.width, 22), "", backgroundColorStyle);

		GUI.Label(new Rect(r.x+4, r.y+4, r.width, 22), decalView == DecalView.Atlas ? "Atlas Packing Settings" : "Decal Settings", colorTextStyleBold);

		if(selected_textures.Count > 0 && decalView == DecalView.Organizational)
			GUI.Label(new Rect(r.x+100, r.y+4, r.width, 22), (selected_textures.Count > 1 ? selected_textures.Count + " selected." : selected_textures[0]), colorTextStyle);

		GUI.backgroundColor = Color.white;
		bool packed = false;

		GUILayout.Space(6);

		GUI.BeginGroup(r);

			GUI.enabled = selected.Count > 0;

			if(decalView == DecalView.Organizational)
			{
				Placement rotate_placement = Placement.Fixed;
				Placement scale_placement = Placement.Fixed;
				Vector3 rotation = Vector3.zero;
				Vector3 scale = Vector3.zero;

				int t = 0;

				bool[] mixedValue_rotation = new bool[] {false, false, false}, mixedValue_scale = new bool[] {false, false, false};
				bool mixedValue_placement_rotation = false;
				bool mixedValue_placement_scale = false;

				foreach(KeyValuePair<int, List<int>> kvp in selected)
				{
					foreach(int n in kvp.Value)
					{
						Placement oldRotationPlacement = rotate_placement;
						Placement oldScalePlacement = scale_placement;
						Vector3 oldScale = scale;
						Vector3 oldRotation = rotation;

						rotate_placement = decalGroups[kvp.Key].decals[n].rotationPlacement;
						scale_placement = decalGroups[kvp.Key].decals[n].scalePlacement;

						rotation = decalGroups[kvp.Key].decals[n].rotation;
						scale = decalGroups[kvp.Key].decals[n].scale;

						if(t != 0)
						{
							if(rotate_placement != oldRotationPlacement)
								mixedValue_placement_rotation = true;

							if(scale_placement != oldScalePlacement)
								mixedValue_placement_scale = true;

							if(oldRotation.x != rotation.x)
								mixedValue_rotation[0] = true;
							if(oldRotation.y != rotation.y)
								mixedValue_rotation[1] = true;

							if(oldRotation.z != rotation.z)
								mixedValue_rotation[2] = true;

							if(oldScale.x != scale.x)
								mixedValue_scale[0] = true;
							if(oldScale.y != scale.y)
								mixedValue_scale[1] = true;

							if(oldScale.z != scale.z)
								mixedValue_scale[2] = true;
						}
						t++;
					}
				}

				///*** Rotation ****///
				{
					GUI.enabled = t > 0 && !mixedValue_rotation[0];
					minVal = rotation.x;
					maxVal = rotation.y;
					defaultVal = rotation.z;
					// GUILayout.Label("Rotation: " + minVal.ToString("F2") + " to " + maxVal.ToString("F2"));
					// GUI.enabled = prev;

					GUILayout.BeginHorizontal();

						GUILayout.Label("Rotation", GUILayout.MaxWidth(60));
						EditorGUI.showMixedValue = mixedValue_placement_rotation;
						EditorGUI.BeginChangeCheck();
						rotate_placement = (Placement) EditorGUILayout.EnumPopup(rotate_placement, GUILayout.MaxWidth(120));
						if(EditorGUI.EndChangeCheck())
						{
							foreach(KeyValuePair<int, List<int>> kvp in selected)
								foreach(int i in kvp.Value)
									decalGroups[kvp.Key].decals[i].rotationPlacement = rotate_placement;
						}

						{
							GUI.enabled = rotate_placement == Placement.Fixed;
							GUILayout.Label("Default", GUILayout.MaxWidth(46));

							EditorGUI.showMixedValue = mixedValue_rotation[2];
							EditorGUI.BeginChangeCheck();

							GUI.SetNextControlName("RotationDefaultValue");
							defaultVal = EditorGUILayout.FloatField(defaultVal, GUILayout.MaxWidth(44));
							if(EditorGUI.EndChangeCheck())
							{
								foreach(KeyValuePair<int, List<int>> kvp in selected)
									foreach(int i in kvp.Value)
										decalGroups[kvp.Key].decals[i].rotation.z = defaultVal;
							}

						}
						// else
						{
							GUILayout.Space(5);
							GUI.enabled = rotate_placement == Placement.Random;

							EditorGUI.showMixedValue = mixedValue_rotation[0];
							EditorGUI.BeginChangeCheck();
							GUILayout.Label("Min", GUILayout.MaxWidth(30));
							GUI.SetNextControlName("RotationMinValue");
							minVal = EditorGUILayout.FloatField(minVal, GUILayout.MaxWidth(44));
							if(EditorGUI.EndChangeCheck())
							{
								foreach(KeyValuePair<int, List<int>> kvp in selected)
									foreach(int i in kvp.Value)
										decalGroups[kvp.Key].decals[i].rotation.x = minVal;
							}

							EditorGUI.showMixedValue = mixedValue_rotation[1];
							EditorGUI.BeginChangeCheck();
							GUILayout.Label("Max", GUILayout.MaxWidth(30));
							GUI.SetNextControlName("RotationMaxValue");
							maxVal = EditorGUILayout.FloatField(maxVal, GUILayout.MaxWidth(44));
							if(EditorGUI.EndChangeCheck())
							{
								foreach(KeyValuePair<int, List<int>> kvp in selected)
									foreach(int i in kvp.Value)
										decalGroups[kvp.Key].decals[i].rotation.y = maxVal;
							}
						}

					GUILayout.EndHorizontal();
				}
				///*** Rotation ****///

				GUILayout.Space(8);

				///*** Scale ****///
				{
					GUI.enabled = t > 0 && !mixedValue_rotation[0];
					minVal = scale.x;
					maxVal = scale.y;
					defaultVal = scale.z;

					GUILayout.BeginHorizontal();

						GUILayout.Label("Scale", GUILayout.MaxWidth(60));
						EditorGUI.showMixedValue = mixedValue_placement_scale;
						EditorGUI.BeginChangeCheck();
						scale_placement = (Placement) EditorGUILayout.EnumPopup(scale_placement, GUILayout.MaxWidth(120));
						if(EditorGUI.EndChangeCheck())
						{
							foreach(KeyValuePair<int, List<int>> kvp in selected)
								foreach(int i in kvp.Value)
									decalGroups[kvp.Key].decals[i].scalePlacement = scale_placement;
						}

						{
							GUI.enabled = scale_placement == Placement.Fixed;
							GUILayout.Label("Default", GUILayout.MaxWidth(46));

							EditorGUI.showMixedValue = mixedValue_scale[2];
							EditorGUI.BeginChangeCheck();
							GUI.SetNextControlName("ScaleDefaultValue");
							defaultVal = EditorGUILayout.FloatField(defaultVal, GUILayout.MaxWidth(44));
							if(EditorGUI.EndChangeCheck())
							{
								foreach(KeyValuePair<int, List<int>> kvp in selected)
									foreach(int i in kvp.Value)
										decalGroups[kvp.Key].decals[i].scale.z = defaultVal;
							}

						}
						// else
						{
							GUILayout.Space(5);
							GUI.enabled = scale_placement == Placement.Random;

							EditorGUI.showMixedValue = mixedValue_scale[0];
							EditorGUI.BeginChangeCheck();
							GUILayout.Label("Min", GUILayout.MaxWidth(30));
							GUI.SetNextControlName("ScaleMinValue");
							minVal = EditorGUILayout.FloatField(minVal, GUILayout.MaxWidth(44));
							if(EditorGUI.EndChangeCheck())
							{
								foreach(KeyValuePair<int, List<int>> kvp in selected)
									foreach(int i in kvp.Value)
										decalGroups[kvp.Key].decals[i].scale.x = minVal;
							}

							EditorGUI.showMixedValue = mixedValue_scale[1];
							EditorGUI.BeginChangeCheck();
							GUILayout.Label("Max", GUILayout.MaxWidth(30));
							GUI.SetNextControlName("ScaleMaxValue");
							maxVal = EditorGUILayout.FloatField(maxVal, GUILayout.MaxWidth(44));
							if(EditorGUI.EndChangeCheck())
							{
								foreach(KeyValuePair<int, List<int>> kvp in selected)
									foreach(int i in kvp.Value)
										decalGroups[kvp.Key].decals[i].scale.y = maxVal;
							}
						}

					GUILayout.EndHorizontal();
				}

				///*** Scale ****///
				GUI.enabled = true;
				EditorGUI.showMixedValue = false;
			} // organizational view
			else
			{
#if UNITY_WEBPLAYER
				GUI.enabled = false;
#endif
				/* ATLAS VIEW */
				Material mat = null;
				int key = -1;

				// Material and packing info
				if(selected.Count > 0)
				{
					key = selected.First().Key;
					DecalGroup decalGroup = decalGroups[key];
					packed = decalGroup.isPacked && decalGroup.material != null;
					mat = decalGroup.material;
				}

				GUI.enabled = false;
				EditorGUILayout.ObjectField("Group Material", mat, typeof(Material), false, GUILayout.MaxWidth(r.width-10));

#if !UNITY_WEBPLAYER
				GUI.enabled = selected.Count == 1;
#endif

				///*** Group Name ***///
				{
					EditorGUI.showMixedValue = selected.Count != 1;
					if(key > -1)
						name = decalGroups[key].name;
					EditorGUI.BeginChangeCheck();
					GUI.SetNextControlName("GroupName");
					if(selected.Count == 1)
						name = EditorGUILayout.TextField("Name", name);
					else
						EditorGUILayout.TextField("Name", name);
					if(EditorGUI.EndChangeCheck() && selected.Count == 1)
					{
						 decalGroups[key].name = name;
						 decalGroups[key].isPacked = false;
					}
				}

				///*** PackTextures settings padding ***///
				{
					int pad = selected.Count == 1 ? decalGroups[key].padding : 4;

					EditorGUI.BeginChangeCheck();
					if(selected.Count == 1)
						pad = EditorGUILayout.IntSlider("Padding", pad, 0, 12);
					else
						EditorGUILayout.IntSlider("Padding", pad, 0, 12);

					if(EditorGUI.EndChangeCheck())
					{
						foreach(KeyValuePair<int, List<int>> kvp in selected)
						{
							decalGroups[kvp.Key].padding = pad;
							decalGroups[kvp.Key].isPacked = false;
						}
					}
				}
			}

			EditorGUI.showMixedValue = false;

			GUI.enabled = true;
		GUI.EndGroup();

		if(decalView == DecalView.Atlas)
		{
#if UNITY_WEBPLAYER
			GUI.enabled = false;
#else
			GUI.enabled = !packed;
#endif
			Color bg = GUI.backgroundColor;

			if(selected.Count > 0)
			{
				GUI.backgroundColor = !packed ? PackedRed : Color.green;
			}
			else
			{
				bool anyUnpackedGroups = decalGroups.Any(x => !x.isPacked);
				GUI.backgroundColor = anyUnpackedGroups ? PackedRed : Color.green;
				GUI.enabled = anyUnpackedGroups;
			}

			if(GUI.Button(new Rect((r.x+r.width)-120, r.y+2, 116, 17),
				new GUIContent( (!packed ? (selected.Count < 1 ? "Pack All Decals" : "Pack Decals") : "Atlased"), "When a decal is selected, this button will show green for a packed group atlas and red if not packed.  When no decals are selected, clicking \"Pack Decals\" will atlas all groups that are not already packed.")))
			{
				if(selected.Count > 0)
				{
					foreach(KeyValuePair<int, List<int>> kvp in selected)
					{
						if(!decalGroups[kvp.Key].isPacked || decalGroups[kvp.Key].material == null)
						{
							database.PackTextures(kvp.Key);
							qdUtil.RefreshSceneDecals(decalGroups[kvp.Key]);
						}
					}
				}
				else
				{
					for(int i = 0; i < decalGroups.Count; i++)
					{
						if(!decalGroups[i].isPacked || decalGroups[i].material == null)
						{
							database.PackTextures(i);
							qdUtil.RefreshSceneDecals(decalGroups[i]);
						}
					}
				}
			}
			GUI.backgroundColor = bg;
		}
	}
#endregion

#region GUI Helpers

	private bool KeyListener()
	{
		// Listen for key shortcuts (delete, some other shortcut, etc)
		if(e.isKey && e.type == EventType.KeyUp)
		{
			if(GUI.GetNameOfFocusedControl().Equals("") && e.keyCode == KeyCode.Backspace || e.keyCode == KeyCode.Delete)
			{
				DeleteSelectedDecals();
				PruneGroups();
				return true;
			}

			if(e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
			{
				GUIUtility.keyboardControl = 0;
			}

			if(e.keyCode == KeyCode.Escape)
			{
				searchString = "";
				GUIUtility.keyboardControl = 0;
			}
		}

		if(e.type == EventType.ScrollWheel && e.alt)
		{
			imageSize -= (int)e.delta.y * SCROLL_MODIFIER;
			Repaint();
		}

		return false;
	}

	bool ListenForDragAndDrop(Rect bounds)
	{
		if(!bounds.Contains(e.mousePosition))
			return false;

		if( (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform || dragging) && DragAndDrop.objectReferences.Length > 0)
		{
			dragging = true;
			selected.Clear();

			Repaint();

			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

			if(Event.current.type == EventType.DragPerform)
			{
				DragAndDrop.AcceptDrag();

				List<QD.Decal> imgs = new List<QD.Decal>();

				foreach(Object img in DragAndDrop.objectReferences)
				{
					if(img is Texture2D)
					{
						if(decalGroups == null || !decalGroups.Exists(element => element.ContainsTexture((Texture2D)img)))
							imgs.Add(new QD.Decal((Texture2D)img));
					}
				}

				if(imgs.Count > 0)
					PerformDragDrop(imgs, mouseOver_groupIndex, mouseOver_textureIndex);

				return true;
			}
		}
		return false;
	}

	int dragOriginIndex = -1;
	Vector2 mouseOrigin = Vector2.zero;
	List<Vector2> dragMouseOffset;
	List<Texture2D> dragTextures;
	private struct Hover
	{
		public Rect bounds;
		public int groupIndex;
		public int textureIndex;
	}
	Hover hovering = new Hover();
	bool validDrag = false;

	void MouseListener()
	{
		// Figure out where the mouse is relative to the DecalGroup layout
		mouseOver_groupIndex = -1;
		mouseOver_textureIndex = -1;

		if(!textureDisplayRect.Contains(e.mousePosition))
			return;

		// first check hovering rect.  use this hovering struct because otherwise the gui
		// runs on/off when checking hover status (if it's over, move it, but then it's moved so
		// it's not over, and move back... cyclical logic ftw)
		if(dragging && hovering.bounds.Contains(mousePositionInGroupRect))
		{
			mouseOver_groupIndex = hovering.groupIndex;
			mouseOver_textureIndex = hovering.textureIndex;
		}
		else
		{
			if(textureDisplayRect.Contains(e.mousePosition))
			for(int i = 0; i < groupRects.Length; i++)
			{
				if( groupRects[i].Contains(mousePositionInGroupRect) )
				{
					mouseOver_groupIndex = i;
					for(int j = 0; j < decalRects[i].Length; j++)
					{
						if(decalRects[i][j].Contains(mousePositionInGroupRect))
							mouseOver_textureIndex = j;
					}
					break;
				}
			}
		}

		switch(Event.current.type)
		{
			case EventType.MouseDown:
				mouseOrigin = e.mousePosition;
				if(mouseOver_groupIndex > -1 && mouseOver_textureIndex > -1)
					validDrag = true;
				else
					validDrag = false;

				break;

			case EventType.MouseDrag:

				if(!validDrag) return;

				if(Vector2.Distance(mouseOrigin, e.mousePosition) > 7f && !dragging && mouseOver_groupIndex > -1 && mouseOver_textureIndex > -1)
				{
					dragging = true;

					dragOriginIndex = mouseOver_groupIndex;

					if(!selected.Contains(mouseOver_groupIndex, mouseOver_textureIndex))
					{
						selected.Clear();
						selected.Add(mouseOver_groupIndex, mouseOver_textureIndex);
					}

					dragTextures = new List<Texture2D>();
					dragMouseOffset = new List<Vector2>();

					foreach(KeyValuePair<int, List<int>> kvp in selected)
					{
						bool exit = false;
						foreach(int n in kvp.Value)
						{
							Rect r = decalRects[kvp.Key][n];
							dragMouseOffset.Add(new Vector2(r.x - mousePositionInGroupRect.x, r.y - mousePositionInGroupRect.y));
							dragTextures.Add(decalGroups[kvp.Key].decals[n].texture);
						}
						if(exit) break;
					}

				}

				if(dragging)
					Repaint();

				break;

			case EventType.Ignore:

				dragging = false;
				dragTextures = null;
				Repaint();
				break;

			case EventType.MouseUp:
				if(dragging)
				{
					dragTextures = null;
					List<QD.Decal> imgs = new List<QD.Decal>();
					foreach(KeyValuePair<int, List<int>> kvp in selected)
						foreach(int n in kvp.Value)
							imgs.Add( decalGroups[kvp.Key].decals[n] );

					// since we might remove some decals that are before the insert point,
					// this offset accounts for it and puts the new images in the right place even afeter
					// deletion
					int textureOffset = 0;
					if(selected.ContainsKey(mouseOver_groupIndex))
					{
						foreach(int n in selected[mouseOver_groupIndex])
							if(n < mouseOver_textureIndex)
								textureOffset++;
					}

					DeleteSelectedDecals();
					PerformDragDrop(imgs, mouseOver_groupIndex, mouseOver_textureIndex-textureOffset);
					PruneGroups();
				}

				if(!e.shift)
					selected.Clear();

				if(mouseOver_groupIndex > -1)
				{
					if(selected.ContainsKey(mouseOver_groupIndex))
					{
						if(mouseOver_textureIndex > -1)
						{
							if(!selected[mouseOver_groupIndex].Contains(mouseOver_textureIndex))
								selected[mouseOver_groupIndex].Add(mouseOver_textureIndex);
							else
								selected[mouseOver_groupIndex].Remove(mouseOver_textureIndex);
						}
					}
					else
					{
						if(mouseOver_textureIndex > -1)
							selected.Add(mouseOver_groupIndex, new List<int>() { mouseOver_textureIndex });
						else
							selected.Add(mouseOver_groupIndex, new List<int>());
					}

					GUIUtility.keyboardControl = 0;
					GUI.FocusControl("");
				}
				Repaint();
				break;

			case EventType.ContextClick:
				GenericMenu menu = new GenericMenu();
				menu.AddItem (new GUIContent("Open as Floating Window", ""), false, ContextOpenFloatingWindow);
				menu.AddItem (new GUIContent("Open as Dockable Window", ""), false, ContextOpenDockableWindow);
				menu.ShowAsContext ();
				e.Use();
				break;
		}
	}

	// DEBUG
	void PrintSelectedDecals()
	{
		foreach(KeyValuePair<int, List<int>> kvp in selected)
		{
			foreach(int i in kvp.Value)
				Debug.Log(decalGroups[kvp.Key].decals[i].ToString());
		}
	}

	void PerformDragDrop(List<QD.Decal> dec, int group, int index)
	{
		if(group > -1)
			database.AddDecals(dec, group, index, decalView);
		else
			database.AddDecals(dec, decalView);

		decalGroups = database.decalGroups;

		dragging = false;
	}

	private void OnValidateCommand(string command)
	{
		switch(command)
		{
			case "UndoRedoPerformed":
				decalGroups = database.decalGroups;
				Repaint();
				break;
		}
	}

	private void UndoRedoPerformed()
	{
		decalGroups = database.decalGroups;
		Repaint();
	}
#endregion

#region SceneGUI

	void OnSceneGUI(SceneView sceneView)
	{
		Event e = Event.current;

		#if UNITY_STANDALONE_OSX
		EventModifiers em = e.modifiers;	// `&=` consumes the event.
		if( (em &= EventModifiers.Shift) != EventModifiers.Shift )
			return;

		int controlID = EditorGUIUtility.GetControlID(FocusType.Passive);
		HandleUtility.AddDefaultControl(controlID);
		#endif

		#if UNITY_STANDALONE_OSX
		if( e.type == EventType.MouseUp && ((e.button == RIGHT_MOUSE_BUTTON && e.modifiers == EventModifiers.Shift) || (e.modifiers == (EventModifiers.Shift | EventModifiers.Control))) )
		#else
		if(e.type == EventType.MouseUp && e.button == RIGHT_MOUSE_BUTTON && e.modifiers == EventModifiers.Shift)
		#endif
		{
			if(selected.Count < 1 || decalView == DecalView.Atlas) return;

			int key = selected.Keys.ToList()[(int)Random.Range(0, selected.Count)];
			if(selected[key].Count < 1) return;
			int val = (int)Random.Range(0, selected[key].Count);

			int grpIndex = key;
			int texIndex = selected[key][val];

			QD.Decal decal = decalGroups[grpIndex].decals[texIndex];

			Texture2D tex = decal.texture;

			Material mat;

			/**
			 * Atlas hasn't been packed yet, but we still want decals to share a single material.
			 */
			if( !database.MaterialWithDecal(decal, out mat) )
			{
				// Make sure that atlasRect is 0,0,1,1 and packed == false;
				decal.isPacked = false;
				decal.atlasRect = new Rect(0, 0, 1, 1);

				GameObject[] existingDecals = qdUtil.FindDecalsWithTexture(tex);

				if(existingDecals == null || existingDecals.Length < 1)
				{
					mat = new Material( database.ShaderWithDecal(decal) );
					mat.mainTexture = tex;
				}
				else
				{
					mat = existingDecals[0].GetComponent<MeshRenderer>().sharedMaterial;
				}
			}

			Rect r = decal.isPacked ? decal.atlasRect : new Rect(0f, 0f, 1f, 1f);

			GameObject decalGo;
			Transform hit;

			if(PlaceDecal(e.mousePosition, mat, r,
				decal.rotationPlacement == Placement.Random ? Random.Range(decal.rotation.x, decal.rotation.y) : decal.rotation.z,
				decal.scalePlacement == Placement.Random ? Random.Range(decal.scale.x, decal.scale.y) : decal.scale.z,
				out decalGo,
				out hit))
			{
				if(qd_Preferences.GetBool(qd_Constant.ParentToHitTransform))
				{
					decalGo.transform.parent = hit;

					if(decalGo.transform.localScale != Vector3.one)
						decalGo.GetComponent<qd_Decal>().FreezeTransform();
				}
				else
				{
					 decalGo.transform.parent = DecalParent.transform;
				}

				Selection.objects = new Object[1] { decalGo };
				SceneView.RepaintAll();

				// DO NOT USE THE EVENT
				// unitay needs shift clicks to register in order
				// to release the view mode shortcut
			}
		}
	}
#endregion


#region Decal Placement

	bool PlaceDecal(Vector2 mousePosition, Material mat, Rect uvCoordinates, float rotation, float scale, out GameObject decal, out Transform hitTransform)
	{
		decal = null;
		hitTransform = null;

		GameObject nearest = HandleUtility.PickGameObject(mousePosition, false);

		if(	nearest == null )
			return false;

		Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
		RaycastHit hit;
		MeshFilter mf = nearest.GetComponent<MeshFilter>();
		TerrainCollider tc = nearest.GetComponent<TerrainCollider>();

		if( mf != null && mf.sharedMesh != null)
		{
			Mesh msh = mf.sharedMesh;

			// Use IntersectRayMesh because no other raycast is capable of intersecting non-collider objects.
			object[] parameters = new object[] { ray, msh, nearest.transform.localToWorldMatrix, null };
			if(IntersectRayMesh == null) IntersectRayMesh = typeof(HandleUtility).GetMethod("IntersectRayMesh", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);
			object result = IntersectRayMesh.Invoke(this, parameters);

			if ( (bool)result )
				hit = (RaycastHit)parameters[3];
			else
				return false;
		}
		else if( tc != null )
		{
			if( !Physics.Raycast(ray, out hit) )
				return false;
		}
		else
		{
			return false;
		}

		hitTransform = nearest.transform;

		decal = qd_Mesh.CreateDecal(mat, uvCoordinates, scale);
		Undo.RegisterCreatedObjectUndo(decal, "Place Decal");

		StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.LightmapStatic | StaticEditorFlags.OccludeeStatic;
		GameObjectUtility.SetStaticEditorFlags(decal, flags);

		decal.transform.position = hit.point + hit.normal.normalized * .01f;
		decal.transform.localRotation = Quaternion.LookRotation(hit.normal);

		Vector3 rot = decal.transform.eulerAngles;
		rot.z += rotation;
		decal.transform.localRotation = Quaternion.Euler(rot);

		return true;
	}
#endregion

#region Decal Management

	private void DeleteSelectedDecals()
	{
		database.DeleteDecals(selected, decalView);
		decalGroups = database.decalGroups;

		selected.Clear();
		Event.current.Use();
	}

	private QD.Decal FirstSelectedDecal()
	{
		if(selected.Count < 1) return null;

		int key = selected.First().Key;
		return decalGroups[key].decals[selected[key][0]];
	}

	private void PruneGroups()
	{
		database.PruneGroups();
		decalGroups = database.decalGroups;
	}
#endregion

#region Internal

	private qd_Database InitDatabase()
	{
		qd_Database db = ScriptableObject.CreateInstance<qd_Database>();

		if(!Directory.Exists(Path.GetDirectoryName(QD_DATABASE_PATH)))
			Directory.CreateDirectory(Path.GetDirectoryName(QD_DATABASE_PATH));
		AssetDatabase.CreateAsset(db, QD_DATABASE_PATH);
		return db;
	}

	private void SetTransparentImage(Texture2D img)
	{
		if(transparentImage != null)
			DestroyImmediate(transparentImage);

		transparentImage = new Texture2D(img.width, img.height, img.format, false);

		Color[] pix = img.GetPixels(0);
		for(int i = 0; i < pix.Length; i++)
			pix[i].a = Mathf.Clamp(pix[i].a - .7f, 0f, 1f);

		transparentImage.SetPixels(pix, 0);
		transparentImage.Apply();
	}
#endregion
}
