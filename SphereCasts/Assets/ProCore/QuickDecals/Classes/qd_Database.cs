#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using ProCore.Decals;
using System.IO;
using QD = ProCore.Decals;

[System.Serializable]
public class qd_Database : ScriptableObject
{
	public static Shader DefaultShader { get { return Shader.Find("Transparent/Diffuse"); } }
	const string DECALSHEETS_PATH = "Assets/ProCore/QuickDecals/DecalSheets/";

#region Private Members

	// Keep an internal store of the current state in DecalGroup form so that we don't have to rebuild
	// from GUIDs every time when calling from qd_Editor
	public List<DecalGroup> decalGroups = new List<DecalGroup>();

	// For serialization purposes, save decalGroups as string[]
	[HideInInspector] [SerializeField] private string[] 	s_decals;

	[HideInInspector] [SerializeField] private string[]		names 		= new string[0];
	[HideInInspector] [SerializeField] private bool[]		isPacked 	= new bool[0];
	[HideInInspector] [SerializeField] private string[]		materials 	= new string[0];
	[HideInInspector] [SerializeField] private string[]		shaders 	= new string[0];
	[HideInInspector] [SerializeField] private int[]		atlasSize 	= new int[0];
	[HideInInspector] [SerializeField] private int[]		padding 	= new int[0];

#endregion

#region Public Get

	public bool LoadDecalGroups(DecalView decalView)
	{
		decalGroups.Clear();

		if(s_decals == null)
			return false;

		Dictionary<int, List<QD.Decal>> dict = new Dictionary<int, List<QD.Decal>>();

		foreach(string str in s_decals)
		{
			QD.Decal d;
			if( QD.Decal.Deserialize(str, out d) )
			{
				int grpIndex = decalView == DecalView.Organizational ? d.orgGroup : d.atlasGroup;

				if( dict.ContainsKey(grpIndex) )
					dict[grpIndex].Add(d);
				else
					dict.Add(grpIndex, new List<QD.Decal>(){d});
			}
		}

		foreach(KeyValuePair<int, List<QD.Decal>> kvp in dict)
		{
			if(decalView == DecalView.Atlas)
			{
				if(materials != null && kvp.Key < materials.Length)
				{
					Material mat = (Material)AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath(materials[kvp.Key]), typeof(Material) );
					decalGroups.Add( new DecalGroup( names[kvp.Key], kvp.Value, isPacked[kvp.Key], Shader.Find(shaders[kvp.Key]), mat, atlasSize[kvp.Key], padding[kvp.Key]) );
				}
				else
				{
					decalGroups.Add( new DecalGroup( kvp.Value[0].name, kvp.Value, false, DefaultShader, null, 1024, 3) );
				}
			}
			else
			{
				decalGroups.Add( new DecalGroup( "", kvp.Value, false, (Shader)null, (Material)null, 0, 0) );
			}

		}

		// now sort the decals per index
		for(int i = 0; i < decalGroups.Count; i++)
			qdUtil.SortDecalsUsingView(ref decalGroups[i].decals, decalView);

		return true;
	}

	/**
	 * Get the Material associated with this decal.  Can be null if sheet isn't packed yet.
	 */
	public bool MaterialWithDecal(QD.Decal decal, out Material mat)
	{
		if(decal.isPacked && materials != null && decal.atlasGroup < materials.Length)
			mat = (Material)AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath(materials[decal.atlasGroup]), typeof(Material) );
		else
			mat = null;

		return mat != null;
	}

	/**
	 * Returns the Shader associated with this decal.  If no shader set, return the default.
	 */
	public Shader ShaderWithDecal(QD.Decal decal)
	{
		if(shaders != null && decal.atlasGroup < shaders.Length)
			return Shader.Find(shaders[decal.atlasGroup]);
		else
			return DefaultShader;
	}
#endregion

#region Public Set

	public void AddDecals(List<QD.Decal> dec, DecalView decalView)
	{
		decalGroups.Add( new DecalGroup(
			(dec.Count > 0 ? dec[0].texture.name : "New Decal Group"),
			dec,
			false,
			DefaultShader,
			(Material)null,
			DecalGroup.MAX_ATLAS_SIZE_DEFAULT,
			DecalGroup.ATLAS_PADDING_DEFAULT ));

		Save(decalView);
	}

	public void AddDecals(List<QD.Decal> dec, int grp, int index, DecalView view)
	{
		if(grp > -1 && grp < decalGroups.Count)
		{
			if(index > -1)
				decalGroups[grp].decals.InsertRange(index, dec);
			else
				decalGroups[grp].decals.AddRange(dec);

			decalGroups[grp].isPacked = false;
		}
		else
		{
			decalGroups.Add( new DecalGroup(
				(dec.Count > 0 ? dec[0].texture.name : "New Decal Group"),
				dec,
				false,
				DefaultShader,
				(Material)null,
				DecalGroup.MAX_ATLAS_SIZE_DEFAULT,
				DecalGroup.ATLAS_PADDING_DEFAULT ));
		}

		Save(view);
	}

	public void DeleteDecals(Dictionary<int, List<int>> del, DecalView decalView)
	{
		foreach(KeyValuePair<int, List<int>> kvp in del)
		{
			// there's probably some linq magic that does this in a cleaner manner, but i don't
			// know what it is
			List<QD.Decal> survivors = new List<QD.Decal>();

			for(int i = 0; i < decalGroups[kvp.Key].decals.Count; i++)
				if( !kvp.Value.Contains(i) )
					survivors.Add( decalGroups[kvp.Key].decals[i] );

			decalGroups[kvp.Key].decals = survivors;
			decalGroups[kvp.Key].isPacked = false;
		}

		Save(decalView);
	}

	public void PruneGroups()
	{
		decalGroups.RemoveAll(EmptyList);
	}

	private static bool EmptyList(DecalGroup grp)
	{
		return grp.decals.Count < 1;
	}

	public void Save(DecalView view)
	{
		List<string> ser 	= new List<string>();
		List<string> mat 	= new List<string>();
		List<string> sha 	= new List<string>();
		List<string> nam 	= new List<string>();
		List<bool> pack 	= new List<bool>();
		List<int> atlas 	= new List<int>();
		List<int> pad 		= new List<int>();

		for(int i = 0; i < decalGroups.Count; i++)
		{
			for(int n = 0; n < decalGroups[i].decals.Count; n++)
			{
				if(view == DecalView.Organizational)
				{
					decalGroups[i].decals[n].orgGroup = i;
					decalGroups[i].decals[n].orgIndex = n;
				}
				else
				{
					decalGroups[i].decals[n].atlasGroup = i;
					decalGroups[i].decals[n].atlasIndex = n;
				}

				ser.Add( decalGroups[i].decals[n].Serialize() );
			}

			if(view == DecalView.Atlas)
			{
				sha.Add(decalGroups[i].shader != null ? decalGroups[i].shader.name : "");
				pack.Add(decalGroups[i].isPacked);
				nam.Add(decalGroups[i].name);
				atlas.Add(decalGroups[i].maxAtlasSize);
				pad.Add(decalGroups[i].padding);
				mat.Add(decalGroups[i].material != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(decalGroups[i].material)) : "");
			}
		}

		s_decals = ser.ToArray();

		if(view == DecalView.Atlas)
		{
			materials 	= mat.ToArray();
			shaders 	= sha.ToArray();
			isPacked 	= pack.ToArray();
			names 		= nam.ToArray();
			padding 	= pad.ToArray();
			atlasSize 	= atlas.ToArray();
		}

		EditorUtility.SetDirty(this);
	}
#endregion

#region Texture Packing

	public bool PackTextures(int index)
	{
#if !UNITY_WEBPLAYER
		int decalCount = decalGroups[index].decals.Count;
		Texture2D[] imgs = new Texture2D[decalCount];

		for(int i = 0; i < decalCount; i++)
		{
			imgs[i] = decalGroups[index].decals[i].texture;
			string path = AssetDatabase.GetAssetPath(imgs[i]);
			TextureImporter textureImporter = (TextureImporter)TextureImporter.GetAtPath( path );

			if(!textureImporter.isReadable)
			{
				textureImporter.isReadable = true;
#if UNITY_4 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4
				textureImporter.textureFormat = TextureImporterFormat.ARGB32;
#else
				textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
#endif
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
			}
		}

		int maxAtlasSize = 8192;

		Texture2D packedTexture = new Texture2D(maxAtlasSize, maxAtlasSize, TextureFormat.ARGB32, true);
		packedTexture.name = decalGroups[index].name;
		Rect[] coordinates = packedTexture.PackTextures(imgs, decalGroups[index].padding, maxAtlasSize, false);

		if(coordinates == null) return false;

		for(int i = 0; i < decalCount; i++)
		{
			decalGroups[index].decals[i].atlasRect = coordinates[i];
			decalGroups[index].decals[i].isPacked = true;
		}

		decalGroups[index].isPacked = true;

		byte[] png = packedTexture.EncodeToPNG();

		if(!Directory.Exists(DECALSHEETS_PATH))
			Directory.CreateDirectory(DECALSHEETS_PATH);

		if(decalGroups[index].material == null)
		{
		 	string matPath = AssetDatabase.GenerateUniqueAssetPath(DECALSHEETS_PATH + decalGroups[index].name + ".mat");

			Material mat = new Material( decalGroups[index].shader );
			AssetDatabase.CreateAsset(mat, matPath);

			decalGroups[index].material = mat;
		}
		else
		{
			if(decalGroups[index].material.name != decalGroups[index].name)
			{
				string matPath = AssetDatabase.GetAssetPath(decalGroups[index].material);
				AssetDatabase.RenameAsset(matPath, decalGroups[index].name);
			}
		}

		string pngPath;

		if(decalGroups[index].material != null && decalGroups[index].material.mainTexture != null)
			pngPath = AssetDatabase.GetAssetPath(decalGroups[index].material.mainTexture);
		else
			pngPath = AssetDatabase.GenerateUniqueAssetPath(DECALSHEETS_PATH + decalGroups[index].name + ".png");

		// http://msdn.microsoft.com/en-us/library/system.io.path.getfilenamewithoutextension%28v=vs.110%29.aspx
		File.WriteAllBytes(pngPath, png);

		string curName = Path.GetFileNameWithoutExtension(pngPath);

		if(curName != decalGroups[index].name)
			AssetDatabase.RenameAsset(pngPath, decalGroups[index].name);

		AssetDatabase.Refresh();

		decalGroups[index].material.mainTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(pngPath, typeof(Texture2D));

		return true;
#else
		return false;
#endif
	}
#endregion
}
#endif
