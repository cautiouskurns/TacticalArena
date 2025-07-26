using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Central combat system coordinator for the tactical arena.
/// Manages attack requests, validation, and execution in coordination with other combat systems.
/// Integrates with Sub-milestone 1.2 systems for complete tactical combat capability.
/// </summary>
public class CombatManager : MonoBehaviour
{
    [Header("Combat Rules")]
    [SerializeField] private int baseDamage = 1;
    [SerializeField] private int attacksPerTurn = 1;
    [SerializeField] private bool allowDiagonalAttacks = false;
    [SerializeField] private bool requireLineOfSight = false;
    [SerializeField] private float attackRange = 1.5f;
    
    [Header("Combat Control")]
    [SerializeField] private bool preventFriendlyFire = true;
    [SerializeField] private bool enableCombatLogging = true;
    [SerializeField] private bool combatEnabled = true;
    
    [Header("Turn Management")]
    [SerializeField] private bool trackAttacksPerTurn = true;
    [SerializeField] private bool resetAttacksOnTurnEnd = true;
    
    // System references
    private AttackValidator attackValidator;
    private AttackExecutor attackExecutor;
    private TargetingSystem targetingSystem;
    private SelectionManager selectionManager;
    private GridManager gridManager;
    private HealthManager healthManager;
    private WinConditionChecker winConditionChecker;
    
    // Combat state tracking
    private Dictionary<IAttacker, int> attacksThisTurn = new Dictionary<IAttacker, int>();
    private List<IAttacker> activeAttackers = new List<IAttacker>();
    private bool combatInProgress = false;
    
    // Events
    public System.Action<IAttacker, IAttackable> OnAttackRequested;
    public System.Action<IAttacker, IAttackable, AttackResult> OnAttackCompleted;
    public System.Action<IAttacker> OnAttackerOutOfAttacks;
    public System.Action OnCombatStateChanged;
    
    // Properties
    public bool CombatEnabled => combatEnabled;
    public bool CombatInProgress => combatInProgress;
    public int BaseDamage => baseDamage;
    public int AttacksPerTurn => attacksPerTurn;
    public bool AllowDiagonalAttacks => allowDiagonalAttacks;
    public float AttackRange => attackRange;
    
    void Awake()
    {
        InitializeCombatManager();
    }
    
    void Start()
    {
        FindSystemReferences();
        SetupEventListeners();
        RegisterExistingUnits();
    }
    
    /// <summary>
    /// Initializes the combat manager
    /// </summary>
    private void InitializeCombatManager()
    {
        if (enableCombatLogging)
        {
            Debug.Log("CombatManager initialized - Tactical combat system ready");
        }
    }
    
    /// <summary>
    /// Finds references to other systems
    /// </summary>
    private void FindSystemReferences()
    {
        attackValidator = GetComponent<AttackValidator>();
        if (attackValidator == null)
        {
            Debug.LogError("CombatManager: AttackValidator not found!");
        }
        
        attackExecutor = GetComponent<AttackExecutor>();
        if (attackExecutor == null)
        {
            Debug.LogError("CombatManager: AttackExecutor not found!");
        }
        
        targetingSystem = GetComponent<TargetingSystem>();
        if (targetingSystem == null)
        {
            Debug.LogError("CombatManager: TargetingSystem not found!");
        }
        
        selectionManager = FindFirstObjectByType<SelectionManager>();
        if (selectionManager == null)
        {
            Debug.LogError("CombatManager: SelectionManager not found!");
        }
        
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("CombatManager: GridManager not found!");
        }
        
        // Find health system components
        healthManager = FindFirstObjectByType<HealthManager>();
        if (healthManager == null)
        {
            Debug.LogWarning("CombatManager: HealthManager not found - combat will use basic damage system");
        }
        
        winConditionChecker = FindFirstObjectByType<WinConditionChecker>();
        if (winConditionChecker == null)
        {
            Debug.LogWarning("CombatManager: WinConditionChecker not found - win conditions not monitored");
        }
        
        if (enableCombatLogging)
        {
            Debug.Log($"CombatManager found references - Validator: {attackValidator != null}, " +
                     $"Executor: {attackExecutor != null}, Targeting: {targetingSystem != null}, " +
                     $"Selection: {selectionManager != null}, Grid: {gridManager != null}, " +
                     $"Health: {healthManager != null}, Win Checker: {winConditionChecker != null}");
        }
    }
    
    /// <summary>
    /// Sets up event listeners for combat coordination
    /// </summary>
    private void SetupEventListeners()
    {
        // Listen for attack execution completion
        if (attackExecutor != null)
        {
            attackExecutor.OnAttackExecuted += OnAttackExecuted;
            attackExecutor.OnAttackFailed += OnAttackFailed;
        }
        
        // Listen for targeting events
        if (targetingSystem != null)
        {
            targetingSystem.OnTargetSelected += OnTargetSelected;
            targetingSystem.OnTargetingCanceled += OnTargetingCanceled;
        }
    }
    
    /// <summary>
    /// Registers existing units with the combat system
    /// </summary>
    private void RegisterExistingUnits()
    {
        // Find all units with attack capability
        AttackCapability[] attackCapabilities = FindObjectsByType<AttackCapability>(FindObjectsSortMode.None);
        foreach (AttackCapability attacker in attackCapabilities)
        {
            RegisterAttacker(attacker);
        }
        
        if (enableCombatLogging)
        {
            Debug.Log($"CombatManager registered {activeAttackers.Count} attackers");
        }
    }
    
    /// <summary>
    /// Registers a new attacker with the combat system
    /// </summary>
    public void RegisterAttacker(IAttacker attacker)
    {
        if (attacker != null && !activeAttackers.Contains(attacker))
        {
            activeAttackers.Add(attacker);
            attacksThisTurn[attacker] = 0;
            
            if (enableCombatLogging)
            {
                Debug.Log($"CombatManager: Registered attacker {attacker}");
            }
        }
    }
    
    /// <summary>
    /// Unregisters an attacker from the combat system
    /// </summary>
    public void UnregisterAttacker(IAttacker attacker)
    {
        if (attacker != null)
        {
            activeAttackers.Remove(attacker);
            attacksThisTurn.Remove(attacker);
            
            if (enableCombatLogging)
            {
                Debug.Log($"CombatManager: Unregistered attacker {attacker}");
            }
        }
    }
    
    /// <summary>
    /// Requests an attack from attacker to target
    /// </summary>
    public AttackResult RequestAttack(IAttacker attacker, IAttackable target)
    {
        if (!combatEnabled)
        {
            return AttackResult.Failed("Combat is disabled");
        }
        
        if (attacker == null)
        {
            return AttackResult.Failed("Attacker is null");
        }
        
        if (target == null)
        {
            return AttackResult.Failed("Target is null");
        }
        
        if (enableCombatLogging)
        {
            Debug.Log($"CombatManager: Attack requested from {attacker} to {target}");
        }
        
        // Fire attack requested event
        OnAttackRequested?.Invoke(attacker, target);
        
        // Validate the attack
        AttackValidationResult validation = attackValidator.ValidateAttack(attacker, target);
        if (!validation.isValid)
        {
            if (enableCombatLogging)
            {
                Debug.LogWarning($"CombatManager: Attack validation failed - {validation.failureReason}");
            }
            return AttackResult.Failed(validation.failureReason);
        }
        
        // Check attack count limits
        if (trackAttacksPerTurn)
        {
            int attacksUsed = attacksThisTurn.ContainsKey(attacker) ? attacksThisTurn[attacker] : 0;
            if (attacksUsed >= attacksPerTurn)
            {
                OnAttackerOutOfAttacks?.Invoke(attacker);
                return AttackResult.Failed("Attacker has used all attacks this turn");
            }
        }
        
        // Execute the attack
        combatInProgress = true;
        OnCombatStateChanged?.Invoke();
        
        AttackResult result = attackExecutor.ExecuteAttack(attacker, target, baseDamage);
        
        // Track attack usage
        if (trackAttacksPerTurn)
        {
            attacksThisTurn[attacker] = attacksThisTurn.ContainsKey(attacker) ? attacksThisTurn[attacker] + 1 : 1;
        }
        
        return result;
    }
    
    /// <summary>
    /// Initiates targeting mode for the specified attacker
    /// </summary>
    public void StartTargeting(IAttacker attacker)
    {
        if (!combatEnabled || attacker == null || targetingSystem == null)
        {
            return;
        }
        
        if (enableCombatLogging)
        {
            Debug.Log($"CombatManager: Starting targeting for {attacker}");
        }
        
        targetingSystem.StartTargeting(attacker);
    }
    
    /// <summary>
    /// Stops targeting mode
    /// </summary>
    public void StopTargeting()
    {
        if (targetingSystem != null)
        {
            targetingSystem.StopTargeting();
        }
        
        if (enableCombatLogging)
        {
            Debug.Log("CombatManager: Targeting stopped");
        }
    }
    
    /// <summary>
    /// Called when a target is selected during targeting mode
    /// </summary>
    private void OnTargetSelected(IAttacker attacker, IAttackable target)
    {
        AttackResult result = RequestAttack(attacker, target);
        
        if (enableCombatLogging)
        {
            Debug.Log($"CombatManager: Target selected attack result - {result.success}: {result.message}");
        }
    }
    
    /// <summary>
    /// Called when targeting is canceled
    /// </summary>
    private void OnTargetingCanceled()
    {
        combatInProgress = false;
        OnCombatStateChanged?.Invoke();
        
        if (enableCombatLogging)
        {
            Debug.Log("CombatManager: Targeting canceled");
        }
    }
    
    /// <summary>
    /// Called when an attack is successfully executed
    /// </summary>
    private void OnAttackExecuted(IAttacker attacker, IAttackable target, int damage)
    {
        combatInProgress = false;
        OnCombatStateChanged?.Invoke();
        
        AttackResult result = AttackResult.Success(damage, "Attack successful");
        OnAttackCompleted?.Invoke(attacker, target, result);
        
        if (enableCombatLogging)
        {
            Debug.Log($"CombatManager: Attack executed - {attacker} dealt {damage} damage to {target}");
        }
    }
    
    /// <summary>
    /// Called when an attack execution fails
    /// </summary>
    private void OnAttackFailed(IAttacker attacker, IAttackable target, string reason)
    {
        combatInProgress = false;
        OnCombatStateChanged?.Invoke();
        
        AttackResult result = AttackResult.Failed(reason);
        OnAttackCompleted?.Invoke(attacker, target, result);
        
        if (enableCombatLogging)
        {
            Debug.LogWarning($"CombatManager: Attack execution failed - {reason}");
        }
    }
    
    /// <summary>
    /// Resets attack counts for a new turn
    /// </summary>
    public void ResetAttacksForNewTurn()
    {
        if (resetAttacksOnTurnEnd && trackAttacksPerTurn)
        {
            foreach (IAttacker attacker in activeAttackers.ToList())
            {
                attacksThisTurn[attacker] = 0;
            }
            
            if (enableCombatLogging)
            {
                Debug.Log("CombatManager: Attack counts reset for new turn");
            }
        }
    }
    
    /// <summary>
    /// Gets the number of attacks used by an attacker this turn
    /// </summary>
    public int GetAttacksUsed(IAttacker attacker)
    {
        return attacksThisTurn.ContainsKey(attacker) ? attacksThisTurn[attacker] : 0;
    }
    
    /// <summary>
    /// Gets the number of attacks remaining for an attacker this turn
    /// </summary>
    public int GetAttacksRemaining(IAttacker attacker)
    {
        int used = GetAttacksUsed(attacker);
        return Mathf.Max(0, attacksPerTurn - used);
    }
    
    /// <summary>
    /// Checks if an attacker can still attack this turn
    /// </summary>
    public bool CanAttack(IAttacker attacker)
    {
        if (!combatEnabled || attacker == null)
        {
            return false;
        }
        
        if (!trackAttacksPerTurn)
        {
            return true;
        }
        
        return GetAttacksRemaining(attacker) > 0;
    }
    
    /// <summary>
    /// Gets combat information for debugging
    /// </summary>
    public string GetCombatInfo()
    {
        return $"Combat State: {(combatEnabled ? "Enabled" : "Disabled")}, " +
               $"In Progress: {combatInProgress}, " +
               $"Active Attackers: {activeAttackers.Count}, " +
               $"Base Damage: {baseDamage}, " +
               $"Attacks Per Turn: {attacksPerTurn}";
    }
    
    /// <summary>
    /// Enable or disable combat system
    /// </summary>
    public void SetCombatEnabled(bool enabled)
    {
        combatEnabled = enabled;
        OnCombatStateChanged?.Invoke();
        
        if (enableCombatLogging)
        {
            Debug.Log($"CombatManager: Combat {(enabled ? "enabled" : "disabled")}");
        }
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Unregister from events
        if (attackExecutor != null)
        {
            attackExecutor.OnAttackExecuted -= OnAttackExecuted;
            attackExecutor.OnAttackFailed -= OnAttackFailed;
        }
        
        if (targetingSystem != null)
        {
            targetingSystem.OnTargetSelected -= OnTargetSelected;
            targetingSystem.OnTargetingCanceled -= OnTargetingCanceled;
        }
        
        // Clear event references
        OnAttackRequested = null;
        OnAttackCompleted = null;
        OnAttackerOutOfAttacks = null;
        OnCombatStateChanged = null;
    }
}