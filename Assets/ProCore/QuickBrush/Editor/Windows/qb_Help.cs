//	QuickBrush: Prefab Placement Tool
//	by PlayTangent
//	all rights reserved
//	www.procore3d.com

using UnityEngine;
using UnityEditor;
using System.Collections;

public class qb_Help : qb_Window
{
	[MenuItem ("Tools/QuickBrush/Help", false, 2)]
	public static void ShowWindow()
	{
		window = EditorWindow.GetWindow<qb_Help>(true, "QB Help", true);
		window.position = new Rect(50,50,400,600);
    }

	static Vector2 sliderVal;

	public override void OnGUI()
	{
		base.OnGUI();

		EditorGUILayout.Space();

		if(GUILayout.Button("Open HTML Documentation"))
			Application.OpenURL("http://www.protoolsforunity3d.com/docs/quickbrush/");

		sliderVal = EditorGUILayout.BeginScrollView(sliderVal,false,false);

		EditorGUILayout.LabelField("Basic Usage",  EditorStyles.boldLabel);

		EditorGUILayout.BeginVertical(menuBlockStyle);
			MenuListItem(true,"When you first open QuickBrush the window is inactive, look at the bar near the bottom of the QB window. You should see a text entry field for template name and a single tab with an 'Open File' icon.");
			MenuListItem(true,"Create a new brush template by clicking on the open folder symbol on the empty tab. Once some more tabs are open, there will be a similar icon at the top of each tab. It can be used to open and create templates");
			MenuListItem(true,"To create a new template, Click on the 'Open Folder' symbol and select 'New Template' from the dropdown that appears");
			MenuListItem(true,"Once you've saved some templates, this drop down will also contain the names of your saved templates, allowing you to select a template to load.");
			MenuListItem(true,"To save a new brush template to disk, type a name into the text field located directly above the template tabs and then click on the save icon next to the field");
			MenuListItem(true,"You can switch between tabs by clicking on the tab to which you would like to switch. The template name is displayed in the help box at the bottom of the QB window when is hovered over a tab for a moment");
			MenuListItem(true,"To set up your brush, drag and drop a prefab (or prefabs) onto the outlined region near the top of the QuickBrush window. This adds it to the prefab list pane");
			MenuListItem(true,"Once in the list pane, each prefab item has a slider, a preview window, and two overlaid controls");
			MenuListItem(true,"The Slider dictates how likely each prefab in the list is to be spawned vs the others when using the brush to place objects");
			MenuListItem(true,"Clicking the 'Red X' overlaid button removes the item from the list");
			MenuListItem(true,"Clicking the 'Green Checkmark' toggles whether an object will be placed exclussively.");
			MenuListItem(true,"You'll notice another set of checkmarks at the bottom of the tool. These can be used to activate and de-activate templates. Activating multiple brushes allows you to paint with all of them simultaneously, with each one applying its own settings");
		EditorGUILayout.EndVertical();

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Painting Controls", EditorStyles.boldLabel);

		EditorGUILayout.BeginVertical(menuBlockStyle);
			MenuListItem(true,"The brush is toggled ON by holding down Ctrl/Cmd");
			MenuListItem(true,"To switch between painting and erasing, press the X key while holding down Ctrl/Cmd");
			MenuListItem(true,"To paint (or erase), click and drag with the mouse while holding Ctrl/Cmd");
			MenuListItem(true,"An additional percision placement mode is available by simultaneously holding down Shift and Ctr/Cmd");
			MenuListItem(true,"While precision placing, clicking the mouse spawns a single prefab. Dragging the cursor while still holding down the mouse allows you to scale and rotate the object being placed");
			MenuListItem(true,"To chose the object which will be percision placed using this control, toggle the green checkmark overlaying the preview pane for the prefab in the prefab list pane.");
			MenuListItem(true,"If no object is selected, QuickBrush will use the first (or leftmost) item in the list belonging to the currently active brush template");
		EditorGUILayout.EndVertical();

		EditorGUILayout.Space();

		EditorGUILayout.LabelField("FAQ (will expand with feedback)", EditorStyles.boldLabel);

		EditorGUILayout.BeginVertical(menuBlockStyle);

			EditorGUILayout.LabelField("Q: When I paint, my objects stack on top of one another instead of painting onto my chosen surface. What gives?", EditorStyles.wordWrappedLabel);
			EditorGUILayout.Space();

			EditorGUILayout.LabelField("A: It is likely that your prefabs have colliders and are layered such that QuickBrush is painting them onto one another. Use the layer settings in order to paint to a different layer than the one your prefabs are on.", EditorStyles.wordWrappedLabel);
			EditorGUILayout.Space();

		EditorGUILayout.EndVertical();

		EditorGUILayout.EndScrollView();
	}


}
