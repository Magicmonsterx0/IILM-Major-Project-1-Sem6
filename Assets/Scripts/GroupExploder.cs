using UnityEngine;

using System.Collections.Generic;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable), typeof(Rigidbody))]
public class GroupExploder : MonoBehaviour
{
    [Header("UI & Settings")]
    public GameObject floatingButtonCanvas;
    public float explodeDistance = 0.2f;
    public float moveSpeed = 5f;

    // State trackers
    private bool isExploded = false;
    private bool isGlobalExplodeMode = false;

    // Memory
    private Dictionary<Transform, Vector3> startPos = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Quaternion> startRot = new Dictionary<Transform, Quaternion>();
    private Dictionary<Transform, Vector3> explodedPos = new Dictionary<Transform, Vector3>();

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable parentGrab;
    private BoxCollider parentCollider;

    void Start()
    {
        parentGrab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // Ensure the parent doesn't fall through the floor!
        Rigidbody parentRb = GetComponent<Rigidbody>();
        parentRb.useGravity = false;
        parentRb.isKinematic = true;

        // Auto-generate the perfect Box Collider
        AutoSizeParentCollider();

        // Catalog all children
        foreach (Transform child in transform)
        {
            if (child.gameObject == floatingButtonCanvas || child.GetComponent<Renderer>() == null) continue;

            startPos[child] = child.localPosition;
            startRot[child] = child.localRotation;

            Vector3 dir = child.localPosition.normalized;
            if (dir == Vector3.zero) dir = Vector3.up;
            explodedPos[child] = child.localPosition + (dir * explodeDistance);

            SetupChildPhysics(child);
        }

        // Default to dissect mode
        SetGlobalMode(false);
    }

    void Update()
    {
        foreach (Transform child in startPos.Keys)
        {
            UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab = child.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab != null && grab.isSelected) continue;

            Vector3 targetPos = isExploded ? explodedPos[child] : startPos[child];
            child.localPosition = Vector3.Lerp(child.localPosition, targetPos, Time.deltaTime * moveSpeed);
            child.localRotation = Quaternion.Lerp(child.localRotation, startRot[child], Time.deltaTime * moveSpeed);
        }
    }

    public void SetGlobalMode(bool explodeModeActive)
    {
        isGlobalExplodeMode = explodeModeActive;

        if (isGlobalExplodeMode)
        {
            parentGrab.enabled = true;
            if (parentCollider != null) parentCollider.enabled = true;
            if (floatingButtonCanvas) floatingButtonCanvas.SetActive(true);

            SetChildrenGrabbable(false); // Locks children, turns off their physics
            isExploded = false;
        }
        else
        {
            parentGrab.enabled = false;
            if (parentCollider != null) parentCollider.enabled = false;
            if (floatingButtonCanvas) floatingButtonCanvas.SetActive(false);

            SetChildrenGrabbable(true); // Unlocks children
            isExploded = false;
        }
    }

    public void ToggleLocalExplode()
    {
        if (!isGlobalExplodeMode) return;

        isExploded = !isExploded;

        SetChildrenGrabbable(isExploded);

        parentGrab.enabled = !isExploded;
        if (parentCollider != null) parentCollider.enabled = !isExploded;
    }

    void SetupChildPhysics(Transform child)
    {
        var grab = child.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab == null) grab = child.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grab.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.VelocityTracking;

        var rb = child.GetComponent<Rigidbody>();
        if (rb == null) rb = child.gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
    }

    // --- THE FIX IS HERE ---
    void SetChildrenGrabbable(bool canGrab)
    {
        foreach (Transform child in startPos.Keys)
        {
            // 1. Toggle Grabbing
            var grab = child.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab != null) grab.enabled = canGrab;

            // 2. Stop the Physics Tug-of-War
            var rb = child.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = !canGrab; // Freezes physics when locked!

            // 3. Turn off child colliders so VR laser only hits the Parent Box
            var colliders = child.GetComponents<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = canGrab;
            }
        }
    }

    void AutoSizeParentCollider()
    {
        parentCollider = gameObject.AddComponent<BoxCollider>();

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;

        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        parentCollider.center = bounds.center - transform.position;
        parentCollider.size = bounds.size;
    }
}
