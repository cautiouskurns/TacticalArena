using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Central management system for all obstacles in the tactical arena.
/// Handles obstacle registration, spatial queries, and line-of-sight calculations.
/// </summary>
public class ObstacleManager : MonoBehaviour
{
    [Header("Obstacle Configuration")]
    [SerializeField] private ObstacleConfiguration configuration;
    [SerializeField] private bool enableLineOfSightBlocking = true;
    [SerializeField] private bool enablePartialCover = true;
    [SerializeField] private bool generateObstacleShadows = true;
    
    [Header("Performance Settings")]
    [SerializeField] private bool enableSpatialOptimization = true;
    [SerializeField] private int maxLineOfSightChecks = 100;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugVisualization = false;
    [SerializeField] private bool logLineOfSightChecks = false;
    
    // Obstacle registry and spatial lookup
    private Dictionary<GridCoordinate, Obstacle> obstacleRegistry;
    private List<Obstacle> allObstacles;
    private GridManager gridManager;
    
    // Line-of-sight cache for performance
    private Dictionary<(GridCoordinate, GridCoordinate), bool> lineOfSightCache;
    private int framesSinceLastCacheClear = 0;
    private const int CACHE_CLEAR_INTERVAL = 300; // Clear cache every 5 seconds at 60fps
    
    // Events for obstacle state changes
    public System.Action<Obstacle> OnObstacleAdded;
    public System.Action<Obstacle> OnObstacleRemoved;
    public System.Action<GridCoordinate, GridCoordinate, bool> OnLineOfSightChecked;
    
    // Public properties
    public int ObstacleCount => allObstacles?.Count ?? 0;
    public bool LineOfSightEnabled => enableLineOfSightBlocking;
    public bool PartialCoverEnabled => enablePartialCover;
    public ObstacleConfiguration Configuration => configuration;
    
    void Awake()
    {
        InitializeObstacleSystem();
    }
    
    void Start()
    {
        FindAndRegisterExistingObstacles();
        ValidateObstacleSetup();
    }
    
    void Update()
    {
        // Periodic cache cleanup for performance
        framesSinceLastCacheClear++;
        if (framesSinceLastCacheClear >= CACHE_CLEAR_INTERVAL)
        {
            ClearLineOfSightCache();
            framesSinceLastCacheClear = 0;
        }
    }
    
    /// <summary>
    /// Initializes the obstacle management system
    /// </summary>
    private void InitializeObstacleSystem()
    {
        obstacleRegistry = new Dictionary<GridCoordinate, Obstacle>();
        allObstacles = new List<Obstacle>();
        lineOfSightCache = new Dictionary<(GridCoordinate, GridCoordinate), bool>();
        
        // Find grid manager reference
        GameObject gridSystem = GameObject.Find("Grid System");
        if (gridSystem != null)
        {
            gridManager = gridSystem.GetComponent<GridManager>();
        }
        
        if (gridManager == null)
        {
            Debug.LogError("ObstacleManager: GridManager not found. Obstacle system may not function correctly.");
        }
        
        Debug.Log($"ObstacleManager initialized with line-of-sight: {enableLineOfSightBlocking}");
    }
    
    /// <summary>
    /// Finds and registers all existing obstacles in the scene
    /// </summary>
    private void FindAndRegisterExistingObstacles()
    {
        GameObject obstaclesGroup = transform.Find("Obstacles")?.gameObject;
        if (obstaclesGroup == null)
        {
            Debug.LogWarning("ObstacleManager: Obstacles group not found");
            return;
        }
        
        int obstaclesFound = 0;
        
        for (int i = 0; i < obstaclesGroup.transform.childCount; i++)
        {
            Transform obstacleTransform = obstaclesGroup.transform.GetChild(i);
            Obstacle obstacle = obstacleTransform.GetComponent<Obstacle>();
            
            if (obstacle != null)
            {
                RegisterObstacle(obstacle);
                obstaclesFound++;
            }
        }
        
        Debug.Log($"ObstacleManager: Registered {obstaclesFound} obstacles");
    }
    
    /// <summary>
    /// Registers an obstacle with the management system
    /// </summary>
    public void RegisterObstacle(Obstacle obstacle)
    {
        if (obstacle == null) return;
        
        GridCoordinate coord = obstacle.GridCoordinate;
        
        // Check for existing obstacle at this position
        if (obstacleRegistry.ContainsKey(coord))
        {
            Debug.LogWarning($"ObstacleManager: Obstacle already exists at {coord}. Replacing.");
            UnregisterObstacle(obstacleRegistry[coord]);
        }
        
        // Register the obstacle
        obstacleRegistry[coord] = obstacle;
        allObstacles.Add(obstacle);
        
        // Set up obstacle event listeners
        obstacle.OnObstacleDestroyed += HandleObstacleDestroyed;
        obstacle.OnObstaclePositionChanged += HandleObstaclePositionChanged;
        
        // Clear line-of-sight cache since obstacle layout changed
        ClearLineOfSightCache();
        
        OnObstacleAdded?.Invoke(obstacle);
        
        if (logLineOfSightChecks)
        {
            Debug.Log($"ObstacleManager: Registered obstacle at {coord}");
        }
    }
    
    /// <summary>
    /// Unregisters an obstacle from the management system
    /// </summary>
    public void UnregisterObstacle(Obstacle obstacle)
    {
        if (obstacle == null) return;
        
        GridCoordinate coord = obstacle.GridCoordinate;
        
        if (obstacleRegistry.ContainsKey(coord) && obstacleRegistry[coord] == obstacle)
        {
            obstacleRegistry.Remove(coord);
        }
        
        allObstacles.Remove(obstacle);
        
        // Remove event listeners
        obstacle.OnObstacleDestroyed -= HandleObstacleDestroyed;
        obstacle.OnObstaclePositionChanged -= HandleObstaclePositionChanged;
        
        // Clear line-of-sight cache since obstacle layout changed
        ClearLineOfSightCache();
        
        OnObstacleRemoved?.Invoke(obstacle);
        
        if (logLineOfSightChecks)
        {
            Debug.Log($"ObstacleManager: Unregistered obstacle at {coord}");
        }
    }
    
    /// <summary>
    /// Gets the obstacle at the specified grid coordinate
    /// </summary>
    public Obstacle GetObstacle(GridCoordinate coordinate)
    {
        obstacleRegistry.TryGetValue(coordinate, out Obstacle obstacle);
        return obstacle;
    }
    
    /// <summary>
    /// Gets the obstacle at the specified grid position
    /// </summary>
    public Obstacle GetObstacle(int x, int z)
    {
        return GetObstacle(new GridCoordinate(x, z));
    }
    
    /// <summary>
    /// Checks if there's an obstacle at the specified coordinate
    /// </summary>
    public bool HasObstacle(GridCoordinate coordinate)
    {
        return obstacleRegistry.ContainsKey(coordinate);
    }
    
    /// <summary>
    /// Gets all obstacles within a specified range of a coordinate
    /// </summary>
    public List<Obstacle> GetObstaclesInRange(GridCoordinate center, int range)
    {
        List<Obstacle> obstaclesInRange = new List<Obstacle>();
        
        for (int x = center.x - range; x <= center.x + range; x++)
        {
            for (int z = center.z - range; z <= center.z + range; z++)
            {
                GridCoordinate coord = new GridCoordinate(x, z);
                if (gridManager != null && gridManager.IsValidCoordinate(coord))
                {
                    Obstacle obstacle = GetObstacle(coord);
                    if (obstacle != null)
                    {
                        obstaclesInRange.Add(obstacle);
                    }
                }
            }
        }
        
        return obstaclesInRange;
    }
    
    /// <summary>
    /// Checks if there's a clear line of sight between two coordinates
    /// </summary>
    public bool HasLineOfSight(GridCoordinate from, GridCoordinate to)
    {
        if (!enableLineOfSightBlocking) return true;
        if (from == to) return true;
        if (gridManager == null) return true;
        
        // Check cache first
        var cacheKey = (from, to);
        if (lineOfSightCache.TryGetValue(cacheKey, out bool cachedResult))
        {
            return cachedResult;
        }
        
        // Calculate line of sight
        bool hasLineOfSight = CalculateLineOfSight(from, to);
        
        // Cache the result (and the reverse for efficiency)
        lineOfSightCache[cacheKey] = hasLineOfSight;
        lineOfSightCache[(to, from)] = hasLineOfSight;
        
        OnLineOfSightChecked?.Invoke(from, to, hasLineOfSight);
        
        if (logLineOfSightChecks)
        {
            Debug.Log($"Line of sight {from} -> {to}: {hasLineOfSight}");
        }
        
        return hasLineOfSight;
    }
    
    /// <summary>
    /// Calculates line of sight between two coordinates using raycasting
    /// </summary>
    private bool CalculateLineOfSight(GridCoordinate from, GridCoordinate to)
    {
        if (gridManager == null) return true;
        
        Vector3 fromWorld = gridManager.GridToWorld(from) + Vector3.up * 0.5f;
        Vector3 toWorld = gridManager.GridToWorld(to) + Vector3.up * 0.5f;
        
        Vector3 direction = (toWorld - fromWorld).normalized;
        float distance = Vector3.Distance(fromWorld, toWorld);
        
        // Use Unity's physics system for line-of-sight checking
        RaycastHit hit;
        if (Physics.Raycast(fromWorld, direction, out hit, distance))
        {
            // Check if we hit an obstacle
            Obstacle hitObstacle = hit.collider.GetComponent<Obstacle>();
            if (hitObstacle != null)
            {
                // Check if this obstacle blocks line of sight at the hit height
                ObstacleData obstacleData = GetObstacleData(hitObstacle.ObstacleType);
                return !obstacleData.BlocksLineOfSightAtHeight(hit.point.y);
            }
        }
        
        return true; // No blocking obstacles found
    }
    
    /// <summary>
    /// Gets the cover value between two coordinates
    /// </summary>
    public float GetCoverValue(GridCoordinate from, GridCoordinate to)
    {
        if (!enablePartialCover) return 0f;
        
        // Simple implementation: check for obstacles that provide cover
        List<Obstacle> obstaclesBetween = GetObstaclesBetween(from, to);
        
        float maxCover = 0f;
        foreach (var obstacle in obstaclesBetween)
        {
            ObstacleData data = GetObstacleData(obstacle.ObstacleType);
            maxCover = Mathf.Max(maxCover, data.coverValue);
        }
        
        return maxCover;
    }
    
    /// <summary>
    /// Gets obstacles that are between two coordinates
    /// </summary>
    private List<Obstacle> GetObstaclesBetween(GridCoordinate from, GridCoordinate to)
    {
        List<Obstacle> obstaclesBetween = new List<Obstacle>();
        
        // Simple line traversal algorithm
        int dx = Mathf.Abs(to.x - from.x);
        int dz = Mathf.Abs(to.z - from.z);
        int x = from.x;
        int z = from.z;
        int n = 1 + dx + dz;
        int x_inc = (to.x > from.x) ? 1 : -1;
        int z_inc = (to.z > from.z) ? 1 : -1;
        int error = dx - dz;
        
        dx *= 2;
        dz *= 2;
        
        for (; n > 0; --n)
        {
            GridCoordinate current = new GridCoordinate(x, z);
            if (current != from && current != to)
            {
                Obstacle obstacle = GetObstacle(current);
                if (obstacle != null)
                {
                    obstaclesBetween.Add(obstacle);
                }
            }
            
            if (error > 0)
            {
                x += x_inc;
                error -= dz;
            }
            else
            {
                z += z_inc;
                error += dx;
            }
        }
        
        return obstaclesBetween;
    }
    
    /// <summary>
    /// Gets all coordinates that are blocked by obstacles
    /// </summary>
    public List<GridCoordinate> GetBlockedCoordinates()
    {
        return obstacleRegistry.Keys.ToList();
    }
    
    /// <summary>
    /// Gets all coordinates that are free for movement
    /// </summary>
    public List<GridCoordinate> GetFreeCoordinates()
    {
        List<GridCoordinate> freeCoordinates = new List<GridCoordinate>();
        
        if (gridManager == null) return freeCoordinates;
        
        for (int x = 0; x < gridManager.GridWidth; x++)
        {
            for (int z = 0; z < gridManager.GridHeight; z++)
            {
                GridCoordinate coord = new GridCoordinate(x, z);
                if (!HasObstacle(coord))
                {
                    freeCoordinates.Add(coord);
                }
            }
        }
        
        return freeCoordinates;
    }
    
    /// <summary>
    /// Gets obstacle data for the specified obstacle type
    /// </summary>
    private ObstacleData GetObstacleData(ObstacleType type)
    {
        if (configuration != null)
        {
            return configuration.GetObstacleData(type);
        }
        
        return ObstacleData.GetDefault(type);
    }
    
    /// <summary>
    /// Clears the line-of-sight cache
    /// </summary>
    private void ClearLineOfSightCache()
    {
        lineOfSightCache.Clear();
        
        if (logLineOfSightChecks)
        {
            Debug.Log("ObstacleManager: Line-of-sight cache cleared");
        }
    }
    
    /// <summary>
    /// Handles when an obstacle is destroyed
    /// </summary>
    private void HandleObstacleDestroyed(Obstacle obstacle)
    {
        UnregisterObstacle(obstacle);
    }
    
    /// <summary>
    /// Handles when an obstacle changes position
    /// </summary>
    private void HandleObstaclePositionChanged(Obstacle obstacle, GridCoordinate oldPosition, GridCoordinate newPosition)
    {
        // Update registry
        if (obstacleRegistry.ContainsKey(oldPosition) && obstacleRegistry[oldPosition] == obstacle)
        {
            obstacleRegistry.Remove(oldPosition);
        }
        
        obstacleRegistry[newPosition] = obstacle;
        
        // Clear cache since positions changed
        ClearLineOfSightCache();
        
        if (logLineOfSightChecks)
        {
            Debug.Log($"ObstacleManager: Obstacle moved from {oldPosition} to {newPosition}");
        }
    }
    
    /// <summary>
    /// Validates the obstacle setup
    /// </summary>
    private void ValidateObstacleSetup()
    {
        bool validationPassed = true;
        
        if (configuration != null)
        {
            if (!configuration.ValidateConfiguration())
            {
                Debug.LogError("ObstacleManager: Invalid obstacle configuration");
                validationPassed = false;
            }
        }
        else
        {
            Debug.LogWarning("ObstacleManager: No obstacle configuration assigned, using defaults");
        }
        
        if (gridManager == null)
        {
            Debug.LogError("ObstacleManager: GridManager reference missing");
            validationPassed = false;
        }
        
        if (validationPassed)
        {
            Debug.Log($"ObstacleManager: Validation passed. Managing {ObstacleCount} obstacles.");
        }
    }
    
    /// <summary>
    /// Gets debug information about the obstacle system
    /// </summary>
    public ObstacleSystemInfo GetSystemInfo()
    {
        return new ObstacleSystemInfo
        {
            obstacleCount = ObstacleCount,
            lineOfSightEnabled = enableLineOfSightBlocking,
            partialCoverEnabled = enablePartialCover,
            cacheSize = lineOfSightCache.Count,
            blockedCoordinates = GetBlockedCoordinates().ToArray(),
            freeCoordinates = GetFreeCoordinates().ToArray()
        };
    }
    
    void OnDrawGizmos()
    {
        if (!enableDebugVisualization) return;
        
        // Draw obstacle positions
        Gizmos.color = Color.red;
        foreach (var obstacle in allObstacles)
        {
            if (obstacle != null && gridManager != null)
            {
                Vector3 worldPos = gridManager.GridToWorld(obstacle.GridCoordinate);
                Gizmos.DrawWireCube(worldPos + Vector3.up * 0.5f, Vector3.one * 0.8f);
            }
        }
        
        // Draw line-of-sight checks if enabled
        if (logLineOfSightChecks && gridManager != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var kvp in lineOfSightCache)
            {
                Vector3 from = gridManager.GridToWorld(kvp.Key.Item1) + Vector3.up * 0.5f;
                Vector3 to = gridManager.GridToWorld(kvp.Key.Item2) + Vector3.up * 0.5f;
                
                Gizmos.color = kvp.Value ? Color.green : Color.red;
                Gizmos.DrawLine(from, to);
            }
        }
    }
}

/// <summary>
/// Information structure for obstacle system state
/// </summary>
[System.Serializable]
public struct ObstacleSystemInfo
{
    public int obstacleCount;
    public bool lineOfSightEnabled;
    public bool partialCoverEnabled;
    public int cacheSize;
    public GridCoordinate[] blockedCoordinates;
    public GridCoordinate[] freeCoordinates;
    
    public override string ToString()
    {
        return $"Obstacles: {obstacleCount}, LOS: {lineOfSightEnabled}, Cache: {cacheSize}, Blocked: {blockedCoordinates.Length}";
    }
}