using UnityEngine;

/// <summary>
/// Interface for objects that can be selected by the mouse selection system.
/// Provides a contract for selection behavior, state management, and team validation.
/// Designed for extensibility to support units, tiles, and other interactive objects.
/// </summary>
public interface ISelectable
{
    /// <summary>
    /// Gets whether this object is currently selected
    /// </summary>
    bool IsSelected { get; }
    
    /// <summary>
    /// Gets whether this object is currently being hovered over
    /// </summary>
    bool IsHovered { get; }
    
    /// <summary>
    /// Gets whether this object can be selected (considering team restrictions, state, etc.)
    /// </summary>
    bool CanBeSelected { get; }
    
    /// <summary>
    /// Gets the team this selectable object belongs to (for validation)
    /// </summary>
    UnitTeam Team { get; }
    
    /// <summary>
    /// Gets the GameObject associated with this selectable
    /// </summary>
    GameObject GameObject { get; }
    
    /// <summary>
    /// Gets the transform of the selectable object
    /// </summary>
    Transform Transform { get; }
    
    /// <summary>
    /// Event fired when this object becomes selected
    /// </summary>
    System.Action<ISelectable> OnSelected { get; set; }
    
    /// <summary>
    /// Event fired when this object becomes deselected
    /// </summary>
    System.Action<ISelectable> OnDeselected { get; set; }
    
    /// <summary>
    /// Event fired when hover state changes
    /// </summary>
    System.Action<ISelectable, bool> OnHoverChanged { get; set; }
    
    /// <summary>
    /// Selects this object
    /// </summary>
    /// <returns>True if selection was successful, false if not allowed</returns>
    bool Select();
    
    /// <summary>
    /// Deselects this object
    /// </summary>
    void Deselect();
    
    /// <summary>
    /// Sets the hover state for this object
    /// </summary>
    /// <param name="isHovered">Whether the object is being hovered over</param>
    void SetHover(bool isHovered);
    
    /// <summary>
    /// Validates whether this object can be selected by the specified team or player
    /// </summary>
    /// <param name="requestingTeam">The team trying to select this object</param>
    /// <param name="restrictToPlayerTeam">Whether selection is restricted to player team only</param>
    /// <returns>True if selection is allowed</returns>
    bool ValidateSelection(UnitTeam requestingTeam, bool restrictToPlayerTeam);
    
    /// <summary>
    /// Gets display information about this selectable object
    /// </summary>
    /// <returns>Formatted string with object information</returns>
    string GetDisplayInfo();
}

/// <summary>
/// Base implementation of ISelectable interface to reduce code duplication.
/// Can be inherited by components that need selection functionality.
/// </summary>
public abstract class SelectableBase : MonoBehaviour, ISelectable
{
    [Header("Selection State")]
    [SerializeField] protected bool isSelected = false;
    [SerializeField] protected bool isHovered = false;
    [SerializeField] protected bool canBeSelected = true;
    
    [Header("Selection Configuration")]
    [SerializeField] protected bool enableSelectionValidation = true;
    [SerializeField] protected bool enableTeamRestrictions = true;
    
    // Events
    public System.Action<ISelectable> OnSelected { get; set; }
    public System.Action<ISelectable> OnDeselected { get; set; }
    public System.Action<ISelectable, bool> OnHoverChanged { get; set; }
    
    // Properties
    public bool IsSelected => isSelected;
    public bool IsHovered => isHovered;
    public virtual bool CanBeSelected => canBeSelected && enabled && gameObject.activeInHierarchy;
    public abstract UnitTeam Team { get; }
    public GameObject GameObject => gameObject;
    public Transform Transform => transform;
    
    /// <summary>
    /// Selects this object
    /// </summary>
    public virtual bool Select()
    {
        if (!CanBeSelected || isSelected)
            return false;
        
        isSelected = true;
        OnSelectionChanged(true);
        OnSelected?.Invoke(this);
        
        if (enableSelectionValidation)
        {
            Debug.Log($"{gameObject.name} selected");
        }
        
        return true;
    }
    
    /// <summary>
    /// Deselects this object
    /// </summary>
    public virtual void Deselect()
    {
        if (!isSelected)
            return;
        
        isSelected = false;
        OnSelectionChanged(false);
        OnDeselected?.Invoke(this);
        
        if (enableSelectionValidation)
        {
            Debug.Log($"{gameObject.name} deselected");
        }
    }
    
    /// <summary>
    /// Sets the hover state
    /// </summary>
    public virtual void SetHover(bool hover)
    {
        if (isHovered == hover)
            return;
        
        isHovered = hover;
        OnHoverStateChanged(hover);
        OnHoverChanged?.Invoke(this, hover);
    }
    
    /// <summary>
    /// Validates selection based on team restrictions
    /// </summary>
    public virtual bool ValidateSelection(UnitTeam requestingTeam, bool restrictToPlayerTeam)
    {
        if (!enableSelectionValidation || !enableTeamRestrictions)
            return CanBeSelected;
        
        if (!CanBeSelected)
            return false;
        
        if (restrictToPlayerTeam && Team != requestingTeam)
        {
            if (enableSelectionValidation)
            {
                Debug.Log($"Selection denied: {gameObject.name} belongs to {Team}, requesting team is {requestingTeam}");
            }
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Gets display information about this selectable
    /// </summary>
    public virtual string GetDisplayInfo()
    {
        return $"{gameObject.name} ({Team})";
    }
    
    /// <summary>
    /// Called when selection state changes - override for custom behavior
    /// </summary>
    protected virtual void OnSelectionChanged(bool selected)
    {
        // Override in derived classes for custom selection behavior
    }
    
    /// <summary>
    /// Called when hover state changes - override for custom behavior
    /// </summary>
    protected virtual void OnHoverStateChanged(bool hovered)
    {
        // Override in derived classes for custom hover behavior
    }
    
    /// <summary>
    /// Unity lifecycle - setup validation
    /// </summary>
    protected virtual void Start()
    {
        ValidateComponent();
    }
    
    /// <summary>
    /// Validates component setup
    /// </summary>
    protected virtual void ValidateComponent()
    {
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"Selectable object {gameObject.name} should have a Collider for mouse detection");
        }
    }
    
    /// <summary>
    /// Unity lifecycle - cleanup
    /// </summary>
    protected virtual void OnDestroy()
    {
        // Clear event references to prevent memory leaks
        OnSelected = null;
        OnDeselected = null;
        OnHoverChanged = null;
    }
}

/// <summary>
/// Selection validation result structure
/// </summary>
[System.Serializable]
public struct SelectionValidationResult
{
    public bool isValid;
    public string reason;
    public ISelectable target;
    
    public SelectionValidationResult(bool valid, string validationReason, ISelectable selectableTarget)
    {
        isValid = valid;
        reason = validationReason;
        target = selectableTarget;
    }
    
    public static SelectionValidationResult Valid(ISelectable target)
    {
        return new SelectionValidationResult(true, "Selection allowed", target);
    }
    
    public static SelectionValidationResult Invalid(string reason, ISelectable target = null)
    {
        return new SelectionValidationResult(false, reason, target);
    }
}