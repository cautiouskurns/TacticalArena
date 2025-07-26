using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Centralized health system coordinator for tactical combat.
/// Manages health tracking for all units, coordinates damage application,
/// and integrates with combat system for complete health management.
/// Part of Task 2.1.3 - Health & Damage System.
/// </summary>
public class HealthManager : MonoBehaviour
{
    [Header("Health System Configuration")]
    [SerializeField] private int defaultMaxHealth = 3;
    [SerializeField] private bool enableHealthRegeneration = false;
    [SerializeField] private float healthRegenRate = 0.5f;
    [SerializeField] private float healthRegenInterval = 1.0f;
    
    [Header("System Integration")]
    [SerializeField] private bool integrateWithCombat = true;
    [SerializeField] private bool enableHealthEvents = true;
    [SerializeField] private bool enableHealthCaching = true;
    [SerializeField] private bool enableHealthPrediction = false;
    
    [Header("Performance Settings")]
    [SerializeField] private float healthUpdateInterval = 0.1f;
    [SerializeField] private int maxHealthUpdatesPerFrame = 10;
    [SerializeField] private bool enableBatchHealthUpdates = true;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableHealthDebugging = true;
    [SerializeField] private bool logHealthChanges = false;
    [SerializeField] private bool visualizeHealthStates = true;
    
    // System references
    private DamageCalculator damageCalculator;
    private DeathHandler deathHandler;
    private WinConditionChecker winConditionChecker;
    private HealthEventBroadcaster eventBroadcaster;
    private CombatManager combatManager;
    
    // Health tracking
    private Dictionary<Unit, HealthComponent> unitHealthMap;
    private List<HealthComponent> allHealthComponents;
    private Queue<HealthUpdateRequest> healthUpdateQueue;
    
    // System state
    private bool isInitialized = false;
    private float lastHealthUpdate = 0f;
    private float lastRegenUpdate = 0f;
    private int healthUpdatesThisFrame = 0;
    
    // Events
    public System.Action<Unit, int, int> OnUnitHealthChanged; // unit, oldHealth, newHealth
    public System.Action<Unit, int, IAttacker> OnUnitDamaged; // unit, damage, attacker
    public System.Action<Unit, int> OnUnitHealed; // unit, healAmount
    public System.Action<Unit, IAttacker> OnUnitDied; // unit, killer
    public System.Action<UnitTeam> OnTeamEliminated; // eliminatedTeam
    public System.Action OnHealthSystemReady;
    
    // Properties
    public int DefaultMaxHealth => defaultMaxHealth;
    public bool EnableHealthRegeneration => enableHealthRegeneration;
    public bool IsInitialized => isInitialized;
    public int TotalUnitsTracked => unitHealthMap?.Count ?? 0;
    public int AliveUnitsCount => GetAliveUnitsCount();
    
    // Health update request structure
    private struct HealthUpdateRequest
    {
        public HealthComponent target;
        public int healthChange;
        public IAttacker source;
        public HealthChangeType changeType;
        
        public HealthUpdateRequest(HealthComponent target, int change, IAttacker source, HealthChangeType type)
        {
            this.target = target;
            this.healthChange = change;
            this.source = source;
            this.changeType = type;
        }
    }
    
    public enum HealthChangeType
    {
        Damage,
        Healing,
        Regeneration,
        SetHealth
    }
    
    void Awake()
    {
        InitializeHealthManager();
    }
    
    void Start()
    {
        FindSystemReferences();
        RegisterAllUnits();
        StartHealthSystemCoroutines();
        CompleteInitialization();
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        // Reset frame counter
        if (Time.time - lastHealthUpdate >= healthUpdateInterval)
        {
            healthUpdatesThisFrame = 0;
            lastHealthUpdate = Time.time;
        }
        
        // Process health updates
        ProcessHealthUpdateQueue();
        
        // Handle regeneration - TEMPORARILY DISABLED FOR DEBUGGING
        // if (enableHealthRegeneration && Time.time - lastRegenUpdate >= healthRegenInterval)
        // {
        //     Debug.Log($"⚠️ HealthManager: Processing health regeneration for all units - enableHealthRegeneration: {enableHealthRegeneration}");
        //     ProcessHealthRegeneration();
        //     lastRegenUpdate = Time.time;
        // }
    }
    
    /// <summary>
    /// Initializes the health management system
    /// </summary>
    private void InitializeHealthManager()
    {
        unitHealthMap = new Dictionary<Unit, HealthComponent>();
        allHealthComponents = new List<HealthComponent>();
        healthUpdateQueue = new Queue<HealthUpdateRequest>();
        
        if (enableHealthDebugging)
        {
            Debug.Log("HealthManager initialized - Tactical health system ready");
        }
    }
    
    /// <summary>
    /// Finds references to other system components
    /// </summary>
    private void FindSystemReferences()
    {
        damageCalculator = GetComponent<DamageCalculator>();
        deathHandler = GetComponent<DeathHandler>();
        winConditionChecker = GetComponent<WinConditionChecker>();
        eventBroadcaster = GetComponent<HealthEventBroadcaster>();
        combatManager = FindFirstObjectByType<CombatManager>();
        
        if (enableHealthDebugging)
        {
            Debug.Log($"HealthManager found references - Damage Calculator: {damageCalculator != null}, " +
                     $"Death Handler: {deathHandler != null}, Win Checker: {winConditionChecker != null}, " +
                     $"Event Broadcaster: {eventBroadcaster != null}, Combat Manager: {combatManager != null}");
        }
    }
    
    /// <summary>
    /// Registers all existing units with the health system
    /// </summary>
    private void RegisterAllUnits()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        foreach (Unit unit in allUnits)
        {
            RegisterUnit(unit);
        }
        
        if (enableHealthDebugging)
        {
            Debug.Log($"HealthManager registered {allUnits.Length} units");
        }
    }
    
    /// <summary>
    /// Starts health system coroutines
    /// </summary>
    private void StartHealthSystemCoroutines()
    {
        if (enableBatchHealthUpdates)
        {
            StartCoroutine(BatchHealthUpdateCoroutine());
        }
        
        if (enableHealthPrediction)
        {
            StartCoroutine(HealthPredictionCoroutine());
        }
    }
    
    /// <summary>
    /// Completes system initialization
    /// </summary>
    private void CompleteInitialization()
    {
        isInitialized = true;
        OnHealthSystemReady?.Invoke();
        
        if (enableHealthDebugging)
        {
            Debug.Log("HealthManager initialization complete - System ready");
        }
    }
    
    #region Unit Registration
    
    /// <summary>
    /// Registers a unit with the health system
    /// </summary>
    public void RegisterUnit(Unit unit)
    {
        if (unit == null)
        {
            Debug.LogWarning("HealthManager: Attempted to register null unit");
            return;
        }
        
        if (unitHealthMap.ContainsKey(unit))
        {
            if (logHealthChanges)
            {
                Debug.Log($"HealthManager: Unit {unit.name} already registered");
            }
            return;
        }
        
        // Get or create health component
        HealthComponent healthComponent = unit.GetComponent<HealthComponent>();
        if (healthComponent == null)
        {
            healthComponent = unit.gameObject.AddComponent<HealthComponent>();
            healthComponent.Initialize(defaultMaxHealth);
            
            if (enableHealthDebugging)
            {
                Debug.Log($"HealthManager: Created health component for {unit.name}");
            }
        }
        
        // Register the unit
        unitHealthMap[unit] = healthComponent;
        allHealthComponents.Add(healthComponent);
        
        // Subscribe to health component events
        healthComponent.OnHealthChanged += (oldHealth, newHealth) => HandleUnitHealthChanged(unit, oldHealth, newHealth);
        healthComponent.OnDamaged += (damage, attacker) => HandleUnitDamaged(unit, damage, attacker);
        healthComponent.OnHealed += (healAmount) => HandleUnitHealed(unit, healAmount);
        healthComponent.OnDied += (killer) => HandleUnitDied(unit, killer);
        
        if (logHealthChanges)
        {
            Debug.Log($"HealthManager: Registered unit {unit.name} with {healthComponent.CurrentHealth}/{healthComponent.MaxHealth} HP");
        }
    }
    
    /// <summary>
    /// Unregisters a unit from the health system
    /// </summary>
    public void UnregisterUnit(Unit unit)
    {
        if (unit == null || !unitHealthMap.ContainsKey(unit))
        {
            return;
        }
        
        HealthComponent healthComponent = unitHealthMap[unit];
        
        // Unsubscribe from events
        if (healthComponent != null)
        {
            healthComponent.OnHealthChanged -= (oldHealth, newHealth) => HandleUnitHealthChanged(unit, oldHealth, newHealth);
            healthComponent.OnDamaged -= (damage, attacker) => HandleUnitDamaged(unit, damage, attacker);
            healthComponent.OnHealed -= (healAmount) => HandleUnitHealed(unit, healAmount);
            healthComponent.OnDied -= (killer) => HandleUnitDied(unit, killer);
            
            allHealthComponents.Remove(healthComponent);
        }
        
        unitHealthMap.Remove(unit);
        
        if (logHealthChanges)
        {
            Debug.Log($"HealthManager: Unregistered unit {unit.name}");
        }
    }
    
    #endregion
    
    #region Health Queries
    
    /// <summary>
    /// Gets the health component for a unit
    /// </summary>
    public HealthComponent GetHealthComponent(Unit unit)
    {
        if (unit == null || !unitHealthMap.ContainsKey(unit))
        {
            return null;
        }
        
        return unitHealthMap[unit];
    }
    
    /// <summary>
    /// Gets the current health of a unit
    /// </summary>
    public int GetUnitHealth(Unit unit)
    {
        HealthComponent healthComponent = GetHealthComponent(unit);
        return healthComponent?.CurrentHealth ?? 0;
    }
    
    /// <summary>
    /// Gets the maximum health of a unit
    /// </summary>
    public int GetUnitMaxHealth(Unit unit)
    {
        HealthComponent healthComponent = GetHealthComponent(unit);
        return healthComponent?.MaxHealth ?? 0;
    }
    
    /// <summary>
    /// Checks if a unit is alive
    /// </summary>
    public bool IsUnitAlive(Unit unit)
    {
        HealthComponent healthComponent = GetHealthComponent(unit);
        return healthComponent?.IsAlive ?? false;
    }
    
    /// <summary>
    /// Gets the health percentage of a unit (0.0 to 1.0)
    /// </summary>
    public float GetUnitHealthPercentage(Unit unit)
    {
        HealthComponent healthComponent = GetHealthComponent(unit);
        if (healthComponent == null || healthComponent.MaxHealth == 0)
        {
            return 0f;
        }
        
        return (float)healthComponent.CurrentHealth / healthComponent.MaxHealth;
    }
    
    /// <summary>
    /// Gets all units with their health status
    /// </summary>
    public Dictionary<Unit, HealthStatus> GetAllUnitHealthStatus()
    {
        Dictionary<Unit, HealthStatus> healthStatus = new Dictionary<Unit, HealthStatus>();
        
        foreach (var kvp in unitHealthMap)
        {
            Unit unit = kvp.Key;
            HealthComponent healthComp = kvp.Value;
            
            if (healthComp != null)
            {
                healthStatus[unit] = new HealthStatus
                {
                    currentHealth = healthComp.CurrentHealth,
                    maxHealth = healthComp.MaxHealth,
                    isAlive = healthComp.IsAlive,
                    healthPercentage = GetUnitHealthPercentage(unit)
                };
            }
        }
        
        return healthStatus;
    }
    
    /// <summary>
    /// Gets the count of alive units
    /// </summary>
    private int GetAliveUnitsCount()
    {
        int aliveCount = 0;
        
        foreach (var healthComponent in allHealthComponents)
        {
            if (healthComponent != null && healthComponent.IsAlive)
            {
                aliveCount++;
            }
        }
        
        return aliveCount;
    }
    
    /// <summary>
    /// Gets alive units by team
    /// </summary>
    public List<Unit> GetAliveUnitsByTeam(UnitTeam team)
    {
        List<Unit> aliveUnits = new List<Unit>();
        
        foreach (var kvp in unitHealthMap)
        {
            Unit unit = kvp.Key;
            HealthComponent healthComp = kvp.Value;
            
            if (unit != null && healthComp != null && healthComp.IsAlive && unit.Team == team)
            {
                aliveUnits.Add(unit);
            }
        }
        
        return aliveUnits;
    }
    
    #endregion
    
    #region Health Modification
    
    /// <summary>
    /// Applies damage to a unit
    /// </summary>
    public DamageResult DamageUnit(Unit target, int damage, IAttacker attacker)
    {
        if (target == null)
        {
            return DamageResult.Blocked("Target is null");
        }
        
        HealthComponent healthComponent = GetHealthComponent(target);
        if (healthComponent == null)
        {
            return DamageResult.Blocked("Target has no health component");
        }
        
        if (!healthComponent.IsAlive)
        {
            return DamageResult.Blocked("Target is already dead");
        }
        
        // Calculate actual damage using damage calculator
        int actualDamage = damage;
        if (damageCalculator != null)
        {
            // Get IAttackable component from the unit for damage calculation
            IAttackable attackableTarget = target.GetComponent<IAttackable>();
            if (attackableTarget != null)
            {
                actualDamage = damageCalculator.CalculateDamage(damage, attacker, attackableTarget);
            }
            else
            {
                // Fallback to basic damage if no IAttackable component
                actualDamage = damage;
            }
        }
        
        // Apply damage
        int damageDealt = healthComponent.TakeDamage(actualDamage, attacker);
        
        // Queue health update if using batch processing
        if (enableBatchHealthUpdates)
        {
            QueueHealthUpdate(healthComponent, -damageDealt, attacker, HealthChangeType.Damage);
        }
        
        if (logHealthChanges)
        {
            Debug.Log($"HealthManager: {target.name} took {damageDealt} damage from {attacker?.GetDisplayInfo() ?? "unknown"}");
        }
        
        return DamageResult.Success(damageDealt, healthComponent.CurrentHealth, !healthComponent.IsAlive);
    }
    
    /// <summary>
    /// Heals a unit
    /// </summary>
    public int HealUnit(Unit target, int healAmount)
    {
        if (target == null)
        {
            return 0;
        }
        
        HealthComponent healthComponent = GetHealthComponent(target);
        if (healthComponent == null || !healthComponent.IsAlive)
        {
            return 0;
        }
        
        int actualHealing = healthComponent.Heal(healAmount);
        
        // Queue health update if using batch processing
        if (enableBatchHealthUpdates)
        {
            QueueHealthUpdate(healthComponent, actualHealing, null, HealthChangeType.Healing);
        }
        
        if (logHealthChanges)
        {
            Debug.Log($"HealthManager: {target.name} healed for {actualHealing} HP");
        }
        
        return actualHealing;
    }
    
    /// <summary>
    /// Sets a unit's health directly
    /// </summary>
    public void SetUnitHealth(Unit target, int newHealth)
    {
        if (target == null)
        {
            return;
        }
        
        HealthComponent healthComponent = GetHealthComponent(target);
        if (healthComponent == null)
        {
            return;
        }
        
        int oldHealth = healthComponent.CurrentHealth;
        healthComponent.SetCurrentHealth(newHealth);
        int healthChange = newHealth - oldHealth;
        
        // Queue health update if using batch processing
        if (enableBatchHealthUpdates)
        {
            QueueHealthUpdate(healthComponent, healthChange, null, HealthChangeType.SetHealth);
        }
        
        if (logHealthChanges)
        {
            Debug.Log($"HealthManager: {target.name} health set to {newHealth}");
        }
    }
    
    /// <summary>
    /// Restores all units to full health
    /// </summary>
    public void RestoreAllUnitsToFullHealth()
    {
        Debug.Log($"⚠️ HealthManager: RestoreAllUnitsToFullHealth() called - STACK TRACE:");
        Debug.Log(System.Environment.StackTrace);
        
        foreach (var kvp in unitHealthMap)
        {
            Unit unit = kvp.Key;
            HealthComponent healthComp = kvp.Value;
            
            if (healthComp != null)
            {
                healthComp.SetCurrentHealth(healthComp.MaxHealth);
            }
        }
        
        if (enableHealthDebugging)
        {
            Debug.Log("HealthManager: Restored all units to full health");
        }
    }
    
    #endregion
    
    #region Health Update Processing
    
    /// <summary>
    /// Queues a health update for batch processing
    /// </summary>
    private void QueueHealthUpdate(HealthComponent target, int healthChange, IAttacker source, HealthChangeType changeType)
    {
        HealthUpdateRequest request = new HealthUpdateRequest(target, healthChange, source, changeType);
        healthUpdateQueue.Enqueue(request);
    }
    
    /// <summary>
    /// Processes queued health updates
    /// </summary>
    private void ProcessHealthUpdateQueue()
    {
        while (healthUpdateQueue.Count > 0 && healthUpdatesThisFrame < maxHealthUpdatesPerFrame)
        {
            HealthUpdateRequest request = healthUpdateQueue.Dequeue();
            ProcessHealthUpdateRequest(request);
            healthUpdatesThisFrame++;
        }
    }
    
    /// <summary>
    /// Processes a single health update request
    /// </summary>
    private void ProcessHealthUpdateRequest(HealthUpdateRequest request)
    {
        if (request.target == null) return;
        
        // Handle different types of health changes
        switch (request.changeType)
        {
            case HealthChangeType.Damage:
                // Damage already applied, just trigger events
                break;
                
            case HealthChangeType.Healing:
                // Healing already applied, just trigger events
                break;
                
            case HealthChangeType.Regeneration:
                request.target.Heal(request.healthChange);
                break;
                
            case HealthChangeType.SetHealth:
                // Health already set, just trigger events
                break;
        }
    }
    
    /// <summary>
    /// Processes health regeneration for all units
    /// </summary>
    private void ProcessHealthRegeneration()
    {
        Debug.Log($"⚠️ HealthManager: ProcessHealthRegeneration called - DISABLED FOR DEBUGGING");
        return; // DISABLED FOR DEBUGGING
        
        Debug.Log($"⚠️ HealthManager: ProcessHealthRegeneration called - enableHealthRegeneration: {enableHealthRegeneration}");
        
        if (!enableHealthRegeneration) return;
        
        int regenAmount = Mathf.RoundToInt(healthRegenRate);
        if (regenAmount <= 0) return;
        
        Debug.Log($"⚠️ HealthManager: Healing all units by {regenAmount} HP");
        
        foreach (var healthComponent in allHealthComponents)
        {
            if (healthComponent != null && healthComponent.IsAlive && 
                healthComponent.CurrentHealth < healthComponent.MaxHealth)
            {
                Debug.Log($"⚠️ HealthManager: Healing {healthComponent.name} from {healthComponent.CurrentHealth} to {healthComponent.CurrentHealth + regenAmount}");
                healthComponent.Heal(regenAmount);
                
                if (logHealthChanges)
                {
                    Debug.Log($"HealthManager: {healthComponent.name} regenerated {regenAmount} HP");
                }
            }
        }
    }
    
    #endregion
    
    #region Event Handlers
    
    /// <summary>
    /// Handles unit health changed events
    /// </summary>
    private void HandleUnitHealthChanged(Unit unit, int oldHealth, int newHealth)
    {
        OnUnitHealthChanged?.Invoke(unit, oldHealth, newHealth);
        
        if (eventBroadcaster != null)
        {
            eventBroadcaster.BroadcastHealthChanged(unit, oldHealth, newHealth);
        }
    }
    
    /// <summary>
    /// Handles unit damaged events
    /// </summary>
    private void HandleUnitDamaged(Unit unit, int damage, IAttacker attacker)
    {
        OnUnitDamaged?.Invoke(unit, damage, attacker);
        
        if (eventBroadcaster != null)
        {
            eventBroadcaster.BroadcastUnitDamaged(unit, damage, attacker);
        }
    }
    
    /// <summary>
    /// Handles unit healed events
    /// </summary>
    private void HandleUnitHealed(Unit unit, int healAmount)
    {
        OnUnitHealed?.Invoke(unit, healAmount);
        
        if (eventBroadcaster != null)
        {
            eventBroadcaster.BroadcastUnitHealed(unit, healAmount);
        }
    }
    
    /// <summary>
    /// Handles unit death events
    /// </summary>
    private void HandleUnitDied(Unit unit, IAttacker killer)
    {
        OnUnitDied?.Invoke(unit, killer);
        
        if (eventBroadcaster != null)
        {
            eventBroadcaster.BroadcastUnitDied(unit, killer);
        }
        
        // Handle death through death handler
        if (deathHandler != null)
        {
            deathHandler.HandleUnitDeath(unit, killer);
        }
        
        // Check win conditions
        if (winConditionChecker != null)
        {
            winConditionChecker.CheckWinConditions();
        }
    }
    
    #endregion
    
    #region Coroutines
    
    /// <summary>
    /// Batch health update coroutine
    /// </summary>
    private IEnumerator BatchHealthUpdateCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(healthUpdateInterval);
            
            if (healthUpdateQueue.Count > 0)
            {
                ProcessHealthUpdateQueue();
            }
        }
    }
    
    /// <summary>
    /// Health prediction coroutine
    /// </summary>
    private IEnumerator HealthPredictionCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f);
            
            // Predict health changes based on current combat state
            if (enableHealthPrediction)
            {
                // This could analyze combat patterns and predict outcomes
            }
        }
    }
    
    #endregion
    
    #region Public Utility Methods
    
    /// <summary>
    /// Gets health system statistics
    /// </summary>
    public string GetHealthSystemStats()
    {
        int aliveUnits = GetAliveUnitsCount();
        int totalUnits = TotalUnitsTracked;
        int queuedUpdates = healthUpdateQueue.Count;
        
        return $"Health System - Alive: {aliveUnits}/{totalUnits}, Queued Updates: {queuedUpdates}";
    }
    
    /// <summary>
    /// Forces immediate processing of all queued health updates
    /// </summary>
    public void FlushHealthUpdateQueue()
    {
        while (healthUpdateQueue.Count > 0)
        {
            HealthUpdateRequest request = healthUpdateQueue.Dequeue();
            ProcessHealthUpdateRequest(request);
        }
        
        if (enableHealthDebugging)
        {
            Debug.Log("HealthManager: Flushed all queued health updates");
        }
    }
    
    /// <summary>
    /// Resets the health system
    /// </summary>
    public void ResetHealthSystem()
    {
        // Clear all tracking
        unitHealthMap.Clear();
        allHealthComponents.Clear();
        healthUpdateQueue.Clear();
        
        // Re-register all units
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in allUnits)
        {
            RegisterUnit(unit);
        }
        
        if (enableHealthDebugging)
        {
            Debug.Log("HealthManager: Health system reset completed");
        }
    }
    
    #endregion
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Clear event references
        OnUnitHealthChanged = null;
        OnUnitDamaged = null;
        OnUnitHealed = null;
        OnUnitDied = null;
        OnTeamEliminated = null;
        OnHealthSystemReady = null;
        
        // Clear collections
        unitHealthMap?.Clear();
        allHealthComponents?.Clear();
        healthUpdateQueue?.Clear();
    }
}

/// <summary>
/// Health status structure for reporting
/// </summary>
[System.Serializable]
public struct HealthStatus
{
    public int currentHealth;
    public int maxHealth;
    public bool isAlive;
    public float healthPercentage;
}