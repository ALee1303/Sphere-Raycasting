using UnityEngine;
using System.Collections;

public class qb_RaycastResult
{
	public qb_RaycastResult(bool success,RaycastHit hit)
	{
		this.success = success;
		this.hit = hit;
	}
		
	public bool success = false;
	public RaycastHit hit;
}

