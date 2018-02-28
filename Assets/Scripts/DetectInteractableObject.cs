using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detects best interactable object from the player's location and view.
/// Uses SphereCastAll and SortedList to get the closest object to the center of screen.
/// Picked object must be inside the SphereCast and not be blocked by any collider.
/// </summary>
public class DetectInteractableObject : MonoBehaviour
{
    #region Editor Fields
    [Tooltip("Determines the width of the Capsule cast used to detect object, max = 0.45f")]
    [SerializeField]
    [Range(0.0f,0.45f)]
    private float castRadius = 0.45f;
    [Tooltip("Distance of the CapsuleCast,max = 2.0f")]
    [SerializeField]
    [Range(0.0f,2.0f)]
    private float castDistance = 1.0f;
    #endregion Editor Fields

    #region private fields
    private IInteractable objectToInteractWith; // reference to best Interact candidate
    private RaycastHit[] allHits; // array that will hold all object gathered by spherecast every frame
    private SortedList<float, GameObject> interactSortedByAngle;
    public static event Action<IInteractable> ObjectToInteractWithChanged;
    #endregion private fields

    // property: sets and pass reference of object to Interact
    public IInteractable ObjectToInteractWith
    {
        get { return objectToInteractWith; }
        private set
        {
            if (objectToInteractWith != value)
            {
                objectToInteractWith = value;
                ObjectToInteractWithChanged.Invoke(objectToInteractWith);
            }
        }
    }

    /// <summary>
    /// Initializes the list and assign getMethod to delegate.
    /// </summary>
    private void Start()
    {
        interactSortedByAngle = new SortedList<float, GameObject>();
    }

    void FixedUpdate()
    {
        querySphereCastHits(); // query all hits from sphereCastAll and check edgecase 1&2

        #region default case: if edgecase1&2* was passed (more than one object collided with spherecast)
        if (allHits.Length > 1)
        {
            collectInteractables(); // preparation for edgecase3*: find and sort all IInteractables
            ObjectToInteractWith = getSuitableInteract(); // cycle through the list to find suitable interact. null if none found.
            interactSortedByAngle.Clear(); // clear the list for next frame
        }
        #endregion default case
    }

    #region SphereCast and edgecase 1&2
    /// <summary>
    /// Method that draws spherecast and query all objects hit to an array.
    /// Then checks edge cases 1 and 2 to see if further checking is needed.
    /// </summary>
    private void querySphereCastHits()
    {
        allHits = Physics.SphereCastAll(this.transform.position, castRadius,
                                        this.transform.forward, castDistance); // spherecast to find the objects.

        #region edgecase1: if it didnt find anything
        if (allHits.Length == 0)
        {
            ObjectToInteractWith = null;
            return;
        }
        #endregion edgecase1

        #region edgecase2: if theres only one object collided
        if (allHits.Length == 1)
        {
            GameObject onlyCollided = allHits[0].collider.gameObject; // the only one that was collided
            //Debug.Log(onlyCollided);
            ObjectToInteractWith = getSuitableInteract(onlyCollided); //skip all the nonsense and just check if the only one is interactable
            return;
        }
        #endregion edgecase2
    }
    /// <summary>
    /// Method for picking all interactables from array created above.
    /// Index through the array of hit and check if it is Interactable.
    /// If it is, calculate the angle from the center of the screen and store it to array as sorted
    /// </summary>
    private void collectInteractables()//find and store all interactables
    {
        Vector3 dir = new Vector3();
        float angle;
        for (int i = 0; i < allHits.Length; i++)
        {
#if UNITY_EDITOR
            Debug.DrawRay(this.transform.position, allHits[i].transform.position - this.transform.position);
#endif
            if (allHits[i].collider.GetComponent<IInteractable>() == null)
                continue; // if it's not interactable, skip to next index
            dir = allHits[i].transform.position - this.transform.position; // direction from player towards the object
            angle = Vector3.Angle(this.transform.forward, dir); // angle between center and object
            interactSortedByAngle.Add(angle, allHits[i].collider.gameObject); // add the object to list
        }
    }
    #endregion SphereCast SphereCast and edgecase 1&2

    #region Methods for raycast check for obstacles
    /// <summary>
    /// check if a single Hit object is blocked by collider.
    /// called by below overload or in edgecase 2 to just check the only object.
    /// </summary>
    /// <param name="toCheck">Hit object to be checked</param>
    /// <returns>
    /// Interactable of the checked object if not blocked.
    /// Null if the object checked isn't IInteractable or blocked.
    /// </returns>
    private IInteractable getSuitableInteract(GameObject toCheck)
    {
        if (!isBlocked(toCheck)) // if its not blocked
        {
            return toCheck.GetComponent<IInteractable>(); // grab the interactable of the object
        }
        else // if its blocked
            return null;
    }
    /// <summary>
    /// override for checking all object in the SortedList for collision.
    /// only called in default case.
    /// </summary>
    /// <returns>
    /// Interactable of the closest unblocked object to the center.
    /// Null if all objects are blocked.
    /// </returns>
    private IInteractable getSuitableInteract() // returns the closest interactable that isn't blocked
    {
        IInteractable returnCandidate;
        foreach (KeyValuePair<float, GameObject> kvp in interactSortedByAngle)
        {
            // checks if current kvp is suitable
            returnCandidate = getSuitableInteract(kvp.Value);
            if (returnCandidate == null) // move onto next if not
                continue;
            return returnCandidate;// when candidate found
        }
        return null; // all are blocked
    }
    #endregion Methods for raycast check for obstacles

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

    #region debug
    //debugging stuff
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(this.transform.position, castRadius);
        Gizmos.DrawWireSphere(this.transform.position + this.transform.forward * castDistance, castRadius);
    }
    private void debugHits()
    {
        Debug.Log(allHits.Length);
        for (int i = 0; i < allHits.Length; i++)
        {
            Debug.Log(allHits[i].collider.gameObject.name);
            Debug.Log(string.Format("Hit [{0}]: {1}", i, allHits[i].distance));
        }
    }
    private void debugList()
    {
        foreach (KeyValuePair<float, GameObject> toCheck in interactSortedByAngle)
        {
            Debug.Log(toCheck.Value.name);
        }
    }
    private void debugInteract()
    {
        if (ObjectToInteractWith != null)
            Debug.Log("Detected");
    }
    #endregion debug
}
