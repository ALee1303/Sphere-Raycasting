using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestInterfaceImplement : MonoBehaviour,IInteractable
{
    public void Interact()
    {
        Debug.Log(this.name + "IInteractable Implemented!");
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
