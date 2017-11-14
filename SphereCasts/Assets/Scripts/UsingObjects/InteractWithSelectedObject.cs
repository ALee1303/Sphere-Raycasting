using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// delegate for holding method that returns IInteractable.
/// declared and assigned at raycast script.
/// </summary>
public delegate void ReturnReferenceMethod(IInteractable returnedReference);

/// <summary>
/// Script needed to detect and interact with the object selected by raycast.
/// Attached to the FirstPersonCharacter child of the FPS Controller.
/// </summary>
public class InteractWithSelectedObject : MonoBehaviour
{
    private IInteractable selectedInteractableObject; // reference pointing to the object best suitable for interaction

    private void Update()
    {
        CheckForInteractionInput();
        DetectInteractableObjectComparative.allName.Clear();
        DetectInteractableObjectComparative.allAngle.Clear();
    }

    /// <summary>
    /// Assigned to raycast script's delegate field.
    /// Returns ToInteract picked by raycast to this script's reference.
    /// </summary>
    #region Method(for delegate assignment)   
    public void GetInteractReference(IInteractable returnedInteract)
    {
        selectedInteractableObject = returnedInteract;
    }
    #endregion Method

    /// <summary>
    /// checks if Interact is not null then if interact input is pressed.
    /// activate interact of selectedInteract if both return true.
    /// </summary>
    private void CheckForInteractionInput()
    {
        if (selectedInteractableObject != null && Input.GetKeyDown(KeyCode.Mouse0))
        {
            selectedInteractableObject.Interact();
        }
        #region Debugging
        else if (selectedInteractableObject == null && Input.GetKeyDown(KeyCode.Mouse0))
        {
            Debug.Log("No Interactable detected");
        }
        #endregion Debugging
    }
}
