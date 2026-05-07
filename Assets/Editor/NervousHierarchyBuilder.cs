using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class NervousHierarchyBuilder : EditorWindow
{
    [MenuItem("Tools/Build AND Sort Nervous System (V8 Final Sweep)")]
    public static void BuildAndSortNervousSystem()
    {
        GameObject mainFolder = GameObject.Find("Nervous system & Sense organs");
        if (mainFolder == null)
        {
            Debug.LogError("Could not find the 'Nervous system & Sense organs' folder.");
            return;
        }

        string[] regions = {
            "Nervous_Central_Brain", "Nervous_Central_SpinalCord",
            "Nervous_Cranial_Nerves", "Nervous_PNS_Upper_Limb",
            "Nervous_PNS_Lower_Limb", "Nervous_PNS_Torso",
            "Sense_Organs_Eye", "Sense_Organs_Ear", "Nervous_Unsorted"
        };

        Dictionary<string, Transform> folderRefs = new Dictionary<string, Transform>();

        foreach (string region in regions)
        {
            Transform existingFolder = mainFolder.transform.Find(region);
            GameObject folder = existingFolder != null ? existingFolder.gameObject : new GameObject(region);

            folder.transform.SetParent(mainFolder.transform);
            folder.transform.localPosition = Vector3.zero;

            if (region != "Nervous_Unsorted" && folder.GetComponent<MagneticBody>() == null)
            {
                MagneticBody magBody = folder.AddComponent<MagneticBody>();
                if (region.Contains("PNS") || region.Contains("Cranial"))
                {
                    magBody.isComplexWeb = true;
                }
                else
                {
                    magBody.isComplexWeb = false;
                }
            }
            folderRefs[region] = folder.transform;
        }

        MeshFilter[] allMeshes = mainFolder.GetComponentsInChildren<MeshFilter>(true);
        int movedCount = 0;
        int unsortedCount = 0;

        foreach (MeshFilter mesh in allMeshes)
        {
            string name = mesh.gameObject.name.ToLower();

            if (name.StartsWith("nervous_") || name.StartsWith("sense_")) continue;

            string targetFolder = "";

            // --- CENTRAL NERVOUS SYSTEM (Brain & Brainstem) ---
            // ADDED: Plurals (gyri, sulci, nuclei), lingula, mamillary, olive, pole, optic chiasm, septum, stria, temporal plane, tentorium, cerebelli, tonsil, telencephalon, white matter
            if (name.Contains("gyrus") || name.Contains("gyri") || name.Contains("sulcus") || name.Contains("sulci") ||
                name.Contains("nucleus") || name.Contains("nuclei") || name.Contains("amygdaloid") || name.Contains("commissure") ||
                name.Contains("midbrain") || name.Contains("peduncle") || name.Contains("cerebr") || name.Contains("thalamus") ||
                name.Contains("pons") || name.Contains("medulla oblongata") || name.Contains("ventricle") ||
                name.Contains("colliculus") || name.Contains("putamen") || name.Contains("globus pallidus") ||
                name.Contains("hippocampus") || name.Contains("fornix") || name.Contains("corpus callosum") ||
                (name.Contains("capsule") && !name.Contains("joint")) || name.Contains("lobule") ||
                name.Contains("culmen") || name.Contains("cuneus") || name.Contains("declive") ||
                name.Contains("flocculus") || name.Contains("folium") || name.Contains("vermis") ||
                name.Contains("habenula") || name.Contains("fossa") || name.Contains("geniculate") || name.Contains("lat_fis") ||
                name.Contains("lingula") || name.Contains("mamillary") || name.Contains("olive") || name.Contains("pole") ||
                name.Contains("optic chiasm") || name.Contains("septum") || name.Contains("stria") || name.Contains("temporal plane") ||
                name.Contains("tentorium") || name.Contains("cerebelli") || name.Contains("tonsil") || name.Contains("telencephalon") ||
                name.Contains("white matter"))
            {
                targetFolder = "Nervous_Central_Brain";
            }
            // --- CENTRAL NERVOUS SYSTEM (Spinal Cord) ---
            // ADDED: intermediate substance, spinal dura, reticular process
            else if (name.Contains("tract") || name.Contains("horn") || name.Contains("fasciculus") ||
                     name.Contains("spinal cord") || name.Contains("funiculus") || name.Contains("lemniscus") ||
                     name.Contains("cauda equina") || name.Contains("central canal") || name.Contains("intermediate substance") ||
                     name.Contains("spinal dura") || name.Contains("reticular process"))
            {
                targetFolder = "Nervous_Central_SpinalCord";
            }
            // --- CRANIAL NERVES ---
            else if (name.Contains("(i)") || name.Contains("(ii)") || name.Contains("(iii)") || name.Contains("(iv)") ||
                     name.Contains("(v)") || name.Contains("(vi)") || name.Contains("(vii)") || name.Contains("(viii)") ||
                     name.Contains("(ix)") || name.Contains("(x)") || name.Contains("(xi)") || name.Contains("(xii)") ||
                     name.Contains("oculomotor") || name.Contains("trochlear") || name.Contains("trigeminal") ||
                     name.Contains("abducens") || name.Contains("facial nerve") || name.Contains("vestibulocochlear") ||
                     name.Contains("glossopharyngeal") || name.Contains("vagus") || name.Contains("accessory nerve") ||
                     name.Contains("hypoglossal") || name.Contains("mandibular") || name.Contains("maxillary") ||
                     name.Contains("ophthalmic nerve") || name.Contains("olfactory") || name.Contains("chorda tympani"))
            {
                targetFolder = "Nervous_Cranial_Nerves";
            }
            // --- PERIPHERAL NERVOUS SYSTEM (Upper Limb) ---
            else if (name.Contains("axillary") || name.Contains("radial nerve") || name.Contains("ulnar nerve") ||
                     name.Contains("median nerve") || name.Contains("musculocutaneous") || name.Contains("brachial plexus") ||
                     name.Contains("suprascapular") || name.Contains("pectoral nerve") || name.Contains("digital nerve") && (name.Contains("palmar") || name.Contains("hand")))
            {
                targetFolder = "Nervous_PNS_Upper_Limb";
            }
            // --- PERIPHERAL NERVOUS SYSTEM (Lower Limb) ---
            else if (name.Contains("femoral nerve") || name.Contains("sciatic nerve") || name.Contains("tibial nerve") ||
                     name.Contains("fibular nerve") || name.Contains("obturator nerve") || name.Contains("sural nerve") ||
                     name.Contains("plantar nerve") || name.Contains("sacral plexus") || name.Contains("gluteal nerve") ||
                     name.Contains("digital nerve") && (name.Contains("plantar") || name.Contains("foot")))
            {
                targetFolder = "Nervous_PNS_Lower_Limb";
            }
            // --- PERIPHERAL NERVOUS SYSTEM (Torso / Spinal Roots / Autonomic) ---
            else if (name.Contains("root of spinal nerve") || name.Contains("intercostal nerve") || name.Contains("sympathetic trunk") ||
                     name.Contains("splanchnic") || name.Contains("ganglion") || name.Contains("pudendal") ||
                     name.Contains("iliohypogastric") || name.Contains("ilioinguinal") || name.Contains("phrenic") || name.Contains("cervical plexus"))
            {
                targetFolder = "Nervous_PNS_Torso";
            }
            // --- SENSE ORGANS (Eye) ---
            // ADDED: vitreous
            else if (name.Contains("eyeball") || name.Contains("retina") || name.Contains("sclera") ||
                     name.Contains("cornea") || name.Contains("iris") || name.Contains("lens") ||
                     name.Contains("ciliary") || name.Contains("macula") || name.Contains("choroid") ||
                     name.Contains("zonular") || name.Contains("lacrimal") || name.Contains("vitreous"))
            {
                targetFolder = "Sense_Organs_Eye";
            }
            // --- SENSE ORGANS (Ear) ---
            else if (name.Contains("auditory") || name.Contains("cochlea") || name.Contains("tympanic") ||
                     name.Contains("vestibule") || name.Contains("semicircular") || name.Contains("auricle") || name.Contains("labyrinth"))
            {
                targetFolder = "Sense_Organs_Ear";
            }
            // --- SAFETY NET ---
            else
            {
                if (name.Contains("nerve"))
                {
                    targetFolder = "Nervous_PNS_Torso";
                }
                else
                {
                    targetFolder = "Nervous_Unsorted";
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
        Debug.Log($"<color=#34ebc9><b>SUCCESS!</b></color> V8 Polish sorted {movedCount} Neural meshes. {unsortedCount} went to Unsorted.");
    }
}