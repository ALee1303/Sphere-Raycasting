using UnityEditor;
using UnityEngine;

namespace QuickEdit
{
	/**
	 * Methods used in manipulating or creating Lightmaps.
	 */
	public static class qe_Lightmapping
	{
		/**
		 * Editor-only extension to qe_Mesh generates lightmap UVs.
		 */
	    public static void GenerateUV2(this Mesh mesh) { mesh.GenerateUV2(false); }

		public static void GenerateUV2(this Mesh mesh, bool forceUpdate)
		{
			// SetUVParams(8f, 15f, 15f, 20f);
			UnwrapParam param;
			UnwrapParam.SetDefaults(out param);
			
			Unwrapping.GenerateSecondaryUVSet(mesh, param);

			EditorUtility.SetDirty(mesh as Object);
		}

		/**
		 * Store the previous GIWorkflowMode and set the current value to OnDemand (or leave it Legacy).
		 */
		[System.Diagnostics.Conditional("UNITY_5")]
		internal static void PushGIWorkflowMode()
		{
	#if UNITY_5
			EditorPrefs.SetInt("qe_GIWorkflowMode", (int)Lightmapping.giWorkflowMode);

			if(Lightmapping.giWorkflowMode != Lightmapping.GIWorkflowMode.Legacy)
				Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
	#endif
		}

		/**
		 * Return GIWorkflowMode to it's prior state.
		 */
		[System.Diagnostics.Conditional("UNITY_5")]
		internal static void PopGIWorkflowMode()
		{
	#if UNITY_5
			// if no key found (?), don't do anything.
			if(!EditorPrefs.HasKey("qe_GIWorkflowMode"))
				return;

			 Lightmapping.giWorkflowMode = (Lightmapping.GIWorkflowMode)EditorPrefs.GetInt("qe_GIWorkflowMode");
	#endif
		}
	}
}