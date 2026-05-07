using UnityEngine;
using UnityEditor;

public class BulkMaterialApplier : EditorWindow
{
    public Material targetMaterial;
    public GameObject targetFolder;

    [MenuItem("Tools/Bulk Material Applier")]
    public static void ShowWindow()
    {
        GetWindow<BulkMaterialApplier>("Bulk Material Tool");
    }

    void OnGUI()
    {
        GUILayout.Label("Assign Material to All Children", EditorStyles.boldLabel);

        targetFolder = (GameObject)EditorGUILayout.ObjectField("Target Folder", targetFolder, typeof(GameObject), true);
        targetMaterial = (Material)EditorGUILayout.ObjectField("Material to Apply", targetMaterial, typeof(Material), false);

        if (GUILayout.Button("Apply Material"))
        {
            if (targetMaterial != null && targetFolder != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(targetFolder, "Apply Materials");

                MeshRenderer[] renderers = targetFolder.GetComponentsInChildren<MeshRenderer>(true);
                int count = 0;

                foreach (MeshRenderer r in renderers)
                {
                    // Create an array of the exact same size as the current materials array
                    Material[] newMaterials = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < newMaterials.Length; i++)
                    {
                        newMaterials[i] = targetMaterial; // Overwrite with new material
                    }
                    r.sharedMaterials = newMaterials;
                    count++;
                }

                Debug.Log($"<color=#55aaff><b>SUCCESS!</b></color> Applied {targetMaterial.name} to {count} meshes inside {targetFolder.name}.");
            }
            else
            {
                Debug.LogWarning("Please assign both a Folder and a Material.");
            }
        }
    }
}