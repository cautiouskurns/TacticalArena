using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Centralized movement coordination system for the tactical arena.
/// Handles click-to-move input, movement validation, and animation coordination.
/// Integrates with SelectionManager to move currently selected units on grid.
/// </summary>
public class MovementManager : MonoBehaviour
{
    [Header("Movement Configuration")]
    [SerializeField] private float movementSpeed = 2.0f;
    [SerializeField] private bool allowDiagonalMovement = false;
    [SerializeField] private bool restrictToAdjacentTiles = true;
    [SerializeField] private bool preventOverlappingMoves = true;
    
    [Header("Raycast Configuration")]
    [SerializeField] private LayerMask gridLayerMask = -1;
    [SerializeField] private float raycastDistance = 100f;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool showMovementPreview = true;
    [SerializeField] private float previewFadeTime = 0.3f;
    [SerializeField] private Material validMoveMaterial;
    [SerializeField] private Material invalidMoveMaterial;
    
    [Header("System Integration")]
    [SerializeField] private SelectionManager selectionManager;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableMovementLogging = true;
    
    // System references
    private GridManager gridManager;
    private MovementValidator movementValidator;
    private MovementAnimator movementAnimator;
    private Camera mainCamera;
    
    // Movement state
    private List<IMovable> movingUnits = new List<IMovable>();
    private Dictionary<IMovable, Coroutine> activeMovements = new Dictionary<IMovable, Coroutine>();
    
    // Input state
    private Vector3 lastClickPosition;
    private float lastClickTime;
    
    // Events
    public System.Action<IMovable, Vector2Int> OnMovementRequested;
    public System.Action<IMovable, Vector2Int> OnMovementStarted;
    public System.Action<IMovable, Vector2Int> OnMovementCompleted;
    public System.Action<IMovable, string> OnMovementFailed;
    
    // Properties
    public bool HasMovingUnits => movingUnits.Count > 0;
    public int MovingUnitCount => movingUnits.Count;
    public IReadOnlyList<IMovable> MovingUnits => movingUnits.AsReadOnly();
    
    void Awake()
    {
        InitializeMovementManager();
    }
    
    void Start()
    {
        FindSystemReferences();
        SetupSystemIntegration();
    }
    
    void Update()
    {
        HandleMovementInput();
    }
    
    /// <summary>
    /// Initializes the movement manager
    /// </summary>
    private void InitializeMovementManager()
    {
        if (enableMovementLogging)
        {
            Debug.Log("MovementManager initialized");
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
            Debug.LogError("MovementManager: GridManager not found! Make sure Task 1.1.x is completed.");
        }
        
        // Find SelectionManager if not assigned
        if (selectionManager == null)
        {
            selectionManager = FindFirstObjectByType<SelectionManager>();
            if (selectionManager == null)
            {
                Debug.LogError("MovementManager: SelectionManager not found! Make sure Task 1.2.2 is completed.");
            }
        }
        
        // Find MovementValidator
        movementValidator = GetComponent<MovementValidator>();
        if (movementValidator == null)
        {
            Debug.LogError("MovementManager: MovementValidator component not found!");
        }
        
        // Find MovementAnimator
        movementAnimator = GetComponent<MovementAnimator>();
        if (movementAnimator == null)
        {
            Debug.LogError("MovementManager: MovementAnimator component not found!");
        }
        
        // Find main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (enableMovementLogging)
        {
            Debug.Log($"MovementManager found references - GridManager: {gridManager != null}, SelectionManager: {selectionManager != null}, Camera: {mainCamera != null}");
        }
    }
    
    /// <summary>
    /// Sets up integration with other systems
    /// </summary>
    private void SetupSystemIntegration()
    {
        if (movementAnimator != null)
        {
            movementAnimator.OnAnimationComplete += OnUnitMovementAnimationComplete;
            movementAnimator.OnAnimationCancelled += OnUnitMovementAnimationCancelled;
        }
    }
    
    /// <summary>
    /// Handles mouse input for movement requests
    /// </summary>
    private void HandleMovementInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ProcessMovementClick();
        }
    }
    
    /// <summary>
    /// Processes a mouse click for potential movement
    /// </summary>
    private void ProcessMovementClick()
    {
        if (mainCamera == null || gridManager == null || selectionManager == null)
        {
            return;
        }
        
        // Check if we have a selected unit that can move
        ISelectable selectedObject = selectionManager.CurrentSelection;
        if (selectedObject == null)
        {
            if (enableMovementLogging)
            {
                Debug.Log("MovementManager: No unit selected for movement");
            }
            return;
        }
        
        // Check if selected object is movable
        IMovable movableUnit = selectedObject as IMovable;
        if (movableUnit == null)
        {
            if (enableMovementLogging)
            {
                Debug.Log($"MovementManager: Selected object {selectedObject.GetDisplayInfo()} is not movable");
            }
            return;
        }
        
        // Raycast to find target grid position
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, gridLayerMask))
        {
            // Convert world position to grid position
            Vector2Int targetGridPosition = new Vector2Int(
                gridManager.WorldToGrid(hit.point).x, 
                gridManager.WorldToGrid(hit.point).z);
            
            if (enableMovementLogging)
            {
                Debug.Log($"MovementManager: Click detected at world {hit.point}, grid {targetGridPosition}");
            }
            
            // Attempt to move unit
            RequestUnitMovement(movableUnit, targetGridPosition);
        }
        else
        {
            if (enableMovementLogging)
            {
                Debug.Log("MovementManager: Click raycast did not hit any grid tiles");
            }
        }
        
        lastClickPosition = Input.mousePosition;
        lastClickTime = Time.time;
    }
    
    /// <summary>
    /// Requests movement for a unit to a target grid position
    /// </summary>
    public bool RequestUnitMovement(IMovable unit, Vector2Int targetGridPosition)
    {
        if (unit == null)
        {
            Debug.LogError("MovementManager: Cannot move null unit");
            return false;
        }
        
        OnMovementRequested?.Invoke(unit, targetGridPosition);
        
        if (enableMovementLogging)
        {
            Debug.Log($"MovementManager: Movement requested for {unit.GetDisplayInfo()} to {targetGridPosition}");
        }
        
        // Check if unit is already moving
        if (unit.IsMoving)
        {
            if (enableMovementLogging)
            {
                Debug.Log($"MovementManager: {unit.GetDisplayInfo()} is already moving");
            }
            OnMovementFailed?.Invoke(unit, "Unit is already moving");
            return false;
        }
        
        // Check overlapping moves restriction
        if (preventOverlappingMoves && HasMovingUnits)
        {
            if (enableMovementLogging)
            {
                Debug.Log("MovementManager: Overlapping moves prevented - another unit is moving");
            }
            OnMovementFailed?.Invoke(unit, "Another unit is currently moving");
            return false;
        }
        
        // Validate movement
        MovementValidationResult validation = ValidateMovement(unit, targetGridPosition);
        if (!validation.isValid)
        {
            if (enableMovementLogging)
            {
                Debug.Log($"MovementManager: Movement validation failed - {validation.reason}");
            }
            OnMovementFailed?.Invoke(unit, validation.reason);
            return false;
        }
        
        // Start movement
        return StartUnitMovement(unit, targetGridPosition);
    }
    
    /// <summary>
    /// Validates movement using the MovementValidator
    /// </summary>
    private MovementValidationResult ValidateMovement(IMovable unit, Vector2Int targetGridPosition)
    {
        if (movementValidator == null)
        {
            return MovementValidationResult.Invalid("Movement validator not available", unit, targetGridPosition);
        }
        
        return movementValidator.ValidateMovement(unit, targetGridPosition);
    }
    
    /// <summary>
    /// Starts the actual movement process for a unit
    /// </summary>
    private bool StartUnitMovement(IMovable unit, Vector2Int targetGridPosition)
    {
        if (gridManager == null || movementAnimator == null)
        {
            Debug.LogError("MovementManager: Required systems not available for movement");
            return false;
        }
        
        // Convert grid position to world position while preserving unit's Y coordinate
        Vector3 targetWorldPosition = gridManager.GridToWorld(new GridCoordinate(targetGridPosition.x, targetGridPosition.y));
        // Preserve unit's original Y position to prevent embedding in ground
        targetWorldPosition.y = unit.Transform.position.y;
        
        // Add to moving units list
        movingUnits.Add(unit);
        
        // Start movement on the unit
        if (!unit.MoveTo(targetGridPosition, targetWorldPosition, true))
        {
            movingUnits.Remove(unit);
            return false;
        }
        
        // Start animation
        Coroutine movementCoroutine = StartCoroutine(AnimateUnitMovement(unit, targetWorldPosition, targetGridPosition));
        activeMovements[unit] = movementCoroutine;
        
        OnMovementStarted?.Invoke(unit, targetGridPosition);
        
        if (enableMovementLogging)
        {
            Debug.Log($"MovementManager: Started movement for {unit.GetDisplayInfo()} to {targetGridPosition}");
        }
        
        return true;
    }
    
    /// <summary>
    /// Animates unit movement to target position
    /// </summary>
    private IEnumerator AnimateUnitMovement(IMovable unit, Vector3 targetWorldPosition, Vector2Int targetGridPosition)
    {
        if (movementAnimator == null)
        {
            // Fallback: instant movement
            unit.Transform.position = targetWorldPosition;
            CompleteUnitMovement(unit, targetGridPosition);
            yield break;
        }
        
        // Use MovementAnimator for smooth animation
        yield return movementAnimator.AnimateMovement(unit.Transform, targetWorldPosition, movementSpeed);
        
        // Complete movement
        CompleteUnitMovement(unit, targetGridPosition);
    }
    
    /// <summary>
    /// Completes unit movement
    /// </summary>
    private void CompleteUnitMovement(IMovable unit, Vector2Int targetGridPosition)
    {
        // Remove from active movements
        if (activeMovements.ContainsKey(unit))
        {
            activeMovements.Remove(unit);
        }
        
        // Remove from moving units list
        movingUnits.Remove(unit);
        
        // Notify unit of completion
        unit.OnMovementCompleted(targetGridPosition);
        
        // Fire events
        OnMovementCompleted?.Invoke(unit, targetGridPosition);
        
        if (enableMovementLogging)
        {
            Debug.Log($"MovementManager: Completed movement for {unit.GetDisplayInfo()} at {targetGridPosition}");
        }
    }
    
    /// <summary>
    /// Cancels unit movement
    /// </summary>
    public void CancelUnitMovement(IMovable unit, string reason = "Movement cancelled")
    {
        if (unit == null) return;
        
        // Stop animation if active
        if (activeMovements.ContainsKey(unit))
        {
            if (activeMovements[unit] != null)
            {
                StopCoroutine(activeMovements[unit]);
            }
            activeMovements.Remove(unit);
        }
        
        // Remove from moving units
        movingUnits.Remove(unit);
        
        // Notify unit of cancellation
        unit.OnMovementCancelled(reason);
        
        // Fire events
        OnMovementFailed?.Invoke(unit, reason);
        
        if (enableMovementLogging)
        {
            Debug.Log($"MovementManager: Cancelled movement for {unit.GetDisplayInfo()} - {reason}");
        }
    }
    
    /// <summary>
    /// Called when movement animation completes
    /// </summary>
    private void OnUnitMovementAnimationComplete(Transform unitTransform, Vector3 finalPosition)
    {
        // Find the corresponding IMovable
        IMovable unit = unitTransform.GetComponent<IMovable>();
        if (unit != null && activeMovements.ContainsKey(unit))
        {
            // Grid position will be set by CompleteUnitMovement
            GridCoordinate gridCoord = gridManager.WorldToGrid(finalPosition);
            Vector2Int gridPos = new Vector2Int(gridCoord.x, gridCoord.z);
            CompleteUnitMovement(unit, gridPos);
        }
    }
    
    /// <summary>
    /// Called when movement animation is cancelled
    /// </summary>
    private void OnUnitMovementAnimationCancelled(Transform unitTransform, string reason)
    {
        IMovable unit = unitTransform.GetComponent<IMovable>();
        if (unit != null)
        {
            CancelUnitMovement(unit, reason);
        }
    }
    
    /// <summary>
    /// Gets all units currently at a grid position
    /// </summary>
    public List<IMovable> GetUnitsAtGridPosition(Vector2Int gridPosition)
    {
        List<IMovable> unitsAtPosition = new List<IMovable>();
        
        // Find all IMovable objects in scene
        IMovable[] allMovables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IMovable>()
            .ToArray();
        
        foreach (IMovable movable in allMovables)
        {
            if (movable.GridPosition == gridPosition)
            {
                unitsAtPosition.Add(movable);
            }
        }
        
        return unitsAtPosition;
    }
    
    /// <summary>
    /// Checks if a grid position is occupied by any unit
    /// </summary>
    public bool IsGridPositionOccupied(Vector2Int gridPosition)
    {
        return GetUnitsAtGridPosition(gridPosition).Count > 0;
    }
    
    /// <summary>
    /// Gets movement information for debugging
    /// </summary>
    public string GetMovementInfo()
    {
        if (movingUnits.Count == 0)
            return "No units currently moving";
        
        return $"Moving units: {string.Join(", ", movingUnits.Select(u => u.GetDisplayInfo()))}";
    }
    
    /// <summary>
    /// Unity Gizmos for debug visualization
    /// </summary>
    void OnDrawGizmos()
    {
        if (!enableMovementLogging) return;
        
        // Draw movement indicators for moving units
        foreach (IMovable movingUnit in movingUnits)
        {
            if (movingUnit?.Transform != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(movingUnit.Transform.position, Vector3.one * 0.8f);
                
                // Draw movement arrow if we have target
                if (gridManager != null)
                {
                    Vector3 targetWorld = gridManager.GridToWorld(new GridCoordinate(movingUnit.GridPosition.x, movingUnit.GridPosition.y));
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(movingUnit.Transform.position, targetWorld);
                }
            }
        }
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Stop all active movements
        foreach (var kvp in activeMovements)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        
        // Clear state
        activeMovements.Clear();
        movingUnits.Clear();
        
        // Unregister from events
        if (movementAnimator != null)
        {
            movementAnimator.OnAnimationComplete -= OnUnitMovementAnimationComplete;
            movementAnimator.OnAnimationCancelled -= OnUnitMovementAnimationCancelled;
        }
        
        // Clear event references
        OnMovementRequested = null;
        OnMovementStarted = null;
        OnMovementCompleted = null;
        OnMovementFailed = null;
    }
}