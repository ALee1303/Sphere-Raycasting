using UnityEngine;
using System.Collections;

namespace ProCore.Decals {
public class qd_Mesh
{

#region Const
	static int[] BILLBOARD_TRIANGLES = new int[6] { 0, 1, 2, 1, 3, 2 };
	static Vector3[] BILLBOARD_VERTICES = new Vector3[4]
	{
		new Vector3(-.5f, -.5f, 0f),
		new Vector3( .5f, -.5f, 0f),
		new Vector3(-.5f, .5f, 0f),
		new Vector3( .5f, .5f, 0f),
	};
	static Vector3[] BILLBOARD_NORMALS = new Vector3[4] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
	static Vector4[] BILLBOARD_TANGENTS = new Vector4[4] { Vector3.right, Vector3.right, Vector3.right, Vector3.right };
	static Vector2[] BILLBOARD_UV2 = new Vector2[4] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };

#endregion

	public static GameObject CreateDecal(Material mat, Rect uvCoords, float scale)
	{
		GameObject decal = new GameObject();
		decal.name = "Decal" + decal.GetInstanceID();
		
		decal.AddComponent<MeshFilter>().sharedMesh = DecalMesh("DecalMesh" + decal.GetInstanceID(), mat, uvCoords, scale);
		decal.AddComponent<MeshRenderer>().sharedMaterial = mat;

		#if UNITY_5
		decal.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		#else
		decal.GetComponent<MeshRenderer>().castShadows = false;
		#endif

		qd_Decal decalComponent = decal.AddComponent<qd_Decal>();

		decalComponent.SetScale(scale);
		decalComponent.SetTexture( (Texture2D)mat.mainTexture );
		decalComponent.SetUVRect(uvCoords);

		return decal;
	}

	public static Mesh DecalMesh(string name, Material mat, Rect uvCoords, float scale)
	{
		float w = uvCoords.width, h = uvCoords.height;
		
		if(mat != null && mat.mainTexture != null)
		{
			if(mat.mainTexture.width > mat.mainTexture.height)	
				h *= mat.mainTexture.height/(float)mat.mainTexture.width;
			else
				w *= mat.mainTexture.width/(float)mat.mainTexture.height;
		}
		
		Vector3 aspectScale = w > h ? new Vector3(1f, h/w, 1f) : new Vector3(w/h, 1f, 1f);

		Mesh m = new Mesh();
		Vector3[] v = new Vector3[4];
		for(int i = 0; i < 4; i++)
			v[i] = Vector3.Scale(BILLBOARD_VERTICES[i], aspectScale) * scale;

		Vector2[] uvs = new Vector2[4]
		{
			new Vector2(uvCoords.x + uvCoords.width, uvCoords.y),
			new Vector2(uvCoords.x, uvCoords.y),
			new Vector2(uvCoords.x + uvCoords.width, uvCoords.y + uvCoords.height),
			new Vector2(uvCoords.x, uvCoords.y + uvCoords.height)
		};

		m.vertices 	= v;
		m.triangles = BILLBOARD_TRIANGLES;
		m.normals 	= BILLBOARD_NORMALS;
		m.tangents 	= BILLBOARD_TANGENTS;
		m.uv 		= uvs;
		m.uv2 		= BILLBOARD_UV2;
		m.name		= name;

		return m;
	}
}
}