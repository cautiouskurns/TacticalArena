using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

/// <summary>
/// Editor automation tool for Task 1.2.1 - Create Unit System.
/// Creates 4 tactical units (2 blue team, 2 red team) positioned on the grid system
/// with proper team assignment, health tracking, and prefab generation.
/// </summary>
public class Task_1_2_1_Setup : EditorWindow
{
    [Header("Unit Configuration")]
    [SerializeField] private int blueTeamCount = 2;
    [SerializeField] private int redTeamCount = 2;
    [SerializeField] private GridCoordinate[] blueTeamPositions = new GridCoordinate[] { 
        new GridCoordinate(0, 0), 
        new GridCoordinate(1, 0) 
    };
    [SerializeField] private GridCoordinate[] redTeamPositions = new GridCoordinate[] { 
        new GridCoordinate(2, 3), 
        new GridCoordinate(3, 3) 
    };
    
    [Header("Unit Appearance")]
    [SerializeField] private float unitHeight = 1.5f;
    [SerializeField] private Vector3 unitSize = new Vector3(0.8f, 1.5f, 0.8f);
    [SerializeField] private Color blueTeamColor = new Color(0.2f, 0.4f, 0.8f, 1f);
    [SerializeField] private Color redTeamColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    
    [Header("Unit Properties")]
    [SerializeField] private int unitMaxHealth = 3;
    [SerializeField] private int unitCurrentHealth = 3;
    [SerializeField] private float unitMoveSpeed = 2f;
    [SerializeField] private bool enableUnitSelection = true;
    
    [Header("System Configuration")]
    [SerializeField] private bool createPrefabs = true;
    [SerializeField] private bool createMaterials = true;
    [SerializeField] private bool organizeHierarchy = true;
    [SerializeField] private bool enableDebugVisualization = false;
    
    [Header("Integration Settings")]
    [SerializeField] private bool validateGridIntegration = true;
    [SerializeField] private bool checkObstacleOverlap = true;
    [SerializeField] private bool ensureTeamSeparation = true;
    
    private Vector2 scrollPosition;
    private bool validationComplete = false;
    private UnitSystemValidationResult lastValidationResult;
    
    [MenuItem("Tactical Tools/Task 1.2.1 - Create Unit System")]
    public static void ShowWindow()
    {
        Task_1_2_1_Setup window = GetWindow<Task_1_2_1_Setup>("Task 1.2.1 Setup");
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
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("Task 1.2.1 - Create Unit System", headerStyle);
        GUILayout.Space(20);
        
        // Description
        EditorGUILayout.HelpBox(
            "Creates 4 tactical units (2 blue team, 2 red team) positioned on the grid system. " +
            "Establishes unit management architecture with team assignment, health tracking, and prefab generation " +
            "for tactical battlefield gameplay.",
            MessageType.Info);
        
        GUILayout.Space(10);
        
        // Unit Configuration Section
        EditorGUILayout.LabelField("Unit Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        blueTeamCount = EditorGUILayout.IntSlider("Blue Team Count", blueTeamCount, 1, 4);
        redTeamCount = EditorGUILayout.IntSlider("Red Team Count", redTeamCount, 1, 4);
        
        // Position configuration
        EditorGUILayout.LabelField("Blue Team Positions:", EditorStyles.miniBoldLabel);
        for (int i = 0; i < blueTeamCount && i < blueTeamPositions.Length; i++)
        {
            blueTeamPositions[i] = DrawGridCoordinateField($"Blue Unit {i + 1}", blueTeamPositions[i]);
        }
        
        EditorGUILayout.LabelField("Red Team Positions:", EditorStyles.miniBoldLabel);
        for (int i = 0; i < redTeamCount && i < redTeamPositions.Length; i++)
        {
            redTeamPositions[i] = DrawGridCoordinateField($"Red Unit {i + 1}", redTeamPositions[i]);
        }
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Unit Appearance Section
        EditorGUILayout.LabelField("Unit Appearance", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        unitHeight = EditorGUILayout.Slider("Unit Height", unitHeight, 0.5f, 3.0f);
        unitSize = EditorGUILayout.Vector3Field("Unit Size", unitSize);
        blueTeamColor = EditorGUILayout.ColorField("Blue Team Color", blueTeamColor);
        redTeamColor = EditorGUILayout.ColorField("Red Team Color", redTeamColor);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Unit Properties Section
        EditorGUILayout.LabelField("Unit Properties", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        unitMaxHealth = EditorGUILayout.IntSlider("Max Health", unitMaxHealth, 1, 10);
        unitCurrentHealth = EditorGUILayout.IntSlider("Starting Health", unitCurrentHealth, 1, unitMaxHealth);
        unitMoveSpeed = EditorGUILayout.Slider("Move Speed", unitMoveSpeed, 0.5f, 5.0f);
        enableUnitSelection = EditorGUILayout.Toggle("Enable Selection", enableUnitSelection);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // System Configuration Section
        EditorGUILayout.LabelField("System Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        createPrefabs = EditorGUILayout.Toggle("Create Prefabs", createPrefabs);
        createMaterials = EditorGUILayout.Toggle("Create Materials", createMaterials);
        organizeHierarchy = EditorGUILayout.Toggle("Organize Hierarchy", organizeHierarchy);
        enableDebugVisualization = EditorGUILayout.Toggle("Debug Visualization", enableDebugVisualization);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(10);
        
        // Integration Settings Section
        EditorGUILayout.LabelField("Integration Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        validateGridIntegration = EditorGUILayout.Toggle("Validate Grid Integration", validateGridIntegration);
        checkObstacleOverlap = EditorGUILayout.Toggle("Check Obstacle Overlap", checkObstacleOverlap);
        ensureTeamSeparation = EditorGUILayout.Toggle("Ensure Team Separation", ensureTeamSeparation);
        
        EditorGUI.indentLevel--;
        GUILayout.Space(20);
        
        // Action Buttons
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fixedHeight = 30
        };
        
        if (GUILayout.Button("Setup Unit System", buttonStyle))
        {
            SetupUnitSystem();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Validate Setup", buttonStyle))
        {
            ValidateSetup();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Reset/Delete Units", buttonStyle))
        {
            if (EditorUtility.DisplayDialog("Reset Unit System", 
                "This will remove all units and unit-related assets. Continue?", 
                "Yes", "Cancel"))
            {
                ResetSetup();
            }
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Regenerate Prefabs", buttonStyle))
        {
            RegeneratePrefabs();
        }
        
        GUILayout.Space(20);
        
        // Current Status Section
        EditorGUILayout.LabelField("Current Unit System Status", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        DisplayCurrentStatus();
        
        EditorGUI.indentLevel--;
        
        // Validation Results
        if (validationComplete && lastValidationResult != null)
        {
            GUILayout.Space(10);
            DisplayValidationResults();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    /// <summary>
    /// Draws a grid coordinate input field
    /// </summary>
    private GridCoordinate DrawGridCoordinateField(string label, GridCoordinate coordinate)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(100));
        int x = EditorGUILayout.IntField("X", coordinate.x, GUILayout.Width(50));
        int z = EditorGUILayout.IntField("Z", coordinate.z, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();
        
        return new GridCoordinate(x, z);
    }
    
    /// <summary>
    /// Main setup method that creates the complete unit system
    /// </summary>
    private void SetupUnitSystem()
    {
        Debug.Log("=== Task 1.2.1: Setting up Unit System ===");
        
        // Validate prerequisites
        if (!ValidatePrerequisites())
        {
            return;
        }
        
        try
        {
            // Create materials if needed
            if (createMaterials)
            {
                CreateUnitMaterials();
            }
            
            // Setup unit manager
            SetupUnitManager();
            
            // Create blue team units
            CreateTeamUnits(UnitTeam.Blue, blueTeamPositions, blueTeamCount);
            
            // Create red team units
            CreateTeamUnits(UnitTeam.Red, redTeamPositions, redTeamCount);
            
            // Create prefabs if requested
            if (createPrefabs)
            {
                CreateUnitPrefabs();
            }
            
            // Organize scene hierarchy
            if (organizeHierarchy)
            {
                OrganizeSceneHierarchy();
            }
            
            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            
            Debug.Log("=== Unit System setup complete! ===");
            EditorUtility.DisplayDialog("Setup Complete", 
                $"Unit System has been created successfully!\\n\\n" +
                $"✓ {blueTeamCount} Blue team units created\\n" +
                $"✓ {redTeamCount} Red team units created\\n" +
                $"✓ Units positioned on grid coordinates\\n" +
                $"✓ Team assignment and health tracking configured\\n" +
                $"✓ Unit prefabs and materials generated\\n\\n" +
                "Units are ready for selection and movement implementation!", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during unit system setup: {e.Message}");
            EditorUtility.DisplayDialog("Setup Error", 
                $"An error occurred during setup:\\n{e.Message}\\n\\nCheck the console for details.", "OK");
        }
    }
    
    /// <summary>
    /// Validates that prerequisites are met before setup
    /// </summary>
    private bool ValidatePrerequisites()
    {
        bool isValid = true;
        string missingElements = "";
        
        // Check for Grid System
        GameObject gridSystem = GameObject.Find("Grid System");
        if (gridSystem == null)
        {
            missingElements += "- Grid System (Run Task 1.1.2)\\n";
            isValid = false;
        }
        else
        {
            GridManager gridManager = gridSystem.GetComponent<GridManager>();
            if (gridManager == null)
            {
                missingElements += "- GridManager component\\n";
                isValid = false;
            }
        }
        
        // Check for camera
        if (Camera.main == null)
        {
            missingElements += "- Main Camera (Run Task 1.1.1)\\n";
            isValid = false;
        }
        
        // Validate unit positions
        if (validateGridIntegration)
        {
            for (int i = 0; i < blueTeamCount && i < blueTeamPositions.Length; i++)
            {
                if (!IsValidGridPosition(blueTeamPositions[i]))
                {
                    missingElements += $"- Invalid blue team position: {blueTeamPositions[i]}\\n";
                    isValid = false;
                }
            }
            
            for (int i = 0; i < redTeamCount && i < redTeamPositions.Length; i++)
            {
                if (!IsValidGridPosition(redTeamPositions[i]))
                {
                    missingElements += $"- Invalid red team position: {redTeamPositions[i]}\\n";
                    isValid = false;
                }
            }
        }
        
        if (!isValid)
        {
            EditorUtility.DisplayDialog("Prerequisites Missing", 
                $"Please resolve the following issues first:\\n\\n{missingElements}", "OK");
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Creates materials for unit teams
    /// </summary>
    private void CreateUnitMaterials()
    {
        // Ensure Materials folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        
        // Create blue team material
        CreateUnitMaterial("BlueTeam", blueTeamColor);
        
        // Create red team material
        CreateUnitMaterial("RedTeam", redTeamColor);
        
        AssetDatabase.SaveAssets();
        Debug.Log("Unit team materials created");
    }
    
    /// <summary>
    /// Creates a material for a unit team
    /// </summary>
    private void CreateUnitMaterial(string materialName, Color color)
    {
        string materialPath = $"Assets/Materials/{materialName}.mat";
        
        // Check if material already exists
        Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (existingMaterial != null)
        {
            existingMaterial.color = color;
            EditorUtility.SetDirty(existingMaterial);
            return;
        }
        
        // Create new material
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = materialName;
        material.color = color;
        
        // Configure for clean aesthetic
        material.SetFloat("_Metallic", 0.1f);
        material.SetFloat("_Smoothness", 0.2f);
        material.SetFloat("_Surface", 0); // Opaque
        
        AssetDatabase.CreateAsset(material, materialPath);
        Debug.Log($"Created unit material: {materialPath}");
    }
    
    /// <summary>
    /// Sets up or finds the unit manager
    /// </summary>
    private void SetupUnitManager()
    {
        GameObject unitManagerObj = GameObject.Find("Unit Manager");
        if (unitManagerObj == null)
        {
            // Create under Units group
            GameObject unitsGroup = GameObject.Find("Units");
            if (unitsGroup == null)
            {
                unitsGroup = new GameObject("Units");
            }
            
            unitManagerObj = new GameObject("Unit Manager");
            unitManagerObj.transform.SetParent(unitsGroup.transform);
        }
        
        UnitManager unitManager = unitManagerObj.GetComponent<UnitManager>();
        if (unitManager == null)
        {
            unitManager = unitManagerObj.AddComponent<UnitManager>();
        }
        
        // Configure unit manager
        var serializedManager = new SerializedObject(unitManager);
        serializedManager.FindProperty("enableDebugVisualization").boolValue = enableDebugVisualization;
        serializedManager.FindProperty("validateGridPositions").boolValue = validateGridIntegration;
        serializedManager.ApplyModifiedProperties();
        
        Debug.Log("Unit Manager configured");
    }
    
    /// <summary>
    /// Creates units for a specific team
    /// </summary>
    private void CreateTeamUnits(UnitTeam team, GridCoordinate[] positions, int count)
    {
        GameObject teamGroup = GetOrCreateTeamGroup(team);
        Color teamColor = team == UnitTeam.Blue ? blueTeamColor : redTeamColor;
        string materialName = team == UnitTeam.Blue ? "BlueTeam" : "RedTeam";
        
        for (int i = 0; i < count && i < positions.Length; i++)
        {
            CreateUnit(team, positions[i], i + 1, teamGroup, teamColor, materialName);
        }
        
        Debug.Log($"Created {count} {team} team units");
    }
    
    /// <summary>
    /// Gets or creates a team group in the hierarchy
    /// </summary>
    private GameObject GetOrCreateTeamGroup(UnitTeam team)
    {
        string teamName = team == UnitTeam.Blue ? "Blue Team" : "Red Team";
        GameObject teamGroup = GameObject.Find(teamName);
        
        if (teamGroup == null)
        {
            GameObject unitsGroup = GameObject.Find("Units");
            if (unitsGroup == null)
            {
                unitsGroup = new GameObject("Units");
            }
            
            teamGroup = new GameObject(teamName);
            teamGroup.transform.SetParent(unitsGroup.transform);
        }
        
        return teamGroup;
    }
    
    /// <summary>
    /// Creates an individual unit
    /// </summary>
    private void CreateUnit(UnitTeam team, GridCoordinate gridPos, int unitNumber, GameObject parent, Color teamColor, string materialName)
    {
        // Create unit GameObject
        string unitName = $"{team} Unit {unitNumber}";
        GameObject existingUnit = GameObject.Find(unitName);
        if (existingUnit != null)
        {
            DestroyImmediate(existingUnit);
        }
        
        GameObject unitObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        unitObj.name = unitName;
        unitObj.transform.SetParent(parent.transform);
        
        // Scale and position unit
        unitObj.transform.localScale = unitSize;
        PositionUnitOnGrid(unitObj, gridPos);
        
        // Apply material
        ApplyUnitMaterial(unitObj, materialName, teamColor);
        
        // Add unit components
        AddUnitComponents(unitObj, team, gridPos);
        
        // Register with grid system
        RegisterUnitWithGrid(unitObj, gridPos);
        
        Debug.Log($"Created {unitName} at grid position {gridPos}");
    }
    
    /// <summary>
    /// Positions a unit on the grid coordinate system
    /// </summary>
    private void PositionUnitOnGrid(GameObject unit, GridCoordinate gridPos)
    {
        GameObject gridSystem = GameObject.Find("Grid System");
        if (gridSystem != null)
        {
            GridManager gridManager = gridSystem.GetComponent<GridManager>();
            if (gridManager != null)
            {
                Vector3 worldPos = gridManager.GridToWorld(gridPos);
                worldPos.y = unitHeight / 2f; // Position unit on ground
                unit.transform.position = worldPos;
                return;
            }
            else
            {
                Debug.LogError("GridManager component not found on Grid System");
            }
        }
        else
        {
            Debug.LogError("Grid System GameObject not found");
        }
        
        // Fallback positioning
        Vector3 fallbackPos = new Vector3(gridPos.x, unitHeight / 2f, gridPos.z);
        unit.transform.position = fallbackPos;
        Debug.LogWarning($"Using fallback positioning for unit at {gridPos} -> {fallbackPos}");
    }
    
    /// <summary>
    /// Applies material to unit
    /// </summary>
    private void ApplyUnitMaterial(GameObject unit, string materialName, Color teamColor)
    {
        Renderer renderer = unit.GetComponent<Renderer>();
        if (renderer != null)
        {
            string materialPath = $"Assets/Materials/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            
            if (material != null)
            {
                renderer.material = material;
            }
            else
            {
                // Create material on the fly if not found
                Material fallbackMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                fallbackMaterial.color = teamColor;
                renderer.material = fallbackMaterial;
                Debug.LogWarning($"Using fallback material for {materialName}");
            }
        }
    }
    
    /// <summary>
    /// Adds required components to unit
    /// </summary>
    private void AddUnitComponents(GameObject unitObj, UnitTeam team, GridCoordinate gridPos)
    {
        // Add Unit component
        Unit unit = unitObj.GetComponent<Unit>();
        if (unit == null)
        {
            unit = unitObj.AddComponent<Unit>();
        }
        
        // Configure unit properties using public methods and serialization
        var serializedUnit = new SerializedObject(unit);
        serializedUnit.FindProperty("moveSpeed").floatValue = unitMoveSpeed;
        serializedUnit.FindProperty("enableSelection").boolValue = enableUnitSelection;
        serializedUnit.ApplyModifiedProperties();
        
        // Set team via serialization to ensure it's properly saved in editor
        serializedUnit.FindProperty("team").enumValueIndex = (int)team;
        
        // Set GridCoordinate via serialization to avoid repositioning conflicts
        var gridCoordProp = serializedUnit.FindProperty("gridCoordinate");
        gridCoordProp.FindPropertyRelative("x").intValue = gridPos.x;
        gridCoordProp.FindPropertyRelative("z").intValue = gridPos.z;
        serializedUnit.ApplyModifiedProperties();
        
        // Add UnitHealth component
        UnitHealth health = unitObj.GetComponent<UnitHealth>();
        if (health == null)
        {
            health = unitObj.AddComponent<UnitHealth>();
        }
        
        // Configure health properties
        var serializedHealth = new SerializedObject(health);
        serializedHealth.FindProperty("maxHealth").intValue = unitMaxHealth;
        serializedHealth.FindProperty("currentHealth").intValue = unitCurrentHealth;
        serializedHealth.ApplyModifiedProperties();
    }
    
    /// <summary>
    /// Registers unit with grid system
    /// </summary>
    private void RegisterUnitWithGrid(GameObject unitObj, GridCoordinate gridPos)
    {
        GameObject gridSystem = GameObject.Find("Grid System");
        if (gridSystem != null)
        {
            GridManager gridManager = gridSystem.GetComponent<GridManager>();
            if (gridManager != null)
            {
                GridTile tile = gridManager.GetTile(gridPos);
                if (tile != null)
                {
                    tile.OccupyTile(unitObj);
                }
            }
        }
    }
    
    /// <summary>
    /// Creates unit prefabs for reuse
    /// </summary>
    private void CreateUnitPrefabs()
    {
        // Ensure Prefabs folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Units"))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Units");
        }
        
        // Create blue team prefab
        CreateUnitPrefab(UnitTeam.Blue, "BlueTeam");
        
        // Create red team prefab
        CreateUnitPrefab(UnitTeam.Red, "RedTeam");
        
        AssetDatabase.SaveAssets();
        Debug.Log("Unit prefabs created");
    }
    
    /// <summary>
    /// Creates a prefab for a specific team
    /// </summary>
    private void CreateUnitPrefab(UnitTeam team, string materialName)
    {
        string prefabName = $"{team}Unit";
        string prefabPath = $"Assets/Prefabs/Units/{prefabName}.prefab";
        
        // Create temporary unit for prefab
        GameObject tempUnit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tempUnit.name = prefabName;
        tempUnit.transform.localScale = unitSize;
        
        // Apply material
        Color teamColor = team == UnitTeam.Blue ? blueTeamColor : redTeamColor;
        ApplyUnitMaterial(tempUnit, materialName, teamColor);
        
        // Add components
        AddUnitComponents(tempUnit, team, new GridCoordinate(0, 0));
        
        // Create prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempUnit, prefabPath);
        
        // Clean up temporary object
        DestroyImmediate(tempUnit);
        
        Debug.Log($"Created unit prefab: {prefabPath}");
    }
    
    /// <summary>
    /// Organizes the scene hierarchy for units
    /// </summary>
    private void OrganizeSceneHierarchy()
    {
        GameObject unitsGroup = GameObject.Find("Units");
        if (unitsGroup != null)
        {
            // Ensure proper ordering
            GameObject blueTeam = GameObject.Find("Blue Team");
            GameObject redTeam = GameObject.Find("Red Team");
            GameObject unitManager = GameObject.Find("Unit Manager");
            
            if (blueTeam != null) blueTeam.transform.SetSiblingIndex(0);
            if (redTeam != null) redTeam.transform.SetSiblingIndex(1);
            if (unitManager != null) unitManager.transform.SetSiblingIndex(2);
        }
        
        Debug.Log("Scene hierarchy organized");
    }
    
    /// <summary>
    /// Regenerates unit prefabs
    /// </summary>
    private void RegeneratePrefabs()
    {
        if (createMaterials)
        {
            CreateUnitMaterials();
        }
        
        CreateUnitPrefabs();
        
        EditorUtility.DisplayDialog("Prefabs Regenerated", 
            "Unit prefabs have been regenerated with current settings.", "OK");
    }
    
    /// <summary>
    /// Validates grid position
    /// </summary>
    private bool IsValidGridPosition(GridCoordinate pos)
    {
        GameObject gridSystem = GameObject.Find("Grid System");
        if (gridSystem != null)
        {
            GridManager gridManager = gridSystem.GetComponent<GridManager>();
            if (gridManager != null)
            {
                return gridManager.IsValidCoordinate(pos);
            }
        }
        
        // Fallback validation for 4x4 grid
        return pos.x >= 0 && pos.x < 4 && pos.z >= 0 && pos.z < 4;
    }
    
    /// <summary>
    /// Displays current system status
    /// </summary>
    private void DisplayCurrentStatus()
    {
        // Count existing units - search all GameObjects, not just "Untagged" ones
        GameObject[] blueUnits = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Where(go => go.name.Contains("Blue Unit")).ToArray();
        GameObject[] redUnits = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Where(go => go.name.Contains("Red Unit")).ToArray();
        
        EditorGUILayout.LabelField($"Blue Team Units: {blueUnits.Length}");
        EditorGUILayout.LabelField($"Red Team Units: {redUnits.Length}");
        EditorGUILayout.LabelField($"Total Units: {blueUnits.Length + redUnits.Length}");
        
        // Check for unit manager
        GameObject unitManager = GameObject.Find("Unit Manager");
        if (unitManager != null)
        {
            EditorGUILayout.LabelField("✓ Unit Manager Found");
        }
        else
        {
            EditorGUILayout.LabelField("○ Unit Manager Not Created");
        }
        
        // Check for prefabs
        bool bluePreab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/BlueUnit.prefab") != null;
        bool redPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/RedUnit.prefab") != null;
        
        if (bluePreab && redPrefab)
        {
            EditorGUILayout.LabelField("✓ Unit Prefabs Created");
        }
        else
        {
            EditorGUILayout.LabelField("○ Unit Prefabs Missing");
        }
    }
    
    /// <summary>
    /// Validates the unit system setup
    /// </summary>
    private void ValidateSetup()
    {
        Debug.Log("=== Validating Unit System Setup ===");
        
        UnitSystemValidationResult result = new UnitSystemValidationResult();
        
        // Test 1: Check unit count
        result.unitsCreated = ValidateUnitCount();
        
        // Test 2: Check team assignment
        result.teamAssignment = ValidateTeamAssignment();
        
        // Test 3: Check grid positioning
        result.gridPositioning = ValidateGridPositioning();
        
        // Test 4: Check component setup
        result.componentSetup = ValidateComponentSetup();
        
        // Test 5: Check prefab creation
        result.prefabCreation = ValidatePrefabCreation();
        
        // Test 6: Check material assignment
        result.materialAssignment = ValidateMaterialAssignment();
        
        // Calculate overall result
        result.CalculateOverallResult();
        
        lastValidationResult = result;
        validationComplete = true;
        
        // Log results
        LogValidationResults(result);
        
        Debug.Log("=== Unit System Validation Complete ===");
    }
    
    /// <summary>
    /// Validates unit count
    /// </summary>
    private bool ValidateUnitCount()
    {
        // Find all GameObjects in the scene, not just those with "Untagged" tag
        GameObject[] blueUnits = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Where(go => go.name.Contains("Blue Unit")).ToArray();
        GameObject[] redUnits = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Where(go => go.name.Contains("Red Unit")).ToArray();
        
        bool countValid = blueUnits.Length == blueTeamCount && redUnits.Length == redTeamCount;
        
        if (countValid)
        {
            Debug.Log($"✓ Unit count validated: {blueUnits.Length} blue, {redUnits.Length} red");
        }
        else
        {
            Debug.LogError($"✗ Unit count mismatch: Expected {blueTeamCount} blue, {redTeamCount} red. Found {blueUnits.Length} blue, {redUnits.Length} red");
        }
        
        return countValid;
    }
    
    /// <summary>
    /// Validates team assignment
    /// </summary>
    private bool ValidateTeamAssignment()
    {
        GameObject[] allUnits = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Where(go => go.name.Contains("Unit") && !go.name.Contains("Unit Manager") && !go.name.Equals("Units")).ToArray();
        
        foreach (GameObject unitObj in allUnits)
        {
            Unit unit = unitObj.GetComponent<Unit>();
            if (unit == null)
            {
                Debug.LogError($"✗ Unit {unitObj.name} missing Unit component");
                return false;
            }
            
            bool isBlue = unitObj.name.Contains("Blue");
            bool isRed = unitObj.name.Contains("Red");
            
            if (isBlue && unit.Team != UnitTeam.Blue)
            {
                Debug.LogError($"✗ Blue unit {unitObj.name} has wrong team assignment");
                return false;
            }
            
            if (isRed && unit.Team != UnitTeam.Red)
            {
                Debug.LogError($"✗ Red unit {unitObj.name} has wrong team assignment");
                return false;
            }
        }
        
        Debug.Log("✓ Team assignment validated");
        return true;
    }
    
    /// <summary>
    /// Validates grid positioning
    /// </summary>
    private bool ValidateGridPositioning()
    {
        GameObject gridSystem = GameObject.Find("Grid System");
        if (gridSystem == null)
        {
            Debug.LogError("✗ Grid System not found");
            return false;
        }
        
        GridManager gridManager = gridSystem.GetComponent<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("✗ GridManager not found");
            return false;
        }
        
        GameObject[] allUnits = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Where(go => go.name.Contains("Unit") && !go.name.Contains("Unit Manager") && !go.name.Equals("Units")).ToArray();
        
        foreach (GameObject unitObj in allUnits)
        {
            Unit unit = unitObj.GetComponent<Unit>();
            if (unit != null)
            {
                GridCoordinate gridPos = unit.GridCoordinate;
                if (!gridManager.IsValidCoordinate(gridPos))
                {
                    Debug.LogError($"✗ Unit {unitObj.name} at invalid grid position {gridPos}");
                    return false;
                }
                
                Vector3 expectedWorldPos = gridManager.GridToWorld(gridPos);
                // Adjust expected Y position to match unit positioning logic
                expectedWorldPos.y = unitHeight / 2f;
                Vector3 actualWorldPos = unitObj.transform.position;
                
                
                if (Vector3.Distance(expectedWorldPos, actualWorldPos) > 0.1f)
                {
                    Debug.LogError($"✗ Unit {unitObj.name} position mismatch. Expected {expectedWorldPos}, actual {actualWorldPos}");
                    return false;
                }
            }
        }
        
        Debug.Log("✓ Grid positioning validated");
        return true;
    }
    
    /// <summary>
    /// Validates component setup
    /// </summary>
    private bool ValidateComponentSetup()
    {
        GameObject[] allUnits = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Where(go => go.name.Contains("Unit") && !go.name.Contains("Unit Manager") && !go.name.Equals("Units")).ToArray();
        
        foreach (GameObject unitObj in allUnits)
        {
            Unit unit = unitObj.GetComponent<Unit>();
            UnitHealth health = unitObj.GetComponent<UnitHealth>();
            
            if (unit == null)
            {
                Debug.LogError($"✗ Unit {unitObj.name} missing Unit component");
                return false;
            }
            
            if (health == null)
            {
                Debug.LogError($"✗ Unit {unitObj.name} missing UnitHealth component");
                return false;
            }
            
            if (health.MaxHealth != unitMaxHealth)
            {
                Debug.LogError($"✗ Unit {unitObj.name} has wrong max health: {health.MaxHealth} (expected {unitMaxHealth})");
                return false;
            }
        }
        
        Debug.Log("✓ Component setup validated");
        return true;
    }
    
    /// <summary>
    /// Validates prefab creation
    /// </summary>
    private bool ValidatePrefabCreation()
    {
        if (!createPrefabs) return true;
        
        bool blueExists = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/BlueUnit.prefab") != null;
        bool redExists = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/RedUnit.prefab") != null;
        
        if (blueExists && redExists)
        {
            Debug.Log("✓ Unit prefabs validated");
            return true;
        }
        else
        {
            Debug.LogError("✗ Unit prefabs missing");
            return false;
        }
    }
    
    /// <summary>
    /// Validates material assignment
    /// </summary>
    private bool ValidateMaterialAssignment()
    {
        GameObject[] blueUnits = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Where(go => go.name.Contains("Blue Unit")).ToArray();
        GameObject[] redUnits = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Where(go => go.name.Contains("Red Unit")).ToArray();
        
        // Check blue units
        foreach (GameObject unit in blueUnits)
        {
            Renderer renderer = unit.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                Debug.LogError($"✗ Blue unit {unit.name} missing material");
                return false;
            }
            
            Color materialColor = renderer.sharedMaterial.color;
            if (Vector4.Distance(materialColor, blueTeamColor) > 0.2f)
            {
                Debug.LogWarning($"⚠ Blue unit {unit.name} color may not match team color");
            }
        }
        
        // Check red units
        foreach (GameObject unit in redUnits)
        {
            Renderer renderer = unit.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                Debug.LogError($"✗ Red unit {unit.name} missing material");
                return false;
            }
            
            Color materialColor = renderer.sharedMaterial.color;
            if (Vector4.Distance(materialColor, redTeamColor) > 0.2f)
            {
                Debug.LogWarning($"⚠ Red unit {unit.name} color may not match team color");
            }
        }
        
        Debug.Log("✓ Material assignment validated");
        return true;
    }
    
    /// <summary>
    /// Displays validation results
    /// </summary>
    private void DisplayValidationResults()
    {
        EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        // Overall result
        MessageType overallMessageType = lastValidationResult.overallSuccess ? MessageType.Info : MessageType.Warning;
        string overallMessage = lastValidationResult.overallSuccess ? 
            "✓ All validation tests passed! Unit system is ready for tactical gameplay." :
            "⚠ Some validation tests failed. Check the console for details.";
        
        EditorGUILayout.HelpBox(overallMessage, overallMessageType);
        GUILayout.Space(10);
        
        // Individual test results
        DrawTestResult("Units Created", lastValidationResult.unitsCreated);
        DrawTestResult("Team Assignment", lastValidationResult.teamAssignment);
        DrawTestResult("Grid Positioning", lastValidationResult.gridPositioning);
        DrawTestResult("Component Setup", lastValidationResult.componentSetup);
        DrawTestResult("Prefab Creation", lastValidationResult.prefabCreation);
        DrawTestResult("Material Assignment", lastValidationResult.materialAssignment);
        
        GUILayout.Space(10);
        
        // Summary
        EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Tests Passed: {lastValidationResult.GetPassedCount()}/6");
        EditorGUILayout.LabelField($"System Status: {(lastValidationResult.overallSuccess ? "Ready" : "Needs Attention")}");
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
    private void LogValidationResults(UnitSystemValidationResult result)
    {
        string summary = $"\\n=== UNIT SYSTEM VALIDATION SUMMARY ===\\n" +
                        $"Units Created: {(result.unitsCreated ? "PASS" : "FAIL")}\\n" +
                        $"Team Assignment: {(result.teamAssignment ? "PASS" : "FAIL")}\\n" +
                        $"Grid Positioning: {(result.gridPositioning ? "PASS" : "FAIL")}\\n" +
                        $"Component Setup: {(result.componentSetup ? "PASS" : "FAIL")}\\n" +
                        $"Prefab Creation: {(result.prefabCreation ? "PASS" : "FAIL")}\\n" +
                        $"Material Assignment: {(result.materialAssignment ? "PASS" : "FAIL")}\\n" +
                        $"\\nOVERALL: {(result.overallSuccess ? "SUCCESS" : "NEEDS ATTENTION")}\\n" +
                        $"Tests Passed: {result.GetPassedCount()}/6\\n";
        
        if (result.overallSuccess)
        {
            Debug.Log(summary);
        }
        else
        {
            Debug.LogWarning(summary);
        }
    }
    
    /// <summary>
    /// Resets the unit system
    /// </summary>
    private void ResetSetup()
    {
        Debug.Log("=== Resetting Unit System ===");
        
        // Remove unit-related GameObjects
        GameObject unitsGroup = GameObject.Find("Units");
        if (unitsGroup != null)
        {
            DestroyImmediate(unitsGroup);
            Debug.Log("Units group removed");
        }
        
        // Clear tile occupations
        GameObject gridSystem = GameObject.Find("Grid System");
        if (gridSystem != null)
        {
            GridManager gridManager = gridSystem.GetComponent<GridManager>();
            if (gridManager != null)
            {
                for (int x = 0; x < 4; x++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        GridTile tile = gridManager.GetTile(x, z);
                        if (tile != null)
                        {
                            tile.ClearOccupation();
                        }
                    }
                }
            }
        }
        
        Debug.Log("Unit system reset complete");
        EditorUtility.DisplayDialog("Reset Complete", "Unit system has been reset.", "OK");
    }
}

/// <summary>
/// Structure to hold unit system validation results
/// </summary>
[System.Serializable]
public class UnitSystemValidationResult
{
    public bool unitsCreated = false;
    public bool teamAssignment = false;
    public bool gridPositioning = false;
    public bool componentSetup = false;
    public bool prefabCreation = false;
    public bool materialAssignment = false;
    public bool overallSuccess = false;
    
    public void CalculateOverallResult()
    {
        overallSuccess = unitsCreated && 
                        teamAssignment && 
                        gridPositioning && 
                        componentSetup && 
                        prefabCreation && 
                        materialAssignment;
    }
    
    public int GetPassedCount()
    {
        int count = 0;
        if (unitsCreated) count++;
        if (teamAssignment) count++;
        if (gridPositioning) count++;
        if (componentSetup) count++;
        if (prefabCreation) count++;
        if (materialAssignment) count++;
        return count;
    }
}