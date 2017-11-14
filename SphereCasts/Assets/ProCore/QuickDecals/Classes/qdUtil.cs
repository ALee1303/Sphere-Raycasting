using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ProCore.Decals
{
public static class qdUtil
{
	/**
	 *	\brief Returns a GameObject array containing all
	 *	decals in the scene with the matching texture.
	 */
	public static GameObject[] FindDecalsWithTexture(Texture2D img)
	{
		List<GameObject> matches = new List<GameObject>();

		foreach(qd_Decal decal in GameObject.FindObjectsOfType(typeof(qd_Decal)))
			if(decal.texture == img)
				matches.Add(decal.gameObject);
		
		return matches.ToArray();
	}

	/**
	 *	\brief Finds all decals in the scene that match an image in this decalgroup,
	 *	then refreshes the UV coordinate and Material.
	 */
	public static void RefreshSceneDecals(DecalGroup dg)
	{
		if(!dg.isPacked)
		{
			Debug.LogWarning("Attempting to RefreshSceneDecals without a packed material");
			return;
		}
		
		qd_Decal[] sceneDecals = (qd_Decal[])GameObject.FindObjectsOfType(typeof(qd_Decal));

		for(int i = 0; i < dg.decals.Count; i++)
		{
			foreach(qd_Decal decal in sceneDecals)
			{
				if(dg.decals[i].texture == decal.texture)
				{
					decal.GetComponent<MeshRenderer>().sharedMaterial = dg.material;
					decal.SetUVRect(dg.decals[i].atlasRect);
				}
			}
		}
	}

	/**
	 * Probably should have Decal implement IComparable and do it that way, but I'm
	 * unsure of a good way to account for the decalview switch using that method
	 */
	public static void SortDecalsUsingView(ref List<Decal> decals, DecalView decalView)
	{
		List<Decal> dec = new List<Decal>();

		foreach(Decal d in decals)
		{
			int ind = decalView == DecalView.Organizational ? d.orgIndex : d.atlasIndex;
			int smallestIndex = dec.Count;
			for(int i = dec.Count-1; i > -1; i--)
			{
				if(ind < (decalView == DecalView.Atlas ? dec[i].atlasIndex : dec[i].orgIndex) && ind < smallestIndex)
				{
					smallestIndex = ind;
				}
			}
			dec.Insert(smallestIndex, d);
		}

		decals = dec;
	}

	public static bool Contains(this Dictionary<int, List<int>> dic, int key, int val)
	{
		return dic.ContainsKey(key) && dic[key].Contains(val);
	}

	public static void Add(this Dictionary<int, List<int>> dic, int key, int val)
	{
		if(key < 0 || val < 0) return;
		
		if(dic.ContainsKey(key))
		{
			if(!dic[key].Contains(val))
				dic[key].Add(val);
		}
		else
			dic.Add(key, new List<int>(){val});
	}

	// debug
	public static string ToFormattedString(this Dictionary<int, List<int>> dic)
	{
		string t = "";
		foreach(KeyValuePair<int, List<int>> kvp in dic)
			t += kvp.Key + " : " + kvp.Value.ToFormattedString(", ") + "\n";
		return t;
	}

	/**
	 *	\brief Returns a string formatted by the passed seperator parameter.
	 *	\code{cs}
	 *	int[] myArray = new int[3]{0, 1, 2};
	 *
	 *	// Prints "0, 1, 2"
	 *	Debug.Log(myArray.ToFormattedString(", "));
	 *	@param _delimiter Inserts this string between entries.
	 *	\returns Formatted string.
	 */
	public static string ToFormattedString<T>(this T[] t, string _delimiter)
	{
		if(t == null || t.Length < 1)
			return "Empty Array.";

		StringBuilder str = new StringBuilder();

		str.Append(t[0].ToString());
		for(int i = 1; i < t.Length; i++) {
			str.Append(_delimiter + ((t[i] == null) ? "null" : t[i].ToString()));
		}
		return str.ToString();		
	}

	public static string ToFormattedString<T>(this List<T> t, string _delimiter)
	{
		return t.ToArray().ToFormattedString(_delimiter);
	}
}
}