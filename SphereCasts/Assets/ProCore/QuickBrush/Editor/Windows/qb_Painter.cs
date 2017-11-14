//	QuickBrush: Prefab Placement Tool
//	by PlayTangent
//	all rights reserved
//	www.procore3d.com

using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class qb_Painter : EditorWindow
{
	[MenuItem ("Tools/QuickBrush/QuickBrush Window", false, 21)]
	public static void ShowWindow()
	{
		window = EditorWindow.GetWindow<qb_Painter>(EditorPrefs.GetBool("qb_isFloating"), "QuickBrush", true);

		window.position = new Rect(50, 50, 284, 600);
		window.minSize = new Vector2(284f, 300f);
		window.maxSize = new Vector2(284f, 800f);
	}

	private static void OpenContextMenu()
	{
		GenericMenu menu = new GenericMenu();

		menu.AddItem (new GUIContent("Open as Floating Window", ""), false, () => { SetWindowFloating(true); } );
		menu.AddItem (new GUIContent("Open as Dockable Window", ""), false, () => { SetWindowFloating(false); } );

		menu.ShowAsContext ();
	}

	static void SetWindowFloating(bool floating)
	{
		EditorPrefs.SetBool("qb_isFloating", floating);
		EditorWindow.GetWindow<qb_Painter>().Close();
		ShowWindow();
	}

	#region Variable Declarations

	static qb_Painter				window;
	static string 					directory;
	static BrushMode 				brushMode;
	static bool						brushDirection = true;		//Positive or negative - Indicates whether we are placing or erasing
	static bool						placementModifier = false;
	private bool					toolActive = true;

	static Texture2D				removePrefabXTexture_normal;
	static Texture2D				removePrefabXTexture_hover;
	static Texture2D				addPrefabTexture;
	static Texture2D				addPrefabFieldTexture;
	static Texture2D				selectPrefabCheckTexture_off;
	static Texture2D				selectPrefabCheckTexture_on;
	static Texture2D				selectPrefabCheckTexture_active;
	static Texture2D				prefabFieldBackgroundTexture;
	static Texture2D				brushIcon_Active;
	static Texture2D				brushIcon_Inactive;
	static Texture2D				eraserIcon_Active;
	static Texture2D				eraserIcon_Inactive;
	static Texture2D				brushIcon_Locked;

	static Texture2D				placementIcon_Active;
	static Texture2D				loadBrushIcon;
	static Texture2D				loadBrushIcon_hover;
	static Texture2D				loadBrushIconLarge;
	static Texture2D				loadBrushIconLarge_hover;
	static Texture2D				prefabPaneDropdownIcon_closed_normal;
	static Texture2D				prefabPaneDropdownIcon_closed_active;
	static Texture2D				prefabPaneDropdownIcon_open_normal;
	static Texture2D				prefabPaneDropdownIcon_open_active;

	static Texture2D				saveIcon;
	static Texture2D				saveIcon_hover;
	static Texture2D				clearBrushIcon;
	static Texture2D				clearBrushIcon_hover;
	static Texture2D				savedBrushIcon;
	static Texture2D				savedBrushIcon_Active;
	static Texture2D				resetSliderIcon;
	static Texture2D				resetSliderIcon_hover;

	static Texture2D				templateActiveIcon_on;
	static Texture2D				templateActiveIcon_off;
	static Texture2D				templateActiveIcon_active;

	static Texture2D				templateDirtyAsterisk;

	static Texture2D				templateTabBackground;
	static Texture2D				templateTabBackground_inactive;
	#endregion

	#region Preference Vars
	static bool prefsLoaded = false;
	static bool prefs_enableLog = false;
	#endregion

	private void OnEnable()
	{
		window = this;
		this.wantsMouseMove = true;

		SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
		SceneView.onSceneGUIDelegate += this.OnSceneGUI;

		directory = qb_Utility.GetHeadDirectory();

		LoadTextures();
		BuildStyles();

		EnableMenu();
	}

	private void OnDisable()
	{
		DisableMenu();
	}

	void EnableMenu()
	{
		UpdateGroups();
		LoadTempSlots();
		LoadToolState();
		UpdateAreActive();;

		brushDirection = true;
		brushMode = BrushMode.Off;

		ranEnable = true;
	}

	void DisableMenu()
	{
		brushDirection = true;
		brushMode = BrushMode.Off;
		//DebugClearData();

		SaveTempSlots();
		SaveToolState();
	}

	[PreferenceItem("QuickBrush")]
	private static void PreferencesGUI()
	{
		// Load the preferences
		if (!prefsLoaded)
		{
			LoadPreferences();
		}
		// Preferences GUI
		prefs_enableLog = EditorGUILayout.Toggle("Enable QB Log Messages", prefs_enableLog);

		// Save the preferences
		if (GUI.changed)
		{
			SavePreferences();
		}
	}

	static void LoadPreferences()
	{
		prefs_enableLog = EditorPrefs.GetBool ("QB_enableLog", false);

		prefsLoaded = true;
	}

	static void SavePreferences()
	{
		EditorPrefs.SetBool("QB_enableLog", prefs_enableLog);
	}

	private void SaveToolState()
	{
		EditorPrefs.SetInt("QB_templateIndex", liveTemplateIndex);
	}

	private void LoadToolState()
	{	//Debug.Log("Reloading Tool State!");
		int tempTemplateIndex = EditorPrefs.GetInt("QB_templateIndex", -1);
		int count = brushTemplates.Length;//EditorPrefs.GetInt("QB_templateCount",0);

		if (count != 0)
		{
			//Template Index Fallbacks Start
			if (tempTemplateIndex == -1 || tempTemplateIndex >= count || window.brushTemplates[tempTemplateIndex] == null)
			{
				for (int i = 0; i < count; i++)
				{
					if (window.brushTemplates[i] != null)
					{
						tempTemplateIndex = i;
						break;
					}
				}
			}
			//Template Index Fallbacks End
		}

		else
			tempTemplateIndex = -1;

		//if(tempTemplateIndex != -1)
		window.SwitchToTab(tempTemplateIndex);
	}
	//Debug Function Only
	/*	private void DebugClearData()
		{
			for(int i = 0; i < 6;i++)
			{
				CloseTab(i);
			}
		}
	*/
	#region Foldouts
	[SerializeField] private bool			brushSettingsFoldout = 	false;
	[SerializeField] private bool			rotationFoldout = 		false;
	[SerializeField] private bool			scaleFoldout = 			false;
	[SerializeField] private bool			positionFoldout = 		false;
	[SerializeField] private bool			sortingFoldout =		false;
	[SerializeField] private bool			eraserFoldout = 		false;
	[SerializeField] private bool			prefabPaneOpen =		false;
	#endregion

	#region Live Vars
	//Templates
	[SerializeField] private qb_Template[]	brushTemplates;// =		//new qb_Template[6];
	[SerializeField] private qb_Template	liveTemplate =			new qb_Template();
	[SerializeField] private int			liveTemplateIndex =		-1;
	static qb_TemplateSignature[]			templateSignatures;

	//Menu Scrolling
	[SerializeField] private Vector2 		topScroll =				Vector2.zero;

	//Painting
	static bool 			paintable =				false;			//set by the mouse raycast to control whether an object can be painted on
	static qb_Point			cursorPoint =			new qb_Point();

	//Groups
	static List<qb_Group> 	groups = 				new List<qb_Group>();
	static List<string>		groupNames = 			new List<string>();

//	static qb_Group			curGroup;
	static string			newGroupName = 			"";

	//Layers
	//private LayerMask 		layersMasked =			0;

	//Prefab Section
	private Vector2			prefabFieldScrollPosition;
	private Object			newPrefab;
	private Object			previousPrefab;

	//Tool Tip
	private string			curTip =				string.Empty;
	private bool			drawCurTip =			false; //this is set false on each redraw and checked at the end of OnGUI - set by DoTipCheck if a control with a tip is currently moused over
	#endregion

	#region OnGUI Variables
	static Texture2D		previewTexture;
	private bool			ranEnable	= 			false;
	private bool			builtStyles =			false;

	private bool			clearSelection = 		true;
	static bool				prevPaintToLayer =		false;
	static bool				prevPaintToSelection =	false;
	#endregion

	void OnGUI()
	{
		Event e = Event.current;

		if (e.type == EventType.ContextClick)
			OpenContextMenu();

		if (ranEnable == false)
			OnEnable();

		if (!builtStyles)
			BuildStyles();

		CaptureInput();

		drawCurTip = false;

		EditorGUILayout.Space();

		EditorGUILayout.BeginVertical(masterVerticalStyle, GUILayout.Width(280)); //Begin Master Vertical

		EditorGUILayout.BeginHorizontal();//Brush Toggles Section Begin

		Texture2D brushIndicatorTexture = null;

		switch (brushMode)
		{
		case BrushMode.Off:

			if (toolActive == true)
			{
				if (brushDirection == true)
				{
					brushIndicatorTexture = brushIcon_Inactive;
				}
				else //if(brushDirection == false)
				{
					brushIndicatorTexture = eraserIcon_Inactive;
				}
			}

			else
				brushIndicatorTexture = brushIcon_Locked;
			break;

		case BrushMode.On:

			if (toolActive == true)
			{
				if (placementModifier)
					brushIndicatorTexture = placementIcon_Active;

				else
				{
					if (brushDirection == true)
					{
						brushIndicatorTexture = brushIcon_Active;
					}
					else //if(brushDirection == false)
					{
						brushIndicatorTexture = eraserIcon_Active;
					}
				}
			}

			else
				brushIndicatorTexture = brushIcon_Locked;
			break;
		}

		if (GUILayout.Button(brushIndicatorTexture, picLabelStyle, GUILayout.Width(32), GUILayout.Height(32)))
		{
			// instead of doing this properly with an enum, this
			// bit allows clicking the on/off button to toggle between
			// brush place, brush remove, and off modes.
			if (toolActive && brushDirection)
			{
				brushDirection = false;
			}
			else if (toolActive && !brushDirection)
			{
				toolActive = false;
			}
			else
			{
				toolActive = true;
				brushDirection = true;
			}

			brushMode = BrushMode.Off;
		}
		DoTipCheck("Brush/Eraser Indicator & master on/off switch" + System.Environment.NewLine + "Click to turn QB on/off and free shortcut keys");

		EditorGUI.BeginDisabledGroup(true);
		GUILayout.Label("Use Brush:" + System.Environment.NewLine + "Precise Place:" + System.Environment.NewLine + "Toggle Eraser:", tipLabelStyle, GUILayout.Width(90), GUILayout.Height(34)); DoTipCheck("Brush On/Off Indicator" + System.Environment.NewLine + "hold ctrl to paint");
		GUILayout.Label("ctrl+click/drag mouse" + System.Environment.NewLine + "ctrl+shift+click/drag mouse" + System.Environment.NewLine + "ctrl+x" , tipLabelStyle, GUILayout.Width(146), GUILayout.Height(32)); DoTipCheck("Brush On/Off Indicator" + System.Environment.NewLine + "hold ctrl to paint");
		EditorGUI.EndDisabledGroup();
		EditorGUILayout.EndHorizontal(); // Brush Toggles Section End

		#region Prefab Picker
		EditorGUI.BeginDisabledGroup(liveTemplate.live == false);//Overall Disable Start

		EditorGUILayout.BeginHorizontal(prefabPanelCrop, GUILayout.Width(280));

		if (liveTemplate.prefabGroup.Length == 0)
		{
			EditorGUILayout.BeginVertical(GUILayout.Height(78));
			PrefabDragBox(274, prefabAddField_Span, "Drag & Drop Prefabs Here");	DoTipCheck("Drag & Drop Prefab Here To Add");
			EditorGUILayout.EndVertical();
		}

		else
		{
			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.BeginVertical();
			PrefabDragBox(30, prefabAddField_Small, ""); DoTipCheck("Drag & Drop Prefab Here To Add");

			if (prefabPaneOpen == false)
			{
				EditorGUI.BeginDisabledGroup(liveTemplate.prefabGroup.Length < 4);
				if (GUILayout.Button("", picButton_PrefabPaneDropdown_Closed, GUILayout.Height(16), GUILayout.Width(30)))
				{
					prefabPaneOpen = true;
					prefabFieldScrollPosition = new Vector2(0, 0);
				} DoTipCheck("Expand Prefab Pane");
				EditorGUI.EndDisabledGroup();
			}

			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();

			if (prefabPaneOpen == false) // Default Scrollable View
			{
				prefabFieldScrollPosition = EditorGUILayout.BeginScrollView(prefabFieldScrollPosition, GUILayout.Height(78), GUILayout.Width(240) ); //, GUILayout.Width(160));
				EditorGUILayout.BeginHorizontal();
				//Prefab Objects can be dragged or selected in this horizontal list
				for (int i = 0; i < liveTemplate.prefabGroup.Length; i++)
				{
					PrefabTile(i);
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndScrollView();
			}

			else // Expanded Pane View
			{
				EditorGUILayout.BeginVertical();
				EditorGUILayout.BeginHorizontal();
				for (int i = 0; i < liveTemplate.prefabGroup.Length; i++)
				{
					if (i % 3 == 0)
					{
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.BeginHorizontal();
					}
					PrefabTile(i);
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
			}

		}

		EditorGUILayout.EndHorizontal();

		if (prefabPaneOpen == true)
		{
			if (GUILayout.Button("", picButton_PrefabPaneDropdown_Open, GUILayout.Height(16), GUILayout.Width(276)))
			{
				prefabPaneOpen = false;
				prefabFieldScrollPosition = new Vector2(0, 0);
			} DoTipCheck("Collapse Prefab Pane to Scrollable");
		}

		EditorGUILayout.Space();
		#endregion

		topScroll = EditorGUILayout.BeginScrollView(topScroll, GUILayout.Width(280));
		EditorGUILayout.BeginVertical(GUILayout.Width(260));

		#region	Stroke Properties
		brushSettingsFoldout = EditorGUILayout.Foldout(brushSettingsFoldout, "Brush Settings:"); DoTipCheck("Brush and Stroke settings");

		if (brushSettingsFoldout == true)
		{
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.BeginVertical(menuBlockStyle, GUILayout.Width(260));

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Brush Radius", sliderLabelStyle, GUILayout.Width(100)); DoTipCheck("The Size of the brush");
			liveTemplate.brushRadius = EditorGUILayout.Slider(liveTemplate.brushRadius, liveTemplate.brushRadiusMin, liveTemplate.brushRadiusMax); DoTipCheck("The Size of the brush");
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Min", GUILayout.Width(70)); DoTipCheck("Minimum Slider Value");
			float tryRadiusMin = EditorGUILayout.FloatField(liveTemplate.brushRadiusMin, floatFieldCompressedStyle); DoTipCheck("Minimum Slider Value");
			liveTemplate.brushRadiusMin = tryRadiusMin < liveTemplate.brushRadiusMax ? tryRadiusMin : liveTemplate.brushRadiusMax;

			EditorGUILayout.LabelField("Max", GUILayout.Width(70)); DoTipCheck("Maximum Slider Value");
			float tryRadiusMax = EditorGUILayout.FloatField(liveTemplate.brushRadiusMax, floatFieldCompressedStyle); DoTipCheck("Maximum Slider Value");
			liveTemplate.brushRadiusMax = tryRadiusMax > liveTemplate.brushRadiusMin ? tryRadiusMax : liveTemplate.brushRadiusMin;
			EditorGUILayout.EndHorizontal();
			/*
			EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Scatter Amount",GUILayout.Width(100)); DoTipCheck("How closely should scattering match brush radius");
				liveTemplate.scatterRadius = EditorGUILayout.Slider(liveTemplate.scatterRadius,0f,1f); DoTipCheck("How closely should scattering match total brush radius");
			EditorGUILayout.EndHorizontal();
			*/
			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical(menuBlockStyle, GUILayout.Width(260));

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Stroke Spacing", GUILayout.Width(100)); DoTipCheck("Distance between brush itterations");
			liveTemplate.brushSpacing = EditorGUILayout.Slider(liveTemplate.brushSpacing, liveTemplate.brushSpacingMin, liveTemplate.brushSpacingMax); DoTipCheck("Distance between brush itterations");
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Min", GUILayout.Width(70)); DoTipCheck("Minimum Slider Value");
			float trySpacingMin = EditorGUILayout.FloatField(liveTemplate.brushSpacingMin, floatFieldCompressedStyle); DoTipCheck("Minimum Slider Value");
			liveTemplate.brushSpacingMin = trySpacingMin < liveTemplate.brushSpacingMax ? trySpacingMin : liveTemplate.brushSpacingMax;

			EditorGUILayout.LabelField("Max", GUILayout.Width(70)); DoTipCheck("Maximum Slider Value");
			float trySpacingMax = EditorGUILayout.FloatField(liveTemplate.brushSpacingMax, floatFieldCompressedStyle); DoTipCheck("Maximum Slider Value");
			liveTemplate.brushSpacingMax = trySpacingMax > liveTemplate.brushSpacingMin ? trySpacingMax : liveTemplate.brushSpacingMin;
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();

			if (EditorGUI.EndChangeCheck())
			{
				liveTemplate.dirty = true;
			}

		}
		#endregion

		EditorGUILayout.Space();

		sortingFoldout = EditorGUILayout.Foldout(sortingFoldout, "Sorting Settings:"); DoTipCheck("Grouping and Layer settings");
		if (sortingFoldout == true)
		{
			EditorGUI.BeginChangeCheck();

			#region Layers
			EditorGUILayout.BeginVertical(menuBlockStyle, GUILayout.Width(260));
			//A toggle determining whether to isolate painting to specific layers
			prevPaintToLayer = liveTemplate.paintToLayer;
			liveTemplate.paintToLayer = EditorGUILayout.Toggle("Paint to Layer", liveTemplate.paintToLayer, EditorStyles.toggle); DoTipCheck("Restrict painting to specific layers?");

			if (prevPaintToLayer != liveTemplate.paintToLayer)
			{
				UpdateCompoundPaintToLayer();
				UpdateCompoundLayerMask();
			}
			//A dropdown where the user can check off which layers to paint to
			EditorGUI.BeginDisabledGroup(!liveTemplate.paintToLayer);

			EditorGUILayout.BeginHorizontal();

			//string layerDisplayName = "Nothing";//string.empty;
			EditorGUILayout.LabelField("Choose Layers", GUILayout.Width(146)); DoTipCheck("Choose the layers to paint onto");

			if (GUILayout.Button((liveTemplate.layerText).ToString(), groupMenuDropdownStyle, GUILayout.Width(102)))
			{
				RenderLayerMenu(Event.current);
			} DoTipCheck("Choose the layers to paint onto");

			EditorGUILayout.EndHorizontal();

			EditorGUI.EndDisabledGroup();

			prevPaintToSelection = liveTemplate.paintToSelection;
			liveTemplate.paintToSelection = EditorGUILayout.Toggle("Restrict to Selection", liveTemplate.paintToSelection); DoTipCheck("Restrinct painting to selected objects in the scene - stacks with Layer Settings");

			if (prevPaintToSelection != liveTemplate.paintToSelection)
				UpdateCompoundPaintToSelection();

			EditorGUILayout.EndVertical();
			#endregion

			#region Groups
			EditorGUILayout.BeginVertical(menuBlockStyle, GUILayout.Width(260));

			liveTemplate.groupObjects = EditorGUILayout.Toggle("Group Placed Objects", liveTemplate.groupObjects); DoTipCheck("Parent placed objects to an in-scene group object?");

			EditorGUI.BeginDisabledGroup(!liveTemplate.groupObjects);

			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.LabelField("Choose Existing Group", GUILayout.Width(146)); DoTipCheck("Choose a group that already exists in the scene");

			string curGroupName = "Nothing";

			if (liveTemplate.groupName != string.Empty)
				curGroupName = liveTemplate.groupName;

			if (GUILayout.Button(curGroupName, groupMenuDropdownStyle, GUILayout.Width(102)))
			{
				RenderGroupMenu(Event.current);
			} DoTipCheck("Choose a group that already exists in the scene");

			EditorGUILayout.EndHorizontal();

			if (EditorGUI.EndChangeCheck())
			{
				liveTemplate.dirty = true;
			}

			EditorGUILayout.BeginHorizontal();
			newGroupName = EditorGUILayout.TextField("Name New Group", newGroupName, GUILayout.Width(210)); DoTipCheck("Enter a name for a new group you'd like to add");

			EditorGUI.BeginDisabledGroup(newGroupName == "");
			if (GUILayout.Button("Add", GUILayout.Width(38)))
			{
				clearSelection = true;

				if (GroupWithNameExists(newGroupName))
				{
					EditorUtility.DisplayDialog("Group Name Conflict", "A Group named '" + newGroupName + "' already exists. Please choose a different name for your new group." , "Ok");
					newGroupName = "";
				}
				else
				{
					qb_Group newGroup = CreateGroup(newGroupName);
					liveTemplate.groupName = newGroupName;
					liveTemplate.curGroup = newGroup;

					newGroupName = "";
				}

			} DoTipCheck("Create your newly named group in the scene");
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.EndHorizontal();

			EditorGUI.EndDisabledGroup();

			EditorGUILayout.EndVertical();
			#endregion
		}

		EditorGUILayout.Space();

		#region Rotation
		rotationFoldout = EditorGUILayout.Foldout(rotationFoldout, "Object Rotation:"); DoTipCheck("Settings for Offsetting the rotation of placed objects");
		if (rotationFoldout == true)
		{
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.BeginVertical();

			EditorGUILayout.BeginVertical(menuBlockStyle, GUILayout.Width(260));
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Align to Surface", GUILayout.Width(130)); DoTipCheck("Placed objects orient based on the surface normals of the painting surface");
			liveTemplate.alignToNormal = EditorGUILayout.Toggle(liveTemplate.alignToNormal, GUILayout.Width(14)); DoTipCheck("Placed objects orient based on the surface normals of the painting surface");
			GUILayout.Space(42);
			EditorGUI.BeginDisabledGroup(!liveTemplate.alignToNormal);
			EditorGUILayout.LabelField("Flip", GUILayout.Width(40)); DoTipCheck("Flip object's up axis along the surface");
			liveTemplate.flipNormalAlign = EditorGUILayout.Toggle(liveTemplate.flipNormalAlign, GUILayout.Width(14)); DoTipCheck("Flip object's up axis along the surface");
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Align to Stroke", GUILayout.Width(130)); DoTipCheck("Placed objects face in the direction of painting");
			liveTemplate.alignToStroke = EditorGUILayout.Toggle(liveTemplate.alignToStroke, GUILayout.Width(14)); DoTipCheck("Placed objects face in the direction of painting");
			GUILayout.Space(42);
			EditorGUI.BeginDisabledGroup(!liveTemplate.alignToStroke);
			EditorGUILayout.LabelField("Flip", GUILayout.Width(40)); DoTipCheck("Flip object's forward axis along the stroke");
			liveTemplate.flipStrokeAlign = EditorGUILayout.Toggle(liveTemplate.flipStrokeAlign, GUILayout.Width(14)); DoTipCheck("Flip object's forward axis along the stroke");
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.BeginVertical(menuBlockStyle, GUILayout.Width(260));

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Offset X", GUILayout.Width(106)); DoTipCheck("Limits (in degrees) to randomly offset object rotation around the X axis");
			EditorGUILayout.BeginHorizontal(saveIconContainerStyle);
			if (GUILayout.Button("", picButton_ResetSlider, GUILayout.Width(16), GUILayout.Height(16)))
			{	liveTemplate.rotationRangeMin.x = 0f;
				liveTemplate.rotationRangeMax.x = 0f;
				liveTemplate.dirty = true;
				clearSelection = true;

			} DoTipCheck("Reset slider to 0");
			EditorGUILayout.EndHorizontal();
			float inputRotMinX = (float)EditorGUILayout.IntField((int)liveTemplate.rotationRangeMin.x, GUILayout.Width(32));
			liveTemplate.rotationRangeMin.x = inputRotMinX < liveTemplate.rotationRangeMax.x ? inputRotMinX : liveTemplate.rotationRangeMax.x; DoTipCheck("Limits (in degrees) to randomly offset object rotation around the X axis");
			EditorGUILayout.MinMaxSlider(ref liveTemplate.rotationRangeMin.x, ref liveTemplate.rotationRangeMax.x, -180f, 180); DoTipCheck("Limits (in degrees) to randomly offset object rotation around the X axis");
			liveTemplate.rotationRangeMin.x = (float)System.Math.Round(liveTemplate.rotationRangeMin.x, 0);
			liveTemplate.rotationRangeMax.x = (float)System.Math.Round(liveTemplate.rotationRangeMax.x, 0);
			float inputRotMaxX = (float)EditorGUILayout.IntField((int)liveTemplate.rotationRangeMax.x, GUILayout.Width(32));
			liveTemplate.rotationRangeMax.x = inputRotMaxX > liveTemplate.rotationRangeMin.x ? inputRotMaxX : liveTemplate.rotationRangeMin.x; DoTipCheck("Limits (in degrees) to randomly offset object rotation around the X axis");
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Offset Y (up)", GUILayout.Width(106)); DoTipCheck("Limits (in degrees) to randomly offset object rotation around the Y axis");
			EditorGUILayout.BeginHorizontal(saveIconContainerStyle);
			if (GUILayout.Button("", picButton_ResetSlider, GUILayout.Width(16), GUILayout.Height(16)))
			{	liveTemplate.rotationRangeMin.y = 0f;
				liveTemplate.rotationRangeMax.y = 0f;
				liveTemplate.dirty = true;
				clearSelection = true;

			} DoTipCheck("Reset slider to 0");
			EditorGUILayout.EndHorizontal();
			float inputRotMinY = (float)EditorGUILayout.IntField((int)liveTemplate.rotationRangeMin.y, GUILayout.Width(32));
			liveTemplate.rotationRangeMin.y = inputRotMinY < liveTemplate.rotationRangeMax.y ? inputRotMinY : liveTemplate.rotationRangeMax.y; DoTipCheck("Limits (in degrees) to randomly offset object rotation around the Y axis");
			EditorGUILayout.MinMaxSlider(ref liveTemplate.rotationRangeMin.y, ref liveTemplate.rotationRangeMax.y, -180f, 180); DoTipCheck("Limits (in degrees) to randomly offset object rotation around the Y axis");
			liveTemplate.rotationRangeMin.y = (float)System.Math.Round(liveTemplate.rotationRangeMin.y, 0);
			liveTemplate.rotationRangeMax.y = (float)System.Math.Round(liveTemplate.rotationRangeMax.y, 0);
			float inputRotMaxY = (float)EditorGUILayout.IntField((int)liveTemplate.rotationRangeMax.y, GUILayout.Width(32));
			liveTemplate.rotationRangeMax.y = inputRotMaxY > liveTemplate.rotationRangeMin.y ? inputRotMaxY : liveTemplate.rotationRangeMin.y; DoTipCheck("Limits (in degrees) to randomly offset object rotation around the Y axis");
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Offset Z", GUILayout.Width(106)); DoTipCheck("Limits (in degrees) to randomly offset object rotation around the Z axis");
			EditorGUILayout.BeginHorizontal(saveIconContainerStyle);
			if (GUILayout.Button("", picButton_ResetSlider, GUILayout.Width(16), GUILayout.Height(16)))
			{	liveTemplate.rotationRangeMin.z = 0f;
				liveTemplate.rotationRangeMax.z = 0f;
				liveTemplate.dirty = true;
				clearSelection = true;

			} DoTipCheck("Reset slider to 0");
			EditorGUILayout.EndHorizontal();
			float inputRotMinZ = (float)EditorGUILayout.IntField((int)liveTemplate.rotationRangeMin.z, GUILayout.Width(32));
			liveTemplate.rotationRangeMin.z = inputRotMinZ < liveTemplate.rotationRangeMax.z ? inputRotMinZ : liveTemplate.rotationRangeMax.z; DoTipCheck("Limits (in degrees) to randomly offset object rotation around the Z axis");
			EditorGUILayout.MinMaxSlider(ref liveTemplate.rotationRangeMin.z, ref liveTemplate.rotationRangeMax.z, -180f, 180); DoTipCheck("Limits (in degrees) to randomly offset object rotation around the Z axis");
			liveTemplate.rotationRangeMin.z = (float)System.Math.Round(liveTemplate.rotationRangeMin.z, 0);
			liveTemplate.rotationRangeMax.z = (float)System.Math.Round(liveTemplate.rotationRangeMax.z, 0);
			float inputRotMaxZ = (float)EditorGUILayout.IntField((int)liveTemplate.rotationRangeMax.z, GUILayout.Width(32));
			liveTemplate.rotationRangeMax.z = inputRotMaxZ > liveTemplate.rotationRangeMin.z ? inputRotMaxZ : liveTemplate.rotationRangeMin.z; DoTipCheck("Limits (in degrees) to randomly offset object rotation around the Z axis");

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();

			if (EditorGUI.EndChangeCheck())
			{
				liveTemplate.dirty = true;
			}
		}
		#endregion

		EditorGUILayout.Space();

		#region Position
		positionFoldout = EditorGUILayout.Foldout(positionFoldout, "Object Position:"); DoTipCheck("Settings for Offsetting the rotation of placed objects");
		if (positionFoldout == true)
		{
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.BeginVertical(menuBlockStyle, GUILayout.Width(260));

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Offset X", GUILayout.Width(130)); DoTipCheck("Offset final placement position along local X axis");
			liveTemplate.positionOffset.x = EditorGUILayout.FloatField(liveTemplate.positionOffset.x); DoTipCheck("Offset final placement position along local X axis");
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Offset Y (Up/Down)", GUILayout.Width(130)); DoTipCheck("Offset final placement position along local Y axis");
			liveTemplate.positionOffset.y = EditorGUILayout.FloatField(liveTemplate.positionOffset.y); DoTipCheck("Offset final placement position along local Y axis");
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Offset Z", GUILayout.Width(130)); DoTipCheck("Offset final placement position along local Z axis");
			liveTemplate.positionOffset.z = EditorGUILayout.FloatField(liveTemplate.positionOffset.z); DoTipCheck("Offset final position placement along local Z axis");
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndHorizontal();

			if (EditorGUI.EndChangeCheck())
			{
				liveTemplate.dirty = true;
			}
		}
		#endregion

		EditorGUILayout.Space();

		#region Scale
		scaleFoldout = EditorGUILayout.Foldout(scaleFoldout, "Object Scale:", EditorStyles.foldout); DoTipCheck("Settings for Offsetting the scale of placed objects");
		if (scaleFoldout == true)
		{
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.BeginHorizontal();

			liveTemplate.scaleUniform = EditorGUILayout.Toggle(liveTemplate.scaleUniform, toggleButtonStyle, GUILayout.Width(15)); DoTipCheck("Placed models are scaled the same on all axes");
			GUILayout.Label("Uniform Scale"); DoTipCheck("Placed models are scaled the same on all axes");

			EditorGUILayout.EndHorizontal();

			EditorGUI.BeginDisabledGroup(!liveTemplate.scaleUniform);

			EditorGUILayout.BeginHorizontal(GUILayout.Width(260));

			EditorGUILayout.BeginVertical(menuBlockStyle, GUILayout.Width(78));

			EditorGUILayout.LabelField(liveTemplate.scaleRandMinUniform.ToString("0.00") + " to " + liveTemplate.scaleRandMaxUniform.ToString("0.00"), GUILayout.Width(78)); DoTipCheck("Random Scaling Range Split Slider (Min/Max)");
			EditorGUILayout.MinMaxSlider(ref liveTemplate.scaleRandMinUniform, ref liveTemplate.scaleRandMaxUniform, liveTemplate.scaleMin, liveTemplate.scaleMax); DoTipCheck("Random Scaling Range Split Slider (Min/Max)");

			liveTemplate.scaleRandMinUniform = (float)System.Math. Round(liveTemplate.scaleRandMinUniform, 2);
			liveTemplate.scaleRandMaxUniform = (float)System.Math.Round(liveTemplate.scaleRandMaxUniform, 2);
			liveTemplate.scaleRandMinUniform = Mathf.Clamp(liveTemplate.scaleRandMinUniform, liveTemplate.scaleMin, liveTemplate.scaleMax);
			liveTemplate.scaleRandMaxUniform = Mathf.Clamp(liveTemplate.scaleRandMaxUniform, liveTemplate.scaleMin, liveTemplate.scaleMax);

			if (liveTemplate.scaleUniform)
			{
				liveTemplate.scaleRandMin = new Vector3(liveTemplate.scaleRandMinUniform, liveTemplate.scaleRandMinUniform, liveTemplate.scaleRandMinUniform);
				liveTemplate.scaleRandMax = new Vector3(liveTemplate.scaleRandMaxUniform, liveTemplate.scaleRandMaxUniform, liveTemplate.scaleRandMaxUniform);
			}

			EditorGUILayout.EndVertical();

			EditorGUI.EndDisabledGroup();

			EditorGUILayout.BeginVertical(menuBlockStyle, GUILayout.Width(170), GUILayout.MaxWidth(170));
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Min", GUILayout.Width(108)); DoTipCheck("Slider Minimum Value");
			liveTemplate.scaleMin = EditorGUILayout.FloatField(liveTemplate.scaleMin, floatFieldCompressedStyle); DoTipCheck("Slider Minimum Value");
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Max", GUILayout.Width(108)); DoTipCheck("Slider Maximum Value");
			liveTemplate.scaleMax = EditorGUILayout.FloatField(liveTemplate.scaleMax, floatFieldCompressedStyle); DoTipCheck("Slider Maximum Value");
			EditorGUILayout.EndHorizontal();

			liveTemplate.scaleMin = (float)System.Math.Round(liveTemplate.scaleMin, 2);
			liveTemplate.scaleMax = (float)System.Math.Round(liveTemplate.scaleMax, 2);
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();

			//-------------------------
			//EditorGUILayout.Space();
			//-------------------------

			EditorGUILayout.BeginHorizontal();

			liveTemplate.scaleUniform = !EditorGUILayout.Toggle(!liveTemplate.scaleUniform, toggleButtonStyle, GUILayout.Width(15)); DoTipCheck("Placed models are scaled separately on each axis");
			GUILayout.Label("Per Axis Scale"); DoTipCheck("Placed models are scaled separately on each axis");

			EditorGUILayout.EndHorizontal();

			EditorGUI.BeginDisabledGroup(liveTemplate.scaleUniform);

			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.BeginVertical(menuBlockStyle);
			liveTemplate.scaleRandMin.x = Mathf.Clamp(liveTemplate.scaleRandMin.x, liveTemplate.scaleMin, liveTemplate.scaleMax);
			EditorGUILayout.LabelField("X", GUILayout.Width(76));
			EditorGUILayout.LabelField(liveTemplate.scaleRandMin.x.ToString("0.00") + " to " + liveTemplate.scaleRandMax.x.ToString("0.00"), GUILayout.Width(76)); DoTipCheck("X Axis Random Scaling Range Split Slider (Min/Max)");
			EditorGUILayout.MinMaxSlider(ref liveTemplate.scaleRandMin.x, ref liveTemplate.scaleRandMax.x, liveTemplate.scaleMin, liveTemplate.scaleMax); DoTipCheck("X Axis Random Scaling Range Split Slider (Min/Max)");

			liveTemplate.scaleRandMin.x = (float)System.Math.Round(liveTemplate.scaleRandMin.x, 2);
			liveTemplate.scaleRandMax.x = (float)System.Math.Round(liveTemplate.scaleRandMax.x, 2);
			liveTemplate.scaleRandMin.x = Mathf.Clamp(liveTemplate.scaleRandMin.x, liveTemplate.scaleMin, liveTemplate.scaleMax);
			liveTemplate.scaleRandMax.x = Mathf.Clamp(liveTemplate.scaleRandMax.x, liveTemplate.scaleMin, liveTemplate.scaleMax);
			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical(menuBlockStyle);
			liveTemplate.scaleRandMin.y = Mathf.Clamp(liveTemplate.scaleRandMin.y, liveTemplate.scaleMin, liveTemplate.scaleMax);
			EditorGUILayout.LabelField("Y", GUILayout.Width(76));
			EditorGUILayout.LabelField(liveTemplate.scaleRandMin.y.ToString("0.00") + " to " + liveTemplate.scaleRandMax.y.ToString("0.00"), GUILayout.Width(76)); DoTipCheck("Y Axis Random Scaling Range Split Slider (Min/Max)");
			EditorGUILayout.MinMaxSlider(ref liveTemplate.scaleRandMin.y, ref liveTemplate.scaleRandMax.y, liveTemplate.scaleMin, liveTemplate.scaleMax); DoTipCheck("Y Axis Random Scaling Range Split Slider (Min/Max)");

			liveTemplate.scaleRandMin.y = (float)System.Math.Round(liveTemplate.scaleRandMin.y, 2);
			liveTemplate.scaleRandMax.y = (float)System.Math.Round(liveTemplate.scaleRandMax.y, 2);
			liveTemplate.scaleRandMin.y = Mathf.Clamp(liveTemplate.scaleRandMin.y, liveTemplate.scaleMin, liveTemplate.scaleMax);
			liveTemplate.scaleRandMax.y = Mathf.Clamp(liveTemplate.scaleRandMax.y, liveTemplate.scaleMin, liveTemplate.scaleMax);
			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical(menuBlockStyle);
			liveTemplate.scaleRandMin.z = Mathf.Clamp(liveTemplate.scaleRandMin.z, liveTemplate.scaleMin, liveTemplate.scaleMax);
			EditorGUILayout.LabelField("Z", GUILayout.Width(76));
			EditorGUILayout.LabelField(liveTemplate.scaleRandMin.z.ToString("0.00") + " to " + liveTemplate.scaleRandMax.z.ToString("0.00"), GUILayout.Width(76)); DoTipCheck("Z Axis Random Scaling Range Split Slider (Min/Max)");
			EditorGUILayout.MinMaxSlider(ref liveTemplate.scaleRandMin.z, ref liveTemplate.scaleRandMax.z, liveTemplate.scaleMin, liveTemplate.scaleMax); DoTipCheck("Z Axis Random Scaling Range Split Slider (Min/Max)");

			liveTemplate.scaleRandMin.z = (float)System.Math.Round(liveTemplate.scaleRandMin.z, 2);
			liveTemplate.scaleRandMax.z = (float)System.Math.Round(liveTemplate.scaleRandMax.z, 2);
			liveTemplate.scaleRandMin.z = Mathf.Clamp(liveTemplate.scaleRandMin.z, liveTemplate.scaleMin, liveTemplate.scaleMax);
			liveTemplate.scaleRandMax.z = Mathf.Clamp(liveTemplate.scaleRandMax.z, liveTemplate.scaleMin, liveTemplate.scaleMax);
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();

			EditorGUI.EndDisabledGroup();


			EditorGUILayout.BeginVertical(menuBlockStyle);//,GUILayout.Width(78));

			GUILayout.Label("Scaling Style");

			EditorGUILayout.BeginHorizontal();

			liveTemplate.scaleAbsolute = EditorGUILayout.Toggle(liveTemplate.scaleAbsolute, toggleButtonStyle, GUILayout.Width(15)); DoTipCheck("Scale settings are applied directly to the transform of the prefabs being placed");
			GUILayout.Label("Absolute"); DoTipCheck("Scale settings are applied directly to the transform of the prefabs being placed");

			liveTemplate.scaleAbsolute = !EditorGUILayout.Toggle(!liveTemplate.scaleAbsolute, toggleButtonStyle, GUILayout.Width(15)); DoTipCheck("Scale settings are applied as a multiplier to the scale saved in the prefabs being placed as a multiplier");
			GUILayout.Label("Prefab Relative"); DoTipCheck("Scale settings are applied as a multiplier to the scale saved in the prefabs being placed");

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndHorizontal();

			if (EditorGUI.EndChangeCheck())
			{
				liveTemplate.dirty = true;
			}
		}
		#endregion

		EditorGUILayout.Space();

		#region Eraser Options
		eraserFoldout = EditorGUILayout.Foldout(eraserFoldout, "Eraser Settings:", EditorStyles.foldout); DoTipCheck("Settings for limiting the Eraser");
		if (eraserFoldout == true)
		{
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.BeginVertical(menuBlockStyle, GUILayout.Width(260));

			liveTemplate.eraseByGroup =		EditorGUILayout.Toggle("Erase by Group", liveTemplate.eraseByGroup, EditorStyles.toggle); DoTipCheck("Restrict Eraser to objects in currently selected group");
			liveTemplate.eraseBySelected =	EditorGUILayout.Toggle("Erase selected Prefab", liveTemplate.eraseBySelected, EditorStyles.toggle); DoTipCheck("Restrict Eraser to checked prefab");

			EditorGUILayout.EndVertical();

			if (EditorGUI.EndChangeCheck())
			{
				liveTemplate.dirty = true;
			}
		}
		#endregion

		EditorGUILayout.EndVertical(); //Overall Vertical Container End
		EditorGUILayout.EndScrollView(); // Overall Scroll View End


		EditorGUILayout.Space();

		#region Templates

		EditorGUILayout.BeginVertical(GUILayout.Width(280));

		EditorGUILayout.BeginHorizontal(menuBlockStyle, GUILayout.Width(276), GUILayout.Height(22));

		EditorGUILayout.LabelField("Name: ", GUILayout.Width(60)); DoTipCheck("Name Template");

		EditorGUI.BeginChangeCheck();
		GUILayout.FlexibleSpace();

		liveTemplate.brushName = EditorGUILayout.TextField(liveTemplate.brushName); DoTipCheck("Name Template");

		if (EditorGUI.EndChangeCheck())
		{
			liveTemplate.dirty = true;
		}

		EditorGUILayout.BeginHorizontal(saveIconContainerStyle);

		EditorGUI.BeginDisabledGroup(liveTemplate.dirty == false);
		if (GUILayout.Button("", picButton_SaveIcon, GUILayout.Width(16), GUILayout.Height(16)))
		{
			//permutation moved to TrySaveTemplate
			TrySaveTemplate(liveTemplate);

			clearSelection = true;

		} DoTipCheck("Save Template to File");
		EditorGUI.EndDisabledGroup();

		EditorGUILayout.EndHorizontal();

		EditorGUILayout.EndHorizontal();

		EditorGUI.EndDisabledGroup(); //Overall Disable End

		EditorGUILayout.BeginHorizontal(brushStripStyle, GUILayout.Width(276));

		int count = brushTemplates.Length;

		if (count != 0)
			for (int i = 0; i < count; i++)
			{
				if (i >= brushTemplates.Length)
					continue;

				if (brushTemplates[i] != null)
				{
					if (brushTemplates[i].live == false)
						brushTemplates[i] = null;
				}

				string slotNum = (i + 1).ToString("00");

				if (liveTemplateIndex == i)
				{
					brushSlotContainerStyle.normal.background	= templateTabBackground;
				}
				else
				{
					brushSlotContainerStyle.normal.background	= templateTabBackground_inactive;
				}

				EditorGUILayout.BeginVertical(brushSlotContainerStyle, GUILayout.Width(32), GUILayout.Height(48)); //Begin Tab

				EditorGUILayout.BeginHorizontal();	//Begin Tab Operations Duo - Load / Clear

				if (GUILayout.Button("", picButton_OpenFile, GUILayout.Width(16), GUILayout.Height(16)))
				{
					RenderTemplateMenu(Event.current, i);

				} DoTipCheck("Assign Template File to Slot" + slotNum);

				EditorGUI.BeginDisabledGroup(brushTemplates[i] == null);

				if (GUILayout.Button("", picButton_ClearSlot, GUILayout.Width(16), GUILayout.Height(16)))
				{
					TryCloseTab(i);

				} DoTipCheck("Clear Slot " + slotNum);

				EditorGUI.EndDisabledGroup();

				EditorGUILayout.EndHorizontal();	//End Slot Operations Duo

				if (i >= brushTemplates.Length)
					continue;

				EditorGUI.BeginDisabledGroup(brushTemplates[i] == null);// || brushTemplates[i].live == false);//!brushStateArray[i]);

				if (liveTemplateIndex != i)
				{
					if (GUILayout.Button(slotNum, picButton_SlotIcon_InActive, GUILayout.Width(32), GUILayout.Height(32)))
					{
						SwitchToTab(i);
					}
				}
				else
				{
					if (GUILayout.Button(slotNum, picButton_SlotIcon_Active, GUILayout.Width(32), GUILayout.Height(32)))
					{
						SwitchToTab(i);
					}
				}

				if (clearSelection == true)
				{
					clearSelection = false;
					EditorGUIUtility.hotControl = 0;
					EditorGUIUtility.keyboardControl = 0;
				}

				string slotName = brushTemplates[i] == null ? "Empty" : brushTemplates[i].brushName == string.Empty ? "Unnamed Template" :  brushTemplates[i].brushName ;
				DoTipCheck("Brush Slot " + slotNum + ": " + slotName);

				EditorGUI.EndDisabledGroup();

				EditorGUILayout.EndVertical();	//End Tab

			}

		count = brushTemplates.Length;

		//The "LOAD" faux-slot
		if (count < 6)
		{
			brushSlotContainerStyle.normal.background	= templateTabBackground_inactive;
			EditorGUILayout.BeginVertical(brushSlotContainerStyle, GUILayout.Width(32), GUILayout.Height(48)); //Begin Tab

			EditorGUILayout.BeginHorizontal(GUILayout.Width(32), GUILayout.Height(16));	//Begin Tab Operations Duo - FILLER in this case
			EditorGUILayout.Space();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();	//Begin Slot Operations Duo

			if (GUILayout.Button("", picButton_OpenFileLarge, GUILayout.Width(32), GUILayout.Height(32)))
			{
				RenderTemplateMenu(Event.current, count);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();	//End Slot
		}

		EditorGUILayout.EndHorizontal();

		//Tab Active Strip
		EditorGUILayout.BeginHorizontal(GUILayout.Height(24), GUILayout.Width(276));
		EditorGUILayout.BeginHorizontal(GUILayout.Height(24), GUILayout.Width(1));
		EditorGUILayout.Space();
		EditorGUILayout.EndHorizontal();

		for (int i = 0; i < count; i++)
		{
			//Template Checkboxes
			if (i == liveTemplateIndex)
			{
				picButton_TemplateActive.normal.background = templateActiveIcon_active;
				picButton_TemplateActive.hover.background = templateActiveIcon_active;
			}

			else
			{
				if (brushTemplates[i].active == false)
				{
					picButton_TemplateActive.normal.background = templateActiveIcon_off;
					picButton_TemplateActive.hover.background = templateActiveIcon_off;
				}
				else
				{
					picButton_TemplateActive.normal.background = templateActiveIcon_on;
					picButton_TemplateActive.hover.background = templateActiveIcon_on;
				}
			}

			EditorGUILayout.BeginHorizontal(slotActiveContainerStyle);//, GUILayout.Width(32), GUILayout.Height(16));

			if (brushTemplates[i].dirty)
			{
				window.picButton_TemplateDirty.hover.background		=	templateDirtyAsterisk;
				window.picButton_TemplateDirty.normal.background	=	templateDirtyAsterisk;
				window.picButton_TemplateDirty.active.background	=	templateDirtyAsterisk;
			}
			else
			{
				window.picButton_TemplateDirty.hover.background		=	null;
				window.picButton_TemplateDirty.normal.background	=	null;
				window.picButton_TemplateDirty.active.background	=	null;

			}

			GUILayout.Label("", picButton_TemplateDirty, GUILayout.Width(16), GUILayout.Height(16));

			if (GUILayout.Button("", picButton_TemplateActive, GUILayout.Width(16), GUILayout.Height(16)))
			{
				brushTemplates[i].active = !brushTemplates[i].active;
				UpdateAreActive();
				UpdateCompoundPaintToLayer();
				UpdateCompoundLayerMask();
				UpdateCompoundPaintToSelection();
				window.Repaint();
			} DoTipCheck("Toggle Template in slot '" + i + "' Active/Inactive." + " If no slot is checked, the currently selected slot is active");
			//End Template Checkboxes

			EditorGUILayout.EndHorizontal();
		}

		EditorGUILayout.Space(); //filler

		EditorGUILayout.EndHorizontal();
		//End Tab Active Strip

		EditorGUILayout.BeginVertical(GUILayout.Width(274));

		if (GUILayout.Button("Reset Current Template"))
		{
			RestoreTemplateDefaults();

		} DoTipCheck("Reset the currently selected slot to Default settings");

		string tipToDraw = drawCurTip ? curTip : string.Empty;

		EditorGUILayout.HelpBox(tipToDraw, MessageType.Info);

		EditorGUILayout.EndHorizontal();

		EditorGUILayout.EndVertical();

		EditorGUILayout.EndVertical();//Master Vertical End
		#endregion

		//EditorUtility.SetDirty(this);
	}

	void PrefabTile(int i)
	{
		EditorGUI.BeginChangeCheck();

		EditorGUILayout.BeginHorizontal(prefabFieldStyle, GUILayout.Height(60), GUILayout.Width(70));

		liveTemplate.prefabGroup[i].weight = GUILayout.VerticalSlider(liveTemplate.prefabGroup[i].weight, 1f, 0.001f, prefabAmountSliderStyle, prefabAmountSliderThumbStyle, GUILayout.Height(50)); DoTipCheck("Likelyhood that this object will be placed vs the others in the list");

		if (EditorGUI.EndChangeCheck())
		{
			liveTemplate.dirty = true;
		}

		EditorGUILayout.BeginVertical();

		//Get Prefab Preview
		previewTexture = liveTemplate.prefabGroup[i].PreviewTexture;

		//if(GUILayout.Button(previewTexture,prefabPreviewWindowStyle,GUILayout.Height(50),GUILayout.Width(50)))
		GUILayout.Label(previewTexture, prefabPreviewWindowStyle, GUILayout.Height(50), GUILayout.Width(50));


		Rect prefabButtonRect = GUILayoutUtility.GetLastRect();
		Rect xControlRect = new Rect(prefabButtonRect.xMax - 14, prefabButtonRect.yMin, 14, 14);

		if (GUI.Button(xControlRect, "", picButton_PrefabX))
		{
			RemovePrefab(i);
			Event.current.Use();
		} DoTipCheck("'Red X' = remove prefab from list" + System.Environment.NewLine + "'Green Check' = mark to place exclusively");

		Rect checkControlRect = new Rect(prefabButtonRect.xMax - 14, prefabButtonRect.yMax - 14, 14, 14);

		if (liveTemplate.selectedPrefabIndex == i)
		{	//prefabSelectedTexture = selectPrefabCheckTexture_hover;
			picButton_PrefabCheck.normal.background = selectPrefabCheckTexture_on;
			picButton_PrefabCheck.hover.background = selectPrefabCheckTexture_on;
		}
		else
		{	//picButton_PrefabCheck. = selectPrefabCheckTexture_normal;
			picButton_PrefabCheck.normal.background = selectPrefabCheckTexture_off;
			picButton_PrefabCheck.hover.background = selectPrefabCheckTexture_off;
		}

		if (GUI.Button(checkControlRect, "", picButton_PrefabCheck))
		{
			if (liveTemplate.selectedPrefabIndex != i)
				liveTemplate.selectedPrefabIndex = i;

			else
				liveTemplate.selectedPrefabIndex = -1;

			liveTemplate.dirty = true;
			clearSelection = true;
		} DoTipCheck("'Green Check' = mark object as selected - it will be placed exclusively");

		if (Event.current.type == EventType.mouseUp)
		{	if (prefabButtonRect.Contains(Event.current.mousePosition))
			{
				if (liveTemplate.prefabGroup[i].prefab != null)
				{
					Selection.activeObject = liveTemplate.prefabGroup[i].prefab;

					Event.current.Use();
					window.Repaint();
				}
			}
		}

		EditorGUILayout.EndVertical();

		EditorGUILayout.EndHorizontal();
	}

	//static List<Object> PrefabDragBox(int width,GUIStyle style, string text)
	static void PrefabDragBox(int width, GUIStyle style, string text)
	{
		List<Object> draggedObjects = new List<Object>();

		// Draw the controls

		//prefabAddButtonStyle.normal.background = texture;

		//GUILayout.Label(text,texture,prefabAddButtonStyle,GUILayout.Width(width),GUILayout.MinWidth(width),GUILayout.Height(60),GUILayout.MinHeight(60));
		GUILayout.Label(text, style, GUILayout.Width(width), GUILayout.MinWidth(width), GUILayout.Height(60), GUILayout.MinHeight(60));

		Rect lastRect = GUILayoutUtility.GetLastRect();

		// Handle events
		Event evt = Event.current;
		switch (evt.type)
		{
		case EventType.DragUpdated:
			// Test against rect from last repaint
			if (lastRect.Contains(evt.mousePosition))
			{
				// Change cursor and consume the event
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				evt.Use();
			}
			break;

		case EventType.DragPerform:
			// Test against rect from last repaint
			if (lastRect.Contains(evt.mousePosition))
			{
				foreach (Object draggedObject in DragAndDrop.objectReferences)
				{
					if (draggedObject.GetType() == typeof(GameObject))
					{
						draggedObjects.Add(draggedObject);
						window.liveTemplate.dirty = true;
						window.clearSelection = true;
					}
				}
				// Change cursor and consume the event and drag
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				DragAndDrop.AcceptDrag();
				evt.Use();

				AddPrefabList(draggedObjects);
			}
			break;
		}
		//return draggedObjects;
	}

	static void DoTipCheck(string entry)
	{
		if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
		{
			window.curTip = entry;
			window.drawCurTip = true;
			window.Repaint();
		}
	}

	private void RenderLayerMenu(Event curEvent)
	{
		GenericMenu menu = new GenericMenu();
		string[] layerArray = GetLayerList();

		menu.AddItem(new GUIContent("Nothing"), false, LayerMenuItemCallback, -2);

		menu.AddItem(new GUIContent("Everything"), false, LayerMenuItemCallback, -1);

		menu.AddSeparator(string.Empty);

		bool selected = false;
		for (int i = 0; i < 32; i++)
		{
			if (layerArray[i] != string.Empty)
			{
				selected = false;

				if ( ((1 << i) & window.liveTemplate.layerIndex) == (1 << i) )
					selected = true;

				menu.AddItem(new GUIContent( layerArray[i] ) , selected, LayerMenuItemCallback, (1 << i));
			}
		}

		menu.ShowAsContext();

		curEvent.Use();
	}

	public void LayerMenuItemCallback(object obj)
	{
		int optionIndex = (int)obj;

		switch (optionIndex)
		{
		case -2://"Nothing":
			window.liveTemplate.layerIndex = 0;
			break;

		case -1://"Everything":
			for (int i = 0; i < 32; i++)
			{
				window.liveTemplate.layerIndex |= (1 << i);
			}
			break;

		default:
			window.liveTemplate.layerIndex ^= optionIndex;
			break;
		}

		UpdateLayers();
		UpdateCompoundLayerMask();
	}

	// refreshes the text used for the layer dropdown button, and un-sets masks for the un-named layers
	static void UpdateLayers()
	{
		string layerText = string.Empty;
		List<string> layersSelected = new List<string>();

		if (window.liveTemplate.layerIndex == 0)
		{
			layerText = "Nothing";
		}
		else
		{
			int definedLayerCount = 0;
			string layerName = string.Empty;

			for (int i = 0; i < 32; i++)
			{
				layerName = LayerMask.LayerToName(i);

				bool layerUndefined = layerName == string.Empty || layerName.Length == 0 || layerName == "";
				bool layerIsSelected = ((1 << i) & window.liveTemplate.layerIndex) == (1 << i);

				if (layerUndefined != true)
				{
					definedLayerCount++;
				}

				if (layerIsSelected == true)
				{
					if (layerUndefined == true)
					{
						window.liveTemplate.layerIndex ^= (1 << i);
					}

					else
					{
						layersSelected.Add(layerName);
					}
				}

			}

			if (layersSelected.Count == 1)
				layerText = layersSelected[0];

			if (layersSelected.Count > 1)
				layerText = "Mixed";

			if (layersSelected.Count == definedLayerCount)
				layerText = "Everything";
		}

		window.liveTemplate.layerText = layerText;
		window.Repaint();
	}

	private void RenderGroupMenu(Event curEvent)
	{
		UpdateGroups();

		int curIndex = GetGroupIndex(liveTemplate.groupName);

		GenericMenu menu = new GenericMenu();
		bool isSelected = false;

		if (curIndex == -1)
			isSelected = true;

		menu.AddItem(new GUIContent("Nothing"), isSelected, GroupMenuItemCallback, -1);

		menu.AddSeparator(string.Empty);

		for (int i = 0; i < groupNames.Count; i++)
		{
			isSelected = false;

			if (curIndex == i)
				isSelected = true;

			menu.AddItem(new GUIContent(groupNames[i]), isSelected, GroupMenuItemCallback, i);
		}

		menu.ShowAsContext();

		curEvent.Use();
	}

	private int GetGroupIndex(string groupName)
	{
		int groupIndex = -1;

		if (groupName == null || groupName == string.Empty)
			return -1;

		for (int i = 0; i < groupNames.Count; i++)
		{
			if (groupNames[i] == groupName)
			{
				groupIndex = i;
				break;
			}
		}

		return groupIndex;
	}

	private void GroupMenuItemCallback(object obj)
	{
		int groupIndex = (int)obj;

		if (groupIndex == -1)
		{
			liveTemplate.groupName = string.Empty;
			liveTemplate.curGroup = null;
		}
		else
		{
			liveTemplate.groupName = groupNames[groupIndex];
			liveTemplate.curGroup = groups[groupIndex];
		}
	}

	private void RenderTemplateMenu(Event curEvent, int tabIndex)
	{
		UpdateTemplateSignatures();

		// Now create the menu, add items and show it
		GenericMenu menu = new GenericMenu();

		menu.AddItem(new GUIContent("New Template"), false, TemplateMenuItemCallback, new KeyValuePair<int, int>(-1, tabIndex));

		menu.AddSeparator(string.Empty);

		for (int i = 0; i < templateSignatures.Length; i++)
		{
			menu.AddItem(new GUIContent(templateSignatures[i].name), false, TemplateMenuItemCallback, new KeyValuePair<int, int>(i, tabIndex));
		}

		menu.ShowAsContext();

		curEvent.Use();
	}

	private void TemplateMenuItemCallback(object obj)
	{
		KeyValuePair<int, int> pair = (KeyValuePair<int, int>)obj;

		int fileIndex = pair.Key;	//The file's index in the dropDown
		int tabIndex = pair.Value;	//index of the slot we want to load into
		int count = brushTemplates.Length;

		//If the user selected "New Template"
		if (fileIndex == -1)
		{
			//if this is the end tab
			if (tabIndex == count)
			{
				ArrayUtility.Add(ref brushTemplates, null);
				qb_Template newTemplate = new qb_Template();
				brushTemplates[tabIndex] = newTemplate;
				newTemplate.live = true;
			}
			else
			{
				if (brushTemplates[tabIndex].dirty == true)
				{
					int option = EditorUtility.DisplayDialogComplex("Template Has Changed", "The Template '" + brushTemplates[tabIndex].brushName + "' in the tab which you are trying to use has changed since it was last saved.", "Save and Close", "Close W/O Saving", "Cancel");

					switch (option)
					{
					case 0:
						if (TrySaveTemplate(brushTemplates[tabIndex]))
						goto case 1;
						break;

					case 1:
						qb_Template newTemplate = new qb_Template();
						brushTemplates[tabIndex] = newTemplate;
						newTemplate.live = true;
						break;

					case 2:
						break;
					}
				}
			}
		}

		//If the user did select a template from the list
		else
		{
			//here, we need to check if the requested template is already in one of the tabs
			string fileName = templateSignatures[fileIndex].name;

			//The slot into which the file is already loaded (if it is)
			int alreadyLoadedIndex = TemplateAlreadyOpen(fileName);

			//if the selected template is not already loaded
			if (alreadyLoadedIndex == -1)
			{
				if (tabIndex == count)
				{	ArrayUtility.Add(ref brushTemplates, null);

					brushTemplates[tabIndex] = qb_Utility.LoadFromDisk(templateSignatures[fileIndex].directory);
					QBLog("Loaded template '" + fileName + "' into slot " + (tabIndex + 1).ToString("00"));
				}

				else
				{
					if (brushTemplates[tabIndex].dirty == true)
					{
						int option = EditorUtility.DisplayDialogComplex("Template Has Changed", "The Template '" + brushTemplates[tabIndex].brushName + "' in the tab which you are trying to use has changed since it was last saved.", "Save and Close", "Close W/O Saving", "Cancel");

						switch (option)
						{
						case 0:
							if (TrySaveTemplate(brushTemplates[tabIndex]))
							goto case 1;
							break;

						case 1:
							brushTemplates[tabIndex] = qb_Utility.LoadFromDisk(templateSignatures[fileIndex].directory);
							QBLog("Loaded template '" + fileName + "' into slot " + (tabIndex + 1).ToString("00"));
							break;

						case 2:
							break;
						}
					}

					else
					{
						brushTemplates[tabIndex] = qb_Utility.LoadFromDisk(templateSignatures[fileIndex].directory);
						QBLog("Loaded template '" + fileName + "' into slot " + (tabIndex + 1).ToString("00"));
					}
				}
			}

			//if the selected template is already loaded
			else
			{
				//If the tab index we are wanting is not the same as the index of the matching already loaded temaplate
				if (tabIndex != alreadyLoadedIndex)
				{
					if (EditorUtility.DisplayDialog("Template Already Open", "This template '" + fileName + "' is already open in tab " + (alreadyLoadedIndex + 1).ToString("00") + ". Would you like to move '" + fileName + "' to this tab?", "Move", "Cancel"))
					{
						//if this is the end tab - append a tab to the end, and close the tab where the template was alread loaded
						if (tabIndex == count)
						{
							ArrayUtility.Add(ref brushTemplates, null);
							brushTemplates[tabIndex] = brushTemplates[alreadyLoadedIndex];

							CloseTab(alreadyLoadedIndex);
							tabIndex -= 1;
						}

						else
						{
							qb_Template templateToSwap			=	brushTemplates[tabIndex]; //This may be null - it doesn't matter - basically the template to move out of the way
							brushTemplates[tabIndex]			=	brushTemplates[alreadyLoadedIndex];
							brushTemplates[alreadyLoadedIndex]	=	templateToSwap;

							if (templateToSwap == null)
								CloseTab(alreadyLoadedIndex);

							string fName = fileName;
							if (fName == string.Empty)
								fName = "Unnamed";

							QBLog("Moved template '" + fName + "' from slot " + (alreadyLoadedIndex + 1).ToString("00") + " to slot " + (tabIndex + 1).ToString("00"));
						}
					}

					//if user canceled
					else
					{
						//this whole else statement is here just to prevent from trying to switch to an index that's out of rage
						if (tabIndex == count)
							tabIndex -= 1;
					}
				}

				else
				{
					if (EditorUtility.DisplayDialog("Reload Template '" + fileName + "'?", "The template you are trying to load '" + fileName + "' is already open in this tab. Would you like to reload '" + fileName + "' from disk and lose any changes since last saving?", "Reload", "Cancel"))
					{
						brushTemplates[tabIndex] = qb_Utility.LoadFromDisk(templateSignatures[fileIndex].directory);
						QBLog("Loaded template '" + fileName + "' into slot " + (tabIndex + 1).ToString("00"));
					}
				}
				//else
				//do nothing - or maybe prompt user to see if he wants a reload from disk
			}
			//if it is not then do go ahead with the load

			//if it is, either move the template to this slot, or if it is already in this slot then do nothing
			//- or prompt user to ask if he wants a to re-load from disk or keep current version.
		}
		UpdateAreActive();
		SwitchToTab(tabIndex);
	}

	private bool TemplateExistsOnDisk(string templateName)
	{
		UpdateTemplateSignatures();

		for (int i = 0; i < templateSignatures.Length; i++)
		{
			if (templateSignatures[i].name == templateName)
				return true;
		}

		return false;
	}

	private int TemplateAlreadyOpen(string templateName)
	{
		int count = brushTemplates.Length;

		for (int i = 0; i < count; i++)
		{
			if (brushTemplates[i] != null)
			{
				if (brushTemplates[i].brushName == templateName)
					return i;
			}

			//else, we should probably clean the broken tab out
			//but this hasn't ever happened so explore if it is even possible before accounting for it
		}

		return -1;
	}

	static int templateCount;
	static private void DrawBrushGizmo(RaycastHit mouseRayHit, SceneView sceneView)
	{
		if (placing == false)
		{
			if (mouseRayHit.collider != null)
			{
				GUIStyle labelStyle = new GUIStyle();
				labelStyle.fontStyle = FontStyle.Bold;

				Handles.color = Color.white;
				Handles.DrawLine(mouseRayHit.point, mouseRayHit.point + (mouseRayHit.normal * window.liveTemplate.brushRadius));
				Handles.color = Color.blue;
				Handles.DrawWireDisc(mouseRayHit.point, mouseRayHit.normal, window.liveTemplate.brushRadius);

				Vector3 right = sceneView.camera.transform.right;
				Handles.Label(mouseRayHit.point + (right.normalized * window.liveTemplate.brushRadius), (window.liveTemplateIndex + 1).ToString("00"), labelStyle);

				Handles.color = Color.grey;
				templateCount = window.brushTemplates.Length;
				for (int i = 0; i < templateCount; i ++)
				{
					if (window.brushTemplates[i].active == true && i != window.liveTemplateIndex)
					{
						Handles.DrawWireDisc(mouseRayHit.point, mouseRayHit.normal, window.brushTemplates[i].brushRadius);
						Handles.Label(mouseRayHit.point + (right.normalized * (window.brushTemplates[i].brushRadius)), (i + 1).ToString("00"), labelStyle);
					}
				}

			}
		}

		else
		{
			if (placingObject != null)
			{
				Handles.color = Color.green;
				Handles.DrawWireDisc(placingObject.transform.position, placingUpVector, Vector3.Distance(placingPlanePoint, placingObject.transform.position));
				Handles.DrawSolidDisc(placingPlanePoint, placingUpVector, 0.1f);
				Handles.DrawPolyLine(new Vector3[2] {placingObject.transform.position, placingPlanePoint});
				Handles.ArrowCap(0, placingPlanePoint + ((placingPlanePoint - placingObject.transform.position).normalized * -0.5f), Quaternion.LookRotation(placingPlanePoint - placingObject.transform.position, placingUpVector), 1f);
			}
		}

	}

	private enum BrushMode
	{
		Off,
		On
	}

	static bool placing;	//currently placing an object - ie have spawned object and not yet stopped modifying scale / rotation
	static bool painting; 	//in stroke

	static bool ctrlWasDown = true;
	static bool shiftWasDown = true;

	public void OnSceneGUI(SceneView sceneView)
	{
		/*
		Rules pseudo code
		//if ALT not down
		//{
			//if mouse click when shift down -		begin PlaceMode
			//if drag while PlaceMode -				scale and rotate (calculate and execute scale and rotation on stored object)
			//if mouse up while placing -			end PlaceMode (if shift is still held down it will pop back on based on the stuff above)
			//if mouse right down while placing -	remove stored object from scene

			//if click when shift up -				begin StrokeMode
			//if drag while StrokeMode -			Paint stuff
			//if mouse up while StokeMode -			end StrokeMode
		//}
		*/

		CaptureInput();

		//dropout conditions
		if (toolActive == false)
			return;

		if (brushMode ==  BrushMode.Off)
			return;

		RaycastHit mouseRayHit = new RaycastHit();
		if (mouseOverWindow == sceneView)
		{
			mouseRayHit = DoMouseRaycastMulti();//DoMouseRaycast();
			DrawBrushGizmo(mouseRayHit, sceneView);
		}

		HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

		Event curEvent = Event.current;

		if (altDown == false)
		{
			switch (curEvent.type)
			{
			case EventType.mouseUp:
				if (curEvent.button == 0)
				{
					if (brushMode == BrushMode.On)
					{
						if (painting == true)
						{
							EndStroke();
						}

						if (placing == true)
						{
							EndPlaceStroke();
						}
					}
				}
				break;

			case EventType.mouseDown:
				if (curEvent.button == 0)
				{
					if (brushMode == BrushMode.On)
					{
						if (!shiftDown)
						{
							BeginStroke();

							if (paintable)
							{
								Paint(mouseRayHit);
								Event.current.Use();
							}
							UnityEditor.Tools.current = UnityEditor.Tool.None;
						}

						else //shiftDown
						{
							if (paintable)
							{
								BeginPlaceStroke();
								Event.current.Use();
							}
							UnityEditor.Tools.current = UnityEditor.Tool.None;
						}
					}
				}
				break;

			case EventType.mouseDrag:
				if (curEvent.button == 0)
				{
					if (brushMode == BrushMode.On)
					{
						if (placing == true)
						{
							//if(paintable)
							//{
							UpdatePlace(sceneView);
							Event.current.Use();
							UnityEditor.Tools.current = UnityEditor.Tool.None; //make sure needed?
							//}
						}

						else if (painting == true)
						{
							if (paintable)
							{
								Paint(mouseRayHit);
								Event.current.Use();
								UnityEditor.Tools.current = UnityEditor.Tool.None; //make sure needed?
							}
						}

					}
				}
				HandleUtility.Repaint();
				break;

			case EventType.mouseMove:
				HandleUtility.Repaint();
				break;

			}
		}

		CalculateCursor(mouseRayHit);

		//This repaint is important to make lines and indicators not hang around for more frames
		sceneView.Repaint();
	}

	static bool altDown = false;
	static bool shiftDown = false;
	static bool ctrlDown = false;

	private void CaptureInput()
	{
		altDown = false;
		shiftDown = false;
		ctrlDown = false;

		if (toolActive == false)
			return;

		Event curEvent = Event.current;

		if (curEvent.control)
		{
			ctrlDown = true;
			//sceneView.Focus(); // Pulls focus to the scene view, but may be responsible for bug where qb to takes over computers when ctrl is down
		}

		if (shiftDown == false && shiftWasDown == true)
		{
			shiftWasDown = false;
			placementModifier = false;
			window.Repaint();
		}

		if (ctrlDown == false && ctrlWasDown == true)
		{
			ctrlWasDown = false;
			//shiftDown = false;
			//shiftWasDown = false;
			//placementModifier = false;

			brushMode = BrushMode.Off;
			EndStroke();
			EndPlaceStroke();
			window.Repaint();
			return;
		}

		else if (ctrlDown == true && ctrlWasDown == false)
		{	ctrlWasDown = true;

			brushMode = BrushMode.On;
			window.Repaint();
		}

		//Default Mode is Paint, from there we can
		if (curEvent.alt)
		{
			//modify mode to Camera Navigation
			//if we are currently painting end stroke
			//if we are currently placing commit
			altDown = true;
		}

		if (curEvent.shift)
			shiftDown = true;


		if (shiftDown == true && shiftWasDown == false) //might want to force end
		{
			shiftWasDown = true;

			//modify mode to Place
			placementModifier = true;
			window.Repaint();
		}

		//Default Mode is Paint, from there we can
		if (curEvent.alt)
		{
			//modify mode to Camera Navigation
			//if we are currently painting end stroke
			//if we are currently placing commit
			altDown = true;
		}

		if (curEvent.shift)
			shiftDown = true;


		if (shiftDown == true && shiftWasDown == false) //might want to force end
		{
			shiftWasDown = true;

			//modify mode to Place
			placementModifier = true;
			window.Repaint();
		}

		//return;
	}

	static void BeginStroke()
	{
		int count = window.brushTemplates.Length;
		int curIndex = window.liveTemplateIndex;

		//if active or live
		if (curIndex != -1 && curIndex < count)
			BeginStroke(curIndex);

		if (areActive == true)
			for (int i = 0; i < count; i++)
			{
				if (i == window.liveTemplateIndex)
					continue;

				if (window.brushTemplates[i] == null) //Skip out if there is no template in this slot is null (but this shouldnt happen hopefully)
					continue;

				if (window.brushTemplates[i].prefabGroup.Length == 0) //Skip out if there are no prefabs in the template
					continue;

				if (window.brushTemplates[i].active == true)
					BeginStroke(i);
			}

		painting = true;
	}

	static void BeginStroke(int i)
	{
		curStrokes[i] = new qb_Stroke();
	}

	static void EndStroke()
	{
		for (int i = 0; i < 6; i++)
		{
			curStrokes[i] = null;
		}

		//curStroke = null;
		painting = false;
	}

	//Per Brush UpdateStroke()
	static qb_Stroke[] curStrokes = new qb_Stroke[6]; //this could be wrapped into qb_template

	static bool areActive = false; //simple bool to show whether there are any templates marked as active
	static void UpdateAreActive() //This should be run on enable, whenever active state is modified, and whenever tab operations are performed
	{
		int count = window.brushTemplates.Length;

		areActive = false;

		for (int i = 0; i < count; i++)
		{
			if (window.brushTemplates[i].active == true)
			{
				areActive = true;
				break;
			}
		}
	}

	static void UpdateStroke() // this could be called up if active templates has any trues otherwise go with the default path if can do without doubling up functions
	{
		int count = window.brushTemplates.Length;
		int curIndex = window.liveTemplateIndex;

		//First do the liveTemplate
		if (curIndex != -1 && curIndex < count)
			UpdateStroke(curIndex);

		//Check if any templates are active
		if (areActive == true)
			for (int i = 0; i < count; i++)
			{
				//We already did the live template so skip it here
				if (i == curIndex)
					continue;

				if (window.brushTemplates[i] == null) //Skip out if there is no template in this slot is null (but this shouldnt happen hopefully)
					continue;

				if (window.brushTemplates[i].prefabGroup.Length == 0) //Skip out if there are no prefabs in the template
					continue;

				if (window.brushTemplates[i].active == true)
					UpdateStroke(i);
			}
	}

	//New version of updateStroke which receives the template index to itterate
	static void UpdateStroke(int i)
	{
		//use the calculated stored cursor position to check distance from previous point on the stroke

		//if the calculated cursor position is at or beyond the BrushSpacingDistance from the last point in the stroke
		//add a point to the stroke

		if (curStrokes[i].GetCurPoint() == null) //there is no cur point, we are starting the stroke
		{
			qb_Point nuPoint = curStrokes[i].AddPoint(cursorPoint.position, cursorPoint.upVector, cursorPoint.dirVector);
			DoBrushIterration(nuPoint, i);
		}

		else
		{
			float distanceFromLastPt = Vector3.Distance(cursorPoint.position, curStrokes[i].GetCurPoint().position);
			Vector3 strokeDirection = cursorPoint.position - curStrokes[i].GetCurPoint().position;

			if (distanceFromLastPt >= window.brushTemplates[i].brushSpacing)
			{
				//Debug.DrawRay(cursorPoint.position,strokeDirection * strokeDirection.magnitude * -1f,Color.red);
				qb_Point newPoint = curStrokes[i].AddPoint(cursorPoint.position, cursorPoint.upVector, strokeDirection.normalized);
				DoBrushIterration(newPoint, i);
			}
		}
	}

	static void DoBrushIterration(qb_Point newPoint, int i) // do whatever needs to be done on the bruh itteration
	{
		//if brush is positive
		//do a paint itteration
		if (brushDirection == true)
			PlaceGeo(newPoint, i);

		//if brush is negative
		//do an erase itteration
		else
			EraseGeo(newPoint, i);

		//later, we'll need another case for a vertex color brush, probably just an additional layer rather than exclusive
	}

	static GameObject placingObject;
	static Vector3 placingUpVector;

	static void BeginPlaceStroke()
	{
		if (window.liveTemplateIndex == -1)
			return;

		//single placement for now should only use the live template - like the old version
		curStrokes[window.liveTemplateIndex] = new qb_Stroke();
		qb_Point nuPoint = curStrokes[window.liveTemplateIndex].AddPoint(cursorPoint.position, cursorPoint.upVector, cursorPoint.dirVector);

		placingObject = PlaceObject(nuPoint, window.liveTemplateIndex);

		if (placingObject != null)
		{
			placing = true;
			placingUpVector = placingObject.transform.up;
		}
	}

	static void EndPlaceStroke()
	{
		for (int i = 0; i < 6; i++)
		{
			curStrokes[i] = null;
		}

		//release from placing mode
		placing = false;
	}

	static Vector3 placingPlanePoint = Vector3.zero;

	static void UpdatePlace(SceneView sceneView)
	{
		if (placingObject != null)
		{
			Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			Vector3 mouseWorldPoint = mouseRay.GetPoint(0f);

			placingPlanePoint = GetLinePlaneIntersectionPoint(mouseWorldPoint, mouseRay.direction, placingObject.transform.position, placingUpVector); //contact;//Vector3.Project(placingObjectRay.direction,placingUpVector);

			Vector3 difVector = placingPlanePoint - placingObject.transform.position;

			Bounds placingObjectBounds = TryGetObjectBounds(placingObject);

			float modifiedScale = 1f;

			//	if(placingObjectBounds == null)
			//		modifiedScale = (difVector.magnitude * 2f);

			//	else
			//		modifiedScale = (difVector.magnitude * 2f) / ((Mathf.Max(placingObject.renderer.bounds.extents.x , placingObject.renderer.bounds.extents.x) * 2) / placingObject.transform.localScale.x);

			modifiedScale = (difVector.magnitude * 2f) / /*((Mathf.Max(placingObjectBounds.extents.x , placingObjectBounds.extents.y) * 2)*/(placingObjectBounds.size.magnitude / placingObject.transform.localScale.x);

			placingObject.transform.rotation = Quaternion.LookRotation(difVector, placingUpVector); //this has to be rotation relative to the original placement rotation along its disk in the direction of the mouse pointer
			placingObject.transform.localScale = new Vector3(modifiedScale, modifiedScale, modifiedScale); //This has to be the distance between the screen cursor's position and the screen bound position of the object's placement point
		}
	}

	static Bounds TryGetObjectBounds(GameObject topObject)
	{
		//We need to itterate through the object's hierarchy and determine if it has any kind of object with bounds
		//if yes	return cumulative bounds
		//if no		return null

		//So, we itterate through the hierarchy to find any renderers or meshes to get bounds from
		//Then we combine all bounds to get a total
		Bounds combinedBounds = new Bounds(topObject.transform.position, new Vector3(1f, 1f, 1f));

		if (topObject.GetComponent(typeof(MeshRenderer)))
		{
			Renderer topRenderer = (Renderer)topObject.GetComponent<Renderer>();
			combinedBounds = topRenderer.bounds;
		}
		Renderer[] renderers = topObject.GetComponentsInChildren<MeshRenderer>() as Renderer[];// as Renderer[];

		foreach (Renderer render in renderers)
		{
			//if (render != renderer)
			combinedBounds.Encapsulate(render.bounds);
		}

		return combinedBounds;
	}

	static Vector3 GetLinePlaneIntersectionPoint(Vector3 rayOrigin, Vector3 rayDirection, Vector3 pointOnPlane, Vector3 planeNormal)
	{
		float epsilon = 0.0000001f;
		Vector3 contact = Vector3.zero;


		Vector3 ray =  (rayOrigin + (rayDirection * 1000f)) - rayOrigin;
		//Vector3 ray = rayOrigin - (rayDirection * Mathf.Infinity);

		Vector3 difVector = rayOrigin - pointOnPlane;

		float dot = Vector3.Dot(planeNormal, ray);

		if (Mathf.Abs(dot) > epsilon)
		{
			float fac = -Vector3.Dot(planeNormal, difVector) / dot;
			Vector3 fin = ray * fac;

			contact = rayOrigin + fin;
		}

		return contact;
	}

	static Vector3 GetFlattenedDirection(Vector3 worldVector, Vector3 flattenUpVector)
	{
		Vector3 flattened = Vector3.Cross(flattenUpVector, worldVector);
		Vector3 diskDirection = Vector3.Cross(flattened, flattenUpVector);

		return diskDirection;
	}

	static Object PickRandPrefab(qb_Template  curTemplate)
	{

		float totalWeight = 0f;
		for (int i = 0; i < curTemplate.prefabGroup.Length ; i++)
		{
			totalWeight += curTemplate.prefabGroup[i].weight;
		}

		float randomNumber = Random.Range(0f, totalWeight);

		float weightSum = 0f;
		int chosenIndex = 0;

		for (int x = 0; x < curTemplate.prefabGroup.Length; x++)
		{
			weightSum += curTemplate.prefabGroup[x].weight;

			if (randomNumber < weightSum)
			{
				chosenIndex = x;
				break;
			}
		}

		return curTemplate.prefabGroup[chosenIndex].prefab;
	}

	static void Paint(RaycastHit mouseRayHit) //This function is called when the stroke reaches its next step - We feed it the hit from the latest Raycast
	{
		//were only here if the cursor is over a paintable object and the mouse button is pressed
		CalculateCursor(mouseRayHit);

		UpdateStroke();
	}

	static Object objectToSpawn;
	private static void PlaceGeo(qb_Point newPoint, int i)
	{
		qb_Template curTemplate = window.brushTemplates[i];

		//-1 : if there are no prefabs in the queue. Do not paint
		if (curTemplate.prefabGroup.Length == 0)
			return;

//		if(window.liveTemplate.prefabGroup.Length == 0)
//			return;

		//0	: declare function variables
		Vector3 spawnPosition = Vector3.zero;
		Quaternion spawnRotation = Quaternion.identity;
		//Vector3 spawnScale = new Vector3(1f,1f,1f);
		Vector3 upVector = Vector3.up;
		Vector3 placeUpVector = Vector3.up;
		Vector3 forwardVector = Vector3.forward; //blank filled - this value should never end up being used

		//1 : if there is more than one prefab in the queue, pick one using the randomizer
		if (curTemplate.prefabGroup.Length > 0)
		{
			if (curTemplate.selectedPrefabIndex != -1)
			{
				if (curTemplate.prefabGroup.Length > curTemplate.selectedPrefabIndex && curTemplate.prefabGroup[curTemplate.selectedPrefabIndex] != null)
					objectToSpawn = curTemplate.prefabGroup[curTemplate.selectedPrefabIndex].prefab;

				else
				{
					curTemplate.selectedPrefabIndex = -1;
					return;
				}
			}

			else
				objectToSpawn = PickRandPrefab(curTemplate);
		}

		else
			return;

		//2 : use the current point in the stroke to Get a random point around its upVector Axis
		Vector3 castPosition = GetRandomPointOnDisk(newPoint.position, newPoint.upVector, curTemplate.brushRadius);// * curTemplate.scatterRadius);//Vector3.zero;

		//3 : use the random disk point to cast down along the upVector of the stroke point
		Vector3 rayDir = -newPoint.upVector;
		//RaycastHit hit;

		qb_RaycastResult result = DoPlacementRaycast(castPosition + (rayDir * -0.02f), rayDir, curTemplate);

		//4 : if cast successful, get cast point and normal - if cast is unsuccessful, return...<---
		if (result.success == true)
		{
			spawnPosition = result.hit.point;

			if (curTemplate.alignToNormal == true)
			{
				upVector = result.hit.normal;
				placeUpVector = upVector;

				if (curTemplate.flipNormalAlign)
					placeUpVector *= -1f;

				forwardVector = GetFlattenedDirection(Vector3.forward, upVector);
			}

			forwardVector = GetFlattenedDirection(Vector3.forward, upVector);


			if (curTemplate.alignToStroke == true)
			{
				forwardVector = GetFlattenedDirection(curStrokes[i].GetCurPoint().dirVector, upVector);

				if (curTemplate.flipStrokeAlign)
					forwardVector *= -1f;
			}
		}


		else
			return;

		//5 : instantiate the prefab
		GameObject newObject = null;

		newObject = PrefabUtility.InstantiatePrefab(objectToSpawn) as GameObject;
		qb_Object marker = newObject.AddComponent<qb_Object>();//.hideFlags = HideFlags.HideInInspector;
		marker.hideFlags = HideFlags.HideInInspector;
		Undo.RegisterCreatedObjectUndo(newObject, "qbP");

		//6 : use settings to scale, rotate, and place the object
		spawnRotation = GetSpawnRotation(upVector, forwardVector);

		newObject.transform.position = spawnPosition;
		newObject.transform.rotation = spawnRotation;
		newObject.transform.position += (newObject.transform.right * curTemplate.positionOffset.x) + (upVector.normalized * curTemplate.positionOffset.y) + (newObject.transform.forward * curTemplate.positionOffset.z);
		Vector3 randomScale;

		if (curTemplate.scaleUniform == true)
		{
			float randomScaleUni = Random.Range(curTemplate.scaleRandMinUniform, curTemplate.scaleRandMaxUniform);
			randomScale = new Vector3(randomScaleUni, randomScaleUni, randomScaleUni);
		}

		else
			randomScale = new Vector3(Random.Range(curTemplate.scaleRandMin.x, curTemplate.scaleRandMax.x), Random.Range(curTemplate.scaleRandMin.y, curTemplate.scaleRandMax.y), Random.Range(curTemplate.scaleRandMin.z, curTemplate.scaleRandMax.z));

		Vector3 finalScale = curTemplate.scaleAbsolute == true ? randomScale : new Vector3 (randomScale.x * newObject.transform.localScale.x, randomScale.y * newObject.transform.localScale.y, randomScale.z * newObject.transform.localScale.z);


		//7 : If we have a group, add the object to the group
		if (curTemplate.groupObjects == true)
		{
			bool doGroup = false;
			//if group is missing - (either not yet assigned based on selection, or does not exist in this scene)
			if (curTemplate.curGroup == null)
			{
				//if the group selected is not "nothing" - otherwise no grouping is done
				if (curTemplate.groupName != string.Empty)
				{
					//the group has a name - check if group exists in scene
					qb_Group testGroup = GetGroupWithName(curTemplate.groupName);

					//if it does exist, assign as curGroup
					if (testGroup == null)
						curTemplate.curGroup = CreateGroup(curTemplate.groupName);

					else
						curTemplate.curGroup = testGroup;

					doGroup = true;
				}
			}

			else
				doGroup = true;

			if (doGroup)
				curTemplate.curGroup.AddObject(newObject);
		}

		//8 : Scaling is applied after grouping to avoid float error
		newObject.transform.localScale = new Vector3(finalScale.x, finalScale.y, finalScale.z); //Random.Range(scaleMin.x,scaleMax.x),Random.Range(scaleMin.y,scaleMax.y),Random.Range(scaleMin.z,scaleMax.z));//spawnScale;

	}

	private static GameObject PlaceObject(qb_Point newPoint, int i)
	{
		qb_Template curTemplate = window.brushTemplates[i];

		//-1 : if there are no prefabs in the queue. Do not place
		if (curTemplate.prefabGroup.Length == 0)
			return null;

		if (curTemplate.prefabGroup[0] == null)
			return null;

		//0	: declare function variables
		Vector3 spawnPosition = Vector3.zero;
		Quaternion spawnRotation = Quaternion.identity;
		Vector3 upVector = Vector3.up;
		Vector3 placeUpVector = Vector3.up;
		//1 : if there is more than one prefab in the queue, pick one

		if (curTemplate.selectedPrefabIndex != -1)
		{
			if (curTemplate.prefabGroup.Length > curTemplate.selectedPrefabIndex && curTemplate.prefabGroup[curTemplate.selectedPrefabIndex] != null)
				objectToSpawn = curTemplate.prefabGroup[curTemplate.selectedPrefabIndex].prefab;

			else
				curTemplate.selectedPrefabIndex = -1;
		}

		else
		{
			if (curTemplate.prefabGroup.Length > 0 && curTemplate.prefabGroup[0] != null)
				objectToSpawn = curTemplate.prefabGroup[0].prefab;

			else
			{
				//window.selectedPrefabIndex = -1;
				return null;
			}
		}
		//else return null;

		//2 : use the current point in the stroke to Get a random point around its upVector Axis
		Vector3 castPosition = newPoint.position;

		//3 : use the random disk point to cast down along the upVector of the stroke point
		Vector3 rayDir = -newPoint.upVector;

		qb_RaycastResult result = DoPlacementRaycast(castPosition, rayDir, curTemplate);

		//4 : if cast successful, get cast point and normal - if cast is unsuccessful, return...<---
		if (result.success == true)
		{
			spawnPosition = result.hit.point;

			if (curTemplate.alignToNormal == true)
			{
				upVector = result.hit.normal;
				placeUpVector = upVector;

				if (curTemplate.flipNormalAlign)
					placeUpVector *= -1f;
			}

		}

		else
			return null;

		//5 : instantiate the prefab
		GameObject newObject = null;

		newObject = PrefabUtility.InstantiatePrefab(objectToSpawn) as GameObject;
		qb_Object marker = newObject.AddComponent<qb_Object>();//.hideFlags = HideFlags.HideInInspector;
		marker.hideFlags = HideFlags.HideInInspector;
		Undo.RegisterCreatedObjectUndo(newObject, "qbP");

		//6 : use settings to scale, rotate, and place the object
		if (curTemplate.alignToNormal)
		{
			spawnRotation = Quaternion.LookRotation(curStrokes[i].GetCurPoint().dirVector, placeUpVector);
		}

		else
		{
			spawnRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
		}

		newObject.transform.position = spawnPosition;
		newObject.transform.rotation = spawnRotation;
		newObject.transform.position += (newObject.transform.right * curTemplate.positionOffset.x) + (upVector.normalized * curTemplate.positionOffset.y) + (newObject.transform.forward * curTemplate.positionOffset.z);

		//7 : If we have a group, add the object to the group
		if (curTemplate.groupObjects == true)
		{
			bool doGroup = false;
			//if group is missing - (either not yet assigned based on selection, or does not exist in this scene)
			if (curTemplate.curGroup == null)
			{
				//if the group selected is not "nothing" - otherwise no grouping is done
				if (curTemplate.groupName != string.Empty)
				{
					//the group has a name - check if group exists in scene
					qb_Group testGroup = GetGroupWithName(curTemplate.groupName);

					//if it does exist, assign as curGroup
					if (testGroup == null)
						curTemplate.curGroup = CreateGroup(curTemplate.groupName);

					else
						curTemplate.curGroup = testGroup;

					doGroup = true;
				}
			}

			else
				doGroup = true;

			if (doGroup)
				curTemplate.curGroup.AddObject(newObject);
		}

		return newObject;
	}

	private static void EraseGeo(qb_Point newPoint, int i)
	{
		qb_Template curTemplate = window.brushTemplates[i];

		GameObject[] objects = window.GetGroupObjects();
		List<int> removalList = new List<int>();

		object curPrefab = null;
		bool eraseSelected = false;
		bool eraseGrouped = false;
		bool groupIsNothing = false;

		if (curTemplate.eraseBySelected == true)
			if (curTemplate.selectedPrefabIndex != -1)
			{
				curPrefab = curTemplate.prefabGroup[curTemplate.selectedPrefabIndex].prefab;
				eraseSelected = true;
			}

		if (curTemplate.eraseByGroup == true)
		{
			if (curTemplate.curGroup != null)
			{
				eraseGrouped = true;
			}

			else
			{
				if (curTemplate.groupName == string.Empty)
				{
					eraseGrouped = true;
					groupIsNothing = true;
				}

				else
				{
					curTemplate.curGroup = GetGroupWithName(curTemplate.groupName);
				}
			}

			if (curTemplate.curGroup == null && groupIsNothing == false)
				eraseGrouped = false;
		}

		bool addToList;
		for (int ii = 0; ii < objects.Length; ii++)
		{
			addToList = true;

			if (Vector3.Distance(objects[ii].transform.position, newPoint.position) < curTemplate.brushRadius)
			{
				if (eraseSelected == true)
				{
					//if the current object's prefab is the curPrefab
					if (PrefabUtility.GetPrefabParent(objects[ii]) != curPrefab)
						addToList = false;
				}
				//if group erase is on
				if (eraseGrouped == true)
				{
					if (groupIsNothing == false)
					{	//Regular, group based
						if (objects[ii].transform.parent != curTemplate.curGroup.transform) //curGroup.transform)
						{
							addToList = false;
						}
					}
					//Only those which have no group
					else
					{
						if (objects[ii].transform.parent != null) //curGroup.transform)
						{
							addToList = false;
						}
					}
				}
				if (addToList == true)
					removalList.Add(ii);
			}
		}

		if (removalList.Count > 0)
			window.EraseObjects(removalList);
	}

	private static qb_RaycastResult DoPlacementRaycast(Vector3 castPosition, Vector3 rayDirection, qb_Template curTemplate)
	{
		RaycastHit hit = new RaycastHit();
		bool success = false;

		//	Physics.Raycast(castPosition + (-0.1f * rayDirection),rayDirection,out hit,float.MaxValue,//curTemplate.layerIndex);//obsolete-prob
		//if the current template is

		if (curTemplate.paintToLayer == false)
			Physics.Raycast(castPosition + (-0.1f * rayDirection), rayDirection, out hit, curTemplate.brushRadius); //float.MaxValue);

		else if (curTemplate.paintToLayer == true && curTemplate.layerIndex != -1)
			Physics.Raycast(castPosition + (-0.1f * rayDirection), rayDirection, out hit, curTemplate.brushRadius, curTemplate.layerIndex);

		if (hit.collider != null)
		{
			success = true;

			if (curTemplate.paintToLayer == true)
			{
				//if(hit.collider.gameObject.layer != window.layerIndex)
				if ( (1 << hit.collider.gameObject.layer & curTemplate.layerIndex) == 0)
					success = false;
			}

			if (curTemplate.paintToSelection == true)
			{

				Transform[] selectedObjects = Selection.transforms;
				bool contains = ArrayUtility.Contains(selectedObjects, hit.collider.transform);

				if (!contains)
					success = false;
			}
		}

		qb_RaycastResult result = new qb_RaycastResult(success, hit);

		return result;
	}

	private static Vector3 GetRandomPointOnDisk(Vector3 position, Vector3 upVector, float scatterRadius)
	{
		float angle = Random.Range(0f, 2f) * Mathf.PI;
		Vector2 direction = new Vector2((float)Mathf.Cos(angle), (float)Mathf.Sin(angle));

		Vector3 direction3D = new Vector3(direction.x, 0f, direction.y);
		//	Debug.DrawRay(position + new Vector3(0f,.2f,0f),direction3D,Color.red,5f);
		//	Debug.DrawLine(position + new Vector3(0f,.2f,0f), position + new Vector3(0f,.2f,0f) + (direction3D * 1f),Color.red, 5f);
		Vector3 flattened = Vector3.Cross(upVector, direction3D);
		Vector3 diskDirection = Vector3.Cross(flattened, upVector);

		//float distanceFromCenter = (window.liveTemplate.scatterRadius * window.liveTemplate.brushRadius)* Random.Range(0.0f,1.0f);
		float distanceFromCenter = scatterRadius * Random.Range(0.0f, 1.0f);

		Vector3 randomPoint = position + (diskDirection.normalized * distanceFromCenter);

		return randomPoint;
	}

	private static Quaternion GetSpawnRotation(Vector3 upVector, Vector3 forwardVector)
	{
		Quaternion rotation = Quaternion.identity;
		Vector3 rotationOffset = Vector3.zero;

		if (upVector.magnitude != 0 && forwardVector.magnitude != 0)
		{
			if (upVector != Vector3.zero && forwardVector != Vector3.zero)
				rotation = Quaternion.LookRotation(forwardVector, upVector);
		}

		if (placing)
			return rotation;

		rotationOffset = new Vector3(Random.Range(window.liveTemplate.rotationRangeMin.x, window.liveTemplate.rotationRangeMax.x), Random.Range(window.liveTemplate.rotationRangeMin.y, window.liveTemplate.rotationRangeMax.y), Random.Range(window.liveTemplate.rotationRangeMin.z, window.liveTemplate.rotationRangeMax.z));

		rotation = rotation * Quaternion.Euler(rotationOffset);

		return rotation;
	}

	private static void UpdateCompoundPaintToLayer()
	{
		int count = window.brushTemplates.Length;

		int activeNum = 0;

		int paintToLayerNum = 0;

		for (int i = 0; i < count; i++)
		{
			if (window.brushTemplates[i].active == true || i == window.liveTemplateIndex)
			{
				activeNum++;
				if (window.brushTemplates[i].paintToLayer == true)
				{
					paintToLayerNum++;
				}
			}
		}

		if (paintToLayerNum == 0)
			window.compoundPaintToLayer = 0;

		else if (paintToLayerNum < activeNum)
			window.compoundPaintToLayer = 1;

		else if (paintToLayerNum == activeNum)
			window.compoundPaintToLayer = 2;

	}

	//We can just update the Compound Layer Mask when the layer menu is changed, when the Active State is changed, and when Switching Tabs
	[SerializeField] private int compoundLayerMask;
	//[SerializeField] private bool compoundPaintToLayer;
	[SerializeField] private int compoundPaintToLayer = 0; //0 is none ; 1 is partial ; 2 is all

	//private static int CompoundLayerMask()
	static int thisMask; //vars to be used in this function to keep from churning through new ones
	static int compoundMask;
	private static void UpdateCompoundLayerMask()
	{
		int count = window.brushTemplates.Length;
		compoundMask =  0;
		//LayerMask oneMask = new LayerMask();
		thisMask =  0;
		//Debug.Log("Updating Compound Layer Mask");

		//get the active template's mask
//		if(window.liveTemplate.paintToLayer)
//		{
//			thisMask = window.liveTemplate.layerIndex;
//			compoundMask |= thisMask;
//		}

		//if any others are active get their masks too
		if (areActive == true)
			for (int i = 0; i < count; i ++)
			{
				//			if(i == window.liveTemplateIndex)
				//			continue;

				if (window.brushTemplates[i].active == true || i == window.liveTemplateIndex)
				{
					if (window.brushTemplates[i].paintToLayer == true)
					{
						//add to layermask
						thisMask =  window.brushTemplates[i].layerIndex;
						compoundMask |= thisMask;
					}
				}
			}

		else
		{
			thisMask = window.liveTemplate.layerIndex;
			compoundMask |= thisMask;
		}

		window.compoundLayerMask = compoundMask;
	}

	[SerializeField] private bool compoundPaintToSelection;

	static bool paintToSelection = true;
	static void UpdateCompoundPaintToSelection()
	{	//Debug.Log("Updating Compound Paint To Selection");
		int count = window.brushTemplates.Length;


		paintToSelection = true;

		//if any templates don't have paint to selection checked, don't restrict painting to selection. (individual brushes will still do it themselves)
		if (areActive == true)
			for (int i = 0; i < count; i++)
			{
				if (window.brushTemplates[i].active == true || i == window.liveTemplateIndex)
				{
					if (window.brushTemplates[i].paintToSelection == false)
					{
						paintToSelection = false;
						break;
					}
				}
			}

		else
		{
			paintToSelection = window.liveTemplate.paintToSelection;
		}

		window.compoundPaintToSelection = paintToSelection;
	}

	private static RaycastHit DoMouseRaycastMulti()
	{
		Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
		RaycastHit hit = new RaycastHit();

		//if any of the layers arent paint to layer, ie if compoundPaintToLayer isnt set to ALL
		if (window.compoundPaintToLayer != 2)
		{
			Physics.Raycast(ray, out hit, float.MaxValue);
		}
		else if (window.compoundLayerMask != -1)
			Physics.Raycast(ray, out hit, float.MaxValue, window.compoundLayerMask);

		if (hit.collider != null)
		{
			paintable = true;

			if (window.compoundPaintToSelection == true)
			{
				Transform hitObject = hit.collider.transform;
				Transform[] selectedObjects = Selection.transforms;
				bool contains = ArrayUtility.Contains(selectedObjects, hitObject);

				if (!contains)
				{
					hit = new RaycastHit();
					paintable = false;
				}
			}
		}

		else
			paintable = false;

		return hit;
	}

	static Vector3 cursorUpVector = Vector3.zero;
	static Vector3 cursorForwardVector = Vector3.zero;
	static Vector3 cursorPositionVector = Vector3.zero;
	private static void CalculateCursor(RaycastHit mouseRayHit)
	{
		cursorUpVector = Vector3.zero;
		cursorForwardVector = Vector3.zero;
		cursorPositionVector = Vector3.zero;

		if (mouseRayHit.collider != null)
		{
			cursorUpVector = mouseRayHit.normal;
			cursorPositionVector = mouseRayHit.point;
			cursorForwardVector = GetFlattenedDirection(Vector3.forward, cursorUpVector); //placement needs a direction to work with- we have no stroke direction yet, so we use flattened forward
		}

		cursorPoint.UpdatePoint(cursorPositionVector, cursorUpVector, cursorForwardVector);
	}

	private static qb_Group CreateGroup(string groupName)
	{
		GameObject newGroupObject = new GameObject("QB_Group_" + groupName);
		qb_Group newGroup = newGroupObject.AddComponent<qb_Group>();
		newGroup.groupName = groupName;

		groups.Add(newGroup);
		groupNames.Add(groupName);

		return newGroup;
	}

	//private static void UpdateGroups() //updates the groups and groupNames arrays based on what is in the scene
	private static List<string> UpdateGroups()
	{
		qb_Group[] groupsInScene =  GameObject.FindObjectsOfType(typeof(qb_Group)) as qb_Group[];

		groups.Clear();
		groupNames.Clear();

		for (int i = 0; i < groupsInScene.Length; i++)
		{
			groups.Add(groupsInScene[i]);
			groupNames.Add(groupsInScene[i].groupName);
		}

		return groupNames;
	}

	private static bool GroupWithNameExists(string groupName)
	{
		qb_Group[] groupsInScene =  GameObject.FindObjectsOfType(typeof(qb_Group)) as qb_Group[];

		bool exists = false;

		for (int i = 0; i < groupsInScene.Length; i++)
		{
			if (groupsInScene[i].groupName == groupName)
				exists = true;
		}

		return exists;
	}

	private static qb_Group GetGroupWithName(string groupName)
	{
		qb_Group[] groupsInScene =  GameObject.FindObjectsOfType(typeof(qb_Group)) as qb_Group[];

		for (int i = 0; i < groupsInScene.Length; i++)
		{
			if (groupsInScene[i].groupName == groupName)
				return groupsInScene[i];
		}

		return null;
	}

	static void AddPrefabList(List<Object> newPrefabs)
	{
		if (newPrefabs.Count > 0)
		{
			foreach (Object newPrefab in newPrefabs)
			{
				ArrayUtility.Add(ref window.liveTemplate.prefabGroup, new qb_PrefabObject(newPrefab, 1f));
			}

			newPrefabs.Clear();
			//	window.RefreshPrefabIcons();
		}
	}

	private void RemovePrefab(int itemIndex)
	{
		if (liveTemplate.selectedPrefabIndex > itemIndex)
		{
			liveTemplate.selectedPrefabIndex -= 1;
		}

		else if (liveTemplate.selectedPrefabIndex == itemIndex)
		{
			liveTemplate.selectedPrefabIndex = -1;
		}

		ArrayUtility.RemoveAt(ref liveTemplate.prefabGroup, itemIndex);

		liveTemplate.dirty = true;
		clearSelection = true;

		if (liveTemplate.prefabGroup.Length < 4)
			prefabPaneOpen = false;

		window.Repaint();
	}

	void OnDestroy()
	{
		brushMode = BrushMode.Off;
		SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
	}

	public KeyCode GetKeyUp { get { return Event.current.type == EventType.KeyUp ? Event.current.keyCode : KeyCode.None; } }

	#region Temp Erasing
	public GameObject[] sceneObjects = new GameObject[0];
	public void EraseObjects(List<int> indexList)
	{
		List<GameObject> removalList = new List<GameObject>();

		foreach (int index in indexList)
		{
			removalList.Add(sceneObjects[index]);
		}

		foreach (GameObject obj in removalList)
		{
			ArrayUtility.Remove(ref sceneObjects, obj);
			EraseObject(obj);
		}
	}

	public void EraseObject(GameObject obj)
	{
		Undo.DestroyObjectImmediate(obj);
	}

	public void VerifyObjects()
	{
		qb_Object[] objs = Object.FindObjectsOfType(typeof(qb_Object)) as qb_Object[];
		sceneObjects = new GameObject[objs.Length];

		for (int i = 0; i < sceneObjects.Length; i++)
		{
			sceneObjects[i] = objs[i].gameObject;
		}
	}

	public GameObject[] GetGroupObjects()
	{
		VerifyObjects();
		return sceneObjects;
	}
	#endregion

////////SAVE-ABLE BRUSHES

	private bool TryLoadTemplate(int tabIndex, string fileName)
	{
		return false;
	}

	//Attempt to save a template to file. This is the wrapper for SaveSettings()
	//It runs through the different permutations of a save situation and pops dialogs if needed
	private bool TrySaveTemplate(qb_Template template)
	{
		bool fileOnDisk = TemplateExistsOnDisk(template.brushName);
		//int alreadyLoadedIndex = TemplateAlreadyOpen(template.brushName);
		//File with this name exists on disk and the template we are attempting to save was not known by this name last time it was saved or loaded

		if (fileOnDisk == true && template.lastKnownAs != template.brushName)
		{
			if (EditorUtility.DisplayDialog("File Override", "A template file with this name exists on disk, override the file?", "Override", "Cancel"))
			{
				SaveSettings(template);
				return true;
			}

		}
		//Otherwise just save
		else
		{
			if (template.brushName == string.Empty)
			{
				EditorUtility.DisplayDialog("File Name Not Set", "A template file must be named before it can be saved", "Ok");

				//this needs to be replaced with a name entry dialog

				//Name Entry Field
				//Save (disabled unless a name exists,Cancel
				//this should basically call this same function again and return its bool when done so if cancel that one will return false or could rely on flow to return false
				//this should account for overrides etc we could pop back to the naming dialog if it returns false
			}

			else
			{
				SaveSettings(template);
				return true;
			}
		}

		//additional permutation
		//file with that name is already open in the tool

		return false;
	}

	//Save an open template to a file - this is the final commit of a save
	private void SaveSettings(qb_Template template)
	{
		if (template.brushName == string.Empty)
			return; //this should be a popup and already is - code should be unreachable, but left here as a safeguard

		template.lastKnownAs = template.brushName;
		template.dirty = false;
		qb_Utility.SaveToDisk(template, directory);
		UpdateTemplateSignatures();
		AssetDatabase.Refresh();
		window.Repaint();
	}

	private void ClearLiveTemplate()
	{
		clearSelection = true;
		liveTemplate = new qb_Template();//ScriptableObject.CreateInstance<qb_Template>();
	}

	private void RestoreTemplateDefaults()
	{
		if (liveTemplateIndex < 0)
			return;

		string	slotName = string.Empty;

		if (liveTemplate != null)
		{
			slotName = liveTemplate.brushName;
		}

		clearSelection = true;
		brushTemplates[liveTemplateIndex] = new qb_Template();
		brushTemplates[liveTemplateIndex].brushName = slotName;
		brushTemplates[liveTemplateIndex].lastKnownAs = slotName;

		SwitchToTab(liveTemplateIndex);
		liveTemplate.dirty = true;
		liveTemplate.live = true;
	}

	//Load settings into the window - from the file associated with the provided tab index
	private void SwitchToTab(int tabIndex)
	{	//Debug.Log(tabIndex);
		if (tabIndex == -1)
			liveTemplate = new qb_Template();

		else
		{
			if (brushTemplates[tabIndex] == null)
				return;

			liveTemplate = brushTemplates[tabIndex];

		}

		clearSelection = true;
		liveTemplateIndex = tabIndex;
		prefabPaneOpen = false;
		UpdateLayers();
		UpdateCompoundLayerMask();
		UpdateCompoundPaintToSelection();
		//	RefreshPrefabIcons();
	}

	private void UpdateTemplateSignatures()
	{
		templateSignatures = qb_Utility.GetTemplateFileSignatures(directory);
	}

	//Wrapper for CloseTab() - will throw dialogs if needed
	private void TryCloseTab(int tabIndex)
	{
		//Permutations
		//(1) File Exists on disk (implicit - has name)
		//Dialog - "" (Cancel, Save & Close, Close w/o Saving)

		//(2) File doesn't exist on disk
		// Template has no Name
		//Dialog - "" (Name Entry Field - (Cancel, Close & Save (disabled unless name is typed), Close w/o saving) )
		//if file with entered name exists on disk,
		//Dialog - ""

		//(3) Template has name
		//Dialog - (Cancel, Save & Close, Close w/o Saving)

		if (brushTemplates[tabIndex].dirty == true)
		{

			int option = EditorUtility.DisplayDialogComplex("Template Has Changed", "The Template in the tab you are trying to close has changed since it was last saved, close it without saving?", "Save and Close", "Close W/O Saving", "Cancel");

			switch (option)
			{

			case 0:
				if (TrySaveTemplate(brushTemplates[tabIndex]) == true)
				{
				goto case 1;
				}
				break;

			case 1:
				CloseTabProper(tabIndex);
				break;

			case 2:
				//nothing
				break;
			}
		}

		else
		{
			CloseTabProper(tabIndex);
		}

	}

	//Close tab and manage GUI
	private void CloseTabProper(int tabIndex)
	{
		string templateName = brushTemplates[tabIndex].brushName;

		CloseTab(tabIndex);

		string fName = templateName;
		if (fName == string.Empty)
			fName = "Unnamed";

		//string tabNum = (tabIndex + 1).ToString("00");
		QBLog("Closed Template: '" + fName + "'");

		int adjustedLiveTemplateIndex = ClearAdjustLiveTemplateIndex(tabIndex);

		//	if(adjustedLiveTemplateIndex != -1)
		SwitchToTab(adjustedLiveTemplateIndex);
	}

	private void CloseTab(int tabIndex)
	{
		ArrayUtility.RemoveAt(ref brushTemplates, tabIndex);

		SaveTempSlots();
		SaveToolState();
		UpdateAreActive();

		window.Repaint();
	}

	private int ClearAdjustLiveTemplateIndex(int removedIndex)
	{
		int count = brushTemplates.Length;
		int adjusted = -1;

		if (count == 0)
		{	return adjusted;	}

		if (liveTemplateIndex > removedIndex)
		{
			adjusted = liveTemplateIndex - 1;
		}
		else
		{
			adjusted = liveTemplateIndex;
		}

		if (liveTemplateIndex >= count)
		{
			adjusted = count - 1;
		}

		return adjusted;
	}

	//When qb is shutting down, save the slot info to user prefs
	private void SaveTempSlots()
	{	//Debug.Log("Saving Temp Slots");

		int count = brushTemplates.Length;
		EditorPrefs.SetInt("QB_templateCount", count);

		for (int i = 0; i < count; i++)
		{
			if (brushTemplates[i] != null) //&& brushTemplates[i].brushName != string.Empty)
			{	//EditorPrefs.SetString(prefix + i.ToString(),brushTemplates[i].brushName);
				qb_Utility.SaveToEditorPrefs(i, brushTemplates[i]);
			}
		}
	}

	//When qb starts up, we re-load the last session's slot contents
	private void LoadTempSlots()
	{
		int count = EditorPrefs.GetInt("QB_templateCount", 0);
		brushTemplates = new qb_Template[System.Math.Max(count, 1)];

		if (count != 0)
		{
			for (int i = 0; i < count; i++)
			{
				brushTemplates[i] =	qb_Utility.LoadFromEditorPrefs(i);
			}
		}
		else
		{
			// no previous template found, load a new one
			brushTemplates[0] = new qb_Template();
			brushTemplates[0].live = true;
			qb_Utility.SaveToEditorPrefs(0, brushTemplates[0]);
		}
	}

	private void QBLog(string message)
	{
		//managed by tool preferences
		if (!prefs_enableLog)
			return;

		Debug.Log("QuickBrush: "  + message);
	}

	[SerializeField] private GUIStyle groupMenuDropdownStyle;

	[SerializeField] private GUIStyle picButton_PrefabX;
	[SerializeField] private GUIStyle picButton_PrefabCheck;
	[SerializeField] private GUIStyle picButton_ResetSlider;
	[SerializeField] private GUIStyle picButton_SaveIcon;
	[SerializeField] private GUIStyle picButton_OpenFile;
	[SerializeField] private GUIStyle picButton_ClearSlot;
	[SerializeField] private GUIStyle picButton_SlotIcon_Active;
	[SerializeField] private GUIStyle picButton_SlotIcon_InActive;
	[SerializeField] private GUIStyle picButton_TemplateActive;
	[SerializeField] private GUIStyle picButton_OpenFileLarge;
	[SerializeField] private GUIStyle picButton_PrefabPaneDropdown_Closed;
	[SerializeField] private GUIStyle picButton_PrefabPaneDropdown_Open;

	[SerializeField] private GUIStyle prefabAddField_Small;
	[SerializeField] private GUIStyle prefabAddField_Span;
	[SerializeField] private GUIStyle prefabPanelCrop;

	[SerializeField] private GUIStyle sliderLabelStyle;
	[SerializeField] private GUIStyle prefabAmountSliderStyle;
	[SerializeField] private GUIStyle prefabAmountSliderThumbStyle;
	[SerializeField] private GUIStyle toggleButtonStyle;
	[SerializeField] private GUIStyle prefabPreviewWindowStyle;
	[SerializeField] private GUIStyle prefabFieldStyle;
	[SerializeField] private GUIStyle floatFieldCompressedStyle;

	[SerializeField] private GUIStyle picLabelStyle;
	[SerializeField] private GUIStyle menuBlockStyle;
	[SerializeField] private GUIStyle masterVerticalStyle;
	[SerializeField] private GUIStyle tipLabelStyle;
	[SerializeField] private GUIStyle brushSlotContainerStyle;
	[SerializeField] private GUIStyle slotActiveContainerStyle;
	[SerializeField] private GUIStyle picButton_TemplateDirty;
	[SerializeField] private GUIStyle brushStripStyle;

	[SerializeField] private GUIStyle shortToggleStyle;
	[SerializeField] private GUIStyle saveIconContainerStyle;

	static void SetStyleParameters()
	{
		window.prefabPanelCrop.margin = new RectOffset(0, 0, 0, -10);
		window.prefabPanelCrop.border = new RectOffset(0, 0, 0, 0);

		MakePicButtonBase(window.picButton_ResetSlider);
		window.picButton_ResetSlider.hover.background	= resetSliderIcon;
		window.picButton_ResetSlider.normal.background	= resetSliderIcon;
		window.picButton_ResetSlider.active.background	= resetSliderIcon_hover;

		MakePicButtonBase(window.picButton_SaveIcon);
		window.picButton_SaveIcon.hover.background	=	saveIcon_hover;
		window.picButton_SaveIcon.normal.background	=	saveIcon;
		window.picButton_SaveIcon.active.background	=	saveIcon;

		MakePicButtonBase(window.picButton_OpenFile);
		window.picButton_OpenFile.hover.background	=	loadBrushIcon;
		window.picButton_OpenFile.normal.background	=	loadBrushIcon;
		window.picButton_OpenFile.active.background	=	loadBrushIcon_hover;

		MakePicButtonBase(window.picButton_ClearSlot);
		window.picButton_ClearSlot.hover.background		=	clearBrushIcon;
		window.picButton_ClearSlot.normal.background	=	clearBrushIcon;
		window.picButton_ClearSlot.active.background	=	clearBrushIcon_hover;

		MakePicButtonBase(window.picButton_SlotIcon_InActive);
		window.picButton_SlotIcon_InActive.hover.background		=	savedBrushIcon;
		window.picButton_SlotIcon_InActive.normal.background	=	savedBrushIcon;
		window.picButton_SlotIcon_InActive.active.background	=	savedBrushIcon;

		MakePicButtonBase(window.picButton_SlotIcon_Active);
		window.picButton_SlotIcon_Active.hover.background	=	savedBrushIcon_Active;
		window.picButton_SlotIcon_Active.normal.background	=	savedBrushIcon_Active;
		window.picButton_SlotIcon_Active.active.background	=	savedBrushIcon_Active;

		MakePicButtonBase(window.picButton_PrefabX);
		window.picButton_PrefabX.hover.background	=	removePrefabXTexture_normal;
		window.picButton_PrefabX.normal.background	=	removePrefabXTexture_normal;
		window.picButton_PrefabX.active.background	=	removePrefabXTexture_hover;

		MakePicButtonBase(window.picButton_PrefabCheck);
		window.picButton_PrefabCheck.hover.background	=	selectPrefabCheckTexture_off;
		window.picButton_PrefabCheck.normal.background	=	selectPrefabCheckTexture_off;
		window.picButton_PrefabCheck.active.background	=	selectPrefabCheckTexture_active;

		MakePicButtonBase(window.picButton_TemplateActive);
		window.picButton_TemplateActive.hover.background	=	templateActiveIcon_off;
		window.picButton_TemplateActive.normal.background	=	templateActiveIcon_off;
		window.picButton_TemplateActive.active.background	=	templateActiveIcon_active;
		window.picButton_TemplateActive.fixedWidth = 16;
		window.picButton_TemplateActive.fixedHeight = 16;

		MakePicButtonBase(window.picButton_TemplateDirty);

		window.picButton_TemplateDirty.fixedWidth = 16;
		window.picButton_TemplateDirty.fixedHeight = 16;

		MakePicButtonBase(window.picButton_OpenFileLarge);
		window.picButton_OpenFileLarge.normal.background	=	loadBrushIconLarge;
		window.picButton_OpenFileLarge.hover.background		=	loadBrushIconLarge;
		window.picButton_OpenFileLarge.active.background	=	loadBrushIconLarge_hover;

		MakePicButtonBase(window.picButton_PrefabPaneDropdown_Closed);
		window.picButton_PrefabPaneDropdown_Closed.normal.background	=	prefabPaneDropdownIcon_closed_normal;
		window.picButton_PrefabPaneDropdown_Closed.hover.background 	=	prefabPaneDropdownIcon_closed_normal;
		window.picButton_PrefabPaneDropdown_Closed.active.background	=	prefabPaneDropdownIcon_closed_active;
		window.picButton_PrefabPaneDropdown_Closed.margin.left = 4;

		MakePicButtonBase(window.picButton_PrefabPaneDropdown_Open);
		window.picButton_PrefabPaneDropdown_Open.normal.background	=	prefabPaneDropdownIcon_open_normal;
		window.picButton_PrefabPaneDropdown_Open.hover.background	=	prefabPaneDropdownIcon_open_normal;
		window.picButton_PrefabPaneDropdown_Open.active.background	=	prefabPaneDropdownIcon_open_active;
		window.picButton_PrefabPaneDropdown_Open.margin.left = 4;
		window.picButton_PrefabPaneDropdown_Open.margin.top = -12;

		window.sliderLabelStyle.stretchWidth = false;

		window.sliderLabelStyle.padding.left = 0;
		window.sliderLabelStyle.padding.right = 0;
		window.sliderLabelStyle.margin.left = 0;
		window.sliderLabelStyle.margin.right = 0;

		window.masterVerticalStyle.margin.left = 0;
		window.masterVerticalStyle.margin.right = 0;
		window.masterVerticalStyle.padding.left = 0;
		window.masterVerticalStyle.padding.left = 0;

		window.prefabAmountSliderStyle.margin.top = 4;
		window.prefabAmountSliderThumbStyle.fixedHeight = 10;

		window.prefabPreviewWindowStyle.padding = new RectOffset(0, 0, 0, 0);
		window.prefabPreviewWindowStyle.margin = new RectOffset(0, 0, 4, 0);
		window.prefabPreviewWindowStyle.fixedHeight = 50;
		window.prefabPreviewWindowStyle.fixedWidth = 50;

		window.prefabAddField_Small.margin = new RectOffset(4, 0, 0, 0);
		window.prefabAddField_Small.padding = new RectOffset(0, 0, 0, 0);
		window.prefabAddField_Small.fixedHeight = 60;
		window.prefabAddField_Small.normal.background = addPrefabTexture;
		window.prefabAddField_Span.alignment = TextAnchor.MiddleCenter;

		window.prefabAddField_Span.margin = new RectOffset(4, 0, 0, 0);
		window.prefabAddField_Span.padding = new RectOffset(0, 0, 0, 0);
		window.prefabAddField_Span.fixedHeight = 60;
		window.prefabAddField_Span.normal.background = addPrefabFieldTexture;

		window.prefabFieldStyle.padding.left = 1;
		window.prefabFieldStyle.padding.right = 1;
		window.prefabFieldStyle.margin.top = 0;
		window.prefabFieldStyle.margin.left = 7;
		window.prefabFieldStyle.margin.right = 2;

		window.prefabFieldStyle.fixedHeight = 60;
		window.prefabFieldStyle.fixedWidth = 72;
		window.prefabFieldStyle.normal.background = prefabFieldBackgroundTexture;

		window.floatFieldCompressedStyle.fixedWidth = 50;
		window.floatFieldCompressedStyle.stretchWidth = false;

		window.picLabelStyle.padding = new RectOffset(0, 0, 0, 0);
		window.picLabelStyle.margin.left = 4;

		window.brushSlotContainerStyle.padding = new RectOffset(0, 0, 3, 0);
		window.brushSlotContainerStyle.margin.left = 10;
		window.brushSlotContainerStyle.margin.right = 12;

		window.brushSlotContainerStyle.overflow.left = 8;
		window.brushSlotContainerStyle.overflow.right = 8;
		window.brushSlotContainerStyle.overflow.bottom = 5;//12;

		window.slotActiveContainerStyle.padding = new RectOffset(0, 0, 0, 0);
		window.slotActiveContainerStyle.margin.left = 10;
		window.slotActiveContainerStyle.margin.right = 12;
		window.slotActiveContainerStyle.fixedWidth = 32;
		window.slotActiveContainerStyle.fixedHeight = 16;

		window.saveIconContainerStyle.padding = new RectOffset(0, 0, 0, 0);
		window.saveIconContainerStyle.margin = new RectOffset(2, 2, 3, 3);

		window.menuBlockStyle.margin.right = 0;

		window.brushStripStyle.margin.right = 0;

		window.tipLabelStyle.fontSize = 8;
		window.tipLabelStyle.padding.top = 0;


		window.shortToggleStyle.overflow = new RectOffset(80, 0, -3, 0);
	}

	static void MakePicButtonBase(GUIStyle style)
	{
		style.padding = new RectOffset(0, 0, 0, 0);
		style.margin = new RectOffset(0, 0, 0, 0);
		style.border = new RectOffset(0, 0, 0, 0);

		style.alignment = TextAnchor.UpperCenter;
		style.fontStyle = FontStyle.Bold;
		style.hover.textColor = Color.black;
		style.normal.textColor = Color.black;
	}

	static void BuildStyles()
	{
		try
		{
			window.groupMenuDropdownStyle = new GUIStyle(EditorStyles.popup);
			window.prefabPanelCrop = new GUIStyle();

			window.picButton_PrefabX = new GUIStyle(EditorStyles.label);
			window.picButton_PrefabCheck = new GUIStyle(EditorStyles.label);
			window.picButton_ResetSlider = new GUIStyle(EditorStyles.label);
			window.picButton_SaveIcon = new GUIStyle(EditorStyles.label);
			window.picButton_OpenFile = new GUIStyle(EditorStyles.label);
			window.picButton_ClearSlot = new GUIStyle(EditorStyles.label);
			window.picButton_SlotIcon_Active = new GUIStyle(EditorStyles.label);
			window.picButton_SlotIcon_InActive = new GUIStyle(EditorStyles.label);
			window.picButton_TemplateActive = new GUIStyle(EditorStyles.label);
			window.picButton_PrefabPaneDropdown_Closed = new GUIStyle(EditorStyles.label);
			window.picButton_PrefabPaneDropdown_Open = new GUIStyle(EditorStyles.label);
			window.picButton_TemplateDirty = new GUIStyle(EditorStyles.label);

			window.picButton_OpenFileLarge = new GUIStyle(EditorStyles.label);

			window.prefabAddField_Small =	new GUIStyle(EditorStyles.label);
			window.prefabAddField_Span =	new GUIStyle(EditorStyles.label);

			window.sliderLabelStyle = new GUIStyle(EditorStyles.label);
			window.masterVerticalStyle = new GUIStyle(EditorStyles.label);
			window.prefabAmountSliderStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).verticalSlider);
			window.prefabAmountSliderThumbStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).verticalSliderThumb);
			window.toggleButtonStyle = new GUIStyle(EditorStyles.radioButton); //new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene).toggle);//
			window.floatFieldCompressedStyle = new GUIStyle(EditorStyles.textField); //new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene).textField);//
			window.prefabPreviewWindowStyle = new GUIStyle(EditorStyles.label); //new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene).button);

			window.prefabFieldStyle = new GUIStyle(EditorStyles.label); //new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene).textField);
			window.picLabelStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).label);//new GUIStyle(EditorStyles.miniButton); //new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene).button);

			window.menuBlockStyle = new GUIStyle(EditorStyles.textField);//new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).textArea);//new GUIStyle(EditorStyles.textField); //new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene).textField);
			window.tipLabelStyle = new GUIStyle(EditorStyles.label);
			window.brushSlotContainerStyle = new GUIStyle(EditorStyles.label);
			window.slotActiveContainerStyle = new GUIStyle(EditorStyles.label);
			window.saveIconContainerStyle = new GUIStyle(EditorStyles.label);
			window.brushStripStyle = new GUIStyle(EditorStyles.label);
			window.shortToggleStyle = new GUIStyle(EditorStyles.toggle);
		}

		catch (System.Exception err)
		{
			err.Data.Clear();
		}

		SetStyleParameters();
		window.builtStyles = true;
	}

	//Loads the icon textures from the skin folder
	private static void LoadTextures()
	{
		string skinPath = "Skin/";

		addPrefabTexture				=		Resources.Load<Texture2D>(skinPath + "qb_addPrefabIcon");
		addPrefabFieldTexture			=		Resources.Load<Texture2D>(skinPath + "qb_addPrefabField");
		removePrefabXTexture_normal		=		Resources.Load<Texture2D>(skinPath + "qb_removePrefabXIcon_normal");
		removePrefabXTexture_hover 		=		Resources.Load<Texture2D>(skinPath + "qb_removePrefabXIcon_hover");

		selectPrefabCheckTexture_off	=		Resources.Load<Texture2D>(skinPath + "qb_selectPrefabCheck_off");
		selectPrefabCheckTexture_on 	=		Resources.Load<Texture2D>(skinPath + "qb_selectPrefabCheck_on");
		selectPrefabCheckTexture_active =		Resources.Load<Texture2D>(skinPath + "qb_selectPrefabCheck_active");

		prefabFieldBackgroundTexture 	=		Resources.Load<Texture2D>(skinPath + "qb_prefabFieldBackground");

		brushIcon_Active 				=		Resources.Load<Texture2D>(skinPath + "qb_brushIcon_Active");
		brushIcon_Inactive 				=		Resources.Load<Texture2D>(skinPath + "qb_brushIcon_Inactive");
		brushIcon_Locked				=		Resources.Load<Texture2D>(skinPath + "qb_brushIcon_Locked");

		eraserIcon_Active				=		Resources.Load<Texture2D>(skinPath + "qb_eraserIcon_Active");
		eraserIcon_Inactive				=		Resources.Load<Texture2D>(skinPath + "qb_eraserIcon_Inactive");

		placementIcon_Active			=		Resources.Load<Texture2D>(skinPath + "qb_placementIcon_Active");

		savedBrushIcon					=		Resources.Load<Texture2D>(skinPath + "qb_SavedBrushIcon");
		savedBrushIcon_Active			=		Resources.Load<Texture2D>(skinPath + "qb_SavedBrushIcon_Active");

		loadBrushIcon					=		Resources.Load<Texture2D>(skinPath + "qb_LoadBrushIcon");
		loadBrushIcon_hover				=		Resources.Load<Texture2D>(skinPath + "qb_LoadBrushIcon_hover");
		loadBrushIconLarge				=		Resources.Load<Texture2D>(skinPath + "qb_LoadBrushIconLarge");
		loadBrushIconLarge_hover		=		Resources.Load<Texture2D>(skinPath + "qb_LoadBrushIconLarge_hover");

		saveIcon						=		Resources.Load<Texture2D>(skinPath + "qb_SaveIcon");
		saveIcon_hover					=		Resources.Load<Texture2D>(skinPath + "qb_SaveIcon_hover");

		clearBrushIcon					= 		Resources.Load<Texture2D>(skinPath + "qb_ClearBrushIcon");
		clearBrushIcon_hover			=		Resources.Load<Texture2D>(skinPath + "qb_ClearBrushIcon_hover");

		resetSliderIcon					=		Resources.Load<Texture2D>(skinPath + "qb_ResetSliderIcon");
		resetSliderIcon_hover			=		Resources.Load<Texture2D>(skinPath + "qb_ResetSliderIcon_hover");

		templateActiveIcon_on			=		Resources.Load<Texture2D>(skinPath + "qb_TemplateActiveIcon_on");
		templateActiveIcon_off			=		Resources.Load<Texture2D>(skinPath + "qb_TemplateActiveIcon_off");
		templateActiveIcon_active		=		Resources.Load<Texture2D>(skinPath + "qb_TemplateActiveIcon_active");
		templateTabBackground			=		Resources.Load<Texture2D>(skinPath + "qb_TemplateTabBackground");
		templateTabBackground_inactive	=		Resources.Load<Texture2D>(skinPath + "qb_TemplateTabBackground_inactive");

		templateDirtyAsterisk			=		Resources.Load<Texture2D>(skinPath + "qb_TemplateDirtyAsterisk");

		prefabPaneDropdownIcon_closed_normal =	Resources.Load<Texture2D>(skinPath + "qb_prefabPaneDropdownIcon_closed_normal");
		prefabPaneDropdownIcon_closed_active =	Resources.Load<Texture2D>(skinPath + "qb_prefabPaneDropdownIcon_closed_active");
		prefabPaneDropdownIcon_open_normal =	Resources.Load<Texture2D>(skinPath + "qb_prefabPaneDropdownIcon_open_normal");
		prefabPaneDropdownIcon_open_active =	Resources.Load<Texture2D>(skinPath + "qb_prefabPaneDropdownIcon_open_active");
	}

	static string[] GetLayerList()
	{
		string[] layerArray = new string[32];

		for (int i = 0; i < 32; i++)
		{
			string name = LayerMask.LayerToName(i);
			//if(name == string.Empty)
			//	name = "    ";//"undefined";

			layerArray[i] = name;
		}
		//Debug.Log("Getting Layer List");

		return layerArray;
	}

}

