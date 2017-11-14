//	QuickBrush: Prefab Placement Tool
//	by PlayTangent
//	all rights reserved
//	www.procore3d.com

using UnityEngine;
using UnityEditor;
using System.Collections;

public class qb_About : qb_Window
{
	[MenuItem ("Tools/QuickBrush/About", false, 0)]
	public static void ShowWindow()
	{
		window = EditorWindow.GetWindow<qb_About>(true, "QuickBrush About", true);

	 	window.position = new Rect(50,50,284,200);
		window.minSize = new Vector2(284f,100f);
		window.maxSize = new Vector2(284f,140f);
	}

	const string RELEASE_VERSION = "1.2.1f0";

	static Texture2D bulletPointTexture;

	GUIStyle centeredLargeLabel = null;
	bool initialized = false;

	void BeginHorizontalCenter()
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
	}

	void EndHorizontalCenter()
	{
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}

	public override void OnGUI()
	{
		base.OnGUI();

		if(!initialized)
		{
			centeredLargeLabel = new GUIStyle( EditorStyles.largeLabel );
			centeredLargeLabel.fontSize = 18;
			centeredLargeLabel.fontStyle = FontStyle.Bold;
			centeredLargeLabel.alignment = TextAnchor.MiddleCenter;
		}

		GUILayout.Space(12);

		GUILayout.Label("QuickBrush " + RELEASE_VERSION, centeredLargeLabel);

		GUILayout.Space(12);

		BeginHorizontalCenter();
		if(GUILayout.Button(" Documentation "))
			Application.OpenURL("http://www.procore3d.com/docs/quickbrush");
		EndHorizontalCenter();

		BeginHorizontalCenter();
		if(GUILayout.Button(" Website "))
			Application.OpenURL("http://www.procore3d.com/quickbrush");

		if(GUILayout.Button(" Contact "))
			Application.OpenURL("mailto:contact@procore3d.com");
		EndHorizontalCenter();
	}
}
