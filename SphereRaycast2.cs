using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detects best IInteractable object from the player's location and view.
/// Uses SphereCastAll and two angle variables to get the closest object to the player.
/// Cycles through all the Hits to find best candidate to interact by:
///     First priority: If an unblocked IInteractable is close enough to the center, use that one. Determined by 'angleFromCenter'.
///     Second priority: An unblocked IInteractable that is closer to the center than a previous one by more than 'comparativeAngle' .
///     Third priority: First unblocked IInteractable that was found.
/// Picked object must be inside the SphereCast and not be blocked by any collider.
/// </summary>
public class SphereRaycast2 : MonoBehaviour
{
    #region Editor fields
    [Tooltip("Determines the width of the Capsule cast used to detect object, max = 0.45f")]
    [SerializeField]
    [Range(0.0f, 0.45f)]
    private float castRadius = 0.35f;

    [Tooltip("Distance of the CapsuleCast, max = 2.0f")]
    [SerializeField]
    [Range(0.0f, 2.0f)]
    private float castDistance = 1.0f;

    [Tooltip("Angle which determines if object is close enought to center to ignore angle check")]
    [SerializeField]
    [Range(0, 90)]
    private int angleFromCenter = 10;

    [Tooltip("Angle which determines the max comparative angle between two objects")]
    [SerializeField]
    [Range(0, 90)]
    private int comparativeAngle = 5;
    #endregion

    #region Private fields
    private IInteractable objectToInteractWith; // reference to best IInteractable candidate
    private RaycastHit[] allHits; // array that will hold all object gathered by spherecast every frame
    public static event Action<IInteractable> ObjectToInteractWithChanged;
    #endregion


    // property: sets and pass reference of IInteractable object to interact with
    private IInteractable ObjectToInteractWith
    {
        get { return objectToInteractWith; }
        set
        {
            if (objectToInteractWith != value)
            {
                objectToInteractWith = value;
                ObjectToInteractWithChanged.Invoke(objectToInteractWith);
            }
        }
    }

    void FixedUpdate()
    {
        allHits = QuerySphereCastHits(); // query all hits and check edgecase 1&2

        switch (allHits.Length) // 
        {
            #region edgecase1: if it didnt find anything
            case 0:
                ObjectToInteractWith = null;
                break;
            #endregion edgecase1
            #region edgecase2: if theres only one object collided
            // skip all the nonsense and just check if the only one is interactable
            case 1:
                GameObject onlyCollided = allHits[0].collider.gameObject;
                ObjectToInteractWith = GetInteractable(onlyCollided);
                break;
            #endregion edgecase2
            #region default case call: if edgecase1&2* was passed (more than one object collided with spherecast)
            default:
                ObjectToInteractWith = GetOptimalInteract(); // cycle through the list to find suitable interact. null if none found.
                break;
                #endregion default case call
        }
    }

    #region spherecast
    /// <summary>
    /// Method that draws spherecast and query all objects hit to an array.
    /// Then checks edge cases 1 and 2 to see if further checking is needed.
    /// </summary>
    private RaycastHit[] QuerySphereCastHits()
    {
        return Physics.SphereCastAll(this.transform.position, castRadius,
                                        this.transform.forward, castDistance);
    }
    #endregion

    /// <summary>
    /// check if a single Hit object is blocked by collider.
    /// called by below overload or in edgecase 2 to just check the only object.
    /// </summary>
    /// <param name="toCheck">Hit object to be checked</param>
    /// <returns>
    ///     Interactable of the checked object if not blocked.
    ///     Null if the object doesn't implement IInteractable or blocked.
    /// </returns>
    #region raycast check for obstacles: check if the IInteractable is suitable for interaction
    private IInteractable GetInteractable(GameObject toCheck)
    {
        if (!IsBlocked(toCheck))
        {
#if UNITY_EDITOR
            Debug.DrawRay(this.transform.position, toCheck.transform.position - this.transform.position);
#endif
            return toCheck.GetComponent<IInteractable>();
        }
        else
            return null;
    }
    #endregion
    /// <summary>
    /// recursion override of the above for checking all objects in the Hits[].
    /// only used when one or more objects are found.
    /// ends when passed in third parameter 'index' passes the Hits[] index,
    /// in which case, the last found IInteractable will be returned.
    /// </summary>
    /// <param name="prevCandidate">Best IInteractable found at previous recursion</param>
    /// <param name="prevAngle">Angle of the last suitable IInteractable found</param>
    /// <param name="index">Current, or next, index of the recursion</param>
    /// <returns>
    ///     First IInteractable with offset angle less than 'angleFromCenter'.
    ///     Best suitable IInteractable found.
    ///     Null if none can be interacted.
    /// </returns>
    #region default case recursion: recursion override of the above for checking all objects    
    private IInteractable GetOptimalInteract(IInteractable prevCandidate = null, int prevAngle = 180, int index = 0)
    {
        #region end recursion
        if (index >= allHits.Length)// at the end of the allHits[] index,
            return prevCandidate; // end the recursion. Return whatever was found. null if none was found
        #endregion end recursion

        // try to get Interactable from current index if it's not blocked
        IInteractable toCompare
            = GetInteractable(allHits[index].collider.gameObject);

        #region ignore angle check
        if (toCompare == null) // ignore anglecheck if toCompare wasn't suitable
            return GetOptimalInteract(prevCandidate, prevAngle, index + 1);
        #endregion ignore angle check

        #region setup for angle check
        Vector3 dir = allHits[index].transform.position - this.transform.position; //directional vector towards object to compare
        int angle = (int)Vector3.Angle(this.transform.forward, dir); // angle to be compared
        #endregion setup for angle check

        #region angle checking
        // if angle between center is small enough to just call this candidate
        if (angle <= angleFromCenter)
            return toCompare;
        // if theres nothing to compare or current object is more closer to center by comparativeAngle,
        else if (prevCandidate == null || prevAngle - angle > comparativeAngle)
            return GetOptimalInteract(toCompare, angle, index + 1); // override the previous Interact candidate
        // if all failed, ignore this index and go onto next index
        return GetOptimalInteract(prevCandidate, prevAngle, index + 1);
        #endregion
    }
    #endregion default case recursion

    /// <summary>
    /// boolean check for obstacles between player and object.
    /// draws raycast from player toward the object,
    /// then check if object hit is object toCheck
    /// only called in 'GetSuitableInteract(GameObject toCheck)'
    /// </summary>
    /// <param name="toCheck">Object to be checked</param>
    /// <returns>
    /// False if object isn't blocked.
    /// True if it is blocked.
    /// </returns>
    private bool IsBlocked(GameObject toCheck)
    {
        bool toReturn;
        Vector3 dirFromPlayer = toCheck.transform.position - this.transform.position;
        RaycastHit hit; //holder for the collided object
        Physics.Raycast(this.transform.position, dirFromPlayer, out hit); // begin raycast from the player camera center
        if (toCheck == hit.collider.gameObject) // check if it hit the object to check
            toReturn = false; // not blocked
        else
            toReturn = true;
        return toReturn; //its blocked
    }

    #region Gizmo
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, castRadius);
        Gizmos.DrawWireSphere(this.transform.position + this.transform.forward * castDistance, castRadius);
    }
#endif
    #endregion Gizmo
}
