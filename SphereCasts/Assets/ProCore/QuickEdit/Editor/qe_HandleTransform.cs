using UnityEngine;

namespace QuickEdit
{
	[System.Serializable]
	internal struct HandleTransform : System.IEquatable<HandleTransform>
	{
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 scale;

		bool Approx(Vector3 lhs, Vector3 rhs)
		{
			return 	Mathf.Abs(lhs.x - rhs.x) < Mathf.Epsilon &&
					Mathf.Abs(lhs.y - rhs.y) < Mathf.Epsilon &&
					Mathf.Abs(lhs.z - rhs.z) < Mathf.Epsilon;
		}

		bool Approx(Quaternion lhs, Quaternion rhs)
		{
			return 	Mathf.Abs(lhs.x - rhs.x) < Mathf.Epsilon &&
					Mathf.Abs(lhs.y - rhs.y) < Mathf.Epsilon &&
					Mathf.Abs(lhs.z - rhs.z) < Mathf.Epsilon &&
					Mathf.Abs(lhs.w - rhs.w) < Mathf.Epsilon;
		}

		public bool Equals(HandleTransform rhs)
		{
			return 	Approx(this.position, rhs.position) &&
					Approx(this.rotation, rhs.rotation) &&
					Approx(this.scale, rhs.scale);
		}

		public override bool Equals(System.Object rhs)
		{
			return rhs is HandleTransform && this.Equals( (HandleTransform) rhs );
		}

		public override int GetHashCode()
		{
			return position.GetHashCode() ^ rotation.GetHashCode() ^ scale.GetHashCode();
		}

		public Matrix4x4 GetMatrix()
		{
			return Matrix4x4.TRS(position, rotation, scale);
		}

		public static HandleTransform operator - (HandleTransform lhs, HandleTransform rhs)
		{
			HandleTransform t = new HandleTransform();

			t.position = lhs.position - rhs.position;
			t.rotation = Quaternion.Inverse(rhs.rotation) * lhs.rotation;
			t.scale = (lhs.scale - rhs.scale) + Vector3.one;

			return t;
		}

		public static bool operator == (HandleTransform lhs, HandleTransform rhs)
		{
			return System.Object.ReferenceEquals(lhs, rhs) || lhs.Equals(rhs);
		}

		public static bool operator != (HandleTransform lhs, HandleTransform rhs)
		{
			return !(lhs == rhs);
		}

		public override string ToString()
		{
			return position.ToString("F2") + "\n" + rotation.ToString("F2") + "\n" + scale.ToString("F2");
		}
	}
}