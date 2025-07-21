using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Centralized selection state management system for the tactical arena.
/// Handles mouse-based unit selection, maintains selection state, and coordinates with other systems.
/// Provides the foundation for tactical gameplay interactions and turn-based mechanics.
/// </summary>
public class SelectionManager : MonoBehaviour
{
    [Header("Selection Configuration")]
    [SerializeField] private bool enableSingleSelection = true;
    [SerializeField] private bool restrictToPlayerTeam = true;
    [SerializeField] private UnitTeam playerTeam = UnitTeam.Blue;
    
    [Header("Interaction Settings")]
    [SerializeField] private bool enableDoubleClickActions = false;
    [SerializeField] private float doubleClickTimeWindow = 0.3f;
    [SerializeField] private bool enableKeyboardSelection = true;
    [SerializeField] private KeyCode nextUnitKey = KeyCode.Tab;
    [SerializeField] private KeyCode clearSelectionKey = KeyCode.Escape;
    
    [Header("Audio Configuration")]
    [SerializeField] private bool enableSelectionAudio = true;
    [SerializeField] private AudioClip selectionSound;
    [SerializeField] private AudioClip deselectionSound;
    [SerializeField] private AudioClip invalidSelectionSound;
    [SerializeField] private float audioVolume = 0.7f;
    
    [Header("Debug and Validation")]
    [SerializeField] private bool enableDebugVisualization = true;
    [SerializeField] private bool enableSelectionLogging = true;
    [SerializeField] private bool validateSelectionOnStart = true;
    
    // Selection state
    private List<ISelectable> selectedObjects = new List<ISelectable>();
    private ISelectable hoveredObject = null;
    private ISelectable lastClickedObject = null;
    private float lastClickTime = 0f;
    
    // System references
    private UnitManager unitManager;
    private AudioSource audioSource;
    private Camera mainCamera;
    
    // Events
    public System.Action<ISelectable> OnObjectSelected;
    public System.Action<ISelectable> OnObjectDeselected;
    public System.Action<List<ISelectable>> OnSelectionChanged;
    public System.Action<ISelectable> OnObjectHovered;
    public System.Action<ISelectable> OnDoubleClicked;
    
    // Properties
    public IReadOnlyList<ISelectable> SelectedObjects => selectedObjects.AsReadOnly();
    public ISelectable CurrentSelection => selectedObjects.FirstOrDefault();
    public ISelectable HoveredObject => hoveredObject;
    public bool HasSelection => selectedObjects.Count > 0;
    public int SelectionCount => selectedObjects.Count;
    public UnitTeam PlayerTeam => playerTeam;
    public bool RestrictToPlayerTeam => restrictToPlayerTeam;
    
    void Awake()
    {
        InitializeSelectionManager();
    }
    
    void Start()
    {
        FindSystemReferences();
        SetupAudioSystem();
        
        if (validateSelectionOnStart)
        {
            ValidateSelectionSystem();
        }
    }
    
    void Update()
    {
        if (enableKeyboardSelection)
        {
            HandleKeyboardInput();
        }
    }
    
    /// <summary>
    /// Initializes the selection manager
    /// </summary>
    private void InitializeSelectionManager()
    {
        if (enableSelectionLogging)
        {
            Debug.Log("SelectionManager initialized");
        }
    }
    
    /// <summary>
    /// Finds references to other systems
    /// </summary>
    private void FindSystemReferences()
    {
        // Find UnitManager
        unitManager = FindFirstObjectByType<UnitManager>();
        if (unitManager == null)
        {
            GameObject unitManagerObj = GameObject.Find("Unit Manager");
            if (unitManagerObj != null)
            {
                unitManager = unitManagerObj.GetComponent<UnitManager>();
            }
        }
        
        // Find main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (enableSelectionLogging)
        {
            Debug.Log($"SelectionManager found references - UnitManager: {unitManager != null}, Camera: {mainCamera != null}");
        }
    }
    
    /// <summary>
    /// Sets up audio system for selection feedback
    /// </summary>
    private void SetupAudioSystem()
    {
        if (!enableSelectionAudio) return;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.playOnAwake = false;
        audioSource.volume = audioVolume;
    }
    
    /// <summary>
    /// Handles keyboard input for selection
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(nextUnitKey))
        {
            SelectNextUnit();
        }
        
        if (Input.GetKeyDown(clearSelectionKey))
        {
            ClearSelection();
        }
    }
    
    /// <summary>
    /// Attempts to select an object
    /// </summary>
    public bool TrySelectObject(ISelectable selectable, Vector3 clickPosition = default)
    {
        if (selectable == null)
        {
            if (enableSelectionLogging)
            {
                Debug.Log("Selection attempt with null object");
            }
            return false;
        }
        
        // Validate selection
        SelectionValidationResult validation = ValidateSelection(selectable);
        if (!validation.isValid)
        {
            if (enableSelectionLogging)
            {
                Debug.Log($"Selection denied: {validation.reason}");
            }
            
            PlaySound(invalidSelectionSound);
            return false;
        }
        
        // Handle double-click detection
        if (enableDoubleClickActions && selectable == lastClickedObject)
        {
            float timeSinceLastClick = Time.time - lastClickTime;
            if (timeSinceLastClick <= doubleClickTimeWindow)
            {
                OnDoubleClicked?.Invoke(selectable);
                if (enableSelectionLogging)
                {
                    Debug.Log($"Double-clicked: {selectable.GetDisplayInfo()}");
                }
            }
        }
        
        lastClickedObject = selectable;
        lastClickTime = Time.time;
        
        // Handle single vs multi-selection
        if (enableSingleSelection)
        {
            // Deselect current selection if different
            if (selectedObjects.Count > 0 && !selectedObjects.Contains(selectable))
            {
                ClearSelection();
            }
        }
        
        // Select the object if not already selected
        if (!selectedObjects.Contains(selectable))
        {
            selectedObjects.Add(selectable);
            selectable.Select();
            
            OnObjectSelected?.Invoke(selectable);
            OnSelectionChanged?.Invoke(selectedObjects);
            
            PlaySound(selectionSound);
            
            if (enableSelectionLogging)
            {
                Debug.Log($"Selected: {selectable.GetDisplayInfo()}");
            }
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Deselects a specific object
    /// </summary>
    public bool DeselectObject(ISelectable selectable)
    {
        if (selectable == null || !selectedObjects.Contains(selectable))
            return false;
        
        selectedObjects.Remove(selectable);
        selectable.Deselect();
        
        OnObjectDeselected?.Invoke(selectable);
        OnSelectionChanged?.Invoke(selectedObjects);
        
        PlaySound(deselectionSound);
        
        if (enableSelectionLogging)
        {
            Debug.Log($"Deselected: {selectable.GetDisplayInfo()}");
        }
        
        return true;
    }
    
    /// <summary>
    /// Clears all current selections
    /// </summary>
    public void ClearSelection()
    {
        if (selectedObjects.Count == 0) return;
        
        List<ISelectable> objectsToDeselect = new List<ISelectable>(selectedObjects);
        selectedObjects.Clear();
        
        foreach (ISelectable selectable in objectsToDeselect)
        {
            selectable.Deselect();
            OnObjectDeselected?.Invoke(selectable);
        }
        
        OnSelectionChanged?.Invoke(selectedObjects);
        
        if (enableSelectionLogging)
        {
            Debug.Log($"Cleared selection of {objectsToDeselect.Count} objects");
        }
    }
    
    /// <summary>
    /// Sets the hovered object
    /// </summary>
    public void SetHoveredObject(ISelectable selectable)
    {
        if (hoveredObject == selectable) return;
        
        // Clear previous hover
        if (hoveredObject != null)
        {
            hoveredObject.SetHover(false);
        }
        
        // Set new hover
        hoveredObject = selectable;
        if (hoveredObject != null)
        {
            hoveredObject.SetHover(true);
            OnObjectHovered?.Invoke(hoveredObject);
        }
    }
    
    /// <summary>
    /// Validates whether an object can be selected
    /// </summary>
    private SelectionValidationResult ValidateSelection(ISelectable selectable)
    {
        if (enableSelectionLogging)
        {
            Debug.Log($"Validating selection: {selectable.GetDisplayInfo()}");
            Debug.Log($"  - CanBeSelected: {selectable.CanBeSelected}");
            Debug.Log($"  - Team: {selectable.Team}");
            Debug.Log($"  - PlayerTeam: {playerTeam}");
            Debug.Log($"  - RestrictToPlayerTeam: {restrictToPlayerTeam}");
        }
        
        if (!selectable.CanBeSelected)
        {
            if (enableSelectionLogging)
            {
                Debug.Log($"Selection DENIED: Object cannot be selected");
            }
            return SelectionValidationResult.Invalid("Object cannot be selected", selectable);
        }
        
        if (restrictToPlayerTeam && !selectable.ValidateSelection(playerTeam, true))
        {
            if (enableSelectionLogging)
            {
                Debug.Log($"Selection DENIED: Team validation failed");
            }
            return SelectionValidationResult.Invalid($"Object belongs to {selectable.Team}, not player team {playerTeam}", selectable);
        }
        
        if (enableSelectionLogging)
        {
            Debug.Log($"Selection ALLOWED: Validation passed");
        }
        return SelectionValidationResult.Valid(selectable);
    }
    
    /// <summary>
    /// Selects the next unit in sequence (for keyboard navigation)
    /// </summary>
    private void SelectNextUnit()
    {
        ISelectable[] allSelectables = FindSelectableUnits();
        if (allSelectables.Length == 0) return;
        
        int currentIndex = -1;
        if (HasSelection)
        {
            currentIndex = System.Array.IndexOf(allSelectables, CurrentSelection);
        }
        
        int nextIndex = (currentIndex + 1) % allSelectables.Length;
        TrySelectObject(allSelectables[nextIndex]);
    }
    
    /// <summary>
    /// Finds all selectable units in the scene
    /// </summary>
    private ISelectable[] FindSelectableUnits()
    {
        List<ISelectable> selectables = new List<ISelectable>();
        
        // Find all objects with ISelectable interface
        ISelectable[] allSelectables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<ISelectable>()
            .Where(s => s.CanBeSelected)
            .ToArray();
        
        // Filter by player team if restricted
        if (restrictToPlayerTeam)
        {
            allSelectables = allSelectables.Where(s => s.Team == playerTeam).ToArray();
        }
        
        return allSelectables;
    }
    
    /// <summary>
    /// Gets the current selection as a specific type
    /// </summary>
    public T GetSelectedObject<T>() where T : class, ISelectable
    {
        return CurrentSelection as T;
    }
    
    /// <summary>
    /// Gets all selected objects of a specific type
    /// </summary>
    public List<T> GetSelectedObjects<T>() where T : class, ISelectable
    {
        return selectedObjects.OfType<T>().ToList();
    }
    
    /// <summary>
    /// Checks if a specific object is selected
    /// </summary>
    public bool IsSelected(ISelectable selectable)
    {
        return selectedObjects.Contains(selectable);
    }
    
    /// <summary>
    /// Gets selection information for debugging
    /// </summary>
    public string GetSelectionInfo()
    {
        if (!HasSelection)
            return "No selection";
        
        if (selectedObjects.Count == 1)
            return $"Selected: {CurrentSelection.GetDisplayInfo()}";
        
        return $"Selected {selectedObjects.Count} objects: {string.Join(", ", selectedObjects.Select(s => s.GetDisplayInfo()))}";
    }
    
    /// <summary>
    /// Plays a selection sound effect
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (enableSelectionAudio && audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// Validates the selection system setup
    /// </summary>
    private void ValidateSelectionSystem()
    {
        bool isValid = true;
        
        if (mainCamera == null)
        {
            Debug.LogError("SelectionManager: No main camera found for raycasting");
            isValid = false;
        }
        
        ISelectable[] selectables = FindSelectableUnits();
        if (selectables.Length == 0)
        {
            Debug.LogWarning("SelectionManager: No selectable objects found in scene");
        }
        
        if (enableSelectionLogging)
        {
            Debug.Log($"SelectionManager validation: {(isValid ? "PASSED" : "FAILED")} - Found {selectables.Length} selectable objects");
        }
    }
    
    /// <summary>
    /// Unity Gizmos for debug visualization
    /// </summary>
    void OnDrawGizmos()
    {
        if (!enableDebugVisualization) return;
        
        // Draw selection indicators
        foreach (ISelectable selected in selectedObjects)
        {
            if (selected?.Transform != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(selected.Transform.position, Vector3.one * 1.2f);
            }
        }
        
        // Draw hover indicator
        if (hoveredObject?.Transform != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(hoveredObject.Transform.position, Vector3.one * 1.1f);
        }
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Clear all event references
        OnObjectSelected = null;
        OnObjectDeselected = null;
        OnSelectionChanged = null;
        OnObjectHovered = null;
        OnDoubleClicked = null;
        
        // Clear selection state
        selectedObjects.Clear();
        hoveredObject = null;
    }
}