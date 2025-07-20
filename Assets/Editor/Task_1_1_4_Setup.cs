using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

public class Task_1_1_4_Setup : EditorWindow
{
    [Header("Material Configuration")]
    [SerializeField] private Color gridLineColor = Color.white;
    [SerializeField] private Color gridTileDefaultColor = new Color(0.9f, 0.9f, 0.9f, 0.1f);
    [SerializeField] private Color gridTileHoverColor = Color.yellow;
    [SerializeField] private Color gridTileSelectedColor = Color.green;
    [SerializeField] private Color gridTileBlockedColor = Color.red;
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.3f, 0.4f);
    
    [Header("Lighting Configuration")]
    [SerializeField] private float lightIntensity = 1.2f;
    [SerializeField] private float ambientIntensity = 0.3f;
    [SerializeField] private Color lightColor = Color.white;
    [SerializeField] private Color ambientColor = new Color(0.5f, 0.5f, 0.8f);
    [SerializeField] private Vector3 lightRotation = new Vector3(50f, -30f, 0f);
    
    [Header("Visual Feedback Configuration")]
    [SerializeField] private bool enableVisualFeedback = true;
    [SerializeField] private bool enableHoverEffects = true;
    [SerializeField] private bool enableSelectionEffects = true;
    [SerializeField] private float feedbackIntensity = 1.0f;
    [SerializeField] private float feedbackSpeed = 2.0f;
    
    [Header("Performance Optimization")]
    [SerializeField] private bool optimizePerformance = true;
    [SerializeField] private bool enableBatching = true;
    [SerializeField] private bool optimizeShadows = true;
    [SerializeField] private int targetFrameRate = 60;
    
    [Header("Quality Settings")]
    [SerializeField] private bool enableAntiAliasing = true;
    [SerializeField] private bool enableHDR = false;
    [SerializeField] private ShadowQuality shadowQuality = ShadowQuality.All;
    [SerializeField] private int vSyncCount = 1;
    
    [Header("Material Style")]
    [SerializeField] private bool useCleanAesthetic = true;
    
    private Vector2 scrollPosition;
    private bool validationResults = false;
    
    [MenuItem("Tactical Tools/Task 1.1.4 - Environment Polish")]
    public static void ShowWindow()
    {
        Task_1_1_4_Setup window = GetWindow<Task_1_1_4_Setup>("Task 1.1.4 Setup");
        window.minSize = new Vector2(500, 900);
        window.Show();
    }
    
    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Space(10);
        
        // Header
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("3D Tactical Arena - Environment Polish", headerStyle);
        GUILayout.Space(20);
        
        // Material Configuration Section
        EditorGUILayout.LabelField("Material Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        gridLineColor = EditorGUILayout.ColorField("Grid Line Color", gridLineColor);
        gridTileDefaultColor = EditorGUILayout.ColorField("Grid Tile Default", gridTileDefaultColor);
        gridTileHoverColor = EditorGUILayout.ColorField("Grid Tile Hover", gridTileHoverColor);
        gridTileSelectedColor = EditorGUILayout.ColorField("Grid Tile Selected", gridTileSelectedColor);
        gridTileBlockedColor = EditorGUILayout.ColorField("Grid Tile Blocked", gridTileBlockedColor);
        backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Lighting Configuration Section
        EditorGUILayout.LabelField("Lighting Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        lightIntensity = EditorGUILayout.Slider("Light Intensity", lightIntensity, 0.5f, 3.0f);
        ambientIntensity = EditorGUILayout.Slider("Ambient Intensity", ambientIntensity, 0.0f, 1.0f);
        lightColor = EditorGUILayout.ColorField("Light Color", lightColor);
        ambientColor = EditorGUILayout.ColorField("Ambient Color", ambientColor);
        lightRotation = EditorGUILayout.Vector3Field("Light Rotation", lightRotation);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Visual Feedback Configuration Section
        EditorGUILayout.LabelField("Visual Feedback Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        enableVisualFeedback = EditorGUILayout.Toggle("Enable Visual Feedback", enableVisualFeedback);
        if (enableVisualFeedback)
        {
            enableHoverEffects = EditorGUILayout.Toggle("Enable Hover Effects", enableHoverEffects);
            enableSelectionEffects = EditorGUILayout.Toggle("Enable Selection Effects", enableSelectionEffects);
            feedbackIntensity = EditorGUILayout.Slider("Feedback Intensity", feedbackIntensity, 0.1f, 2.0f);
            feedbackSpeed = EditorGUILayout.Slider("Feedback Speed", feedbackSpeed, 0.5f, 5.0f);
        }
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Performance Optimization Section
        EditorGUILayout.LabelField("Performance Optimization", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        optimizePerformance = EditorGUILayout.Toggle("Optimize Performance", optimizePerformance);
        if (optimizePerformance)
        {
            enableBatching = EditorGUILayout.Toggle("Enable Batching", enableBatching);
            optimizeShadows = EditorGUILayout.Toggle("Optimize Shadows", optimizeShadows);
            targetFrameRate = EditorGUILayout.IntSlider("Target Frame Rate", targetFrameRate, 30, 120);
        }
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Quality Settings Section
        EditorGUILayout.LabelField("Quality Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        enableAntiAliasing = EditorGUILayout.Toggle("Enable Anti-Aliasing", enableAntiAliasing);
        enableHDR = EditorGUILayout.Toggle("Enable HDR", enableHDR);
        shadowQuality = (ShadowQuality)EditorGUILayout.EnumPopup("Shadow Quality", shadowQuality);
        vSyncCount = EditorGUILayout.IntSlider("VSync Count", vSyncCount, 0, 2);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(20);
        
        // Action Buttons
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fixedHeight = 30
        };
        
        if (GUILayout.Button("Setup Environment Polish", buttonStyle))
        {
            SetupEnvironmentPolish();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Validate Setup", buttonStyle))
        {
            ValidateSetup();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Reset/Delete Polish", buttonStyle))
        {
            if (EditorUtility.DisplayDialog("Reset Environment Polish", 
                "This will remove all polish elements and return to basic environment. Continue?", 
                "Yes", "Cancel"))
            {
                ResetSetup();
            }
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Apply Performance Settings", buttonStyle))
        {
            ApplyPerformanceSettings();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Convert Materials to URP", buttonStyle))
        {
            ConvertMaterialsToURP();
        }
        
        GUILayout.Space(20);
        
        // Current Status Section
        EditorGUILayout.LabelField("Current Environment Status", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        // Check for required systems
        GameObject gridSystem = GameObject.Find("Grid System");
        if (gridSystem != null)
        {
            EditorGUILayout.LabelField("✓ Grid System Found");
            GridManager gridManager = gridSystem.GetComponent<GridManager>();
            if (gridManager != null)
            {
                EditorGUILayout.LabelField($"✓ Grid Manager: {gridManager.GridWidth}x{gridManager.GridHeight}");
            }
        }
        else
        {
            EditorGUILayout.LabelField("✗ Grid System Missing - Run Task 1.1.2 first");
        }
        
        GameObject obstacleSystem = GameObject.Find("Obstacle System");
        if (obstacleSystem != null)
        {
            EditorGUILayout.LabelField("✓ Obstacle System Found");
            ObstacleManager obstacleManager = obstacleSystem.GetComponent<ObstacleManager>();
            if (obstacleManager != null)
            {
                EditorGUILayout.LabelField($"✓ Obstacles: {obstacleManager.ObstacleCount}");
            }
        }
        else
        {
            EditorGUILayout.LabelField("⚠ Obstacle System Missing - Run Task 1.1.3 for full polish");
        }
        
        // Check camera setup
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            EditorGUILayout.LabelField($"✓ Camera: {(mainCamera.orthographic ? "Orthographic" : "Perspective")}");
        }
        else
        {
            EditorGUILayout.LabelField("✗ Main Camera Missing");
        }
        
        // Check materials
        GameObject materialManager = GameObject.Find("Material Manager");
        if (materialManager != null)
        {
            EditorGUILayout.LabelField("✓ Material Manager Active");
        }
        else
        {
            EditorGUILayout.LabelField("○ Material Manager Not Yet Created");
        }
        
        // Performance info
        EditorGUILayout.LabelField($"Target FPS: {Application.targetFrameRate}");
        EditorGUILayout.LabelField($"VSync: {QualitySettings.vSyncCount}");
        
        EditorGUI.indentLevel--;
        
        EditorGUILayout.EndScrollView();
    }
    
    private void SetupEnvironmentPolish()
    {
        Debug.Log("=== Task 1.1.4: Setting up Environment Polish ===");
        
        // Validate prerequisites
        if (!ValidatePrerequisites())
        {
            return;
        }
        
        // Create or update material manager
        SetupMaterialManager();
        
        // Create or update visual feedback manager
        SetupVisualFeedbackManager();
        
        // Apply materials to all environmental elements
        ApplyEnvironmentMaterials();
        
        // Optimize lighting setup
        OptimizeLighting();
        
        // Apply performance settings
        if (optimizePerformance)
        {
            ApplyPerformanceSettings();
        }
        
        // Setup visual feedback systems
        if (enableVisualFeedback)
        {
            SetupVisualFeedback();
        }
        
        // Mark scene as dirty
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        
        Debug.Log("=== Environment Polish setup complete! ===");
        EditorUtility.DisplayDialog("Setup Complete", 
            $"Environment Polish has been applied!\\n\\n" +
            $"✓ Materials applied with clean geometric aesthetic\\n" +
            $"✓ Lighting optimized for tactical clarity\\n" +
            $"✓ Visual feedback systems configured\\n" +
            $"✓ Performance settings optimized\\n\\n" +
            "Sub-milestone 1.1 foundation complete!", "OK");
    }
    
    private bool ValidatePrerequisites()
    {
        bool isValid = true;
        string missingElements = "";
        
        if (GameObject.Find("Grid System") == null)
        {
            missingElements += "- Grid System (Run Task 1.1.2)\\n";
            isValid = false;
        }
        
        if (Camera.main == null)
        {
            missingElements += "- Main Camera (Run Task 1.1.1)\\n";
            isValid = false;
        }
        
        if (!isValid)
        {
            EditorUtility.DisplayDialog("Prerequisites Missing", 
                $"Please complete the following tasks first:\\n\\n{missingElements}", "OK");
        }
        
        return isValid;
    }
    
    private void SetupMaterialManager()
    {
        GameObject materialManagerObj = GameObject.Find("Material Manager");
        if (materialManagerObj == null)
        {
            // Create under Environment group
            GameObject environmentGroup = GameObject.Find("Environment");
            if (environmentGroup == null)
            {
                environmentGroup = new GameObject("Environment");
            }
            
            materialManagerObj = new GameObject("Material Manager");
            materialManagerObj.transform.SetParent(environmentGroup.transform);
        }
        
        MaterialManager materialManager = materialManagerObj.GetComponent<MaterialManager>();
        if (materialManager == null)
        {
            materialManager = materialManagerObj.AddComponent<MaterialManager>();
        }
        
        // Configure material manager
        var serializedManager = new SerializedObject(materialManager);
        serializedManager.FindProperty("gridLineColor").colorValue = gridLineColor;
        serializedManager.FindProperty("gridTileDefaultColor").colorValue = gridTileDefaultColor;
        serializedManager.FindProperty("gridTileHoverColor").colorValue = gridTileHoverColor;
        serializedManager.FindProperty("gridTileSelectedColor").colorValue = gridTileSelectedColor;
        serializedManager.FindProperty("gridTileBlockedColor").colorValue = gridTileBlockedColor;
        serializedManager.FindProperty("backgroundColor").colorValue = backgroundColor;
        serializedManager.ApplyModifiedProperties();
        
        Debug.Log("Material Manager configured");
    }
    
    private void SetupVisualFeedbackManager()
    {
        GameObject feedbackManagerObj = GameObject.Find("Visual Feedback Manager");
        if (feedbackManagerObj == null)
        {
            GameObject environmentGroup = GameObject.Find("Environment");
            if (environmentGroup == null)
            {
                environmentGroup = new GameObject("Environment");
            }
            
            feedbackManagerObj = new GameObject("Visual Feedback Manager");
            feedbackManagerObj.transform.SetParent(environmentGroup.transform);
        }
        
        VisualFeedbackManager feedbackManager = feedbackManagerObj.GetComponent<VisualFeedbackManager>();
        if (feedbackManager == null)
        {
            feedbackManager = feedbackManagerObj.AddComponent<VisualFeedbackManager>();
        }
        
        // Configure visual feedback manager
        var serializedFeedback = new SerializedObject(feedbackManager);
        serializedFeedback.FindProperty("enableVisualFeedback").boolValue = enableVisualFeedback;
        serializedFeedback.FindProperty("enableHoverEffects").boolValue = enableHoverEffects;
        serializedFeedback.FindProperty("enableSelectionEffects").boolValue = enableSelectionEffects;
        serializedFeedback.FindProperty("feedbackIntensity").floatValue = feedbackIntensity;
        serializedFeedback.FindProperty("feedbackSpeed").floatValue = feedbackSpeed;
        serializedFeedback.ApplyModifiedProperties();
        
        Debug.Log("Visual Feedback Manager configured");
    }
    
    private void ApplyEnvironmentMaterials()
    {
        // Update materials
        UpdateGridMaterials();
        UpdateObstacleMaterials();
        UpdateCameraMaterials();
        
        Debug.Log("Environment materials applied");
    }
    
    private void UpdateGridMaterials()
    {
        // Update existing grid materials with new colors
        string[] materialNames = { "GridLine", "GridTile_Default", "GridTile_Hover", "GridTile_Selected", "GridTile_Blocked" };
        Color[] colors = { gridLineColor, gridTileDefaultColor, gridTileHoverColor, gridTileSelectedColor, gridTileBlockedColor };
        
        for (int i = 0; i < materialNames.Length; i++)
        {
            string materialPath = $"Assets/Materials/{materialNames[i]}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            
            if (material != null)
            {
                material.color = colors[i];
                EditorUtility.SetDirty(material);
            }
            else if (i < 2) // Create essential materials if missing
            {
                CreateMaterial(materialNames[i], colors[i]);
            }
        }
        
        AssetDatabase.SaveAssets();
    }
    
    private void UpdateObstacleMaterials()
    {
        // Apply consistent material treatment to obstacles
        string[] obstacleTypes = { "Obstacle_LowCover", "Obstacle_HighWall", "Obstacle_Terrain" };
        
        foreach (string typeName in obstacleTypes)
        {
            string materialPath = $"Assets/Materials/{typeName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            
            if (material != null)
            {
                // Enhance material properties for clean aesthetic
                material.SetFloat("_Metallic", 0.1f);
                material.SetFloat("_Smoothness", 0.2f);
                EditorUtility.SetDirty(material);
            }
        }
        
        AssetDatabase.SaveAssets();
    }
    
    private void UpdateCameraMaterials()
    {
        // Set camera background color
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = backgroundColor;
            EditorUtility.SetDirty(mainCamera);
        }
    }
    
    private void OptimizeLighting()
    {
        // Configure main directional light
        Light[] lights = FindObjectsOfType<Light>();
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
            directionalLight.intensity = lightIntensity;
            directionalLight.color = lightColor;
            directionalLight.transform.rotation = Quaternion.Euler(lightRotation);
            
            if (optimizeShadows)
            {
                directionalLight.shadows = LightShadows.Soft;
                directionalLight.shadowStrength = 0.5f;
            }
            
            EditorUtility.SetDirty(directionalLight);
        }
        
        // Configure ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
        RenderSettings.ambientIntensity = ambientIntensity;
        
        Debug.Log("Lighting optimized for tactical clarity");
    }
    
    private void SetupVisualFeedback()
    {
        // Ensure visual feedback manager is properly connected to grid tiles
        GameObject gridSystem = GameObject.Find("Grid System");
        if (gridSystem != null)
        {
            GridManager gridManager = gridSystem.GetComponent<GridManager>();
            VisualFeedbackManager feedbackManager = GameObject.Find("Visual Feedback Manager")?.GetComponent<VisualFeedbackManager>();
            
            if (gridManager != null && feedbackManager != null)
            {
                // Connect grid manager to feedback manager
                var serializedGrid = new SerializedObject(gridManager);
                serializedGrid.FindProperty("visualFeedbackManager").objectReferenceValue = feedbackManager;
                serializedGrid.ApplyModifiedProperties();
            }
        }
        
        Debug.Log("Visual feedback systems configured");
    }
    
    private void ApplyPerformanceSettings()
    {
        // Application settings
        Application.targetFrameRate = targetFrameRate;
        
        // Quality settings
        QualitySettings.vSyncCount = vSyncCount;
        QualitySettings.shadows = shadowQuality;
        
        if (enableAntiAliasing)
        {
            QualitySettings.antiAliasing = 4;
        }
        else
        {
            QualitySettings.antiAliasing = 0;
        }
        
        // Batching settings
        if (enableBatching)
        {
            PlayerSettings.gpuSkinning = true;
        }
        
        Debug.Log($"Performance settings applied - Target FPS: {targetFrameRate}");
    }
    
    private void CreateMaterial(string materialName, Color color)
    {
        // Create Materials folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = materialName;
        material.color = color;
        
        // Configure for clean aesthetic
        material.SetFloat("_Metallic", 0.0f);
        material.SetFloat("_Smoothness", 0.1f);
        
        string materialPath = $"Assets/Materials/{materialName}.mat";
        AssetDatabase.CreateAsset(material, materialPath);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Created material: {materialPath}");
    }
    
    private void ValidateSetup()
    {
        Debug.Log("=== Validating Environment Polish Setup ===");
        
        bool validationPassed = true;
        string validationReport = "Environment Polish Validation Results:\\n\\n";
        
        // Check material manager
        GameObject materialManager = GameObject.Find("Material Manager");
        if (materialManager != null && materialManager.GetComponent<MaterialManager>() != null)
        {
            validationReport += "✓ Material Manager configured\\n";
        }
        else
        {
            validationReport += "✗ Material Manager missing\\n";
            validationPassed = false;
        }
        
        // Check visual feedback manager
        GameObject feedbackManager = GameObject.Find("Visual Feedback Manager");
        if (feedbackManager != null && feedbackManager.GetComponent<VisualFeedbackManager>() != null)
        {
            validationReport += "✓ Visual Feedback Manager configured\\n";
        }
        else
        {
            validationReport += "✗ Visual Feedback Manager missing\\n";
            validationPassed = false;
        }
        
        // Check materials
        string[] requiredMaterials = { "GridLine", "GridTile_Default" };
        foreach (string matName in requiredMaterials)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{matName}.mat") != null)
            {
                validationReport += $"✓ Material {matName} found\\n";
            }
            else
            {
                validationReport += $"✗ Material {matName} missing\\n";
                validationPassed = false;
            }
        }
        
        // Check lighting
        Light directionalLight = FindObjectOfType<Light>();
        if (directionalLight != null && directionalLight.type == LightType.Directional)
        {
            validationReport += "✓ Directional light configured\\n";
        }
        else
        {
            validationReport += "✗ Directional light missing or misconfigured\\n";
            validationPassed = false;
        }
        
        // Check performance settings
        if (Application.targetFrameRate == targetFrameRate)
        {
            validationReport += $"✓ Target FPS set to {targetFrameRate}\\n";
        }
        else
        {
            validationReport += "⚠ Target FPS not set\\n";
        }
        
        validationReport += "\\n";
        if (validationPassed)
        {
            validationReport += "✓ All validation checks passed! Environment polish ready for tactical gameplay.";
            Debug.Log(validationReport);
            EditorUtility.DisplayDialog("Validation Passed", validationReport, "OK");
        }
        else
        {
            validationReport += "✗ Some validation checks failed. Run setup again if needed.";
            Debug.LogWarning(validationReport);
            EditorUtility.DisplayDialog("Validation Issues", validationReport, "OK");
        }
        
        validationResults = validationPassed;
    }
    
    private void ResetSetup()
    {
        Debug.Log("=== Resetting Environment Polish ===");
        
        // Remove polish managers
        GameObject materialManager = GameObject.Find("Material Manager");
        if (materialManager != null)
        {
            DestroyImmediate(materialManager);
            Debug.Log("Material Manager removed");
        }
        
        GameObject feedbackManager = GameObject.Find("Visual Feedback Manager");
        if (feedbackManager != null)
        {
            DestroyImmediate(feedbackManager);
            Debug.Log("Visual Feedback Manager removed");
        }
        
        // Reset camera background
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = new Color(0.19f, 0.3f, 0.47f);
            EditorUtility.SetDirty(mainCamera);
        }
        
        // Reset lighting to defaults
        Light directionalLight = FindObjectOfType<Light>();
        if (directionalLight != null && directionalLight.type == LightType.Directional)
        {
            directionalLight.intensity = 1.0f;
            directionalLight.color = Color.white;
            EditorUtility.SetDirty(directionalLight);
        }
        
        Debug.Log("Environment polish reset complete");
        EditorUtility.DisplayDialog("Reset Complete", "Environment polish has been removed.", "OK");
    }
    
    /// <summary>
    /// Converts all materials to URP-compatible shaders
    /// </summary>
    private void ConvertMaterialsToURP()
    {
        Debug.Log("=== Converting Materials to URP Shaders ===");
        
        int materialsConverted = 0;
        
        // Find all materials in the project
        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
        
        foreach (string guid in materialGUIDs)
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            
            if (material != null && material.shader != null)
            {
                string shaderName = material.shader.name;
                string newShaderName = null;
                
                // Convert common shaders to URP equivalents
                switch (shaderName)
                {
                    case "Standard":
                        newShaderName = "Universal Render Pipeline/Lit";
                        break;
                    case "Unlit/Color":
                        newShaderName = "Universal Render Pipeline/Unlit";
                        break;
                    case "Unlit/Transparent":
                        newShaderName = "Universal Render Pipeline/Unlit";
                        break;
                    case "Legacy Shaders/Diffuse":
                        newShaderName = "Universal Render Pipeline/Lit";
                        break;
                    case "Legacy Shaders/Transparent/Diffuse":
                        newShaderName = "Universal Render Pipeline/Lit";
                        break;
                }
                
                if (newShaderName != null)
                {
                    Shader newShader = Shader.Find(newShaderName);
                    if (newShader != null)
                    {
                        // Store the current color before changing shader
                        Color currentColor = material.color;
                        
                        material.shader = newShader;
                        
                        // Restore the color after shader change
                        material.color = currentColor;
                        
                        // Configure URP material properties
                        if (newShaderName.Contains("Lit"))
                        {
                            // Set surface type for transparency if needed
                            if (currentColor.a < 1.0f)
                            {
                                material.SetFloat("_Surface", 1); // Transparent
                                material.SetFloat("_Blend", 0); // Alpha
                            }
                            else
                            {
                                material.SetFloat("_Surface", 0); // Opaque
                            }
                            
                            // Set workflow mode
                            material.SetFloat("_WorkflowMode", 1); // Specular workflow
                            
                            // Configure for clean aesthetic
                            if (useCleanAesthetic)
                            {
                                material.SetFloat("_Metallic", 0.0f);
                                material.SetFloat("_Smoothness", 0.1f);
                            }
                        }
                        
                        EditorUtility.SetDirty(material);
                        materialsConverted++;
                        
                        Debug.Log($"Converted material '{material.name}' from '{shaderName}' to '{newShaderName}'");
                    }
                    else
                    {
                        Debug.LogWarning($"URP shader '{newShaderName}' not found for material '{material.name}'");
                    }
                }
            }
        }
        
        if (materialsConverted > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"Material conversion complete! Converted {materialsConverted} materials to URP shaders.");
            EditorUtility.DisplayDialog("URP Conversion Complete", 
                $"Successfully converted {materialsConverted} materials to URP-compatible shaders.\n\n" +
                "Your prefabs should now display correctly with URP.", "OK");
        }
        else
        {
            Debug.Log("No materials needed conversion - all materials are already using compatible shaders.");
            EditorUtility.DisplayDialog("No Conversion Needed", 
                "All materials are already using URP-compatible shaders.", "OK");
        }
    }
}