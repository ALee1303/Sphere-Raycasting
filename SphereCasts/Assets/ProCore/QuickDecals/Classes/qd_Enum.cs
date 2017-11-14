using UnityEngine;
using System.Collections;

namespace ProCore.Decals
{
	public enum DecalView
	{
		Organizational,
		Atlas
	}

	public enum Placement
	{
		Fixed,
		Random
	}

	public static class qd_Const
	{
		// not really enums...
		public static int[] ATLAS_SIZES = new int[]
		{
			32,
			64,
			128,
			256,
			512,
			1024,
			2048,
			4096,
			8192
		};

		public static string[] ATLAS_SIZES_STRING = new string[]
		{
			"32",
			"64",
			"128",
			"256",
			"512",
			"1024",
			"2048",
			"4096",
			"8192"
		};	
	}
}