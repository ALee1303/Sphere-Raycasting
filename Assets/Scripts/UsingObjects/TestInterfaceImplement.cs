using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestInterfaceImplement : MonoBehaviour,IInteractable
{
    public void Interact()
    {
        //while (DetectInteractableObjectComparative.allName.Count != 0)
        //    Debug.Log(DetectInteractableObjectComparative.allName.Pop().ToString() + ":" + DetectInteractableObjectComparative.allAngle.Pop().ToString());
        Debug.Log(this.name + "IInteractable Implemented!");
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
