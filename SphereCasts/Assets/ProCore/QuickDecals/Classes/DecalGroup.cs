using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ProCore.Decals
{
	[System.Serializable]
	public class DecalGroup
	{
		public const int MAX_ATLAS_SIZE_DEFAULT = 4096;
		public const int ATLAS_PADDING_DEFAULT = 4;

		public List<ProCore.Decals.Decal> decals;

		public string name;
		public Shader shader;
		public bool isPacked;
		public Material material;
		public int maxAtlasSize;
		public int padding;
		
		public DecalGroup(
			string name,
			List<Decal> decals,
			bool isPacked,
			Shader shader,
			Material material,
			int maxAtlasSize,
			int padding)
		{
			this.name = name;
			this.decals = decals;
			this.shader = shader;
			this.isPacked = isPacked;
			this.material = material;
			this.maxAtlasSize = maxAtlasSize;
			this.padding = padding;
		}

		public bool ContainsTexture(Texture2D tex)
		{
			foreach(Decal decal in decals)	
				if(decal.texture == tex)
					return true;
			return false;
		}
	}
}
