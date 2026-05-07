using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class AnatomyCSVGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Anatomy CSV")]
    public static void GenerateCSV()
    {
        // Find the main Anatomy parent object in the scene
        GameObject anatomyRoot = GameObject.Find("Anatomy");

        if (anatomyRoot == null)
        {
            Debug.LogError("Could not find a GameObject named 'Anatomy' in the scene. Please make sure your root folder is named exactly 'Anatomy'.");
            return;
        }

        // Set up the file path in the main Assets folder
        string filePath = Path.Combine(Application.dataPath, "Auto_Master_Nomenclature.csv");
        StringBuilder csvContent = new StringBuilder();

        // Write the header matching your two-column format
        csvContent.AppendLine("Hierarchy_Name,Display_Name");

        // Keep track of names to avoid duplicates if you have repeated meshes
        HashSet<string> loggedNames = new HashSet<string>();

        // Get EVERY mesh filter under the Anatomy folder (ignoring empty group folders)
        MeshFilter[] allMeshes = anatomyRoot.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter mesh in allMeshes)
        {
            string rawName = mesh.gameObject.name;

            if (!loggedNames.Contains(rawName))
            {
                // Generate a clean display name
                string cleanName = GenerateDisplayName(rawName);

                // Add to CSV
                csvContent.AppendLine($"{rawName},{cleanName}");
                loggedNames.Add(rawName);
            }
        }

        // Save the file
        File.WriteAllText(filePath, csvContent.ToString());
        AssetDatabase.Refresh(); // Tells Unity to update the project folder

        Debug.Log($"SUCCESS! Generated CSV with {loggedNames.Count} anatomical parts at: {filePath}");
    }

    // Helper function to turn "Biceps brachii.l" into "Left Biceps Brachii"
    private static string GenerateDisplayName(string rawName)
    {
        string formattedName = rawName.Replace("_", " "); // Remove underscores

        // Handle Left/Right suffixes typical of Z-Anatomy
        if (formattedName.EndsWith(".l") || formattedName.EndsWith(".L"))
        {
            formattedName = "Left " + formattedName.Substring(0, formattedName.Length - 2);
        }
        else if (formattedName.EndsWith(".r") || formattedName.EndsWith(".R"))
        {
            formattedName = "Right " + formattedName.Substring(0, formattedName.Length - 2);
        }

        // Capitalize the first letter for UI cleanliness
        if (formattedName.Length > 0)
        {
            formattedName = char.ToUpper(formattedName[0]) + formattedName.Substring(1);
        }

        // Remove annoying bracket imports if present
        formattedName = formattedName.Replace("(", "").Replace(")", "");

        return formattedName;
    }
}
