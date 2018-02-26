using UnityEngine;
using UnityEditor;
using System.Collections;

public class qd_Preferences : Editor
{
	static bool preferencesLoaded = false;

	static bool ParentToHitTransform = false;

	[PreferenceItem("QuickDecals")]
	public static void OnGUI()
	{
		if(!preferencesLoaded)
			LoadPreferences();

		EditorGUI.BeginChangeCheck();


		ParentToHitTransform = EditorGUILayout.Toggle("Hit Transform is Decal Parent", ParentToHitTransform);

		if(EditorGUI.EndChangeCheck())
			SavePreferences();
	}

	static void LoadPreferences()
	{
		ParentToHitTransform = GetBool(qd_Constant.ParentToHitTransform);
		preferencesLoaded = true;
	}

	static void SavePreferences()
	{
		EditorPrefs.SetBool(qd_Constant.ParentToHitTransform, ParentToHitTransform);
	}

	public static bool GetBool(string key)
	{
		switch(key)
		{
			case qd_Constant.ParentToHitTransform:
				return EditorPrefs.HasKey(key) ? EditorPrefs.GetBool(key) : false;

			default:
				return true;
		}
	}
}