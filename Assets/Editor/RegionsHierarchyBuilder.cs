using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RegionsHierarchyBuilder : EditorWindow
{
    [MenuItem("Tools/Build AND Sort Surface Regions (V13 Absolute Final)")]
    public static void BuildAndSortRegions()
    {
        GameObject mainFolder = GameObject.Find("Regions of human body");
        if (mainFolder == null)
        {
            Debug.LogError("Could not find the 'Regions of human body' folder.");
            return;
        }

        string[] regions = {
            "Region_Head_and_Neck", "Region_Thorax_and_Abdomen", "Region_Back", "Region_Pelvis_and_Perineum",
            "Region_Upper_Limb_L", "Region_Upper_Limb_R", "Region_Lower_Limb_L", "Region_Lower_Limb_R",
            "Region_Unsorted"
        };

        Dictionary<string, Transform> folderRefs = new Dictionary<string, Transform>();

        foreach (string region in regions)
        {
            Transform existingFolder = mainFolder.transform.Find(region);
            GameObject folder = existingFolder != null ? existingFolder.gameObject : new GameObject(region);

            folder.transform.SetParent(mainFolder.transform);
            folder.transform.localPosition = Vector3.zero;

            if (region != "Region_Unsorted" && folder.GetComponent<MagneticBody>() == null)
            {
                MagneticBody magBody = folder.AddComponent<MagneticBody>();
                magBody.isComplexWeb = false;
            }
            folderRefs[region] = folder.transform;
        }

        MeshFilter[] allMeshes = mainFolder.GetComponentsInChildren<MeshFilter>(true);
        int movedCount = 0;
        int unsortedCount = 0;

        foreach (MeshFilter mesh in allMeshes)
        {
            string name = mesh.gameObject.name.ToLower();

            if (name.StartsWith("region_")) continue;

            bool isLeft = name.EndsWith(".l") || name.Contains("sinister") || name.Contains("left");
            bool isRight = name.EndsWith(".r") || name.Contains("dexter") || name.Contains("right");

            string targetFolder = "";

            // --- HEAD & NECK --- (Added: parotideomasseteric, philtrum, scapha, tragus, triangular fossa)
            if (name.Contains("head") || name.Contains("face") || name.Contains("neck") || name.Contains("auricle") ||
                name.Contains("mouth") || name.Contains("buccal") || name.Contains("carotid") || name.Contains("antihelix") ||
                name.Contains("antitragus") || name.Contains("concha") || name.Contains("cranial") || name.Contains("cervical") ||
                name.Contains("orbital") || name.Contains("nasal") || name.Contains("oral") || name.Contains("mental") ||
                name.Contains("occipital") || name.Contains("parietal") || name.Contains("frontal") || name.Contains("temporal") ||
                name.Contains("zygomatic") || name.Contains("lip") || name.Contains("cheek") || name.Contains("chin") ||
                name.Contains("triangle") || name.Contains("auricular") || name.Contains("eminentia") || name.Contains("eyebrow") ||
                name.Contains("eyelash") || name.Contains("hair") || name.Contains("antihelica") || name.Contains("helix") ||
                name.Contains("intertragic") || name.Contains("labial") || name.Contains("mastoid") ||
                name.Contains("parotideomasseteric") || name.Contains("philtrum") || name.Contains("scapha") ||
                name.Contains("tragus") || name.Contains("triangular fossa"))
            {
                targetFolder = "Region_Head_and_Neck";
            }
            // --- UPPER LIMB --- (Added: palm, radial foveola, nail plate, perionyx)
            else if (name.Contains("arm") || name.Contains("elbow") || name.Contains("forearm") || name.Contains("wrist") ||
                     name.Contains("hand") || name.Contains("palmar") || name.Contains("carpal") || name.Contains("cubital") ||
                     name.Contains("deltoid") || name.Contains("brachial") || name.Contains("axillary") || name.Contains("thumb") ||
                     name.Contains("pollex") || name.Contains("finger") || name.Contains("acromial") || name.Contains("olecranon") ||
                     name.Contains("bicipital") || name.Contains("palm") || name.Contains("radial foveola") ||
                     name.Contains("nail plate") || name.Contains("perionyx"))
            {
                targetFolder = isLeft ? "Region_Upper_Limb_L" : "Region_Upper_Limb_R";
                if (!isLeft && !isRight) targetFolder = "Region_Upper_Limb_R";
            }
            // --- LOWER LIMB --- (Added: sole)
            else if (name.Contains("thigh") || name.Contains("knee") || name.Contains("leg") || name.Contains("ankle") ||
                     name.Contains("foot") || name.Contains("plantar") || name.Contains("femoral") || name.Contains("popliteal") ||
                     name.Contains("calcaneal") || name.Contains("tarsal") || name.Contains("sural") || name.Contains("crural") ||
                     name.Contains("patellar") || name.Contains("hallux") || name.Contains("toe") || name.Contains("gluteal") ||
                     name.Contains("calf") || name.Contains("hallucial") || name.Contains("heel") || name.Contains("hip") ||
                     name.Contains("malleol") || name.Contains("sole"))
            {
                targetFolder = isLeft ? "Region_Lower_Limb_L" : "Region_Lower_Limb_R";
                if (!isLeft && !isRight) targetFolder = "Region_Lower_Limb_R";
            }
            // --- PELVIS & PERINEUM ---
            else if (name.Contains("anal") || name.Contains("perineal") || name.Contains("pubic") || name.Contains("urogenital") ||
                     name.Contains("pelvic") || name.Contains("sacral") || name.Contains("coxal") || name.Contains("inguinal") || name.Contains("pudendal"))
            {
                targetFolder = "Region_Pelvis_and_Perineum";
            }
            // --- BACK ---
            else if (name.Contains("back") || name.Contains("dorsal") || name.Contains("vertebral") || name.Contains("scapular") || name.Contains("lumbar"))
            {
                targetFolder = "Region_Back";
            }
            // --- THORAX & ABDOMEN --- (Added: umbilicus)
            else if (name.Contains("thorax") || name.Contains("chest") || name.Contains("pectoral") || name.Contains("abdomen") ||
                     name.Contains("abdominal") || name.Contains("umbilical") || name.Contains("epigastric") || name.Contains("sternal") ||
                     name.Contains("clavicular") || name.Contains("costal") || name.Contains("hypochondriac") || name.Contains("flank") ||
                     name.Contains("iliac") || name.Contains("trunk") || name.Contains("hypogastric") || name.Contains("mammary") ||
                     name.Contains("umbilicus"))
            {
                targetFolder = "Region_Thorax_and_Abdomen";
            }
            // --- SAFETY NET ---
            else
            {
                targetFolder = "Region_Unsorted";
                unsortedCount++;
            }

            if (targetFolder != "")
            {
                mesh.transform.SetParent(folderRefs[targetFolder]);
                movedCount++;
            }
        }

        EditorGUIUtility.PingObject(mainFolder);
        Debug.Log($"<color=#55aaff><b>SUCCESS!</b></color> V13 Polish sorted {movedCount} Surface Regions. {unsortedCount} remaining in Unsorted.");
    }
}