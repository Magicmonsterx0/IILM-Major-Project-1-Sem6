using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;    // Unity 6 Interactor Library
using UnityEngine.XR.Interaction.Toolkit.Interactables;  // Unity 6 Interactable Library

public class VRInputManager : MonoBehaviour
{
    [Header("Controller Mapping")]
    [Tooltip("Select the controller button (e.g., Right Hand Primary Button)")]
    public InputActionReference unfoldButton;

    [Header("Unity 6 XR Interactor")]
    [Tooltip("Drag your Near-Far Interactor here")]
    // CHANGED: We now use the base class so it accepts the new NearFarInteractor!
    public XRBaseInteractor rayInteractor;

    private void OnEnable()
    {
        if (unfoldButton != null)
        {
            unfoldButton.action.Enable();
            unfoldButton.action.performed += OnUnfoldPressed;
        }
    }

    private void OnDisable()
    {
        if (unfoldButton != null)
        {
            unfoldButton.action.performed -= OnUnfoldPressed;
        }
    }

    private void OnUnfoldPressed(InputAction.CallbackContext context)
    {
        // 1. Safety check: Do we have a laser to read?
        if (rayInteractor == null) return;

        // 2. Unity 6 Magic: Instead of shooting a physics raycast, 
        // we just ask the laser if it is currently touching an interactable bone!
        if (rayInteractor.hasHover && rayInteractor.interactablesHovered.Count > 0)
        {
            // Get the transform of the exact bone the laser is highlighting
            IXRHoverInteractable hoveredTarget = rayInteractor.interactablesHovered[0];
            Transform hitTransform = hoveredTarget.transform;

            // 3. Scenario A: Did we hit a folded group directly?
            MagneticBody targetGroup = hitTransform.GetComponent<MagneticBody>();

            // 4. Scenario B: Did we hit an unfolded child bone? 
            // (Search 'up' the hierarchy to find its parent group)
            if (targetGroup == null)
            {
                targetGroup = hitTransform.GetComponentInParent<MagneticBody>();
            }

            // 5. If we successfully found a group in either scenario, trigger the magic!
            if (targetGroup != null)
            {
                targetGroup.TriggerUnfold();
            }
        }
    }
}