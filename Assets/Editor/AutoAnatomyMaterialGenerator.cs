using UnityEngine;
using UnityEditor;

public class AutoAnatomyMaterialGenerator : EditorWindow
{
    public GameObject targetFolder;
    // The folder where the script will save the generated materials
    private string savePath = "Assets/Anatomy_Materials_Auto";

    [MenuItem("Tools/Smart Material Auto-Applier")]
    public static void ShowWindow()
    {
        GetWindow<AutoAnatomyMaterialGenerator>("Smart Material Gen");
    }

    void OnGUI()
    {
        GUILayout.Label("AI-Assisted Material Generator", EditorStyles.boldLabel);
        GUILayout.Label("Reads object names and auto-generates materials.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);

        targetFolder = (GameObject)EditorGUILayout.ObjectField("Target Parent Folder", targetFolder, typeof(GameObject), true);

        if (GUILayout.Button("Scan, Generate, & Apply", GUILayout.Height(40)))
        {
            if (targetFolder != null)
            {
                GenerateAndApply();
            }
            else
            {
                Debug.LogWarning("Please assign a Target Folder first!");
            }
        }
    }

    void GenerateAndApply()
    {
        // Create the folder to save materials if it doesn't exist
        if (!AssetDatabase.IsValidFolder(savePath))
        {
            AssetDatabase.CreateFolder("Assets", "Anatomy_Materials_Auto");
        }

        MeshRenderer[] renderers = targetFolder.GetComponentsInChildren<MeshRenderer>(true);
        int applyCount = 0;

        foreach (MeshRenderer r in renderers)
        {
            // Convert name to lowercase to make keyword searching easy
            string objName = r.gameObject.name.ToLower();

            // Send the name to our database logic
            Material mat = GetOrCreateMaterialForName(objName);

            if (mat != null)
            {
                Undo.RecordObject(r, "Apply Auto Material");
                Material[] newMats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < newMats.Length; i++) newMats[i] = mat;
                r.sharedMaterials = newMats;
                applyCount++;
            }
        }

        Debug.Log($"<color=#55aaff><b>SUCCESS!</b></color> Scanned {renderers.Length} objects. Auto-generated and applied materials to {applyCount} meshes.");
    }

    Material GetOrCreateMaterialForName(string name)
    {
        string matName = "";
        Color matColor = Color.white;
        float smoothness = 0.1f;

        // --- THE MEDICAL KEYWORD DATABASE ---
        if (name.Contains("liver") || name.Contains("hepar"))
        { matName = "Mat_Liver"; ColorUtility.TryParseHtmlString("#5B2C22", out matColor); smoothness = 0.4f; }
        else if (name.Contains("stomach") || name.Contains("gastric") || name.Contains("intestine") || name.Contains("colon"))
        { matName = "Mat_DigestiveTract"; ColorUtility.TryParseHtmlString("#D98880", out matColor); smoothness = 0.4f; }
        else if (name.Contains("lung") || name.Contains("pulmo"))
        { matName = "Mat_Lung"; ColorUtility.TryParseHtmlString("#E6B0AA", out matColor); smoothness = 0.05f; }
        else if (name.Contains("kidney") || name.Contains("renal"))
        { matName = "Mat_Kidney"; ColorUtility.TryParseHtmlString("#78281F", out matColor); smoothness = 0.3f; }
        else if (name.Contains("heart") || name.Contains("cardio"))
        { matName = "Mat_Heart"; ColorUtility.TryParseHtmlString("#8B2525", out matColor); smoothness = 0.25f; }
        else if (name.Contains("gallbladder"))
        { matName = "Mat_Gallbladder"; ColorUtility.TryParseHtmlString("#16A085", out matColor); smoothness = 0.5f; }
        else if (name.Contains("pancreas"))
        { matName = "Mat_Pancreas"; ColorUtility.TryParseHtmlString("#D4AC0D", out matColor); smoothness = 0.2f; }
        else if (name.Contains("brain") || name.Contains("encephalon") || name.Contains("cereb"))
        { matName = "Mat_Brain"; ColorUtility.TryParseHtmlString("#E2D9D1", out matColor); smoothness = 0.2f; }
        else if (name.Contains("cartilage") || name.Contains("bronch") || name.Contains("trachea"))
        { matName = "Mat_Cartilage"; ColorUtility.TryParseHtmlString("#A8CEDB", out matColor); smoothness = 0.5f; }
        else if (name.Contains("bone") || name.Contains("oste"))
        { matName = "Mat_Bone"; ColorUtility.TryParseHtmlString("#EAE6DF", out matColor); smoothness = 0.1f; }
        else if (name.Contains("vein") || name.Contains("vena"))
        { matName = "Mat_Vein"; ColorUtility.TryParseHtmlString("#2151D9", out matColor); smoothness = 0.3f; }
        else if (name.Contains("artery") || name.Contains("arteria") || name.Contains("aorta"))
        { matName = "Mat_Artery"; ColorUtility.TryParseHtmlString("#D92121", out matColor); smoothness = 0.3f; }
        else if (name.Contains("nerve") || name.Contains("nervus") || name.Contains("spinal cord"))
        { matName = "Mat_Nerve"; ColorUtility.TryParseHtmlString("#F4D03F", out matColor); smoothness = 0.2f; }
        else if (name.Contains("lymph") || name.Contains("spleen"))
        { matName = "Mat_Lymphoid"; ColorUtility.TryParseHtmlString("#2ECC71", out matColor); smoothness = 0.2f; }
        else if (name.Contains("muscle") || name.Contains("musculus") || name.Contains("diaphragm"))
        { matName = "Mat_Muscle"; ColorUtility.TryParseHtmlString("#A73A3A", out matColor); smoothness = 0.15f; }
        else
            return null; // If no keyword matches, skip this mesh!

        // Check if we already created this material in a previous run
        string assetPath = $"{savePath}/{matName}.mat";
        Material existingMat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (existingMat != null) return existingMat; // Reuse it to save memory!

        // If it doesn't exist, create it!
        // NOTE: If using URP, change "Standard" below to "Universal Render Pipeline/Simple Lit"
        // Change it to use the URP Lit shader instead!
        Material newMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        newMat.color = matColor;
        newMat.SetFloat("_Glossiness", smoothness);

        // AUTO-ENABLE GPU INSTANCING FOR VR PERFORMANCE!
        newMat.enableInstancing = true;

        // Save the material to the project folder
        AssetDatabase.CreateAsset(newMat, assetPath);
        return newMat;
    }
}