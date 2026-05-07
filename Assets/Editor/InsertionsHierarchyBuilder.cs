using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class InsertionsHierarchyBuilder : EditorWindow
{
    [MenuItem("Tools/Build AND Sort Muscular Insertions (V5 Final)")]
    public static void BuildAndSortInsertions()
    {
        // 1. Find your EXISTING folder
        GameObject mainFolder = GameObject.Find("Muscular insertions");
        if (mainFolder == null)
        {
            Debug.LogError("Could not find the 'Muscular insertions' folder.");
            return;
        }

        // 2. Define the Regional Folders
        string[] regions = {
            "Insertions_Head_and_Neck", "Insertions_Spine", "Insertions_Thorax_and_Abdomen", "Insertions_Pelvis",
            "Insertions_Upper_Limb_L", "Insertions_Upper_Limb_R", "Insertions_Lower_Limb_L", "Insertions_Lower_Limb_R",
            "Insertions_Unsorted"
        };

        Dictionary<string, Transform> folderRefs = new Dictionary<string, Transform>();

        foreach (string region in regions)
        {
            Transform existingFolder = mainFolder.transform.Find(region);
            GameObject folder = existingFolder != null ? existingFolder.gameObject : new GameObject(region);

            folder.transform.SetParent(mainFolder.transform);
            folder.transform.localPosition = Vector3.zero;

            if (region != "Insertions_Unsorted" && folder.GetComponent<MagneticBody>() == null)
            {
                MagneticBody magBody = folder.AddComponent<MagneticBody>();
                magBody.isComplexWeb = false;
            }

            folderRefs[region] = folder.transform;
        }

        // 3. Scan ONLY inside the Muscular insertions folder
        MeshFilter[] allMeshes = mainFolder.GetComponentsInChildren<MeshFilter>(true);
        int movedCount = 0;
        int unsortedCount = 0;

        foreach (MeshFilter mesh in allMeshes)
        {
            string name = mesh.gameObject.name.ToLower();

            // Skip the regional folders themselves
            if (name.StartsWith("insertions_")) continue;

            bool isLeft = name.EndsWith("l") || name.Contains("sinister") || name.Contains(".ol") || name.Contains(".el") || name.Contains(".e1l") || name.Contains(".o1l");
            bool isRight = name.EndsWith("r") || name.Contains("dexter") || name.Contains(".or") || name.Contains(".er") || name.Contains(".e1r") || name.Contains(".o1r");

            string targetFolder = "";

            // --- HEAD & NECK --- (Added bucinator, scalenus, mentalis, nasalis, occipitalis, procerus, depressors, and facial levators)
            if (name.Contains("occipito") || name.Contains("frontalis") || name.Contains("temporalis") || name.Contains("masseter") ||
                name.Contains("pterygoid") || name.Contains("orbicularis") || name.Contains("buccinator") || name.Contains("bucinator") ||
                name.Contains("zygomaticus") || name.Contains("platysma") || name.Contains("sternocleidomastoid") || name.Contains("scalene") ||
                name.Contains("scalenus") || name.Contains("capitis") || name.Contains("colli") || name.Contains("hyoid") ||
                name.Contains("digastric") || name.Contains("constrictor") || name.Contains("auricular") || name.Contains("depressor") ||
                name.Contains("mentalis") || name.Contains("nasalis") || name.Contains("occipitalis") || name.Contains("procerus") ||
                name.Contains("levator anguli") || name.Contains("levator labii") || name.Contains("levator nasolabialis"))
            {
                targetFolder = "Insertions_Head_and_Neck";
            }
            // --- SPINE / BACK --- (Added levator scapulae)
            else if (name.Contains("trapezius") || name.Contains("latissimus") || name.Contains("rhomboid") || name.Contains("splenius") ||
                     name.Contains("erector spinae") || name.Contains("iliocostalis") || name.Contains("longissimus") || name.Contains("spinalis") ||
                     name.Contains("multifidus") || name.Contains("rotatores") || name.Contains("serratus posterior") || name.Contains("levator scapulae"))
            {
                targetFolder = "Insertions_Spine";
            }
            // --- THORAX & ABDOMEN --- (Added costarum for rib levators)
            else if (name.Contains("pectoralis") || name.Contains("serratus anterior") || name.Contains("intercostal") || name.Contains("diaphragm") ||
                     name.Contains("rectus abdominis") || name.Contains("oblique") || name.Contains("transversus") || name.Contains("quadratus lumborum") ||
                     name.Contains("pyramidalis") || name.Contains("costarum"))
            {
                targetFolder = "Insertions_Thorax_and_Abdomen";
            }
            // --- PELVIC GIRDLE / PERINEUM ---
            else if (name.Contains("levator ani") || name.Contains("coccygeus") || name.Contains("sphincter") || name.Contains("bulbospongiosus") ||
                     name.Contains("ischiocavernosus") || name.Contains("perineal"))
            {
                targetFolder = "Insertions_Pelvis";
            }
            // --- UPPER LIMB --- (Added common flexor/extensor tendons and extensor indicis)
            else if (name.Contains("deltoid") || name.Contains("supraspinatus") || name.Contains("infraspinatus") || name.Contains("teres") ||
                     name.Contains("subscapularis") || name.Contains("biceps brachii") || name.Contains("brachialis") || name.Contains("coracobrachialis") ||
                     name.Contains("triceps") || name.Contains("anconeus") || name.Contains("pronator") || name.Contains("flexor carpi") ||
                     name.Contains("palmaris") || name.Contains("flexor digitorum") || name.Contains("pollicis") || name.Contains("brachioradialis") ||
                     name.Contains("extensor carpi") || name.Contains("extensor digitorum") || name.Contains("supinator") || name.Contains("carpi") ||
                     name.Contains("common extensor tendon") || name.Contains("common flexor tendon") || name.Contains("extensor indicis") ||
                     (name.Contains("lumbrical") && name.Contains("hand")) || (name.Contains("interossei") && name.Contains("hand")) ||
                     name.Contains("opponens") || name.Contains("digiti minimi of hand"))
            {
                targetFolder = isLeft ? "Insertions_Upper_Limb_L" : "Insertions_Upper_Limb_R";
                if (!isLeft && !isRight) targetFolder = "Insertions_Upper_Limb_R";
            }
            // --- LOWER LIMB --- (Changed plantaris to plantar to catch the foot interossei)
            else if (name.Contains("iliacus") || name.Contains("psoas") || name.Contains("gluteus") || name.Contains("tensor fasciae") ||
                     name.Contains("piriformis") || name.Contains("obturator") || name.Contains("gemellus") || name.Contains("quadratus femoris") ||
                     name.Contains("sartorius") || name.Contains("quadriceps") || name.Contains("rectus femoris") || name.Contains("vastus") ||
                     name.Contains("pectineus") || name.Contains("adductor") || name.Contains("gracilis") || name.Contains("biceps femoris") ||
                     name.Contains("semitendinosus") || name.Contains("semimembranosus") || name.Contains("tibialis") || name.Contains("fibularis") ||
                     name.Contains("peroneus") || name.Contains("gastrocnemius") || name.Contains("soleus") || name.Contains("plantar") ||
                     name.Contains("popliteus") || name.Contains("hallucis") || name.Contains("digiti minimi of foot") || name.Contains("pedis") ||
                     (name.Contains("lumbrical") && name.Contains("foot")) || (name.Contains("interossei") && name.Contains("foot")))
            {
                targetFolder = isLeft ? "Insertions_Lower_Limb_L" : "Insertions_Lower_Limb_R";
                if (!isLeft && !isRight) targetFolder = "Insertions_Lower_Limb_R";
            }
            // --- SAFETY NET ---
            else
            {
                targetFolder = "Insertions_Unsorted";
                unsortedCount++;
            }

            // Move the mesh
            if (targetFolder != "")
            {
                mesh.transform.SetParent(folderRefs[targetFolder]);
                movedCount++;
            }
        }

        EditorGUIUtility.PingObject(mainFolder);
        Debug.Log($"<color=#34eb74><b>SUCCESS!</b></color> V5 Polish sorted {movedCount} Muscular Insertions. {unsortedCount} remaining in Unsorted.");
    }
}