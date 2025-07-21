using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Movement validation system for the tactical arena.
/// Handles all movement legality checking including obstacles, boundaries, adjacency, and team restrictions.
/// Provides comprehensive validation rules for grid-based tactical movement.
/// </summary>
public class MovementValidator : MonoBehaviour
{
    [Header("Validation Configuration")]
    [SerializeField] private bool enableValidation = true;
    [SerializeField] private bool allowDiagonalMovement = false;
    [SerializeField] private bool restrictToAdjacentTiles = true;
    [SerializeField] private bool checkObstacles = true;
    [SerializeField] private bool checkUnitOccupancy = true;
    
    [Header("Movement Rules")]
    [SerializeField] private int maxMovementDistance = 1;
    [SerializeField] private bool allowMovementThroughUnits = false;
    [SerializeField] private bool allowMovementThroughObstacles = false;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableValidationLogging = true;
    
    // System references
    private GridManager gridManager;
    private MovementManager movementManager;
    
    // Obstacle tracking
    private List<Vector2Int> obstaclePositions = new List<Vector2Int>();
    private bool obstaclesCached = false;
    
    void Awake()
    {
        InitializeValidator();
    }
    
    void Start()
    {
        FindSystemReferences();
        CacheObstaclePositions();
    }
    
    /// <summary>
    /// Initializes the movement validator
    /// </summary>
    private void InitializeValidator()
    {
        if (enableValidationLogging)
        {
            Debug.Log("MovementValidator initialized");
        }
    }
    
    /// <summary>
    /// Finds references to other systems
    /// </summary>
    private void FindSystemReferences()
    {
        // Find GridManager
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("MovementValidator: GridManager not found!");
        }
        
        // Find MovementManager
        movementManager = GetComponent<MovementManager>();
        if (movementManager == null)
        {
            Debug.LogWarning("MovementValidator: MovementManager not found on same GameObject");
        }
        
        if (enableValidationLogging)
        {
            Debug.Log($"MovementValidator found references - GridManager: {gridManager != null}");
        }
    }
    
    /// <summary>
    /// Caches obstacle positions for efficient validation
    /// </summary>
    private void CacheObstaclePositions()
    {
        if (!checkObstacles || gridManager == null)
        {
            obstaclesCached = true;
            return;
        }
        
        obstaclePositions.Clear();
        
        // Find all obstacle objects in the scene
        Obstacle[] obstacles = FindObjectsByType<Obstacle>(FindObjectsSortMode.None);
        
        foreach (Obstacle obstacle in obstacles)
        {
            GridCoordinate gridCoord = gridManager.WorldToGrid(obstacle.transform.position);
            Vector2Int gridPos = new Vector2Int(gridCoord.x, gridCoord.z);
            if (!obstaclePositions.Contains(gridPos))
            {
                obstaclePositions.Add(gridPos);
            }
        }
        
        obstaclesCached = true;
        
        if (enableValidationLogging)
        {
            Debug.Log($"MovementValidator cached {obstaclePositions.Count} obstacle positions");
        }
    }
    
    /// <summary>
    /// Validates movement for a unit to a target position
    /// </summary>
    public MovementValidationResult ValidateMovement(IMovable unit, Vector2Int targetGridPosition)
    {
        if (!enableValidation)
        {
            return MovementValidationResult.Valid(unit, targetGridPosition);
        }
        
        if (unit == null)
        {
            return MovementValidationResult.Invalid("Unit is null", unit, targetGridPosition);
        }
        
        if (enableValidationLogging)
        {
            Debug.Log($"MovementValidator: Validating movement for {unit.GetDisplayInfo()} to {targetGridPosition}");
        }
        
        // Run all validation checks
        MovementValidationResult result;
        
        // 1. Basic movement validation
        result = ValidateBasicMovement(unit, targetGridPosition);
        if (!result.isValid) return result;
        
        // 2. Grid boundary validation
        result = ValidateGridBoundaries(unit, targetGridPosition);
        if (!result.isValid) return result;
        
        // 3. Distance and adjacency validation
        result = ValidateMovementDistance(unit, targetGridPosition);
        if (!result.isValid) return result;
        
        // 4. Obstacle validation
        result = ValidateObstacles(unit, targetGridPosition);
        if (!result.isValid) return result;
        
        // 5. Unit occupancy validation
        result = ValidateUnitOccupancy(unit, targetGridPosition);
        if (!result.isValid) return result;
        
        // 6. Unit-specific validation
        result = ValidateUnitSpecificRules(unit, targetGridPosition);
        if (!result.isValid) return result;
        
        if (enableValidationLogging)
        {
            Debug.Log($"MovementValidator: Movement APPROVED for {unit.GetDisplayInfo()} to {targetGridPosition}");
        }
        
        return MovementValidationResult.Valid(unit, targetGridPosition);
    }
    
    /// <summary>
    /// Validates basic movement requirements
    /// </summary>
    private MovementValidationResult ValidateBasicMovement(IMovable unit, Vector2Int targetGridPosition)
    {
        // Check if unit can move
        if (!unit.CanMove)
        {
            return MovementValidationResult.Invalid("Unit cannot move at this time", unit, targetGridPosition);
        }
        
        // Check if trying to move to current position
        if (unit.GridPosition == targetGridPosition)
        {
            return MovementValidationResult.Invalid("Cannot move to current position", unit, targetGridPosition);
        }
        
        return MovementValidationResult.Valid(unit, targetGridPosition);
    }
    
    /// <summary>
    /// Validates movement is within grid boundaries
    /// </summary>
    private MovementValidationResult ValidateGridBoundaries(IMovable unit, Vector2Int targetGridPosition)
    {
        if (gridManager == null)
        {
            return MovementValidationResult.Invalid("Grid manager not available", unit, targetGridPosition);
        }
        
        GridCoordinate targetCoord = new GridCoordinate(targetGridPosition.x, targetGridPosition.y);
        if (!gridManager.IsValidCoordinate(targetCoord))
        {
            return MovementValidationResult.Invalid($"Target position {targetGridPosition} is outside grid boundaries", unit, targetGridPosition);
        }
        
        return MovementValidationResult.Valid(unit, targetGridPosition);
    }
    
    /// <summary>
    /// Validates movement distance and adjacency rules
    /// </summary>
    private MovementValidationResult ValidateMovementDistance(IMovable unit, Vector2Int targetGridPosition)
    {
        Vector2Int currentPos = unit.GridPosition;
        Vector2Int distance = targetGridPosition - currentPos;
        
        // Calculate movement distance
        int movementDistance;
        if (allowDiagonalMovement)
        {
            // Chebyshev distance (max of x and y differences)
            movementDistance = Mathf.Max(Mathf.Abs(distance.x), Mathf.Abs(distance.y));
        }
        else
        {
            // Manhattan distance (sum of x and y differences)
            movementDistance = Mathf.Abs(distance.x) + Mathf.Abs(distance.y);
            
            // Also check for diagonal movement when not allowed
            if (distance.x != 0 && distance.y != 0)
            {
                return MovementValidationResult.Invalid("Diagonal movement is not allowed", unit, targetGridPosition);
            }
        }
        
        // Check distance restriction
        if (restrictToAdjacentTiles && movementDistance > maxMovementDistance)
        {
            return MovementValidationResult.Invalid($"Movement distance {movementDistance} exceeds maximum {maxMovementDistance}", unit, targetGridPosition);
        }
        
        return MovementValidationResult.Valid(unit, targetGridPosition);
    }
    
    /// <summary>
    /// Validates movement against obstacles
    /// </summary>
    private MovementValidationResult ValidateObstacles(IMovable unit, Vector2Int targetGridPosition)
    {
        if (!checkObstacles)
        {
            return MovementValidationResult.Valid(unit, targetGridPosition);
        }
        
        // Refresh obstacle cache if needed
        if (!obstaclesCached)
        {
            CacheObstaclePositions();
        }
        
        // Check if target position has obstacle
        if (obstaclePositions.Contains(targetGridPosition))
        {
            return MovementValidationResult.Invalid("Target position is blocked by obstacle", unit, targetGridPosition);
        }
        
        // Check path to target (for multi-tile movements)
        if (!allowMovementThroughObstacles)
        {
            List<Vector2Int> path = GetMovementPath(unit.GridPosition, targetGridPosition);
            foreach (Vector2Int pathPosition in path)
            {
                if (pathPosition != unit.GridPosition && obstaclePositions.Contains(pathPosition))
                {
                    return MovementValidationResult.Invalid($"Movement path blocked by obstacle at {pathPosition}", unit, targetGridPosition);
                }
            }
        }
        
        return MovementValidationResult.Valid(unit, targetGridPosition);
    }
    
    /// <summary>
    /// Validates movement against unit occupancy
    /// </summary>
    private MovementValidationResult ValidateUnitOccupancy(IMovable unit, Vector2Int targetGridPosition)
    {
        if (!checkUnitOccupancy)
        {
            return MovementValidationResult.Valid(unit, targetGridPosition);
        }
        
        // Check if target position is occupied by other units
        if (movementManager != null)
        {
            List<IMovable> unitsAtTarget = movementManager.GetUnitsAtGridPosition(targetGridPosition);
            
            // Remove the current unit from the list (it's okay to move to where you are)
            unitsAtTarget.RemoveAll(u => u == unit);
            
            if (unitsAtTarget.Count > 0)
            {
                string unitNames = string.Join(", ", unitsAtTarget.Select(u => u.GetDisplayInfo()));
                return MovementValidationResult.Invalid($"Target position occupied by: {unitNames}", unit, targetGridPosition);
            }
        }
        
        return MovementValidationResult.Valid(unit, targetGridPosition);
    }
    
    /// <summary>
    /// Validates unit-specific movement rules
    /// </summary>
    private MovementValidationResult ValidateUnitSpecificRules(IMovable unit, Vector2Int targetGridPosition)
    {
        // Use the unit's own validation if available
        MovementValidationResult unitValidation = unit.ValidateMovement(targetGridPosition);
        if (!unitValidation.isValid)
        {
            return unitValidation;
        }
        
        return MovementValidationResult.Valid(unit, targetGridPosition);
    }
    
    /// <summary>
    /// Gets the movement path between two positions
    /// </summary>
    private List<Vector2Int> GetMovementPath(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        
        // For adjacent moves, path is just start and end
        Vector2Int distance = end - start;
        
        if (Mathf.Abs(distance.x) <= 1 && Mathf.Abs(distance.y) <= 1)
        {
            path.Add(start);
            path.Add(end);
        }
        else
        {
            // For longer moves, create intermediate points
            Vector2Int current = start;
            while (current != end)
            {
                path.Add(current);
                
                // Move one step toward target
                Vector2Int step = Vector2Int.zero;
                if (current.x != end.x)
                    step.x = current.x < end.x ? 1 : -1;
                if (current.y != end.y)
                    step.y = current.y < end.y ? 1 : -1;
                
                current += step;
            }
            path.Add(end);
        }
        
        return path;
    }
    
    /// <summary>
    /// Checks if a grid position is valid for movement
    /// </summary>
    public bool IsPositionValidForMovement(Vector2Int gridPosition)
    {
        GridCoordinate coord = new GridCoordinate(gridPosition.x, gridPosition.y);
        if (gridManager == null || !gridManager.IsValidCoordinate(coord))
        {
            return false;
        }
        
        if (checkObstacles && obstaclePositions.Contains(gridPosition))
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Gets all valid adjacent positions for a unit
    /// </summary>
    public List<Vector2Int> GetValidAdjacentPositions(IMovable unit)
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        
        if (unit == null) return validPositions;
        
        Vector2Int currentPos = unit.GridPosition;
        
        // Generate adjacent positions
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; // Skip current position
                
                if (!allowDiagonalMovement && x != 0 && y != 0) continue; // Skip diagonals if not allowed
                
                Vector2Int adjacentPos = currentPos + new Vector2Int(x, y);
                
                MovementValidationResult validation = ValidateMovement(unit, adjacentPos);
                if (validation.isValid)
                {
                    validPositions.Add(adjacentPos);
                }
            }
        }
        
        return validPositions;
    }
    
    /// <summary>
    /// Refreshes cached obstacle positions
    /// </summary>
    public void RefreshObstacleCache()
    {
        obstaclesCached = false;
        CacheObstaclePositions();
    }
    
    /// <summary>
    /// Gets validation info for debugging
    /// </summary>
    public string GetValidationInfo()
    {
        return $"Validation Rules: Adjacent={restrictToAdjacentTiles}, Diagonal={allowDiagonalMovement}, Obstacles={checkObstacles} ({obstaclePositions.Count} cached), Units={checkUnitOccupancy}";
    }
    
    /// <summary>
    /// Unity Gizmos for debug visualization
    /// </summary>
    void OnDrawGizmos()
    {
        if (!enableValidationLogging || gridManager == null) return;
        
        // Draw obstacle positions
        Gizmos.color = Color.red;
        foreach (Vector2Int obstaclePos in obstaclePositions)
        {
            Vector3 worldPos = gridManager.GridToWorld(new GridCoordinate(obstaclePos.x, obstaclePos.y));
            Gizmos.DrawWireCube(worldPos, Vector3.one * 0.9f);
        }
    }
}