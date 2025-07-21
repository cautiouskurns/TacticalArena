using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor automation tool for Task 1.2.2 - Mouse Selection System.
/// Creates mouse-based unit selection with visual feedback and raycast detection.
/// Integrates with existing Unit components and prepares for movement system.
/// </summary>
public class Task_1_2_2_Setup : EditorWindow
{
    [Header("Selection Visual Configuration")]
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private Material hoveredMaterial;
    [SerializeField] private Color selectionHighlightColor = new Color(1f, 1f, 0f, 1f); // Yellow
    [SerializeField] private Color hoverHighlightColor = new Color(1f, 1f, 1f, 0.8f); // White
    [SerializeField] private float highlightIntensity = 1.5f;
    [SerializeField] private float selectionRimWidth = 0.02f;
    
    [Header("Raycast Configuration")]
    [SerializeField] private LayerMask unitLayerMask = -1;
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private bool enableHoverFeedback = true;
    [SerializeField] private bool restrictToPlayerTeam = true;
    
    [Header("System Configuration")]
    [SerializeField] private bool enableSingleSelection = true;
    [SerializeField] private bool enableSelectionAudio = true;
    [SerializeField] private bool enableDebugVisualization = false;
    [SerializeField] private UnitTeam playerTeam = UnitTeam.Blue;
    
    // Validation state
    private bool validationComplete = false;
    private SelectionSystemValidationResult lastValidationResult;
    
    [MenuItem("Tactical Tools/Task 1.2.2 - Mouse Selection")]
    public static void ShowWindow()
    {
        Task_1_2_2_Setup window = GetWindow<Task_1_2_2_Setup>("Task 1.2.2 Setup");
        window.minSize = new Vector2(500, 700);
        window.Show();
    }
    
    void OnGUI()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Mouse Selection System Setup", EditorStyles.largeLabel);
        EditorGUILayout.LabelField("Task 1.2.2 - Implement mouse-based unit selection with visual feedback", EditorStyles.helpBox);
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // Visual Configuration Section
        EditorGUILayout.LabelField("Selection Visual Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        selectedMaterial = EditorGUILayout.ObjectField("Selected Material", selectedMaterial, typeof(Material), false) as Material;
        hoveredMaterial = EditorGUILayout.ObjectField("Hovered Material", hoveredMaterial, typeof(Material), false) as Material;
        selectionHighlightColor = EditorGUILayout.ColorField("Selection Highlight Color", selectionHighlightColor);
        hoverHighlightColor = EditorGUILayout.ColorField("Hover Highlight Color", hoverHighlightColor);
        highlightIntensity = EditorGUILayout.Slider("Highlight Intensity", highlightIntensity, 1.0f, 3.0f);
        selectionRimWidth = EditorGUILayout.Slider("Selection Rim Width", selectionRimWidth, 0.01f, 0.1f);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Raycast Configuration Section
        EditorGUILayout.LabelField("Raycast Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        unitLayerMask = EditorGUILayout.MaskField("Unit Layer Mask", unitLayerMask, UnityEditorInternal.InternalEditorUtility.layers);
        raycastDistance = EditorGUILayout.FloatField("Raycast Distance", raycastDistance);
        enableHoverFeedback = EditorGUILayout.Toggle("Enable Hover Feedback", enableHoverFeedback);
        restrictToPlayerTeam = EditorGUILayout.Toggle("Restrict to Player Team", restrictToPlayerTeam);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // System Configuration Section
        EditorGUILayout.LabelField("System Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        enableSingleSelection = EditorGUILayout.Toggle("Enable Single Selection", enableSingleSelection);
        enableSelectionAudio = EditorGUILayout.Toggle("Enable Selection Audio", enableSelectionAudio);
        enableDebugVisualization = EditorGUILayout.Toggle("Enable Debug Visualization", enableDebugVisualization);
        playerTeam = (UnitTeam)EditorGUILayout.EnumPopup("Player Team", playerTeam);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(15);
        
        // Action Buttons
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Setup Mouse Selection System", GUILayout.Height(40)))
        {
            SetupMouseSelectionSystem();
        }
        
        if (GUILayout.Button("Reset/Delete Setup", GUILayout.Height(40)))
        {
            ResetSetup();
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // Validation Section
        EditorGUILayout.LabelField("Validation & Testing", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Validate Selection System", GUILayout.Height(30)))
        {
            ValidateSetup();
        }
        
        // Display validation results
        if (validationComplete && lastValidationResult != null)
        {
            DisplayValidationResults();
        }
        
        GUILayout.Space(10);
        
        // Help Section
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Usage Instructions:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("1. Configure visual and raycast settings above", EditorStyles.helpBox);
        EditorGUILayout.LabelField("2. Click 'Setup Mouse Selection System' to create all components", EditorStyles.helpBox);
        EditorGUILayout.LabelField("3. Use 'Validate Selection System' to test functionality", EditorStyles.helpBox);
        EditorGUILayout.LabelField("4. Test in Play Mode: Click units to select, mouse over for hover", EditorStyles.helpBox);
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// Main setup method that creates the complete mouse selection system
    /// </summary>
    private void SetupMouseSelectionSystem()
    {
        Debug.Log("Setting up Mouse Selection System for Task 1.2.2...");
        
        try
        {
            // Step 1: Create selection materials if needed
            CreateSelectionMaterials();
            
            // Step 2: Create or configure SelectionManager
            SetupSelectionManager();
            
            // Step 3: Setup MouseInputHandler
            SetupMouseInputHandler();
            
            // Step 4: Configure existing units with selection capability
            ConfigureUnitsForSelection();
            
            // Step 5: Setup visual feedback system
            SetupVisualFeedbackSystem();
            
            // Step 6: Configure camera for raycasting
            ConfigureCameraForRaycasting();
            
            Debug.Log("Mouse Selection System setup complete!");
            EditorUtility.DisplayDialog("Setup Complete", "Mouse Selection System has been successfully configured!", "OK");
            
            // Run automatic validation
            ValidateSetup();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting up Mouse Selection System: {e.Message}");
            EditorUtility.DisplayDialog("Setup Error", $"Failed to setup Mouse Selection System:\n{e.Message}", "OK");
        }
    }
    
    /// <summary>
    /// Creates selection highlight materials
    /// </summary>
    private void CreateSelectionMaterials()
    {
        // Ensure Materials folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Selection"))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "Selection");
        }
        
        // Create selection highlight material if not assigned
        if (selectedMaterial == null)
        {
            selectedMaterial = CreateHighlightMaterial("SelectedUnit", selectionHighlightColor);
            Debug.Log("Created selection highlight material");
        }
        
        // Create hover highlight material if not assigned
        if (hoveredMaterial == null)
        {
            hoveredMaterial = CreateHighlightMaterial("HoveredUnit", hoverHighlightColor);
            Debug.Log("Created hover highlight material");
        }
    }
    
    /// <summary>
    /// Creates a highlight material with specified color
    /// </summary>
    private Material CreateHighlightMaterial(string name, Color color)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = name;
        material.color = color;
        material.SetFloat("_Metallic", 0.0f);
        material.SetFloat("_Smoothness", 0.8f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * highlightIntensity);
        
        string path = $"Assets/Materials/Selection/{name}.mat";
        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();
        
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }
    
    /// <summary>
    /// Sets up or configures the SelectionManager
    /// </summary>
    private void SetupSelectionManager()
    {
        GameObject selectionManagerObj = GameObject.Find("Selection Manager");
        
        if (selectionManagerObj == null)
        {
            selectionManagerObj = new GameObject("Selection Manager");
        }
        
        SelectionManager selectionManager = selectionManagerObj.GetComponent<SelectionManager>();
        if (selectionManager == null)
        {
            selectionManager = selectionManagerObj.AddComponent<SelectionManager>();
        }
        
        // Configure SelectionManager properties using reflection or serialization
        var serializedManager = new SerializedObject(selectionManager);
        serializedManager.FindProperty("enableSingleSelection").boolValue = enableSingleSelection;
        serializedManager.FindProperty("restrictToPlayerTeam").boolValue = restrictToPlayerTeam;
        serializedManager.FindProperty("playerTeam").enumValueIndex = (int)playerTeam;
        serializedManager.FindProperty("enableDebugVisualization").boolValue = enableDebugVisualization;
        serializedManager.ApplyModifiedProperties();
        
        Debug.Log("SelectionManager configured");
    }
    
    /// <summary>
    /// Sets up the MouseInputHandler component
    /// </summary>
    private void SetupMouseInputHandler()
    {
        GameObject selectionManagerObj = GameObject.Find("Selection Manager");
        
        MouseInputHandler inputHandler = selectionManagerObj.GetComponent<MouseInputHandler>();
        if (inputHandler == null)
        {
            inputHandler = selectionManagerObj.AddComponent<MouseInputHandler>();
        }
        
        // Configure MouseInputHandler properties
        var serializedHandler = new SerializedObject(inputHandler);
        serializedHandler.FindProperty("unitLayerMask").intValue = unitLayerMask;
        serializedHandler.FindProperty("raycastDistance").floatValue = raycastDistance;
        serializedHandler.FindProperty("enableHoverFeedback").boolValue = enableHoverFeedback;
        serializedHandler.ApplyModifiedProperties();
        
        Debug.Log("MouseInputHandler configured");
    }
    
    /// <summary>
    /// Configures existing units for selection capability
    /// </summary>
    private void ConfigureUnitsForSelection()
    {
        // Find units by component (more reliable than tags)
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        GameObject[] unitObjects = new GameObject[units.Length];
        for (int i = 0; i < units.Length; i++)
        {
            unitObjects[i] = units[i].gameObject;
        }
        
        if (unitObjects.Length == 0)
        {
            Debug.LogWarning("No units found in scene. Make sure Task 1.2.1 has been completed first.");
            return;
        }
        
        foreach (GameObject unitObj in unitObjects)
        {
            // Skip if this is not actually a unit
            Unit unit = unitObj.GetComponent<Unit>();
            if (unit == null) continue;
            
            // Ensure unit has a collider for raycasting
            Collider collider = unitObj.GetComponent<Collider>();
            if (collider == null)
            {
                collider = unitObj.AddComponent<BoxCollider>();
            }
            
            // Add SelectionHighlight component
            SelectionHighlight highlight = unitObj.GetComponent<SelectionHighlight>();
            bool wasAdded = false;
            if (highlight == null)
            {
                highlight = unitObj.AddComponent<SelectionHighlight>();
                wasAdded = true;
                Debug.Log($"Added SelectionHighlight to {unitObj.name}");
            }
            else
            {
                Debug.Log($"Updating existing SelectionHighlight on {unitObj.name}");
            }
            
            // Configure SelectionHighlight with materials
            var serializedHighlight = new SerializedObject(highlight);
            
            // Set materials
            serializedHighlight.FindProperty("selectedMaterial").objectReferenceValue = selectedMaterial;
            serializedHighlight.FindProperty("hoveredMaterial").objectReferenceValue = hoveredMaterial;
            
            // Set colors
            serializedHighlight.FindProperty("selectedColor").colorValue = selectionHighlightColor;
            serializedHighlight.FindProperty("hoveredColor").colorValue = hoverHighlightColor;
            
            // Set other properties
            serializedHighlight.FindProperty("highlightIntensity").floatValue = highlightIntensity;
            serializedHighlight.FindProperty("rimWidth").floatValue = selectionRimWidth;
            serializedHighlight.FindProperty("enableSmoothTransitions").boolValue = false; // Disabled for persistent highlighting
            serializedHighlight.FindProperty("usePropertyBlocks").boolValue = false; // Use direct material swapping for visibility
            
            serializedHighlight.ApplyModifiedProperties();
            
            // Force refresh to ensure settings are applied
            if (highlight != null)
            {
                // Call RefreshHighlight to apply new settings
                highlight.RefreshHighlight();
            }
            
            // Disable Unit's built-in visual feedback to avoid conflicts
            Unit unitComponent = unitObj.GetComponent<Unit>();
            if (unitComponent != null)
            {
                var serializedUnit = new SerializedObject(unitComponent);
                serializedUnit.FindProperty("enableVisualFeedback").boolValue = false;
                serializedUnit.FindProperty("enableMouseHover").boolValue = false;
                serializedUnit.ApplyModifiedProperties();
            }
            
            Debug.Log($"Configured {unitObj.name} for selection");
        }
    }
    
    /// <summary>
    /// Sets up the visual feedback system
    /// </summary>
    private void SetupVisualFeedbackSystem()
    {
        // Find or create VisualFeedbackManager
        VisualFeedbackManager feedbackManager = FindFirstObjectByType<VisualFeedbackManager>();
        if (feedbackManager == null)
        {
            GameObject feedbackObj = new GameObject("Visual Feedback Manager");
            feedbackManager = feedbackObj.AddComponent<VisualFeedbackManager>();
        }
        
        // Configure for selection feedback (if the component supports it)
        Debug.Log("Visual feedback system configured for selection");
    }
    
    /// <summary>
    /// Configures the camera for proper raycasting
    /// </summary>
    private void ConfigureCameraForRaycasting()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (mainCamera != null)
        {
            // Ensure camera can raycast to units
            if ((mainCamera.cullingMask & unitLayerMask) == 0)
            {
                Debug.LogWarning("Main camera culling mask may not include unit layers. Check camera configuration.");
            }
            
            Debug.Log("Camera configured for raycasting");
        }
        else
        {
            Debug.LogWarning("No main camera found for raycast configuration");
        }
    }
    
    /// <summary>
    /// Validates the selection system setup
    /// </summary>
    private void ValidateSetup()
    {
        lastValidationResult = new SelectionSystemValidationResult();
        
        // Test 1: SelectionManager exists
        GameObject selectionManagerObj = GameObject.Find("Selection Manager");
        lastValidationResult.selectionManagerExists = selectionManagerObj != null && selectionManagerObj.GetComponent<SelectionManager>() != null;
        
        // Test 2: MouseInputHandler exists
        lastValidationResult.mouseInputHandlerExists = selectionManagerObj != null && selectionManagerObj.GetComponent<MouseInputHandler>() != null;
        
        // Test 3: Units have selection components
        ValidateUnitConfiguration();
        
        // Test 4: Materials exist
        ValidateMaterials();
        
        // Test 5: Camera configuration
        ValidateCameraSetup();
        
        lastValidationResult.CalculateOverallResult();
        validationComplete = true;
        
        // Log results
        if (lastValidationResult.overallSuccess)
        {
            Debug.Log("✓ Mouse Selection System validation passed!");
        }
        else
        {
            Debug.LogWarning($"Mouse Selection System validation issues found. Passed: {lastValidationResult.GetPassedCount()}/5");
        }
    }
    
    /// <summary>
    /// Validates unit configuration for selection
    /// </summary>
    private void ValidateUnitConfiguration()
    {
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        lastValidationResult.unitConfigurationValid = true;
        
        foreach (Unit unit in units)
        {
            // Check for collider
            if (unit.GetComponent<Collider>() == null)
            {
                Debug.LogError($"Unit {unit.name} missing Collider component for selection");
                lastValidationResult.unitConfigurationValid = false;
            }
            
            // Check for SelectionHighlight
            if (unit.GetComponent<SelectionHighlight>() == null)
            {
                Debug.LogError($"Unit {unit.name} missing SelectionHighlight component");
                lastValidationResult.unitConfigurationValid = false;
            }
        }
    }
    
    /// <summary>
    /// Validates selection materials
    /// </summary>
    private void ValidateMaterials()
    {
        lastValidationResult.materialsExist = selectedMaterial != null && hoveredMaterial != null;
        
        if (!lastValidationResult.materialsExist)
        {
            Debug.LogError("Selection materials are missing");
        }
    }
    
    /// <summary>
    /// Validates camera setup for raycasting
    /// </summary>
    private void ValidateCameraSetup()
    {
        Camera mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();
        lastValidationResult.cameraSetupValid = mainCamera != null;
        
        if (!lastValidationResult.cameraSetupValid)
        {
            Debug.LogError("No camera found for raycasting");
        }
    }
    
    /// <summary>
    /// Displays validation results in the editor
    /// </summary>
    private void DisplayValidationResults()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);
        
        // Overall status
        Color originalColor = GUI.color;
        GUI.color = lastValidationResult.overallSuccess ? Color.green : Color.red;
        EditorGUILayout.LabelField($"Overall Status: {(lastValidationResult.overallSuccess ? "PASSED" : "NEEDS ATTENTION")}", EditorStyles.boldLabel);
        GUI.color = originalColor;
        
        EditorGUILayout.LabelField($"Tests Passed: {lastValidationResult.GetPassedCount()}/5", EditorStyles.helpBox);
        
        // Individual test results
        DrawValidationItem("Selection Manager", lastValidationResult.selectionManagerExists);
        DrawValidationItem("Mouse Input Handler", lastValidationResult.mouseInputHandlerExists);
        DrawValidationItem("Unit Configuration", lastValidationResult.unitConfigurationValid);
        DrawValidationItem("Selection Materials", lastValidationResult.materialsExist);
        DrawValidationItem("Camera Setup", lastValidationResult.cameraSetupValid);
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// Draws a validation result item
    /// </summary>
    private void DrawValidationItem(string testName, bool passed)
    {
        Color originalColor = GUI.color;
        GUI.color = passed ? Color.green : Color.red;
        string status = passed ? "✓" : "✗";
        EditorGUILayout.LabelField($"{status} {testName}");
        GUI.color = originalColor;
    }
    
    /// <summary>
    /// Resets/deletes the mouse selection setup
    /// </summary>
    private void ResetSetup()
    {
        if (EditorUtility.DisplayDialog("Reset Selection System", 
            "This will remove all mouse selection components and materials. Continue?", 
            "Yes", "Cancel"))
        {
            try
            {
                // Remove SelectionManager
                GameObject selectionManagerObj = GameObject.Find("Selection Manager");
                if (selectionManagerObj != null)
                {
                    DestroyImmediate(selectionManagerObj);
                }
                
                // Remove SelectionHighlight components from units
                Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
                foreach (Unit unit in units)
                {
                    SelectionHighlight highlight = unit.GetComponent<SelectionHighlight>();
                    if (highlight != null)
                    {
                        DestroyImmediate(highlight);
                    }
                }
                
                // Delete selection materials
                if (selectedMaterial != null)
                {
                    string path = AssetDatabase.GetAssetPath(selectedMaterial);
                    if (!string.IsNullOrEmpty(path))
                    {
                        AssetDatabase.DeleteAsset(path);
                    }
                }
                
                if (hoveredMaterial != null)
                {
                    string path = AssetDatabase.GetAssetPath(hoveredMaterial);
                    if (!string.IsNullOrEmpty(path))
                    {
                        AssetDatabase.DeleteAsset(path);
                    }
                }
                
                // Reset references
                selectedMaterial = null;
                hoveredMaterial = null;
                validationComplete = false;
                
                Debug.Log("Mouse Selection System reset complete");
                EditorUtility.DisplayDialog("Reset Complete", "Mouse Selection System has been reset.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error resetting selection system: {e.Message}");
            }
        }
    }
}

/// <summary>
/// Structure to hold selection system validation results
/// </summary>
[System.Serializable]
public class SelectionSystemValidationResult
{
    public bool selectionManagerExists = false;
    public bool mouseInputHandlerExists = false;
    public bool unitConfigurationValid = false;
    public bool materialsExist = false;
    public bool cameraSetupValid = false;
    public bool overallSuccess = false;
    
    public void CalculateOverallResult()
    {
        overallSuccess = selectionManagerExists && 
                        mouseInputHandlerExists && 
                        unitConfigurationValid && 
                        materialsExist && 
                        cameraSetupValid;
    }
    
    public int GetPassedCount()
    {
        int count = 0;
        if (selectionManagerExists) count++;
        if (mouseInputHandlerExists) count++;
        if (unitConfigurationValid) count++;
        if (materialsExist) count++;
        if (cameraSetupValid) count++;
        return count;
    }
}