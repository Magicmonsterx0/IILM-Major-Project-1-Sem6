using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MasterTurntable : MonoBehaviour
{
    private Vector3 startPos;
    private Quaternion startRot;
    private Rigidbody masterRb;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable masterGrab;
    private BoxCollider masterCollider;
    private MagneticBody[] allMagneticGroups;

    [Header("Auto Spin (Optional)")]
    public float spinSpeed = 0f; // Set this higher than 0 in the Inspector if you want it to spin by itself
    private bool isSpinning = false;

    void Awake()
    {
        startPos = transform.position;
        startRot = transform.rotation;

        // 1. Set up Master Physics
        masterRb = gameObject.GetComponent<Rigidbody>();
        if (masterRb == null) masterRb = gameObject.AddComponent<Rigidbody>();
        masterRb.isKinematic = true;
        masterRb.useGravity = false;

        masterCollider = gameObject.GetComponent<BoxCollider>();
        if (masterCollider == null) masterCollider = gameObject.AddComponent<BoxCollider>();
        masterCollider.enabled = false;

        // --- THE UNPARENTING HACK TO SILENCE XRI WARNINGS ---
        // Hide all the subgroups from the XR system temporarily
        System.Collections.Generic.List<Transform> tempChildren = new System.Collections.Generic.List<Transform>();
        while (transform.childCount > 0)
        {
            Transform child = transform.GetChild(0);
            child.SetParent(null, true);
            tempChildren.Add(child);
        }

        // 2. Safely add the Master Grab while the children are hidden
        masterGrab = gameObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (masterGrab == null) masterGrab = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        masterGrab.enabled = false;

        SetupTwoHandedZoom(masterGrab);

        // Lock it to the master collider ONLY
        masterGrab.colliders.Clear();
        masterGrab.colliders.Add(masterCollider);

        // 3. Put all the subgroups perfectly back into place
        foreach (Transform child in tempChildren)
        {
            child.SetParent(transform, true);
        }
        // ----------------------------------------------------

        // Cache the groups for the toggle logic
        allMagneticGroups = GetComponentsInChildren<MagneticBody>(true);
    }

    void Update()
    {
        if (isSpinning && spinSpeed > 0f)
        {
            // Rotates the object smoothly around the Y-axis (Up/Down) over time
            transform.Rotate(0, spinSpeed * Time.deltaTime, 0, Space.World);
        }
    }

    public void ToggleInspectMode(bool enable)
    {
        isSpinning = enable;

        if (enable)
        {
            // 1. Force all subgroups to fold up
            foreach (var group in allMagneticGroups)
            {
                if (group.isExploded) group.TriggerUnfold();
            }

            // CRITICAL FIX 2: Turn off child colliders so the raycast hits the Master box
            Collider[] allColliders = GetComponentsInChildren<Collider>(true);
            foreach (var col in allColliders)
            {
                if (col != masterCollider) col.enabled = false;
            }

            // CRITICAL FIX 3: Freeze every child bone's physics so centrifugal force doesn't rip them apart!
            Rigidbody[] allRbs = GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in allRbs)
            {
                if (rb != masterRb) rb.isKinematic = true;
            }

            // 2. Build the giant master hitbox
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            }
            masterCollider.center = bounds.center - transform.position;
            masterCollider.size = bounds.size;
            masterCollider.enabled = true;

            // 3. Turn on Master Grab and apply Turntable Physics
            masterGrab.enabled = true;
            masterRb.isKinematic = false;

            masterRb.constraints = RigidbodyConstraints.FreezePosition |
                                   RigidbodyConstraints.FreezeRotationX |
                                   RigidbodyConstraints.FreezeRotationZ;

            masterGrab.trackPosition = false;
            masterGrab.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.VelocityTracking;
        }
        else
        {
            // 1. Turn off the Master Grab
            masterGrab.enabled = false;
            masterCollider.enabled = false;
            masterRb.isKinematic = true;
            masterRb.constraints = RigidbodyConstraints.None;

            // 2. Restore all subgroups.
            // This automatically turns the child colliders and child rigidbodies back on!
            foreach (var group in allMagneticGroups)
            {
                group.SetGroupMode(false);
            }
        }
    }

    private void SetupTwoHandedZoom(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab)
    {
        // 1. Allow multiple hands to grab the object
        grab.selectMode = UnityEngine.XR.Interaction.Toolkit.Interactables.InteractableSelectMode.Multiple;

        // 2. Add the Unity scaling transformer
        var transformer = grab.gameObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Transformers.XRGeneralGrabTransformer>();
        if (transformer == null)
        {
            transformer = grab.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Transformers.XRGeneralGrabTransformer>();
        }

        // 3. Enable the zoom and set safe limits
        transformer.allowTwoHandedScaling = true;
        transformer.minimumScaleRatio = 0.2f; // 20% of original size
        transformer.maximumScaleRatio = 5.0f; // 500% of original size

        // We completely removed Step 4! Your version of XRI automatically detects the component we just added above.
    }

    public void ResetToOrigin()
    {
        // 1. Find every single bone in the body
        Rigidbody[] allRbs = GetComponentsInChildren<Rigidbody>();
        System.Collections.Generic.Dictionary<Rigidbody, bool> previousStates = new System.Collections.Generic.Dictionary<Rigidbody, bool>();

        // 2. Temporarily freeze ALL of them so they don't explode from G-forces
        foreach (var rb in allRbs)
        {
            previousStates[rb] = rb.isKinematic;
            rb.isKinematic = true;
        }

        // 3. Safely teleport and rotate the parent
        transform.position = startPos;
        transform.rotation = startRot;

        // 4. Give them their original physics states back
        foreach (var rb in allRbs)
        {
            rb.isKinematic = previousStates[rb];
        }
    }
}