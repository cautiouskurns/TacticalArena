using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor window for setting up Task 2.1.2 - Line of Sight Mechanics
/// Automates the creation and configuration of line-of-sight validation system with obstacle blocking
/// </summary>
public class Task_2_1_2_Setup : EditorWindow
{
    [Header("Line of Sight Configuration")]
    [SerializeField] private LayerMask obstacleLayerMask = -1;
    [SerializeField] private float raycastHeight = 0.5f;
    [SerializeField] private float raycastRadius = 0.1f;
    [SerializeField] private bool useSphereCast = true;
    [SerializeField] private bool enableLineOfSightDebugging = true;
    
    [Header("Visual Feedback Configuration")]
    [SerializeField] private Material lineOfSightMaterial;
    [SerializeField] private Color blockedLineColor = Color.red;
    [SerializeField] private Color clearLineColor = Color.green;
    [SerializeField] private float lineVisualizationDuration = 2.0f;
    [SerializeField] private bool showLineOfSightLines = true;
    [SerializeField] private bool showObstacleBlockingFeedback = true;
    
    [Header("Attack Integration Configuration")]
    [SerializeField] private bool requireLineOfSightForAttacks = true;
    [SerializeField] private bool allowAttackThroughDiagonalGaps = false;
    [SerializeField] private float lineOfSightTolerance = 0.1f;
    [SerializeField] private bool cacheLineOfSightResults = true;
    
    [Header("Performance Configuration")]
    [SerializeField] private int maxRaycastsPerFrame = 10;
    [SerializeField] private bool enableLineOfSightOptimization = true;
    [SerializeField] private float lineOfSightUpdateInterval = 0.1f;
    
    [Header("Debug Configuration")]
    [SerializeField] private bool enableRaycastVisualization = false;
    [SerializeField] private bool logLineOfSightResults = false;
    [SerializeField] private bool showLineOfSightGizmos = true;

    // Setup state tracking
    private bool hasValidatedPrerequisites = false;
    private List<string> validationErrors = new List<string>();

    [MenuItem("Tactical Tools/Task 2.1.2 - Line of Sight")]
    public static void ShowWindow()
    {
        Task_2_1_2_Setup window = GetWindow<Task_2_1_2_Setup>("Task 2.1.2 Setup");
        window.minSize = new Vector2(400, 700);
        window.maxSize = new Vector2(500, 800);
    }

    void OnGUI()
    {
        GUILayout.Label("Task 2.1.2 - Line of Sight Mechanics Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Prerequisites validation
        if (!hasValidatedPrerequisites)
        {
            EditorGUILayout.HelpBox("Click 'Validate Prerequisites' before setup to ensure Task 2.1.1 is complete", MessageType.Warning);
            if (GUILayout.Button("Validate Prerequisites"))
            {
                ValidatePrerequisites();
            }
            GUILayout.Space(10);
        }
        else if (validationErrors.Count > 0)
        {
            EditorGUILayout.HelpBox("Prerequisites validation failed. Please complete Task 2.1.1 first.", MessageType.Error);
            foreach (string error in validationErrors)
            {
                EditorGUILayout.HelpBox("• " + error, MessageType.None);
            }
            if (GUILayout.Button("Re-validate Prerequisites"))
            {
                ValidatePrerequisites();
            }
            return;
        }

        // Line of Sight Core Configuration
        GUILayout.Label("Line of Sight Configuration", EditorStyles.boldLabel);
        obstacleLayerMask = EditorGUILayout.MaskField("Obstacle Layer Mask", obstacleLayerMask, UnityEditorInternal.InternalEditorUtility.layers);
        raycastHeight = EditorGUILayout.Slider("Raycast Height", raycastHeight, 0.1f, 2.0f);
        raycastRadius = EditorGUILayout.Slider("Raycast Radius", raycastRadius, 0.05f, 0.5f);
        useSphereCast = EditorGUILayout.Toggle("Use Sphere Cast", useSphereCast);
        enableLineOfSightDebugging = EditorGUILayout.Toggle("Enable LOS Debugging", enableLineOfSightDebugging);
        
        GUILayout.Space(5);
        GUILayout.Label("Visual Feedback", EditorStyles.boldLabel);
        lineOfSightMaterial = EditorGUILayout.ObjectField("Line of Sight Material", lineOfSightMaterial, typeof(Material), false) as Material;
        if (lineOfSightMaterial == null)
        {
            EditorGUILayout.HelpBox("Line renderer material will be auto-created if not provided", MessageType.Info);
        }
        blockedLineColor = EditorGUILayout.ColorField("Blocked Line Color", blockedLineColor);
        clearLineColor = EditorGUILayout.ColorField("Clear Line Color", clearLineColor);
        lineVisualizationDuration = EditorGUILayout.Slider("Line Visualization Duration", lineVisualizationDuration, 0.5f, 5.0f);
        showLineOfSightLines = EditorGUILayout.Toggle("Show LOS Lines", showLineOfSightLines);
        showObstacleBlockingFeedback = EditorGUILayout.Toggle("Show Blocking Feedback", showObstacleBlockingFeedback);
        
        GUILayout.Space(5);
        GUILayout.Label("Attack Integration", EditorStyles.boldLabel);
        requireLineOfSightForAttacks = EditorGUILayout.Toggle("Require LOS for Attacks", requireLineOfSightForAttacks);
        allowAttackThroughDiagonalGaps = EditorGUILayout.Toggle("Allow Diagonal Gap Attacks", allowAttackThroughDiagonalGaps);
        lineOfSightTolerance = EditorGUILayout.Slider("LOS Tolerance", lineOfSightTolerance, 0.0f, 0.5f);
        cacheLineOfSightResults = EditorGUILayout.Toggle("Cache LOS Results", cacheLineOfSightResults);
        
        GUILayout.Space(5);
        GUILayout.Label("Performance Settings", EditorStyles.boldLabel);
        maxRaycastsPerFrame = EditorGUILayout.IntSlider("Max Raycasts Per Frame", maxRaycastsPerFrame, 1, 20);
        enableLineOfSightOptimization = EditorGUILayout.Toggle("Enable LOS Optimization", enableLineOfSightOptimization);
        lineOfSightUpdateInterval = EditorGUILayout.Slider("LOS Update Interval", lineOfSightUpdateInterval, 0.05f, 1.0f);
        
        GUILayout.Space(5);
        GUILayout.Label("Debug Options", EditorStyles.boldLabel);
        enableRaycastVisualization = EditorGUILayout.Toggle("Enable Raycast Visualization", enableRaycastVisualization);
        logLineOfSightResults = EditorGUILayout.Toggle("Log LOS Results", logLineOfSightResults);
        showLineOfSightGizmos = EditorGUILayout.Toggle("Show LOS Gizmos", showLineOfSightGizmos);

        GUILayout.Space(10);

        // Setup and Reset buttons
        GUI.enabled = hasValidatedPrerequisites && validationErrors.Count == 0;
        if (GUILayout.Button("Setup Line of Sight System", GUILayout.Height(30)))
        {
            SetupLineOfSightSystem();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Reset/Delete Setup"))
        {
            if (EditorUtility.DisplayDialog("Reset Line of Sight Setup", 
                "This will remove all Line of Sight components and configurations. Continue?", 
                "Yes", "Cancel"))
            {
                ResetSetup();
            }
        }

        GUILayout.Space(10);

        // Validation button
        if (GUILayout.Button("Validate Current Setup"))
        {
            ValidateSetup();
        }

        if (GUILayout.Button("Test Line of Sight"))
        {
            TestLineOfSight();
        }
    }

    private void ValidatePrerequisites()
    {
        Debug.Log("=== Validating Task 2.1.2 Prerequisites ===");
        validationErrors.Clear();

        // Check for CombatManager
        CombatManager combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager == null)
        {
            validationErrors.Add("CombatManager not found - Task 2.1.1 required");
        }
        else
        {
            Debug.Log("✓ CombatManager found");
            
            // Check CombatManager components
            if (combatManager.GetComponent<AttackValidator>() == null)
                validationErrors.Add("AttackValidator component missing from CombatManager");
            if (combatManager.GetComponent<AttackExecutor>() == null)
                validationErrors.Add("AttackExecutor component missing from CombatManager");
            if (combatManager.GetComponent<TargetingSystem>() == null)
                validationErrors.Add("TargetingSystem component missing from CombatManager");
        }

        // Check for GridManager
        GridManager gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            validationErrors.Add("GridManager not found - Sub-milestone 1.1 required");
        }
        else
        {
            Debug.Log("✓ GridManager found");
        }

        // Check for obstacles
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        if (obstacles.Length == 0)
        {
            Debug.LogWarning("⚠ No obstacles found with 'Obstacle' tag - line-of-sight blocking may not work");
        }
        else
        {
            Debug.Log($"✓ Found {obstacles.Length} obstacles for line-of-sight blocking");
        }

        // Check for units with combat capabilities
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        if (units.Length == 0)
        {
            validationErrors.Add("No units found - Sub-milestone 1.2 required");
        }
        else
        {
            int unitsWithAttackCapability = 0;
            foreach (Unit unit in units)
            {
                if (unit.GetComponent<AttackCapability>() != null)
                    unitsWithAttackCapability++;
            }
            
            if (unitsWithAttackCapability == 0)
            {
                validationErrors.Add("No units have AttackCapability - Task 2.1.1 required");
            }
            else
            {
                Debug.Log($"✓ Found {unitsWithAttackCapability} units with combat capability");
            }
        }

        hasValidatedPrerequisites = true;

        if (validationErrors.Count == 0)
        {
            Debug.Log("✓ All prerequisites validated - ready for line-of-sight setup");
        }
        else
        {
            Debug.LogError($"✗ Prerequisites validation failed with {validationErrors.Count} errors");
            foreach (string error in validationErrors)
            {
                Debug.LogError($"  - {error}");
            }
        }
    }

    private void SetupLineOfSightSystem()
    {
        try
        {
            Debug.Log("=== Setting up Line of Sight System ===");

            // Find or create LineOfSightManager
            LineOfSightManager losManager = FindFirstObjectByType<LineOfSightManager>();
            if (losManager == null)
            {
                GameObject losObject = new GameObject("LineOfSightManager");
                losManager = losObject.AddComponent<LineOfSightManager>();
                Debug.Log("✓ Created LineOfSightManager");
            }
            else
            {
                Debug.Log("✓ Using existing LineOfSightManager");
            }

            // Configure LineOfSightManager
            SetupLineOfSightManagerConfiguration(losManager);

            // Setup line-of-sight visualization if enabled
            if (showLineOfSightLines)
            {
                SetupLineOfSightVisualization(losManager);
            }

            // Integrate with existing CombatManager
            IntegrateWithCombatSystem();

            // Setup performance optimization if enabled
            if (enableLineOfSightOptimization)
            {
                SetupPerformanceOptimization(losManager);
            }

            // Create line-of-sight material if needed
            if (lineOfSightMaterial == null)
            {
                CreateLineOfSightMaterial();
            }

            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("✓ Line of Sight System setup complete!");
            EditorUtility.DisplayDialog("Setup Complete", 
                "Line of Sight System has been successfully configured!\n\n" +
                "• LineOfSightManager created and configured\n" +
                "• Combat system integration complete\n" +
                "• Visual feedback system ready\n" +
                "• Performance optimization enabled\n\n" +
                "Use 'Validate Current Setup' to verify functionality.", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting up Line of Sight System: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            EditorUtility.DisplayDialog("Setup Error", 
                $"Failed to setup Line of Sight System:\n{e.Message}\n\nCheck console for details.", "OK");
        }
    }

    private void SetupLineOfSightManagerConfiguration(LineOfSightManager losManager)
    {
        // Use reflection to set private fields since they might not have public setters
        var losManagerType = typeof(LineOfSightManager);
        
        SetFieldSafe(losManager, "obstacleLayerMask", obstacleLayerMask);
        SetFieldSafe(losManager, "raycastHeight", raycastHeight);
        SetFieldSafe(losManager, "raycastRadius", raycastRadius);
        SetFieldSafe(losManager, "useSphereCast", useSphereCast);
        SetFieldSafe(losManager, "enableDebugging", enableLineOfSightDebugging);
        SetFieldSafe(losManager, "requireLineOfSightForAttacks", requireLineOfSightForAttacks);
        SetFieldSafe(losManager, "allowAttackThroughDiagonalGaps", allowAttackThroughDiagonalGaps);
        SetFieldSafe(losManager, "lineOfSightTolerance", lineOfSightTolerance);
        SetFieldSafe(losManager, "cacheResults", cacheLineOfSightResults);
        SetFieldSafe(losManager, "maxRaycastsPerFrame", maxRaycastsPerFrame);
        SetFieldSafe(losManager, "updateInterval", lineOfSightUpdateInterval);
        SetFieldSafe(losManager, "enableRaycastVisualization", enableRaycastVisualization);
        SetFieldSafe(losManager, "logResults", logLineOfSightResults);
        SetFieldSafe(losManager, "showGizmos", showLineOfSightGizmos);

        Debug.Log("✓ LineOfSightManager configured with settings");
    }

    private void SetupLineOfSightVisualization(LineOfSightManager losManager)
    {
        // Add LineOfSightVisualizer if it doesn't exist
        LineOfSightVisualizer visualizer = losManager.GetComponent<LineOfSightVisualizer>();
        if (visualizer == null)
        {
            visualizer = losManager.gameObject.AddComponent<LineOfSightVisualizer>();
            Debug.Log("✓ Added LineOfSightVisualizer component");
        }

        // Configure visualizer
        SetFieldSafe(visualizer, "blockedLineColor", blockedLineColor);
        SetFieldSafe(visualizer, "clearLineColor", clearLineColor);
        SetFieldSafe(visualizer, "lineVisualizationDuration", lineVisualizationDuration);
        SetFieldSafe(visualizer, "showLineOfSightLines", showLineOfSightLines);
        SetFieldSafe(visualizer, "showObstacleBlockingFeedback", showObstacleBlockingFeedback);
        SetFieldSafe(visualizer, "lineOfSightMaterial", lineOfSightMaterial);

        Debug.Log("✓ LineOfSightVisualizer configured");
    }

    private void IntegrateWithCombatSystem()
    {
        CombatManager combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager == null) return;

        // Add LineOfSightValidator to CombatManager if it doesn't exist
        LineOfSightValidator losValidator = combatManager.GetComponent<LineOfSightValidator>();
        if (losValidator == null)
        {
            losValidator = combatManager.gameObject.AddComponent<LineOfSightValidator>();
            Debug.Log("✓ Added LineOfSightValidator to CombatManager");
        }

        // Ensure AttackValidator is aware of line-of-sight requirements
        AttackValidator attackValidator = combatManager.GetComponent<AttackValidator>();
        if (attackValidator != null)
        {
            SetFieldSafe(attackValidator, "requireLineOfSight", requireLineOfSightForAttacks);
            Debug.Log("✓ Updated AttackValidator with line-of-sight requirements");
        }

        Debug.Log("✓ Combat system integration complete");
    }

    private void SetupPerformanceOptimization(LineOfSightManager losManager)
    {
        // Add RaycastOptimizer if it doesn't exist
        RaycastOptimizer optimizer = losManager.GetComponent<RaycastOptimizer>();
        if (optimizer == null)
        {
            optimizer = losManager.gameObject.AddComponent<RaycastOptimizer>();
            Debug.Log("✓ Added RaycastOptimizer component");
        }

        // Configure optimizer
        SetFieldSafe(optimizer, "maxRaycastsPerFrame", maxRaycastsPerFrame);
        SetFieldSafe(optimizer, "enableOptimization", enableLineOfSightOptimization);
        SetFieldSafe(optimizer, "updateInterval", lineOfSightUpdateInterval);

        Debug.Log("✓ Performance optimization configured");
    }

    private void CreateLineOfSightMaterial()
    {
        // Create a simple unlit material for line-of-sight visualization
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.name = "LineOfSight_Material";
        mat.color = clearLineColor;

        // Save as asset
        string materialPath = "Assets/Materials/LineOfSight_Material.mat";
        AssetDatabase.CreateAsset(mat, materialPath);
        AssetDatabase.SaveAssets();

        lineOfSightMaterial = mat;
        Debug.Log($"✓ Created line-of-sight material at {materialPath}");
    }

    private void SetFieldSafe(MonoBehaviour component, string fieldName, object value)
    {
        try
        {
            var field = component.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(component, value);
            }
            else
            {
                // Try property instead
                var property = component.GetType().GetProperty(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (property != null && property.CanWrite)
                {
                    property.SetValue(component, value);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not set {fieldName} on {component.GetType().Name}: {e.Message}");
        }
    }

    private void ResetSetup()
    {
        Debug.Log("=== Resetting Line of Sight Setup ===");

        // Remove LineOfSightManager and related components
        LineOfSightManager losManager = FindFirstObjectByType<LineOfSightManager>();
        if (losManager != null)
        {
            DestroyImmediate(losManager.gameObject);
            Debug.Log("✓ Removed LineOfSightManager");
        }

        // Remove LineOfSightValidator from CombatManager
        CombatManager combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager != null)
        {
            LineOfSightValidator losValidator = combatManager.GetComponent<LineOfSightValidator>();
            if (losValidator != null)
            {
                DestroyImmediate(losValidator);
                Debug.Log("✓ Removed LineOfSightValidator from CombatManager");
            }
        }

        // Reset validation state
        hasValidatedPrerequisites = false;
        validationErrors.Clear();

        Debug.Log("✓ Line of Sight setup reset complete");
    }

    private void ValidateSetup()
    {
        Debug.Log("=== Validating Line of Sight Setup ===");

        bool allValid = true;
        List<string> issues = new List<string>();

        // Check LineOfSightManager exists
        LineOfSightManager losManager = FindFirstObjectByType<LineOfSightManager>();
        if (losManager == null)
        {
            issues.Add("LineOfSightManager not found");
            allValid = false;
        }
        else
        {
            Debug.Log("✓ LineOfSightManager found");

            // Check components
            if (losManager.GetComponent<LineOfSightVisualizer>() == null)
            {
                issues.Add("LineOfSightVisualizer missing from LineOfSightManager");
                allValid = false;
            }

            if (enableLineOfSightOptimization && losManager.GetComponent<RaycastOptimizer>() == null)
            {
                issues.Add("RaycastOptimizer missing from LineOfSightManager");
                allValid = false;
            }
        }

        // Check CombatManager integration
        CombatManager combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager == null)
        {
            issues.Add("CombatManager not found");
            allValid = false;
        }
        else
        {
            if (combatManager.GetComponent<LineOfSightValidator>() == null)
            {
                issues.Add("LineOfSightValidator missing from CombatManager");
                allValid = false;
            }
        }

        // Report results
        if (allValid)
        {
            Debug.Log("✓ All line-of-sight systems validated successfully");
            EditorUtility.DisplayDialog("Validation Successful", 
                "Line of Sight system is properly configured and ready to use!", "OK");
        }
        else
        {
            Debug.LogError("✗ Line of Sight validation failed:");
            foreach (string issue in issues)
            {
                Debug.LogError($"  - {issue}");
            }
            EditorUtility.DisplayDialog("Validation Failed", 
                $"Line of Sight system has {issues.Count} issues:\n\n" + 
                string.Join("\n", issues), "OK");
        }
    }

    private void TestLineOfSight()
    {
        Debug.Log("=== Testing Line of Sight ===");

        LineOfSightManager losManager = FindFirstObjectByType<LineOfSightManager>();
        if (losManager == null)
        {
            Debug.LogError("LineOfSightManager not found - run setup first");
            return;
        }

        // Find units for testing
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        if (units.Length < 2)
        {
            Debug.LogError("Need at least 2 units to test line-of-sight");
            return;
        }

        // Test line-of-sight between first two units
        Unit unit1 = units[0];
        Unit unit2 = units[1];

        Debug.Log($"Testing line-of-sight between {unit1.name} and {unit2.name}");

        // This will be implemented once LineOfSightManager is created
        // bool hasLineOfSight = losManager.HasLineOfSight(unit1.transform.position, unit2.transform.position);
        // Debug.Log($"Line of sight result: {hasLineOfSight}");
    }
}