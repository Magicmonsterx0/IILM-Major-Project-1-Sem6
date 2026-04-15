using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LayerToggleManager : MonoBehaviour
{
    [System.Serializable]
    public class LayerMap
    {
        public string name;
        public GameObject anatomyLayer; //3D Model
        public Toggle uiToggle;         //UI Toggle
    }

    public List<LayerMap> layers; // Drag your 3 items here in Inspector

    void Start()
    {
        // Setup listeners automatically
        foreach (var layer in layers)
        {
            if (layer.uiToggle != null)
            {
                // 1. Set starting state (sync UI with 3D model)
                bool isInitiallyOn = layer.anatomyLayer.activeSelf;
                layer.uiToggle.isOn = isInitiallyOn;

                // 2. Listen for clicks
                layer.uiToggle.onValueChanged.AddListener((isOn) =>
                {
                    ToggleLayer(layer.anatomyLayer, isOn);
                });
            }
        }
    }

    void ToggleLayer(GameObject layerObject, bool state)
    {
        if (layerObject != null)
        {
            layerObject.SetActive(state);
        }
    }
}
