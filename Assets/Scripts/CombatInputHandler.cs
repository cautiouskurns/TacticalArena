using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Combat input handler that integrates attack functionality with the existing selection system.
/// Manages combat-specific input for target selection and attack execution.
/// Works in conjunction with SelectionManager to provide seamless combat interaction.
/// </summary>
public class CombatInputHandler : MonoBehaviour
{
    [Header("Combat Input Configuration")]
    [SerializeField] private bool enableCombatInput = true;
    [SerializeField] private CombatInputMode combatInputMode = CombatInputMode.ClickToAttack;
    [SerializeField] private KeyCode attackModeKey = KeyCode.A;
    [SerializeField] private KeyCode cancelKey = KeyCode.Escape;
    
    [Header("Input Timing")]
    [SerializeField] private float doubleClickTime = 0.3f;
    [SerializeField] private float inputCooldown = 0.1f;
    [SerializeField] private bool preventSpamClicking = true;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool showCombatCursor = true;
    [SerializeField] private Texture2D attackCursor;
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableInputLogging = true;
    [SerializeField] private bool enableDetailedRaycastLogging = true;
    
    // System references
    private SelectionManager selectionManager;
    private CombatManager combatManager;
    private TargetingSystem targetingSystem;
    private Camera mainCamera;
    
    // Input state
    private bool combatModeActive = false;
    private float lastInputTime = 0f;
    private float lastClickTime = 0f;
    private int clickCount = 0;
    private IAttacker selectedAttacker;
    
    // Input mode options
    public enum CombatInputMode
    {
        ClickToAttack,      // Click on enemy to attack
        AttackModeToggle,   // Press key to enter attack mode, then click
        DoubleClickAttack,  // Double-click to attack
        RightClickAttack    // Right-click to attack
    }
    
    // Events
    public System.Action<IAttacker> OnCombatModeActivated;
    public System.Action<IAttacker> OnCombatModeDeactivated;
    public System.Action<IAttacker, IAttackable> OnAttackInputReceived;
    
    // Properties
    public bool CombatModeActive => combatModeActive;
    public IAttacker SelectedAttacker => selectedAttacker;
    public CombatInputMode InputMode => combatInputMode;
    
    void Awake()
    {
        InitializeInputHandler();
    }
    
    void Start()
    {
        Debug.Log("CombatInputHandler: Starting - finding system references...");
        FindSystemReferences();
        SetupEventListeners();
        Debug.Log($"CombatInputHandler: Start complete - Input mode: {combatInputMode}, Combat enabled: {enableCombatInput}");
    }
    
    void Update()
    {
        if (enableCombatInput)
        {
            HandleCombatInput();
            UpdateInputTimers();
        }
    }
    
    /// <summary>
    /// Initializes the combat input handler
    /// </summary>
    private void InitializeInputHandler()
    {
        if (enableInputLogging)
        {
            Debug.Log("CombatInputHandler initialized - Combat input system ready");
        }
    }
    
    /// <summary>
    /// Finds references to other systems
    /// </summary>
    private void FindSystemReferences()
    {
        selectionManager = GetComponent<SelectionManager>();
        if (selectionManager == null)
        {
            Debug.LogError("CombatInputHandler: SelectionManager not found on same GameObject!");
            // Try finding it in the scene
            selectionManager = FindFirstObjectByType<SelectionManager>();
            if (selectionManager != null)
            {
                Debug.LogWarning("CombatInputHandler: Found SelectionManager in scene but not on same GameObject - this may cause issues!");
            }
        }
        else
        {
            Debug.Log("CombatInputHandler: SelectionManager found on same GameObject");
        }
        
        combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager == null)
        {
            Debug.LogError("CombatInputHandler: CombatManager not found!");
        }
        
        targetingSystem = combatManager?.GetComponent<TargetingSystem>();
        if (targetingSystem == null)
        {
            Debug.LogError("CombatInputHandler: TargetingSystem not found!");
        }
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (enableInputLogging)
        {
            Debug.Log($"CombatInputHandler found references - Selection: {selectionManager != null}, " +
                     $"Combat: {combatManager != null}, Targeting: {targetingSystem != null}, Camera: {mainCamera != null}");
        }
    }
    
    /// <summary>
    /// Sets up event listeners for integration with other systems
    /// </summary>
    private void SetupEventListeners()
    {
        Debug.Log("CombatInputHandler: Setting up event listeners...");
        
        if (selectionManager != null)
        {
            selectionManager.OnObjectSelected += OnUnitSelected;
            selectionManager.OnObjectDeselected += OnUnitDeselected;
            selectionManager.OnSelectionChanged += OnSelectionChanged;
            Debug.Log("CombatInputHandler: Subscribed to SelectionManager events");
        }
        else
        {
            Debug.LogError("CombatInputHandler: Cannot setup event listeners - SelectionManager is null!");
        }
        
        if (combatManager != null)
        {
            combatManager.OnCombatStateChanged += OnCombatStateChanged;
            Debug.Log("CombatInputHandler: Subscribed to CombatManager events");
        }
        
        if (targetingSystem != null)
        {
            targetingSystem.OnTargetingStarted += OnTargetingStarted;
            targetingSystem.OnTargetingStopped += OnTargetingStopped;
            Debug.Log("CombatInputHandler: Subscribed to TargetingSystem events");
        }
    }
    
    /// <summary>
    /// Updates input timers
    /// </summary>
    private void UpdateInputTimers()
    {
        // Handle double-click timing
        if (Time.time - lastClickTime > doubleClickTime)
        {
            clickCount = 0;
        }
    }
    
    /// <summary>
    /// Main combat input handling
    /// </summary>
    private void HandleCombatInput()
    {
        // Check input cooldown to prevent spam
        if (preventSpamClicking && Time.time - lastInputTime < inputCooldown)
        {
            return;
        }
        
        // Handle mode-specific input
        switch (combatInputMode)
        {
            case CombatInputMode.ClickToAttack:
                HandleClickToAttackInput();
                break;
            case CombatInputMode.AttackModeToggle:
                HandleAttackModeToggleInput();
                break;
            case CombatInputMode.DoubleClickAttack:
                HandleDoubleClickAttackInput();
                break;
            case CombatInputMode.RightClickAttack:
                HandleRightClickAttackInput();
                break;
        }
        
        // Handle cancel input
        if (Input.GetKeyDown(cancelKey))
        {
            CancelCombatMode();
        }
    }

    /// <summary>
    /// Handles click-to-attack input mode
    /// </summary>
    private void HandleClickToAttackInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"CombatInputHandler: Mouse clicked - selectedAttacker={selectedAttacker != null}");

            if (selectedAttacker != null)
            {
                Vector3 mousePosition = Input.mousePosition;
                Ray ray = mainCamera.ScreenPointToRay(mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (enableDetailedRaycastLogging)
                    {
                        Debug.Log($"CombatInputHandler: Raycast hit {hit.collider.gameObject.name} at {hit.point}");
                    }

                    IAttackable target = hit.collider.GetComponent<IAttackable>();
                    if (target != null)
                    {
                        if (enableDetailedRaycastLogging)
                        {
                            Debug.Log($"CombatInputHandler: Found IAttackable target - {target.GetDisplayInfo()}");
                        }

                        // Check if this is a valid attack target
                        AttackValidationResult validation = combatManager.GetComponent<AttackValidator>()?.ValidateAttack(selectedAttacker, target);
                        if (validation != null && validation.isValid)
                        {
                            ExecuteAttack(selectedAttacker, target);
                            lastInputTime = Time.time;
                        }
                        else
                        {
                            if (enableInputLogging)
                            {
                                Debug.Log($"CombatInputHandler: Invalid attack target - {validation?.failureReason ?? "Unknown reason"}");
                            }
                        }
                    }
                    else
                    {
                        if (enableDetailedRaycastLogging)
                        {
                            Debug.Log($"CombatInputHandler: Hit object {hit.collider.gameObject.name} has no IAttackable component");
                        }
                    }
                }
                else
                {
                    if (enableDetailedRaycastLogging)
                    {
                        Debug.Log("CombatInputHandler: Raycast hit nothing");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Handles attack mode toggle input
    /// </summary>
    private void HandleAttackModeToggleInput()
    {
        if (Input.GetKeyDown(attackModeKey))
        {
            if (!combatModeActive && selectedAttacker != null)
            {
                ActivateCombatMode(selectedAttacker);
            }
            else
            {
                DeactivateCombatMode();
            }
            lastInputTime = Time.time;
        }
        
        // Handle target selection in attack mode
        if (combatModeActive && Input.GetMouseButtonDown(0))
        {
            HandleTargetSelection();
        }
    }
    
    /// <summary>
    /// Handles double-click attack input
    /// </summary>
    private void HandleDoubleClickAttackInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            clickCount++;
            float timeSinceLastClick = Time.time - lastClickTime;
            lastClickTime = Time.time;
            
            if (clickCount == 2 && timeSinceLastClick <= doubleClickTime && selectedAttacker != null)
            {
                Vector3 mousePosition = Input.mousePosition;
                Ray ray = mainCamera.ScreenPointToRay(mousePosition);
                
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    IAttackable target = hit.collider.GetComponent<IAttackable>();
                    if (target != null)
                    {
                        ExecuteAttack(selectedAttacker, target);
                        clickCount = 0;
                        lastInputTime = Time.time;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Handles right-click attack input
    /// </summary>
    private void HandleRightClickAttackInput()
    {
        if (Input.GetMouseButtonDown(1) && selectedAttacker != null)
        {
            Vector3 mousePosition = Input.mousePosition;
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                IAttackable target = hit.collider.GetComponent<IAttackable>();
                if (target != null)
                {
                    ExecuteAttack(selectedAttacker, target);
                    lastInputTime = Time.time;
                }
            }
        }
    }
    
    /// <summary>
    /// Handles target selection in attack mode
    /// </summary>
    private void HandleTargetSelection()
    {
        if (mainCamera == null) return;
        
        Vector3 mousePosition = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            IAttackable target = hit.collider.GetComponent<IAttackable>();
            if (target != null && selectedAttacker != null)
            {
                ExecuteAttack(selectedAttacker, target);
            }
        }
        
        // Deactivate combat mode after target selection
        DeactivateCombatMode();
        lastInputTime = Time.time;
    }
    
    /// <summary>
    /// Executes an attack from attacker to target
    /// </summary>
    private void ExecuteAttack(IAttacker attacker, IAttackable target)
    {
        if (combatManager == null)
        {
            Debug.LogError("CombatInputHandler: CombatManager is null - cannot execute attack!");
            return;
        }
        
        Debug.Log($"CombatInputHandler: Executing attack from {attacker.GetDisplayInfo()} to {target.GetDisplayInfo()}");
        
        OnAttackInputReceived?.Invoke(attacker, target);
        
        AttackResult result = combatManager.RequestAttack(attacker, target);
        
        Debug.Log($"CombatInputHandler: Attack requested - {result.success}: {result.message}");
    }
    
    /// <summary>
    /// Activates combat mode for the specified attacker
    /// </summary>
    private void ActivateCombatMode(IAttacker attacker)
    {
        combatModeActive = true;
        selectedAttacker = attacker;
        
        // Start targeting system
        if (targetingSystem != null)
        {
            targetingSystem.StartTargeting(attacker);
        }
        
        // Update cursor if enabled
        if (showCombatCursor && attackCursor != null)
        {
            Cursor.SetCursor(attackCursor, cursorHotspot, CursorMode.Auto);
        }
        
        OnCombatModeActivated?.Invoke(attacker);
        
        if (enableInputLogging)
        {
            Debug.Log($"CombatInputHandler: Combat mode activated for {attacker.GetDisplayInfo()}");
        }
    }
    
    /// <summary>
    /// Deactivates combat mode
    /// </summary>
    private void DeactivateCombatMode()
    {
        if (!combatModeActive) return;
        
        IAttacker previousAttacker = selectedAttacker;
        combatModeActive = false;
        
        // Stop targeting system
        if (targetingSystem != null)
        {
            targetingSystem.StopTargeting();
        }
        
        // Restore default cursor
        if (showCombatCursor)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        
        OnCombatModeDeactivated?.Invoke(previousAttacker);
        
        if (enableInputLogging)
        {
            Debug.Log("CombatInputHandler: Combat mode deactivated");
        }
    }
    
    /// <summary>
    /// Cancels combat mode
    /// </summary>
    private void CancelCombatMode()
    {
        DeactivateCombatMode();
        
        if (enableInputLogging)
        {
            Debug.Log("CombatInputHandler: Combat mode canceled");
        }
    }
    
    /// <summary>
    /// Called when a unit is selected
    /// </summary>
    private void OnUnitSelected(ISelectable selectable)
    {
        Debug.Log($"CombatInputHandler: OnUnitSelected called with {selectable?.GetDisplayInfo() ?? "null"}");
        
        // First check if the selectable itself is an attacker
        if (selectable is IAttacker attacker)
        {
            selectedAttacker = attacker;
            Debug.Log($"CombatInputHandler: Unit is an attacker - setting selectedAttacker to {attacker.GetDisplayInfo()}");
        }
        else
        {
            // If not, check if it's a MonoBehaviour and has an AttackCapability component
            MonoBehaviour selectableMono = selectable as MonoBehaviour;
            if (selectableMono != null)
            {
                AttackCapability attackCap = selectableMono.GetComponent<AttackCapability>();
                if (attackCap != null)
                {
                    selectedAttacker = attackCap;
                    Debug.Log($"CombatInputHandler: Found AttackCapability on selected unit - setting selectedAttacker to {attackCap.GetDisplayInfo()}");
                    attacker = attackCap;
                }
                else
                {
                    Debug.Log($"CombatInputHandler: Selected unit has no AttackCapability - clearing selectedAttacker");
                    selectedAttacker = null;
                    DeactivateCombatMode();
                    return;
                }
            }
            else
            {
                Debug.Log($"CombatInputHandler: Selected object is not a MonoBehaviour - clearing selectedAttacker");
                selectedAttacker = null;
                DeactivateCombatMode();
                return;
            }
        }
        
        // Auto-activate combat mode based on input mode
        if (attacker != null && (combatInputMode == CombatInputMode.ClickToAttack || combatInputMode == CombatInputMode.RightClickAttack || combatInputMode == CombatInputMode.DoubleClickAttack))
        {
            ActivateCombatMode(attacker);
            
            if (enableInputLogging)
            {
                Debug.Log($"CombatInputHandler: Attacker selected - {attacker.GetDisplayInfo()}");
            }
        }
    }
    
    /// <summary>
    /// Called when a unit is deselected
    /// </summary>
    private void OnUnitDeselected(ISelectable selectable)
    {
        if (selectable is IAttacker)
        {
            selectedAttacker = null;
            DeactivateCombatMode();
        }
    }
    
    /// <summary>
    /// Called when selection changes
    /// </summary>
    private void OnSelectionChanged(List<ISelectable> selectedObjects)
    {
        // Find first attacker in selection
        IAttacker newSelectedAttacker = null;
        foreach (ISelectable selectable in selectedObjects)
        {
            // First check if the selectable itself is an attacker
            if (selectable is IAttacker attacker)
            {
                newSelectedAttacker = attacker;
                break;
            }
            
            // If not, check if it has an AttackCapability component
            MonoBehaviour selectableMono = selectable as MonoBehaviour;
            if (selectableMono != null)
            {
                AttackCapability attackCap = selectableMono.GetComponent<AttackCapability>();
                if (attackCap != null)
                {
                    newSelectedAttacker = attackCap;
                    break;
                }
            }
        }
        
        // Only update if we have a different attacker OR no selection
        if (newSelectedAttacker != selectedAttacker)
        {
            // If we had a previous attacker and now have none, deactivate
            if (selectedAttacker != null && newSelectedAttacker == null)
            {
                selectedAttacker = null;
                DeactivateCombatMode();
            }
            // If we have a new attacker, update and activate
            else if (newSelectedAttacker != null)
            {
                selectedAttacker = newSelectedAttacker;
                
                // Auto-activate for appropriate input modes
                if (combatInputMode == CombatInputMode.ClickToAttack || combatInputMode == CombatInputMode.RightClickAttack || combatInputMode == CombatInputMode.DoubleClickAttack)
                {
                    ActivateCombatMode(selectedAttacker);
                }
            }
        }
    }
    
    /// <summary>
    /// Called when combat state changes
    /// </summary>
    private void OnCombatStateChanged()
    {
        // Could implement additional logic based on combat state
    }
    
    /// <summary>
    /// Called when targeting starts
    /// </summary>
    private void OnTargetingStarted(IAttacker attacker)
    {
        if (enableInputLogging)
        {
            Debug.Log($"CombatInputHandler: Targeting started for {attacker.GetDisplayInfo()}");
        }
    }
    
    /// <summary>
    /// Called when targeting stops
    /// </summary>
    private void OnTargetingStopped(IAttacker attacker)
    {
        if (enableInputLogging)
        {
            Debug.Log($"CombatInputHandler: Targeting stopped for {attacker?.GetDisplayInfo() ?? "unknown"}");
        }
    }
    
    /// <summary>
    /// Sets the combat input mode
    /// </summary>
    public void SetInputMode(CombatInputMode newMode)
    {
        combatInputMode = newMode;
        
        if (enableInputLogging)
        {
            Debug.Log($"CombatInputHandler: Input mode changed to {newMode}");
        }
    }
    
    /// <summary>
    /// Enables or disables combat input
    /// </summary>
    public void SetCombatInputEnabled(bool enabled)
    {
        enableCombatInput = enabled;
        
        if (!enabled)
        {
            DeactivateCombatMode();
        }
        
        if (enableInputLogging)
        {
            Debug.Log($"CombatInputHandler: Combat input {(enabled ? "enabled" : "disabled")}");
        }
    }
    
    /// <summary>
    /// Gets combat input information for debugging
    /// </summary>
    public string GetInputInfo()
    {
        return $"Input State: {(enableCombatInput ? "Enabled" : "Disabled")}, " +
               $"Combat Mode: {(combatModeActive ? "Active" : "Inactive")}, " +
               $"Input Mode: {combatInputMode}, " +
               $"Selected Attacker: {selectedAttacker?.GetDisplayInfo() ?? "None"}";
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Restore default cursor
        if (showCombatCursor)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        
        // Unregister from events
        if (selectionManager != null)
        {
            selectionManager.OnObjectSelected -= OnUnitSelected;
            selectionManager.OnObjectDeselected -= OnUnitDeselected;
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
        }
        
        if (combatManager != null)
        {
            combatManager.OnCombatStateChanged -= OnCombatStateChanged;
        }
        
        if (targetingSystem != null)
        {
            targetingSystem.OnTargetingStarted -= OnTargetingStarted;
            targetingSystem.OnTargetingStopped -= OnTargetingStopped;
        }
        
        // Clear event references
        OnCombatModeActivated = null;
        OnCombatModeDeactivated = null;
        OnAttackInputReceived = null;
    }
}