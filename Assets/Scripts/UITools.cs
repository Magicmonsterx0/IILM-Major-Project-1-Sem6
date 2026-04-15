using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class UITools : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("System References")]
    public MasterTurntable masterTurntable;
    public NomenclatureSystem nomenclatureSystem;

    [Tooltip("Drag the 'DynamicLabel' GameObject from your Hierarchy here!")]
    public GameObject dynamicLabelContainer;

    public List<MagneticBody> allBoneGroups;

    // UI Element Trackers
    private Button btnDissect;
    private Button btnInspect;
    private Button btnLabels;
    private Button btnReset;

    // Internal State Trackers
    private bool isDissectModeOn = false;
    private bool isInspectModeOn = false;
    private bool areLabelsOn = false;

    void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        VisualElement root = uiDocument.rootVisualElement;

        btnDissect = root.Q<Button>("Btn_ExplodeView");
        btnInspect = root.Q<Button>("Btn_InspectMode");
        btnLabels = root.Q<Button>("Btn_Labels");
        btnReset = root.Q<Button>("Btn_Reset");

        if (btnDissect != null) btnDissect.clicked += OnDissectClicked;
        if (btnInspect != null) btnInspect.clicked += OnInspectClicked;
        if (btnLabels != null) btnLabels.clicked += OnLabelsClicked;
        if (btnReset != null) btnReset.clicked += OnResetClicked;

        if (masterTurntable == null) masterTurntable = FindFirstObjectByType<MasterTurntable>();
        if (nomenclatureSystem == null) nomenclatureSystem = FindFirstObjectByType<NomenclatureSystem>();

        if (btnDissect != null) btnDissect.text = isDissectModeOn ? "Ungroup Bones" : "Explode View";
        if (btnInspect != null) btnInspect.text = isInspectModeOn ? "Exit Inspect" : "Inspect Mode";
        if (btnLabels != null) btnLabels.text = areLabelsOn ? "Hide Labels" : "Labels";
    }

    void OnDisable()
    {
        if (btnDissect != null) btnDissect.clicked -= OnDissectClicked;
        if (btnInspect != null) btnInspect.clicked -= OnInspectClicked;
        if (btnLabels != null) btnLabels.clicked -= OnLabelsClicked;
        if (btnReset != null) btnReset.clicked -= OnResetClicked;
    }

    private void OnDissectClicked()
    {
        isDissectModeOn = !isDissectModeOn;

        foreach (MagneticBody group in allBoneGroups)
        {
            if (group != null) group.SetGroupMode(isDissectModeOn);
        }

        btnDissect.text = isDissectModeOn ? "Ungroup Bones" : "Explode View";

        if (isDissectModeOn && isInspectModeOn) OnInspectClicked();
    }

    private void OnInspectClicked()
    {
        isInspectModeOn = !isInspectModeOn;

        if (masterTurntable != null)
        {
            masterTurntable.ToggleInspectMode(isInspectModeOn);
        }

        btnInspect.text = isInspectModeOn ? "Exit Inspect" : "Inspect Mode";

        if (isInspectModeOn && isDissectModeOn) OnDissectClicked();
    }

    private void OnLabelsClicked()
    {
        areLabelsOn = !areLabelsOn;

        // 1. Turn the script on/off
        if (nomenclatureSystem != null)
        {
            nomenclatureSystem.enabled = areLabelsOn;
        }

        // 2. NEW FIX: Physically hide the floating UI canvases!
        if (dynamicLabelContainer != null)
        {
            dynamicLabelContainer.SetActive(areLabelsOn);
        }

        btnLabels.text = areLabelsOn ? "Hide Labels" : "Labels";
    }

    private void OnResetClicked()
    {
        // 1. Snap the skeleton back FIRST! 
        // We do this immediately so the bones are in their correct spots before modes change.
        if (masterTurntable != null)
        {
            masterTurntable.ResetToOrigin();
        }

        // 2. NOW it is safe to turn off the toggles and unfreeze the bones
        if (isDissectModeOn) OnDissectClicked();
        if (isInspectModeOn) OnInspectClicked();
        if (areLabelsOn) OnLabelsClicked();
    }
}