//	QuickBrush: Prefab Placement Tool
//	by PlayTangent
//	all rights reserved
//	www.procore3d.com

using UnityEngine;
using UnityEditor;
using System.Collections;

public class qb_Window : EditorWindow
{
	public static qb_Window window;

	private bool builtStyles = false;

	void OnEnable()
	{
		window = this;
		LoadTextures();
	}

	public virtual void OnGUI()
	{
		if(builtStyles == false)
			BuildStyles();
	}

	protected static void MenuListItem(bool bulleted, bool centered, string text)
	{
		EditorGUILayout.BeginHorizontal();

		if(bulleted)
			GUILayout.Label(bulletPointTexture, window.bulletPointStyle);

		if(centered)
		{
			EditorGUILayout.LabelField(text, window.labelStyle_centered);
		}
		else
		{
			EditorGUILayout.LabelField(text, EditorStyles.wordWrappedLabel);
			GUILayout.FlexibleSpace();
		}

		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Space();
	}

	protected static void MenuListItem(bool bulleted, string text)
	{
		MenuListItem(bulleted, false, text);
	}

	protected static void MenuListItem(string text)
	{
		MenuListItem(false, false, text);
	}

	protected static void LoadTextures()
	{
		window.DoLoadTextures();
	}
	protected virtual void DoLoadTextures()
	{
		string guiPath = "Skin/";
		bulletPointTexture 	= Resources.Load<Texture2D>(guiPath + "qb_bullet");
	}

#region Shared Textures
	static Texture2D bulletPointTexture;
#endregion

#region Shared Styles
	[SerializeField] protected GUIStyle labelStyle_centered;
	[SerializeField] protected GUIStyle menuBlockStyle;
	[SerializeField] protected GUIStyle bulletPointStyle;
#endregion

    protected void BuildStyles()
    {
    	DoBuildStyles();
		builtStyles = true;
	}

	protected virtual void DoBuildStyles()
	{
		labelStyle_centered = new GUIStyle(EditorStyles.wordWrappedLabel);
		labelStyle_centered.alignment = TextAnchor.MiddleCenter;

		bulletPointStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).label);
		bulletPointStyle.margin = new RectOffset(0,0,0,0);
		bulletPointStyle.padding = new RectOffset(0,0,0,0);

		menuBlockStyle = new GUIStyle(EditorStyles.textField);
		menuBlockStyle.alignment = TextAnchor.UpperLeft;
	}

}
