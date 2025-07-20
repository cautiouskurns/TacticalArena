using UnityEngine;
using System;

/// <summary>
/// Core unit component that represents an individual tactical unit on the battlefield.
/// Handles unit properties, team assignment, positioning, and state management.
/// Serves as the foundation for all unit-based tactical gameplay mechanics.
/// </summary>
public class Unit : MonoBehaviour
{
    [Header("Unit Identity")]
    [SerializeField] private string unitName = "Tactical Unit";
    [SerializeField] private UnitTeam team = UnitTeam.Blue;
    [SerializeField] private int unitID = -1;
    
    [Header("Grid Positioning")]
    [SerializeField] private GridCoordinate gridCoordinate = new GridCoordinate(0, 0);
    [SerializeField] private Vector3 targetWorldPosition;
    [SerializeField] private bool snapToGrid = true;
    
    [Header("Unit Properties")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private int movementRange = 1;
    [SerializeField] private int actionPoints = 2;
    [SerializeField] private int currentActionPoints = 2;
    
    [Header("Unit State")]
    [SerializeField] private bool isSelected = false;
    [SerializeField] private bool isActive = true;
    [SerializeField] private bool hasMoved = false;
    [SerializeField] private bool hasActed = false;
    
    [Header("Selection and Interaction")]
    [SerializeField] private bool enableSelection = true;
    [SerializeField] private bool enableMouseHover = true;
    [SerializeField] private LayerMask selectionLayerMask = -1;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject selectionIndicator;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private Material hoveredMaterial;
    [SerializeField] private Color teamColor = Color.blue;
    [SerializeField] private bool enableVisualFeedback = true;
    
    [Header("Audio Feedback")]
    [SerializeField] private AudioClip selectionSound;
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip actionSound;
    [SerializeField] private float audioVolume = 0.5f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogging = false;
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool enableGizmosDrawing = true;
    
    // Component references
    private UnitHealth unitHealth;
    private UnitManager unitManager;
    private GridManager gridManager;
    private Renderer unitRenderer;
    private Collider unitCollider;
    private AudioSource audioSource;
    
    // State tracking
    private bool isAlive = true;
    private bool isInitialized = false;
    private Material originalMaterial;
    private Vector3 originalScale;
    
    // Movement and animation
    private bool isMoving = false;
    private Vector3 movementStartPosition;
    private Vector3 movementTargetPosition;
    private float movementProgress = 0f;
    
    // Selection state
    private bool wasSelectedLastFrame = false;
    private bool isHovered = false;
    
    // Events for unit state changes
    public System.Action<Unit> OnUnitSelected;
    public System.Action<Unit> OnUnitDeselected;
    public System.Action<Unit, GridCoordinate, GridCoordinate> OnUnitMoved;
    public System.Action<Unit> OnUnitActed;
    public System.Action<Unit> OnUnitDied;
    public System.Action<Unit> OnUnitRevived;
    public System.Action<Unit, bool> OnUnitActiveStateChanged;
    
    // Public properties
    public string UnitName => unitName;
    public UnitTeam Team => team;
    public int UnitID => unitID;
    public GridCoordinate GridCoordinate => gridCoordinate;
    public Vector3 WorldPosition => transform.position;
    public bool IsSelected => isSelected;
    public bool IsActive => isActive;
    public bool IsAlive => isAlive;
    public bool HasMoved => hasMoved;
    public bool HasActed => hasActed;
    public bool CanAct => isAlive && isActive && currentActionPoints > 0;
    public bool CanMove => isAlive && isActive && !hasMoved && currentActionPoints > 0;
    public bool IsMoving => isMoving;
    public float MoveSpeed => moveSpeed;
    public int MovementRange => movementRange;
    public int ActionPoints => actionPoints;
    public int CurrentActionPoints => currentActionPoints;
    public bool EnableSelection => enableSelection;
    public UnitHealth Health => unitHealth;
    
    void Awake()
    {
        InitializeUnit();
    }
    
    void Start()
    {
        FindComponentReferences();
        RegisterWithSystems();
        SetupInitialState();
        ValidateUnitSetup();
    }
    
    void Update()
    {
        HandleMovementAnimation();
        UpdateSelectionState();
        UpdateVisualFeedback();
    }
    
    void OnMouseEnter()
    {
        if (enableMouseHover && enableSelection)
        {
            HandleMouseHover(true);
        }
    }
    
    void OnMouseExit()
    {
        if (enableMouseHover && enableSelection)
        {
            HandleMouseHover(false);
        }
    }
    
    void OnMouseDown()
    {
        if (enableSelection && isAlive)
        {
            HandleMouseClick();
        }
    }
    
    /// <summary>
    /// Initializes the unit component
    /// </summary>
    private void InitializeUnit()
    {
        // Generate unique ID if not set
        if (unitID < 0)
        {
            unitID = GetInstanceID();
        }
        
        // Set default unit name if empty
        if (string.IsNullOrEmpty(unitName))
        {
            unitName = $"{team} Unit {unitID}";
        }
        
        // Initialize action points
        currentActionPoints = actionPoints;
        
        // Store original scale
        originalScale = transform.localScale;
        
        if (enableDebugLogging)
        {
            Debug.Log($"Unit {unitName} initialized with ID {unitID}");
        }
    }
    
    /// <summary>
    /// Finds references to required components
    /// </summary>
    private void FindComponentReferences()
    {
        unitHealth = GetComponent<UnitHealth>();
        unitRenderer = GetComponent<Renderer>();
        unitCollider = GetComponent<Collider>();
        
        // Store original material
        if (unitRenderer != null)
        {
            originalMaterial = unitRenderer.material;
        }
        
        // Find manager references
        GameObject unitManagerObj = GameObject.Find("Unit Manager");
        if (unitManagerObj != null)
        {
            unitManager = unitManagerObj.GetComponent<UnitManager>();
        }
        
        GameObject gridSystemObj = GameObject.Find("Grid System");
        if (gridSystemObj != null)
        {
            gridManager = gridSystemObj.GetComponent<GridManager>();
        }
        
        // Setup audio
        SetupAudioSystem();
    }
    
    /// <summary>
    /// Sets up audio system
    /// </summary>
    private void SetupAudioSystem()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (selectionSound != null || moveSound != null || actionSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = audioVolume;
            audioSource.playOnAwake = false;
        }
    }
    
    /// <summary>
    /// Registers with various game systems
    /// </summary>
    private void RegisterWithSystems()
    {
        // Register with unit manager
        if (unitManager != null)
        {
            unitManager.RegisterUnit(this);
        }
        
        // Register with health system
        if (unitHealth != null)
        {
            unitHealth.OnUnitDeath += HandleDeath;
            unitHealth.OnUnitRevived += HandleRevival;
        }
        
        // Register with grid system
        if (gridManager != null && snapToGrid)
        {
            SnapToGridPosition();
        }
    }
    
    /// <summary>
    /// Sets up initial unit state
    /// </summary>
    private void SetupInitialState()
    {
        // Apply team color
        ApplyTeamColor();
        
        // Set initial position
        if (snapToGrid)
        {
            targetWorldPosition = CalculateWorldPosition();
            transform.position = targetWorldPosition;
        }
        
        // Mark as initialized
        isInitialized = true;
        
        if (enableDebugLogging)
        {
            Debug.Log($"Unit {unitName} setup complete at position {gridCoordinate}");
        }
    }
    
    /// <summary>
    /// Validates unit setup
    /// </summary>
    private void ValidateUnitSetup()
    {
        bool isValid = true;
        
        if (unitHealth == null)
        {
            Debug.LogWarning($"Unit {unitName}: UnitHealth component missing");
            isValid = false;
        }
        
        if (unitRenderer == null)
        {
            Debug.LogWarning($"Unit {unitName}: Renderer component missing");
            isValid = false;
        }
        
        if (unitCollider == null)
        {
            Debug.LogWarning($"Unit {unitName}: Collider component missing for selection");
            isValid = false;
        }
        
        if (gridManager != null && !gridManager.IsValidCoordinate(gridCoordinate))
        {
            Debug.LogError($"Unit {unitName}: Invalid grid coordinate {gridCoordinate}");
            isValid = false;
        }
        
        if (isValid && enableDebugLogging)
        {
            Debug.Log($"Unit {unitName}: Validation passed");
        }
    }
    
    /// <summary>
    /// Calculates world position from grid coordinate
    /// </summary>
    private Vector3 CalculateWorldPosition()
    {
        if (gridManager != null)
        {
            return gridManager.GridToWorld(gridCoordinate);
        }
        
        // Fallback calculation
        return new Vector3(gridCoordinate.x, transform.position.y, gridCoordinate.z);
    }
    
    /// <summary>
    /// Snaps unit to grid position
    /// </summary>
    private void SnapToGridPosition()
    {
        Vector3 worldPos = CalculateWorldPosition();
        worldPos.y = transform.position.y; // Preserve Y position
        transform.position = worldPos;
        targetWorldPosition = worldPos;
    }
    
    /// <summary>
    /// Applies team color to unit
    /// </summary>
    private void ApplyTeamColor()
    {
        if (unitRenderer != null)
        {
            TeamData teamData = TeamData.GetDefault(team);
            teamColor = teamData.teamColor;
            
            if (originalMaterial != null)
            {
                Material teamMaterial = new Material(originalMaterial);
                teamMaterial.color = teamColor;
                unitRenderer.material = teamMaterial;
                originalMaterial = teamMaterial;
            }
        }
    }
    
    /// <summary>
    /// Handles mouse hover events
    /// </summary>
    private void HandleMouseHover(bool isHovering)
    {
        isHovered = isHovering;
        
        if (enableDebugLogging)
        {
            Debug.Log($"Unit {unitName} hover: {isHovering}");
        }
    }
    
    /// <summary>
    /// Handles mouse click events
    /// </summary>
    private void HandleMouseClick()
    {
        if (unitManager != null)
        {
            // Let unit manager handle selection logic
            unitManager.HandleUnitSelection(this);
        }
        else
        {
            // Fallback: toggle selection directly
            SetSelected(!isSelected);
        }
        
        // Play selection sound
        PlaySound(selectionSound);
        
        if (enableDebugLogging)
        {
            Debug.Log($"Unit {unitName} clicked, selected: {isSelected}");
        }
    }
    
    /// <summary>
    /// Sets the unit's team
    /// </summary>
    public void SetTeam(UnitTeam newTeam)
    {
        team = newTeam;
        ApplyTeamColor();
        
        if (enableDebugLogging)
        {
            Debug.Log($"Unit {unitName} team set to {team}");
        }
    }
    
    /// <summary>
    /// Sets the unit's grid coordinate
    /// </summary>
    public void SetGridCoordinate(GridCoordinate newCoordinate)
    {
        if (gridManager != null && !gridManager.IsValidCoordinate(newCoordinate))
        {
            Debug.LogError($"Invalid grid coordinate: {newCoordinate}");
            return;
        }
        
        GridCoordinate oldCoordinate = gridCoordinate;
        gridCoordinate = newCoordinate;
        
        if (snapToGrid)
        {
            targetWorldPosition = CalculateWorldPosition();
            
            if (!isMoving)
            {
                transform.position = targetWorldPosition;
            }
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"Unit {unitName} coordinate changed: {oldCoordinate} -> {newCoordinate}");
        }
    }
    
    /// <summary>
    /// Moves unit to target grid coordinate
    /// </summary>
    public bool MoveTo(GridCoordinate targetCoordinate)
    {
        if (!CanMove)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"Unit {unitName} cannot move: CanMove = {CanMove}");
            }
            return false;
        }
        
        if (gridManager != null && !gridManager.IsValidCoordinate(targetCoordinate))
        {
            Debug.LogError($"Invalid target coordinate: {targetCoordinate}");
            return false;
        }
        
        // Check if target is within movement range
        float distance = gridCoordinate.DistanceTo(targetCoordinate);
        if (distance > movementRange)
        {
            Debug.LogWarning($"Target {targetCoordinate} is out of movement range ({distance} > {movementRange})");
            return false;
        }
        
        // Check if target is occupied
        if (gridManager != null && gridManager.IsCoordinateOccupied(targetCoordinate))
        {
            Debug.LogWarning($"Target coordinate {targetCoordinate} is occupied");
            return false;
        }
        
        // Start movement
        GridCoordinate oldCoordinate = gridCoordinate;
        gridCoordinate = targetCoordinate;
        targetWorldPosition = CalculateWorldPosition();
        
        StartMovementAnimation();
        
        // Update state
        hasMoved = true;
        ConsumeActionPoint();
        
        // Trigger events
        OnUnitMoved?.Invoke(this, oldCoordinate, targetCoordinate);
        
        // Play move sound
        PlaySound(moveSound);
        
        if (enableDebugLogging)
        {
            Debug.Log($"Unit {unitName} moving from {oldCoordinate} to {targetCoordinate}");
        }
        
        return true;
    }
    
    /// <summary>
    /// Starts movement animation
    /// </summary>
    private void StartMovementAnimation()
    {
        movementStartPosition = transform.position;
        movementTargetPosition = targetWorldPosition;
        movementProgress = 0f;
        isMoving = true;
    }
    
    /// <summary>
    /// Handles movement animation update
    /// </summary>
    private void HandleMovementAnimation()
    {
        if (!isMoving) return;
        
        movementProgress += Time.deltaTime * moveSpeed;
        
        if (movementProgress >= 1f)
        {
            // Movement complete
            movementProgress = 1f;
            transform.position = movementTargetPosition;
            isMoving = false;
            
            // Update grid system
            if (gridManager != null)
            {
                GridTile tile = gridManager.GetTile(gridCoordinate);
                if (tile != null)
                {
                    tile.OccupyTile(gameObject);
                }
            }
        }
        else
        {
            // Interpolate position
            transform.position = Vector3.Lerp(movementStartPosition, movementTargetPosition, movementProgress);
        }
    }
    
    /// <summary>
    /// Performs an action (consumes action points)
    /// </summary>
    public bool PerformAction()
    {
        if (!CanAct)
        {
            return false;
        }
        
        hasActed = true;
        ConsumeActionPoint();
        
        OnUnitActed?.Invoke(this);
        PlaySound(actionSound);
        
        if (enableDebugLogging)
        {
            Debug.Log($"Unit {unitName} performed action. AP remaining: {currentActionPoints}");
        }
        
        return true;
    }
    
    /// <summary>
    /// Consumes an action point
    /// </summary>
    private void ConsumeActionPoint()
    {
        currentActionPoints = Mathf.Max(0, currentActionPoints - 1);
    }
    
    /// <summary>
    /// Resets unit for new turn
    /// </summary>
    public void ResetForNewTurn()
    {
        currentActionPoints = actionPoints;
        hasMoved = false;
        hasActed = false;
        
        if (enableDebugLogging)
        {
            Debug.Log($"Unit {unitName} reset for new turn");
        }
    }
    
    /// <summary>
    /// Sets unit selection state
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;
        
        isSelected = selected;
        
        if (selected)
        {
            OnUnitSelected?.Invoke(this);
        }
        else
        {
            OnUnitDeselected?.Invoke(this);
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"Unit {unitName} selection: {selected}");
        }
    }
    
    /// <summary>
    /// Sets unit active state
    /// </summary>
    public void SetActive(bool active)
    {
        if (isActive == active) return;
        
        isActive = active;
        OnUnitActiveStateChanged?.Invoke(this, active);
        
        // Update visual state
        if (unitRenderer != null)
        {
            unitRenderer.enabled = active;
        }
        
        if (unitCollider != null)
        {
            unitCollider.enabled = active;
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"Unit {unitName} active state: {active}");
        }
    }
    
    /// <summary>
    /// Handles unit death
    /// </summary>
    public void HandleDeath()
    {
        isAlive = false;
        isSelected = false;
        
        OnUnitDied?.Invoke(this);
        
        // Visual feedback for death
        if (unitRenderer != null)
        {
            Color deathColor = Color.gray;
            unitRenderer.material.color = deathColor;
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"Unit {unitName} died");
        }
    }
    
    /// <summary>
    /// Handles unit revival
    /// </summary>
    public void HandleRevival()
    {
        isAlive = true;
        
        OnUnitRevived?.Invoke(this);
        
        // Restore team color
        ApplyTeamColor();
        
        if (enableDebugLogging)
        {
            Debug.Log($"Unit {unitName} revived");
        }
    }
    
    /// <summary>
    /// Updates selection state visualization
    /// </summary>
    private void UpdateSelectionState()
    {
        if (wasSelectedLastFrame != isSelected)
        {
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(isSelected);
            }
            
            wasSelectedLastFrame = isSelected;
        }
    }
    
    /// <summary>
    /// Updates visual feedback
    /// </summary>
    private void UpdateVisualFeedback()
    {
        if (!enableVisualFeedback || unitRenderer == null) return;
        
        // Handle material changes based on state
        Material targetMaterial = originalMaterial;
        
        if (isSelected && selectedMaterial != null)
        {
            targetMaterial = selectedMaterial;
        }
        else if (isHovered && hoveredMaterial != null)
        {
            targetMaterial = hoveredMaterial;
        }
        
        if (unitRenderer.material != targetMaterial)
        {
            unitRenderer.material = targetMaterial;
        }
        
        // Handle scale changes for feedback
        Vector3 targetScale = originalScale;
        
        if (isSelected)
        {
            targetScale *= 1.1f; // Slightly larger when selected
        }
        else if (isHovered)
        {
            targetScale *= 1.05f; // Slightly larger when hovered
        }
        
        if (transform.localScale != targetScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 5f);
        }
    }
    
    /// <summary>
    /// Plays a sound effect
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, audioVolume);
        }
    }
    
    /// <summary>
    /// Gets unit status information
    /// </summary>
    public UnitStatusInfo GetUnitStatus()
    {
        return new UnitStatusInfo
        {
            unitName = unitName,
            unitID = unitID,
            team = team,
            gridCoordinate = gridCoordinate,
            worldPosition = transform.position,
            isSelected = isSelected,
            isActive = isActive,
            isAlive = isAlive,
            hasMoved = hasMoved,
            hasActed = hasActed,
            isMoving = isMoving,
            moveSpeed = moveSpeed,
            movementRange = movementRange,
            actionPoints = actionPoints,
            currentActionPoints = currentActionPoints,
            canAct = CanAct,
            canMove = CanMove,
            healthStatus = unitHealth != null ? unitHealth.GetHealthStatus() : new HealthStatusInfo()
        };
    }
    
    void OnDrawGizmos()
    {
        if (!enableGizmosDrawing) return;
        
        // Draw unit position
        Gizmos.color = teamColor;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);
        
        // Draw grid coordinate
        if (Application.isPlaying && gridManager != null)
        {
            Vector3 gridWorldPos = gridManager.GridToWorld(gridCoordinate);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(gridWorldPos, Vector3.one * 0.1f);
            
            // Draw line to grid position if different
            if (Vector3.Distance(transform.position, gridWorldPos) > 0.1f)
            {
                Gizmos.DrawLine(transform.position, gridWorldPos);
            }
        }
        
        // Draw movement range
        if (isSelected && Application.isPlaying)
        {
            Gizmos.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0.3f);
            
            for (int x = -movementRange; x <= movementRange; x++)
            {
                for (int z = -movementRange; z <= movementRange; z++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(z) <= movementRange)
                    {
                        GridCoordinate targetCoord = new GridCoordinate(gridCoordinate.x + x, gridCoordinate.z + z);
                        if (gridManager != null && gridManager.IsValidCoordinate(targetCoord))
                        {
                            Vector3 worldPos = gridManager.GridToWorld(targetCoord);
                            Gizmos.DrawCube(worldPos, Vector3.one * 0.8f);
                        }
                    }
                }
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        // Draw detailed debug information
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // Draw target position if moving
        if (isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(targetWorldPosition, Vector3.one * 0.5f);
            Gizmos.DrawLine(transform.position, targetWorldPosition);
        }
    }
}

/// <summary>
/// Information structure for unit status
/// </summary>
[System.Serializable]
public struct UnitStatusInfo
{
    public string unitName;
    public int unitID;
    public UnitTeam team;
    public GridCoordinate gridCoordinate;
    public Vector3 worldPosition;
    public bool isSelected;
    public bool isActive;
    public bool isAlive;
    public bool hasMoved;
    public bool hasActed;
    public bool isMoving;
    public float moveSpeed;
    public int movementRange;
    public int actionPoints;
    public int currentActionPoints;
    public bool canAct;
    public bool canMove;
    public HealthStatusInfo healthStatus;
    
    public override string ToString()
    {
        return $"{unitName} ({team}) at {gridCoordinate} - AP: {currentActionPoints}/{actionPoints}, {(isAlive ? "Alive" : "Dead")}";
    }
}