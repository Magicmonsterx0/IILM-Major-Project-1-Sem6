using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class VisceralHierarchyBuilder : EditorWindow
{
    [MenuItem("Tools/Build AND Sort Visceral Systems")]
    public static void BuildAndSortViscera()
    {
        GameObject mainFolder = GameObject.Find("Visceral systems");
        if (mainFolder == null)
        {
            Debug.LogError("Could not find the 'Visceral systems' folder.");
            return;
        }

        // 1. The Five Major Visceral Systems
        string[] regions = {
            "Visceral_Digestive", "Visceral_Respiratory",
            "Visceral_Urinary", "Visceral_Reproductive",
            "Visceral_Endocrine", "Visceral_Unsorted"
        };

        Dictionary<string, Transform> folderRefs = new Dictionary<string, Transform>();

        foreach (string region in regions)
        {
            Transform existingFolder = mainFolder.transform.Find(region);
            GameObject folder = existingFolder != null ? existingFolder.gameObject : new GameObject(region);

            folder.transform.SetParent(mainFolder.transform);
            folder.transform.localPosition = Vector3.zero;

            if (region != "Visceral_Unsorted" && folder.GetComponent<MagneticBody>() == null)
            {
                MagneticBody magBody = folder.AddComponent<MagneticBody>();
                magBody.isComplexWeb = false; // Internal organs are solid chunks!
            }
            folderRefs[region] = folder.transform;
        }

        MeshFilter[] allMeshes = mainFolder.GetComponentsInChildren<MeshFilter>(true);
        int movedCount = 0;
        int unsortedCount = 0;

        foreach (MeshFilter mesh in allMeshes)
        {
            string name = mesh.gameObject.name.ToLower();

            if (name.StartsWith("visceral_")) continue;

            string targetFolder = "";

            // --- ENDOCRINE SYSTEM (Hormones/Glands) ---
            // Catch these first so they don't get confused with digestive/reproductive glands
            if (name.Contains("thyroid gland") || name.Contains("parathyroid") || name.Contains("adrenal") ||
                name.Contains("suprarenal") || name.Contains("pineal") || name.Contains("pituitary") ||
                name.Contains("hypophysis") || name.Contains("islets of langerhans"))
            {
                targetFolder = "Visceral_Endocrine";
            }
            // --- RESPIRATORY SYSTEM (Breathing) ---
            else if (name.Contains("lung") || name.Contains("bronch") || name.Contains("trachea") ||
                     name.Contains("larynx") || name.Contains("epiglottis") || name.Contains("pleura") ||
                     name.Contains("vocal") || name.Contains("cricoid") || name.Contains("arytenoid") ||
                     name.Contains("alveolar") || name.Contains("thyroid cartilage")) // cartilage is respiratory, gland is endocrine!
            {
                targetFolder = "Visceral_Respiratory";
            }
            // --- URINARY SYSTEM (Filtration) ---
            else if (name.Contains("kidney") || name.Contains("ureter") || name.Contains("bladder") ||
                     name.Contains("urethra") || name.Contains("renal") || name.Contains("calyx"))
            {
                targetFolder = "Visceral_Urinary";
            }
            // --- REPRODUCTIVE SYSTEM ---
            else if (name.Contains("penis") || name.Contains("testis") || name.Contains("epididymis") ||
                     name.Contains("ductus deferens") || name.Contains("ejaculatory") || name.Contains("prostate") ||
                     name.Contains("seminal vesicle") || name.Contains("ovary") || name.Contains("uterus") ||
                     name.Contains("vagina") || name.Contains("clitoris") || name.Contains("fallopian") ||
                     name.Contains("scrotum") || name.Contains("corpus cavernosum") || name.Contains("corpus spongiosum") ||
                     name.Contains("glans") || name.Contains("bulbourethral") || name.Contains("vestibular gland"))
            {
                targetFolder = "Visceral_Reproductive";
            }
            // --- DIGESTIVE SYSTEM (Alimentary) ---
            else if (name.Contains("stomach") || name.Contains("colon") || name.Contains("duodenum") ||
                     name.Contains("jejunum") || name.Contains("ileum") || name.Contains("cecum") ||
                     name.Contains("appendix") || name.Contains("rectum") || name.Contains("anus") ||
                     name.Contains("esophagus") || name.Contains("liver") || name.Contains("pancrea") ||
                     name.Contains("gallbladder") || name.Contains("bile") || name.Contains("parotid") ||
                     name.Contains("submandibular") || name.Contains("sublingual") || name.Contains("tooth") ||
                     name.Contains("teeth") || name.Contains("gingiva") || name.Contains("tongue") ||
                     name.Contains("palate") || name.Contains("pharynx") || name.Contains("omentum") ||
                     name.Contains("mesentery") || name.Contains("peritoneum") || name.Contains("taenia") ||
                     name.Contains("bowel") || name.Contains("intestine"))
            {
                targetFolder = "Visceral_Digestive";
            }
            // --- SAFETY NET ---
            else
            {
                targetFolder = "Visceral_Unsorted";
                unsortedCount++;
            }

            if (targetFolder != "")
            {
                mesh.transform.SetParent(folderRefs[targetFolder]);
                movedCount++;
            }
        }

        EditorGUIUtility.PingObject(mainFolder);
        Debug.Log($"<color=#ffaa00><b>SUCCESS!</b></color> Sorted {movedCount} Visceral organs. {unsortedCount} went to Unsorted.");
    }
}