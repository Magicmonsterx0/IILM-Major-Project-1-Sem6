using UnityEngine;
using UnityEngine.Profiling;

public class PerformanceHUD : MonoBehaviour
{
    private float deltaTime = 0.0f;

    void Update()
    {
        // Smooth out the delta time for a more stable FPS reading
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        // Set up the font size and color for the UI text
        int w = Screen.width, h = Screen.height;
        GUIStyle style = new GUIStyle();
        Rect rect = new Rect(20, 20, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 3 / 100; // Adjust this if the text is too big/small
        style.normal.textColor = Color.yellow; // Yellow shows up well against backgrounds

        // 1. Calculate FPS and Milliseconds
        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;

        // 2. Calculate Memory (Converted to Megabytes)
        long memoryMB = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);

        // 3. Count Active Colliders
        MeshCollider[] colliders = FindObjectsByType<MeshCollider>(FindObjectsSortMode.None);

        // Build the text string
        string text = string.Format("PERFORMANCE BENCHMARK\n" +
                                    "FPS: {0:0.} ({1:0.0} ms)\n" +
                                    "Allocated Memory: {2} MB\n" +
                                    "Active MeshColliders: {3}",
                                    fps, msec, memoryMB, colliders.Length);

        // Draw the text to the screen
        GUI.Label(rect, text, style);
    }
}
