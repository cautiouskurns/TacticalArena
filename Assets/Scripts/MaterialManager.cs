using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized material management system for the tactical arena environment.
/// Handles material creation, application, and dynamic updates for visual polish.
/// </summary>
public class MaterialManager : MonoBehaviour
{
    [Header("Grid Material Configuration")]
    [SerializeField] private Color gridLineColor = Color.white;
    [SerializeField] private Color gridTileDefaultColor = new Color(0.9f, 0.9f, 0.9f, 0.1f);
    [SerializeField] private Color gridTileHoverColor = Color.yellow;
    [SerializeField] private Color gridTileSelectedColor = Color.green;
    [SerializeField] private Color gridTileBlockedColor = Color.red;
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.3f, 0.4f);
    
    [Header("Obstacle Material Configuration")]
    [SerializeField] private Material obstacleBaseMaterial;
    [SerializeField] private float obstacleMetallic = 0.1f;
    [SerializeField] private float obstacleSmoothness = 0.2f;
    
    [Header("Environment Material Configuration")]
    [SerializeField] private Material floorMaterial;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private bool useCleanAesthetic = true;
    
    [Header("Dynamic Material Settings")]
    [SerializeField] private bool enableMaterialUpdates = true;
    [SerializeField] private float materialUpdateInterval = 0.1f;
    [SerializeField] private bool enableGradientEffects = false;
    
    [Header("Shader Configuration")]
    [SerializeField] private Shader standardShader;
    [SerializeField] private Shader unlitShader;
    [SerializeField] private Shader transparentShader;
    
    // Material cache for performance
    private Dictionary<string, Material> materialCache;
    private Dictionary<ObstacleType, Material> obstacleMaterials;
    private Dictionary<GridTileState, Material> tileMaterials;
    
    // Component references
    private GridManager gridManager;
    private ObstacleManager obstacleManager;
    private VisualFeedbackManager visualFeedbackManager;
    
    // Update tracking
    private float lastUpdateTime;
    private bool materialsInitialized = false;
    
    // Events for material changes
    public System.Action<Material, Color> OnMaterialColorChanged;
    public System.Action OnMaterialsUpdated;
    
    // Public properties
    public Color GridLineColor => gridLineColor;
    public Color GridTileDefaultColor => gridTileDefaultColor;
    public Color GridTileHoverColor => gridTileHoverColor;
    public Color GridTileSelectedColor => gridTileSelectedColor;
    public Color GridTileBlockedColor => gridTileBlockedColor;
    public Color BackgroundColor => backgroundColor;
    public bool UseCleanAesthetic => useCleanAesthetic;
    public bool MaterialUpdatesEnabled => enableMaterialUpdates;
    
    void Awake()
    {
        InitializeMaterialSystem();
    }
    
    void Start()
    {
        FindManagerReferences();
        CreateMaterialCache();
        ApplyEnvironmentMaterials();
    }
    
    void Update()
    {
        if (enableMaterialUpdates && Time.time - lastUpdateTime >= materialUpdateInterval)
        {
            UpdateDynamicMaterials();
            lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// Initializes the material management system
    /// </summary>
    private void InitializeMaterialSystem()
    {
        materialCache = new Dictionary<string, Material>();
        obstacleMaterials = new Dictionary<ObstacleType, Material>();
        tileMaterials = new Dictionary<GridTileState, Material>();
        
        // Find default URP shaders if not assigned
        if (standardShader == null)
            standardShader = Shader.Find("Universal Render Pipeline/Lit");
        if (unlitShader == null)
            unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (transparentShader == null)
            transparentShader = Shader.Find("Universal Render Pipeline/Lit");
        
        Debug.Log("MaterialManager: System initialized");
    }
    
    /// <summary>
    /// Finds references to other manager components
    /// </summary>
    private void FindManagerReferences()
    {
        GameObject gridSystem = GameObject.Find("Grid System");
        if (gridSystem != null)
        {
            gridManager = gridSystem.GetComponent<GridManager>();
        }
        
        GameObject obstacleSystem = GameObject.Find("Obstacle System");
        if (obstacleSystem != null)
        {
            obstacleManager = obstacleSystem.GetComponent<ObstacleManager>();
        }
        
        GameObject feedbackSystem = GameObject.Find("Visual Feedback Manager");
        if (feedbackSystem != null)
        {
            visualFeedbackManager = feedbackSystem.GetComponent<VisualFeedbackManager>();
        }
    }
    
    /// <summary>
    /// Creates and caches all materials used by the system
    /// </summary>
    private void CreateMaterialCache()
    {
        CreateGridTileMaterials();
        CreateObstacleMaterials();
        CreateEnvironmentMaterials();
        
        materialsInitialized = true;
        Debug.Log("MaterialManager: Material cache created");
    }
    
    /// <summary>
    /// Creates materials for grid tiles
    /// </summary>
    private void CreateGridTileMaterials()
    {
        // Grid line material
        Material gridLineMat = CreateMaterial("GridLine", gridLineColor, unlitShader);
        gridLineMat.SetFloat("_Metallic", 0.0f);
        gridLineMat.SetFloat("_Smoothness", 0.0f);
        materialCache["GridLine"] = gridLineMat;
        tileMaterials[GridTileState.Normal] = gridLineMat;
        
        // Default tile material
        Material defaultTileMat = CreateMaterial("GridTile_Default", gridTileDefaultColor, transparentShader);
        ConfigureTransparentMaterial(defaultTileMat);
        materialCache["GridTile_Default"] = defaultTileMat;
        tileMaterials[GridTileState.Normal] = defaultTileMat;
        
        // Hover tile material
        Material hoverTileMat = CreateMaterial("GridTile_Hover", gridTileHoverColor, standardShader);
        ConfigureTileMaterial(hoverTileMat);
        materialCache["GridTile_Hover"] = hoverTileMat;
        tileMaterials[GridTileState.Hovered] = hoverTileMat;
        
        // Selected tile material
        Material selectedTileMat = CreateMaterial("GridTile_Selected", gridTileSelectedColor, standardShader);
        ConfigureTileMaterial(selectedTileMat);
        materialCache["GridTile_Selected"] = selectedTileMat;
        tileMaterials[GridTileState.Selected] = selectedTileMat;
        
        // Blocked tile material
        Material blockedTileMat = CreateMaterial("GridTile_Blocked", gridTileBlockedColor, standardShader);
        ConfigureTileMaterial(blockedTileMat);
        materialCache["GridTile_Blocked"] = blockedTileMat;
        tileMaterials[GridTileState.Blocked] = blockedTileMat;
    }
    
    /// <summary>
    /// Creates materials for different obstacle types
    /// </summary>
    private void CreateObstacleMaterials()
    {
        // Low Cover obstacle
        ObstacleData lowCoverData = ObstacleData.GetDefault(ObstacleType.LowCover);
        Material lowCoverMat = CreateMaterial("Obstacle_LowCover", lowCoverData.defaultColor, standardShader);
        ConfigureObstacleMaterial(lowCoverMat);
        materialCache["Obstacle_LowCover"] = lowCoverMat;
        obstacleMaterials[ObstacleType.LowCover] = lowCoverMat;
        
        // High Wall obstacle
        ObstacleData highWallData = ObstacleData.GetDefault(ObstacleType.HighWall);
        Material highWallMat = CreateMaterial("Obstacle_HighWall", highWallData.defaultColor, standardShader);
        ConfigureObstacleMaterial(highWallMat);
        materialCache["Obstacle_HighWall"] = highWallMat;
        obstacleMaterials[ObstacleType.HighWall] = highWallMat;
        
        // Terrain obstacle
        ObstacleData terrainData = ObstacleData.GetDefault(ObstacleType.Terrain);
        Material terrainMat = CreateMaterial("Obstacle_Terrain", terrainData.defaultColor, standardShader);
        ConfigureObstacleMaterial(terrainMat);
        materialCache["Obstacle_Terrain"] = terrainMat;
        obstacleMaterials[ObstacleType.Terrain] = terrainMat;
    }
    
    /// <summary>
    /// Creates materials for environment elements
    /// </summary>
    private void CreateEnvironmentMaterials()
    {
        // Floor material
        if (floorMaterial == null)
        {
            floorMaterial = CreateMaterial("Environment_Floor", new Color(0.8f, 0.8f, 0.8f), standardShader);
            ConfigureEnvironmentMaterial(floorMaterial);
        }
        materialCache["Environment_Floor"] = floorMaterial;
        
        // Wall material
        if (wallMaterial == null)
        {
            wallMaterial = CreateMaterial("Environment_Wall", new Color(0.7f, 0.7f, 0.7f), standardShader);
            ConfigureEnvironmentMaterial(wallMaterial);
        }
        materialCache["Environment_Wall"] = wallMaterial;
    }
    
    /// <summary>
    /// Creates a new material with specified properties
    /// </summary>
    private Material CreateMaterial(string name, Color color, Shader shader)
    {
        Material material = new Material(shader);
        material.name = name;
        material.color = color;
        
        if (useCleanAesthetic)
        {
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.1f);
        }
        
        return material;
    }
    
    /// <summary>
    /// Configures a material for transparent rendering
    /// </summary>
    private void ConfigureTransparentMaterial(Material material)
    {
        if (material.shader.name.Contains("Standard"))
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
    }
    
    /// <summary>
    /// Configures a material for tile rendering
    /// </summary>
    private void ConfigureTileMaterial(Material material)
    {
        if (useCleanAesthetic)
        {
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.1f);
        }
        else
        {
            material.SetFloat("_Metallic", 0.2f);
            material.SetFloat("_Smoothness", 0.5f);
        }
    }
    
    /// <summary>
    /// Configures a material for obstacle rendering
    /// </summary>
    private void ConfigureObstacleMaterial(Material material)
    {
        material.SetFloat("_Metallic", obstacleMetallic);
        material.SetFloat("_Smoothness", obstacleSmoothness);
        
        if (useCleanAesthetic)
        {
            material.SetFloat("_Metallic", Mathf.Min(obstacleMetallic, 0.1f));
            material.SetFloat("_Smoothness", Mathf.Min(obstacleSmoothness, 0.2f));
        }
    }
    
    /// <summary>
    /// Configures a material for environment rendering
    /// </summary>
    private void ConfigureEnvironmentMaterial(Material material)
    {
        if (useCleanAesthetic)
        {
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.1f);
        }
        else
        {
            material.SetFloat("_Metallic", 0.1f);
            material.SetFloat("_Smoothness", 0.3f);
        }
    }
    
    /// <summary>
    /// Applies materials to all environment elements
    /// </summary>
    public void ApplyEnvironmentMaterials()
    {
        if (!materialsInitialized) return;
        
        ApplyGridMaterials();
        ApplyObstacleMaterials();
        ApplyCameraMaterials();
        
        OnMaterialsUpdated?.Invoke();
        Debug.Log("MaterialManager: Environment materials applied");
    }
    
    /// <summary>
    /// Applies materials to grid system
    /// </summary>
    private void ApplyGridMaterials()
    {
        if (gridManager == null) return;
        
        // Apply materials to grid tiles
        for (int x = 0; x < gridManager.GridWidth; x++)
        {
            for (int z = 0; z < gridManager.GridHeight; z++)
            {
                GridTile tile = gridManager.GetTile(x, z);
                if (tile != null)
                {
                    ApplyTileMaterial(tile, GridTileState.Normal);
                }
            }
        }
    }
    
    /// <summary>
    /// Applies materials to obstacle system
    /// </summary>
    private void ApplyObstacleMaterials()
    {
        if (obstacleManager == null) return;
        
        // Apply materials to all obstacles
        foreach (ObstacleType obstacleType in System.Enum.GetValues(typeof(ObstacleType)))
        {
            if (obstacleMaterials.TryGetValue(obstacleType, out Material material))
            {
                ApplyObstacleTypeMaterial(obstacleType, material);
            }
        }
    }
    
    /// <summary>
    /// Applies materials to camera system
    /// </summary>
    private void ApplyCameraMaterials()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = backgroundColor;
        }
    }
    
    /// <summary>
    /// Gets a material from the cache
    /// </summary>
    public Material GetMaterial(string materialName)
    {
        if (materialCache.TryGetValue(materialName, out Material material))
        {
            return material;
        }
        
        Debug.LogWarning($"MaterialManager: Material '{materialName}' not found in cache");
        return null;
    }
    
    /// <summary>
    /// Gets a material for the specified tile state
    /// </summary>
    public Material GetTileMaterial(GridTileState state)
    {
        if (tileMaterials.TryGetValue(state, out Material material))
        {
            return material;
        }
        
        return tileMaterials[GridTileState.Normal];
    }
    
    /// <summary>
    /// Gets a material for the specified obstacle type
    /// </summary>
    public Material GetObstacleMaterial(ObstacleType type)
    {
        if (obstacleMaterials.TryGetValue(type, out Material material))
        {
            return material;
        }
        
        Debug.LogWarning($"MaterialManager: No material found for obstacle type {type}");
        return GetMaterial("Obstacle_LowCover");
    }
    
    /// <summary>
    /// Applies a material to a specific tile
    /// </summary>
    public void ApplyTileMaterial(GridTile tile, GridTileState state)
    {
        if (tile == null) return;
        
        Material material = GetTileMaterial(state);
        if (material != null)
        {
            Renderer tileRenderer = tile.GetComponent<Renderer>();
            if (tileRenderer != null)
            {
                tileRenderer.material = material;
            }
        }
    }
    
    /// <summary>
    /// Applies a material to all obstacles of a specific type
    /// </summary>
    public void ApplyObstacleTypeMaterial(ObstacleType type, Material material)
    {
        if (obstacleManager == null || material == null) return;
        
        // Find all obstacles of this type and apply material
        Obstacle[] allObstacles = FindObjectsByType<Obstacle>(FindObjectsSortMode.None);
        foreach (Obstacle obstacle in allObstacles)
        {
            if (obstacle.ObstacleType == type)
            {
                Renderer obstacleRenderer = obstacle.GetComponentInChildren<Renderer>();
                if (obstacleRenderer != null)
                {
                    obstacleRenderer.material = material;
                }
            }
        }
    }
    
    /// <summary>
    /// Updates material colors dynamically
    /// </summary>
    public void UpdateMaterialColor(string materialName, Color newColor)
    {
        Material material = GetMaterial(materialName);
        if (material != null)
        {
            material.color = newColor;
            OnMaterialColorChanged?.Invoke(material, newColor);
        }
    }
    
    /// <summary>
    /// Updates tile state colors
    /// </summary>
    public void UpdateTileStateColor(GridTileState state, Color newColor)
    {
        switch (state)
        {
            case GridTileState.Normal:
                gridTileDefaultColor = newColor;
                break;
            case GridTileState.Hovered:
                gridTileHoverColor = newColor;
                break;
            case GridTileState.Selected:
                gridTileSelectedColor = newColor;
                break;
            case GridTileState.Blocked:
                gridTileBlockedColor = newColor;
                break;
        }
        
        Material material = GetTileMaterial(state);
        if (material != null)
        {
            material.color = newColor;
            OnMaterialColorChanged?.Invoke(material, newColor);
        }
    }
    
    /// <summary>
    /// Updates dynamic materials during runtime
    /// </summary>
    private void UpdateDynamicMaterials()
    {
        if (!enableMaterialUpdates) return;
        
        // Update gradient effects if enabled
        if (enableGradientEffects)
        {
            UpdateGradientEffects();
        }
        
        // Sync with visual feedback manager if available
        if (visualFeedbackManager != null)
        {
            SyncWithVisualFeedback();
        }
    }
    
    /// <summary>
    /// Updates gradient visual effects
    /// </summary>
    private void UpdateGradientEffects()
    {
        float time = Time.time;
        float intensity = Mathf.Sin(time * 2f) * 0.1f + 0.9f;
        
        // Apply intensity variation to hover materials
        Material hoverMaterial = GetTileMaterial(GridTileState.Hovered);
        if (hoverMaterial != null)
        {
            Color baseColor = gridTileHoverColor;
            hoverMaterial.color = baseColor * intensity;
        }
    }
    
    /// <summary>
    /// Synchronizes materials with visual feedback system
    /// </summary>
    private void SyncWithVisualFeedback()
    {
        // Future: Sync material updates with visual feedback events
    }
    
    /// <summary>
    /// Resets all materials to default configuration
    /// </summary>
    public void ResetMaterials()
    {
        // Clear cache and recreate
        materialCache.Clear();
        obstacleMaterials.Clear();
        tileMaterials.Clear();
        
        CreateMaterialCache();
        ApplyEnvironmentMaterials();
        
        Debug.Log("MaterialManager: Materials reset to defaults");
    }
    
    /// <summary>
    /// Validates material system integrity
    /// </summary>
    public bool ValidateMaterials()
    {
        bool isValid = true;
        
        // Check essential materials exist
        string[] essentialMaterials = { "GridLine", "GridTile_Default", "Obstacle_LowCover" };
        foreach (string matName in essentialMaterials)
        {
            if (!materialCache.ContainsKey(matName))
            {
                Debug.LogError($"MaterialManager: Essential material '{matName}' missing");
                isValid = false;
            }
        }
        
        // Check shaders are valid
        if (standardShader == null || unlitShader == null)
        {
            Debug.LogError("MaterialManager: Essential shaders not found");
            isValid = false;
        }
        
        // Check manager references
        if (gridManager == null)
        {
            Debug.LogWarning("MaterialManager: GridManager reference missing");
        }
        
        return isValid;
    }
}

/// <summary>
/// Enumeration for different grid tile visual states
/// </summary>
public enum GridTileState
{
    Normal,
    Hovered,
    Selected,
    Blocked,
    Highlighted
}