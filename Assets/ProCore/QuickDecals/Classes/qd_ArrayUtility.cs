#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;

namespace ProCore.Decals
{
public class qd_ArrayUtility
{
	/**
	 * Inserts a range of T[] at index.
	 */
	public static bool InsertRange<T>(ref T[] source, int index, T[] range)
	{
		if(index >= source.Length || index < 0)
		{
			ArrayUtility.AddRange(ref source, range);
			return true;
		}	

		T[] full = new T[source.Length+range.Length];
		System.Array.Copy(source, 0, full, 0, index);
		System.Array.Copy(range, 0, full, index, range.Length);
		System.Array.Copy(source, index, full, index+range.Length, source.Length-index);

		source = full;

		return true;
	}
}
}
#endif