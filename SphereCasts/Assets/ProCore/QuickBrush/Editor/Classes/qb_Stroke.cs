using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//The stroke is laid down as a succession of points, 
//the position and rotation of which is an impression of the handle position and rotation at the moment a new point is created
//The handle calculation takes care of all point to point directional estimation
//how the stroke is interpreted is determined later based on the settings
public class qb_Stroke
{
	// A list of positions in order of placement
	private List<qb_Point> points;
	private qb_Point curPoint;
	
	public qb_Stroke()
	{
		points = new List<qb_Point>();
	}
	
	public qb_Point AddPoint(Vector3 position, Vector3 upVector, Vector3 dirVector)
	{
		qb_Point newPoint = new qb_Point(position, upVector, dirVector);
		
		points.Add(newPoint);
		curPoint = newPoint;
		
		return curPoint;
	}
	
	public List<qb_Point> GetPoints()
	{
		return points;
	}
	
	public qb_Point GetCurPoint()
	{
		return curPoint;
	}

}
