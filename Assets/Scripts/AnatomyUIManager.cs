using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class AnatomyUIManager : MonoBehaviour
{
    [System.Serializable]
    public class LayerMapping
    {
        [Tooltip("Exact Name of the Toggle in UI Builder (e.g., Toggle_Skeleton)")]
        public string uiToggleName;
        [Tooltip("The actual 3D model in your scene to hide/show")]
        public GameObject anatomyMesh;

        // We hide this from the Inspector, it's just for memory cleanup
        [HideInInspector] public Toggle linkedToggle;
    }

    [Header("UI & Layers")]
    public UIDocument uiDocument;
    public List<LayerMapping> anatomicalLayers;

    [Header("External Systems")]
    public NomenclatureSystem nomenclatureSystem; // Your label system

    // Internal State Trackers
    private Button btnExplode;
    private Button btnLabels;
    private Button btnReset;

    private bool isExploded = false;
    private bool areLabelsShowing = false;

    void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        VisualElement root = uiDocument.rootVisualElement;

        // 1. SETUP LEFT PANEL (TOGGLES)
        foreach (var layer in anatomicalLayers)
        {
            if (string.IsNullOrEmpty(layer.uiToggleName)) continue;

            Toggle toggle = root.Q<Toggle>(layer.uiToggleName);
            if (toggle != null && layer.anatomyMesh != null)
            {
                layer.linkedToggle = toggle;

                // Sync UI with current 3D model state without triggering an event
                toggle.SetValueWithoutNotify(layer.anatomyMesh.activeSelf);

                // Register the click event
                toggle.RegisterValueChangedCallback(evt => layer.anatomyMesh.SetActive(evt.newValue));
            }
            else
            {
                Debug.LogWarning($"Could not map: {layer.uiToggleName}");
            }
        }

        // 2. SETUP RIGHT PANEL (BUTTONS)
        btnExplode = root.Q<Button>("Btn_Explode");
        btnLabels = root.Q<Button>("Btn_Labels");
        btnReset = root.Q<Button>("Btn_Reset");

        if (btnExplode != null) btnExplode.clicked += ToggleExplodeMode;
        if (btnLabels != null) btnLabels.clicked += ToggleLabels;
        if (btnReset != null) btnReset.clicked += ResetAllSystems;

        // Find external systems if not linked
        if (nomenclatureSystem == null) nomenclatureSystem = FindFirstObjectByType<NomenclatureSystem>();
    }

    // CRITICAL: Unregister everything when panel turns off to prevent Red Errors!
    void OnDisable()
    {
        // Cleanup Toggles
        foreach (var layer in anatomicalLayers)
        {
            if (layer.linkedToggle != null)
            {
                layer.linkedToggle.UnregisterValueChangedCallback(evt => layer.anatomyMesh.SetActive(evt.newValue));
            }
        }

        // Cleanup Buttons
        if (btnExplode != null) btnExplode.clicked -= ToggleExplodeMode;
        if (btnLabels != null) btnLabels.clicked -= ToggleLabels;
        if (btnReset != null) btnReset.clicked -= ResetAllSystems;
    }

    // --- INTERACTION LOGIC ---

    private void ToggleExplodeMode()
    {
        isExploded = !isExploded;
        MagneticBody[] allBones = FindObjectsByType<MagneticBody>(FindObjectsSortMode.None);

        foreach (MagneticBody bone in allBones)
        {
            bone.SetGroupMode(isExploded);
        }

        btnExplode.text = isExploded ? "Reset Explode" : "Explode View";
    }

    private void ToggleLabels()
    {
        areLabelsShowing = !areLabelsShowing;

        if (nomenclatureSystem != null)
        {
            nomenclatureSystem.enabled = areLabelsShowing;
        }

        btnLabels.text = areLabelsShowing ? "Hide Labels" : "Labels";
    }

    private void ResetAllSystems()
    {
        // Reset Explode
        if (isExploded) ToggleExplodeMode();

        // Reset Labels
        if (areLabelsShowing) ToggleLabels();

        // Reset all layers to ON
        foreach (var layer in anatomicalLayers)
        {
            if (layer.anatomyMesh != null) layer.anatomyMesh.SetActive(true);
            if (layer.linkedToggle != null) layer.linkedToggle.SetValueWithoutNotify(true);
        }
    }
}
