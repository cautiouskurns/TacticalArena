using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor automation tool for Task 1.2.3 - Grid-Based Movement System.
/// Creates click-to-move functionality with grid validation, smooth animation, and state management.
/// Integrates with existing SelectionManager and GridManager systems.
/// </summary>
public class Task_1_2_3_Setup : EditorWindow
{
    [Header("Movement Configuration")]
    [SerializeField] private float movementSpeed = 2.0f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool allowDiagonalMovement = false;
    [SerializeField] private float movementTolerance = 0.1f;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool showMovementPreview = true;
    [SerializeField] private float previewFadeTime = 0.3f;
    [SerializeField] private Material validMoveMaterial;
    [SerializeField] private Material invalidMoveMaterial;
    [SerializeField] private Color validMoveColor = Color.green;
    [SerializeField] private Color invalidMoveColor = Color.red;
    
    [Header("Raycast Configuration")]
    [SerializeField] private LayerMask gridLayerMask = -1;
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private bool enableMovementLogging = true;
    
    [Header("System Configuration")]
    [SerializeField] private bool enableMovementValidation = true;
    [SerializeField] private bool enableMovementAnimation = true;
    [SerializeField] private bool restrictToAdjacentTiles = true;
    [SerializeField] private bool preventOverlappingMoves = true;
    
    // Validation state
    private bool validationComplete = false;
    private MovementSystemValidationResult lastValidationResult;
    
    [MenuItem("Tactical Tools/Task 1.2.3 - Grid Movement")]
    public static void ShowWindow()
    {
        Task_1_2_3_Setup window = GetWindow<Task_1_2_3_Setup>("Task 1.2.3 Setup");
        window.minSize = new Vector2(500, 800);
        window.Show();
    }
    
    void OnGUI()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Grid-Based Movement System Setup", EditorStyles.largeLabel);
        EditorGUILayout.LabelField("Task 1.2.3 - Implement click-to-move with grid validation and smooth animation", EditorStyles.helpBox);
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // Movement Configuration Section
        EditorGUILayout.LabelField("Movement Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        movementSpeed = EditorGUILayout.Slider("Movement Speed", movementSpeed, 0.5f, 5.0f);
        movementCurve = EditorGUILayout.CurveField("Movement Animation Curve", movementCurve);
        allowDiagonalMovement = EditorGUILayout.Toggle("Allow Diagonal Movement", allowDiagonalMovement);
        movementTolerance = EditorGUILayout.Slider("Movement Tolerance", movementTolerance, 0.01f, 0.5f);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Visual Feedback Section
        EditorGUILayout.LabelField("Visual Feedback", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        showMovementPreview = EditorGUILayout.Toggle("Show Movement Preview", showMovementPreview);
        previewFadeTime = EditorGUILayout.Slider("Preview Fade Time", previewFadeTime, 0.1f, 1.0f);
        validMoveMaterial = EditorGUILayout.ObjectField("Valid Move Material", validMoveMaterial, typeof(Material), false) as Material;
        invalidMoveMaterial = EditorGUILayout.ObjectField("Invalid Move Material", invalidMoveMaterial, typeof(Material), false) as Material;
        validMoveColor = EditorGUILayout.ColorField("Valid Move Color", validMoveColor);
        invalidMoveColor = EditorGUILayout.ColorField("Invalid Move Color", invalidMoveColor);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Raycast Configuration Section
        EditorGUILayout.LabelField("Raycast Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        gridLayerMask = EditorGUILayout.MaskField("Grid Layer Mask", gridLayerMask, UnityEditorInternal.InternalEditorUtility.layers);
        raycastDistance = EditorGUILayout.FloatField("Raycast Distance", raycastDistance);
        enableMovementLogging = EditorGUILayout.Toggle("Enable Movement Logging", enableMovementLogging);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // System Configuration Section
        EditorGUILayout.LabelField("System Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        enableMovementValidation = EditorGUILayout.Toggle("Enable Movement Validation", enableMovementValidation);
        enableMovementAnimation = EditorGUILayout.Toggle("Enable Movement Animation", enableMovementAnimation);
        restrictToAdjacentTiles = EditorGUILayout.Toggle("Restrict to Adjacent Tiles", restrictToAdjacentTiles);
        preventOverlappingMoves = EditorGUILayout.Toggle("Prevent Overlapping Moves", preventOverlappingMoves);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(15);
        
        // Action Buttons
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Setup Grid Movement System", GUILayout.Height(40)))
        {
            SetupGridMovementSystem();
        }
        
        if (GUILayout.Button("Reset/Delete Setup", GUILayout.Height(40)))
        {
            ResetSetup();
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // Validation Section
        EditorGUILayout.LabelField("Validation & Testing", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Validate Movement System", GUILayout.Height(30)))
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
        EditorGUILayout.LabelField("1. Ensure Tasks 1.2.1 (Unit System) and 1.2.2 (Mouse Selection) are completed", EditorStyles.helpBox);
        EditorGUILayout.LabelField("2. Configure movement speed, animation, and validation settings above", EditorStyles.helpBox);
        EditorGUILayout.LabelField("3. Set Grid Layer Mask to include grid tiles/ground for raycast detection", EditorStyles.helpBox);
        EditorGUILayout.LabelField("4. Click 'Setup Grid Movement System' to create all movement components", EditorStyles.helpBox);
        EditorGUILayout.LabelField("5. Use 'Validate Movement System' to test click-to-move functionality", EditorStyles.helpBox);
        EditorGUILayout.LabelField("6. Test in Play Mode: Select unit, click adjacent grid tile to move", EditorStyles.helpBox);
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// Main setup method that creates the complete grid movement system
    /// </summary>
    private void SetupGridMovementSystem()
    {
        Debug.Log("Setting up Grid-Based Movement System for Task 1.2.3...");
        
        try
        {
            // Step 1: Create movement materials if needed
            CreateMovementMaterials();
            
            // Step 2: Create or configure MovementManager
            SetupMovementManager();
            
            // Step 3: Setup movement validation system
            SetupMovementValidator();
            
            // Step 4: Setup movement animation system
            SetupMovementAnimator();
            
            // Step 5: Configure existing units with movement capability
            ConfigureUnitsForMovement();
            
            // Step 6: Integrate with existing SelectionManager
            IntegrateWithSelectionManager();
            
            // Step 7: Setup visual feedback system
            SetupMovementVisualFeedback();
            
            Debug.Log("Grid-Based Movement System setup complete!");
            EditorUtility.DisplayDialog("Setup Complete", "Grid Movement System has been successfully configured!\n\nSelect a unit and click on adjacent grid tiles to test movement.", "OK");
            
            // Run automatic validation
            ValidateSetup();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting up Grid Movement System: {e.Message}");
            EditorUtility.DisplayDialog("Setup Error", $"Failed to setup Grid Movement System:\n{e.Message}", "OK");
        }
    }
    
    /// <summary>
    /// Creates movement preview materials
    /// </summary>
    private void CreateMovementMaterials()
    {
        // Ensure Materials folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Movement"))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "Movement");
        }
        
        // Create valid move material if not assigned
        if (validMoveMaterial == null)
        {
            validMoveMaterial = CreateMovementMaterial("ValidMove", validMoveColor);
            Debug.Log("Created valid move material");
        }
        
        // Create invalid move material if not assigned
        if (invalidMoveMaterial == null)
        {
            invalidMoveMaterial = CreateMovementMaterial("InvalidMove", invalidMoveColor);
            Debug.Log("Created invalid move material");
        }
    }
    
    /// <summary>
    /// Creates a movement preview material with specified color
    /// </summary>
    private Material CreateMovementMaterial(string name, Color color)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = name;
        material.color = color;
        material.SetFloat("_Metallic", 0.0f);
        material.SetFloat("_Smoothness", 0.3f);
        
        // Make it semi-transparent for preview effect
        material.SetFloat("_Surface", 1); // Transparent
        material.SetFloat("_Blend", 0); // Alpha blend
        Color transparentColor = color;
        transparentColor.a = 0.6f;
        material.color = transparentColor;
        material.renderQueue = 3000; // Transparent queue
        
        string path = $"Assets/Materials/Movement/{name}.mat";
        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();
        
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }
    
    /// <summary>
    /// Sets up or configures the MovementManager
    /// </summary>
    private void SetupMovementManager()
    {
        GameObject movementManagerObj = GameObject.Find("Movement Manager");
        
        if (movementManagerObj == null)
        {
            movementManagerObj = new GameObject("Movement Manager");
        }
        
        MovementManager movementManager = movementManagerObj.GetComponent<MovementManager>();
        if (movementManager == null)
        {
            movementManager = movementManagerObj.AddComponent<MovementManager>();
        }
        
        // Configure MovementManager properties
        var serializedManager = new SerializedObject(movementManager);
        serializedManager.FindProperty("movementSpeed").floatValue = movementSpeed;
        serializedManager.FindProperty("allowDiagonalMovement").boolValue = allowDiagonalMovement;
        serializedManager.FindProperty("restrictToAdjacentTiles").boolValue = restrictToAdjacentTiles;
        serializedManager.FindProperty("preventOverlappingMoves").boolValue = preventOverlappingMoves;
        serializedManager.FindProperty("enableMovementLogging").boolValue = enableMovementLogging;
        serializedManager.FindProperty("gridLayerMask").intValue = gridLayerMask;
        serializedManager.FindProperty("raycastDistance").floatValue = raycastDistance;
        serializedManager.ApplyModifiedProperties();
        
        Debug.Log("MovementManager configured");
    }
    
    /// <summary>
    /// Sets up the MovementValidator component
    /// </summary>
    private void SetupMovementValidator()
    {
        GameObject movementManagerObj = GameObject.Find("Movement Manager");
        
        MovementValidator validator = movementManagerObj.GetComponent<MovementValidator>();
        if (validator == null)
        {
            validator = movementManagerObj.AddComponent<MovementValidator>();
        }
        
        // Configure MovementValidator properties
        var serializedValidator = new SerializedObject(validator);
        serializedValidator.FindProperty("enableValidation").boolValue = enableMovementValidation;
        serializedValidator.FindProperty("allowDiagonalMovement").boolValue = allowDiagonalMovement;
        serializedValidator.FindProperty("restrictToAdjacentTiles").boolValue = restrictToAdjacentTiles;
        serializedValidator.FindProperty("enableValidationLogging").boolValue = enableMovementLogging;
        serializedValidator.ApplyModifiedProperties();
        
        Debug.Log("MovementValidator configured");
    }
    
    /// <summary>
    /// Sets up the MovementAnimator component
    /// </summary>
    private void SetupMovementAnimator()
    {
        GameObject movementManagerObj = GameObject.Find("Movement Manager");
        
        MovementAnimator animator = movementManagerObj.GetComponent<MovementAnimator>();
        if (animator == null)
        {
            animator = movementManagerObj.AddComponent<MovementAnimator>();
        }
        
        // Configure MovementAnimator properties
        var serializedAnimator = new SerializedObject(animator);
        serializedAnimator.FindProperty("enableAnimation").boolValue = enableMovementAnimation;
        serializedAnimator.FindProperty("movementSpeed").floatValue = movementSpeed;
        serializedAnimator.FindProperty("movementCurve").animationCurveValue = movementCurve;
        serializedAnimator.FindProperty("movementTolerance").floatValue = movementTolerance;
        serializedAnimator.FindProperty("enableAnimationLogging").boolValue = enableMovementLogging;
        // Disable problematic features that cause tilting and ground embedding
        serializedAnimator.FindProperty("enableHeightAnimation").boolValue = false;
        serializedAnimator.FindProperty("enableRotationAlignment").boolValue = false;
        serializedAnimator.ApplyModifiedProperties();
        
        Debug.Log("MovementAnimator configured");
    }
    
    /// <summary>
    /// Configures existing units for movement capability
    /// </summary>
    private void ConfigureUnitsForMovement()
    {
        // Find units by component
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        if (units.Length == 0)
        {
            Debug.LogWarning("No units found in scene. Make sure Task 1.2.1 has been completed first.");
            return;
        }
        
        foreach (Unit unit in units)
        {
            // Ensure unit has IMovable interface (Unit should implement this)
            if (!(unit is IMovable))
            {
                Debug.LogWarning($"Unit {unit.name} does not implement IMovable interface");
                continue;
            }
            
            // Add GridMovementComponent
            GridMovementComponent movementComponent = unit.GetComponent<GridMovementComponent>();
            if (movementComponent == null)
            {
                movementComponent = unit.gameObject.AddComponent<GridMovementComponent>();
                Debug.Log($"Added GridMovementComponent to {unit.name}");
            }
            
            // Configure GridMovementComponent
            var serializedMovement = new SerializedObject(movementComponent);
            serializedMovement.FindProperty("movementSpeed").floatValue = movementSpeed;
            serializedMovement.FindProperty("enableMovementAnimation").boolValue = enableMovementAnimation;
            serializedMovement.FindProperty("enableMovementLogging").boolValue = enableMovementLogging;
            serializedMovement.ApplyModifiedProperties();
            
            Debug.Log($"Configured {unit.name} for movement");
        }
    }
    
    /// <summary>
    /// Integrates the movement system with SelectionManager
    /// </summary>
    private void IntegrateWithSelectionManager()
    {
        SelectionManager selectionManager = FindFirstObjectByType<SelectionManager>();
        if (selectionManager == null)
        {
            Debug.LogError("SelectionManager not found! Make sure Task 1.2.2 is completed first.");
            return;
        }
        
        MovementManager movementManager = FindFirstObjectByType<MovementManager>();
        if (movementManager == null)
        {
            Debug.LogError("MovementManager not found!");
            return;
        }
        
        // Configure movement manager to reference selection manager
        var serializedMovement = new SerializedObject(movementManager);
        serializedMovement.FindProperty("selectionManager").objectReferenceValue = selectionManager;
        serializedMovement.ApplyModifiedProperties();
        
        Debug.Log("MovementManager integrated with SelectionManager");
    }
    
    /// <summary>
    /// Sets up visual feedback for movement previews
    /// </summary>
    private void SetupMovementVisualFeedback()
    {
        MovementManager movementManager = FindFirstObjectByType<MovementManager>();
        if (movementManager == null) return;
        
        // Configure visual feedback materials
        var serializedMovement = new SerializedObject(movementManager);
        serializedMovement.FindProperty("showMovementPreview").boolValue = showMovementPreview;
        serializedMovement.FindProperty("previewFadeTime").floatValue = previewFadeTime;
        serializedMovement.FindProperty("validMoveMaterial").objectReferenceValue = validMoveMaterial;
        serializedMovement.FindProperty("invalidMoveMaterial").objectReferenceValue = invalidMoveMaterial;
        serializedMovement.ApplyModifiedProperties();
        
        Debug.Log("Movement visual feedback configured");
    }
    
    /// <summary>
    /// Validates the movement system setup
    /// </summary>
    private void ValidateSetup()
    {
        lastValidationResult = new MovementSystemValidationResult();
        
        // Test 1: MovementManager exists
        MovementManager movementManager = FindFirstObjectByType<MovementManager>();
        lastValidationResult.movementManagerExists = movementManager != null;
        
        // Test 2: MovementValidator exists
        MovementValidator validator = FindFirstObjectByType<MovementValidator>();
        lastValidationResult.movementValidatorExists = validator != null;
        
        // Test 3: MovementAnimator exists
        MovementAnimator animator = FindFirstObjectByType<MovementAnimator>();
        lastValidationResult.movementAnimatorExists = animator != null;
        
        // Test 4: Units have movement components
        ValidateUnitMovementConfiguration();
        
        // Test 5: Integration with SelectionManager
        ValidateSelectionIntegration();
        
        // Test 6: Materials exist
        ValidateMovementMaterials();
        
        lastValidationResult.CalculateOverallResult();
        validationComplete = true;
        
        // Log results
        if (lastValidationResult.overallSuccess)
        {
            Debug.Log("✓ Grid Movement System validation passed!");
        }
        else
        {
            Debug.LogWarning($"Grid Movement System validation issues found. Passed: {lastValidationResult.GetPassedCount()}/6");
        }
    }
    
    /// <summary>
    /// Validates unit movement configuration
    /// </summary>
    private void ValidateUnitMovementConfiguration()
    {
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        lastValidationResult.unitMovementConfigurationValid = true;
        
        foreach (Unit unit in units)
        {
            // Check for IMovable interface
            if (!(unit is IMovable))
            {
                Debug.LogError($"Unit {unit.name} does not implement IMovable interface");
                lastValidationResult.unitMovementConfigurationValid = false;
            }
            
            // Check for GridMovementComponent
            if (unit.GetComponent<GridMovementComponent>() == null)
            {
                Debug.LogError($"Unit {unit.name} missing GridMovementComponent");
                lastValidationResult.unitMovementConfigurationValid = false;
            }
        }
    }
    
    /// <summary>
    /// Validates integration with SelectionManager
    /// </summary>
    private void ValidateSelectionIntegration()
    {
        SelectionManager selectionManager = FindFirstObjectByType<SelectionManager>();
        MovementManager movementManager = FindFirstObjectByType<MovementManager>();
        
        lastValidationResult.selectionIntegrationValid = selectionManager != null && movementManager != null;
        
        if (!lastValidationResult.selectionIntegrationValid)
        {
            Debug.LogError("Selection-Movement integration validation failed");
        }
    }
    
    /// <summary>
    /// Validates movement materials
    /// </summary>
    private void ValidateMovementMaterials()
    {
        lastValidationResult.movementMaterialsExist = validMoveMaterial != null && invalidMoveMaterial != null;
        
        if (!lastValidationResult.movementMaterialsExist)
        {
            Debug.LogError("Movement preview materials are missing");
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
        
        EditorGUILayout.LabelField($"Tests Passed: {lastValidationResult.GetPassedCount()}/6", EditorStyles.helpBox);
        
        // Individual test results
        DrawValidationItem("Movement Manager", lastValidationResult.movementManagerExists);
        DrawValidationItem("Movement Validator", lastValidationResult.movementValidatorExists);
        DrawValidationItem("Movement Animator", lastValidationResult.movementAnimatorExists);
        DrawValidationItem("Unit Movement Config", lastValidationResult.unitMovementConfigurationValid);
        DrawValidationItem("Selection Integration", lastValidationResult.selectionIntegrationValid);
        DrawValidationItem("Movement Materials", lastValidationResult.movementMaterialsExist);
        
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
    /// Resets/deletes the movement system setup
    /// </summary>
    private void ResetSetup()
    {
        if (EditorUtility.DisplayDialog("Reset Movement System", 
            "This will remove all movement components and materials. Continue?", 
            "Yes", "Cancel"))
        {
            try
            {
                // Remove MovementManager
                MovementManager movementManager = FindFirstObjectByType<MovementManager>();
                if (movementManager != null)
                {
                    DestroyImmediate(movementManager.gameObject);
                }
                
                // Remove GridMovementComponent from units
                Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
                foreach (Unit unit in units)
                {
                    GridMovementComponent movementComponent = unit.GetComponent<GridMovementComponent>();
                    if (movementComponent != null)
                    {
                        DestroyImmediate(movementComponent);
                    }
                }
                
                // Delete movement materials
                if (validMoveMaterial != null)
                {
                    string path = AssetDatabase.GetAssetPath(validMoveMaterial);
                    if (!string.IsNullOrEmpty(path))
                    {
                        AssetDatabase.DeleteAsset(path);
                    }
                }
                
                if (invalidMoveMaterial != null)
                {
                    string path = AssetDatabase.GetAssetPath(invalidMoveMaterial);
                    if (!string.IsNullOrEmpty(path))
                    {
                        AssetDatabase.DeleteAsset(path);
                    }
                }
                
                // Reset references
                validMoveMaterial = null;
                invalidMoveMaterial = null;
                validationComplete = false;
                
                Debug.Log("Grid Movement System reset complete");
                EditorUtility.DisplayDialog("Reset Complete", "Grid Movement System has been reset.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error resetting movement system: {e.Message}");
            }
        }
    }
}

/// <summary>
/// Structure to hold movement system validation results
/// </summary>
[System.Serializable]
public class MovementSystemValidationResult
{
    public bool movementManagerExists = false;
    public bool movementValidatorExists = false;
    public bool movementAnimatorExists = false;
    public bool unitMovementConfigurationValid = false;
    public bool selectionIntegrationValid = false;
    public bool movementMaterialsExist = false;
    public bool overallSuccess = false;
    
    public void CalculateOverallResult()
    {
        overallSuccess = movementManagerExists && 
                        movementValidatorExists && 
                        movementAnimatorExists && 
                        unitMovementConfigurationValid && 
                        selectionIntegrationValid && 
                        movementMaterialsExist;
    }
    
    public int GetPassedCount()
    {
        int count = 0;
        if (movementManagerExists) count++;
        if (movementValidatorExists) count++;
        if (movementAnimatorExists) count++;
        if (unitMovementConfigurationValid) count++;
        if (selectionIntegrationValid) count++;
        if (movementMaterialsExist) count++;
        return count;
    }
}