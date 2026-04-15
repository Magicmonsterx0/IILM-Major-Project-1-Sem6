using UnityEngine;
using UnityEngine.InputSystem; // Needed for the New Input System

public class ToggleMenu : MonoBehaviour
{
    [Header("Settings")]
    public GameObject uiPanel; // Drag your UI_Anatomy_Menu here
    public InputActionProperty toggleButton; // We will set this to the "Menu" button
    public float distanceFromHead = 0.6f;

    [Header("Optional")]
    public bool centerOnOpen = true; // Should it snap to your face when opened?

    void Update()
    {
        // Check if the button was pressed this frame
        if (toggleButton.action != null && toggleButton.action.WasPressedThisFrame())
        {
            bool newState = !uiPanel.activeSelf;

            // 1. Toggle visibility
            uiPanel.SetActive(newState);

            // 2. If opening, snap it to front of face immediately
            if (newState && centerOnOpen)
            {
                SnapToFace();
            }
        }
    }

    void SnapToFace()
    {
        Transform head = Camera.main.transform;

        // Move to front
        uiPanel.transform.position = head.position + (head.forward * distanceFromHead);

        // Rotate to face user (Standard Billboard)
        uiPanel.transform.LookAt(uiPanel.transform.position + head.rotation * Vector3.forward,
                                 head.rotation * Vector3.up);
    }
}