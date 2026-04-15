using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class MagneticBody : MonoBehaviour
{
    // Tracks the currently selected group so other scripts can reference it easily
    public static MagneticBody ActiveGroup;

    [Header("Magnet Settings")]
    public float magnetRange = 0.15f;
    public float snapSpeed = 5f;

    [Header("Unfold Settings (Group Mode)")]
    public float explodeDistance = 0.2f;
    public bool isExploded = false;

    private bool isGroupModeActive = false;

    // --- PARENT MEMORY ---
    // Stores where the entire group belongs in the world
    private Vector3 parentStartPos;
    private Quaternion parentStartRot;
    private Vector3 parentStartScale;
    private Vector3 parentVisualStartLocalPos;
    private bool isParentSnapped = true;

    // --- CHILD MEMORY ---
    // Ledgers tracking every individual bone's states
    private Dictionary<Transform, Vector3> foldedPositions = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Quaternion> foldedRotations = new Dictionary<Transform, Quaternion>();
    private Dictionary<Transform, Vector3> explodedPositions = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, bool> isSnapped = new Dictionary<Transform, bool>();

    // Tracks the true physical center of every bone mesh relative to its pivot
    private Dictionary<Transform, Vector3> childVisualOffsets = new Dictionary<Transform, Vector3>();

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable parentGrab;
    private BoxCollider parentCollider;

    void Awake()
    {
        // 1. Calculate the bounding box for the whole group
        AutoSizeParentCollider();

        // 2. Record where the parent group started
        parentStartPos = transform.localPosition;
        parentStartRot = transform.localRotation;
        parentStartScale = transform.localScale;
        parentVisualStartLocalPos = transform.localPosition + (transform.localRotation * parentCollider.center);

        // 3. Temporarily unparent children to set up the parent XR components cleanly
        List<Transform> tempChildren = new List<Transform>();
        while (transform.childCount > 0)
        {
            Transform child = transform.GetChild(0);
            child.SetParent(null, true);
            tempChildren.Add(child);
        }

        // 4. Ensure the parent has the required XR and Physics components
        parentGrab = gameObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (parentGrab == null) parentGrab = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // UNITY 6 FIX: Explicitly connect the dynamically generated script to the Manager
        if (parentGrab.interactionManager == null)
        {
            parentGrab.interactionManager = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
        }

        parentGrab.useDynamicAttach = true;
        parentGrab.throwOnDetach = false;       // Prevents the group from flying away if accidentally dropped

        SetupTwoHandedZoom(parentGrab);

        Rigidbody parentRb = gameObject.GetComponent<Rigidbody>();
        if (parentRb == null) parentRb = gameObject.AddComponent<Rigidbody>();
        parentRb.useGravity = false;
        parentRb.isKinematic = true;        // Kinematic ensures it stays where it is snapped

        // Link the auto-sized collider to the XR Grab script
        parentGrab.colliders.Clear();
        parentGrab.colliders.Add(parentCollider);

        // 5. Put the children back into the group
        foreach (Transform child in tempChildren)
        {
            child.SetParent(transform, true);
        }
    }

    void Start()
    {
        // Register this group as active when grabbed
        parentGrab.selectEntered.AddListener((args) => { ActiveGroup = this; });

        // Calculate the absolute geometric center of the entire cluster of bones
        Vector3 groupCenter = transform.position;
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
        if (allRenderers.Length > 0)
        {
            Bounds groupBounds = allRenderers[0].bounds;
            foreach (Renderer r in allRenderers) groupBounds.Encapsulate(r.bounds);
            groupCenter = groupBounds.center;
        }

        // Loop through every bone to set up its memory and explode paths
        foreach (Transform child in transform)
        {
            Renderer rend = child.GetComponent<Renderer>();
            if (rend == null) continue;

            // Save its starting (folded) state
            foldedPositions[child] = child.localPosition;
            foldedRotations[child] = child.localRotation;
            isSnapped[child] = true;

            // Calculate offset: Where is the mesh center compared to the transform pivot?
            childVisualOffsets[child] = child.InverseTransformPoint(rend.bounds.center);

            // Calculate Explode Position: Push outward from the group center
            Vector3 realBoneCenter = rend.bounds.center;
            Vector3 worldDirection = (realBoneCenter - groupCenter).normalized;
            if (worldDirection == Vector3.zero) worldDirection = Vector3.up;

            Vector3 localDirection = transform.InverseTransformDirection(worldDirection);
            explodedPositions[child] = child.localPosition + (localDirection * explodeDistance);

            SetupChildPhysics(child);
        }

        // Initialize default state
        SetGroupMode(false);
    }

    void Update()
    {
        // --- 1. PARENT GROUP MAGNET LOGIC ---
        // If holding the parent, it is no longer snapped
        if (parentGrab != null && parentGrab.isSelected)
        {
            isParentSnapped = false;
        }
        else if (!isParentSnapped)
        {
            // If dropped, check if it is close enough to its original spawn point to snap back
            Vector3 currentVisualLocalPos = transform.localPosition + (transform.localRotation * parentCollider.center);
            if (Vector3.Distance(currentVisualLocalPos, parentVisualStartLocalPos) < magnetRange)
            {
                isParentSnapped = true;
            }
        }

        // Animate the parent back to origin
        if (isParentSnapped)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, parentStartPos, Time.deltaTime * snapSpeed);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, parentStartRot, Time.deltaTime * snapSpeed);
            transform.localScale = Vector3.Lerp(transform.localScale, parentStartScale, Time.deltaTime * snapSpeed);
        }

        // --- 2. INDIVIDUAL BONE MAGNET LOGIC ---
        foreach (Transform child in foldedPositions.Keys)
        {
            UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable childGrab = child.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

            // If the user is actively holding this specific bone, release its magnetic snap
            if (childGrab != null && childGrab.isSelected)
            {
                isSnapped[child] = false;
                continue;
            }

            // Determine where the bone *should* go (Exploded vs Folded)
            Vector3 targetLocalPos = isExploded ? explodedPositions[child] : foldedPositions[child];
            Quaternion targetLocalRot = foldedRotations[child];

            if (!isSnapped[child])
            {
                // CRITICAL FIX: Calculate where the center of the mesh is RIGHT NOW, based on how the user twisted it via raycast
                Vector3 currentVisualLocalPos = child.localPosition + (child.localRotation * childVisualOffsets[child]);

                // Calculate where the center of the mesh SHOULD BE
                Vector3 targetVisualLocalPos = targetLocalPos + (targetLocalRot * childVisualOffsets[child]);

                // Compare the physical meshes, completely ignoring the pivot points!
                if (Vector3.Distance(currentVisualLocalPos, targetVisualLocalPos) < magnetRange)
                {
                    isSnapped[child] = true;
                }
            }

            // Animate the bone into its target position
            if (isSnapped[child])
            {
                child.localPosition = Vector3.Lerp(child.localPosition, targetLocalPos, Time.deltaTime * snapSpeed);
                child.localRotation = Quaternion.Lerp(child.localRotation, targetLocalRot, Time.deltaTime * snapSpeed);

                // Kill physics momentum so it doesn't jitter while snapping
                Rigidbody rb = child.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }


    // Toggles between interacting with the whole group vs individual bones
    public void SetGroupMode(bool isGroupMode)
    {
        isGroupModeActive = isGroupMode;

        if (!isGroupModeActive)
        {
            isExploded = false;
            isParentSnapped = true;

            // Force scattered bones to fly back into the group
            foreach (Transform child in foldedPositions.Keys)
            {
                isSnapped[child] = true;
            }
        }

        UpdateGrabbableState();
    }

    // Explodes the group outward or pulls them back in.
    public void TriggerUnfold()
    {
        if (!isGroupModeActive) return;

        isExploded = !isExploded;

        // If folding back up, force everything to snap back to the origin
        if (!isExploded)
        {
            foreach (Transform child in foldedPositions.Keys)
            {
                isSnapped[child] = true;
            }
        }

        UpdateGrabbableState();
    }

    // Manages XR Grab states to prevent Unity from confusing Parent/Child grabs.
    private void UpdateGrabbableState()
    {
        // Parent is only grabbable if Group Mode is on AND it is not exploded
        bool parentActive = isGroupModeActive && !isExploded;

        // Children are grabbable if we are out of Group Mode, OR if they are currently exploded
        bool childrenActive = !isGroupModeActive || isExploded;

        if (parentGrab != null) parentGrab.enabled = parentActive;
        if (parentCollider != null) parentCollider.enabled = parentActive;

        foreach (Transform child in foldedPositions.Keys)
        {
            var grab = child.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            var col = child.GetComponent<Collider>();
            var rb = child.GetComponent<Rigidbody>();

            if (grab != null) grab.enabled = childrenActive;
            if (col != null) col.enabled = childrenActive;
            if (rb != null) rb.isKinematic = !childrenActive;
        }
    }

    // Automatically applies rigidbodies and grab scripts to bones.
    // Automatically applies rigidbodies and grab scripts to bones.
    void SetupChildPhysics(Transform part)
    {
        // UNITY 6 FIX: If the bone is completely missing a collider, generate one!
        Collider[] cols = part.GetComponents<Collider>();
        if (cols.Length == 0)
        {
            MeshCollider mc = part.gameObject.AddComponent<MeshCollider>();
            mc.convex = true; // MUST be convex for grab to work with rigidbodies
            cols = new Collider[] { mc };
        }
        else
        {
            foreach (Collider c in cols) c.enabled = true;
        }

        var grab = part.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab == null) grab = part.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // UNITY 6 FIX: Explicitly connect the child grab script to the Manager
        if (grab.interactionManager == null)
        {
            grab.interactionManager = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
        }

        grab.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.VelocityTracking;
        grab.useDynamicAttach = true;

        grab.colliders.Clear();
        foreach (Collider c in cols) grab.colliders.Add(c);

        var rb = part.GetComponent<Rigidbody>();
        if (rb == null) rb = part.gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.linearDamping = 2f;
        rb.angularDamping = 2f;
    }

    // Calculates a BoxCollider that encompasses all child meshes.
    void AutoSizeParentCollider()
    {
        parentCollider = GetComponent<BoxCollider>();
        if (parentCollider == null) parentCollider = gameObject.AddComponent<BoxCollider>();

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);

        parentCollider.center = bounds.center - transform.position;
        parentCollider.size = bounds.size;
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
}