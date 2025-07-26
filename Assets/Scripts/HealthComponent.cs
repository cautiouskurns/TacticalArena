using UnityEngine;
using System.Collections;

/// <summary>
/// Individual unit health tracking component for tactical combat.
/// Manages health points, damage reception, healing, and death state for a single unit.
/// Integrates with HealthManager and combat system for complete health management.
/// Part of Task 2.1.3 - Health & Damage System.
/// </summary>
public class HealthComponent : MonoBehaviour
{
    [Header("Health Configuration")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth = 3;
    [SerializeField] private bool canBeHealed = true;
    [SerializeField] private bool canTakeDamage = true;
    
    [Header("Death Configuration")]
    [SerializeField] private bool destroyOnDeath = false;
    [SerializeField] private float deathDelay = 0.5f;
    [SerializeField] private bool preventActionsWhenDead = true;
    [SerializeField] private bool showDeathEffects = true;
    
    [Header("Damage Resistance")]
    [SerializeField] private bool hasDamageResistance = false;
    [SerializeField] private float damageResistancePercentage = 0.0f;
    [SerializeField] private int minimumDamageThreshold = 0;
    
    [Header("Regeneration")]
    [SerializeField] private bool hasHealthRegeneration = false;
    [SerializeField] private float regenRate = 0.5f;
    [SerializeField] private float regenInterval = 2.0f;
    [SerializeField] private int maxRegenHealth = -1; // -1 means regen to full
    
    [Header("Visual Feedback")]
    [SerializeField] private bool enableHealthVisualization = true;
    [SerializeField] private bool showDamageNumbers = true;
    [SerializeField] private bool enableHealthBarUpdates = true;
    [SerializeField] private GameObject deathEffect;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableHealthDebugging = true;
    [SerializeField] private bool logHealthChanges = true;
    [SerializeField] private bool logDamageCalculations = true;
    
    // Component references
    private Unit unit;
    private Renderer unitRenderer;
    private Collider unitCollider;
    private HealthManager healthManager;
    
    // Health state
    private bool isAlive = true;
    private bool isDying = false;
    private float lastRegenTime = 0f;
    private int healthBeforeDamage = 0;
    
    // Visual state
    private Color originalColor;
    private bool hasOriginalColor = false;
    
    // Events
    public System.Action<int, int> OnHealthChanged; // oldHealth, newHealth
    public System.Action<int, IAttacker> OnDamaged; // damage, attacker
    public System.Action<int> OnHealed; // healAmount
    public System.Action<IAttacker> OnDied; // killer
    public System.Action OnHealthRestored; // when fully healed
    public System.Action<float> OnHealthPercentageChanged; // healthPercentage (0-1)
    
    // Properties
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsAlive => isAlive && !isDying;
    public bool IsDying => isDying;
    public bool CanBeHealed => canBeHealed && isAlive;
    public bool CanTakeDamage => canTakeDamage && isAlive;
    public float HealthPercentage => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    public bool IsFullHealth => currentHealth >= maxHealth;
    public bool IsCriticalHealth => HealthPercentage <= 0.25f;
    public bool IsLowHealth => HealthPercentage <= 0.5f;
    
    void Awake()
    {
        InitializeHealthComponent();
    }
    
    void Start()
    {
        FindComponentReferences();
        RegisterWithHealthManager();
        SetupInitialState();
        StartHealthRegenerationIfEnabled();
    }
    
    void Update()
    {
        // DISABLED FOR DEBUGGING - NO HEALTH REGENERATION
        // if (hasHealthRegeneration && isAlive && !isDying)
        // {
        //     Debug.Log($"⚠️ HealthComponent: {gameObject.name} processing health regeneration in Update()");
        //     ProcessHealthRegeneration();
        // }
    }
    
    /// <summary>
    /// Initializes the health component
    /// </summary>
    private void InitializeHealthComponent()
    {
        // Ensure health values are valid
        if (maxHealth <= 0)
        {
            maxHealth = 3; // Default tactical arena health
        }
        
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        
        if (currentHealth <= 0)
        {
            currentHealth = maxHealth; // Start at full health
        }
        
        isAlive = currentHealth > 0;
        
        if (enableHealthDebugging)
        {
            Debug.Log($"HealthComponent initialized for {gameObject.name} - {currentHealth}/{maxHealth} HP");
        }
    }
    
    /// <summary>
    /// Finds references to other components
    /// </summary>
    private void FindComponentReferences()
    {
        unit = GetComponent<Unit>();
        unitRenderer = GetComponent<Renderer>();
        unitCollider = GetComponent<Collider>();
        healthManager = FindFirstObjectByType<HealthManager>();
        
        if (unitRenderer != null && !hasOriginalColor)
        {
            originalColor = unitRenderer.material.color;
            hasOriginalColor = true;
        }
        
        if (enableHealthDebugging)
        {
            Debug.Log($"HealthComponent found references - Unit: {unit != null}, " +
                     $"Renderer: {unitRenderer != null}, Health Manager: {healthManager != null}");
        }
    }
    
    /// <summary>
    /// Registers this component with the health manager
    /// </summary>
    private void RegisterWithHealthManager()
    {
        if (healthManager != null && unit != null)
        {
            healthManager.RegisterUnit(unit);
        }
    }
    
    /// <summary>
    /// Sets up initial health state
    /// </summary>
    private void SetupInitialState()
    {
        UpdateHealthVisualization();
        TriggerHealthPercentageChanged();
    }
    
    /// <summary>
    /// Starts health regeneration coroutine if enabled
    /// </summary>
    private void StartHealthRegenerationIfEnabled()
    {
        if (hasHealthRegeneration)
        {
            StartCoroutine(HealthRegenerationCoroutine());
        }
    }
    
    #region Public Health Interface
    
    /// <summary>
    /// Initializes health with specific values (called by HealthManager)
    /// </summary>
    public void Initialize(int initialMaxHealth, int initialCurrentHealth = -1)
    {
        Debug.Log($"⚠️ HealthComponent: {gameObject.name} Initialize() called with {initialMaxHealth}/{initialCurrentHealth} - STACK TRACE:");
        Debug.Log(System.Environment.StackTrace);
        
        maxHealth = initialMaxHealth;
        currentHealth = initialCurrentHealth >= 0 ? initialCurrentHealth : initialMaxHealth;
        
        // Ensure current health doesn't exceed max
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        
        isAlive = currentHealth > 0;
        
        UpdateHealthVisualization();
        TriggerHealthPercentageChanged();
        
        if (logHealthChanges)
        {
            Debug.Log($"HealthComponent initialized {gameObject.name} with {currentHealth}/{maxHealth} HP");
        }
    }
    
    /// <summary>
    /// Takes damage from an attacker
    /// </summary>
    public int TakeDamage(int damage, IAttacker attacker)
    {
        if (!CanTakeDamage || damage <= 0)
        {
            if (logDamageCalculations)
            {
                Debug.Log($"HealthComponent: {gameObject.name} cannot take damage or damage is 0");
            }
            return 0;
        }
        
        healthBeforeDamage = currentHealth;
        
        // Apply damage resistance if enabled
        int actualDamage = CalculateActualDamage(damage);
        
        // Apply damage
        int oldHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - actualDamage);
        
        // Update alive state
        if (currentHealth <= 0 && isAlive)
        {
            HandleDeath(attacker);
        }
        
        // Trigger events
        OnHealthChanged?.Invoke(oldHealth, currentHealth);
        OnDamaged?.Invoke(actualDamage, attacker);
        TriggerHealthPercentageChanged();
        
        // Update visual feedback
        UpdateHealthVisualization();
        ShowDamageEffect(actualDamage);
        
        if (logHealthChanges)
        {
            Debug.Log($"HealthComponent: {gameObject.name} took {actualDamage} damage ({oldHealth} → {currentHealth})");
        }
        
        return actualDamage;
    }
    
    /// <summary>
    /// Heals the unit by the specified amount
    /// </summary>
    public int Heal(int healAmount)
    {
        Debug.Log($"⚠️ HealthComponent: {gameObject.name} Heal({healAmount}) called - STACK TRACE:");
        Debug.Log(System.Environment.StackTrace);
        
        if (!CanBeHealed || healAmount <= 0)
        {
            return 0;
        }
        
        int oldHealth = currentHealth;
        int maxHealTo = maxRegenHealth > 0 ? Mathf.Min(maxHealth, maxRegenHealth) : maxHealth;
        
        currentHealth = Mathf.Min(maxHealTo, currentHealth + healAmount);
        int actualHealing = currentHealth - oldHealth;
        
        // Trigger events
        if (actualHealing > 0)
        {
            OnHealthChanged?.Invoke(oldHealth, currentHealth);
            OnHealed?.Invoke(actualHealing);
            TriggerHealthPercentageChanged();
            
            if (IsFullHealth)
            {
                OnHealthRestored?.Invoke();
            }
            
            // Update visual feedback
            UpdateHealthVisualization();
            ShowHealingEffect(actualHealing);
            
            if (logHealthChanges)
            {
                Debug.Log($"HealthComponent: {gameObject.name} healed for {actualHealing} HP ({oldHealth} → {currentHealth})");
            }
        }
        
        return actualHealing;
    }
    
    /// <summary>
    /// Sets current health directly
    /// </summary>
    public void SetCurrentHealth(int newHealth)
    {
        int oldHealth = currentHealth;
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        
        // Update alive state
        bool wasAlive = isAlive;
        isAlive = currentHealth > 0;
        
        if (wasAlive && !isAlive)
        {
            HandleDeath(null);
        }
        else if (!wasAlive && isAlive)
        {
            HandleResurrection();
        }
        
        // Trigger events
        OnHealthChanged?.Invoke(oldHealth, currentHealth);
        TriggerHealthPercentageChanged();
        
        // Update visual feedback
        UpdateHealthVisualization();
        
        // ALWAYS log health changes for debugging
        Debug.Log($"⚠️ HealthComponent: {gameObject.name} health DIRECTLY SET to {currentHealth} (was {oldHealth}) - STACK TRACE:");
        Debug.Log(System.Environment.StackTrace);
    }
    
    /// <summary>
    /// Sets maximum health and optionally adjusts current health
    /// </summary>
    public void SetMaxHealth(int newMaxHealth, bool adjustCurrentHealth = true)
    {
        if (newMaxHealth <= 0)
        {
            Debug.LogWarning($"HealthComponent: Attempted to set invalid max health {newMaxHealth} for {gameObject.name}");
            return;
        }
        
        int oldMaxHealth = maxHealth;
        maxHealth = newMaxHealth;
        
        if (adjustCurrentHealth)
        {
            // Maintain health percentage
            float healthPercentage = oldMaxHealth > 0 ? (float)currentHealth / oldMaxHealth : 1f;
            SetCurrentHealth(Mathf.RoundToInt(maxHealth * healthPercentage));
        }
        else if (currentHealth > maxHealth)
        {
            // Clamp current health to new maximum
            SetCurrentHealth(maxHealth);
        }
        
        if (logHealthChanges)
        {
            Debug.Log($"HealthComponent: {gameObject.name} max health changed from {oldMaxHealth} to {maxHealth}");
        }
    }
    
    /// <summary>
    /// Restores to full health
    /// </summary>
    public void RestoreToFullHealth()
    {
        Debug.Log($"⚠️ HealthComponent: {gameObject.name} RestoreToFullHealth() called - STACK TRACE:");
        Debug.Log(System.Environment.StackTrace);
        
        SetCurrentHealth(maxHealth);
        
        if (logHealthChanges)
        {
            Debug.Log($"HealthComponent: {gameObject.name} restored to full health");
        }
    }
    
    /// <summary>
    /// Kills this unit instantly
    /// </summary>
    public void Kill(IAttacker killer = null)
    {
        if (!isAlive) return;
        
        int oldHealth = currentHealth;
        currentHealth = 0;
        
        HandleDeath(killer);
        
        // Trigger events
        OnHealthChanged?.Invoke(oldHealth, currentHealth);
        TriggerHealthPercentageChanged();
        
        // Update visual feedback
        UpdateHealthVisualization();
        
        if (logHealthChanges)
        {
            Debug.Log($"HealthComponent: {gameObject.name} killed by {killer?.GetDisplayInfo() ?? "unknown"}");
        }
    }
    
    #endregion
    
    #region Private Health Logic
    
    /// <summary>
    /// Calculates actual damage after resistances
    /// </summary>
    private int CalculateActualDamage(int incomingDamage)
    {
        float actualDamage = incomingDamage;
        
        // Apply damage resistance
        if (hasDamageResistance && damageResistancePercentage > 0)
        {
            actualDamage *= (1f - damageResistancePercentage);
        }
        
        // Apply minimum damage threshold
        int finalDamage = Mathf.RoundToInt(actualDamage);
        if (finalDamage < minimumDamageThreshold)
        {
            finalDamage = 0;
        }
        
        if (logDamageCalculations)
        {
            Debug.Log($"HealthComponent: {gameObject.name} damage calculation: {incomingDamage} → {finalDamage} " +
                     $"(resistance: {damageResistancePercentage:P0}, threshold: {minimumDamageThreshold})");
        }
        
        return finalDamage;
    }
    
    /// <summary>
    /// Handles unit death
    /// </summary>
    private void HandleDeath(IAttacker killer)
    {
        if (isDying) return; // Already dying
        
        isAlive = false;
        isDying = true;
        
        // Trigger death event
        OnDied?.Invoke(killer);
        
        // Start death sequence
        StartCoroutine(DeathSequenceCoroutine(killer));
        
        if (enableHealthDebugging)
        {
            Debug.Log($"HealthComponent: {gameObject.name} died, killed by {killer?.GetDisplayInfo() ?? "unknown"}");
        }
    }
    
    /// <summary>
    /// Handles unit resurrection (if health is restored from 0)
    /// </summary>
    private void HandleResurrection()
    {
        isAlive = true;
        isDying = false;
        
        // Restore visual state
        if (unitRenderer != null && hasOriginalColor)
        {
            unitRenderer.material.color = originalColor;
        }
        
        if (unitCollider != null)
        {
            unitCollider.enabled = true;
        }
        
        if (enableHealthDebugging)
        {
            Debug.Log($"HealthComponent: {gameObject.name} resurrected");
        }
    }
    
    /// <summary>
    /// Processes health regeneration
    /// </summary>
    private void ProcessHealthRegeneration()
    {
        if (Time.time - lastRegenTime < regenInterval) return;
        if (IsFullHealth) return;
        
        int maxHealTo = maxRegenHealth > 0 ? Mathf.Min(maxHealth, maxRegenHealth) : maxHealth;
        if (currentHealth >= maxHealTo) return;
        
        int regenAmount = Mathf.RoundToInt(regenRate);
        if (regenAmount > 0)
        {
            Heal(regenAmount);
            lastRegenTime = Time.time;
        }
    }
    
    /// <summary>
    /// Triggers health percentage changed event
    /// </summary>
    private void TriggerHealthPercentageChanged()
    {
        OnHealthPercentageChanged?.Invoke(HealthPercentage);
    }
    
    #endregion
    
    #region Visual Feedback
    
    /// <summary>
    /// Updates health visualization
    /// </summary>
    private void UpdateHealthVisualization()
    {
        if (!enableHealthVisualization || unitRenderer == null || !hasOriginalColor)
        {
            return;
        }
        
        if (!isAlive)
        {
            // Death visualization
            unitRenderer.material.color = Color.gray;
        }
        else if (IsCriticalHealth)
        {
            // Critical health visualization
            unitRenderer.material.color = Color.Lerp(originalColor, Color.red, 0.7f);
        }
        else if (IsLowHealth)
        {
            // Low health visualization
            unitRenderer.material.color = Color.Lerp(originalColor, Color.yellow, 0.5f);
        }
        else
        {
            // Normal health visualization
            unitRenderer.material.color = originalColor;
        }
        
        // Update health bar if integration is enabled
        if (enableHealthBarUpdates)
        {
            UpdateHealthBarDisplay();
        }
    }
    
    /// <summary>
    /// Shows damage effect
    /// </summary>
    private void ShowDamageEffect(int damage)
    {
        if (!showDamageNumbers) return;
        
        // This could spawn floating damage numbers or other visual effects
        if (enableHealthDebugging)
        {
            Debug.Log($"Damage effect: -{damage} on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Shows healing effect
    /// </summary>
    private void ShowHealingEffect(int healAmount)
    {
        if (!showDamageNumbers) return;
        
        // This could spawn floating healing numbers or other visual effects
        if (enableHealthDebugging)
        {
            Debug.Log($"Healing effect: +{healAmount} on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Updates health bar display (placeholder for UI integration)
    /// </summary>
    private void UpdateHealthBarDisplay()
    {
        // This will be implemented in Task 2.1.4 - Health Bar UI System
        // For now, just log the health percentage
        if (enableHealthDebugging)
        {
            Debug.Log($"Health bar update: {gameObject.name} at {HealthPercentage:P0} health");
        }
    }
    
    #endregion
    
    #region Coroutines
    
    /// <summary>
    /// Death sequence coroutine
    /// </summary>
    private IEnumerator DeathSequenceCoroutine(IAttacker killer)
    {
        // Play death effects
        if (showDeathEffects && deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, transform.rotation);
            Destroy(effect, 2f);
        }
        
        // Disable interactions
        if (preventActionsWhenDead)
        {
            if (unitCollider != null)
            {
                unitCollider.enabled = false;
            }
        }
        
        // Wait for death delay
        yield return new WaitForSeconds(deathDelay);
        
        // Destroy if configured
        if (destroyOnDeath)
        {
            if (healthManager != null && unit != null)
            {
                healthManager.UnregisterUnit(unit);
            }
            
            Destroy(gameObject);
        }
        else
        {
            isDying = false; // Allow for potential resurrection
        }
    }
    
    /// <summary>
    /// Health regeneration coroutine
    /// </summary>
    private IEnumerator HealthRegenerationCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(regenInterval);
            
            if (hasHealthRegeneration && isAlive && !isDying && !IsFullHealth)
            {
                int maxHealTo = maxRegenHealth > 0 ? Mathf.Min(maxHealth, maxRegenHealth) : maxHealth;
                if (currentHealth < maxHealTo)
                {
                    int regenAmount = Mathf.RoundToInt(regenRate);
                    if (regenAmount > 0)
                    {
                        Heal(regenAmount);
                    }
                }
            }
        }
    }
    
    #endregion
    
    #region Debug and Utility
    
    /// <summary>
    /// Gets health status for debugging
    /// </summary>
    public string GetHealthStatus()
    {
        return $"{gameObject.name}: {currentHealth}/{maxHealth} HP ({HealthPercentage:P0}) - " +
               $"Alive: {isAlive}, Dying: {isDying}";
    }
    
    /// <summary>
    /// Validates health component state
    /// </summary>
    public bool ValidateHealthState()
    {
        bool isValid = true;
        
        if (maxHealth <= 0)
        {
            Debug.LogError($"HealthComponent: {gameObject.name} has invalid max health: {maxHealth}");
            isValid = false;
        }
        
        if (currentHealth < 0 || currentHealth > maxHealth)
        {
            Debug.LogError($"HealthComponent: {gameObject.name} has invalid current health: {currentHealth}/{maxHealth}");
            isValid = false;
        }
        
        if (currentHealth > 0 && !isAlive)
        {
            Debug.LogError($"HealthComponent: {gameObject.name} has health but is marked as dead");
            isValid = false;
        }
        
        if (currentHealth <= 0 && isAlive)
        {
            Debug.LogError($"HealthComponent: {gameObject.name} has no health but is marked as alive");
            isValid = false;
        }
        
        return isValid;
    }
    
    #endregion
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Unregister from health manager
        if (healthManager != null && unit != null)
        {
            healthManager.UnregisterUnit(unit);
        }
        
        // Clear event references
        OnHealthChanged = null;
        OnDamaged = null;
        OnHealed = null;
        OnDied = null;
        OnHealthRestored = null;
        OnHealthPercentageChanged = null;
    }
}