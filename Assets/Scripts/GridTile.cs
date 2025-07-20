using UnityEngine;

/// <summary>
/// Individual tile behavior for the tactical grid system.
/// Handles tile selection states, visual feedback, and mouse interaction.
/// </summary>
public class GridTile : MonoBehaviour
{
    [Header("Tile Configuration")]
    [SerializeField] private GridCoordinate coordinate = GridCoordinate.Zero;
    [SerializeField] private Vector3 worldPosition = Vector3.zero;
    [SerializeField] private float tileSize = 1f;
    
    [Header("State Management")]
    [SerializeField] private bool isOccupied = false;
    [SerializeField] private bool isSelected = false;
    [SerializeField] private bool isHovered = false;
    [SerializeField] private bool isBlocked = false;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool enableVisualFeedback = true;
    [SerializeField] private bool enableHoverEffect = true;
    [SerializeField] private bool enableClickEffect = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Material references for different states
    private Material defaultMaterial;
    private Material hoverMaterial;
    private Material selectedMaterial;
    private Material blockedMaterial;
    
    // Components
    private Renderer tileRenderer;
    private Collider tileCollider;
    
    // Events for grid system communication
    public System.Action<GridCoordinate> OnTileHovered;
    public System.Action<GridCoordinate> OnTileClicked;
    public System.Action<GridCoordinate> OnTileOccupiedChanged;
    
    // Public properties
    public GridCoordinate Coordinate => coordinate;
    public Vector3 WorldPosition => worldPosition;
    public bool IsOccupied => isOccupied;
    public bool IsSelected => isSelected;
    public bool IsHovered => isHovered;
    public bool IsBlocked => isBlocked;
    public float TileSize => tileSize;
    
    void Awake()
    {
        InitializeComponents();
        LoadMaterials();
    }
    
    void Start()
    {
        InitializeTile();
        UpdateVisualState();
    }
    
    /// <summary>
    /// Initializes tile components and references
    /// </summary>
    private void InitializeComponents()
    {
        // Get or add required components
        tileCollider = GetComponent<Collider>();
        if (tileCollider == null)
        {
            Debug.LogWarning($"GridTile {name}: No collider found. Tile interaction may not work.");
        }
        
        // Look for renderer in child objects (visual representation)
        tileRenderer = GetComponentInChildren<Renderer>();
        if (tileRenderer == null && enableVisualFeedback)
        {
            Debug.LogWarning($"GridTile {name}: No renderer found. Visual feedback disabled.");
            enableVisualFeedback = false;
        }
    }
    
    /// <summary>
    /// Loads materials for different tile states
    /// </summary>
    private void LoadMaterials()
    {
        if (!enableVisualFeedback) return;
        
        // Load materials from Resources or create defaults
        defaultMaterial = LoadMaterial("GridTile_Default");
        hoverMaterial = LoadMaterial("GridTile_Hover");
        selectedMaterial = LoadMaterial("GridTile_Selected");
        blockedMaterial = LoadMaterial("GridTile_Blocked");
    }
    
    /// <summary>
    /// Loads a material by name, creates default if not found
    /// </summary>
    private Material LoadMaterial(string materialName)
    {
        string materialPath = $"Assets/Materials/{materialName}.mat";
        Material material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        
        if (material == null)
        {
            // Create a default material if specific one not found
            material = new Material(Shader.Find("Standard"));
            
            // Set colors based on material name
            switch (materialName)
            {
                case "GridTile_Default":
                    material.color = new Color(0.8f, 0.8f, 0.8f, 0.3f);
                    break;
                case "GridTile_Hover":
                    material.color = Color.yellow;
                    break;
                case "GridTile_Selected":
                    material.color = Color.green;
                    break;
                case "GridTile_Blocked":
                    material.color = Color.red;
                    break;
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"GridTile: Created default material for {materialName}");
            }
        }
        
        return material;
    }
    
    /// <summary>
    /// Initializes tile with current configuration
    /// </summary>
    private void InitializeTile()
    {
        // Set world position if not already set
        if (worldPosition == Vector3.zero)
        {
            worldPosition = transform.position;
        }
        
        // Validate coordinate bounds
        if (!coordinate.IsValid)
        {
            Debug.LogWarning($"GridTile {name}: Invalid coordinate {coordinate}");
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"GridTile initialized: {name} at {coordinate} (world: {worldPosition})");
        }
    }
    
    /// <summary>
    /// Updates the visual appearance based on current state
    /// </summary>
    private void UpdateVisualState()
    {
        if (!enableVisualFeedback || tileRenderer == null) return;
        
        Material targetMaterial = GetCurrentStateMaterial();
        if (targetMaterial != null && tileRenderer.material != targetMaterial)
        {
            tileRenderer.material = targetMaterial;
        }
    }
    
    /// <summary>
    /// Gets the appropriate material for the current tile state
    /// </summary>
    private Material GetCurrentStateMaterial()
    {
        // Priority order: Blocked > Selected > Hovered > Default
        if (isBlocked && blockedMaterial != null)
            return blockedMaterial;
        
        if (isSelected && selectedMaterial != null)
            return selectedMaterial;
        
        if (isHovered && hoverMaterial != null)
            return hoverMaterial;
        
        return defaultMaterial;
    }
    
    /// <summary>
    /// Sets the selection state of this tile
    /// </summary>
    public void SetSelectionState(bool selected)
    {
        if (isSelected != selected)
        {
            isSelected = selected;
            UpdateVisualState();
            
            if (showDebugInfo)
            {
                Debug.Log($"GridTile {name}: Selection state changed to {selected}");
            }
        }
    }
    
    /// <summary>
    /// Sets the hover state of this tile
    /// </summary>
    public void SetHoverState(bool hovered)
    {
        if (isHovered != hovered)
        {
            isHovered = hovered;
            UpdateVisualState();
            
            if (enableHoverEffect && hovered)
            {
                OnTileHovered?.Invoke(coordinate);
            }
        }
    }
    
    /// <summary>
    /// Sets the occupied state of this tile
    /// </summary>
    public void SetOccupiedState(bool occupied)
    {
        if (isOccupied != occupied)
        {
            isOccupied = occupied;
            UpdateVisualState();
            OnTileOccupiedChanged?.Invoke(coordinate);
            
            if (showDebugInfo)
            {
                Debug.Log($"GridTile {name}: Occupied state changed to {occupied}");
            }
        }
    }
    
    /// <summary>
    /// Sets the blocked state of this tile
    /// </summary>
    public void SetBlockedState(bool blocked)
    {
        if (isBlocked != blocked)
        {
            isBlocked = blocked;
            UpdateVisualState();
            
            if (showDebugInfo)
            {
                Debug.Log($"GridTile {name}: Blocked state changed to {blocked}");
            }
        }
    }
    
    /// <summary>
    /// Checks if this tile can be occupied (not blocked, not already occupied)
    /// </summary>
    public bool CanBeOccupied()
    {
        return !isBlocked && !isOccupied;
    }
    
    /// <summary>
    /// Checks if this tile is accessible for movement
    /// </summary>
    public bool IsAccessible()
    {
        return !isBlocked;
    }
    
    /// <summary>
    /// Gets the movement cost for this tile (for pathfinding)
    /// </summary>
    public float GetMovementCost()
    {
        if (isBlocked) return float.MaxValue;
        if (isOccupied) return 2f; // Higher cost for occupied tiles
        return 1f; // Normal movement cost
    }
    
    /// <summary>
    /// Gets tile information for debugging and UI
    /// </summary>
    public TileInfo GetTileInfo()
    {
        return new TileInfo
        {
            coordinate = coordinate,
            worldPosition = worldPosition,
            isOccupied = isOccupied,
            isSelected = isSelected,
            isHovered = isHovered,
            isBlocked = isBlocked,
            isAccessible = IsAccessible(),
            movementCost = GetMovementCost()
        };
    }
    
    // Mouse interaction events
    void OnMouseEnter()
    {
        if (enableHoverEffect)
        {
            SetHoverState(true);
        }
    }
    
    void OnMouseExit()
    {
        if (enableHoverEffect)
        {
            SetHoverState(false);
        }
    }
    
    void OnMouseDown()
    {
        if (enableClickEffect)
        {
            OnTileClicked?.Invoke(coordinate);
            
            if (showDebugInfo)
            {
                Debug.Log($"GridTile clicked: {name} at {coordinate}");
            }
        }
    }
    
    // Trigger-based interaction for alternative input systems
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Unit"))
        {
            if (showDebugInfo)
            {
                Debug.Log($"GridTile {name}: Unit entered tile");
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Unit"))
        {
            if (showDebugInfo)
            {
                Debug.Log($"GridTile {name}: Unit exited tile");
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;
        
        // Draw tile bounds
        Gizmos.color = isSelected ? Color.green : (isHovered ? Color.yellow : Color.white);
        Gizmos.DrawWireCube(transform.position, new Vector3(tileSize, 0.1f, tileSize));
        
        // Draw coordinate text
        if (Application.isPlaying)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.2f, coordinate.ToString());
#endif
        }
        
        // Draw state indicators
        if (isOccupied)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, 0.1f);
        }
        
        if (isBlocked)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawCube(transform.position + Vector3.up * 0.05f, new Vector3(tileSize * 0.8f, 0.1f, tileSize * 0.8f));
        }
    }
    
    void OnValidate()
    {
        // Update world position when transform changes in editor
        if (worldPosition != transform.position)
        {
            worldPosition = transform.position;
        }
        
        // Update visual state if properties changed
        if (Application.isPlaying)
        {
            UpdateVisualState();
        }
    }
}

/// <summary>
/// Information structure for tile state
/// </summary>
[System.Serializable]
public struct TileInfo
{
    public GridCoordinate coordinate;
    public Vector3 worldPosition;
    public bool isOccupied;
    public bool isSelected;
    public bool isHovered;
    public bool isBlocked;
    public bool isAccessible;
    public float movementCost;
    
    public override string ToString()
    {
        return $"Tile {coordinate}: Occupied={isOccupied}, Selected={isSelected}, Blocked={isBlocked}, Cost={movementCost}";
    }
}