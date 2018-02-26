using UnityEngine;
using UnityEditor;
using System.Collections;

/**
 * JS compiled in Editor pass doesn't know about CS compiled in Editor pass.
 */
public class qe_About : Editor
{
	[MenuItem("Tools/QuickEdit/About", false, 0)]
	public static void MenuAbout ()
	{
		qe_AboutWindow.Init("Assets/ProCore/QuickEdit/About/pc_AboutEntry_QuickEdit.txt", true);
	}
}
