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

    [Header("Mesh Optimization")]
    [Tooltip("Check this ONLY for delicate webs like Veins/Nerves. Prevents physics crashing.")]
    public bool isComplexWeb = false;

    private bool isGroupModeActive = false;

    // --- PARENT MEMORY ---
    private Vector3 parentStartPos;
    private Quaternion parentStartRot;
    private Vector3 parentStartScale;
    private Vector3 parentVisualStartLocalPos;
    private bool isParentSnapped = true;

    // --- CHILD MEMORY ---
    private Dictionary<Transform, Vector3> foldedPositions = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Quaternion> foldedRotations = new Dictionary<Transform, Quaternion>();
    private Dictionary<Transform, Vector3> explodedPositions = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, bool> isSnapped = new Dictionary<Transform, bool>();
    private Dictionary<Transform, Vector3> childVisualOffsets = new Dictionary<Transform, Vector3>();

    // --- PERFORMANCE CACHES (To prevent frame drops in Update) ---
    private Dictionary<Transform, UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> cachedGrabs = new Dictionary<Transform, UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    private Dictionary<Transform, Rigidbody> cachedRbs = new Dictionary<Transform, Rigidbody>();

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable parentGrab;
    private BoxCollider parentCollider;

    void Awake()
    {
        AutoSizeParentCollider();

        parentStartPos = transform.localPosition;
        parentStartRot = transform.localRotation;
        parentStartScale = transform.localScale;
        parentVisualStartLocalPos = transform.localPosition + (transform.localRotation * parentCollider.center);

        List<Transform> tempChildren = new List<Transform>();
        while (transform.childCount > 0)
        {
            Transform child = transform.GetChild(0);
            child.SetParent(null, true);
            tempChildren.Add(child);
        }

        parentGrab = gameObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (parentGrab == null) parentGrab = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (parentGrab.interactionManager == null)
        {
            parentGrab.interactionManager = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
        }

        parentGrab.useDynamicAttach = true;
        parentGrab.throwOnDetach = false;

        SetupTwoHandedZoom(parentGrab);

        Rigidbody parentRb = gameObject.GetComponent<Rigidbody>();
        if (parentRb == null) parentRb = gameObject.AddComponent<Rigidbody>();
        parentRb.useGravity = false;
        parentRb.isKinematic = true;

        parentGrab.colliders.Clear();
        parentGrab.colliders.Add(parentCollider);

        foreach (Transform child in tempChildren)
        {
            child.SetParent(transform, true);
        }
    }

    void Start()
    {
        parentGrab.selectEntered.AddListener((args) => { ActiveGroup = this; });

        Vector3 groupCenter = transform.position;
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
        if (allRenderers.Length > 0)
        {
            Bounds groupBounds = allRenderers[0].bounds;
            foreach (Renderer r in allRenderers) groupBounds.Encapsulate(r.bounds);
            groupCenter = groupBounds.center;
        }

        foreach (Transform child in transform)
        {
            Renderer rend = child.GetComponent<Renderer>();
            if (rend == null) continue;

            foldedPositions[child] = child.localPosition;
            foldedRotations[child] = child.localRotation;
            isSnapped[child] = true;

            childVisualOffsets[child] = child.InverseTransformPoint(rend.bounds.center);

            Vector3 realBoneCenter = rend.bounds.center;
            Vector3 worldDirection = (realBoneCenter - groupCenter).normalized;
            if (worldDirection == Vector3.zero) worldDirection = Vector3.up;

            Vector3 localDirection = transform.InverseTransformDirection(worldDirection);
            explodedPositions[child] = child.localPosition + (localDirection * explodeDistance);

            SetupChildPhysics(child);
        }

        SetGroupMode(true);
    }

    void Update()
    {
        // --- 1. PARENT GROUP MAGNET LOGIC ---
        if (parentGrab != null && parentGrab.isSelected)
        {
            isParentSnapped = false;
        }
        else if (!isParentSnapped)
        {
            Vector3 currentVisualLocalPos = transform.localPosition + (transform.localRotation * parentCollider.center);
            if (Vector3.Distance(currentVisualLocalPos, parentVisualStartLocalPos) < magnetRange)
            {
                isParentSnapped = true;
            }
        }

        if (isParentSnapped)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, parentStartPos, Time.deltaTime * snapSpeed);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, parentStartRot, Time.deltaTime * snapSpeed);
            transform.localScale = Vector3.Lerp(transform.localScale, parentStartScale, Time.deltaTime * snapSpeed);
        }

        // --- 2. INDIVIDUAL BONE MAGNET LOGIC ---
        foreach (Transform child in foldedPositions.Keys)
        {
            // PERFORMANCE FIX: Read from dictionary memory instead of searching for the component!
            var childGrab = cachedGrabs[child];

            if (childGrab != null && childGrab.isSelected)
            {
                isSnapped[child] = false;
                continue;
            }

            Vector3 targetLocalPos = isExploded ? explodedPositions[child] : foldedPositions[child];
            Quaternion targetLocalRot = foldedRotations[child];

            if (!isSnapped[child])
            {
                Vector3 currentVisualLocalPos = child.localPosition + (child.localRotation * childVisualOffsets[child]);
                Vector3 targetVisualLocalPos = targetLocalPos + (targetLocalRot * childVisualOffsets[child]);

                if (Vector3.Distance(child.localPosition, targetLocalPos) < magnetRange)
                {
                    isSnapped[child] = true;
                }
            }

            if (isSnapped[child])
            {
                // PERFORMANCE FIX: Read from dictionary memory instead of searching for the component!
                Rigidbody rb = cachedRbs[child];
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                child.localPosition = Vector3.Lerp(child.localPosition, targetLocalPos, Time.deltaTime * snapSpeed);
                child.localRotation = Quaternion.Lerp(child.localRotation, targetLocalRot, Time.deltaTime * snapSpeed);
            }
        }
    }

    public void SetGroupMode(bool isGroupMode)
    {
        isGroupModeActive = isGroupMode;

        if (!isGroupModeActive)
        {
            isExploded = false;
            isParentSnapped = true;

            foreach (Transform child in foldedPositions.Keys)
            {
                isSnapped[child] = true;
            }
        }

        UpdateGrabbableState();
    }

    public void TriggerUnfold()
    {
        if (!isGroupModeActive) return;

        isExploded = !isExploded;

        if (!isExploded)
        {
            foreach (Transform child in foldedPositions.Keys)
            {
                isSnapped[child] = true;
            }
        }

        UpdateGrabbableState();
    }

    private void UpdateGrabbableState()
    {
        bool parentActive = isGroupModeActive && !isExploded;
        bool childrenActive = !isGroupModeActive || isExploded;

        if (parentGrab != null) parentGrab.enabled = parentActive;
        if (parentCollider != null) parentCollider.enabled = parentActive;

        foreach (Transform child in foldedPositions.Keys)
        {
            var grab = cachedGrabs[child];
            var col = child.GetComponent<Collider>();
            var rb = cachedRbs[child];

            if (grab != null) grab.enabled = childrenActive;
            if (col != null) col.enabled = childrenActive;
            if (rb != null)
            {
                rb.isKinematic = isComplexWeb ? true : !childrenActive;
            }
        }
    }

    void SetupChildPhysics(Transform part)
    {
        Collider[] cols = part.GetComponents<Collider>();
        if (cols.Length == 0)
        {
            MeshCollider mc = part.gameObject.AddComponent<MeshCollider>();
            mc.convex = !isComplexWeb;
            cols = new Collider[] { mc };
        }
        else
        {
            foreach (Collider c in cols)
            {
                c.enabled = true;
                if (c is MeshCollider meshCol) meshCol.convex = !isComplexWeb;
            }
        }

        var grab = part.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab == null) grab = part.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (grab.interactionManager == null)
        {
            grab.interactionManager = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
        }

        grab.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Kinematic;
        grab.useDynamicAttach = true;

        grab.colliders.Clear();
        foreach (Collider c in cols) grab.colliders.Add(c);

        var rb = part.GetComponent<Rigidbody>();
        if (rb == null) rb = part.gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.linearDamping = 2f;
        rb.angularDamping = 2f;

        // PERFORMANCE FIX: Save the components to memory dictionaries so Update() doesn't have to search for them
        cachedGrabs[part] = grab;
        cachedRbs[part] = rb;
    }

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
        grab.selectMode = UnityEngine.XR.Interaction.Toolkit.Interactables.InteractableSelectMode.Multiple;

        var transformer = grab.gameObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Transformers.XRGeneralGrabTransformer>();
        if (transformer == null)
        {
            transformer = grab.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Transformers.XRGeneralGrabTransformer>();
        }

        transformer.allowTwoHandedScaling = true;
        transformer.minimumScaleRatio = 0.2f;
        transformer.maximumScaleRatio = 5.0f;
    }
}