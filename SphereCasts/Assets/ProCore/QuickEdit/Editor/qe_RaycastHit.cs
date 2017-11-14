using UnityEngine;

namespace QuickEdit
{
	public struct qe_RaycastHit
	{
		public float Distance;
		public Vector3 Point;
		public Vector3 Normal;
		public int FaceIndex;

		public qe_RaycastHit(float InDistance, Vector3 InPoint, Vector3 InNormal, int InFaceIndex)
		{
			this.Distance = InDistance;
			this.Point = InPoint;
			this.Normal = InNormal;
			this.FaceIndex = InFaceIndex;
		}

		public override string ToString()
		{
			return string.Format("Point: {0}  Triangle: {1}", Point, FaceIndex);
		}
	}
}