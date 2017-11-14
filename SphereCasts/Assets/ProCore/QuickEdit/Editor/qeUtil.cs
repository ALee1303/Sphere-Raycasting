using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace QuickEdit
{
	/**
	 *	\brief Frequently used extensions.
	 */
	internal static class qeUtil
	{
#region UNDO

		/**
		 * Do this crazy undo dance because calling Undo.RecordObject(UnityEngine.mesh, "") is insanely slow.
		 */
		public static void RecordMeshUndo(qe_Mesh mesh, string message)
		{
			Mesh m = qe_Mesh_Utility.Clone( mesh.cloneMesh );
			Undo.RegisterCreatedObjectUndo(m, message);

			Mesh old = mesh.cloneMesh;

			Undo.RecordObject(mesh, message);
			{
				mesh.cloneMesh = m;
				mesh.Apply();
			}

			Undo.DestroyObjectImmediate(old);
			
			#if UNITY_5
			Undo.SetCurrentGroupName(message);
			#endif
		}
#endregion

#region ARRAY / LIST UTILITY

	    public static T[] ValuesWithIndices<T>(T[] arr, IList<int> indices)
		{
			T[] vals = new T[indices.Count];
			for(int i = 0; i < indices.Count; i++)
				vals[i] = arr[indices[i]];
			return vals;
		}

		public static T[] FilledArray<T>(T val, int length)
		{
			T[] arr = new T[length];
			for(int i = 0; i < length; i++) {
				arr[i] = val;
			}
			return arr;
		}

		/**
		 * Holds a start and end index for a binary search.
		 */
		private struct SearchRange
		{
			public int begin, end;

			public SearchRange(int begin, int end)
			{
				this.begin = begin;
				this.end = end;
			}

			public bool Valid() { return end - begin > 1; }
			public int Center() { return begin + (end-begin)/2; }

			public override string ToString()
			{
				return "{" + begin + ", " + end + "} : " + Center();
			}
		}

		/**
		 * Given a sorted list and value, returns the index of the greatest value in sorted_list that is 
		 * less than value.  Ex: List( { 0, 1, 4, 6, 7 } ) Value(5) returns 2, which is the index of value 
		 * 4.
		 * If value is less than sorted[0], -1 is returned.  If value is greater than sorted[end], sorted.Count-1 
		 * is returned.  If an exact match is found, the index prior to that match is returned.
		 */
		public static int NearestIndexPriorToValue<T>(IList<T> sorted_list, T value) where T : System.IComparable<T>
		{
			int count = sorted_list.Count;
			if(count < 1) return -1;

			SearchRange range = new SearchRange(0, count-1);

			if(value.CompareTo(sorted_list[0]) < 0)
				return -1;

			if(value.CompareTo(sorted_list[count-1]) > 0)
				return count-1;

			while( range.Valid() )
			{
				if( sorted_list[range.Center()].CompareTo(value) > 0)
				{
					// sb.AppendLine(sorted_list[range.Center()] + " > " + value + " [" + range.begin + ", " + range.end +"] -> [" + range.begin + ", " + range.Center() + "]");
					range.end = range.Center();
				}
				else
				{
					// sb.AppendLine(sorted_list[range.Center()] + " < " + value + " [" + range.begin + ", " + range.end +"] -> [" + range.Center() + ", " + range.end + "]");
					range.begin = range.Center();

					if( sorted_list[range.begin+1].CompareTo(value) >= 0 )
					{
						return range.begin;
					}
				}
			}
		
			return 0;
		}
#endregion

#region STRING UTILITY

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
		public static string ToFormattedString<T>(this IList<T> t, string _delimiter)
		{
			return t.ToFormattedString(_delimiter, 0, -1);
		}

		public static string ToFormattedString<T>(this IList<T> t, string _delimiter, int entriesPerLine, int maxEntries)
		{
			int len = maxEntries > 0 ? (int)Mathf.Min(t.Count, maxEntries) : t.Count;
			if(t == null || len < 1)
				return "Empty Array.";

			StringBuilder str = new StringBuilder();

			// str.Append(_delimiter.Replace("\n", "") + (t[0] == null ? "null" : t[0].ToString()) + _delimiter );

			for(int i = 0; i < len-1; i++)
			{
				if(entriesPerLine > 0 && (i+1) % entriesPerLine == 0)
					str.AppendLine( ((t[i] == null) ? "null" : t[i].ToString()) + _delimiter );
				else
					str.Append( ((t[i] == null) ? "null" : t[i].ToString()) + _delimiter);
			}
			
			str.Append( (t[len-1] == null) ? "null" : t[len-1].ToString() );

			return str.ToString();		
		}

		/**
		 *	\brief Returns a string formatted by the passed seperator parameter.
		 *	\code{cs}
		 *	List<int> myList = new List<int>(){0, 1, 2};
		 *
		 *	// Prints "0, 1, 2"
		 *	Debug.Log(myList.ToFormattedString(", "));
		 *	@param _delimiter Inserts this string between entries.
		 *	\returns Formatted string.
		 */
		public static string ToFormattedString<T>(this List<T> t, string _delimiter)
		{
			return t.ToArray().ToFormattedString(_delimiter);
		}

		public static string ToFormattedString<T>(this HashSet<T> t, string _delimiter)
		{
			return t.ToArray().ToFormattedString(_delimiter);
		}
#endregion
	}
}