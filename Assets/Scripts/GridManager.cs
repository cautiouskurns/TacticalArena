using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Core grid system management for the 4x4 tactical battlefield.
/// Handles coordinate conversion, tile management, and spatial queries.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    [SerializeField] private int gridWidth = 4;
    [SerializeField] private int gridHeight = 4;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float tileSpacing = 0.05f;
    [SerializeField] private Vector3 gridCenter = new Vector3(1.5f, 0f, 1.5f);
    
    [Header("Runtime Settings")]
    [SerializeField] private bool enableTileColliders = true;
    [SerializeField] private bool enableDebugGizmos = true;
    
    [Header("Obstacle Integration")]
    [SerializeField] private ObstacleManager obstacleManager;
    
    [Header("Debug Information")]
    [SerializeField] private GridCoordinate hoveredTile = GridCoordinate.Invalid;
    [SerializeField] private GridCoordinate selectedTile = GridCoordinate.Invalid;
    
    // Grid data storage
    private GridTile[,] gridTiles;
    private Dictionary<GridCoordinate, GridTile> coordinateToTile;
    private Dictionary<Vector3, GridCoordinate> worldToGridLookup;
    
    // Cached values for performance
    private Vector3 gridStartPosition;
    private float totalGridWidth;
    private float totalGridHeight;
    
    // Events for tile interaction
    public System.Action<GridCoordinate> OnTileHovered;
    public System.Action<GridCoordinate> OnTileSelected;
    public System.Action<GridCoordinate> OnTileDeselected;
    
    // Public properties
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public int TotalTiles => gridWidth * gridHeight;
    public float TileSize => tileSize;
    public float TileSpacing => tileSpacing;
    public Vector3 GridCenter => gridCenter;
    public GridCoordinate HoveredTile => hoveredTile;
    public GridCoordinate SelectedTile => selectedTile;
    
    void Awake()
    {
        InitializeGridSystem();
    }
    
    void Start()
    {
        ValidateGridSetup();
        CacheGridTiles();
    }
    
    /// <summary>
    /// Initializes the grid system with current configuration
    /// </summary>
    private void InitializeGridSystem()
    {
        CalculateGridDimensions();
        InitializeDataStructures();
        
        Debug.Log($"GridManager initialized: {gridWidth}x{gridHeight} grid with {TotalTiles} tiles");
    }
    
    /// <summary>
    /// Calculates grid positioning and dimensions
    /// </summary>
    private void CalculateGridDimensions()
    {
        totalGridWidth = (gridWidth * tileSize) + ((gridWidth - 1) * tileSpacing);
        totalGridHeight = (gridHeight * tileSize) + ((gridHeight - 1) * tileSpacing);
        
        gridStartPosition = gridCenter - new Vector3(
            (totalGridWidth - tileSize) / 2f,
            0f,
            (totalGridHeight - tileSize) / 2f
        );
    }
    
    /// <summary>
    /// Initializes data structures for grid management
    /// </summary>
    private void InitializeDataStructures()
    {
        gridTiles = new GridTile[gridWidth, gridHeight];
        coordinateToTile = new Dictionary<GridCoordinate, GridTile>();
        worldToGridLookup = new Dictionary<Vector3, GridCoordinate>();
    }
    
    /// <summary>
    /// Finds and caches all grid tiles in the scene
    /// </summary>
    private void CacheGridTiles()
    {
        GameObject tilesGroup = transform.Find("Tiles")?.gameObject;
        if (tilesGroup == null)
        {
            Debug.LogWarning("GridManager: Tiles group not found. Grid tiles may not be properly initialized.");
            return;
        }
        
        int tilesFound = 0;
        
        for (int i = 0; i < tilesGroup.transform.childCount; i++)
        {
            Transform tileTransform = tilesGroup.transform.GetChild(i);
            GridTile gridTile = tileTransform.GetComponent<GridTile>();
            
            if (gridTile != null)
            {
                GridCoordinate coord = gridTile.Coordinate;
                
                if (coord.IsWithinBounds(gridWidth, gridHeight))
                {
                    gridTiles[coord.x, coord.z] = gridTile;
                    coordinateToTile[coord] = gridTile;
                    worldToGridLookup[gridTile.WorldPosition] = coord;
                    
                    // Set up tile event listeners
                    gridTile.OnTileHovered += HandleTileHovered;
                    gridTile.OnTileClicked += HandleTileClicked;
                    
                    tilesFound++;
                }
                else
                {
                    Debug.LogWarning($"GridManager: Tile {gridTile.name} has invalid coordinates {coord}");
                }
            }
        }
        
        Debug.Log($"GridManager: Cached {tilesFound} grid tiles");
    }
    
    /// <summary>
    /// Validates that the grid setup is correct
    /// </summary>
    private void ValidateGridSetup()
    {
        bool isValid = true;
        
        // Check grid bounds
        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogError("GridManager: Grid dimensions must be positive");
            isValid = false;
        }
        
        // Check tile size
        if (tileSize <= 0f)
        {
            Debug.LogError("GridManager: Tile size must be positive");
            isValid = false;
        }
        
        // Check for camera integration
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 viewportPoint = mainCamera.WorldToViewportPoint(gridCenter);
            if (viewportPoint.x < 0.1f || viewportPoint.x > 0.9f || 
                viewportPoint.y < 0.1f || viewportPoint.y > 0.9f)
            {
                Debug.LogWarning("GridManager: Grid may not be fully visible from main camera");
            }
        }
        
        if (isValid)
        {
            Debug.Log("GridManager: Grid setup validation passed");
        }
    }
    
    /// <summary>
    /// Converts world position to grid coordinate
    /// </summary>
    public GridCoordinate WorldToGrid(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition - gridStartPosition;
        
        int gridX = Mathf.RoundToInt(localPos.x / (tileSize + tileSpacing));
        int gridZ = Mathf.RoundToInt(localPos.z / (tileSize + tileSpacing));
        
        GridCoordinate coord = new GridCoordinate(gridX, gridZ);
        
        // Validate bounds
        if (!coord.IsWithinBounds(gridWidth, gridHeight))
        {
            return GridCoordinate.Invalid;
        }
        
        return coord;
    }
    
    /// <summary>
    /// Converts grid coordinate to world position
    /// </summary>
    public Vector3 GridToWorld(GridCoordinate coordinate)
    {
        // Ensure grid dimensions are calculated
        if (gridStartPosition == Vector3.zero || totalGridWidth == 0)
        {
            CalculateGridDimensions();
        }
        
        if (!coordinate.IsWithinBounds(gridWidth, gridHeight))
        {
            Debug.LogWarning($"GridManager: Invalid coordinate {coordinate}");
            return Vector3.zero;
        }
        
        float offsetX = coordinate.x * (tileSize + tileSpacing);
        float offsetZ = coordinate.z * (tileSize + tileSpacing);
        
        return gridStartPosition + new Vector3(offsetX, 0f, offsetZ);
    }
    
    /// <summary>
    /// Gets the tile at the specified grid coordinate
    /// </summary>
    public GridTile GetTile(GridCoordinate coordinate)
    {
        // Ensure data structures are initialized
        if (coordinateToTile == null)
        {
            InitializeDataStructures();
            CacheGridTiles();
        }
        
        if (coordinateToTile.TryGetValue(coordinate, out GridTile tile))
        {
            return tile;
        }
        return null;
    }
    
    /// <summary>
    /// Gets the tile at the specified grid position
    /// </summary>
    public GridTile GetTile(int x, int z)
    {
        return GetTile(new GridCoordinate(x, z));
    }
    
    /// <summary>
    /// Checks if a coordinate is valid and within grid bounds
    /// </summary>
    public bool IsValidCoordinate(GridCoordinate coordinate)
    {
        return coordinate.IsWithinBounds(gridWidth, gridHeight);
    }
    
    /// <summary>
    /// Checks if a coordinate is occupied (for future obstacle/unit placement)
    /// </summary>
    public bool IsCoordinateOccupied(GridCoordinate coordinate)
    {
        GridTile tile = GetTile(coordinate);
        if (tile != null)
        {
            return tile.IsOccupied;
        }
        return false;
    }
    
    /// <summary>
    /// Gets all tiles within a specified range of a coordinate
    /// </summary>
    public List<GridTile> GetTilesInRange(GridCoordinate center, int range)
    {
        List<GridTile> tilesInRange = new List<GridTile>();
        
        for (int x = center.x - range; x <= center.x + range; x++)
        {
            for (int z = center.z - range; z <= center.z + range; z++)
            {
                GridCoordinate coord = new GridCoordinate(x, z);
                if (IsValidCoordinate(coord))
                {
                    GridTile tile = GetTile(coord);
                    if (tile != null)
                    {
                        tilesInRange.Add(tile);
                    }
                }
            }
        }
        
        return tilesInRange;
    }
    
    /// <summary>
    /// Gets all neighboring tiles of a coordinate
    /// </summary>
    public List<GridTile> GetNeighboringTiles(GridCoordinate coordinate, bool includeOccupied = true)
    {
        List<GridTile> neighbors = new List<GridTile>();
        GridCoordinate[] neighborCoords = coordinate.GetOrthogonalNeighbors();
        
        foreach (GridCoordinate neighborCoord in neighborCoords)
        {
            if (IsValidCoordinate(neighborCoord))
            {
                GridTile tile = GetTile(neighborCoord);
                if (tile != null && (includeOccupied || !tile.IsOccupied))
                {
                    neighbors.Add(tile);
                }
            }
        }
        
        return neighbors;
    }
    
    /// <summary>
    /// Checks if there's a clear line of sight between two coordinates
    /// </summary>
    public bool HasLineOfSight(GridCoordinate from, GridCoordinate to)
    {
        // Delegate to obstacle manager if available
        if (obstacleManager != null && obstacleManager.LineOfSightEnabled)
        {
            return obstacleManager.HasLineOfSight(from, to);
        }
        
        // Fallback: Simple implementation - just check if both coordinates are valid
        return IsValidCoordinate(from) && IsValidCoordinate(to);
    }
    
    /// <summary>
    /// Gets the cover value between two coordinates
    /// </summary>
    public float GetCoverValue(GridCoordinate from, GridCoordinate to)
    {
        if (obstacleManager != null && obstacleManager.PartialCoverEnabled)
        {
            return obstacleManager.GetCoverValue(from, to);
        }
        
        return 0f; // No cover if obstacle manager not available
    }
    
    /// <summary>
    /// Checks if a coordinate has an obstacle
    /// </summary>
    public bool HasObstacle(GridCoordinate coordinate)
    {
        if (obstacleManager != null)
        {
            return obstacleManager.HasObstacle(coordinate);
        }
        
        // Fallback: Check tile occupation status
        GridTile tile = GetTile(coordinate);
        return tile != null && tile.IsOccupied;
    }
    
    /// <summary>
    /// Gets all coordinates that are blocked for movement
    /// </summary>
    public List<GridCoordinate> GetBlockedCoordinates()
    {
        if (obstacleManager != null)
        {
            return obstacleManager.GetBlockedCoordinates();
        }
        
        // Fallback: Check all tiles for occupation
        List<GridCoordinate> blockedCoords = new List<GridCoordinate>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                GridCoordinate coord = new GridCoordinate(x, z);
                if (IsCoordinateOccupied(coord))
                {
                    blockedCoords.Add(coord);
                }
            }
        }
        
        return blockedCoords;
    }
    
    /// <summary>
    /// Gets all coordinates that are free for movement
    /// </summary>
    public List<GridCoordinate> GetFreeCoordinates()
    {
        if (obstacleManager != null)
        {
            return obstacleManager.GetFreeCoordinates();
        }
        
        // Fallback: Check all tiles for availability
        List<GridCoordinate> freeCoords = new List<GridCoordinate>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                GridCoordinate coord = new GridCoordinate(x, z);
                if (!IsCoordinateOccupied(coord))
                {
                    freeCoords.Add(coord);
                }
            }
        }
        
        return freeCoords;
    }
    
    /// <summary>
    /// Sets the obstacle manager reference
    /// </summary>
    public void SetObstacleManager(ObstacleManager manager)
    {
        obstacleManager = manager;
        Debug.Log("GridManager: Obstacle manager integration established");
    }
    
    /// <summary>
    /// Selects a tile at the specified coordinate
    /// </summary>
    public void SelectTile(GridCoordinate coordinate)
    {
        // Deselect previous tile
        if (selectedTile.IsValid)
        {
            GridTile previousTile = GetTile(selectedTile);
            if (previousTile != null)
            {
                previousTile.SetSelectionState(false);
            }
            OnTileDeselected?.Invoke(selectedTile);
        }
        
        // Select new tile
        if (IsValidCoordinate(coordinate))
        {
            selectedTile = coordinate;
            GridTile newTile = GetTile(coordinate);
            if (newTile != null)
            {
                newTile.SetSelectionState(true);
            }
            OnTileSelected?.Invoke(coordinate);
        }
        else
        {
            selectedTile = GridCoordinate.Invalid;
        }
    }
    
    /// <summary>
    /// Clears tile selection
    /// </summary>
    public void ClearSelection()
    {
        SelectTile(GridCoordinate.Invalid);
    }
    
    /// <summary>
    /// Handle tile hover events
    /// </summary>
    private void HandleTileHovered(GridCoordinate coordinate)
    {
        hoveredTile = coordinate;
        OnTileHovered?.Invoke(coordinate);
    }
    
    /// <summary>
    /// Handle tile click events
    /// </summary>
    private void HandleTileClicked(GridCoordinate coordinate)
    {
        SelectTile(coordinate);
    }
    
    /// <summary>
    /// Gets grid information for debugging and UI
    /// </summary>
    public GridInfo GetGridInfo()
    {
        return new GridInfo
        {
            gridSize = new Vector2Int(gridWidth, gridHeight),
            tileSize = tileSize,
            tileSpacing = tileSpacing,
            gridCenter = gridCenter,
            totalTiles = TotalTiles,
            selectedTile = selectedTile,
            hoveredTile = hoveredTile,
            gridBounds = new Bounds(gridCenter, new Vector3(totalGridWidth, 0.1f, totalGridHeight)),
            obstacleCount = obstacleManager != null ? obstacleManager.ObstacleCount : 0,
            lineOfSightEnabled = obstacleManager != null ? obstacleManager.LineOfSightEnabled : false
        };
    }
    
    void OnDrawGizmos()
    {
        if (!enableDebugGizmos) return;
        
        // Draw grid bounds
        Gizmos.color = Color.cyan;
        if (Application.isPlaying)
        {
            Gizmos.DrawWireCube(gridCenter, new Vector3(totalGridWidth, 0.1f, totalGridHeight));
        }
        else
        {
            // Preview during edit mode
            float previewWidth = gridWidth * (tileSize + tileSpacing) - tileSpacing;
            float previewHeight = gridHeight * (tileSize + tileSpacing) - tileSpacing;
            Gizmos.DrawWireCube(gridCenter, new Vector3(previewWidth, 0.1f, previewHeight));
        }
        
        // Draw grid center
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(gridCenter, 0.1f);
        
        // Draw selected tile
        if (Application.isPlaying && selectedTile.IsValid)
        {
            Gizmos.color = Color.green;
            Vector3 selectedWorldPos = GridToWorld(selectedTile);
            Gizmos.DrawWireCube(selectedWorldPos + Vector3.up * 0.05f, 
                              new Vector3(tileSize, 0.1f, tileSize));
        }
        
        // Draw hovered tile
        if (Application.isPlaying && hoveredTile.IsValid && hoveredTile != selectedTile)
        {
            Gizmos.color = Color.yellow;
            Vector3 hoveredWorldPos = GridToWorld(hoveredTile);
            Gizmos.DrawWireCube(hoveredWorldPos + Vector3.up * 0.05f, 
                              new Vector3(tileSize, 0.1f, tileSize));
        }
    }
}

/// <summary>
/// Information structure for grid system state
/// </summary>
[System.Serializable]
public struct GridInfo
{
    public Vector2Int gridSize;
    public float tileSize;
    public float tileSpacing;
    public Vector3 gridCenter;
    public int totalTiles;
    public GridCoordinate selectedTile;
    public GridCoordinate hoveredTile;
    public Bounds gridBounds;
    public int obstacleCount;
    public bool lineOfSightEnabled;
    
    public override string ToString()
    {
        return $"Grid {gridSize.x}x{gridSize.y}, {totalTiles} tiles, {obstacleCount} obstacles, Selected: {selectedTile}, Hovered: {hoveredTile}";
    }
}