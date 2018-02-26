#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections;

public class qdUndo : Editor {

	public static void RecordObject(Object obj, string msg)
	{
		Undo.RecordObject(obj, msg);
	}

	public static void RecordObjects(Object[] objs, string msg)
	{
		Undo.RecordObjects(objs, msg);
	}

	public static void DestroyImmediate(Object obj, string msg)
	{
		Undo.DestroyObjectImmediate(obj);
	}
}
#endif