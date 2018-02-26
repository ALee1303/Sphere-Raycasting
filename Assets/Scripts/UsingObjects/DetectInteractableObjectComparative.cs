using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detects best interactable object from the player's location and view.
/// Uses SphereCastAll and two angle variables to get the closest object to the player.
/// Cycles through all the Hits to find best candidate to interact by:
///     First priority: First object from Hits[] that has an offset angle from center that is less than 'angleFromCenter'.
///     Second priority: Last unblocked-Interactable that is closer to the center by more than 'comparativeAngle' than previous one.
///     Third priority: First unblocked-Interactable that was found.
/// Picked object must be inside the SphereCast and not be blocked by any collider.
/// </summary>
public class DetectInteractableObjectComparative : MonoBehaviour
{
    #region Editor Fields
    [Tooltip("Determines the width of the Capsule cast used to detect object, max = 0.45f")]
    [SerializeField]
    [Range(0.0f, 0.45f)]
    private float castRadius = 0.45f;
    [Tooltip("Distance of the CapsuleCast,max = 2.0f")]
    [SerializeField]
    [Range(0.0f, 2.0f)]
    private float castDistance = 1.0f;
    [Tooltip("Angle which determines if object is close enought to center to ignore angle check")]
    [SerializeField]
    [Range(0,90)]
    private int angleFromCenter = 10;
    [Tooltip("Angle which determines the max comparative angle between two objects")]
    [SerializeField]
    [Range(0, 90)]
    private int comparativeAngle = 5;
    #endregion Editor Fields

    #region private fields
    private IInteractable objectToInteractWith; // reference to best Interact candidate
    private RaycastHit[] allHits; // array that will hold all object gathered by spherecast every frame
    private ReturnReferenceMethod returnInteractReference; // delegate for passing objectToInteractWith reference
    #endregion private fields
    
    ////Debug////
    public static Stack<float> allAngle;
    public static Stack<string> allName;

    // property: sets and pass reference of object to Interact
    public IInteractable ObjectToInteractWith
    {
        get { return objectToInteractWith; }
        private set
        {
            if (objectToInteractWith != value) 
            {
                objectToInteractWith = value;
                returnInteractReference(objectToInteractWith);
            }
        }
    }

    private void Start()
    {
        returnInteractReference = this.GetComponent<InteractWithSelectedObject>().GetInteractReference;
        allAngle = new Stack<float>();
        allName = new Stack<string>();
    }

    void FixedUpdate()
    {
        querySphereCastHits(); // query all hits and check edgecase 1&2

        #region default case call: if edgecase1&2* was passed (more than one object collided with spherecast)
        if (allHits.Length > 1)
            ObjectToInteractWith = getSuitableInteract(); // cycle through the list to find suitable interact. null if none found.
        #endregion default case call
    }

    #region spherecast and edgecase 1&2
    /// <summary>
    /// Method that draws spherecast and query all objects hit to an array.
    /// Then checks edge cases 1 and 2 to see if further checking is needed.
    /// </summary>
    private void querySphereCastHits()
    {
        allHits = Physics.SphereCastAll(this.transform.position, castRadius,
                                        this.transform.forward, castDistance);
        #region edgecase1: if it didnt find anything
        if (allHits.Length == 0)
        {
            ObjectToInteractWith = null;
            return;
        }
        #endregion edgecase1

        #region edgecase2: if theres only one object collided
        // skip all the nonsense and just check if the only one is interactable
        if (allHits.Length == 1)
        {
            GameObject onlyCollided = allHits[0].collider.gameObject;
            ObjectToInteractWith = getSuitableInteract(onlyCollided);
            return;
        }
        #endregion edgecase2
    }
    #endregion spherecast and edgecase 1&2

    /// <summary>
    /// check if a single Hit object is blocked by collider.
    /// called by below overload or in edgecase 2 to just check the only object.
    /// </summary>
    /// <param name="toCheck">Hit object to be checked</param>
    /// <returns>
    ///     Interactable of the checked object if not blocked.
    ///     Null if the object checked isn't IInteractable or blocked.
    /// </returns>
    #region raycast check for obstacles: check if the Interactable is suitable for interaction
    private IInteractable getSuitableInteract(GameObject toCheck)
    {
        if (!isBlocked(toCheck))
        {
            Debug.DrawRay(this.transform.position, toCheck.transform.position - this.transform.position);
            return toCheck.GetComponent<IInteractable>();
        }
        else
            return null;
    }
    #endregion raycast check for obstacles

    /// <summary>
    /// recursion override of the above for checking all objects in the Hits[].
    /// only used when one or more objects are found.
    /// ends when passed in third parameter 'idx' passes the Hits[] index,
    /// in which case, the last found Interactable will be returned.
    /// </summary>
    /// <param name="prevCandidate">Best interactable found at previous recursion</param>
    /// <param name="prevAngle">Angle of the last suitable interact found</param>
    /// <param name="idx">Current, or next, index of the recursion</param>
    /// <returns>
    ///     First Interact with offset angle less than 'angleFromCenter'.
    ///     Best suitable IInteractable found.
    ///     Null if none can be interacted.
    /// </returns>
    #region default case recursion: recursion override of the above for checking all objects    
    private IInteractable getSuitableInteract(IInteractable prevCandidate = null, int prevAngle = 180, int idx = 0)
    {
        #region end recursion
        if (idx >= allHits.Length)// at the end of the allHits[] index,
            return prevCandidate; // end the recursion. Return whatever was found. null if none was found
        #endregion end recursion

        // try to get Interactable from current index if it's not blocked
        IInteractable toCompare
            = getSuitableInteract(allHits[idx].collider.gameObject);

        #region ignore angle check
        if (toCompare == null) // ignore anglecheck if toCompare wasn't suitable
            return getSuitableInteract(prevCandidate, prevAngle, idx + 1);
        #endregion ignore angle check

        #region setup for angle check
        Vector3 dir = allHits[idx].transform.position - this.transform.position; //directional vector towards object to compare
        int angle = (int)Vector3.Angle(this.transform.forward, dir); // angle to be compared
        #endregion setup for angle check

        ////debug////
        allAngle.Push(angle);
        allName.Push(allHits[idx].collider.gameObject.name);

        #region angle checking
        // if angle between center is small enough to just call this candidate
        if (angle <= angleFromCenter)
            return toCompare;
        // if theres nothing to compare or current object is more closer to center by comparativeAngle,
        else if (prevCandidate == null || prevAngle - angle > comparativeAngle)
            return getSuitableInteract(toCompare, angle, idx + 1); // override the previous Interact candidate
        // if all failed, ignore this index and go onto next index
        return getSuitableInteract(prevCandidate, prevAngle, idx + 1);
        #endregion angle checking
    }
    #endregion default case recursion

    /// <summary>
    /// boolean check for obstacles between player and object.
    /// draws raycast from player toward the object,
    /// then check if object hit is object toCheck
    /// only called in 'getSuitableInteract(GameObject toCheck)'
    /// </summary>
    /// <param name="toCheck">Object to be checked</param>
    /// <returns>
    /// True if object isn't blocked.
    /// Flase if it is blocked.
    /// </returns>
    private bool isBlocked(GameObject toCheck)
    {
        Vector3 dir = toCheck.transform.position - this.transform.position; // direction from player to object
        RaycastHit hit; //holder for the collided object
        Physics.Raycast(this.transform.position, dir, out hit); // begin raycast from the player camera center
        if (toCheck == hit.collider.gameObject) // check if it hit the object to check
            return false; // not blocked
        return true; //its blocked
    }

    #region debugging
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, castRadius);
        Gizmos.DrawWireSphere(this.transform.position + this.transform.forward * castDistance, castRadius);
    }
    #endregion debugging
}
