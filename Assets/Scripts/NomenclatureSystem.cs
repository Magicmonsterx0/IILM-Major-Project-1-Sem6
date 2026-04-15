using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class NomenclatureSystem : MonoBehaviour
{
    [Header("The Database")]
    public TextAsset boneDatabaseCSV;

    [Header("The UI Elements")]
    public GameObject labelCanvas;
    public TextMeshProUGUI labelText;
    public LineRenderer pointerLine;

    [Header("VR Setup (Unity 6 Updated)")]
    // UNITY 6 FIX: We swapped the strict XR script for a standard Transform
    public Transform rightHandPointer;
    public float maxRayDistance = 10f;

    [Header("Dynamic Positioning")]
    public float outwardPush = 0.15f;
    public float pullTowardsPlayer = 0.1f;
    public float glideSpeed = 12f;

    private Dictionary<string, string> boneDictionary = new Dictionary<string, string>();
    private bool isLabelActive = false;

    void Start()
    {
        LoadDatabase();
        if (labelCanvas != null) labelCanvas.SetActive(false);
        if (pointerLine != null) pointerLine.enabled = false;
    }

    void LoadDatabase()
    {
        if (boneDatabaseCSV == null) return;
        string[] records = boneDatabaseCSV.text.Split('\n');
        foreach (string record in records)
        {
            string[] fields = record.Split(',');
            if (fields.Length >= 2)
            {
                string hierarchyName = fields[0].Trim();
                string properName = fields[1].Trim();
                if (!boneDictionary.ContainsKey(hierarchyName))
                {
                    boneDictionary.Add(hierarchyName, properName);
                }
            }
        }
    }

    void Update()
    {
        // UNITY 6 FIX: Using a standard Physics Raycast from the controller's forward direction
        if (rightHandPointer != null && Physics.Raycast(rightHandPointer.position, rightHandPointer.forward, out RaycastHit hit, maxRayDistance))
        {
            string hitObjectName = hit.collider.gameObject.name;
            if (boneDictionary.ContainsKey(hitObjectName))
            {
                UpdateDynamicLabel(hitObjectName, hit);
            }
            else HideLabel();
        }
        else HideLabel();
    }

    void UpdateDynamicLabel(string objectName, RaycastHit hit)
    {
        if (labelCanvas == null || pointerLine == null || labelText == null) return;

        Vector3 surfaceOutward = hit.normal * outwardPush;
        Vector3 towardsCamera = (Camera.main.transform.position - hit.point).normalized * pullTowardsPlayer;
        Vector3 targetPosition = hit.point + surfaceOutward + towardsCamera;

        labelText.text = boneDictionary[objectName];

        if (!isLabelActive)
        {
            labelCanvas.transform.position = targetPosition;
            labelCanvas.SetActive(true);
            pointerLine.enabled = true;
            isLabelActive = true;
        }
        else
        {
            labelCanvas.transform.position = Vector3.Lerp(labelCanvas.transform.position, targetPosition, Time.deltaTime * glideSpeed);
        }

        labelCanvas.transform.LookAt(labelCanvas.transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
        pointerLine.SetPosition(0, labelCanvas.transform.position);
        pointerLine.SetPosition(1, hit.point);
    }

    void HideLabel()
    {
        if (isLabelActive)
        {
            if (labelCanvas != null) labelCanvas.SetActive(false);
            if (pointerLine != null) pointerLine.enabled = false;
            isLabelActive = false;
        }
    }

    void OnDisable()
    {
        // When the UI turns this script off, force the label to hide immediately!
        HideLabel();
    }
}