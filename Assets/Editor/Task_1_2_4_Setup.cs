using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor automation tool for Task 1.2.4 - Movement Visual Feedback System.
/// Creates comprehensive visual feedback for movement including valid/invalid move highlighting,
/// enhanced animations, and professional tactical clarity. Completes Sub-milestone 1.2.
/// </summary>
public class Task_1_2_4_Setup : EditorWindow
{
    [Header("Visual Materials Configuration")]
    [SerializeField] private Material validMoveMaterial;
    [SerializeField] private Material invalidMoveMaterial;
    [SerializeField] private Material previewMoveMaterial;
    [SerializeField] private Color validMoveColor = Color.green;
    [SerializeField] private Color invalidMoveColor = Color.red;
    [SerializeField] private Color previewMoveColor = Color.yellow;
    
    [Header("Animation Enhancement Configuration")]
    [SerializeField] private float anticipationScale = 1.1f;
    [SerializeField] private float anticipationDuration = 0.1f;
    [SerializeField] private float bounceIntensity = 0.2f;
    [SerializeField] private bool enableMovementParticles = true;
    [SerializeField] private bool enableObstacleCollisionFeedback = true;
    
    [Header("Highlighting Configuration")]
    [SerializeField] private float highlightFadeSpeed = 2.0f;
    [SerializeField] private float highlightPulseSpeed = 1.5f;
    [SerializeField] private bool enableTileHighlightPulsing = true;
    [SerializeField] private bool showMovementPreview = true;
    [SerializeField] private float previewDelay = 0.2f;
    [SerializeField] private float highlightAlpha = 0.6f;
    
    [Header("Performance Settings")]
    [SerializeField] private int maxConcurrentHighlights = 16;
    [SerializeField] private bool useObjectPooling = true;
    [SerializeField] private bool optimizeForPerformance = true;
    
    [Header("Accessibility Settings")]
    [SerializeField] private bool enableColorblindFriendly = true;
    [SerializeField] private bool enableHighContrast = false;
    [SerializeField] private float visualEffectIntensity = 1.0f;
    
    // Validation state
    private bool validationComplete = false;
    private MovementVisualFeedbackValidationResult lastValidationResult;

    [MenuItem("Tactical Tools/Task 1.2.4 - Movement Visual Feedback")]
    public static void ShowWindow()
    {
        Task_1_2_4_Setup window = GetWindow<Task_1_2_4_Setup>("Task 1.2.4 Setup");
        window.minSize = new Vector2(500, 900);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Movement Visual Feedback System Setup", EditorStyles.largeLabel);
        EditorGUILayout.LabelField("Task 1.2.4 - Complete Sub-milestone 1.2 with professional visual feedback", EditorStyles.helpBox);
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // Visual Materials Configuration Section
        EditorGUILayout.LabelField("Visual Materials Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        validMoveMaterial = EditorGUILayout.ObjectField("Valid Move Material", validMoveMaterial, typeof(Material), false) as Material;
        invalidMoveMaterial = EditorGUILayout.ObjectField("Invalid Move Material", invalidMoveMaterial, typeof(Material), false) as Material;
        previewMoveMaterial = EditorGUILayout.ObjectField("Preview Move Material", previewMoveMaterial, typeof(Material), false) as Material;
        
        GUILayout.Space(5);
        EditorGUILayout.LabelField("Highlight Colors", EditorStyles.boldLabel);
        validMoveColor = EditorGUILayout.ColorField("Valid Move Color", validMoveColor);
        invalidMoveColor = EditorGUILayout.ColorField("Invalid Move Color", invalidMoveColor);
        previewMoveColor = EditorGUILayout.ColorField("Preview Move Color", previewMoveColor);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Animation Enhancement Configuration Section
        EditorGUILayout.LabelField("Animation Enhancement Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        anticipationScale = EditorGUILayout.Slider("Anticipation Scale", anticipationScale, 1.0f, 1.5f);
        anticipationDuration = EditorGUILayout.Slider("Anticipation Duration", anticipationDuration, 0.05f, 0.3f);
        bounceIntensity = EditorGUILayout.Slider("Bounce Intensity", bounceIntensity, 0.1f, 0.5f);
        enableMovementParticles = EditorGUILayout.Toggle("Enable Movement Particles", enableMovementParticles);
        enableObstacleCollisionFeedback = EditorGUILayout.Toggle("Enable Collision Feedback", enableObstacleCollisionFeedback);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Highlighting Configuration Section
        EditorGUILayout.LabelField("Highlighting Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        highlightFadeSpeed = EditorGUILayout.Slider("Highlight Fade Speed", highlightFadeSpeed, 0.5f, 5.0f);
        highlightPulseSpeed = EditorGUILayout.Slider("Highlight Pulse Speed", highlightPulseSpeed, 0.5f, 3.0f);
        enableTileHighlightPulsing = EditorGUILayout.Toggle("Enable Highlight Pulsing", enableTileHighlightPulsing);
        showMovementPreview = EditorGUILayout.Toggle("Show Movement Preview", showMovementPreview);
        previewDelay = EditorGUILayout.Slider("Preview Delay", previewDelay, 0.0f, 1.0f);
        highlightAlpha = EditorGUILayout.Slider("Highlight Alpha", highlightAlpha, 0.2f, 1.0f);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Performance Settings Section
        EditorGUILayout.LabelField("Performance Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        maxConcurrentHighlights = EditorGUILayout.IntSlider("Max Concurrent Highlights", maxConcurrentHighlights, 8, 32);
        useObjectPooling = EditorGUILayout.Toggle("Use Object Pooling", useObjectPooling);
        optimizeForPerformance = EditorGUILayout.Toggle("Optimize for Performance", optimizeForPerformance);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Accessibility Settings Section
        EditorGUILayout.LabelField("Accessibility Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        enableColorblindFriendly = EditorGUILayout.Toggle("Colorblind Friendly", enableColorblindFriendly);
        enableHighContrast = EditorGUILayout.Toggle("High Contrast Mode", enableHighContrast);
        visualEffectIntensity = EditorGUILayout.Slider("Visual Effect Intensity", visualEffectIntensity, 0.5f, 2.0f);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(15);
        
        // Action Buttons
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Setup Movement Visual Feedback", GUILayout.Height(40)))
        {
            SetupMovementVisualFeedback();
        }
        
        if (GUILayout.Button("Reset/Delete Setup", GUILayout.Height(40)))
        {
            ResetSetup();
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // Validation Section
        EditorGUILayout.LabelField("Validation & Testing", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Validate Visual Feedback System", GUILayout.Height(30)))
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
        EditorGUILayout.LabelField("1. Ensure Tasks 1.2.1, 1.2.2, and 1.2.3 are completed (Unit System, Selection, Movement)", EditorStyles.helpBox);
        EditorGUILayout.LabelField("2. Configure visual materials and colors (auto-generated if not provided)", EditorStyles.helpBox);
        EditorGUILayout.LabelField("3. Adjust animation enhancement settings for desired game feel", EditorStyles.helpBox);
        EditorGUILayout.LabelField("4. Configure highlighting behavior (pulsing, fading, preview timing)", EditorStyles.helpBox);
        EditorGUILayout.LabelField("5. Enable accessibility options for inclusive design", EditorStyles.helpBox);
        EditorGUILayout.LabelField("6. Click 'Setup Movement Visual Feedback' to create all systems", EditorStyles.helpBox);
        EditorGUILayout.LabelField("7. Use 'Validate Visual Feedback System' to test all functionality", EditorStyles.helpBox);
        EditorGUILayout.LabelField("8. Test in Play Mode: Select unit to see valid move highlights", EditorStyles.helpBox);
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Main setup method that creates the complete movement visual feedback system
    /// </summary>
    private void SetupMovementVisualFeedback()
    {
        Debug.Log("Setting up Movement Visual Feedback System for Task 1.2.4...");
        
        try
        {
            // Step 1: Create or update movement preview materials
            CreateMovementPreviewMaterials();
            
            // Step 2: Setup MovementPreviewSystem
            SetupMovementPreviewSystem();
            
            // Step 3: Setup MovementAnimationEnhancer
            SetupMovementAnimationEnhancer();
            
            // Step 4: Setup TileHighlighter components
            SetupTileHighlighters();
            
            // Step 5: Setup CollisionFeedbackSystem
            SetupCollisionFeedbackSystem();
            
            // Step 6: Enhance VisualFeedbackManager for movement integration
            EnhanceVisualFeedbackManager();
            
            // Step 7: Integrate with existing selection and movement systems
            IntegrateWithExistingSystems();
            
            Debug.Log("Movement Visual Feedback System setup complete!");
            EditorUtility.DisplayDialog("Setup Complete", 
                "Movement Visual Feedback System has been successfully configured!\n\n" +
                "Sub-milestone 1.2 is now complete with professional visual feedback.\n" +
                "Select units in Play Mode to see valid move highlighting.", "OK");
            
            // Run automatic validation
            ValidateSetup();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting up Movement Visual Feedback System: {e.Message}");
            EditorUtility.DisplayDialog("Setup Error", $"Failed to setup Movement Visual Feedback System:\n{e.Message}", "OK");
        }
    }

    /// <summary>
    /// Creates movement preview materials for highlighting
    /// </summary>
    private void CreateMovementPreviewMaterials()
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
        
        // Adjust colors for accessibility if enabled
        if (enableColorblindFriendly)
        {
            validMoveColor = new Color(0.0f, 0.5f, 1.0f, highlightAlpha); // Blue instead of green
            invalidMoveColor = new Color(1.0f, 0.3f, 0.0f, highlightAlpha); // Orange instead of red
            previewMoveColor = new Color(1.0f, 1.0f, 0.0f, highlightAlpha); // Keep yellow
        }
        
        if (enableHighContrast)
        {
            // Increase color intensity for high contrast mode
            validMoveColor = Color.Lerp(validMoveColor, Color.white, 0.3f);
            invalidMoveColor = Color.Lerp(invalidMoveColor, Color.black, 0.3f);
            previewMoveColor = Color.Lerp(previewMoveColor, Color.white, 0.2f);
        }
        
        // Create valid move material if not assigned
        if (validMoveMaterial == null)
        {
            validMoveMaterial = CreateHighlightMaterial("ValidMoveHighlight", validMoveColor);
            Debug.Log("Created valid move material");
        }
        
        // Create invalid move material if not assigned
        if (invalidMoveMaterial == null)
        {
            invalidMoveMaterial = CreateHighlightMaterial("InvalidMoveHighlight", invalidMoveColor);
            Debug.Log("Created invalid move material");
        }
        
        // Create preview move material if not assigned
        if (previewMoveMaterial == null)
        {
            previewMoveMaterial = CreateHighlightMaterial("PreviewMoveHighlight", previewMoveColor);
            Debug.Log("Created preview move material");
        }
    }

    /// <summary>
    /// Creates a highlight material with specified color and transparency
    /// </summary>
    private Material CreateHighlightMaterial(string name, Color color)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = name;
        
        // Configure for transparent highlighting
        material.SetFloat("_Surface", 1); // Transparent
        material.SetFloat("_Blend", 0); // Alpha blend
        material.SetFloat("_AlphaClip", 0); // No alpha clipping
        material.SetFloat("_Metallic", 0.0f);
        material.SetFloat("_Smoothness", 0.5f);
        
        // Apply color with alpha
        color.a = highlightAlpha;
        material.color = color;
        
        // Enable emission for glowing effect
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * (enableHighContrast ? 1.5f : 0.5f));
        
        material.renderQueue = 3000; // Transparent queue
        
        string path = $"Assets/Materials/Movement/{name}.mat";
        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();
        
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    /// <summary>
    /// Sets up the MovementPreviewSystem component
    /// </summary>
    private void SetupMovementPreviewSystem()
    {
        // Find MovementManager component instead of GameObject by name
        MovementManager movementManager = FindFirstObjectByType<MovementManager>();
        if (movementManager == null)
        {
            Debug.LogError("MovementManager not found! Ensure Task 1.2.3 is completed first.");
            return;
        }

        GameObject movementManagerObj = movementManager.gameObject;
        MovementPreviewSystem previewSystem = movementManagerObj.GetComponent<MovementPreviewSystem>();
        if (previewSystem == null)
        {
            previewSystem = movementManagerObj.AddComponent<MovementPreviewSystem>();
        }

        // Configure MovementPreviewSystem properties
        var serializedPreview = new SerializedObject(previewSystem);
        serializedPreview.FindProperty("enablePreview").boolValue = showMovementPreview;
        serializedPreview.FindProperty("previewDelay").floatValue = previewDelay;
        serializedPreview.FindProperty("validMoveMaterial").objectReferenceValue = validMoveMaterial;
        serializedPreview.FindProperty("invalidMoveMaterial").objectReferenceValue = invalidMoveMaterial;
        serializedPreview.FindProperty("previewMaterial").objectReferenceValue = previewMoveMaterial;
        serializedPreview.FindProperty("maxConcurrentPreviews").intValue = maxConcurrentHighlights;
        serializedPreview.FindProperty("fadeSpeed").floatValue = highlightFadeSpeed;
        serializedPreview.FindProperty("enablePulsing").boolValue = enableTileHighlightPulsing;
        serializedPreview.FindProperty("pulseSpeed").floatValue = highlightPulseSpeed;
        serializedPreview.ApplyModifiedProperties();

        Debug.Log("MovementPreviewSystem configured");
    }

    /// <summary>
    /// Sets up the MovementAnimationEnhancer component
    /// </summary>
    private void SetupMovementAnimationEnhancer()
    {
        // Find MovementManager component instead of GameObject by name
        MovementManager movementManager = FindFirstObjectByType<MovementManager>();
        if (movementManager == null)
        {
            Debug.LogError("MovementManager not found! Ensure Task 1.2.3 is completed first.");
            return;
        }

        GameObject movementManagerObj = movementManager.gameObject;
        MovementAnimationEnhancer enhancer = movementManagerObj.GetComponent<MovementAnimationEnhancer>();
        if (enhancer == null)
        {
            enhancer = movementManagerObj.AddComponent<MovementAnimationEnhancer>();
        }

        // Configure MovementAnimationEnhancer properties
        var serializedEnhancer = new SerializedObject(enhancer);
        serializedEnhancer.FindProperty("enableAnticipation").boolValue = true;
        serializedEnhancer.FindProperty("anticipationScale").floatValue = anticipationScale;
        serializedEnhancer.FindProperty("anticipationDuration").floatValue = anticipationDuration;
        serializedEnhancer.FindProperty("enableFollowThrough").boolValue = true;
        serializedEnhancer.FindProperty("bounceIntensity").floatValue = bounceIntensity;
        serializedEnhancer.FindProperty("enableParticles").boolValue = enableMovementParticles;
        serializedEnhancer.FindProperty("visualEffectIntensity").floatValue = visualEffectIntensity;
        serializedEnhancer.ApplyModifiedProperties();

        Debug.Log("MovementAnimationEnhancer configured");
    }

    /// <summary>
    /// Sets up TileHighlighter components on grid tiles
    /// </summary>
    private void SetupTileHighlighters()
    {
        Debug.Log("Setting up TileHighlighter components...");
        
        // Try multiple possible paths for grid tiles
        GameObject tilesGroup = GameObject.Find("Grid System/Tiles");
        Debug.Log($"Found 'Grid System/Tiles': {tilesGroup != null}");
        
        if (tilesGroup == null)
        {
            tilesGroup = GameObject.Find("Tiles");
            Debug.Log($"Found 'Tiles': {tilesGroup != null}");
        }
        
        if (tilesGroup == null)
        {
            Debug.Log("Trying to find GridTile components directly...");
            // Find all GameObjects with GridTile components
            GridTile[] allGridTiles = FindObjectsByType<GridTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"Found {allGridTiles.Length} GridTile components");
            
            if (allGridTiles.Length > 0)
            {
                // Setup TileHighlighter on all found grid tiles
                int gridTilesConfigured = 0;
                foreach (GridTile gridTile in allGridTiles)
                {
                    if (gridTile == null || gridTile.gameObject == null) continue;
                    
                    TileHighlighter highlighter = gridTile.GetComponent<TileHighlighter>();
                    if (highlighter == null)
                    {
                        highlighter = gridTile.gameObject.AddComponent<TileHighlighter>();
                        Debug.Log($"Added TileHighlighter to {gridTile.gameObject.name}");
                    }

                    // Configure TileHighlighter properties
                    try
                    {
                        var serializedHighlighter = new SerializedObject(highlighter);
                        var validMaterialProp = serializedHighlighter.FindProperty("validMoveMaterial");
                        var invalidMaterialProp = serializedHighlighter.FindProperty("invalidMoveMaterial");
                        var previewMaterialProp = serializedHighlighter.FindProperty("previewMaterial");
                        var fadeSpeedProp = serializedHighlighter.FindProperty("fadeSpeed");
                        var pulseSpeedProp = serializedHighlighter.FindProperty("pulseSpeed");
                        var enablePulsingProp = serializedHighlighter.FindProperty("enablePulsing");
                        var usePoolingProp = serializedHighlighter.FindProperty("useObjectPooling");
                        
                        if (validMaterialProp != null) validMaterialProp.objectReferenceValue = validMoveMaterial;
                        if (invalidMaterialProp != null) invalidMaterialProp.objectReferenceValue = invalidMoveMaterial;
                        if (previewMaterialProp != null) previewMaterialProp.objectReferenceValue = previewMoveMaterial;
                        if (fadeSpeedProp != null) fadeSpeedProp.floatValue = highlightFadeSpeed;
                        if (pulseSpeedProp != null) pulseSpeedProp.floatValue = highlightPulseSpeed;
                        if (enablePulsingProp != null) enablePulsingProp.boolValue = enableTileHighlightPulsing;
                        if (usePoolingProp != null) usePoolingProp.boolValue = useObjectPooling;
                        
                        serializedHighlighter.ApplyModifiedProperties();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Failed to configure TileHighlighter on {gridTile.gameObject.name}: {e.Message}");
                    }

                    gridTilesConfigured++;
                }
                Debug.Log($"TileHighlighter configured on {gridTilesConfigured} tiles via GridTile components");
                return;
            }
            
            Debug.LogError("Grid tiles not found! Ensure Task 1.1.2 is completed first. No GridTile components found in scene.");
            return;
        }

        int tilesConfigured = 0;
        for (int i = 0; i < tilesGroup.transform.childCount; i++)
        {
            Transform tileTransform = tilesGroup.transform.GetChild(i);
            if (tileTransform == null || tileTransform.gameObject == null) continue;
            
            TileHighlighter highlighter = tileTransform.GetComponent<TileHighlighter>();
            
            if (highlighter == null)
            {
                highlighter = tileTransform.gameObject.AddComponent<TileHighlighter>();
            }

            // Configure TileHighlighter properties
            try
            {
                var serializedHighlighter = new SerializedObject(highlighter);
                var validMaterialProp = serializedHighlighter.FindProperty("validMoveMaterial");
                var invalidMaterialProp = serializedHighlighter.FindProperty("invalidMoveMaterial");
                var previewMaterialProp = serializedHighlighter.FindProperty("previewMaterial");
                var fadeSpeedProp = serializedHighlighter.FindProperty("fadeSpeed");
                var pulseSpeedProp = serializedHighlighter.FindProperty("pulseSpeed");
                var enablePulsingProp = serializedHighlighter.FindProperty("enablePulsing");
                var usePoolingProp = serializedHighlighter.FindProperty("useObjectPooling");
                
                if (validMaterialProp != null) validMaterialProp.objectReferenceValue = validMoveMaterial;
                if (invalidMaterialProp != null) invalidMaterialProp.objectReferenceValue = invalidMoveMaterial;
                if (previewMaterialProp != null) previewMaterialProp.objectReferenceValue = previewMoveMaterial;
                if (fadeSpeedProp != null) fadeSpeedProp.floatValue = highlightFadeSpeed;
                if (pulseSpeedProp != null) pulseSpeedProp.floatValue = highlightPulseSpeed;
                if (enablePulsingProp != null) enablePulsingProp.boolValue = enableTileHighlightPulsing;
                if (usePoolingProp != null) usePoolingProp.boolValue = useObjectPooling;
                
                serializedHighlighter.ApplyModifiedProperties();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to configure TileHighlighter on {tileTransform.name}: {e.Message}");
            }

            tilesConfigured++;
        }

        Debug.Log($"TileHighlighter configured on {tilesConfigured} tiles");
    }

    /// <summary>
    /// Sets up the CollisionFeedbackSystem component
    /// </summary>
    private void SetupCollisionFeedbackSystem()
    {
        // Find MovementManager component instead of GameObject by name
        MovementManager movementManager = FindFirstObjectByType<MovementManager>();
        if (movementManager == null)
        {
            Debug.LogError("MovementManager not found! Ensure Task 1.2.3 is completed first.");
            return;
        }

        GameObject movementManagerObj = movementManager.gameObject;
        CollisionFeedbackSystem collisionSystem = movementManagerObj.GetComponent<CollisionFeedbackSystem>();
        if (collisionSystem == null)
        {
            collisionSystem = movementManagerObj.AddComponent<CollisionFeedbackSystem>();
        }

        // Configure CollisionFeedbackSystem properties
        var serializedCollision = new SerializedObject(collisionSystem);
        serializedCollision.FindProperty("enableCollisionFeedback").boolValue = enableObstacleCollisionFeedback;
        serializedCollision.FindProperty("bounceIntensity").floatValue = bounceIntensity;
        serializedCollision.FindProperty("invalidMoveMaterial").objectReferenceValue = invalidMoveMaterial;
        serializedCollision.FindProperty("enableParticles").boolValue = enableMovementParticles;
        serializedCollision.FindProperty("visualEffectIntensity").floatValue = visualEffectIntensity;
        serializedCollision.ApplyModifiedProperties();

        Debug.Log("CollisionFeedbackSystem configured");
    }

    /// <summary>
    /// Enhances the existing VisualFeedbackManager for movement integration
    /// </summary>
    private void EnhanceVisualFeedbackManager()
    {
        VisualFeedbackManager feedbackManager = FindFirstObjectByType<VisualFeedbackManager>();
        if (feedbackManager == null)
        {
            Debug.LogWarning("VisualFeedbackManager not found. Creating new one...");
            GameObject feedbackObj = new GameObject("Visual Feedback Manager");
            feedbackManager = feedbackObj.AddComponent<VisualFeedbackManager>();
        }

        // Configure VisualFeedbackManager for movement integration
        var serializedFeedback = new SerializedObject(feedbackManager);
        serializedFeedback.FindProperty("enableVisualFeedback").boolValue = true;
        serializedFeedback.FindProperty("feedbackIntensity").floatValue = visualEffectIntensity;
        serializedFeedback.FindProperty("maxConcurrentEffects").intValue = maxConcurrentHighlights;
        serializedFeedback.FindProperty("useObjectPooling").boolValue = useObjectPooling;
        serializedFeedback.ApplyModifiedProperties();

        Debug.Log("VisualFeedbackManager enhanced for movement integration");
    }

    /// <summary>
    /// Integrates visual feedback with existing selection and movement systems
    /// </summary>
    private void IntegrateWithExistingSystems()
    {
        // Integrate with SelectionManager
        SelectionManager selectionManager = FindFirstObjectByType<SelectionManager>();
        MovementManager movementManager = FindFirstObjectByType<MovementManager>();
        MovementPreviewSystem previewSystem = FindFirstObjectByType<MovementPreviewSystem>();

        if (selectionManager == null)
        {
            Debug.LogError("SelectionManager not found! Ensure Task 1.2.2 is completed first.");
            return;
        }

        if (movementManager == null)
        {
            Debug.LogError("MovementManager not found! Ensure Task 1.2.3 is completed first.");
            return;
        }

        if (previewSystem != null)
        {
            // Configure movement manager to reference preview system
            var serializedMovement = new SerializedObject(movementManager);
            serializedMovement.FindProperty("validMoveMaterial").objectReferenceValue = validMoveMaterial;
            serializedMovement.FindProperty("invalidMoveMaterial").objectReferenceValue = invalidMoveMaterial;
            serializedMovement.ApplyModifiedProperties();

            Debug.Log("Visual feedback systems integrated");
        }
    }

    /// <summary>
    /// Validates the movement visual feedback system setup
    /// </summary>
    private void ValidateSetup()
    {
        lastValidationResult = new MovementVisualFeedbackValidationResult();
        
        // Test 1: MovementPreviewSystem exists
        MovementPreviewSystem previewSystem = FindFirstObjectByType<MovementPreviewSystem>();
        lastValidationResult.previewSystemExists = previewSystem != null;
        
        // Test 2: MovementAnimationEnhancer exists
        MovementAnimationEnhancer enhancer = FindFirstObjectByType<MovementAnimationEnhancer>();
        lastValidationResult.animationEnhancerExists = enhancer != null;
        
        // Test 3: CollisionFeedbackSystem exists
        CollisionFeedbackSystem collisionSystem = FindFirstObjectByType<CollisionFeedbackSystem>();
        lastValidationResult.collisionSystemExists = collisionSystem != null;
        
        // Test 4: TileHighlighter components exist
        ValidateTileHighlighters();
        
        // Test 5: Materials exist and are configured
        ValidateMovementMaterials();
        
        // Test 6: System integration
        ValidateSystemIntegration();
        
        lastValidationResult.CalculateOverallResult();
        validationComplete = true;
        
        // Log results
        if (lastValidationResult.overallSuccess)
        {
            Debug.Log("✓ Movement Visual Feedback System validation passed!");
        }
        else
        {
            Debug.LogWarning($"Movement Visual Feedback System validation issues found. Passed: {lastValidationResult.GetPassedCount()}/6");
        }
    }

    /// <summary>
    /// Validates TileHighlighter components on grid tiles
    /// </summary>
    private void ValidateTileHighlighters()
    {
        lastValidationResult.tileHighlightersValid = false;
        int highlightersFound = 0;
        
        Debug.Log("Validating TileHighlighter components...");
        
        // Try to find TileHighlighter components directly
        TileHighlighter[] allHighlighters = FindObjectsByType<TileHighlighter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        highlightersFound = allHighlighters.Length;
        Debug.Log($"Found {highlightersFound} TileHighlighter components directly");
        
        if (highlightersFound == 0)
        {
            Debug.Log("No TileHighlighter found directly, trying path-based approach...");
            // Try the traditional path-based approach
            GameObject tilesGroup = GameObject.Find("Grid System/Tiles");
            Debug.Log($"Found 'Grid System/Tiles': {tilesGroup != null}");
            
            if (tilesGroup == null)
            {
                tilesGroup = GameObject.Find("Tiles");
                Debug.Log($"Found 'Tiles': {tilesGroup != null}");
            }
            
            if (tilesGroup != null)
            {
                Debug.Log($"Checking {tilesGroup.transform.childCount} children in {tilesGroup.name}");
                for (int i = 0; i < tilesGroup.transform.childCount; i++)
                {
                    Transform tileTransform = tilesGroup.transform.GetChild(i);
                    if (tileTransform == null || tileTransform.gameObject == null) continue;
                    
                    TileHighlighter highlighter = tileTransform.GetComponent<TileHighlighter>();
                    if (highlighter != null)
                    {
                        highlightersFound++;
                        Debug.Log($"Found TileHighlighter on {tileTransform.name}");
                    }
                    else
                    {
                        Debug.Log($"No TileHighlighter on {tileTransform.name}");
                    }
                }
            }
            else
            {
                // Check for GridTile components to see what we're working with
                GridTile[] gridTiles = FindObjectsByType<GridTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Debug.Log($"Found {gridTiles.Length} GridTile components in scene");
                foreach (GridTile tile in gridTiles)
                {
                    if (tile != null)
                    {
                        Debug.Log($"GridTile found: {tile.gameObject.name}");
                        TileHighlighter highlighter = tile.GetComponent<TileHighlighter>();
                        Debug.Log($"  Has TileHighlighter: {highlighter != null}");
                    }
                }
            }
        }

        lastValidationResult.tileHighlightersValid = highlightersFound > 0;
        
        if (lastValidationResult.tileHighlightersValid)
        {
            Debug.Log($"✓ Found {highlightersFound} TileHighlighter components");
        }
        else
        {
            Debug.LogError("No TileHighlighter components found");
        }
    }

    /// <summary>
    /// Validates movement materials
    /// </summary>
    private void ValidateMovementMaterials()
    {
        lastValidationResult.movementMaterialsValid = 
            validMoveMaterial != null && 
            invalidMoveMaterial != null && 
            previewMoveMaterial != null;
        
        if (!lastValidationResult.movementMaterialsValid)
        {
            Debug.LogError("Movement materials are missing or invalid");
        }
    }

    /// <summary>
    /// Validates system integration
    /// </summary>
    private void ValidateSystemIntegration()
    {
        SelectionManager selectionManager = FindFirstObjectByType<SelectionManager>();
        MovementManager movementManager = FindFirstObjectByType<MovementManager>();
        VisualFeedbackManager feedbackManager = FindFirstObjectByType<VisualFeedbackManager>();
        
        lastValidationResult.systemIntegrationValid = 
            selectionManager != null && 
            movementManager != null && 
            feedbackManager != null;
        
        if (!lastValidationResult.systemIntegrationValid)
        {
            Debug.LogError("System integration validation failed - missing core managers");
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
        DrawValidationItem("Movement Preview System", lastValidationResult.previewSystemExists);
        DrawValidationItem("Animation Enhancer", lastValidationResult.animationEnhancerExists);
        DrawValidationItem("Collision Feedback System", lastValidationResult.collisionSystemExists);
        DrawValidationItem("Tile Highlighters", lastValidationResult.tileHighlightersValid);
        DrawValidationItem("Movement Materials", lastValidationResult.movementMaterialsValid);
        DrawValidationItem("System Integration", lastValidationResult.systemIntegrationValid);
        
        if (lastValidationResult.overallSuccess)
        {
            EditorGUILayout.Space(5);
            GUI.color = Color.green;
            EditorGUILayout.LabelField("🎉 Sub-milestone 1.2 COMPLETE! Professional tactical unit system ready for combat mechanics.", EditorStyles.helpBox);
            GUI.color = originalColor;
        }
        
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
    /// Resets/deletes the movement visual feedback system setup
    /// </summary>
    private void ResetSetup()
    {
        if (EditorUtility.DisplayDialog("Reset Movement Visual Feedback System", 
            "This will remove all visual feedback components and materials. Continue?", 
            "Yes", "Cancel"))
        {
            try
            {
                // Remove MovementPreviewSystem
                MovementPreviewSystem previewSystem = FindFirstObjectByType<MovementPreviewSystem>();
                if (previewSystem != null)
                {
                    DestroyImmediate(previewSystem);
                }
                
                // Remove MovementAnimationEnhancer
                MovementAnimationEnhancer enhancer = FindFirstObjectByType<MovementAnimationEnhancer>();
                if (enhancer != null)
                {
                    DestroyImmediate(enhancer);
                }
                
                // Remove CollisionFeedbackSystem
                CollisionFeedbackSystem collisionSystem = FindFirstObjectByType<CollisionFeedbackSystem>();
                if (collisionSystem != null)
                {
                    DestroyImmediate(collisionSystem);
                }
                
                // Remove TileHighlighter components
                TileHighlighter[] allHighlighters = FindObjectsByType<TileHighlighter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (TileHighlighter highlighter in allHighlighters)
                {
                    if (highlighter != null)
                    {
                        DestroyImmediate(highlighter);
                    }
                }
                
                // Delete movement materials
                DeleteMaterialIfExists(validMoveMaterial);
                DeleteMaterialIfExists(invalidMoveMaterial);
                DeleteMaterialIfExists(previewMoveMaterial);
                
                // Reset references
                validMoveMaterial = null;
                invalidMoveMaterial = null;
                previewMoveMaterial = null;
                validationComplete = false;
                
                Debug.Log("Movement Visual Feedback System reset complete");
                EditorUtility.DisplayDialog("Reset Complete", "Movement Visual Feedback System has been reset.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error resetting visual feedback system: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Deletes a material asset if it exists
    /// </summary>
    private void DeleteMaterialIfExists(Material material)
    {
        if (material != null)
        {
            string path = AssetDatabase.GetAssetPath(material);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}

/// <summary>
/// Structure to hold movement visual feedback validation results
/// </summary>
[System.Serializable]
public class MovementVisualFeedbackValidationResult
{
    public bool previewSystemExists = false;
    public bool animationEnhancerExists = false;
    public bool collisionSystemExists = false;
    public bool tileHighlightersValid = false;
    public bool movementMaterialsValid = false;
    public bool systemIntegrationValid = false;
    public bool overallSuccess = false;
    
    public void CalculateOverallResult()
    {
        overallSuccess = previewSystemExists && 
                        animationEnhancerExists && 
                        collisionSystemExists && 
                        tileHighlightersValid && 
                        movementMaterialsValid && 
                        systemIntegrationValid;
    }
    
    public int GetPassedCount()
    {
        int count = 0;
        if (previewSystemExists) count++;
        if (animationEnhancerExists) count++;
        if (collisionSystemExists) count++;
        if (tileHighlightersValid) count++;
        if (movementMaterialsValid) count++;
        if (systemIntegrationValid) count++;
        return count;
    }
}