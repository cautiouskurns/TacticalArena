using UnityEngine;

/// <summary>
/// Individual obstacle component that handles behavior, visual presentation,
/// and integration with the grid system for tactical gameplay.
/// </summary>
public class Obstacle : MonoBehaviour
{
    [Header("Obstacle Configuration")]
    [SerializeField] private GridCoordinate gridCoordinate;
    [SerializeField] private ObstacleType obstacleType = ObstacleType.LowCover;
    [SerializeField] private Vector3 worldPosition;
    
    [Header("Visual Settings")]
    [SerializeField] private GameObject visualObject;
    [SerializeField] private Renderer obstacleRenderer;
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material highlightMaterial;
    
    [Header("Gameplay Properties")]
    [SerializeField] private bool isDestructible = false;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float maxHealth = 100f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugVisualization = false;
    [SerializeField] private bool logStateChanges = false;
    
    // Component references
    private ObstacleManager obstacleManager;
    private GridManager gridManager;
    private Collider obstacleCollider;
    
    // Cached obstacle data
    private ObstacleData cachedObstacleData;
    private bool dataInitialized = false;
    
    // State tracking
    private bool isHighlighted = false;
    private bool isDestroyed = false;
    
    // Events for obstacle state changes
    public System.Action<Obstacle> OnObstacleDestroyed;
    public System.Action<Obstacle, GridCoordinate, GridCoordinate> OnObstaclePositionChanged;
    public System.Action<Obstacle, float> OnObstacleHealthChanged;
    public System.Action<Obstacle, bool> OnObstacleHighlightChanged;
    
    // Public properties
    public GridCoordinate GridCoordinate => gridCoordinate;
    public ObstacleType ObstacleType => obstacleType;
    public Vector3 WorldPosition => worldPosition;
    public ObstacleData ObstacleData => GetObstacleData();
    public bool IsDestructible => isDestructible;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsDestroyed => isDestroyed;
    public bool IsHighlighted => isHighlighted;
    
    void Awake()
    {
        InitializeObstacle();
    }
    
    void Start()
    {
        RegisterWithManagers();
        ValidateObstacleSetup();
        InitializeVisualState();
    }
    
    void OnDestroy()
    {
        if (!isDestroyed)
        {
            HandleDestruction();
        }
    }
    
    /// <summary>
    /// Initializes the obstacle component and caches references
    /// </summary>
    private void InitializeObstacle()
    {
        // Cache component references
        obstacleRenderer = GetComponentInChildren<Renderer>();
        obstacleCollider = GetComponent<Collider>();
        visualObject = transform.Find("Visual")?.gameObject;
        
        // Set initial position if not set
        if (worldPosition == Vector3.zero)
        {
            worldPosition = transform.position;
        }
        
        // Initialize health
        if (maxHealth <= 0)
        {
            maxHealth = 100f;
        }
        currentHealth = maxHealth;
        
        if (logStateChanges)
        {
            Debug.Log($"Obstacle initialized at {gridCoordinate} with type {obstacleType}");
        }
    }
    
    /// <summary>
    /// Registers this obstacle with the relevant manager systems
    /// </summary>
    private void RegisterWithManagers()
    {
        // Find and register with obstacle manager
        GameObject obstacleSystem = GameObject.Find("Obstacle System");
        if (obstacleSystem != null)
        {
            obstacleManager = obstacleSystem.GetComponent<ObstacleManager>();
            if (obstacleManager != null)
            {
                obstacleManager.RegisterObstacle(this);
            }
        }
        
        // Find grid manager reference
        GameObject gridSystem = GameObject.Find("Grid System");
        if (gridSystem != null)
        {
            gridManager = gridSystem.GetComponent<GridManager>();
        }
        
        if (obstacleManager == null)
        {
            Debug.LogWarning($"Obstacle at {gridCoordinate}: ObstacleManager not found");
        }
        
        if (gridManager == null)
        {
            Debug.LogWarning($"Obstacle at {gridCoordinate}: GridManager not found");
        }
    }
    
    /// <summary>
    /// Gets the obstacle data configuration for this obstacle type
    /// </summary>
    public ObstacleData GetObstacleData()
    {
        if (!dataInitialized)
        {
            if (obstacleManager != null && obstacleManager.Configuration != null)
            {
                cachedObstacleData = obstacleManager.Configuration.GetObstacleData(obstacleType);
            }
            else
            {
                cachedObstacleData = ObstacleData.GetDefault(obstacleType);
            }
            dataInitialized = true;
        }
        
        return cachedObstacleData;
    }
    
    /// <summary>
    /// Updates the obstacle's grid coordinate and world position
    /// </summary>
    public void SetPosition(GridCoordinate newCoordinate)
    {
        if (newCoordinate == gridCoordinate) return;
        
        GridCoordinate oldCoordinate = gridCoordinate;
        gridCoordinate = newCoordinate;
        
        // Update world position through grid manager
        if (gridManager != null)
        {
            worldPosition = gridManager.GridToWorld(newCoordinate);
            transform.position = worldPosition;
        }
        
        OnObstaclePositionChanged?.Invoke(this, oldCoordinate, newCoordinate);
        
        if (logStateChanges)
        {
            Debug.Log($"Obstacle moved from {oldCoordinate} to {newCoordinate}");
        }
    }
    
    /// <summary>
    /// Updates the obstacle's world position and calculates grid coordinate
    /// </summary>
    public void SetWorldPosition(Vector3 newWorldPosition)
    {
        worldPosition = newWorldPosition;
        transform.position = worldPosition;
        
        // Update grid coordinate through grid manager
        if (gridManager != null)
        {
            GridCoordinate newCoordinate = gridManager.WorldToGrid(worldPosition);
            if (newCoordinate != gridCoordinate && newCoordinate.IsValid)
            {
                SetPosition(newCoordinate);
            }
        }
    }
    
    /// <summary>
    /// Checks if this obstacle blocks line of sight at the specified height
    /// </summary>
    public bool BlocksLineOfSightAtHeight(float height)
    {
        ObstacleData data = GetObstacleData();
        return data.BlocksLineOfSightAtHeight(height);
    }
    
    /// <summary>
    /// Gets the cover value provided by this obstacle
    /// </summary>
    public float GetCoverValue(Vector3 attackDirection = default)
    {
        ObstacleData data = GetObstacleData();
        return data.GetCoverValue(attackDirection);
    }
    
    /// <summary>
    /// Gets the movement cost for traversing this obstacle
    /// </summary>
    public float GetMovementCost()
    {
        ObstacleData data = GetObstacleData();
        return data.GetMovementCost();
    }
    
    /// <summary>
    /// Checks if movement through this obstacle is blocked
    /// </summary>
    public bool BlocksMovement()
    {
        ObstacleData data = GetObstacleData();
        return data.blocksMovement;
    }
    
    /// <summary>
    /// Applies damage to the obstacle if it's destructible
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (!isDestructible || isDestroyed) return;
        
        float previousHealth = currentHealth;
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        
        OnObstacleHealthChanged?.Invoke(this, currentHealth);
        
        if (logStateChanges)
        {
            Debug.Log($"Obstacle at {gridCoordinate} took {damage} damage. Health: {currentHealth}/{maxHealth}");
        }
        
        // Check for destruction
        if (currentHealth <= 0f && !isDestroyed)
        {
            DestroyObstacle();
        }
        else
        {
            UpdateVisualDamageState();
        }
    }
    
    /// <summary>
    /// Heals the obstacle if it's destructible
    /// </summary>
    public void Heal(float healAmount)
    {
        if (!isDestructible || isDestroyed) return;
        
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        OnObstacleHealthChanged?.Invoke(this, currentHealth);
        
        UpdateVisualDamageState();
        
        if (logStateChanges)
        {
            Debug.Log($"Obstacle at {gridCoordinate} healed for {healAmount}. Health: {currentHealth}/{maxHealth}");
        }
    }
    
    /// <summary>
    /// Sets the highlight state of the obstacle
    /// </summary>
    public void SetHighlight(bool highlighted)
    {
        if (isHighlighted == highlighted) return;
        
        isHighlighted = highlighted;
        UpdateVisualHighlightState();
        
        OnObstacleHighlightChanged?.Invoke(this, highlighted);
        
        if (logStateChanges)
        {
            Debug.Log($"Obstacle at {gridCoordinate} highlight: {highlighted}");
        }
    }
    
    /// <summary>
    /// Destroys the obstacle and handles cleanup
    /// </summary>
    public void DestroyObstacle()
    {
        if (isDestroyed) return;
        
        isDestroyed = true;
        
        // Clear tile occupation if grid manager is available
        if (gridManager != null)
        {
            GridTile tile = gridManager.GetTile(gridCoordinate);
            if (tile != null)
            {
                tile.ClearOccupation();
            }
        }
        
        HandleDestruction();
        
        if (logStateChanges)
        {
            Debug.Log($"Obstacle at {gridCoordinate} destroyed");
        }
        
        // Destroy the GameObject
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Handles the destruction process and cleanup
    /// </summary>
    private void HandleDestruction()
    {
        OnObstacleDestroyed?.Invoke(this);
        
        // Unregister from obstacle manager
        if (obstacleManager != null)
        {
            obstacleManager.UnregisterObstacle(this);
        }
    }
    
    /// <summary>
    /// Initializes the visual state of the obstacle
    /// </summary>
    private void InitializeVisualState()
    {
        if (obstacleRenderer != null && defaultMaterial == null)
        {
            defaultMaterial = obstacleRenderer.material;
        }
        
        UpdateVisualDamageState();
        UpdateVisualHighlightState();
    }
    
    /// <summary>
    /// Updates the visual state based on damage level
    /// </summary>
    private void UpdateVisualDamageState()
    {
        if (obstacleRenderer == null || !isDestructible) return;
        
        // Adjust material color based on health percentage
        Color baseColor = GetObstacleData().defaultColor;
        float healthPercentage = HealthPercentage;
        
        if (healthPercentage < 0.3f)
        {
            // Heavily damaged - darker and more red
            baseColor = Color.Lerp(Color.red, baseColor, healthPercentage / 0.3f);
        }
        else if (healthPercentage < 0.7f)
        {
            // Moderately damaged - slightly darker
            baseColor = Color.Lerp(baseColor * 0.7f, baseColor, (healthPercentage - 0.3f) / 0.4f);
        }
        
        if (obstacleRenderer.material != null)
        {
            obstacleRenderer.material.color = baseColor;
        }
    }
    
    /// <summary>
    /// Updates the visual state based on highlight status
    /// </summary>
    private void UpdateVisualHighlightState()
    {
        if (obstacleRenderer == null) return;
        
        if (isHighlighted && highlightMaterial != null)
        {
            obstacleRenderer.material = highlightMaterial;
        }
        else if (defaultMaterial != null)
        {
            obstacleRenderer.material = defaultMaterial;
            UpdateVisualDamageState(); // Reapply damage coloring
        }
    }
    
    /// <summary>
    /// Validates the obstacle setup and configuration
    /// </summary>
    private void ValidateObstacleSetup()
    {
        bool validationPassed = true;
        
        if (obstacleCollider == null)
        {
            Debug.LogWarning($"Obstacle at {gridCoordinate}: No collider found");
            validationPassed = false;
        }
        
        if (obstacleRenderer == null)
        {
            Debug.LogWarning($"Obstacle at {gridCoordinate}: No renderer found");
            validationPassed = false;
        }
        
        if (gridManager != null && !gridManager.IsValidCoordinate(gridCoordinate))
        {
            Debug.LogError($"Obstacle at invalid coordinate: {gridCoordinate}");
            validationPassed = false;
        }
        
        if (validationPassed && logStateChanges)
        {
            Debug.Log($"Obstacle at {gridCoordinate}: Validation passed");
        }
    }
    
    /// <summary>
    /// Gets debug information about this obstacle
    /// </summary>
    public ObstacleInfo GetObstacleInfo()
    {
        return new ObstacleInfo
        {
            gridCoordinate = gridCoordinate,
            worldPosition = worldPosition,
            obstacleType = obstacleType,
            isDestructible = isDestructible,
            currentHealth = currentHealth,
            maxHealth = maxHealth,
            isHighlighted = isHighlighted,
            isDestroyed = isDestroyed,
            obstacleData = GetObstacleData()
        };
    }
    
    void OnDrawGizmos()
    {
        if (!enableDebugVisualization) return;
        
        // Draw grid coordinate
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);
        
        // Draw health indicator if destructible
        if (isDestructible)
        {
            float healthPercentage = HealthPercentage;
            Gizmos.color = Color.Lerp(Color.red, Color.green, healthPercentage);
            
            Vector3 healthBarPos = transform.position + Vector3.up * (GetObstacleData().height + 0.2f);
            Gizmos.DrawCube(healthBarPos, new Vector3(healthPercentage, 0.1f, 0.1f));
        }
        
        // Draw line-of-sight blocking indicator
        if (GetObstacleData().blocksLineOfSight)
        {
            Gizmos.color = Color.red;
            Vector3 center = transform.position + Vector3.up * (GetObstacleData().height / 2f);
            Gizmos.DrawWireSphere(center, 0.1f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw detailed information when selected
        ObstacleData data = GetObstacleData();
        
        // Draw height indicator
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * data.height);
        
        // Draw cover range
        if (data.coverValue > 0f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * (data.height / 2f), data.coverValue);
        }
    }
}

/// <summary>
/// Information structure for obstacle state
/// </summary>
[System.Serializable]
public struct ObstacleInfo
{
    public GridCoordinate gridCoordinate;
    public Vector3 worldPosition;
    public ObstacleType obstacleType;
    public bool isDestructible;
    public float currentHealth;
    public float maxHealth;
    public bool isHighlighted;
    public bool isDestroyed;
    public ObstacleData obstacleData;
    
    public override string ToString()
    {
        return $"Obstacle {obstacleType} at {gridCoordinate}, Health: {currentHealth}/{maxHealth}";
    }
}