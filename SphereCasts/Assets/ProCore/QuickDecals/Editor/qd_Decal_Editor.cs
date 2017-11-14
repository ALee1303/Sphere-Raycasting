using UnityEngine;
using UnityEditor;
using System.Collections;
using ProCore.Decals;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[CustomEditor(typeof(qd_Decal))]
public class qd_Decal_Editor : Editor
{
	public override void OnInspectorGUI()
	{
		Color bc = GUI.backgroundColor;
		GUI.backgroundColor = Color.green;
		if(GUILayout.Button("Open QuickDecals"))
			qd_Editor.MenuOpenDecalsWindow();
		GUI.backgroundColor = bc;

		if(((qd_Decal)target).transform.localScale != Vector3.one)
		{
			GUILayout.Space(5);

			if(GUILayout.Button("Freeze Transform"))
				((qd_Decal)target).FreezeTransform();
		}
	}
}
