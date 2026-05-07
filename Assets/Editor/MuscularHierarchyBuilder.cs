using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MuscularHierarchyBuilder : EditorWindow
{
    [MenuItem("Tools/Build AND Sort Muscular System (V15 Absolute Zero)")]
    public static void BuildAndSortMuscles()
    {
        GameObject mainFolder = GameObject.Find("Muscular systems");
        if (mainFolder == null)
        {
            Debug.LogError("Could not find the 'Muscular systems' folder.");
            return;
        }

        string[] regions = {
            "Muscle_Head_and_Neck", "Muscle_Spine_and_Back", "Muscle_Thorax_and_Abdomen", "Muscle_Pelvis_and_Perineum",
            "Muscle_Upper_Limb_L", "Muscle_Upper_Limb_R", "Muscle_Lower_Limb_L", "Muscle_Lower_Limb_R",
            "Muscle_Unsorted"
        };

        Dictionary<string, Transform> folderRefs = new Dictionary<string, Transform>();

        foreach (string region in regions)
        {
            Transform existingFolder = mainFolder.transform.Find(region);
            GameObject folder = existingFolder != null ? existingFolder.gameObject : new GameObject(region);

            folder.transform.SetParent(mainFolder.transform);
            folder.transform.localPosition = Vector3.zero;

            if (region != "Muscle_Unsorted" && folder.GetComponent<MagneticBody>() == null)
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

            if (name.StartsWith("muscle_")) continue;

            bool isLeft = name.EndsWith(".l") || name.Contains("sinister") || name.Contains("left") || name.Contains("_l");
            bool isRight = name.EndsWith(".r") || name.Contains("dexter") || name.Contains("right") || name.Contains("_r");

            string targetFolder = "";

            // --- HEAD & NECK --- (Added: cervical fascia, temporal fascia, temporoparietalis)
            if (name.Contains("occipito") || name.Contains("frontalis") || name.Contains("temporalis") || name.Contains("masseter") ||
                name.Contains("pterygoid") || name.Contains("orbicularis") || name.Contains("buccinator") || name.Contains("bucinator") ||
                name.Contains("zygomaticus") || name.Contains("platysma") || name.Contains("sternocleidomastoid") || name.Contains("scalene") ||
                name.Contains("scalenus") || name.Contains("capitis") || name.Contains("colli") || name.Contains("hyoid") ||
                name.Contains("digastric") || name.Contains("constrictor") || name.Contains("auricular") || name.Contains("depressor") ||
                name.Contains("mentalis") || name.Contains("nasalis") || name.Contains("occipitalis") || name.Contains("procerus") ||
                name.Contains("levator anguli") || name.Contains("levator labii") || name.Contains("levator nasolabialis") ||
                name.Contains("corrugator") || name.Contains("epicranius") || name.Contains("galea aponeurotica") ||
                name.Contains("glossus") || name.Contains("arytenoid") || name.Contains("tarsus") || name.Contains("tendinous ring") ||
                name.Contains("inferior rectus") || name.Contains("superior rectus") || name.Contains("lateral rectus") || name.Contains("medial rectus") ||
                name.Contains("palpebrae") || name.Contains("pharyngeus") || name.Contains("risorius") || name.Contains("thyroid muscle") ||
                name.Contains("cricothyroid") || name.Contains("cervical fascia") || name.Contains("temporal fascia") || name.Contains("temporoparietalis"))
            {
                targetFolder = "Muscle_Head_and_Neck";
            }
            // --- SPINE / BACK --- 
            else if (name.Contains("trapezius") || name.Contains("latissimus") || name.Contains("rhomboid") || name.Contains("splenius") ||
                     name.Contains("erector spinae") || name.Contains("iliocostalis") || name.Contains("longissimus") || name.Contains("spinalis") ||
                     name.Contains("multifidus") || name.Contains("rotatores") || name.Contains("serratus posterior") || name.Contains("levator scapulae") ||
                     name.Contains("thoracolumbar") || name.Contains("intertransversarii") || name.Contains("interspinales"))
            {
                targetFolder = "Muscle_Spine_and_Back";
            }
            // --- THORAX & ABDOMEN --- (Added: transversalis)
            else if (name.Contains("pectoralis") || name.Contains("serratus anterior") || name.Contains("intercostal") || name.Contains("diaphragm") ||
                     name.Contains("rectus abdominis") || name.Contains("oblique") || name.Contains("transversus") || name.Contains("quadratus lumborum") ||
                     name.Contains("pyramidalis") || name.Contains("costarum") || name.Contains("subclavius") || name.Contains("linea alba") ||
                     name.Contains("cremaster") || name.Contains("clavipectoral") || name.Contains("abdominal fascia") || name.Contains("pectoral fascia") ||
                     name.Contains("transversalis"))
            {
                targetFolder = "Muscle_Thorax_and_Abdomen";
            }
            // --- PELVIC GIRDLE / PERINEUM --- 
            else if (name.Contains("levator ani") || name.Contains("coccygeus") || name.Contains("sphincter") || name.Contains("bulbospongiosus") ||
                     name.Contains("ischiocavernosus") || name.Contains("perineal") || name.Contains("dartos") || name.Contains("pubo-analis"))
            {
                targetFolder = "Muscle_Pelvis_and_Perineum";
            }
            // --- UPPER LIMB --- (Added: sheaths of digits of hand)
            else if (name.Contains("deltoid") || name.Contains("supraspinatus") || name.Contains("infraspinatus") || name.Contains("teres") ||
                     name.Contains("subscapularis") || name.Contains("biceps brachii") || name.Contains("brachialis") || name.Contains("brachial") ||
                     name.Contains("coracobrachialis") || name.Contains("coracobrachial") || name.Contains("triceps") || name.Contains("anconeus") ||
                     name.Contains("pronator") || name.Contains("flexor carpi") || name.Contains("palmaris") || name.Contains("flexor digitorum") ||
                     name.Contains("pollicis") || name.Contains("brachioradialis") || name.Contains("extensor carpi") || name.Contains("extensor digitorum") ||
                     name.Contains("supinator") || name.Contains("carpi") || name.Contains("common extensor tendon") || name.Contains("common flexor tendon") ||
                     name.Contains("extensor indicis") || name.Contains("extensor digiti minimi") || name.Contains("bicipito") || name.Contains("intertubercular") ||
                     (name.Contains("lumbrical") && name.Contains("hand")) || (name.Contains("interossei") && name.Contains("hand")) ||
                     name.Contains("opponens") || name.Contains("digiti minimi of hand") || name.Contains("antebrachial") ||
                     name.Contains("palmar aponeurosis") || name.Contains("fascia of hand") || name.Contains("septum of arm") ||
                     name.Contains("subacromial") || name.Contains("acromial bursa") || name.Contains("palmar interossei") || name.Contains("sheaths of digits of hand"))
            {
                targetFolder = isLeft ? "Muscle_Upper_Limb_L" : "Muscle_Upper_Limb_R";
                if (!isLeft && !isRight) targetFolder = "Muscle_Upper_Limb_R";
            }
            // --- LOWER LIMB --- (Added: tibia, prepatellar, trochanteric, suprapatellar, iliotibial)
            else if (name.Contains("iliacus") || name.Contains("psoas") || name.Contains("gluteus") || name.Contains("gluteal") ||
                     name.Contains("tensor fasciae") || name.Contains("piriformis") || name.Contains("obturator") || name.Contains("gemellus") ||
                     name.Contains("quadratus femoris") || name.Contains("sartorius") || name.Contains("quadriceps") || name.Contains("rectus femoris") ||
                     name.Contains("vastus") || name.Contains("pectineus") || name.Contains("adductor") || name.Contains("gracilis") ||
                     name.Contains("biceps femoris") || name.Contains("semitendinosus") || name.Contains("semimembranosus") || name.Contains("tibialis") ||
                     name.Contains("fibularis") || name.Contains("peroneus") || name.Contains("gastrocnemius") || name.Contains("soleus") ||
                     name.Contains("plantar") || name.Contains("popliteus") || name.Contains("hallucis") || name.Contains("digiti minimi of foot") ||
                     name.Contains("pedis") || name.Contains("calcaneal") || name.Contains("crural") || name.Contains("infrapatellar") ||
                     (name.Contains("lumbrical") && name.Contains("foot")) || (name.Contains("interossei") && name.Contains("foot")) ||
                     name.Contains("iliopectineal") || name.Contains("anserine") || name.Contains("fascia lata") || name.Contains("leg") ||
                     name.Contains("thigh") || name.Contains("femoral intermuscular") || name.Contains("popliteal fascia") ||
                     name.Contains("plantae") || name.Contains("malleolus") || name.Contains("tibia") || name.Contains("prepatellar") ||
                     name.Contains("trochanteric") || name.Contains("suprapatellar") || name.Contains("iliotibial"))
            {
                targetFolder = isLeft ? "Muscle_Lower_Limb_L" : "Muscle_Lower_Limb_R";
                if (!isLeft && !isRight) targetFolder = "Muscle_Lower_Limb_R";
            }
            // --- SAFETY NET ---
            else
            {
                targetFolder = "Muscle_Unsorted";
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
        Debug.Log($"<color=#ff5555><b>SUCCESS!</b></color> V15 Absolute Zero sorted {movedCount} Muscles/Fascia. {unsortedCount} remaining in Unsorted.");
    }
}