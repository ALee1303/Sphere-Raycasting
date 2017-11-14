using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#pragma warning disable 0414

public class qb_Group : MonoBehaviour
{
	public string				groupName;
	
	private bool				visible;
	private bool 				frozen;
	//private List<GameObject> 	children = new List<GameObject>();
	
	public void AddObject(GameObject newObject)
	{
		newObject.transform.parent = this.transform;
		//children.Add(newObject);
	}
	
	public void Hide()
	{
		visible = false;
	}
	
	public void Show()
	{
		visible = true;
	}
	
	public void Freeze()
	{
		frozen = true;
	}
	
	public void UnFreeze()
	{
		frozen = false;
	}
	
	public void CleanUp()
	{
		GameObject.DestroyImmediate(this.gameObject);
	}	
}