using UnityEngine;
using System;

/// <summary>
/// Interface for objects that can move on the grid system.
/// Defines the contract for grid-based movement with validation and animation.
/// Works in conjunction with MovementManager to provide tactical movement mechanics.
/// </summary>
public interface IMovable
{
    /// <summary>
    /// Current grid position of the object
    /// </summary>
    Vector2Int GridPosition { get; set; }
    
    /// <summary>
    /// Transform component for world position manipulation
    /// </summary>
    Transform Transform { get; }
    
    /// <summary>
    /// Whether this object can currently move
    /// </summary>
    bool CanMove { get; }
    
    /// <summary>
    /// Whether this object is currently moving
    /// </summary>
    bool IsMoving { get; }
    
    /// <summary>
    /// Team assignment for movement validation
    /// </summary>
    UnitTeam Team { get; }
    
    /// <summary>
    /// Display information for logging and debugging
    /// </summary>
    string GetDisplayInfo();
    
    /// <summary>
    /// Attempts to move to a target grid position
    /// </summary>
    /// <param name="targetGridPosition">Target position in grid coordinates</param>
    /// <param name="worldPosition">Target position in world coordinates</param>
    /// <param name="animated">Whether to animate the movement</param>
    /// <returns>True if movement was initiated successfully</returns>
    bool MoveTo(Vector2Int targetGridPosition, Vector3 worldPosition, bool animated = true);
    
    /// <summary>
    /// Validates whether movement to a target position is legal
    /// </summary>
    /// <param name="targetGridPosition">Target grid position to validate</param>
    /// <returns>Movement validation result with success status and reason</returns>
    MovementValidationResult ValidateMovement(Vector2Int targetGridPosition);
    
    /// <summary>
    /// Called when movement starts
    /// </summary>
    /// <param name="targetPosition">Target grid position</param>
    void OnMovementStarted(Vector2Int targetPosition);
    
    /// <summary>
    /// Called when movement completes successfully
    /// </summary>
    /// <param name="finalPosition">Final grid position</param>
    void OnMovementCompleted(Vector2Int finalPosition);
    
    /// <summary>
    /// Called when movement is cancelled or fails
    /// </summary>
    /// <param name="reason">Reason for movement cancellation</param>
    void OnMovementCancelled(string reason);
    
    // Events
    event Action<IMovable, Vector2Int> OnMovementStart;
    event Action<IMovable, Vector2Int> OnMovementComplete;
    event Action<IMovable, string> OnMovementCancel;
}

/// <summary>
/// Abstract base class implementing common IMovable functionality.
/// Provides default implementations and event management for grid-movable objects.
/// </summary>
public abstract class MovableBase : MonoBehaviour, IMovable
{
    [Header("Movement State")]
    [SerializeField] private Vector2Int gridPosition;
    [SerializeField] private bool canMove = true;
    [SerializeField] private bool isMoving = false;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableMovementLogging = false;
    
    // IMovable Properties
    public virtual Vector2Int GridPosition 
    { 
        get => gridPosition; 
        set 
        { 
            Vector2Int oldPosition = gridPosition;
            gridPosition = value;
            
            if (enableMovementLogging)
            {
                Debug.Log($"{GetDisplayInfo()} grid position changed: {oldPosition} -> {value}");
            }
        } 
    }
    
    public virtual Transform Transform => transform;
    public virtual bool CanMove => canMove && !isMoving;
    public virtual bool IsMoving => isMoving;
    public abstract UnitTeam Team { get; }
    
    // Events
    public event Action<IMovable, Vector2Int> OnMovementStart;
    public event Action<IMovable, Vector2Int> OnMovementComplete;
    public event Action<IMovable, string> OnMovementCancel;
    
    public virtual string GetDisplayInfo()
    {
        return $"{gameObject.name} [{Team}] at {GridPosition}";
    }
    
    public virtual bool MoveTo(Vector2Int targetGridPosition, Vector3 worldPosition, bool animated = true)
    {
        if (!CanMove)
        {
            if (enableMovementLogging)
            {
                Debug.Log($"{GetDisplayInfo()} cannot move (CanMove = false)");
            }
            return false;
        }
        
        // Validate movement
        MovementValidationResult validation = ValidateMovement(targetGridPosition);
        if (!validation.isValid)
        {
            if (enableMovementLogging)
            {
                Debug.Log($"{GetDisplayInfo()} movement denied: {validation.reason}");
            }
            OnMovementCancelled(validation.reason);
            return false;
        }
        
        // Start movement
        isMoving = true;
        OnMovementStarted(targetGridPosition);
        
        if (enableMovementLogging)
        {
            Debug.Log($"{GetDisplayInfo()} starting movement to {targetGridPosition}");
        }
        
        return true;
    }
    
    public virtual MovementValidationResult ValidateMovement(Vector2Int targetGridPosition)
    {
        // Basic validation - override in derived classes for specific rules
        if (targetGridPosition == GridPosition)
        {
            return MovementValidationResult.Invalid("Cannot move to current position", this);
        }
        
        return MovementValidationResult.Valid(this);
    }
    
    public virtual void OnMovementStarted(Vector2Int targetPosition)
    {
        OnMovementStart?.Invoke(this, targetPosition);
        
        if (enableMovementLogging)
        {
            Debug.Log($"{GetDisplayInfo()} movement started to {targetPosition}");
        }
    }
    
    public virtual void OnMovementCompleted(Vector2Int finalPosition)
    {
        GridPosition = finalPosition;
        isMoving = false;
        OnMovementComplete?.Invoke(this, finalPosition);
        
        if (enableMovementLogging)
        {
            Debug.Log($"{GetDisplayInfo()} movement completed at {finalPosition}");
        }
    }
    
    public virtual void OnMovementCancelled(string reason)
    {
        isMoving = false;
        OnMovementCancel?.Invoke(this, reason);
        
        if (enableMovementLogging)
        {
            Debug.Log($"{GetDisplayInfo()} movement cancelled: {reason}");
        }
    }
    
    /// <summary>
    /// Sets the movement state (used by movement system)
    /// </summary>
    public virtual void SetMovementState(bool moving)
    {
        isMoving = moving;
    }
    
    /// <summary>
    /// Sets whether this object can move
    /// </summary>
    public virtual void SetCanMove(bool canMove)
    {
        this.canMove = canMove;
    }
    
    /// <summary>
    /// Unity Inspector validation
    /// </summary>
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            // Ensure grid position matches world position if possible
            GridManager gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager != null)
            {
                Vector3 expectedWorldPos = gridManager.GridToWorld(new GridCoordinate(GridPosition.x, GridPosition.y));
                if (Vector3.Distance(transform.position, expectedWorldPos) > 0.5f)
                {
                    if (enableMovementLogging)
                    {
                        Debug.Log($"{GetDisplayInfo()} position mismatch detected - syncing to grid");
                    }
                }
            }
        }
    }
}

/// <summary>
/// Result structure for movement validation
/// </summary>
[System.Serializable]
public class MovementValidationResult
{
    public bool isValid;
    public string reason;
    public IMovable movable;
    public Vector2Int targetPosition;
    
    public MovementValidationResult(bool isValid, string reason, IMovable movable, Vector2Int targetPosition = default)
    {
        this.isValid = isValid;
        this.reason = reason ?? "";
        this.movable = movable;
        this.targetPosition = targetPosition;
    }
    
    public static MovementValidationResult Valid(IMovable movable, Vector2Int targetPosition = default)
    {
        return new MovementValidationResult(true, "Movement is valid", movable, targetPosition);
    }
    
    public static MovementValidationResult Invalid(string reason, IMovable movable, Vector2Int targetPosition = default)
    {
        return new MovementValidationResult(false, reason, movable, targetPosition);
    }
    
    public override string ToString()
    {
        return $"MovementValidation: {(isValid ? "VALID" : "INVALID")} - {reason}";
    }
}