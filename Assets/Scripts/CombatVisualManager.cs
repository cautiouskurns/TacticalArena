using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Centralized visual feedback coordination and effect management for tactical combat.
/// Coordinates all combat visual feedback including health bars, attack effects, death animations,
/// and tactical indicators to provide clear communication and professional polish.
/// Part of Task 2.1.4 - Combat Visual Feedback - COMPLETES SUB-MILESTONE 2.1
/// </summary>
public class CombatVisualManager : MonoBehaviour
{
    [Header("Visual Feedback Systems")]
    [SerializeField] private bool enableHealthBars = true;
    [SerializeField] private bool enableAttackEffects = true;
    [SerializeField] private bool enableDeathAnimations = true;
    [SerializeField] private bool enableLOSVisualization = true;
    [SerializeField] private bool enableCombatIndicators = true;
    
    [Header("Performance Configuration")]
    [SerializeField] private bool enableEffectPooling = true;
    [SerializeField] private int maxParticleSystemsActive = 10;
    [SerializeField] private int effectPoolSize = 20;
    [SerializeField] private bool enableLODOptimization = true;
    [SerializeField] private float cullingDistance = 50.0f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugging = true;
    [SerializeField] private bool logVisualEvents = true;
    [SerializeField] private bool showPerformanceStats = false;
    
    // System references
    private HealthManager healthManager;
    private AttackExecutor attackExecutor;
    private LineOfSightManager lineOfSightManager;
    private SelectionManager selectionManager;
    private DeathHandler deathHandler;
    
    // Visual effect managers
    private HealthBarManager healthBarManager;
    private AttackEffectManager attackEffectManager;
    private DeathAnimationController deathAnimationController;
    private LineOfSightVisualizer lineOfSightVisualizer;
    private CombatStateIndicatorManager combatStateIndicatorManager;
    private EffectPoolManager effectPoolManager;
    
    // Performance tracking
    private int activeEffectsCount = 0;
    private float lastPerformanceCheck = 0f;
    private const float PERFORMANCE_CHECK_INTERVAL = 1.0f;
    
    // Events
    public System.Action<string> OnVisualEffectTriggered;
    public System.Action<float> OnPerformanceUpdate;
    
    // Properties
    public bool IsInitialized { get; private set; } = false;
    public int ActiveEffectsCount => activeEffectsCount;
    public float EffectPoolUtilization => effectPoolManager?.PoolUtilization ?? 0f;
    
    void Awake()
    {
        InitializeCombatVisualManager();
    }
    
    void Start()
    {
        FindSystemReferences();
        InitializeVisualSystems();
        SubscribeToEvents();
        
        if (enableDebugging)
        {
            Debug.Log("CombatVisualManager initialized - Sub-Milestone 2.1 visual feedback active");
        }
    }
    
    void Update()
    {
        if (showPerformanceStats && Time.time - lastPerformanceCheck >= PERFORMANCE_CHECK_INTERVAL)
        {
            UpdatePerformanceStats();
            lastPerformanceCheck = Time.time;
        }
    }
    
    /// <summary>
    /// Initializes the combat visual manager
    /// </summary>
    private void InitializeCombatVisualManager()
    {
        // Ensure singleton behavior
        CombatVisualManager[] existingManagers = FindObjectsByType<CombatVisualManager>(FindObjectsSortMode.None);
        if (existingManagers.Length > 1)
        {
            for (int i = 1; i < existingManagers.Length; i++)
            {
                if (enableDebugging)
                {
                    Debug.LogWarning($"Multiple CombatVisualManagers found, destroying duplicate: {existingManagers[i].name}");
                }
                Destroy(existingManagers[i].gameObject);
            }
        }
        
        if (enableDebugging)
        {
            Debug.Log("CombatVisualManager initializing - Sub-Milestone 2.1 completion");
        }
    }
    
    /// <summary>
    /// Finds references to other system components
    /// </summary>
    private void FindSystemReferences()
    {
        healthManager = FindFirstObjectByType<HealthManager>();
        attackExecutor = FindFirstObjectByType<AttackExecutor>();
        lineOfSightManager = FindFirstObjectByType<LineOfSightManager>();
        selectionManager = FindFirstObjectByType<SelectionManager>();
        deathHandler = FindFirstObjectByType<DeathHandler>();
        
        if (enableDebugging)
        {
            Debug.Log($"CombatVisualManager found references - Health: {healthManager != null}, " +
                     $"Attack: {attackExecutor != null}, LOS: {lineOfSightManager != null}, " +
                     $"Selection: {selectionManager != null}, Death: {deathHandler != null}");
        }
    }
    
    /// <summary>
    /// Initializes all visual feedback systems
    /// </summary>
    private void InitializeVisualSystems()
    {
        // Initialize health bar manager
        if (enableHealthBars)
        {
            healthBarManager = GetOrCreateComponent<HealthBarManager>();
            healthBarManager.Initialize(this);
        }
        
        // Initialize attack effect manager
        if (enableAttackEffects)
        {
            attackEffectManager = GetOrCreateComponent<AttackEffectManager>();
            attackEffectManager.Initialize(this);
        }
        
        // Initialize death animation controller
        if (enableDeathAnimations)
        {
            deathAnimationController = GetOrCreateComponent<DeathAnimationController>();
            deathAnimationController.Initialize(this);
        }
        
        // Initialize LOS visualizer (only if LineOfSightManager exists)
        if (enableLOSVisualization && lineOfSightManager != null)
        {
            lineOfSightVisualizer = GetOrCreateComponent<LineOfSightVisualizer>();
            lineOfSightVisualizer.Initialize(this);
        }
        else if (enableLOSVisualization && lineOfSightManager == null)
        {
            if (enableDebugging)
            {
                Debug.LogWarning("CombatVisualManager: LineOfSightManager not found - LOS visualization disabled");
            }
            enableLOSVisualization = false;
        }
        
        // Initialize combat state indicators
        if (enableCombatIndicators)
        {
            combatStateIndicatorManager = GetOrCreateComponent<CombatStateIndicatorManager>();
            combatStateIndicatorManager.Initialize(this);
        }
        
        // Initialize effect pooling
        if (enableEffectPooling)
        {
            effectPoolManager = GetOrCreateComponent<EffectPoolManager>();
            effectPoolManager.Initialize(effectPoolSize, maxParticleSystemsActive);
        }
        
        IsInitialized = true;
        
        if (enableDebugging)
        {
            Debug.Log("CombatVisualManager: All visual systems initialized");
        }
    }
    
    /// <summary>
    /// Subscribes to combat system events
    /// </summary>
    private void SubscribeToEvents()
    {
        // Subscribe to health events
        if (healthManager != null)
        {
            healthManager.OnUnitHealthChanged += HandleHealthChanged;
            healthManager.OnUnitDamaged += HandleUnitDamaged;
            healthManager.OnUnitDied += HandleUnitDied;
            
            if (enableDebugging)
            {
                Debug.Log("CombatVisualManager: Subscribed to HealthManager events");
            }
        }
        else if (enableDebugging)
        {
            Debug.LogWarning("CombatVisualManager: HealthManager not found - health bar updates will not work");
        }
        
        // Subscribe to attack events
        if (attackExecutor != null)
        {
            attackExecutor.OnAttackExecuted += HandleAttackExecuted;
            attackExecutor.OnAttackStarted += HandleAttackStarted;
            attackExecutor.OnAttackCompleted += HandleAttackCompleted;
        }
        
        // Subscribe to selection events
        if (selectionManager != null)
        {
            selectionManager.OnObjectSelected += HandleUnitSelected;
            selectionManager.OnObjectDeselected += HandleUnitDeselected;
        }
        
        // Subscribe to death events
        if (deathHandler != null)
        {
            deathHandler.OnUnitDeath += HandleUnitDeath;
        }
        
        if (enableDebugging)
        {
            Debug.Log("CombatVisualManager: Event subscriptions complete");
        }
    }
    
    #region Event Handlers
    
    /// <summary>
    /// Handles unit health changed events
    /// </summary>
    private void HandleHealthChanged(Unit unit, int oldHealth, int newHealth)
    {
        if (logVisualEvents)
        {
            Debug.Log($"CombatVisualManager: Health changed - {unit.name}: {oldHealth} → {newHealth}");
        }
        
        // Update health bar
        if (enableHealthBars && healthBarManager != null)
        {
            healthBarManager.UpdateHealthBar(unit, newHealth);
        }
        
        OnVisualEffectTriggered?.Invoke($"HealthChanged_{unit.name}");
    }
    
    /// <summary>
    /// Handles unit damaged events
    /// </summary>
    private void HandleUnitDamaged(Unit unit, int damage, IAttacker attacker)
    {
        if (logVisualEvents)
        {
            Debug.Log($"CombatVisualManager: Unit damaged - {unit.name} took {damage} damage");
        }
        
        // Trigger damage visual effects
        if (enableAttackEffects && attackEffectManager != null)
        {
            // Check if unit implements IAttackable interface
            IAttackable attackableUnit = unit.GetComponent<IAttackable>() ?? unit as IAttackable;
            if (attackableUnit != null)
            {
                attackEffectManager.PlayDamageEffect(attackableUnit, damage);
            }
        }
        
        OnVisualEffectTriggered?.Invoke($"UnitDamaged_{unit.name}");
    }
    
    /// <summary>
    /// Handles unit died events
    /// </summary>
    private void HandleUnitDied(Unit unit, IAttacker killer)
    {
        if (logVisualEvents)
        {
            Debug.Log($"CombatVisualManager: Unit died - {unit.name}");
        }
        
        // Trigger death animation
        if (enableDeathAnimations && deathAnimationController != null)
        {
            deathAnimationController.PlayDeathAnimation(unit);
        }
        
        OnVisualEffectTriggered?.Invoke($"UnitDied_{unit.name}");
    }
    
    /// <summary>
    /// Handles unit death events
    /// </summary>
    private void HandleUnitDeath(Unit unit, IAttacker killer)
    {
        if (logVisualEvents)
        {
            Debug.Log($"CombatVisualManager: Processing unit death - {unit.name}");
        }
        
        // Clean up visual elements
        if (enableHealthBars && healthBarManager != null)
        {
            healthBarManager.RemoveHealthBar(unit);
        }
    }
    
    /// <summary>
    /// Handles attack executed events
    /// </summary>
    private void HandleAttackExecuted(IAttacker attacker, IAttackable target, int damage)
    {
        if (logVisualEvents)
        {
            Debug.Log($"CombatVisualManager: Attack executed - {attacker.GetDisplayInfo()} → {target.GetDisplayInfo()}, Damage: {damage}");
        }
        
        // Create attack result for visual effects
        AttackResult result = AttackResult.Success(damage, "Attack successful");
        
        // Trigger attack visual effects
        if (enableAttackEffects && attackEffectManager != null)
        {
            attackEffectManager.PlayAttackEffect(attacker, target, result);
        }
        
        OnVisualEffectTriggered?.Invoke($"AttackExecuted_{attacker.GetDisplayInfo()}");
    }
    
    /// <summary>
    /// Handles attack started events
    /// </summary>
    private void HandleAttackStarted(IAttacker attacker, IAttackable target)
    {
        if (logVisualEvents)
        {
            Debug.Log($"CombatVisualManager: Attack started - {attacker.GetDisplayInfo()}");
        }
        
        // Show line of sight visualization
        if (enableLOSVisualization && lineOfSightVisualizer != null)
        {
            lineOfSightVisualizer.ShowAttackLine(attacker, target);
        }
    }
    
    /// <summary>
    /// Handles attack completed events
    /// </summary>
    private void HandleAttackCompleted(IAttacker attacker, IAttackable target)
    {
        if (logVisualEvents)
        {
            Debug.Log($"CombatVisualManager: Attack completed - {attacker?.GetDisplayInfo() ?? "null"} → {target?.GetDisplayInfo() ?? "null"}");
        }
        
        // Hide line of sight visualization
        if (enableLOSVisualization && lineOfSightVisualizer != null)
        {
            lineOfSightVisualizer.HideAttackLine();
        }
    }
    
    /// <summary>
    /// Handles unit selected events
    /// </summary>
    private void HandleUnitSelected(ISelectable selectable)
    {
        if (selectable is Unit unit)
        {
            if (logVisualEvents)
            {
                Debug.Log($"CombatVisualManager: Unit selected - {unit.name}");
            }
            
            // Show combat state indicators
            if (enableCombatIndicators && combatStateIndicatorManager != null)
            {
                combatStateIndicatorManager.ShowCombatIndicators(unit);
            }
            
            OnVisualEffectTriggered?.Invoke($"UnitSelected_{unit.name}");
        }
    }
    
    /// <summary>
    /// Handles unit deselected events
    /// </summary>
    private void HandleUnitDeselected(ISelectable selectable)
    {
        if (selectable is Unit unit)
        {
            if (logVisualEvents)
            {
                Debug.Log($"CombatVisualManager: Unit deselected - {unit.name}");
            }
            
            // Hide combat state indicators
            if (enableCombatIndicators && combatStateIndicatorManager != null)
            {
                combatStateIndicatorManager.HideCombatIndicators(unit);
            }
        }
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Manually triggers a visual effect
    /// </summary>
    public void TriggerVisualEffect(string effectName, Vector3 position, float duration = 1.0f)
    {
        if (!IsInitialized) return;
        
        if (effectPoolManager != null && enableEffectPooling)
        {
            GameObject effect = effectPoolManager.GetPooledEffect(effectName);
            if (effect != null)
            {
                effect.transform.position = position;
                effect.SetActive(true);
                StartCoroutine(ReturnEffectToPool(effect, duration));
            }
        }
        
        OnVisualEffectTriggered?.Invoke(effectName);
        
        if (logVisualEvents)
        {
            Debug.Log($"CombatVisualManager: Manual effect triggered - {effectName} at {position}");
        }
    }
    
    /// <summary>
    /// Updates visual settings at runtime
    /// </summary>
    public void UpdateVisualSettings(bool healthBars, bool attackEffects, bool deathAnimations, bool losVisualization, bool combatIndicators)
    {
        enableHealthBars = healthBars;
        enableAttackEffects = attackEffects;
        enableDeathAnimations = deathAnimations;
        enableLOSVisualization = losVisualization;
        enableCombatIndicators = combatIndicators;
        
        // Reinitialize systems if needed
        if (IsInitialized)
        {
            InitializeVisualSystems();
        }
        
        if (enableDebugging)
        {
            Debug.Log("CombatVisualManager: Visual settings updated");
        }
    }
    
    /// <summary>
    /// Gets performance statistics
    /// </summary>
    public VisualPerformanceStats GetPerformanceStats()
    {
        return new VisualPerformanceStats
        {
            activeEffectsCount = activeEffectsCount,
            poolUtilization = EffectPoolUtilization,
            maxEffectsAllowed = maxParticleSystemsActive,
            effectPoolSize = effectPoolSize,
            systemsEnabled = GetEnabledSystemsCount()
        };
    }
    
    /// <summary>
    /// Forces cleanup of all visual effects
    /// </summary>
    public void CleanupAllEffects()
    {
        if (effectPoolManager != null)
        {
            effectPoolManager.ReturnAllEffectsToPool();
        }
        
        activeEffectsCount = 0;
        
        if (enableDebugging)
        {
            Debug.Log("CombatVisualManager: All effects cleaned up");
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Gets or creates a component on this GameObject
    /// </summary>
    private T GetOrCreateComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }
        return component;
    }
    
    /// <summary>
    /// Returns effect to pool after duration
    /// </summary>
    private IEnumerator ReturnEffectToPool(GameObject effect, float duration)
    {
        activeEffectsCount++;
        yield return new WaitForSeconds(duration);
        
        if (effectPoolManager != null)
        {
            effectPoolManager.ReturnEffectToPool(effect);
        }
        
        activeEffectsCount--;
    }
    
    /// <summary>
    /// Updates performance statistics
    /// </summary>
    private void UpdatePerformanceStats()
    {
        float utilization = EffectPoolUtilization;
        OnPerformanceUpdate?.Invoke(utilization);
        
        if (utilization > 0.8f && enableDebugging)
        {
            Debug.LogWarning($"CombatVisualManager: High effect pool utilization: {utilization:P0}");
        }
    }
    
    /// <summary>
    /// Gets count of enabled visual systems
    /// </summary>
    private int GetEnabledSystemsCount()
    {
        int count = 0;
        if (enableHealthBars) count++;
        if (enableAttackEffects) count++;
        if (enableDeathAnimations) count++;
        if (enableLOSVisualization) count++;
        if (enableCombatIndicators) count++;
        return count;
    }
    
    #endregion
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Unsubscribe from events
        if (healthManager != null)
        {
            healthManager.OnUnitHealthChanged -= HandleHealthChanged;
            healthManager.OnUnitDamaged -= HandleUnitDamaged;
            healthManager.OnUnitDied -= HandleUnitDied;
        }
        
        if (attackExecutor != null)
        {
            attackExecutor.OnAttackExecuted -= HandleAttackExecuted;
            attackExecutor.OnAttackStarted -= HandleAttackStarted;
            attackExecutor.OnAttackCompleted -= HandleAttackCompleted;
        }
        
        if (selectionManager != null)
        {
            selectionManager.OnObjectSelected -= HandleUnitSelected;
            selectionManager.OnObjectDeselected -= HandleUnitDeselected;
        }
        
        if (deathHandler != null)
        {
            deathHandler.OnUnitDeath -= HandleUnitDeath;
        }
        
        // Clear event references
        OnVisualEffectTriggered = null;
        OnPerformanceUpdate = null;
        
        if (enableDebugging)
        {
            Debug.Log("CombatVisualManager destroyed - Visual feedback cleanup complete");
        }
    }
}

/// <summary>
/// Performance statistics for visual effects
/// </summary>
[System.Serializable]
public struct VisualPerformanceStats
{
    public int activeEffectsCount;
    public float poolUtilization;
    public int maxEffectsAllowed;
    public int effectPoolSize;
    public int systemsEnabled;
    
    public override string ToString()
    {
        return $"Visual Performance - Active: {activeEffectsCount}/{maxEffectsAllowed}, " +
               $"Pool: {poolUtilization:P0}, Systems: {systemsEnabled}";
    }
}