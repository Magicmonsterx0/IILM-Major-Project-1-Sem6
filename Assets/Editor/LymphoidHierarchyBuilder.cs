using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class LymphoidHierarchyBuilder : EditorWindow
{
    [MenuItem("Tools/Build AND Sort Lymphoid System")]
    public static void BuildAndSortLymphoid()
    {
        GameObject mainFolder = GameObject.Find("Lymphoid organs");
        if (mainFolder == null)
        {
            Debug.LogError("Could not find the 'Lymphoid organs' folder.");
            return;
        }

        // 1. Regional Clusters
        string[] regions = {
            "Lymphoid_Solid_Organs", "LymphNodes_Cervical", "LymphNodes_Axillary",
            "LymphNodes_Thoracic", "LymphNodes_Abdominal", "LymphNodes_Inguinal", "Lymphoid_Unsorted"
        };

        Dictionary<string, Transform> folderRefs = new Dictionary<string, Transform>();

        foreach (string region in regions)
        {
            Transform existingFolder = mainFolder.transform.Find(region);
            GameObject folder = existingFolder != null ? existingFolder.gameObject : new GameObject(region);

            folder.transform.SetParent(mainFolder.transform);
            folder.transform.localPosition = Vector3.zero;

            if (region != "Lymphoid_Unsorted" && folder.GetComponent<MagneticBody>() == null)
            {
                MagneticBody magBody = folder.AddComponent<MagneticBody>();
                // The solid organs are chunky. The node clusters act like webs.
                magBody.isComplexWeb = region != "Lymphoid_Solid_Organs";
            }
            folderRefs[region] = folder.transform;
        }

        MeshFilter[] allMeshes = mainFolder.GetComponentsInChildren<MeshFilter>(true);
        int movedCount = 0;
        int unsortedCount = 0;

        foreach (MeshFilter mesh in allMeshes)
        {
            string name = mesh.gameObject.name.ToLower();

            if (name.StartsWith("lymph")) continue; // Skip the parent folders themselves

            string targetFolder = "";

            // --- SOLID ORGANS ---
            if (name.Contains("spleen") || name.Contains("thymus") || name.Contains("tonsil") || name.Contains("waldeyer"))
            {
                targetFolder = "Lymphoid_Solid_Organs";
            }
            // --- CERVICAL (Head/Neck) NODES ---
            else if (name.Contains("cervical") || name.Contains("submandibular") || name.Contains("parotid") ||
                     name.Contains("mastoid") || name.Contains("occipital") || name.Contains("jugular") || name.Contains("facial"))
            {
                targetFolder = "LymphNodes_Cervical";
            }
            // --- AXILLARY (Arm/Armpit) NODES ---
            else if (name.Contains("axillary") || name.Contains("cubital") || name.Contains("brachial") ||
                     name.Contains("pectoral") || name.Contains("subscapular") || name.Contains("apical"))
            {
                targetFolder = "LymphNodes_Axillary";
            }
            // --- THORACIC (Chest) NODES ---
            else if (name.Contains("thoracic") || name.Contains("tracheobronchial") || name.Contains("bronchopulmonary") ||
                     name.Contains("mediastinal") || name.Contains("parasternal") || name.Contains("intercostal") || name.Contains("pulmonary"))
            {
                targetFolder = "LymphNodes_Thoracic";
            }
            // --- ABDOMINAL/PELVIC NODES ---
            else if (name.Contains("mesenteric") || name.Contains("celiac") || name.Contains("gastric") ||
                     name.Contains("hepatic") || name.Contains("pancreatic") || name.Contains("aortic") ||
                     name.Contains("iliac") || name.Contains("sacral") || name.Contains("lumbar") || name.Contains("colic"))
            {
                targetFolder = "LymphNodes_Abdominal";
            }
            // --- INGUINAL (Groin/Leg) NODES ---
            else if (name.Contains("inguinal") || name.Contains("popliteal") || name.Contains("femoral"))
            {
                targetFolder = "LymphNodes_Inguinal";
            }
            // --- SAFETY NET ---
            else
            {
                // If it's a generic node with no specific location tag, drop it in the abdomen as a catch-all
                if (name.Contains("node"))
                {
                    targetFolder = "LymphNodes_Abdominal";
                }
                else
                {
                    targetFolder = "Lymphoid_Unsorted";
                    unsortedCount++;
                }
            }

            if (targetFolder != "")
            {
                mesh.transform.SetParent(folderRefs[targetFolder]);
                movedCount++;
            }
        }

        EditorGUIUtility.PingObject(mainFolder);
        Debug.Log($"<color=#ff00ff><b>SUCCESS!</b></color> Sorted {movedCount} Lymphoid structures. {unsortedCount} went to Unsorted.");
    }
}
