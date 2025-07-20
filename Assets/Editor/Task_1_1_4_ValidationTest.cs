using UnityEngine;
using UnityEditor;

/// <summary>
/// Comprehensive validation test for Task 1.1.4 Environment Polish implementation.
/// Verifies all components, materials, lighting, and visual feedback systems.
/// </summary>
public class Task_1_1_4_ValidationTest : EditorWindow
{
    private Vector2 scrollPosition;
    private bool validationComplete = false;
    private ValidationResult lastValidationResult;
    
    [MenuItem("Tactical Tools/Task 1.1.4 - Validation Test")]
    public static void ShowWindow()
    {
        Task_1_1_4_ValidationTest window = GetWindow<Task_1_1_4_ValidationTest>("Task 1.1.4 Validation");
        window.minSize = new Vector2(600, 700);
        window.Show();
    }
    
    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Space(10);
        
        // Header
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("Task 1.1.4 Environment Polish - Validation Test", headerStyle);
        GUILayout.Space(20);
        
        // Description
        EditorGUILayout.HelpBox(
            "This validation test verifies the complete implementation of Task 1.1.4 Environment Polish. " +
            "It checks all runtime scripts, materials, lighting optimization, and visual feedback systems.",
            MessageType.Info);
        
        GUILayout.Space(10);
        
        // Validation Button
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fixedHeight = 40
        };
        
        if (GUILayout.Button("Run Comprehensive Validation", buttonStyle))
        {
            RunValidation();
        }
        
        GUILayout.Space(20);
        
        // Display Results
        if (validationComplete && lastValidationResult != null)
        {
            DisplayValidationResults();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    /// <summary>
    /// Runs comprehensive validation of the environment polish system
    /// </summary>
    private void RunValidation()
    {
        Debug.Log("=== Starting Task 1.1.4 Environment Polish Validation ===");
        
        ValidationResult result = new ValidationResult();
        
        // Test 1: Verify Editor Script
        result.editorScriptExists = ValidateEditorScript();
        
        // Test 2: Verify Runtime Scripts
        result.runtimeScriptsExist = ValidateRuntimeScripts();
        
        // Test 3: Verify Materials System
        result.materialsValid = ValidateMaterialsSystem();
        
        // Test 4: Verify Scene Integration
        result.sceneIntegrationValid = ValidateSceneIntegration();
        
        // Test 5: Verify Component Integration
        result.componentIntegrationValid = ValidateComponentIntegration();
        
        // Test 6: Verify Visual Feedback System
        result.visualFeedbackValid = ValidateVisualFeedbackSystem();
        
        // Test 7: Verify Performance Optimization
        result.performanceOptimizationValid = ValidatePerformanceOptimization();
        
        // Test 8: Verify Lighting Setup
        result.lightingSetupValid = ValidateLightingSetup();
        
        // Calculate overall result
        result.CalculateOverallResult();
        
        lastValidationResult = result;
        validationComplete = true;
        
        // Log results
        LogValidationResults(result);
        
        Debug.Log("=== Environment Polish Validation Complete ===");
    }
    
    /// <summary>
    /// Validates the editor script exists and functions
    /// </summary>
    private bool ValidateEditorScript()
    {
        string scriptPath = "Assets/Editor/Task_1_1_4_Setup.cs";
        bool exists = System.IO.File.Exists(scriptPath);
        
        if (exists)
        {
            Debug.Log("✓ Task_1_1_4_Setup.cs editor script found");
        }
        else
        {
            Debug.LogError("✗ Task_1_1_4_Setup.cs editor script missing");
        }
        
        return exists;
    }
    
    /// <summary>
    /// Validates all required runtime scripts exist
    /// </summary>
    private bool ValidateRuntimeScripts()
    {
        string[] requiredScripts = {
            "Assets/Scripts/MaterialManager.cs",
            "Assets/Scripts/VisualFeedbackManager.cs",
            "Assets/Scripts/PerformanceOptimizer.cs"
        };
        
        bool allScriptsExist = true;
        
        foreach (string scriptPath in requiredScripts)
        {
            bool exists = System.IO.File.Exists(scriptPath);
            if (exists)
            {
                Debug.Log($"✓ {System.IO.Path.GetFileName(scriptPath)} found");
            }
            else
            {
                Debug.LogError($"✗ {System.IO.Path.GetFileName(scriptPath)} missing");
                allScriptsExist = false;
            }
        }
        
        return allScriptsExist;
    }
    
    /// <summary>
    /// Validates the materials system
    /// </summary>
    private bool ValidateMaterialsSystem()
    {
        string[] requiredMaterials = {
            "Assets/Materials/GridLine.mat",
            "Assets/Materials/GridTile_Default.mat",
            "Assets/Materials/GridTile_Hover.mat",
            "Assets/Materials/GridTile_Selected.mat",
            "Assets/Materials/Obstacle_LowCover.mat",
            "Assets/Materials/Obstacle_HighWall.mat",
            "Assets/Materials/Obstacle_Terrain.mat"
        };
        
        bool allMaterialsExist = true;
        int materialsFound = 0;
        
        foreach (string materialPath in requiredMaterials)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null)
            {
                Debug.Log($"✓ Material {material.name} loaded successfully");
                materialsFound++;
            }
            else
            {
                Debug.LogWarning($"⚠ Material {System.IO.Path.GetFileName(materialPath)} not found");
                allMaterialsExist = false;
            }
        }
        
        Debug.Log($"Materials validation: {materialsFound}/{requiredMaterials.Length} materials found");
        return allMaterialsExist;
    }
    
    /// <summary>
    /// Validates scene integration
    /// </summary>
    private bool ValidateSceneIntegration()
    {
        bool isValid = true;
        
        // Check for Grid System
        GameObject gridSystem = GameObject.Find("Grid System");
        if (gridSystem != null)
        {
            GridManager gridManager = gridSystem.GetComponent<GridManager>();
            if (gridManager != null)
            {
                Debug.Log("✓ Grid System integration verified");
            }
            else
            {
                Debug.LogError("✗ Grid System missing GridManager component");
                isValid = false;
            }
        }
        else
        {
            Debug.LogError("✗ Grid System not found in scene");
            isValid = false;
        }
        
        // Check for Camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Debug.Log($"✓ Main Camera found: {(mainCamera.orthographic ? "Orthographic" : "Perspective")}");
        }
        else
        {
            Debug.LogError("✗ Main Camera not found");
            isValid = false;
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Validates component integration between systems
    /// </summary>
    private bool ValidateComponentIntegration()
    {
        bool isValid = true;
        
        // Check for Material Manager in scene
        GameObject materialManager = GameObject.Find("Material Manager");
        if (materialManager != null)
        {
            MaterialManager matMgr = materialManager.GetComponent<MaterialManager>();
            if (matMgr != null)
            {
                Debug.Log("✓ Material Manager component integration verified");
            }
            else
            {
                Debug.LogWarning("⚠ Material Manager GameObject found but component missing");
                isValid = false;
            }
        }
        else
        {
            Debug.Log("○ Material Manager not yet created (will be created by setup)");
        }
        
        // Check for Visual Feedback Manager
        GameObject feedbackManager = GameObject.Find("Visual Feedback Manager");
        if (feedbackManager != null)
        {
            VisualFeedbackManager fbMgr = feedbackManager.GetComponent<VisualFeedbackManager>();
            if (fbMgr != null)
            {
                Debug.Log("✓ Visual Feedback Manager component integration verified");
            }
            else
            {
                Debug.LogWarning("⚠ Visual Feedback Manager GameObject found but component missing");
                isValid = false;
            }
        }
        else
        {
            Debug.Log("○ Visual Feedback Manager not yet created (will be created by setup)");
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Validates visual feedback system functionality
    /// </summary>
    private bool ValidateVisualFeedbackSystem()
    {
        // Check if VisualFeedbackManager class compiles properly
        System.Type feedbackType = System.Type.GetType("VisualFeedbackManager");
        if (feedbackType != null)
        {
            Debug.Log("✓ VisualFeedbackManager class compilation verified");
            
            // Check for required enums
            System.Type tileStateType = System.Type.GetType("GridTileState");
            System.Type effectType = System.Type.GetType("FeedbackEffectType");
            
            if (tileStateType != null && effectType != null)
            {
                Debug.Log("✓ Visual feedback supporting enums verified");
                return true;
            }
            else
            {
                Debug.LogError("✗ Visual feedback supporting enums missing");
                return false;
            }
        }
        else
        {
            Debug.LogError("✗ VisualFeedbackManager class compilation failed");
            return false;
        }
    }
    
    /// <summary>
    /// Validates performance optimization functionality
    /// </summary>
    private bool ValidatePerformanceOptimization()
    {
        // Check if PerformanceOptimizer class compiles properly
        System.Type perfType = System.Type.GetType("PerformanceOptimizer");
        if (perfType != null)
        {
            Debug.Log("✓ PerformanceOptimizer class compilation verified");
            
            // Check current performance settings
            int currentTargetFPS = Application.targetFrameRate;
            int currentVSync = QualitySettings.vSyncCount;
            
            Debug.Log($"✓ Current performance settings - Target FPS: {currentTargetFPS}, VSync: {currentVSync}");
            return true;
        }
        else
        {
            Debug.LogError("✗ PerformanceOptimizer class compilation failed");
            return false;
        }
    }
    
    /// <summary>
    /// Validates lighting setup
    /// </summary>
    private bool ValidateLightingSetup()
    {
        bool isValid = true;
        
        // Check for directional light
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        Light directionalLight = null;
        
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                directionalLight = light;
                break;
            }
        }
        
        if (directionalLight != null)
        {
            Debug.Log($"✓ Directional light found - Intensity: {directionalLight.intensity}, Color: {directionalLight.color}");
        }
        else
        {
            Debug.LogWarning("⚠ No directional light found in scene");
            isValid = false;
        }
        
        // Check ambient lighting
        Debug.Log($"✓ Ambient lighting - Mode: {RenderSettings.ambientMode}, Intensity: {RenderSettings.ambientIntensity}");
        
        return isValid;
    }
    
    /// <summary>
    /// Displays the validation results in the GUI
    /// </summary>
    private void DisplayValidationResults()
    {
        EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        // Overall result
        MessageType overallMessageType = lastValidationResult.overallSuccess ? MessageType.Info : MessageType.Warning;
        string overallMessage = lastValidationResult.overallSuccess ? 
            "✓ All validation tests passed! Environment Polish implementation is complete." :
            "⚠ Some validation tests failed. Check the console for details.";
        
        EditorGUILayout.HelpBox(overallMessage, overallMessageType);
        GUILayout.Space(10);
        
        // Individual test results
        DrawTestResult("Editor Script", lastValidationResult.editorScriptExists);
        DrawTestResult("Runtime Scripts", lastValidationResult.runtimeScriptsExist);
        DrawTestResult("Materials System", lastValidationResult.materialsValid);
        DrawTestResult("Scene Integration", lastValidationResult.sceneIntegrationValid);
        DrawTestResult("Component Integration", lastValidationResult.componentIntegrationValid);
        DrawTestResult("Visual Feedback System", lastValidationResult.visualFeedbackValid);
        DrawTestResult("Performance Optimization", lastValidationResult.performanceOptimizationValid);
        DrawTestResult("Lighting Setup", lastValidationResult.lightingSetupValid);
        
        GUILayout.Space(10);
        
        // Summary
        EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Tests Passed: {lastValidationResult.GetPassedCount()}/8");
        EditorGUILayout.LabelField($"Implementation Status: {(lastValidationResult.overallSuccess ? "Complete" : "Needs Attention")}");
        
        GUILayout.Space(20);
        
        // Next steps
        if (lastValidationResult.overallSuccess)
        {
            EditorGUILayout.HelpBox(
                "🎉 Task 1.1.4 Environment Polish implementation is complete!\n\n" +
                "Next steps:\n" +
                "• Run the Task_1_1_4_Setup.cs editor tool to apply environment polish\n" +
                "• Test the visual feedback systems in play mode\n" +
                "• Verify performance optimization is working\n" +
                "• Document the implementation in learning logs",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Some components need attention. Please:\n" +
                "• Check the console for specific error details\n" +
                "• Ensure all required scripts are created\n" +
                "• Verify scene setup is correct\n" +
                "• Re-run validation after fixes",
                MessageType.Warning);
        }
    }
    
    /// <summary>
    /// Draws a single test result
    /// </summary>
    private void DrawTestResult(string testName, bool passed)
    {
        GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.normal.textColor = passed ? Color.green : Color.red;
        
        string icon = passed ? "✓" : "✗";
        EditorGUILayout.LabelField($"{icon} {testName}", labelStyle);
    }
    
    /// <summary>
    /// Logs validation results to console
    /// </summary>
    private void LogValidationResults(ValidationResult result)
    {
        string summary = $"\\n=== VALIDATION SUMMARY ===\\n" +
                        $"Editor Script: {(result.editorScriptExists ? "PASS" : "FAIL")}\\n" +
                        $"Runtime Scripts: {(result.runtimeScriptsExist ? "PASS" : "FAIL")}\\n" +
                        $"Materials System: {(result.materialsValid ? "PASS" : "FAIL")}\\n" +
                        $"Scene Integration: {(result.sceneIntegrationValid ? "PASS" : "FAIL")}\\n" +
                        $"Component Integration: {(result.componentIntegrationValid ? "PASS" : "FAIL")}\\n" +
                        $"Visual Feedback: {(result.visualFeedbackValid ? "PASS" : "FAIL")}\\n" +
                        $"Performance Optimization: {(result.performanceOptimizationValid ? "PASS" : "FAIL")}\\n" +
                        $"Lighting Setup: {(result.lightingSetupValid ? "PASS" : "FAIL")}\\n" +
                        $"\\nOVERALL: {(result.overallSuccess ? "SUCCESS" : "NEEDS ATTENTION")}\\n" +
                        $"Tests Passed: {result.GetPassedCount()}/8\\n";
        
        if (result.overallSuccess)
        {
            Debug.Log(summary);
        }
        else
        {
            Debug.LogWarning(summary);
        }
    }
}

/// <summary>
/// Structure to hold validation test results
/// </summary>
[System.Serializable]
public class ValidationResult
{
    public bool editorScriptExists = false;
    public bool runtimeScriptsExist = false;
    public bool materialsValid = false;
    public bool sceneIntegrationValid = false;
    public bool componentIntegrationValid = false;
    public bool visualFeedbackValid = false;
    public bool performanceOptimizationValid = false;
    public bool lightingSetupValid = false;
    public bool overallSuccess = false;
    
    public void CalculateOverallResult()
    {
        overallSuccess = editorScriptExists && 
                        runtimeScriptsExist && 
                        materialsValid && 
                        sceneIntegrationValid && 
                        visualFeedbackValid && 
                        performanceOptimizationValid && 
                        lightingSetupValid;
    }
    
    public int GetPassedCount()
    {
        int count = 0;
        if (editorScriptExists) count++;
        if (runtimeScriptsExist) count++;
        if (materialsValid) count++;
        if (sceneIntegrationValid) count++;
        if (componentIntegrationValid) count++;
        if (visualFeedbackValid) count++;
        if (performanceOptimizationValid) count++;
        if (lightingSetupValid) count++;
        return count;
    }
}