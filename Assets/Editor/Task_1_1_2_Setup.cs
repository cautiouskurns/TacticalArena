using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class Task_1_1_2_Setup : EditorWindow
{
    [Header("Grid Configuration")]
    [SerializeField] private int gridWidth = 4;
    [SerializeField] private int gridHeight = 4;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float tileSpacing = 0.05f;
    
    [Header("Visual Configuration")]
    [SerializeField] private bool useLineRenderers = true;
    [SerializeField] private bool createTilePlanes = true;
    [SerializeField] private Color gridLineColor = Color.white;
    [SerializeField] private Color tileDefaultColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
    [SerializeField] private Color tileHoverColor = Color.yellow;
    [SerializeField] private Color tileSelectedColor = Color.green;
    [SerializeField] private float lineWidth = 0.05f;
    
    [Header("Grid Positioning")]
    [SerializeField] private Vector3 gridCenter = new Vector3(1.5f, 0f, 1.5f);
    [SerializeField] private bool autoCalculateCenter = true;
    
    [Header("Performance")]
    [SerializeField] private bool enableTileColliders = true;
    [SerializeField] private bool optimizeForBatching = true;
    
    private Vector2 scrollPosition;
    
    [MenuItem("Tactical Tools/Task 1.1.2 - Create Grid System")]
    public static void ShowWindow()
    {
        Task_1_1_2_Setup window = GetWindow<Task_1_1_2_Setup>("Task 1.1.2 Setup");
        window.minSize = new Vector2(450, 700);
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
        EditorGUILayout.LabelField("3D Tactical Arena - Grid System Setup", headerStyle);
        GUILayout.Space(20);
        
        // Grid Configuration Section
        EditorGUILayout.LabelField("Grid Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        gridWidth = EditorGUILayout.IntSlider("Grid Width", gridWidth, 2, 8);
        gridHeight = EditorGUILayout.IntSlider("Grid Height", gridHeight, 2, 8);
        tileSize = EditorGUILayout.Slider("Tile Size", tileSize, 0.5f, 2f);
        tileSpacing = EditorGUILayout.Slider("Tile Spacing", tileSpacing, 0f, 0.2f);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Visual Configuration Section
        EditorGUILayout.LabelField("Visual Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        useLineRenderers = EditorGUILayout.Toggle("Use Line Renderers", useLineRenderers);
        createTilePlanes = EditorGUILayout.Toggle("Create Tile Planes", createTilePlanes);
        gridLineColor = EditorGUILayout.ColorField("Grid Line Color", gridLineColor);
        tileDefaultColor = EditorGUILayout.ColorField("Tile Default Color", tileDefaultColor);
        tileHoverColor = EditorGUILayout.ColorField("Tile Hover Color", tileHoverColor);
        tileSelectedColor = EditorGUILayout.ColorField("Tile Selected Color", tileSelectedColor);
        
        if (useLineRenderers)
        {
            lineWidth = EditorGUILayout.Slider("Line Width", lineWidth, 0.01f, 0.2f);
        }
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Grid Positioning Section
        EditorGUILayout.LabelField("Grid Positioning", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        autoCalculateCenter = EditorGUILayout.Toggle("Auto Calculate Center", autoCalculateCenter);
        
        if (autoCalculateCenter)
        {
            float totalWidth = (gridWidth * tileSize) + ((gridWidth - 1) * tileSpacing);
            float totalHeight = (gridHeight * tileSize) + ((gridHeight - 1) * tileSpacing);
            Vector3 calculatedCenter = new Vector3((totalWidth - tileSize) / 2f, 0f, (totalHeight - tileSize) / 2f);
            gridCenter = calculatedCenter;
            
            EditorGUILayout.LabelField($"Calculated Center: {calculatedCenter}");
            EditorGUILayout.LabelField($"Total Grid Size: {totalWidth:F2} x {totalHeight:F2}");
        }
        else
        {
            gridCenter = EditorGUILayout.Vector3Field("Grid Center", gridCenter);
        }
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Performance Section
        EditorGUILayout.LabelField("Performance Options", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        enableTileColliders = EditorGUILayout.Toggle("Enable Tile Colliders", enableTileColliders);
        optimizeForBatching = EditorGUILayout.Toggle("Optimize for Batching", optimizeForBatching);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(20);
        
        // Action Buttons
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fixedHeight = 30
        };
        
        if (GUILayout.Button("Setup Grid System", buttonStyle))
        {
            SetupGridSystem();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Validate Setup", buttonStyle))
        {
            ValidateSetup();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Reset/Delete Grid", buttonStyle))
        {
            if (EditorUtility.DisplayDialog("Reset Grid", 
                "This will delete all grid-related GameObjects. Continue?", 
                "Yes", "Cancel"))
            {
                ResetSetup();
            }
        }
        
        GUILayout.Space(20);
        
        // Information Section
        EditorGUILayout.LabelField("Current Grid Status", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        GameObject gridSystem = GameObject.Find("Grid System");
        if (gridSystem != null)
        {
            GridManager gridManager = gridSystem.GetComponent<GridManager>();
            if (gridManager != null)
            {
                EditorGUILayout.LabelField($"Grid Manager Found: {gridManager.name}");
                EditorGUILayout.LabelField($"Grid Size: {gridManager.GridWidth}x{gridManager.GridHeight}");
                EditorGUILayout.LabelField($"Total Tiles: {gridManager.TotalTiles}");
            }
            else
            {
                EditorGUILayout.LabelField("Grid System found but no GridManager component");
            }
        }
        else
        {
            EditorGUILayout.LabelField("No Grid System found in scene");
        }
        
        // Camera integration status
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            EditorGUILayout.LabelField($"Camera Position: {mainCamera.transform.position}");
            EditorGUILayout.LabelField($"Camera Orthographic Size: {mainCamera.orthographicSize}");
            
            if (gridSystem != null)
            {
                Vector3 gridWorldCenter = gridCenter;
                Vector3 cameraViewportPoint = mainCamera.WorldToViewportPoint(gridWorldCenter);
                bool gridInView = cameraViewportPoint.x >= 0.1f && cameraViewportPoint.x <= 0.9f && 
                                cameraViewportPoint.y >= 0.1f && cameraViewportPoint.y <= 0.9f;
                EditorGUILayout.LabelField($"Grid in Camera View: {(gridInView ? "✓ Yes" : "✗ No")}");
            }
        }
        
        EditorGUI.indentLevel--;
        
        EditorGUILayout.EndScrollView();
    }
    
    private void SetupGridSystem()
    {
        Debug.Log("=== Task 1.1.2: Setting up Grid System ===");
        
        // Clean up existing grid
        ResetSetup();
        
        // Create main grid system hierarchy
        GameObject gridSystemRoot = CreateGridHierarchy();
        
        // Create grid manager
        GridManager gridManager = CreateGridManager(gridSystemRoot);
        
        // Generate grid tiles
        CreateGridTiles(gridSystemRoot, gridManager);
        
        // Create grid visualization
        if (useLineRenderers)
        {
            CreateGridLines(gridSystemRoot);
        }
        
        // Create materials for tile feedback
        CreateGridMaterials();
        
        // Mark scene as dirty
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        
        Debug.Log("=== Grid System setup complete! ===");
        EditorUtility.DisplayDialog("Setup Complete", 
            $"4x4 Grid System has been created!\n\n" +
            $"Grid Size: {gridWidth}x{gridHeight}\n" +
            $"Total Tiles: {gridWidth * gridHeight}\n" +
            $"Tile Size: {tileSize}\n\n" +
            "Check the Scene view and Game view to see the grid.", "OK");
    }
    
    private GameObject CreateGridHierarchy()
    {
        // Get or create Environment group from Task 1.1.1
        GameObject environmentGroup = GameObject.Find("Environment");
        if (environmentGroup == null)
        {
            environmentGroup = new GameObject("Environment");
        }
        
        // Create Grid System under Environment
        GameObject gridSystem = new GameObject("Grid System");
        gridSystem.transform.SetParent(environmentGroup.transform);
        gridSystem.transform.position = Vector3.zero;
        
        // Create sub-groups
        GameObject tilesGroup = new GameObject("Tiles");
        tilesGroup.transform.SetParent(gridSystem.transform);
        
        GameObject visualsGroup = new GameObject("Visuals");
        visualsGroup.transform.SetParent(gridSystem.transform);
        
        Debug.Log("Grid hierarchy created under Environment group");
        return gridSystem;
    }
    
    private GridManager CreateGridManager(GameObject gridSystemRoot)
    {
        GridManager gridManager = gridSystemRoot.AddComponent<GridManager>();
        
        // Configure grid manager with current parameters
        var serializedObject = new SerializedObject(gridManager);
        serializedObject.FindProperty("gridWidth").intValue = gridWidth;
        serializedObject.FindProperty("gridHeight").intValue = gridHeight;
        serializedObject.FindProperty("tileSize").floatValue = tileSize;
        serializedObject.FindProperty("tileSpacing").floatValue = tileSpacing;
        serializedObject.FindProperty("gridCenter").vector3Value = gridCenter;
        serializedObject.FindProperty("enableTileColliders").boolValue = enableTileColliders;
        serializedObject.ApplyModifiedProperties();
        
        Debug.Log($"GridManager created and configured: {gridWidth}x{gridHeight} grid");
        return gridManager;
    }
    
    private void CreateGridTiles(GameObject gridSystemRoot, GridManager gridManager)
    {
        GameObject tilesGroup = gridSystemRoot.transform.Find("Tiles").gameObject;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                // Create tile GameObject
                GameObject tileObj = new GameObject($"Tile_{x}_{z}");
                tileObj.transform.SetParent(tilesGroup.transform);
                
                // Calculate world position
                Vector3 worldPos = CalculateTileWorldPosition(x, z);
                tileObj.transform.position = worldPos;
                
                // Add GridTile component
                GridTile gridTile = tileObj.AddComponent<GridTile>();
                
                // Configure tile with serialized properties
                var serializedTile = new SerializedObject(gridTile);
                serializedTile.FindProperty("coordinate").FindPropertyRelative("x").intValue = x;
                serializedTile.FindProperty("coordinate").FindPropertyRelative("z").intValue = z;
                serializedTile.FindProperty("worldPosition").vector3Value = worldPos;
                serializedTile.FindProperty("tileSize").floatValue = tileSize;
                serializedTile.ApplyModifiedProperties();
                
                // Create visual representation if requested
                if (createTilePlanes)
                {
                    CreateTileVisual(tileObj);
                }
                
                // Add collider for mouse interaction if requested
                if (enableTileColliders)
                {
                    CreateTileCollider(tileObj);
                }
            }
        }
        
        Debug.Log($"Created {gridWidth * gridHeight} grid tiles");
    }
    
    private Vector3 CalculateTileWorldPosition(int gridX, int gridZ)
    {
        float offsetX = gridX * (tileSize + tileSpacing);
        float offsetZ = gridZ * (tileSize + tileSpacing);
        
        Vector3 startPos = gridCenter - new Vector3(
            ((gridWidth - 1) * (tileSize + tileSpacing)) / 2f,
            0f,
            ((gridHeight - 1) * (tileSize + tileSpacing)) / 2f
        );
        
        return startPos + new Vector3(offsetX, 0f, offsetZ);
    }
    
    private void CreateTileVisual(GameObject tileObj)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Plane);
        visual.name = "Visual";
        visual.transform.SetParent(tileObj.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(tileSize * 0.1f, 1f, tileSize * 0.1f);
        
        // Remove the default collider since we'll add our own
        DestroyImmediate(visual.GetComponent<Collider>());
        
        // Set up material
        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material tileMaterial = CreateOrGetTileMaterial("GridTile_Default", tileDefaultColor);
            renderer.material = tileMaterial;
        }
    }
    
    private void CreateTileCollider(GameObject tileObj)
    {
        BoxCollider collider = tileObj.AddComponent<BoxCollider>();
        collider.size = new Vector3(tileSize, 0.1f, tileSize);
        collider.center = new Vector3(0f, 0.05f, 0f);
        collider.isTrigger = true;
    }
    
    private void CreateGridLines(GameObject gridSystemRoot)
    {
        GameObject visualsGroup = gridSystemRoot.transform.Find("Visuals").gameObject;
        GameObject gridLinesObj = new GameObject("Grid Lines");
        gridLinesObj.transform.SetParent(visualsGroup.transform);
        
        // Create horizontal lines
        for (int i = 0; i <= gridHeight; i++)
        {
            CreateGridLine(gridLinesObj, i, true);
        }
        
        // Create vertical lines
        for (int i = 0; i <= gridWidth; i++)
        {
            CreateGridLine(gridLinesObj, i, false);
        }
        
        Debug.Log("Grid lines created using LineRenderer components");
    }
    
    private void CreateGridLine(GameObject parent, int index, bool isHorizontal)
    {
        GameObject lineObj = new GameObject(isHorizontal ? $"HLine_{index}" : $"VLine_{index}");
        lineObj.transform.SetParent(parent.transform);
        
        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.material = CreateOrGetLineMaterial();
        lineRenderer.startColor = gridLineColor;
        lineRenderer.endColor = gridLineColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = false;
        
        Vector3 startPos, endPos;
        
        if (isHorizontal)
        {
            float zOffset = index * (tileSize + tileSpacing) - ((gridHeight * (tileSize + tileSpacing) - tileSpacing) / 2f);
            startPos = gridCenter + new Vector3(-((gridWidth * (tileSize + tileSpacing) - tileSpacing) / 2f), 0.01f, zOffset);
            endPos = gridCenter + new Vector3(((gridWidth * (tileSize + tileSpacing) - tileSpacing) / 2f), 0.01f, zOffset);
        }
        else
        {
            float xOffset = index * (tileSize + tileSpacing) - ((gridWidth * (tileSize + tileSpacing) - tileSpacing) / 2f);
            startPos = gridCenter + new Vector3(xOffset, 0.01f, -((gridHeight * (tileSize + tileSpacing) - tileSpacing) / 2f));
            endPos = gridCenter + new Vector3(xOffset, 0.01f, ((gridHeight * (tileSize + tileSpacing) - tileSpacing) / 2f));
        }
        
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }
    
    private Material CreateOrGetLineMaterial()
    {
        string materialPath = "Assets/Materials/GridLine.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        
        if (material == null)
        {
            // Create Materials folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            
            material = new Material(Shader.Find("Sprites/Default"));
            material.name = "GridLine";
            AssetDatabase.CreateAsset(material, materialPath);
        }
        
        // Always update the material color with current editor value
        material.color = gridLineColor;
        
        // Mark the material as dirty so changes are saved
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        
        return material;
    }
    
    private Material CreateOrGetTileMaterial(string materialName, Color color)
    {
        string materialPath = $"Assets/Materials/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        
        if (material == null)
        {
            // Create Materials folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            
            material = new Material(Shader.Find("Standard"));
            material.name = materialName;
            AssetDatabase.CreateAsset(material, materialPath);
        }
        
        // Always update the material properties with current editor values
        material.color = color;
        material.SetFloat("_Mode", 2); // Set to Fade mode for transparency
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        
        // Mark the material as dirty so changes are saved
        EditorUtility.SetDirty(material);
        
        return material;
    }
    
    private void CreateGridMaterials()
    {
        // Create materials for different tile states
        CreateOrGetTileMaterial("GridTile_Default", tileDefaultColor);
        CreateOrGetTileMaterial("GridTile_Hover", tileHoverColor);
        CreateOrGetTileMaterial("GridTile_Selected", tileSelectedColor);
        
        AssetDatabase.SaveAssets();
        Debug.Log("Grid materials created in Assets/Materials/");
    }
    
    private void ValidateSetup()
    {
        Debug.Log("=== Validating Grid System Setup ===");
        
        bool validationPassed = true;
        string validationReport = "Grid System Validation Results:\n\n";
        
        // Check if grid system exists
        GameObject gridSystem = GameObject.Find("Grid System");
        if (gridSystem != null)
        {
            validationReport += "✓ Grid System GameObject found\n";
            
            // Check GridManager
            GridManager gridManager = gridSystem.GetComponent<GridManager>();
            if (gridManager != null)
            {
                validationReport += "✓ GridManager component found\n";
                validationReport += $"✓ Grid configured as {gridManager.GridWidth}x{gridManager.GridHeight}\n";
                
                // Check tile count
                GameObject tilesGroup = gridSystem.transform.Find("Tiles")?.gameObject;
                if (tilesGroup != null)
                {
                    int tileCount = tilesGroup.transform.childCount;
                    int expectedTiles = gridManager.GridWidth * gridManager.GridHeight;
                    
                    if (tileCount == expectedTiles)
                    {
                        validationReport += $"✓ Correct number of tiles created: {tileCount}\n";
                    }
                    else
                    {
                        validationReport += $"✗ Incorrect tile count: {tileCount} (expected {expectedTiles})\n";
                        validationPassed = false;
                    }
                }
                else
                {
                    validationReport += "✗ Tiles group not found\n";
                    validationPassed = false;
                }
            }
            else
            {
                validationReport += "✗ GridManager component missing\n";
                validationPassed = false;
            }
        }
        else
        {
            validationReport += "✗ Grid System not found in scene\n";
            validationPassed = false;
        }
        
        // Check camera integration
        Camera mainCamera = Camera.main;
        if (mainCamera != null && gridSystem != null)
        {
            Vector3 gridWorldCenter = gridCenter;
            Vector3 cameraViewportPoint = mainCamera.WorldToViewportPoint(gridWorldCenter);
            bool gridInView = cameraViewportPoint.x >= 0.1f && cameraViewportPoint.x <= 0.9f && 
                            cameraViewportPoint.y >= 0.1f && cameraViewportPoint.y <= 0.9f;
            
            if (gridInView)
            {
                validationReport += "✓ Grid is visible from camera perspective\n";
            }
            else
            {
                validationReport += "✗ Grid may not be fully visible from camera\n";
                validationPassed = false;
            }
            
            if (mainCamera.orthographic)
            {
                validationReport += "✓ Camera is orthographic (good for tactical view)\n";
            }
            else
            {
                validationReport += "⚠ Camera is perspective (tactical games usually use orthographic)\n";
            }
        }
        
        // Check materials
        if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GridTile_Default.mat") != null)
        {
            validationReport += "✓ Grid materials created successfully\n";
        }
        else
        {
            validationReport += "✗ Grid materials not found\n";
            validationPassed = false;
        }
        
        validationReport += "\n";
        if (validationPassed)
        {
            validationReport += "✓ All validation checks passed! Grid system ready for tactical gameplay.";
            Debug.Log(validationReport);
            EditorUtility.DisplayDialog("Validation Passed", validationReport, "OK");
        }
        else
        {
            validationReport += "✗ Some validation checks failed. Check issues and run setup again if needed.";
            Debug.LogWarning(validationReport);
            EditorUtility.DisplayDialog("Validation Issues", validationReport, "OK");
        }
    }
    
    private void ResetSetup()
    {
        Debug.Log("=== Resetting Grid System ===");
        
        // Remove Grid System and all children
        GameObject gridSystem = GameObject.Find("Grid System");
        if (gridSystem != null)
        {
            DestroyImmediate(gridSystem);
            Debug.Log("Grid System removed from scene");
        }
        
        // Clean up materials (optional - keep them for reuse)
        // Note: We'll keep materials as they can be reused
        
        Debug.Log("Grid system reset complete");
        EditorUtility.DisplayDialog("Reset Complete", "Grid System has been removed from the scene.", "OK");
    }
}