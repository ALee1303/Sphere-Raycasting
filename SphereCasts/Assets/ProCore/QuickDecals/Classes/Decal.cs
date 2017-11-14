using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

/**
 * Container Class for a single Decal
 */
namespace ProCore.Decals
{
[System.Serializable]
public class Decal
{
	// Cost
	static Vector3 DefaultRotation = new Vector3(-45f, 45f, 0f);
	static Vector3 DefaultScale = new Vector3(.8f, 1.2f, 1f);

	public string name;					///< User friendly name.
	public string id;					///< The asset GUID
	public bool isPacked;				///< Is this decal atlased, and is that atlas up to date?
	public string materialId;			///< The atlas material id (can be null)
	public Vector3 rotation;			///< Min, Max, Default - @todo Use a dedicated type.
	public Vector3 scale;				///< Min, Max, Default - @todo Use a dedicated type.
	public Rect atlasRect;				///< Where in the atlas is this texture ( {0,0,0,0} if not packed)
	public int orgGroup;				///< Organizational Group
	public int orgIndex;				///< Organizational Group index (used when rendering groups)
	public int atlasGroup;				///< Which atlas group this belongs to
	public int atlasIndex;				///< Index in the atlas group
	public Placement rotationPlacement; ///< Does this decal use a randomized rotation when placed in scene?
	public Placement scalePlacement; 	///< Does this decal use a randomized scale when placed in scene?

	public Texture2D texture;			///< Only used as an instance var.

	public Decal() {}

	public Decal(Texture2D img)//, int orgGroup, int atlasGroup, int orgIndex, int atlasIndex)
	{
		this.name = img.name;
		this.texture = img;
		#if UNITY_EDITOR
		this.id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(img));
		#endif
		this.materialId = "";
		this.isPacked = false;
		this.rotation = new Vector3(-45f, 45f, 0f);
		this.scale = new Vector3(.8f, 1.2f, 1f);
		this.atlasRect = new Rect(0f, 0f, 0f, 0f);
	}

	public override string ToString()
	{
		return name + "(Org: " + orgIndex + " Atlas: " + atlasIndex + " Packed: " + isPacked + ")";
	}

	public static bool Deserialize(string txt, out Decal decal)
	{
		decal = new Decal();

		string[] split = txt.Replace("{", "").Replace("}", "").Trim().Split('\n');
		if(split.Length < 11) return false;
		
		decal.name 		= split[0];
		decal.id 		= split[1];

		#if UNITY_EDITOR
		decal.texture = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(decal.id), typeof(Texture2D));
		#endif
		
		decal.rotation = DefaultRotation;
		if(!Vec3WithString(split[2], ref decal.rotation))
			Debug.LogWarning("Failed parsing default rotation values.  Using defaults.");
		
		decal.scale = DefaultScale;
		if(!Vec3WithString(split[3], ref decal.scale))
			Debug.LogWarning("Failed parsing default scale values.  Using defaults.");
		
		Vector4 v4 = Vector4.one;	// my laziness knows no bounds
		if(!Vec4WithString(split[4], ref v4))
			Debug.LogWarning("Failed parsing atlas rect.  Using default.");
		decal.atlasRect = Vec4ToRect(v4);

		decal.orgGroup = 0;
		if(!int.TryParse(split[5], out decal.orgGroup))
			Debug.LogWarning("Failed parsing organizational group.  Setting to group 0");

		decal.atlasGroup = 0;
		if(!int.TryParse(split[6], out decal.atlasGroup))
			Debug.LogWarning("Failed parsing atlas group.  Setting to group 0");

		decal.orgIndex = 0;
		if(!int.TryParse(split[7], out decal.orgIndex))
			Debug.LogWarning("Failed parsing organizational group.  Setting to group 0");

		decal.atlasIndex = 0;
		if(!int.TryParse(split[8], out decal.atlasIndex))
			Debug.LogWarning("Failed parsing atlas group.  Setting to group 0");

		int i;

		decal.rotationPlacement = Placement.Fixed;
		if(!int.TryParse(split[9], out i))
			Debug.LogWarning("Failed parsing rotationPlacement.  Setting to \"Fixed\"");
		else
			decal.rotationPlacement = (Placement)i;

		decal.scalePlacement = Placement.Fixed;
		if(!int.TryParse(split[10], out i))
			Debug.LogWarning("Failed parsing scalePlacement.  Setting to \"Fixed\"");
		else
			decal.scalePlacement = (Placement)i;

		bool packed = false;
		if(split.Length < 12 || !bool.TryParse(split[11], out packed))
		{
			packed = false;
			Debug.LogWarning("Failed parsing packed.  Setting to \"false\"");
		}
		else
		{
			decal.isPacked = packed;
		}

		return true;
	}

	public string Serialize()
	{
		// string str = "{\n" + 
		// "\tname : " + name.Replace(",", "\\,") + "\n" + 
		// "\tid: " + id + "\n" + 
		// "\trotation: " + rotation.ToString() + "\n" +
		// "\tscale: " +	scale.ToString() + "\n" +
		// "\trect: " +	"(" + atlasRect.xMin + ", " + atlasRect.yMin + ", " + atlasRect.width + ", " + atlasRect.height + ")" + "\n" + 
		// "\torg group: " +	orgGroup + "\n" + 
		// "\tatlas group: " + atlasGroup + "\n" +			
		// "\torg index: " +	orgIndex + "\n" +		
		// "\tatlas index: " + atlasIndex + "\n" +
		// "\trotation value: " + (int)rotationPlacement + "\n" +
		// "\tscale placement: " + (int)scalePlacement + "\n" +
		// "\tisPacked: " + isPacked +		
		// /*   */	"\n}";

		// Debug.Log(str);

		return "{\n" + 
		/* 0  */	name.Replace(",", "\\,") + "\n" + 
		/* 1  */	id + "\n" + 
		/* 2  */	rotation.ToString() + "\n" +
		/* 3  */	scale.ToString() + "\n" +
		/* 4  */	"(" + atlasRect.xMin + ", " + atlasRect.yMin + ", " + atlasRect.width + ", " + atlasRect.height + ")" + "\n" + 
		/* 5  */	orgGroup + "\n" + 
		/* 6  */	atlasGroup + "\n" +			
		/* 7  */	orgIndex + "\n" +		
		/* 8  */	atlasIndex + "\n" +
		/* 9  */	(int)rotationPlacement + "\n" +
		/* 10 */	(int)scalePlacement + "\n" +	
		/* 11 */	(bool)isPacked +		
		/*    */	"\n}";
	}

	private static bool Vec3WithString(string str, ref Vector3 vec3)
	{
		string[] split = str.Replace("(", "").Replace(")", "").Split(',');		
		float x, y, z;
		if(!float.TryParse(split[0], out x)) return false;
		if(!float.TryParse(split[1], out y)) return false;
		if(!float.TryParse(split[2], out z)) return false;
		vec3 = new Vector3(x, y, z);
		return true;
	}

	private static bool Vec4WithString(string str, ref Vector4 vec4)
	{
		string[] split = str.Replace("(", "").Replace(")", "").Split(',');
		float x, y, z, w;
		if(!float.TryParse(split[0], out x)) return false;
		if(!float.TryParse(split[1], out y)) return false;
		if(!float.TryParse(split[2], out z)) return false;
		if(!float.TryParse(split[3], out w)) return false;
		vec4 = new Vector4(x, y, z, w);
		return true;
	}

	/**
	 * Instead of writing a custom StringToRect just convert it to vec4 and back
	 */
	private static Rect Vec4ToRect(Vector4 v)
	{
		return new Rect(v.x, v.y, v.z, v.w);
	}
}
}