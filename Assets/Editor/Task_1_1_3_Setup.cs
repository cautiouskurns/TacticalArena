using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class Task_1_1_3_Setup : EditorWindow
{
    [Header("Obstacle Configuration")]
    [SerializeField] private int obstacleCount = 3;
    [SerializeField] private ObstaclePreset obstaclePreset = ObstaclePreset.Strategic;
    
    [Header("Obstacle Types")]
    [SerializeField] private bool includeLowCover = true;
    [SerializeField] private bool includeHighWalls = true;
    [SerializeField] private bool includeTerrainFeatures = true;
    
    [Header("Placement Strategy")]
    [SerializeField] private PlacementStrategy placementStrategy = PlacementStrategy.Chokepoints;
    [SerializeField] private bool allowManualPlacement = false;
    [SerializeField] private bool randomizePositions = true;
    [SerializeField] private GridCoordinate manualPlacement1 = new GridCoordinate(1, 1);
    [SerializeField] private GridCoordinate manualPlacement2 = new GridCoordinate(2, 2);
    [SerializeField] private GridCoordinate manualPlacement3 = new GridCoordinate(1, 2);
    [SerializeField] private GridCoordinate manualPlacement4 = new GridCoordinate(0, 3);
    [SerializeField] private GridCoordinate manualPlacement5 = new GridCoordinate(3, 0);
    
    [Header("Visual Configuration")]
    [SerializeField] private Color lowCoverColor = new Color(0.6f, 0.4f, 0.2f, 1f);
    [SerializeField] private Color highWallColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color terrainColor = new Color(0.3f, 0.6f, 0.3f, 1f);
    [SerializeField] private float lowCoverHeight = 0.5f;
    [SerializeField] private float highWallHeight = 1.5f;
    [SerializeField] private float terrainHeight = 0.3f;
    
    [Header("Gameplay Configuration")]
    [SerializeField] private bool enableLineOfSightBlocking = true;
    [SerializeField] private bool enablePartialCover = true;
    [SerializeField] private bool generateObstacleShadows = true;
    
    private Vector2 scrollPosition;
    private GridManager cachedGridManager;
    private bool logObstacleDetails = false; // Debug flag
    
    public enum ObstaclePreset
    {
        Strategic,  // Optimal tactical placement
        Balanced,   // Even distribution
        Defensive,  // Corner and edge placement
        Central     // Center-focused placement
    }
    
    public enum PlacementStrategy
    {
        Chokepoints,    // Create 1-2 tile wide passages
        Corners,        // Place in strategic corners
        Center,         // Central battlefield control
        Manual          // User-defined positions
    }
    
    [MenuItem("Tactical Tools/Task 1.1.3 - Place Obstacles & Terrain")]
    public static void ShowWindow()
    {
        Task_1_1_3_Setup window = GetWindow<Task_1_1_3_Setup>("Task 1.1.3 Setup");
        window.minSize = new Vector2(500, 800);
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
        EditorGUILayout.LabelField("3D Tactical Arena - Obstacle & Terrain Setup", headerStyle);
        GUILayout.Space(20);
        
        // Obstacle Configuration Section
        EditorGUILayout.LabelField("Obstacle Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        obstacleCount = EditorGUILayout.IntSlider("Obstacle Count", obstacleCount, 1, 5);
        obstaclePreset = (ObstaclePreset)EditorGUILayout.EnumPopup("Obstacle Preset", obstaclePreset);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Obstacle Types to Include:", EditorStyles.miniBoldLabel);
        includeLowCover = EditorGUILayout.Toggle("Low Cover (partial blocking)", includeLowCover);
        includeHighWalls = EditorGUILayout.Toggle("High Walls (full blocking)", includeHighWalls);
        includeTerrainFeatures = EditorGUILayout.Toggle("Terrain Features (low obstacles)", includeTerrainFeatures);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Placement Strategy Section
        EditorGUILayout.LabelField("Placement Strategy", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        placementStrategy = (PlacementStrategy)EditorGUILayout.EnumPopup("Strategy", placementStrategy);
        
        if (placementStrategy == PlacementStrategy.Manual)
        {
            allowManualPlacement = true;
        }
        
        if (placementStrategy != PlacementStrategy.Manual)
        {
            randomizePositions = EditorGUILayout.Toggle("Randomize Positions", randomizePositions);
        }
        
        allowManualPlacement = EditorGUILayout.Toggle("Manual Placement Override", allowManualPlacement);
        
        if (allowManualPlacement)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Manual Positions (Grid Coordinates):", EditorStyles.miniBoldLabel);
            
            for (int i = 1; i <= obstacleCount && i <= 5; i++)
            {
                switch (i)
                {
                    case 1: manualPlacement1 = DrawGridCoordinateField("Position 1", manualPlacement1); break;
                    case 2: manualPlacement2 = DrawGridCoordinateField("Position 2", manualPlacement2); break;
                    case 3: manualPlacement3 = DrawGridCoordinateField("Position 3", manualPlacement3); break;
                    case 4: manualPlacement4 = DrawGridCoordinateField("Position 4", manualPlacement4); break;
                    case 5: manualPlacement5 = DrawGridCoordinateField("Position 5", manualPlacement5); break;
                }
            }
        }
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Visual Configuration Section
        EditorGUILayout.LabelField("Visual Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        lowCoverColor = EditorGUILayout.ColorField("Low Cover Color", lowCoverColor);
        lowCoverHeight = EditorGUILayout.Slider("Low Cover Height", lowCoverHeight, 0.2f, 1f);
        
        EditorGUILayout.Space(3);
        highWallColor = EditorGUILayout.ColorField("High Wall Color", highWallColor);
        highWallHeight = EditorGUILayout.Slider("High Wall Height", highWallHeight, 1f, 2.5f);
        
        EditorGUILayout.Space(3);
        terrainColor = EditorGUILayout.ColorField("Terrain Color", terrainColor);
        terrainHeight = EditorGUILayout.Slider("Terrain Height", terrainHeight, 0.1f, 0.8f);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Gameplay Configuration Section
        EditorGUILayout.LabelField("Gameplay Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        enableLineOfSightBlocking = EditorGUILayout.Toggle("Line-of-Sight Blocking", enableLineOfSightBlocking);
        enablePartialCover = EditorGUILayout.Toggle("Partial Cover Mechanics", enablePartialCover);
        generateObstacleShadows = EditorGUILayout.Toggle("Generate Obstacle Shadows", generateObstacleShadows);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(20);
        
        // Preview Section
        EditorGUILayout.LabelField("Placement Preview", EditorStyles.boldLabel);
        
        if (cachedGridManager == null)
        {
            GameObject gridSystem = GameObject.Find("Grid System");
            if (gridSystem != null)
            {
                cachedGridManager = gridSystem.GetComponent<GridManager>();
                // Ensure the GridManager is properly initialized
                if (cachedGridManager != null && !Application.isPlaying)
                {
                    // In edit mode, manually trigger initialization if needed
                    cachedGridManager.GetTile(0, 0); // This will trigger initialization in GetTile
                }
            }
        }
        
        if (cachedGridManager != null)
        {
            EditorGUILayout.LabelField($"Grid Size: {cachedGridManager.GridWidth}x{cachedGridManager.GridHeight}");
            EditorGUILayout.LabelField($"Available Tiles: {cachedGridManager.TotalTiles}");
            
            var positions = GetPlacementPositions();
            EditorGUILayout.LabelField($"Planned Positions: {positions.Length}");
            for (int i = 0; i < positions.Length; i++)
            {
                EditorGUILayout.LabelField($"  Obstacle {i + 1}: {positions[i]}");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Grid System not found. Please run Task 1.1.2 first to create the grid system.", MessageType.Warning);
        }
        
        GUILayout.Space(15);
        
        // Action Buttons
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fixedHeight = 30
        };
        
        GUI.enabled = cachedGridManager != null;
        if (GUILayout.Button("Setup Obstacles & Terrain", buttonStyle))
        {
            SetupObstacles();
        }
        GUI.enabled = true;
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Validate Setup", buttonStyle))
        {
            ValidateSetup();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Reset/Delete Obstacles", buttonStyle))
        {
            if (EditorUtility.DisplayDialog("Reset Obstacles", 
                "This will delete all obstacle-related GameObjects. Continue?", 
                "Yes", "Cancel"))
            {
                ResetSetup();
            }
        }
        
        GUILayout.Space(20);
        
        // Current Status Section
        EditorGUILayout.LabelField("Current Obstacle Status", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        GameObject obstacleSystem = GameObject.Find("Obstacle System");
        if (obstacleSystem != null)
        {
            ObstacleManager obstacleManager = obstacleSystem.GetComponent<ObstacleManager>();
            if (obstacleManager != null)
            {
                EditorGUILayout.LabelField($"Obstacle Manager Found: {obstacleManager.name}");
                EditorGUILayout.LabelField($"Active Obstacles: {obstacleManager.ObstacleCount}");
                EditorGUILayout.LabelField($"Line-of-Sight Enabled: {(enableLineOfSightBlocking ? "✓" : "✗")}");
            }
            else
            {
                EditorGUILayout.LabelField("Obstacle System found but no ObstacleManager component");
            }
        }
        else
        {
            EditorGUILayout.LabelField("No Obstacle System found in scene");
        }
        
        EditorGUI.indentLevel--;
        
        EditorGUILayout.EndScrollView();
    }
    
    private GridCoordinate DrawGridCoordinateField(string label, GridCoordinate coord)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(80));
        
        int x = EditorGUILayout.IntField("X", coord.x, GUILayout.Width(50));
        int z = EditorGUILayout.IntField("Z", coord.z, GUILayout.Width(50));
        
        x = Mathf.Clamp(x, 0, 3);
        z = Mathf.Clamp(z, 0, 3);
        
        EditorGUILayout.LabelField($"({x},{z})", GUILayout.Width(40));
        EditorGUILayout.EndHorizontal();
        
        return new GridCoordinate(x, z);
    }
    
    private GridCoordinate[] GetPlacementPositions()
    {
        if (allowManualPlacement)
        {
            var positions = new System.Collections.Generic.List<GridCoordinate>();
            if (obstacleCount >= 1) positions.Add(manualPlacement1);
            if (obstacleCount >= 2) positions.Add(manualPlacement2);
            if (obstacleCount >= 3) positions.Add(manualPlacement3);
            if (obstacleCount >= 4) positions.Add(manualPlacement4);
            if (obstacleCount >= 5) positions.Add(manualPlacement5);
            return positions.ToArray();
        }
        
        GridCoordinate[] basePositions;
        switch (placementStrategy)
        {
            case PlacementStrategy.Chokepoints:
                basePositions = GetChokePointPositions();
                break;
            case PlacementStrategy.Corners:
                basePositions = GetCornerPositions();
                break;
            case PlacementStrategy.Center:
                basePositions = GetCenterPositions();
                break;
            default:
                basePositions = GetStrategicPositions();
                break;
        }
        
        // Apply randomization if enabled
        if (randomizePositions && placementStrategy != PlacementStrategy.Manual)
        {
            basePositions = RandomizePositions(basePositions);
        }
        
        return basePositions;
    }
    
    private GridCoordinate[] GetChokePointPositions()
    {
        // Create 1-2 tile wide chokepoints for tactical gameplay
        var chokePoints = new GridCoordinate[]
        {
            new GridCoordinate(1, 2), // Center choke
            new GridCoordinate(1, 1), // Lower choke
            new GridCoordinate(2, 2), // Upper choke
            new GridCoordinate(0, 2), // Left choke
            new GridCoordinate(2, 0)  // Right corner
        };
        
        return TakeFirstN(chokePoints, obstacleCount);
    }
    
    private GridCoordinate[] GetCornerPositions()
    {
        // Place obstacles in strategic corner positions
        var corners = new GridCoordinate[]
        {
            new GridCoordinate(0, 0), // Bottom-left
            new GridCoordinate(3, 3), // Top-right
            new GridCoordinate(0, 3), // Top-left
            new GridCoordinate(3, 0), // Bottom-right
            new GridCoordinate(1, 1)  // Near center
        };
        
        return TakeFirstN(corners, obstacleCount);
    }
    
    private GridCoordinate[] GetCenterPositions()
    {
        // Central battlefield control positions
        var centerPositions = new GridCoordinate[]
        {
            new GridCoordinate(1, 1), // Center-left
            new GridCoordinate(2, 2), // Center-right
            new GridCoordinate(1, 2), // Center-top
            new GridCoordinate(2, 1), // Center-bottom
            new GridCoordinate(0, 1)  // Edge position
        };
        
        return TakeFirstN(centerPositions, obstacleCount);
    }
    
    private GridCoordinate[] GetStrategicPositions()
    {
        // Optimal tactical placement based on preset
        switch (obstaclePreset)
        {
            case ObstaclePreset.Strategic:
                return GetChokePointPositions();
            case ObstaclePreset.Balanced:
                var balanced = new GridCoordinate[]
                {
                    new GridCoordinate(0, 1), new GridCoordinate(2, 1), new GridCoordinate(1, 3),
                    new GridCoordinate(3, 2), new GridCoordinate(1, 0)
                };
                return TakeFirstN(balanced, obstacleCount);
            case ObstaclePreset.Defensive:
                return GetCornerPositions();
            case ObstaclePreset.Central:
                return GetCenterPositions();
            default:
                return GetChokePointPositions();
        }
    }
    
    /// <summary>
    /// Takes the first N elements from an array, or all elements if N > array length
    /// </summary>
    private GridCoordinate[] TakeFirstN(GridCoordinate[] source, int count)
    {
        int actualCount = Mathf.Min(count, source.Length);
        GridCoordinate[] result = new GridCoordinate[actualCount];
        System.Array.Copy(source, result, actualCount);
        return result;
    }
    
    /// <summary>
    /// Randomizes the positions while maintaining strategic balance
    /// </summary>
    private GridCoordinate[] RandomizePositions(GridCoordinate[] basePositions)
    {
        // Create a pool of all valid grid positions
        var allPositions = new System.Collections.Generic.List<GridCoordinate>();
        for (int x = 0; x < 4; x++)
        {
            for (int z = 0; z < 4; z++)
            {
                allPositions.Add(new GridCoordinate(x, z));
            }
        }
        
        // Shuffle the list
        for (int i = 0; i < allPositions.Count; i++)
        {
            var temp = allPositions[i];
            int randomIndex = UnityEngine.Random.Range(i, allPositions.Count);
            allPositions[i] = allPositions[randomIndex];
            allPositions[randomIndex] = temp;
        }
        
        // Take the requested number of positions
        int actualCount = Mathf.Min(obstacleCount, allPositions.Count);
        GridCoordinate[] result = new GridCoordinate[actualCount];
        for (int i = 0; i < actualCount; i++)
        {
            result[i] = allPositions[i];
        }
        
        return result;
    }
    
    private void SetupObstacles()
    {
        Debug.Log("=== Task 1.1.3: Setting up Obstacles & Terrain ===");
        
        // Ensure grid manager is available and ready
        if (cachedGridManager == null)
        {
            EditorUtility.DisplayDialog("Setup Error", 
                "Grid System not found or not properly initialized. Please run Task 1.1.2 first to create the grid system.", 
                "OK");
            return;
        }
        
        if (logObstacleDetails)
        {
            Debug.Log($"Grid Manager found: Center={cachedGridManager.GridCenter}, Size={cachedGridManager.GridWidth}x{cachedGridManager.GridHeight}, TileSize={cachedGridManager.TileSize}, Spacing={cachedGridManager.TileSpacing}");
        }
        
        // Clean up existing obstacles
        ResetSetup();
        
        // Create obstacle system hierarchy
        GameObject obstacleSystemRoot = CreateObstacleHierarchy();
        
        // Create obstacle manager
        ObstacleManager obstacleManager = CreateObstacleManager(obstacleSystemRoot);
        
        // Place obstacles at calculated positions
        var positions = GetPlacementPositions();
        CreateObstacles(obstacleSystemRoot, positions);
        
        // Create obstacle materials
        CreateObstacleMaterials();
        
        // Update grid system integration
        UpdateGridIntegration(obstacleManager);
        
        // Mark scene as dirty
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        
        Debug.Log("=== Obstacle System setup complete! ===");
        EditorUtility.DisplayDialog("Setup Complete", 
            $"Obstacle System has been created!\\n\\n" +
            $"Obstacles Placed: {positions.Length}\\n" +
            $"Strategy: {placementStrategy}\\n" +
            $"Line-of-Sight: {(enableLineOfSightBlocking ? "Enabled" : "Disabled")}\\n\\n" +
            "Check the Scene view to see the tactical obstacles.", "OK");
    }
    
    private GameObject CreateObstacleHierarchy()
    {
        // Get Environment group from previous tasks
        GameObject environmentGroup = GameObject.Find("Environment");
        if (environmentGroup == null)
        {
            Debug.LogError("Environment group not found. Please run Task 1.1.1 first.");
            return null;
        }
        
        // Create Obstacle System under Environment
        GameObject obstacleSystem = new GameObject("Obstacle System");
        obstacleSystem.transform.SetParent(environmentGroup.transform);
        obstacleSystem.transform.position = Vector3.zero;
        
        // Create sub-groups
        GameObject obstaclesGroup = new GameObject("Obstacles");
        obstaclesGroup.transform.SetParent(obstacleSystem.transform);
        
        Debug.Log("Obstacle hierarchy created under Environment group");
        return obstacleSystem;
    }
    
    private ObstacleManager CreateObstacleManager(GameObject obstacleSystemRoot)
    {
        ObstacleManager obstacleManager = obstacleSystemRoot.AddComponent<ObstacleManager>();
        
        // Configure obstacle manager with current parameters
        var serializedObject = new SerializedObject(obstacleManager);
        serializedObject.FindProperty("enableLineOfSightBlocking").boolValue = enableLineOfSightBlocking;
        serializedObject.FindProperty("enablePartialCover").boolValue = enablePartialCover;
        serializedObject.FindProperty("generateObstacleShadows").boolValue = generateObstacleShadows;
        serializedObject.ApplyModifiedProperties();
        
        Debug.Log($"ObstacleManager created and configured");
        return obstacleManager;
    }
    
    private void CreateObstacles(GameObject obstacleSystemRoot, GridCoordinate[] positions)
    {
        GameObject obstaclesGroup = obstacleSystemRoot.transform.Find("Obstacles").gameObject;
        
        for (int i = 0; i < positions.Length; i++)
        {
            var coord = positions[i];
            var obstacleType = GetObstacleTypeForIndex(i);
            
            // Create obstacle GameObject
            GameObject obstacleObj = new GameObject($"Obstacle_{coord.x}_{coord.z}");
            obstacleObj.transform.SetParent(obstaclesGroup.transform);
            
            // Calculate world position using grid manager
            Vector3 worldPos = cachedGridManager.GridToWorld(coord);
            obstacleObj.transform.position = worldPos;
            
            if (logObstacleDetails)
            {
                // Also check where the corresponding tile is positioned for comparison
                GridTile existingTile = cachedGridManager.GetTile(coord);
                Vector3 tilePosition = existingTile != null ? existingTile.transform.position : Vector3.zero;
                Debug.Log($"Placing obstacle at grid coordinate {coord} -> world position {worldPos}");
                Debug.Log($"Existing tile at {coord} is at position {tilePosition}");
                Debug.Log($"Difference: {worldPos - tilePosition}");
            }
            
            // Add Obstacle component
            Obstacle obstacle = obstacleObj.AddComponent<Obstacle>();
            
            // Configure obstacle with serialized properties
            var serializedObstacle = new SerializedObject(obstacle);
            serializedObstacle.FindProperty("gridCoordinate").FindPropertyRelative("x").intValue = coord.x;
            serializedObstacle.FindProperty("gridCoordinate").FindPropertyRelative("z").intValue = coord.z;
            serializedObstacle.FindProperty("obstacleType").enumValueIndex = (int)obstacleType;
            serializedObstacle.FindProperty("worldPosition").vector3Value = worldPos;
            serializedObstacle.ApplyModifiedProperties();
            
            // Create visual representation
            CreateObstacleVisual(obstacleObj, obstacleType);
            
            // Create collider for line-of-sight blocking
            CreateObstacleCollider(obstacleObj, obstacleType);
            
            // Mark tile as occupied
            GridTile tile = cachedGridManager.GetTile(coord);
            if (tile != null)
            {
                var serializedTile = new SerializedObject(tile);
                serializedTile.FindProperty("isOccupied").boolValue = true;
                serializedTile.FindProperty("occupyingObject").objectReferenceValue = obstacleObj;
                serializedTile.ApplyModifiedProperties();
            }
        }
        
        Debug.Log($"Created {positions.Length} obstacles");
    }
    
    private ObstacleType GetObstacleTypeForIndex(int index)
    {
        // Distribute obstacle types based on configuration
        if (index == 0 && includeHighWalls) return ObstacleType.HighWall;
        if (index == 1 && includeLowCover) return ObstacleType.LowCover;
        if (index == 2 && includeTerrainFeatures) return ObstacleType.Terrain;
        
        // Fallback to available types
        if (includeHighWalls) return ObstacleType.HighWall;
        if (includeLowCover) return ObstacleType.LowCover;
        return ObstacleType.Terrain;
    }
    
    private void CreateObstacleVisual(GameObject obstacleObj, ObstacleType obstacleType)
    {
        GameObject visual;
        float height;
        Color color;
        
        switch (obstacleType)
        {
            case ObstacleType.LowCover:
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                height = lowCoverHeight;
                color = lowCoverColor;
                break;
            case ObstacleType.HighWall:
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                height = highWallHeight;
                color = highWallColor;
                break;
            case ObstacleType.Terrain:
                visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                height = terrainHeight;
                color = terrainColor;
                break;
            default:
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                height = 1f;
                color = Color.gray;
                break;
        }
        
        visual.name = "Visual";
        visual.transform.SetParent(obstacleObj.transform);
        visual.transform.localPosition = Vector3.up * (height / 2f);
        
        if (obstacleType == ObstacleType.Terrain)
        {
            visual.transform.localScale = new Vector3(0.8f, height, 0.8f);
        }
        else
        {
            visual.transform.localScale = new Vector3(0.9f, height, 0.9f);
        }
        
        // Set up material
        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material obstacleMaterial = CreateOrGetObstacleMaterial($"Obstacle_{obstacleType}", color);
            renderer.material = obstacleMaterial;
            renderer.shadowCastingMode = generateObstacleShadows ? 
                UnityEngine.Rendering.ShadowCastingMode.On : 
                UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        
        // Remove default collider since we'll add our own
        DestroyImmediate(visual.GetComponent<Collider>());
    }
    
    private void CreateObstacleCollider(GameObject obstacleObj, ObstacleType obstacleType)
    {
        BoxCollider collider = obstacleObj.AddComponent<BoxCollider>();
        
        float height = obstacleType == ObstacleType.LowCover ? lowCoverHeight :
                      obstacleType == ObstacleType.HighWall ? highWallHeight : terrainHeight;
        
        collider.size = new Vector3(0.9f, height, 0.9f);
        collider.center = new Vector3(0f, height / 2f, 0f);
        collider.isTrigger = false; // Solid collider for line-of-sight blocking
    }
    
    private Material CreateOrGetObstacleMaterial(string materialName, Color color)
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
        
        // Update material properties
        material.color = color;
        material.SetFloat("_Metallic", 0.2f);
        material.SetFloat("_Smoothness", 0.3f);
        
        EditorUtility.SetDirty(material);
        return material;
    }
    
    private void CreateObstacleMaterials()
    {
        // Create materials for different obstacle types
        CreateOrGetObstacleMaterial("Obstacle_LowCover", lowCoverColor);
        CreateOrGetObstacleMaterial("Obstacle_HighWall", highWallColor);
        CreateOrGetObstacleMaterial("Obstacle_Terrain", terrainColor);
        
        AssetDatabase.SaveAssets();
        Debug.Log("Obstacle materials created in Assets/Materials/");
    }
    
    private void UpdateGridIntegration(ObstacleManager obstacleManager)
    {
        // Connect obstacle manager with grid manager
        if (cachedGridManager != null)
        {
            var serializedGrid = new SerializedObject(cachedGridManager);
            serializedGrid.FindProperty("obstacleManager").objectReferenceValue = obstacleManager;
            serializedGrid.ApplyModifiedProperties();
            
            Debug.Log("Grid system integration updated with obstacle manager");
        }
    }
    
    private void ValidateSetup()
    {
        Debug.Log("=== Validating Obstacle System Setup ===");
        
        bool validationPassed = true;
        string validationReport = "Obstacle System Validation Results:\\n\\n";
        
        // Check if obstacle system exists
        GameObject obstacleSystem = GameObject.Find("Obstacle System");
        if (obstacleSystem != null)
        {
            validationReport += "✓ Obstacle System GameObject found\\n";
            
            // Check ObstacleManager
            ObstacleManager obstacleManager = obstacleSystem.GetComponent<ObstacleManager>();
            if (obstacleManager != null)
            {
                validationReport += "✓ ObstacleManager component found\\n";
                validationReport += $"✓ Active obstacles: {obstacleManager.ObstacleCount}\\n";
                
                // Check grid integration
                if (cachedGridManager != null)
                {
                    validationReport += "✓ Grid system integration verified\\n";
                    
                    // Check tile occupation
                    int occupiedTiles = 0;
                    for (int x = 0; x < cachedGridManager.GridWidth; x++)
                    {
                        for (int z = 0; z < cachedGridManager.GridHeight; z++)
                        {
                            GridTile tile = cachedGridManager.GetTile(x, z);
                            if (tile != null && tile.IsOccupied)
                            {
                                occupiedTiles++;
                            }
                        }
                    }
                    
                    validationReport += $"✓ Occupied tiles: {occupiedTiles}\\n";
                }
                else
                {
                    validationReport += "✗ Grid system not found for integration\\n";
                    validationPassed = false;
                }
            }
            else
            {
                validationReport += "✗ ObstacleManager component missing\\n";
                validationPassed = false;
            }
        }
        else
        {
            validationReport += "✗ Obstacle System not found in scene\\n";
            validationPassed = false;
        }
        
        // Check materials
        if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Obstacle_LowCover.mat") != null)
        {
            validationReport += "✓ Obstacle materials created successfully\\n";
        }
        else
        {
            validationReport += "✗ Obstacle materials not found\\n";
            validationPassed = false;
        }
        
        validationReport += "\\n";
        if (validationPassed)
        {
            validationReport += "✓ All validation checks passed! Obstacle system ready for tactical gameplay.";
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
        Debug.Log("=== Resetting Obstacle System ===");
        
        // Remove Obstacle System and all children
        GameObject obstacleSystem = GameObject.Find("Obstacle System");
        if (obstacleSystem != null)
        {
            DestroyImmediate(obstacleSystem);
            Debug.Log("Obstacle System removed from scene");
        }
        
        // Reset tile occupation status
        if (cachedGridManager != null)
        {
            try
            {
                for (int x = 0; x < cachedGridManager.GridWidth; x++)
                {
                    for (int z = 0; z < cachedGridManager.GridHeight; z++)
                    {
                        GridTile tile = cachedGridManager.GetTile(x, z);
                        if (tile != null)
                        {
                            var serializedTile = new SerializedObject(tile);
                            serializedTile.FindProperty("isOccupied").boolValue = false;
                            serializedTile.FindProperty("occupyingObject").objectReferenceValue = null;
                            serializedTile.ApplyModifiedProperties();
                        }
                    }
                }
                Debug.Log("Grid tile occupation status reset");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not reset tile occupation status: {e.Message}. This is normal if the grid system hasn't been fully initialized yet.");
            }
        }
        
        Debug.Log("Obstacle system reset complete");
    }
}