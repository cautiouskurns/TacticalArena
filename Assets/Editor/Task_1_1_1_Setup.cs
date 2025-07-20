using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class Task_1_1_1_Setup : EditorWindow
{
    [Header("Camera Configuration")]
    [SerializeField] private float cameraAngleX = 45f;
    [SerializeField] private float cameraAngleY = 45f;
    [SerializeField] private float cameraDistance = 12f;
    [SerializeField] private float cameraHeight = 8f;
    [SerializeField] private float orthographicSize = 6f;
    
    [Header("Lighting Configuration")]
    [SerializeField] private float lightIntensity = 1.2f;
    [SerializeField] private float lightAngleX = 50f;
    [SerializeField] private float lightAngleY = -30f;
    [SerializeField] private Color lightColor = Color.white;
    
    [Header("Scene Configuration")]
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.3f, 0.4f);
    [SerializeField] private string sceneName = "BattleScene";
    
    [Header("Grid Area Reference")]
    [SerializeField] private float gridSize = 4f;
    [SerializeField] private float tileSize = 1f;
    
    private Vector2 scrollPosition;
    
    [MenuItem("Tactical Tools/Task 1.1.1 - Setup 3D Scene & Camera")]
    public static void ShowWindow()
    {
        Task_1_1_1_Setup window = GetWindow<Task_1_1_1_Setup>("Task 1.1.1 Setup");
        window.minSize = new Vector2(400, 600);
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
        EditorGUILayout.LabelField("3D Tactical Arena - Scene & Camera Setup", headerStyle);
        GUILayout.Space(20);
        
        // Camera Configuration Section
        EditorGUILayout.LabelField("Camera Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        cameraAngleX = EditorGUILayout.Slider("Camera Angle X", cameraAngleX, 30f, 60f);
        cameraAngleY = EditorGUILayout.Slider("Camera Angle Y", cameraAngleY, 30f, 60f);
        cameraDistance = EditorGUILayout.Slider("Camera Distance", cameraDistance, 8f, 20f);
        cameraHeight = EditorGUILayout.Slider("Camera Height", cameraHeight, 5f, 15f);
        orthographicSize = EditorGUILayout.Slider("Orthographic Size", orthographicSize, 4f, 10f);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Lighting Configuration Section
        EditorGUILayout.LabelField("Lighting Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        lightIntensity = EditorGUILayout.Slider("Light Intensity", lightIntensity, 0.5f, 2f);
        lightAngleX = EditorGUILayout.Slider("Light Angle X", lightAngleX, 30f, 80f);
        lightAngleY = EditorGUILayout.Slider("Light Angle Y", lightAngleY, -60f, 60f);
        lightColor = EditorGUILayout.ColorField("Light Color", lightColor);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Scene Configuration Section
        EditorGUILayout.LabelField("Scene Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
        sceneName = EditorGUILayout.TextField("Scene Name", sceneName);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Grid Reference Section (for positioning)
        EditorGUILayout.LabelField("Grid Area Reference", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.LabelField($"Grid Size: {gridSize}x{gridSize} tiles");
        EditorGUILayout.LabelField($"Total Area: {gridSize * tileSize}x{gridSize * tileSize} units");
        EditorGUILayout.LabelField($"Camera will be positioned to view entire grid");
        
        EditorGUI.indentLevel--;
        GUILayout.Space(20);
        
        // Action Buttons
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fixedHeight = 30
        };
        
        if (GUILayout.Button("Setup Scene & Camera", buttonStyle))
        {
            SetupSceneAndCamera();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Validate Setup", buttonStyle))
        {
            ValidateSetup();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Reset to Default Unity Scene", buttonStyle))
        {
            if (EditorUtility.DisplayDialog("Reset Scene", 
                "This will reset the scene to default Unity state. Continue?", 
                "Yes", "Cancel"))
            {
                ResetSetup();
            }
        }
        
        GUILayout.Space(20);
        
        // Information Section
        EditorGUILayout.LabelField("Current Scene Status", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            EditorGUILayout.LabelField($"Camera Position: {mainCamera.transform.position}");
            EditorGUILayout.LabelField($"Camera Rotation: {mainCamera.transform.eulerAngles}");
            EditorGUILayout.LabelField($"Orthographic: {mainCamera.orthographic}");
            EditorGUILayout.LabelField($"Orthographic Size: {mainCamera.orthographicSize}");
        }
        else
        {
            EditorGUILayout.LabelField("No Main Camera found in scene");
        }
        
        Light mainLight = FindFirstObjectByType<Light>();
        if (mainLight != null)
        {
            EditorGUILayout.LabelField($"Light Intensity: {mainLight.intensity}");
            EditorGUILayout.LabelField($"Light Rotation: {mainLight.transform.eulerAngles}");
        }
        else
        {
            EditorGUILayout.LabelField("No Directional Light found in scene");
        }
        
        EditorGUI.indentLevel--;
        
        EditorGUILayout.EndScrollView();
    }
    
    private void SetupSceneAndCamera()
    {
        Debug.Log("=== Task 1.1.1: Setting up 3D Scene & Camera ===");
        
        // Rename current scene
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        if (!string.IsNullOrEmpty(currentScenePath))
        {
            string newScenePath = currentScenePath.Replace("SampleScene", sceneName);
            if (newScenePath != currentScenePath)
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), newScenePath);
                Debug.Log($"Scene renamed to: {sceneName}");
            }
        }
        
        // Setup Camera
        SetupIsometricCamera();
        
        // Setup Lighting
        SetupTacticalLighting();
        
        // Setup Scene Organization
        SetupSceneHierarchy();
        
        // Set render settings
        SetupRenderSettings();
        
        Debug.Log("=== Scene and Camera setup complete! ===");
        EditorUtility.DisplayDialog("Setup Complete", 
            "3D Scene and Camera have been configured for tactical gameplay!\n\n" +
            "Check the Game view to see the isometric perspective.", "OK");
    }
    
    private void SetupIsometricCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObj = new GameObject("Main Camera");
            mainCamera = cameraObj.AddComponent<Camera>();
            cameraObj.tag = "MainCamera";
        }
        
        // Calculate camera position for isometric view
        float totalGridWorldSize = gridSize * tileSize;
        Vector3 gridCenter = new Vector3(totalGridWorldSize / 2f - 0.5f, 0, totalGridWorldSize / 2f - 0.5f);
        
        // Position camera at an isometric angle
        Vector3 cameraPosition = gridCenter;
        cameraPosition += new Vector3(-cameraDistance * Mathf.Cos(cameraAngleY * Mathf.Deg2Rad), 
                                      cameraHeight,
                                      -cameraDistance * Mathf.Sin(cameraAngleY * Mathf.Deg2Rad));
        
        mainCamera.transform.position = cameraPosition;
        mainCamera.transform.rotation = Quaternion.Euler(cameraAngleX, cameraAngleY, 0);
        
        // Set orthographic projection
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = orthographicSize;
        mainCamera.backgroundColor = backgroundColor;
        
        // Set appropriate clipping planes
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 100f;
        
        Debug.Log($"Camera positioned at: {cameraPosition} with rotation: {mainCamera.transform.eulerAngles}");
    }
    
    private void SetupTacticalLighting()
    {
        Light mainLight = FindFirstObjectByType<Light>();
        if (mainLight == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            mainLight = lightObj.AddComponent<Light>();
        }
        
        mainLight.type = LightType.Directional;
        mainLight.intensity = lightIntensity;
        mainLight.color = lightColor;
        mainLight.shadows = LightShadows.Soft;
        
        // Position light for good geometric visibility
        mainLight.transform.rotation = Quaternion.Euler(lightAngleX, lightAngleY, 0);
        
        Debug.Log($"Lighting configured: Intensity {lightIntensity}, Rotation {mainLight.transform.eulerAngles}");
    }
    
    private void SetupSceneHierarchy()
    {
        // Create organized hierarchy for future systems
        GameObject environmentGroup = GameObject.Find("Environment");
        if (environmentGroup == null)
        {
            environmentGroup = new GameObject("Environment");
        }
        
        GameObject lightingGroup = GameObject.Find("Lighting");
        if (lightingGroup == null)
        {
            lightingGroup = new GameObject("Lighting");
            Light mainLight = FindFirstObjectByType<Light>();
            if (mainLight != null)
            {
                mainLight.transform.SetParent(lightingGroup.transform);
            }
        }
        
        Debug.Log("Scene hierarchy organized for future grid and unit systems");
    }
    
    private void SetupRenderSettings()
    {
        // Configure render settings for tactical gameplay
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.2f, 0.3f, 0.4f);
        RenderSettings.ambientEquatorColor = new Color(0.15f, 0.25f, 0.35f);
        RenderSettings.ambientGroundColor = new Color(0.1f, 0.2f, 0.3f);
        
        Debug.Log("Render settings configured for tactical aesthetic");
    }
    
    private void ValidateSetup()
    {
        Debug.Log("=== Validating Scene & Camera Setup ===");
        
        bool validationPassed = true;
        string validationReport = "Validation Results:\n\n";
        
        // Check camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            if (mainCamera.orthographic)
            {
                validationReport += "✓ Camera is set to orthographic projection\n";
            }
            else
            {
                validationReport += "✗ Camera should be orthographic for tactical view\n";
                validationPassed = false;
            }
            
            Vector3 rotation = mainCamera.transform.eulerAngles;
            if (Mathf.Abs(rotation.x - cameraAngleX) < 5f && Mathf.Abs(rotation.y - cameraAngleY) < 5f)
            {
                validationReport += "✓ Camera is positioned at isometric angle\n";
            }
            else
            {
                validationReport += "✗ Camera angle needs adjustment for isometric view\n";
                validationPassed = false;
            }
            
            if (mainCamera.orthographicSize >= 4f && mainCamera.orthographicSize <= 10f)
            {
                validationReport += "✓ Orthographic size appropriate for tactical view\n";
            }
            else
            {
                validationReport += "✗ Orthographic size may not show battlefield properly\n";
                validationPassed = false;
            }
        }
        else
        {
            validationReport += "✗ No Main Camera found in scene\n";
            validationPassed = false;
        }
        
        // Check lighting
        Light mainLight = FindFirstObjectByType<Light>();
        if (mainLight != null && mainLight.type == LightType.Directional)
        {
            validationReport += "✓ Directional lighting configured\n";
            
            if (mainLight.intensity >= 0.8f && mainLight.intensity <= 2f)
            {
                validationReport += "✓ Light intensity appropriate for tactical visibility\n";
            }
            else
            {
                validationReport += "✗ Light intensity may be too dim or bright\n";
                validationPassed = false;
            }
        }
        else
        {
            validationReport += "✗ No Directional Light found or configured\n";
            validationPassed = false;
        }
        
        // Check scene organization
        if (GameObject.Find("Environment") != null)
        {
            validationReport += "✓ Scene hierarchy organized for future systems\n";
        }
        else
        {
            validationReport += "✗ Scene hierarchy not properly organized\n";
            validationPassed = false;
        }
        
        validationReport += "\n";
        if (validationPassed)
        {
            validationReport += "✓ All validation checks passed! Scene ready for grid system.";
            Debug.Log(validationReport);
            EditorUtility.DisplayDialog("Validation Passed", validationReport, "OK");
        }
        else
        {
            validationReport += "✗ Some validation checks failed. Run setup again or adjust manually.";
            Debug.LogWarning(validationReport);
            EditorUtility.DisplayDialog("Validation Issues", validationReport, "OK");
        }
    }
    
    private void ResetSetup()
    {
        Debug.Log("=== Resetting Scene to Default Unity State ===");
        
        // Reset camera to default
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transform.position = new Vector3(0, 1, -10);
            mainCamera.transform.rotation = Quaternion.identity;
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 60f;
            mainCamera.backgroundColor = new Color(0.19f, 0.3f, 0.47f);
        }
        
        // Reset lighting
        Light mainLight = FindFirstObjectByType<Light>();
        if (mainLight != null)
        {
            mainLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            mainLight.intensity = 1f;
            mainLight.color = Color.white;
        }
        
        // Clean up hierarchy
        GameObject environmentGroup = GameObject.Find("Environment");
        if (environmentGroup != null && environmentGroup.transform.childCount == 0)
        {
            DestroyImmediate(environmentGroup);
        }
        
        GameObject lightingGroup = GameObject.Find("Lighting");
        if (lightingGroup != null)
        {
            if (mainLight != null)
            {
                mainLight.transform.SetParent(null);
            }
            DestroyImmediate(lightingGroup);
        }
        
        Debug.Log("Scene reset to default Unity state");
        EditorUtility.DisplayDialog("Reset Complete", "Scene has been reset to default Unity state.", "OK");
    }
}