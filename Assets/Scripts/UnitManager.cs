using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Centralized unit management system for the tactical arena.
/// Handles unit creation, registration, selection, and coordination between all units.
/// Provides the foundation for tactical gameplay mechanics and turn-based systems.
/// </summary>
public class UnitManager : MonoBehaviour
{
    [Header("Unit Management Configuration")]
    [SerializeField] private bool enableDebugVisualization = false;
    [SerializeField] private bool validateGridPositions = true;
    [SerializeField] private bool allowMultipleSelection = false;
    [SerializeField] private bool enableAutomaticRegistration = true;
    
    [Header("Selection Settings")]
    [SerializeField] private UnitTeam playerTeam = UnitTeam.Blue;
    [SerializeField] private bool restrictSelectionToPlayerTeam = true;
    [SerializeField] private bool enableKeyboardSelection = true;
    [SerializeField] private KeyCode nextUnitKey = KeyCode.Tab;
    [SerializeField] private KeyCode previousUnitKey = KeyCode.LeftShift;
    
    [Header("Turn Management")]
    [SerializeField] private bool enableTurnSystem = false;
    [SerializeField] private UnitTeam currentTurnTeam = UnitTeam.Blue;
    [SerializeField] private float turnTimeLimit = 30f;
    [SerializeField] private bool autoEndTurn = false;
    
    [Header("Unit Limits")]
    [SerializeField] private int maxUnitsPerTeam = 4;
    [SerializeField] private int maxTotalUnits = 8;
    [SerializeField] private bool enforceUnitLimits = true;
    
    [Header("Integration Settings")]
    [SerializeField] private bool integrateWithGrid = true;
    [SerializeField] private bool integrateWithObstacles = true;
    [SerializeField] private bool enableCollisionDetection = true;
    
    // Unit tracking
    private List<Unit> allUnits = new List<Unit>();
    private Dictionary<UnitTeam, List<Unit>> unitsByTeam = new Dictionary<UnitTeam, List<Unit>>();
    private Dictionary<int, Unit> unitsById = new Dictionary<int, Unit>();
    private Dictionary<GridCoordinate, Unit> unitsByPosition = new Dictionary<GridCoordinate, Unit>();
    
    // Selection tracking
    private List<Unit> selectedUnits = new List<Unit>();
    private Unit primarySelectedUnit = null;
    private int selectedUnitIndex = 0;
    
    // System references
    private GridManager gridManager;
    private ObstacleManager obstacleManager;
    private MaterialManager materialManager;
    
    // Turn system tracking
    private float currentTurnStartTime = 0f;
    private bool turnInProgress = false;
    
    // Events for unit management
    public System.Action<Unit> OnUnitRegistered;
    public System.Action<Unit> OnUnitUnregistered;
    public System.Action<Unit> OnUnitSelected;
    public System.Action<Unit> OnUnitDeselected;
    public System.Action OnSelectionCleared;
    public System.Action<UnitTeam> OnTurnStarted;
    public System.Action<UnitTeam> OnTurnEnded;
    public System.Action<UnitTeam> OnTeamEliminated;
    
    // Public properties
    public int TotalUnitCount => allUnits.Count;
    public int SelectedUnitCount => selectedUnits.Count;
    public Unit PrimarySelectedUnit => primarySelectedUnit;
    public List<Unit> SelectedUnits => new List<Unit>(selectedUnits);
    public List<Unit> AllUnits => new List<Unit>(allUnits);
    public UnitTeam CurrentTurnTeam => currentTurnTeam;
    public bool TurnInProgress => turnInProgress;
    public float TurnTimeRemaining => turnTimeLimit - (Time.time - currentTurnStartTime);
    
    void Awake()
    {
        InitializeUnitManager();
    }
    
    void Start()
    {
        FindSystemReferences();
        
        if (enableAutomaticRegistration)
        {
            RegisterExistingUnits();
        }
        
        if (enableTurnSystem)
        {
            StartTurnSystem();
        }
    }
    
    void Update()
    {
        HandleKeyboardInput();
        UpdateTurnSystem();
        ValidateUnitPositions();
    }
    
    /// <summary>
    /// Initializes the unit manager
    /// </summary>
    private void InitializeUnitManager()
    {
        allUnits = new List<Unit>();
        selectedUnits = new List<Unit>();
        unitsByTeam = new Dictionary<UnitTeam, List<Unit>>();
        unitsById = new Dictionary<int, Unit>();
        unitsByPosition = new Dictionary<GridCoordinate, Unit>();
        
        // Initialize team dictionaries immediately
        foreach (UnitTeam team in System.Enum.GetValues(typeof(UnitTeam)))
        {
            unitsByTeam[team] = new List<Unit>();
        }
        
        Debug.Log("UnitManager: System initialized");
    }
    
    /// <summary>
    /// Finds references to other game systems
    /// </summary>
    private void FindSystemReferences()
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
        
        GameObject materialSystem = GameObject.Find("Material Manager");
        if (materialSystem != null)
        {
            materialManager = materialSystem.GetComponent<MaterialManager>();
        }
        
        if (gridManager == null && integrateWithGrid)
        {
            Debug.LogWarning("UnitManager: GridManager not found, grid integration disabled");
            integrateWithGrid = false;
        }
    }
    
    /// <summary>
    /// Registers existing units in the scene
    /// </summary>
    private void RegisterExistingUnits()
    {
        Unit[] existingUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        foreach (Unit unit in existingUnits)
        {
            RegisterUnit(unit);
        }
        
        Debug.Log($"UnitManager: Registered {existingUnits.Length} existing units");
    }
    
    /// <summary>
    /// Registers a unit with the manager
    /// </summary>
    public void RegisterUnit(Unit unit)
    {
        if (unit == null || allUnits.Contains(unit))
            return;
        
        // Check unit limits
        if (enforceUnitLimits)
        {
            if (allUnits.Count >= maxTotalUnits)
            {
                Debug.LogWarning($"Cannot register unit: Total unit limit ({maxTotalUnits}) reached");
                return;
            }
            
            List<Unit> teamUnits = GetUnitsOfTeam(unit.Team);
            if (teamUnits.Count >= maxUnitsPerTeam)
            {
                Debug.LogWarning($"Cannot register unit: Team {unit.Team} limit ({maxUnitsPerTeam}) reached");
                return;
            }
        }
        
        // Add to tracking collections
        allUnits.Add(unit);
        unitsByTeam[unit.Team].Add(unit);
        unitsById[unit.UnitID] = unit;
        
        // Update position tracking
        UpdateUnitPositionTracking(unit);
        
        // Subscribe to unit events
        SubscribeToUnitEvents(unit);
        
        OnUnitRegistered?.Invoke(unit);
        
        Debug.Log($"UnitManager: Registered unit {unit.UnitName} (ID: {unit.UnitID}, Team: {unit.Team})");
    }
    
    /// <summary>
    /// Unregisters a unit from the manager
    /// </summary>
    public void UnregisterUnit(Unit unit)
    {
        if (unit == null || !allUnits.Contains(unit))
            return;
        
        // Remove from tracking collections
        allUnits.Remove(unit);
        unitsByTeam[unit.Team].Remove(unit);
        unitsById.Remove(unit.UnitID);
        unitsByPosition.Remove(unit.GridCoordinate);
        
        // Remove from selection
        if (selectedUnits.Contains(unit))
        {
            DeselectUnit(unit);
        }
        
        // Unsubscribe from unit events
        UnsubscribeFromUnitEvents(unit);
        
        OnUnitUnregistered?.Invoke(unit);
        
        Debug.Log($"UnitManager: Unregistered unit {unit.UnitName}");
        
        // Check for team elimination
        CheckTeamElimination(unit.Team);
    }
    
    /// <summary>
    /// Subscribes to unit events
    /// </summary>
    private void SubscribeToUnitEvents(Unit unit)
    {
        unit.OnUnitSelected += HandleUnitSelected;
        unit.OnUnitDeselected += HandleUnitDeselected;
        unit.OnUnitMoved += HandleUnitMoved;
        unit.OnUnitDied += HandleUnitDied;
    }
    
    /// <summary>
    /// Unsubscribes from unit events
    /// </summary>
    private void UnsubscribeFromUnitEvents(Unit unit)
    {
        unit.OnUnitSelected -= HandleUnitSelected;
        unit.OnUnitDeselected -= HandleUnitDeselected;
        unit.OnUnitMoved -= HandleUnitMoved;
        unit.OnUnitDied -= HandleUnitDied;
    }
    
    /// <summary>
    /// Updates unit position tracking
    /// </summary>
    private void UpdateUnitPositionTracking(Unit unit)
    {
        // Remove old position tracking
        var oldEntry = unitsByPosition.FirstOrDefault(kvp => kvp.Value == unit);
        if (oldEntry.Value != null)
        {
            unitsByPosition.Remove(oldEntry.Key);
        }
        
        // Add new position tracking
        unitsByPosition[unit.GridCoordinate] = unit;
    }
    
    /// <summary>
    /// Handles unit selection
    /// </summary>
    public void HandleUnitSelection(Unit unit)
    {
        if (unit == null || !unit.EnableSelection)
            return;
        
        // Check team restrictions
        if (restrictSelectionToPlayerTeam && unit.Team != playerTeam)
        {
            Debug.Log($"Cannot select unit from team {unit.Team} (player team is {playerTeam})");
            return;
        }
        
        // Check turn restrictions
        if (enableTurnSystem && unit.Team != currentTurnTeam)
        {
            Debug.Log($"Cannot select unit: It's {currentTurnTeam}'s turn");
            return;
        }
        
        if (unit.IsSelected)
        {
            // Deselect if already selected
            DeselectUnit(unit);
        }
        else
        {
            // Select unit
            SelectUnit(unit);
        }
    }
    
    /// <summary>
    /// Selects a unit
    /// </summary>
    public void SelectUnit(Unit unit)
    {
        if (unit == null || selectedUnits.Contains(unit))
            return;
        
        // Clear selection if multiple selection not allowed
        if (!allowMultipleSelection && selectedUnits.Count > 0)
        {
            ClearSelection();
        }
        
        selectedUnits.Add(unit);
        unit.SetSelected(true);
        
        // Set as primary if first selection
        if (primarySelectedUnit == null)
        {
            primarySelectedUnit = unit;
        }
        
        OnUnitSelected?.Invoke(unit);
        
        Debug.Log($"UnitManager: Selected unit {unit.UnitName}");
    }
    
    /// <summary>
    /// Deselects a unit
    /// </summary>
    public void DeselectUnit(Unit unit)
    {
        if (unit == null || !selectedUnits.Contains(unit))
            return;
        
        selectedUnits.Remove(unit);
        unit.SetSelected(false);
        
        // Update primary selection
        if (primarySelectedUnit == unit)
        {
            primarySelectedUnit = selectedUnits.Count > 0 ? selectedUnits[0] : null;
        }
        
        OnUnitDeselected?.Invoke(unit);
        
        Debug.Log($"UnitManager: Deselected unit {unit.UnitName}");
    }
    
    /// <summary>
    /// Clears all unit selection
    /// </summary>
    public void ClearSelection()
    {
        List<Unit> unitsToDeselect = new List<Unit>(selectedUnits);
        
        foreach (Unit unit in unitsToDeselect)
        {
            unit.SetSelected(false);
        }
        
        selectedUnits.Clear();
        primarySelectedUnit = null;
        
        OnSelectionCleared?.Invoke();
        
        Debug.Log("UnitManager: Selection cleared");
    }
    
    /// <summary>
    /// Handles keyboard input for unit selection
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (!enableKeyboardSelection) return;
        
        if (Input.GetKeyDown(nextUnitKey))
        {
            SelectNextUnit();
        }
        else if (Input.GetKeyDown(previousUnitKey))
        {
            SelectPreviousUnit();
        }
    }
    
    /// <summary>
    /// Selects the next unit in sequence
    /// </summary>
    public void SelectNextUnit()
    {
        List<Unit> selectableUnits = GetSelectableUnits();
        
        if (selectableUnits.Count == 0) return;
        
        selectedUnitIndex = (selectedUnitIndex + 1) % selectableUnits.Count;
        
        ClearSelection();
        SelectUnit(selectableUnits[selectedUnitIndex]);
    }
    
    /// <summary>
    /// Selects the previous unit in sequence
    /// </summary>
    public void SelectPreviousUnit()
    {
        List<Unit> selectableUnits = GetSelectableUnits();
        
        if (selectableUnits.Count == 0) return;
        
        selectedUnitIndex = (selectedUnitIndex - 1 + selectableUnits.Count) % selectableUnits.Count;
        
        ClearSelection();
        SelectUnit(selectableUnits[selectedUnitIndex]);
    }
    
    /// <summary>
    /// Gets units that can be selected
    /// </summary>
    private List<Unit> GetSelectableUnits()
    {
        return allUnits.Where(unit => 
            unit.IsAlive && 
            unit.EnableSelection && 
            (!restrictSelectionToPlayerTeam || unit.Team == playerTeam) &&
            (!enableTurnSystem || unit.Team == currentTurnTeam)
        ).ToList();
    }
    
    /// <summary>
    /// Gets all units of a specific team
    /// </summary>
    public List<Unit> GetUnitsOfTeam(UnitTeam team)
    {
        return unitsByTeam.ContainsKey(team) ? new List<Unit>(unitsByTeam[team]) : new List<Unit>();
    }
    
    /// <summary>
    /// Gets living units of a specific team
    /// </summary>
    public List<Unit> GetLivingUnitsOfTeam(UnitTeam team)
    {
        return GetUnitsOfTeam(team).Where(unit => unit.IsAlive).ToList();
    }
    
    /// <summary>
    /// Gets a unit by ID
    /// </summary>
    public Unit GetUnitById(int unitId)
    {
        return unitsById.ContainsKey(unitId) ? unitsById[unitId] : null;
    }
    
    /// <summary>
    /// Gets a unit at a specific grid position
    /// </summary>
    public Unit GetUnitAtPosition(GridCoordinate position)
    {
        return unitsByPosition.ContainsKey(position) ? unitsByPosition[position] : null;
    }
    
    /// <summary>
    /// Checks if a position is occupied by a unit
    /// </summary>
    public bool IsPositionOccupied(GridCoordinate position)
    {
        return unitsByPosition.ContainsKey(position) && unitsByPosition[position].IsAlive;
    }
    
    /// <summary>
    /// Starts the turn system
    /// </summary>
    private void StartTurnSystem()
    {
        currentTurnTeam = UnitTeam.Blue; // Player team goes first
        StartTurn(currentTurnTeam);
    }
    
    /// <summary>
    /// Starts a turn for the specified team
    /// </summary>
    private void StartTurn(UnitTeam team)
    {
        currentTurnTeam = team;
        currentTurnStartTime = Time.time;
        turnInProgress = true;
        
        // Reset units for new turn
        List<Unit> teamUnits = GetUnitsOfTeam(team);
        foreach (Unit unit in teamUnits)
        {
            if (unit.IsAlive)
            {
                unit.ResetForNewTurn();
            }
        }
        
        OnTurnStarted?.Invoke(team);
        
        Debug.Log($"UnitManager: Turn started for {team}");
    }
    
    /// <summary>
    /// Ends the current turn
    /// </summary>
    public void EndTurn()
    {
        if (!turnInProgress) return;
        
        UnitTeam previousTeam = currentTurnTeam;
        turnInProgress = false;
        
        OnTurnEnded?.Invoke(previousTeam);
        
        // Switch to next team
        UnitTeam nextTeam = GetNextTeam(currentTurnTeam);
        StartTurn(nextTeam);
    }
    
    /// <summary>
    /// Gets the next team in turn order
    /// </summary>
    private UnitTeam GetNextTeam(UnitTeam currentTeam)
    {
        switch (currentTeam)
        {
            case UnitTeam.Blue:
                return UnitTeam.Red;
            case UnitTeam.Red:
                return UnitTeam.Blue;
            default:
                return UnitTeam.Blue;
        }
    }
    
    /// <summary>
    /// Updates the turn system
    /// </summary>
    private void UpdateTurnSystem()
    {
        if (!enableTurnSystem || !turnInProgress) return;
        
        // Check for automatic turn end
        if (autoEndTurn)
        {
            bool allUnitsActed = true;
            List<Unit> currentTeamUnits = GetLivingUnitsOfTeam(currentTurnTeam);
            
            foreach (Unit unit in currentTeamUnits)
            {
                if (unit.CanAct || unit.CanMove)
                {
                    allUnitsActed = false;
                    break;
                }
            }
            
            if (allUnitsActed)
            {
                EndTurn();
                return;
            }
        }
        
        // Check for turn time limit
        if (turnTimeLimit > 0 && TurnTimeRemaining <= 0)
        {
            Debug.Log($"Turn time limit exceeded for {currentTurnTeam}");
            EndTurn();
        }
    }
    
    /// <summary>
    /// Validates unit positions
    /// </summary>
    private void ValidateUnitPositions()
    {
        if (!validateGridPositions || gridManager == null) return;
        
        foreach (Unit unit in allUnits)
        {
            if (!gridManager.IsValidCoordinate(unit.GridCoordinate))
            {
                Debug.LogWarning($"Unit {unit.UnitName} at invalid position {unit.GridCoordinate}");
            }
        }
    }
    
    /// <summary>
    /// Handles unit movement events
    /// </summary>
    private void HandleUnitMoved(Unit unit, GridCoordinate from, GridCoordinate to)
    {
        UpdateUnitPositionTracking(unit);
        
        Debug.Log($"UnitManager: Unit {unit.UnitName} moved from {from} to {to}");
    }
    
    /// <summary>
    /// Handles unit selection events
    /// </summary>
    private void HandleUnitSelected(Unit unit)
    {
        // Additional logic for unit selection can be added here
    }
    
    /// <summary>
    /// Handles unit deselection events
    /// </summary>
    private void HandleUnitDeselected(Unit unit)
    {
        // Additional logic for unit deselection can be added here
    }
    
    /// <summary>
    /// Handles unit death events
    /// </summary>
    private void HandleUnitDied(Unit unit)
    {
        // Remove from selection if dead
        if (selectedUnits.Contains(unit))
        {
            DeselectUnit(unit);
        }
        
        // Remove from position tracking
        unitsByPosition.Remove(unit.GridCoordinate);
        
        Debug.Log($"UnitManager: Unit {unit.UnitName} died");
        
        // Check for team elimination
        CheckTeamElimination(unit.Team);
    }
    
    /// <summary>
    /// Checks if a team has been eliminated
    /// </summary>
    private void CheckTeamElimination(UnitTeam team)
    {
        List<Unit> livingUnits = GetLivingUnitsOfTeam(team);
        
        if (livingUnits.Count == 0)
        {
            OnTeamEliminated?.Invoke(team);
            Debug.Log($"UnitManager: Team {team} eliminated!");
        }
    }
    
    /// <summary>
    /// Gets unit manager status information
    /// </summary>
    public UnitManagerStatusInfo GetManagerStatus()
    {
        return new UnitManagerStatusInfo
        {
            totalUnits = allUnits.Count,
            selectedUnits = selectedUnits.Count,
            blueTeamUnits = GetLivingUnitsOfTeam(UnitTeam.Blue).Count,
            redTeamUnits = GetLivingUnitsOfTeam(UnitTeam.Red).Count,
            currentTurnTeam = currentTurnTeam,
            turnInProgress = turnInProgress,
            turnTimeRemaining = TurnTimeRemaining,
            enableTurnSystem = enableTurnSystem,
            enableMultipleSelection = allowMultipleSelection
        };
    }
    
    void OnDrawGizmos()
    {
        if (!enableDebugVisualization) return;
        
        // Draw unit positions
        foreach (Unit unit in allUnits)
        {
            if (unit != null)
            {
                Gizmos.color = unit.IsSelected ? Color.green : TeamUtilities.GetTeamColor(unit.Team);
                Gizmos.DrawWireCube(unit.WorldPosition, Vector3.one);
            }
        }
        
        // Draw selection connections
        if (selectedUnits.Count > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < selectedUnits.Count - 1; i++)
            {
                if (selectedUnits[i] != null && selectedUnits[i + 1] != null)
                {
                    Gizmos.DrawLine(selectedUnits[i].WorldPosition, selectedUnits[i + 1].WorldPosition);
                }
            }
        }
    }
}

/// <summary>
/// Information structure for unit manager status
/// </summary>
[System.Serializable]
public struct UnitManagerStatusInfo
{
    public int totalUnits;
    public int selectedUnits;
    public int blueTeamUnits;
    public int redTeamUnits;
    public UnitTeam currentTurnTeam;
    public bool turnInProgress;
    public float turnTimeRemaining;
    public bool enableTurnSystem;
    public bool enableMultipleSelection;
    
    public override string ToString()
    {
        return $"Units: {totalUnits} (Selected: {selectedUnits}), Teams: Blue {blueTeamUnits} vs Red {redTeamUnits}, Turn: {currentTurnTeam}";
    }
}