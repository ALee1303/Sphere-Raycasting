using UnityEngine;
using System.Collections.Generic;

namespace QuickEdit
{
	[System.Serializable]
	public class qe_Triangle : System.IEquatable<qe_Triangle>
	{
		// Triangle indices.
		public int[] indices = new int[3];

		public int x { get { return indices[0]; } set { indices[0] = value; } }
		public int y { get { return indices[1]; } set { indices[1] = value; } }
		public int z { get { return indices[2]; } set { indices[2] = value; } }

		public qe_Triangle(int x, int y, int z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		/**
		 * Value-wise equals.
		 */
		public bool Equals(qe_Triangle tri)
		{
			return 	( x == tri.x && y == tri.y && z == tri.z ) ||
					( x == tri.x && y == tri.z && z == tri.y ) ||

					( x == tri.y && y == tri.z && z == tri.x ) ||
					( x == tri.y && y == tri.x && z == tri.z ) ||

					( x == tri.z && y == tri.y && z == tri.x ) ||
					( x == tri.z && y == tri.x && z == tri.y );

		}

		/**
		 * Value-wise equals.
		 */
		public bool Equals(int[] tri)
		{
			return 	( x == tri[0] && y == tri[1] && z == tri[2] ) ||
					( x == tri[0] && y == tri[2] && z == tri[1] ) ||

					( x == tri[1] && y == tri[2] && z == tri[0] ) ||
					( x == tri[1] && y == tri[0] && z == tri[2] ) ||

					( x == tri[2] && y == tri[1] && z == tri[0] ) ||
					( x == tri[2] && y == tri[0] && z == tri[1] );

		}

		/**
		 * Value-wise equals.
		 */
		public bool Equals(int a, int b, int c)
		{
			return 	( x == a && y == b && z == c ) ||
					( x == a && y == c && z == b ) ||

					( x == b && y == c && z == a ) ||
					( x == b && y == a && z == c ) ||

					( x == c && y == b && z == a ) ||
					( x == c && y == a && z == b );

		}

		public override bool Equals(System.Object b)
		{
			return b is qe_Triangle && this.Equals( (qe_Triangle)b );
		}

		private static int MIN(int lhs, int rhs)
		{
			return lhs < rhs ? lhs : rhs;
		}

		private static int MAX(int lhs, int rhs)
		{
			return lhs > rhs ? lhs : rhs;
		}

		public override int GetHashCode()
		{
			int min = MIN(MIN(x, y), z);
			int max = MAX(MAX(x, y), z);
			int mid = (x != min && x != max) ? x : (y != min && y != max) ? y : z;

			// Calculate the hash code for the product. 
			return min ^ mid ^ max;
		}

		public override string ToString()
		{
			return string.Format("[{0}, {1}, {2}]", x, y, z);
		}

		public qe_Edge[] GetEdges()
		{
			return new qe_Edge[3] {
					new qe_Edge(x, y),
					new qe_Edge(y, z),
					new qe_Edge(z, x)
				};
		}

		public int[] GetIndices()
		{
			return indices;
		}
	}

	internal static class qe_TriangleExt
	{
		public static int IndexOf(this IList<qe_Triangle> list, int a, int b, int c)
		{
			for(int i = 0; i < list.Count; i++)
			{
				if( list[i].Equals(a, b, c) )
					return i;
			}
			return -1;
		}
	}
}