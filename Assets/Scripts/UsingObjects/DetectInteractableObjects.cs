using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectInteractableObjects : MonoBehaviour
{
    [Tooltip("Determines the width of the Capsule cast used to detect object, suggested float = 0.125f")]
    [SerializeField]
    private float CapsuleCastRadius;
    [Tooltip("Distance of the CapsuleCast, float recommendation of 2.0f")]
    [SerializeField]
    private float CapsuleCastDistance;
    [Tooltip("Objects auto assigned by capsuleCastAll")]
    [SerializeField]
    private GameObject[] HitObjects;
    [Tooltip("Auto Assigned by RayCast")]
    [SerializeField]
    private GameObject TestGo;
    private GameObject closest;
    private RaycastHit[] Hits;
    private RaycastHit hit;
    private bool isBeingBlocked;

    public GameObject ClosestInteractableObject
    {
        get { return closest; }
        
    }


    private void Awake()
    {
        isBeingBlocked = true;
    }
    // Update is called once per frame
    void Update()
    {
        CastingForInteractableGameObjects();
        if (isBeingBlocked == false)
        {
            HighlightSelectedObject();
        }
        
       
        if (HitObjects.Length > 0)
        {
            Array.Clear(Hits, 0, Hits.Length);
            Array.Clear(HitObjects, 0, HitObjects.Length);
            isBeingBlocked = true;
            
        }
        Debug.DrawRay(transform.position, transform.forward, Color.magenta);

    }

    /// <summary> CastingForInteractableGameObjects():
    /// 
    /// </summary>
    private void CastingForInteractableGameObjects()
    {
        Hits = Physics.CapsuleCastAll(this.transform.position, this.transform.forward,
            CapsuleCastRadius, this.transform.forward, CapsuleCastDistance);

       

        InitializeInteractableObjects();
    }

    /// <summary> InitializeInteractableObjects():
    /// 
    /// </summary>
    private void InitializeInteractableObjects()
    {
        int arrayelement = 0;
        HitObjects = new GameObject[Hits.Length];
        if (Hits.Length > 0)
        {
            for (int i = 0; i < Hits.Length; i++)
            {
                if (Hits[i].rigidbody != null && Hits[i].rigidbody.gameObject.tag!="Untagged")
                {
                    if (Hits[i].rigidbody.gameObject.GetComponent<IInteractable>() != null && Hits[i].rigidbody.gameObject.tag != "Player")
                    {
                        HitObjects[arrayelement] = Hits[i].rigidbody.gameObject;
                        arrayelement++;
                    }
                }
            }
            if (HitObjects[0] != null)
            {
                DetermineClosetsObject();

            }
        }
    }

    /// <summary> DetermineClosetsObject():
    /// 
    /// </summary>
    private void DetermineClosetsObject()
    {
        float smallestAngle = 1000;
        closest = HitObjects[0];
        List<float> Angles = new List<float>();
        SortedList<int, GameObject> ObjectsToTest = new SortedList<int, GameObject>();
        if (HitObjects.Length > 1)
        {
            for (int i = 0; i < HitObjects.Length; i++)
            {
                if (HitObjects[i]==null)
                {
                    HitObjects[i] = HitObjects[0];
                }
            }

            for (int i = 0; i < HitObjects.Length; i++)
            {
                
                    Vector3 TargetDir = HitObjects[i].transform.position - this.transform.position;
                    float angle = Vector3.Angle(TargetDir, this.transform.forward);
                if (HitObjects[i].GetComponent<IInteractable>()!=null)
                {
                    Angles.Add(angle);
                    ObjectsToTest.Add(i, HitObjects[i]);
                }
                    
                
            }
            
        }
        else
        {
            Vector3 TargetDir = HitObjects[0].transform.position - this.transform.position;
            float angle = Vector3.Angle(TargetDir, this.transform.forward);
            Angles.Add(angle);
        }
        for (int i = 1; i < HitObjects.Length; i++)
        {
            if (Angles[i] < smallestAngle)
            {
                smallestAngle = Angles[i];

                closest = ObjectsToTest[i];
            }
        }
        NotBeingBlockedTest();
    }

    /// <summary> NotBeingBlockedTest():
    /// 
    /// </summary>
    private void NotBeingBlockedTest()
    {
        if (Physics.Raycast(closest.transform.position, closest.transform.position- this.transform.position, out hit, CapsuleCastDistance * 2))
        {
            if (hit.rigidbody != null)
            {
                TestGo = hit.rigidbody.gameObject;
            }

        }
        
        if (TestGo == this.gameObject)
        {
            isBeingBlocked = false;
        }
        else 
        {
            closest = null;
            isBeingBlocked = true;
           // TestGo = null;
        }
    }

    /// <summary>
    /// This function will implement a minor graphic inorder to convey which object is currently being selected
    /// </summary>
    private void HighlightSelectedObject()
    {
        Debug.Log("Highlighted " + closest.name);
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position, CapsuleCastRadius);
        Gizmos.DrawSphere(transform.forward, CapsuleCastRadius);
    }
}

