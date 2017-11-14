using UnityEngine;
using System.Collections;
using ProCore.Decals;

// namespace ProCore.Decals // Cannot serialize MonoBehaviours in namespaces in Unity 3.5 ?
// {
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class qd_Decal : MonoBehaviour
{
	public Texture2D texture { get { return _texture; } }
	[HideInInspector] [SerializeField] private Texture2D _texture;
	[HideInInspector] [SerializeField] private Rect _rect;
	[HideInInspector] [SerializeField] private float _scale;

	void Awake()
	{
		Verify();
	}

	public void SetScale(float scale)
	{
		_scale = scale;
	}

	public void SetTexture(Texture2D tex)
	{
		_texture = tex;
	}

	public void SetUVRect(Rect r)
	{
		_rect = r;

		Vector2[] uvs = new Vector2[4]
		{
			new Vector2(_rect.x + _rect.width, _rect.y),
			new Vector2(_rect.x, _rect.y),
			new Vector2(_rect.x + _rect.width, _rect.y + _rect.height),
			new Vector2(_rect.x, _rect.y + _rect.height)
		};

		GetComponent<MeshFilter>().sharedMesh.uv = uvs;
	}

	/**
	 * Freeze the scale transform.  Useful 'cause non-Vector3.one scales break dynamic batching.
	 */
	public void FreezeTransform()
	{
		Vector3 scale = transform.localScale;
		// _scale *= scale.x;

		Mesh m = transform.GetComponent<MeshFilter>().sharedMesh;
		Vector3[] v = m.vertices;
		for(int i = 0; i < v.Length; i++)
			v[i] = Vector3.Scale(v[i], scale);
		m.vertices = v;
		transform.localScale = Vector3.one;
	}

	public void Verify()
	{
		Material mat = GetComponent<MeshRenderer>().sharedMaterial;

		if(mat == null)
		{
			GameObject[] existingDecals = qdUtil.FindDecalsWithTexture(_texture);
			existingDecals = System.Array.FindAll(existingDecals, x => x.GetComponent<MeshRenderer>().sharedMaterial != null);

			if(existingDecals == null || existingDecals.Length < 1)
			{
				mat = new Material( Shader.Find("Transparent/Diffuse") );
				mat.mainTexture = _texture;
			}
			else
			{
				mat = existingDecals[0].GetComponent<MeshRenderer>().sharedMaterial;
			}	
			
			GetComponent<MeshRenderer>().sharedMaterial = mat;
		}

		if( GetComponent<MeshFilter>().sharedMesh == null )
		{
			GetComponent<MeshFilter>().sharedMesh = qd_Mesh.DecalMesh("DecalMesh" + GetInstanceID(), mat, _rect, _scale);
		}
	}
	
// #if PB_DEBUG

// 	void OnDrawGizmos()
// 	{
// 		Mesh m = GetComponent<MeshFilter>().sharedMesh;

// 		Vector3 n = transform.TransformDirection(m.normals[0]);
// 		Gizmos.color = new Color(n.x, n.y, n.z, 1f);

// 		for(int i = 0; i < m.normals.Length; i++)
// 		{
// 			Gizmos.DrawLine( 	transform.TransformPoint(m.vertices[i]),
// 								transform.TransformPoint(m.vertices[i]) + transform.TransformDirection(m.normals[i]) * .2f);
// 		}
// 	}
// #endif
}
// }