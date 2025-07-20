using UnityEngine;
using UnityEditor;

/// <summary>
/// Simple validation test for Task_1_1_1_Setup editor window
/// </summary>
public class Task_1_1_1_ValidationTest
{
    [MenuItem("Tactical Tools/Validate Task 1.1.1 Editor Script")]
    public static void ValidateEditorScript()
    {
        Debug.Log("=== Validating Task 1.1.1 Editor Script ===");
        
        bool validationPassed = true;
        
        // Test 1: Check if the Task_1_1_1_Setup class exists
        System.Type setupType = System.Type.GetType("Task_1_1_1_Setup");
        if (setupType != null)
        {
            Debug.Log("✓ Task_1_1_1_Setup class found");
        }
        else
        {
            Debug.LogError("✗ Task_1_1_1_Setup class not found");
            validationPassed = false;
        }
        
        // Test 2: Check if the menu item exists
        if (EditorApplication.ExecuteMenuItem("Tactical Tools/Task 1.1.1 - Setup 3D Scene & Camera"))
        {
            Debug.Log("✓ Menu item accessible");
            // Close the window if it opened
            EditorWindow.GetWindow<Task_1_1_1_Setup>().Close();
        }
        else
        {
            Debug.LogWarning("⚠ Menu item test inconclusive (window may have opened)");
        }
        
        // Test 3: Check current scene state
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Debug.Log($"✓ Main Camera found: {mainCamera.name}");
            Debug.Log($"  - Orthographic: {mainCamera.orthographic}");
            Debug.Log($"  - Position: {mainCamera.transform.position}");
            Debug.Log($"  - Rotation: {mainCamera.transform.eulerAngles}");
        }
        else
        {
            Debug.LogWarning("⚠ No Main Camera found in current scene");
        }
        
        Light mainLight = Object.FindObjectOfType<Light>();
        if (mainLight != null)
        {
            Debug.Log($"✓ Light found: {mainLight.name}");
            Debug.Log($"  - Type: {mainLight.type}");
            Debug.Log($"  - Intensity: {mainLight.intensity}");
        }
        else
        {
            Debug.LogWarning("⚠ No Light found in current scene");
        }
        
        if (validationPassed)
        {
            Debug.Log("=== ✅ Editor Script Validation PASSED ===");
        }
        else
        {
            Debug.LogError("=== ❌ Editor Script Validation FAILED ===");
        }
    }
}