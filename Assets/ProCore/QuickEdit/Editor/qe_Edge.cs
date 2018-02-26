using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace QuickEdit
{
	[System.Serializable]
	public class qe_Edge : System.IEquatable<qe_Edge>
	{
		public int x, y;

		public qe_Edge(int _x, int _y)
		{
			x = _x;
			y = _y;
		}

		public qe_Edge(qe_Edge edge)
		{
			x = edge.x;
			y = edge.y;
		}

		public bool IsValid()
		{
			return x > -1 && y > -1 && x != y;
		}

		public override string ToString()
		{
			return "[" + x + ", " + y + "]";
		}

		public bool Equals(qe_Edge edge)
		{
			return (this.x == edge.x && this.y == edge.y) || (this.x == edge.y && this.y == edge.x);
		}

		public override bool Equals(System.Object b)
		{
			return b is qe_Edge && (this.x == ((qe_Edge)b).x || this.x == ((qe_Edge)b).y) && (this.y == ((qe_Edge)b).x || this.y == ((qe_Edge)b).y);
		}

		public override int GetHashCode()
		{
			int hashX;
			int hashY;

			if(x < y)
			{
				hashX = x.GetHashCode();
				hashY = y.GetHashCode();	
			}
			else
			{
				hashX = y.GetHashCode();
				hashY = x.GetHashCode();
			}

			//Calculate the hash code for the product. 
			return hashX ^ hashY;
		}

		public int[] ToArray()
		{
			return new int[2] {x, y};
		}

		/**
		 * \brief Compares edges and takes shared triangles into account.
		 * @param a First edge to compare.
		 * @param b Second edge to compare against.
		 * @param sharedIndices A qe_IntArray[] containing int[] of triangles that share a vertex.
		 * \returns True or false if edge a is equal to b.
		 */
		public bool Equals(qe_Edge rhs, Dictionary<int, int> triangleLookup)
		{
			int a = triangleLookup[this.x];
			int b = triangleLookup[this.y];

			int c = triangleLookup[rhs.y];
			int d = triangleLookup[rhs.y];

			return (a == c && b == d) || (a == d && b == c);
		}

		public bool Contains(int a)
		{
			return (x == a || y == a);
		} 

		public bool Contains(int a, Dictionary<int, int> triangleLookup)
		{
			return triangleLookup[a] == triangleLookup[x] || triangleLookup[a] == triangleLookup[y];
		}
	}

	public static class qe_Edge_Ext
	{
		public static IEnumerable<qe_Edge> ToSharedIndex(this IEnumerable<qe_Edge> arr, Dictionary<int, int> lookup)
		{
			List<qe_Edge> vals = new List<qe_Edge>();

			foreach(qe_Edge edge in arr)
			{
				vals.Add( new qe_Edge(
					lookup[edge.x], 
					lookup[edge.y] ));
			}

			return vals;
		}

		public static IEnumerable<qe_Edge> ToTriangleIndex(this IEnumerable<qe_Edge> arr, List<List<int>> shared)
		{
			List<qe_Edge> keys = new List<qe_Edge>();

			foreach(qe_Edge edge in arr)
			{
				keys.Add(new qe_Edge(shared[edge.x][0], shared[edge.y][0]));
			}

			return keys;
		}

		public static IList<int> ToIndices(this IList<qe_Edge> arr)
		{
			int[] ind = new int[arr.Count * 2];
			int n = 0;

			for(int i = 0; i < arr.Count; i++)
			{
				ind[n++] = arr[i].x;
				ind[n++] = arr[i].y;
			}

			return ind;
		}
	}
}