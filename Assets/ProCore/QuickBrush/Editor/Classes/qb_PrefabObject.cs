using UnityEngine;
using System;
using System.Collections;

[Serializable]
public class qb_PrefabObject
{
	public qb_PrefabObject(UnityEngine.Object prefab)
	{
		this.prefab = prefab;
	}
	
	public qb_PrefabObject(UnityEngine.Object prefab, float weight)
	{
		this.prefab = prefab;
		this.weight = weight;
	}
	
	public UnityEngine.Object	prefab;
	public Texture2D			PreviewTexture
	{
		get
		{
			if(previewTexture == null)
			{
				if(prefab == null)
					return null;
					
				TryGetAssetPreview();
			}
			
			return previewTexture;	
		}
	}
	
	public Texture2D 			previewTexture;
	public float				weight = 0.1f;
	
	public void TryGetAssetPreview()
	{
		previewTexture = UnityEditor.AssetPreview.GetAssetPreview(prefab);
	}
}
