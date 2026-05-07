using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class JointsHierarchyBuilder : EditorWindow
{
    [MenuItem("Tools/Build AND Sort Joints (Z-Anatomy V2)")]
    public static void BuildAndSortJoints()
    {
        GameObject anatomyRoot = GameObject.Find("Anatomy");
        if (anatomyRoot == null)
        {
            Debug.LogError("Could not find the 'Anatomy' root folder.");
            return;
        }

        // 1. Create the Master Folder
        GameObject jointsMainFolder = new GameObject("Joints");
        jointsMainFolder.transform.SetParent(anatomyRoot.transform);
        jointsMainFolder.transform.localPosition = Vector3.zero;

        // 2. Define the TA2 Arthrology Regions
        string[] jointRegions = {
            "Joints_of_skull", "Vertebral_joints", "Thoracic_joints", "Joints_of_pelvic_girdle",
            "Joints_of_upper_limb_L", "Joints_of_upper_limb_R", "Joints_of_lower_limb_L", "Joints_of_lower_limb_R",
            "Joints_Unsorted" // Safety net for weird Latin naming exceptions
        };

        Dictionary<string, Transform> folderRefs = new Dictionary<string, Transform>();

        foreach (string region in jointRegions)
        {
            GameObject folder = new GameObject(region);
            folder.transform.SetParent(jointsMainFolder.transform);
            folder.transform.localPosition = Vector3.zero;

            // Only attach MagneticBody to actual grouped regions, not the unsorted bin
            if (region != "Joints_Unsorted")
            {
                MagneticBody magBody = folder.AddComponent<MagneticBody>();
                magBody.isComplexWeb = false;
            }

            folderRefs[region] = folder.transform;
        }

        // 3. Scan and sort all meshes
        MeshFilter[] allMeshes = anatomyRoot.GetComponentsInChildren<MeshFilter>(true);
        int movedCount = 0;
        int unsortedCount = 0;

        // Expanded TA2 Tissue Classifications
        string[] jointTissues = {
            "ligament", "capsul", "meniscus", "disc", "symphysis", "retinaculum",
            "labrum", "membran", "chorda", "syndesmosis", "sutur", "articular",
            "articulatio", "junctura", "annular", "cruciform", "aponeurosis"
        };

        foreach (MeshFilter mesh in allMeshes)
        {
            string name = mesh.gameObject.name.ToLower();

            // Check if it's actually a joint tissue
            bool isJoint = false;
            foreach (string tissue in jointTissues)
            {
                if (name.Contains(tissue)) { isJoint = true; break; }
            }
            if (!isJoint) continue;

            // Z-Anatomy L/R Identifiers (English & Latin)
            bool isLeft = name.EndsWith(".l") || name.Contains("sinister") || name.Contains("sinistra") || name.Contains("sinistrum") || name.EndsWith("_l");
            bool isRight = name.EndsWith(".r") || name.Contains("dexter") || name.Contains("dextra") || name.Contains("dextrum") || name.EndsWith("_r");

            string targetFolder = "";

            // --- LOWER LIMB (Juncturae membri inferioris) ---
            // Roots: Hip, Knee, Tibiofibular, Ankle, Foot
            if (name.Contains("femor") || name.Contains("tibi") || name.Contains("fibul") ||
                name.Contains("patella") || name.Contains("tarsal") || name.Contains("metatarsal") ||
                name.Contains("cuneo") || name.Contains("cuboid") || name.Contains("navicular") ||
                name.Contains("talo") || name.Contains("calcaneo") || name.Contains("plantar") ||
                name.Contains("cruciat") || name.Contains("coxae") || name.Contains("popliteal") ||
                name.Contains("bifurcat") || name.Contains("pedis") || name.Contains("acetabular") || name.Contains("malleol"))
            {
                targetFolder = isLeft ? "Joints_of_lower_limb_L" : "Joints_of_lower_limb_R";
                if (!isLeft && !isRight) targetFolder = "Joints_of_lower_limb_R";
            }
            // --- UPPER LIMB (Juncturae membri superioris) ---
            // Roots: Shoulder girdle, Elbow, Radioulnar, Wrist, Hand
            else if (name.Contains("humer") || name.Contains("radi") || name.Contains("ulnar") ||
                     name.Contains("carpal") || name.Contains("metacarpal") || name.Contains("gleno") ||
                     name.Contains("coraco") || name.Contains("acromio") || name.Contains("cubiti") ||
                     name.Contains("manus") || name.Contains("palmar") || name.Contains("trapez") ||
                     name.Contains("pisometacarpal") || name.Contains("clavic"))
            {
                targetFolder = isLeft ? "Joints_of_upper_limb_L" : "Joints_of_upper_limb_R";
                if (!isLeft && !isRight) targetFolder = "Joints_of_upper_limb_R";
            }
            // --- PELVIC GIRDLE (Juncturae cinguli pelvici) ---
            else if (name.Contains("sacroiliac") || name.Contains("pubic") || name.Contains("sacrotuberous") ||
                     name.Contains("sacrospinous") || name.Contains("obturator membrane"))
            {
                targetFolder = "Joints_of_pelvic_girdle";
            }
            // --- THORACIC (Juncturae thoracis) ---
            else if (name.Contains("costal") || name.Contains("stern") || name.Contains("chondro") ||
                     name.Contains("radiate") || name.Contains("xiphi"))
            {
                targetFolder = "Thoracic_joints";
            }
            // --- VERTEBRAL (Juncturae columnae vertebralis) ---
            else if (name.Contains("vertebral") || name.Contains("atlanto") || name.Contains("axial") ||
                     name.Contains("flavum") || name.Contains("nuchae") || name.Contains("interspinal") ||
                     name.Contains("supraspinal") || name.Contains("longitudinal") || name.Contains("sacrococcygeal") ||
                     name.Contains("tectoria") || name.Contains("zygapophysial"))
            {
                targetFolder = "Vertebral_joints";
            }
            // --- SKULL (Juncturae cranii) ---
            else if (name.Contains("temporomandibular") || name.Contains("cranial") || name.Contains("sutur") ||
                     name.Contains("spheno") || name.Contains("petro") || name.Contains("occipito") || name.Contains("stylo"))
            {
                targetFolder = "Joints_of_skull";
            }
            // --- SAFETY NET ---
            else
            {
                targetFolder = "Joints_Unsorted";
                unsortedCount++;
                Debug.LogWarning($"[Z-Anatomy Sorter] Could not determine region for: {mesh.gameObject.name}. Moved to Unsorted.");
            }

            // Move the mesh
            if (targetFolder != "")
            {
                mesh.transform.SetParent(folderRefs[targetFolder]);
                movedCount++;
            }
        }

        EditorGUIUtility.PingObject(jointsMainFolder);
        Debug.Log($"<color=green><b>SUCCESS!</b></color> Sorted {movedCount} joint tissues. {unsortedCount} were placed in 'Joints_Unsorted'.");
    }
}
