using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace QuickEdit
{

	/**
	 * Geometry math and Array extensions.
	 */
	public static class qe_Math
	{
#region Geometry

		/**
		 * Returns true if a raycast intersects a triangle.
		 * http://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm
		 * http://www.cs.virginia.edu/~gfx/Courses/2003/ImageSynthesis/papers/Acceleration/Fast%20MinimumStorage%20RayTriangle%20Intersection.pdf
		 */
		public static bool RayIntersectsTriangle(Ray InRay, Vector3 InTriangleA,  Vector3 InTriangleB,  Vector3 InTriangleC, out float OutDistance, out Vector3 OutPoint)
		{
			OutDistance = 0f;
			OutPoint = Vector3.zero;
			
			Vector3 e1, e2;  //Edge1, Edge2
			Vector3 P, Q, T;
			float det, inv_det, u, v;
			float t;

			//Find vectors for two edges sharing V1
			e1 = InTriangleB - InTriangleA;
			e2 = InTriangleC - InTriangleA;

			//Begin calculating determinant - also used to calculate `u` parameter
			P = Vector3.Cross(InRay.direction, e2);
			
			//if determinant is near zero, ray lies in plane of triangle
			det = Vector3.Dot(e1, P);

			// Non-culling branch
			// {
				if(det > -Mathf.Epsilon && det < Mathf.Epsilon)
					return false;

				inv_det = 1f / det;

				//calculate distance from V1 to ray origin
				T = InRay.origin - InTriangleA;

				// Calculate u parameter and test bound
				u = Vector3.Dot(T, P) * inv_det;

				//The intersection lies outside of the triangle
				if(u < 0f || u > 1f)
					return false;

				//Prepare to test v parameter
				Q = Vector3.Cross(T, e1);

				//Calculate V parameter and test bound
				v = Vector3.Dot(InRay.direction, Q) * inv_det;

				//The intersection lies outside of the triangle
				if(v < 0f || u + v  > 1f)
					return false;

				t = Vector3.Dot(e2, Q) * inv_det;
			// }

			if(t > Mathf.Epsilon)
			{ 
				//ray intersection
				OutDistance = t;

				OutPoint.x = (u * InTriangleB.x + v * InTriangleC.x + (1-(u+v)) * InTriangleA.x);
				OutPoint.y = (u * InTriangleB.y + v * InTriangleC.y + (1-(u+v)) * InTriangleA.y);
				OutPoint.z = (u * InTriangleB.z + v * InTriangleC.z + (1-(u+v)) * InTriangleA.z);

				return true;
			}

			return false;
		}
#endregion

#region Normal and Tangents

		/**
		 * Calculate the unit vector normal of 3 points:  B-A x C-A
		 */
		public static Vector3 Normal(Vector3 p0, Vector3 p1, Vector3 p2)
		{
			Vector3 cross = Vector3.Cross(p1 - p0, p2 - p0);
			if (cross.magnitude < Mathf.Epsilon)
				return new Vector3(0f, 0f, 0f); // bad triangle
			else
			{
				return cross.normalized;
			}
		}

		public static Vector3 Normal(this qe_Mesh mesh, qe_Triangle tri)
		{
			return Normal(mesh.vertices[tri.x], mesh.vertices[tri.y], mesh.vertices[tri.z]);
		}
		
		/**
		 * If p.Length % 3 == 0, finds the normal of each triangle in a face and returns the average.
		 * Otherwise return the normal of the first three points.
		 */
		public static Vector3 Normal(Vector3[] p)
		{
			if(p.Length % 3 == 0)
			{
				Vector3 nrm = Vector3.zero;

				for(int i = 0; i < p.Length; i+=3)
					nrm += Normal(	p[i+0], 
									p[i+1], 
									p[i+2]);

				return nrm / (p.Length/3f);
			}
			else
			{
				Vector3 cross = Vector3.Cross(p[1] - p[0], p[2] - p[0]);
				if (cross.magnitude < Mathf.Epsilon)
					return new Vector3(0f, 0f, 0f); // bad triangle
				else
				{
					return cross.normalized;
				}
			}
		}

        /**
		 * Returns the first normal, tangent, and bitangent for this face, using the first triangle available for tangent and bitangent.
		 * Does not rely on mesh.msh for normal or uv information - uses mesh.vertices & mesh.uv.
		 */
		public static void NormalTangentBitangent(Vector3[] vertices, Vector2[] uv, qe_Triangle tri, out Vector3 normal, out Vector3 tangent, out Vector3 bitangent)
		{
			normal = qe_Math.Normal(vertices[tri.x], vertices[tri.y], vertices[tri.z]);

			Vector3 tan1 = Vector3.zero;
			Vector3 tan2 = Vector3.zero;
			Vector4 tan = new Vector4(0f,0f,0f,1f);

			long i1 = tri.x;
			long i2 = tri.y;
			long i3 = tri.z;

			Vector3 v1 = vertices[i1];
			Vector3 v2 = vertices[i2];
			Vector3 v3 = vertices[i3];

			Vector2 w1 = uv[i1];
			Vector2 w2 = uv[i2];
			Vector2 w3 = uv[i3];

			float x1 = v2.x - v1.x;
			float x2 = v3.x - v1.x;
			float y1 = v2.y - v1.y;
			float y2 = v3.y - v1.y;
			float z1 = v2.z - v1.z;
			float z2 = v3.z - v1.z;

			float s1 = w2.x - w1.x;
			float s2 = w3.x - w1.x;
			float t1 = w2.y - w1.y;
			float t2 = w3.y - w1.y;

			float r = 1.0f / (s1 * t2 - s2 * t1);

			Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
			Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

			tan1 += sdir;
			tan2 += tdir;

			Vector3 n = normal;

			Vector3.OrthoNormalize(ref n, ref tan1);

			tan.x = tan1.x;
			tan.y = tan1.y;
			tan.z = tan1.z;

			tan.w = (Vector3.Dot(Vector3.Cross(n, tan1), tan2) < 0.0f) ? -1.0f : 1.0f;

			tangent = ((Vector3)tan) * tan.w;
			bitangent = Vector3.Cross(normal, tangent);
		}
#endregion

#region Compare (Max, Min, Average, etc)

		public static T Max<T>(T[] array) where T : System.IComparable<T>
		{
			if(array == null || array.Length < 1)
				return default(T);

			T max = array[0];
			for(int i = 1; i < array.Length; i++)
				if(array[i].CompareTo(max) >= 0)
					max = array[i];
			return max;
		}

		public static T Min<T>(T[] array) where T : System.IComparable<T>
		{
			if(array == null || array.Length < 1)
				return default(T);

			T min = array[0];
			for(int i = 1; i < array.Length; i++)
				if(array[i].CompareTo(min) < 0)
					min = array[i];
			return min;
		}

		/**
		 * Return the largest axis in a Vector3.
		 */
		public static float LargestValue(Vector3 v)
		{
			if(v.x > v.y && v.x > v.z) return v.x;
			if(v.y > v.x && v.y > v.z) return v.y;
			return v.z;
		}
		
		/**
		 * Return the largest axis in a Vector2.
		 */
		public static float LargestValue(Vector2 v)
		{
			return (v.x > v.y) ? v.x :v.y;
		}

		/**
		 * The smallest X and Y value found in an array of Vector2.  May or may not belong to the same Vector2.
		 */
		public static Vector2 SmallestVector2(Vector2[] v)
		{
			Vector2 s = v[0];
			for(int i = 1; i < v.Length; i++)
			{
				if(v[i].x < s.x)
					s.x = v[i].x;
				if(v[i].y < s.y)
					s.y = v[i].y;
			}
			return s;
		}

		/**
		 * The largest X and Y value in an array.  May or may not belong to the same Vector2.
		 */
		public static Vector2 LargestVector2(Vector2[] v)
		{
			Vector2 l = v[0];
			for(int i = 0; i < v.Length; i++)
			{
				if(v[i].x > l.x)
					l.x = v[i].x;
				if(v[i].y > l.y)
					l.y = v[i].y;
			}
			return l;
		}

		/**
		 * Creates an AABB with vertices and returns the Center point.
		 */
		public static Vector3 BoundsCenter(Vector3[] verts)
		{
			if( verts.Length < 1 ) return Vector3.zero;

			Vector3 min = verts[0];
			Vector3 max = min;

			for(int i = 1; i < verts.Length; i++)
			{
				min.x = Mathf.Min(verts[i].x, min.x);
				max.x = Mathf.Max(verts[i].x, max.x);

				min.y = Mathf.Min(verts[i].y, min.y);
				max.y = Mathf.Max(verts[i].y, max.y);

				min.z = Mathf.Min(verts[i].z, min.z);
				max.z = Mathf.Max(verts[i].z, max.z);
			}

			return (min+max) * .5f;
		}

		public static Rect ClampRect(Rect rect, Rect bounds)
		{
			if(rect.x+rect.width > bounds.x+bounds.width)
				rect.x = (bounds.x + bounds.width) - rect.width;
			else if(rect.x < bounds.x)
				rect.x = bounds.x;

			if(rect.y + rect.height > bounds.y + bounds.height)
				rect.y = (bounds.y + bounds.height) - rect.height;
			else if(rect.y < bounds.y)
				rect.y = bounds.y;

			return rect;
		}

		/**
		 *	\brief Gets the center point of the supplied Vector3[] array.
		 *	\returns Average Vector3 of passed vertex array.
		 */
		public static Vector3 Average(List<Vector3> v)
		{
			Vector3 sum = Vector3.zero;
			for(int i = 0; i < v.Count; i++)
				sum += v[i];
			return sum/(float)v.Count;
		}

		public static Vector3 Average(Vector3[] v)
		{
			Vector3 sum = Vector3.zero;
			for(int i = 0; i < v.Length; i++)
				sum += v[i];
			return sum/(float)v.Length;
		}

		public static Vector2 Average(List<Vector2> v)
		{
			Vector2 sum = Vector2.zero;
			for(int i = 0; i < v.Count; i++)
				sum += v[i];
			return sum/(float)v.Count;
		}

		public static Vector2 Average(Vector2[] v)
		{
			Vector2 sum = Vector2.zero;
			for(int i = 0; i < v.Length; i++)
				sum += v[i];
			return sum/(float)v.Length;
		}

		public static Vector4 Average(List<Vector4> v)
		{
			Vector4 sum = Vector4.zero;
			for(int i = 0; i < v.Count; i++)
				sum += v[i];
			return sum/(float)v.Count;
		}

		public static Vector4 Average(Vector4[] v)
		{
			Vector4 sum = Vector4.zero;
			for(int i = 0; i < v.Length; i++)
				sum += v[i];
			return sum/(float)v.Length;
		}

		public static Color Average(Color[] Array)
		{
			Color sum = Array[0];

			for(int i = 1; i < Array.Length; i++)
				sum += Array[i];

			return sum / (float)Array.Length;
		}

		/**
		 *	\brief Compares 2 vector3 objects, allowing for a margin of error.
		 */
		public static bool Approx(this Vector3 v, Vector3 b, float delta)
		{
			return 
				Mathf.Abs(v.x - b.x) < delta &&
				Mathf.Abs(v.y - b.y) < delta &&
				Mathf.Abs(v.z - b.z) < delta;
		}

		/**
		 *	\brief Compares 2 color objects, allowing for a margin of error.
		 */
		public static bool Approx(this Color a, Color b, float delta)
		{
			return 	Mathf.Abs(a.r - b.r) < delta &&
					Mathf.Abs(a.g - b.g) < delta &&
					Mathf.Abs(a.b - b.b) < delta &&
					Mathf.Abs(a.a - b.a) < delta;
		}
#endregion

	}
}
