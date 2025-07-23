using UnityEngine;

/// <summary>
/// Component that provides IAttackable capability to units in the tactical combat system.
/// Handles damage reception, health management, and integration with Unit component.
/// Part of the combat system foundation implementing the IAttackable interface.
/// </summary>
[RequireComponent(typeof(Unit))]
public class TargetCapability : MonoBehaviour, IAttackable
{
    [Header("Health Configuration")]
    [SerializeField] private int currentHealth = 3;
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private bool canBeTargeted = true;
    [SerializeField] private bool isInvulnerable = false;
    
    [Header("Damage Settings")]
    [SerializeField] private bool enableDamageReduction = false;
    [SerializeField] private int damageReduction = 0;
    [SerializeField] private bool enableDamageReflection = false;
    [SerializeField] private float damageReflectionPercent = 0f;
    
    [Header("Status Effects")]
    [SerializeField] private bool isAlive = true;
    [SerializeField] private bool canDie = true;
    [SerializeField] private float invulnerabilityDuration = 0f;
    
    [Header("Target Validation")]
    [SerializeField] private bool validateAttackerTeam = true;
    [SerializeField] private bool validateAttackerState = true;
    [SerializeField] private bool validateSelfState = true;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableTargetLogging = true;
    
    // Component references
    private Unit unit;
    private UnitHealth unitHealth;
    
    // Target state tracking
    private float invulnerabilityTimer = 0f;
    private int totalDamageTaken = 0;
    private IAttacker lastAttacker;
    
    // Events
    public System.Action<IAttacker, int> OnDamageReceived;
    public System.Action<IAttacker, int> OnHealthChanged;
    public System.Action<IAttacker> OnTargetDeath;
    public System.Action<IAttacker, int> OnDamageReduced;
    public System.Action<IAttacker, int> OnDamageReflected;
    
    // IAttackable interface properties
    public Transform Transform => transform;
    public Vector2Int GridPosition => unit != null ? unit.GridPosition : Vector2Int.zero;
    public UnitTeam Team => unit != null ? unit.Team : UnitTeam.Neutral;
    public int CurrentHealth => unitHealth != null ? unitHealth.CurrentHealth : currentHealth;
    public int MaxHealth => unitHealth != null ? unitHealth.MaxHealth : maxHealth;
    public bool CanBeTargeted => canBeTargeted && IsAlive && !IsInvulnerable;
    public bool IsAlive => unitHealth != null ? unitHealth.IsAlive : (isAlive && currentHealth > 0);
    
    // Additional properties
    public bool IsInvulnerable => isInvulnerable || invulnerabilityTimer > 0f;
    public float HealthPercentage => unitHealth != null ? unitHealth.HealthPercentage : (maxHealth > 0 ? (float)currentHealth / maxHealth : 0f);
    public int TotalDamageTaken => totalDamageTaken;
    public IAttacker LastAttacker => lastAttacker;
    
    void Awake()
    {
        InitializeTargetCapability();
    }
    
    void Start()
    {
        FindComponentReferences();
        ValidateInitialState();
    }
    
    void Update()
    {
        UpdateInvulnerabilityTimer();
    }
    
    /// <summary>
    /// Initializes the target capability
    /// </summary>
    private void InitializeTargetCapability()
    {
        // Ensure health values are valid
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        isAlive = currentHealth > 0;
        
        if (enableTargetLogging)
        {
            Debug.Log($"TargetCapability initialized for {gameObject.name} - Health: {currentHealth}/{maxHealth}");
        }
    }
    
    /// <summary>
    /// Finds required component references
    /// </summary>
    private void FindComponentReferences()
    {
        unit = GetComponent<Unit>();
        if (unit == null)
        {
            Debug.LogError($"TargetCapability: Unit component not found on {gameObject.name}");
        }
        
        unitHealth = GetComponent<UnitHealth>();
        if (unitHealth == null)
        {
            Debug.LogError($"TargetCapability: UnitHealth component not found on {gameObject.name}");
        }
        
        // Team is handled by Unit component - no additional setup needed
    }
    
    /// <summary>
    /// Validates initial state consistency
    /// </summary>
    private void ValidateInitialState()
    {
        if (maxHealth <= 0)
        {
            Debug.LogWarning($"TargetCapability: {gameObject.name} has invalid max health ({maxHealth}). Setting to 1.");
            maxHealth = 1;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }
        
        if (currentHealth <= 0 && isAlive)
        {
            Debug.LogWarning($"TargetCapability: {gameObject.name} has no health but is marked as alive. Fixing state.");
            isAlive = false;
        }
    }
    
    /// <summary>
    /// Updates invulnerability timer
    /// </summary>
    private void UpdateInvulnerabilityTimer()
    {
        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer < 0f)
            {
                invulnerabilityTimer = 0f;
            }
        }
    }
    
    /// <summary>
    /// Receives damage from an attack (IAttackable interface)
    /// </summary>
    public int TakeDamage(int damage, IAttacker attacker)
    {
        if (!CanBeTargeted || damage <= 0)
        {
            return 0;
        }
        
        lastAttacker = attacker;
        int originalDamage = damage;
        
        // Apply damage reduction
        if (enableDamageReduction && damageReduction > 0)
        {
            int reducedAmount = Mathf.Min(damage, damageReduction);
            damage -= reducedAmount;
            
            if (reducedAmount > 0)
            {
                OnDamageReduced?.Invoke(attacker, reducedAmount);
                
                if (enableTargetLogging)
                {
                    Debug.Log($"TargetCapability: {gameObject.name} reduced {reducedAmount} damage");
                }
            }
        }
        
        // Delegate to UnitHealth if available
        int actualDamage = 0;
        if (unitHealth != null)
        {
            // Use UnitHealth's damage system
            int healthBefore = unitHealth.CurrentHealth;
            unitHealth.TakeDamage(damage);
            actualDamage = healthBefore - unitHealth.CurrentHealth;
            
            // Update internal tracking to stay in sync
            currentHealth = unitHealth.CurrentHealth;
            isAlive = unitHealth.IsAlive;
        }
        else
        {
            // Fallback to internal health tracking
            actualDamage = Mathf.Min(damage, currentHealth);
            currentHealth -= actualDamage;
            
            // Check for death
            if (currentHealth <= 0 && canDie)
            {
                currentHealth = 0;
                isAlive = false;
            }
        }
        
        totalDamageTaken += actualDamage;
        
        // Trigger events
        OnDamageReceived?.Invoke(attacker, actualDamage);
        OnHealthChanged?.Invoke(attacker, CurrentHealth);
        
        // Handle damage reflection
        if (enableDamageReflection && damageReflectionPercent > 0f && attacker != null)
        {
            int reflectedDamage = Mathf.RoundToInt(actualDamage * (damageReflectionPercent / 100f));
            if (reflectedDamage > 0)
            {
                // Try to apply reflected damage to attacker if they're also attackable
                IAttackable attackableAttacker = attacker as IAttackable;
                if (attackableAttacker != null)
                {
                    attackableAttacker.TakeDamage(reflectedDamage, null); // No attacker for reflected damage
                    OnDamageReflected?.Invoke(attacker, reflectedDamage);
                    
                    if (enableTargetLogging)
                    {
                        Debug.Log($"TargetCapability: {gameObject.name} reflected {reflectedDamage} damage to {attacker.GetDisplayInfo()}");
                    }
                }
            }
        }
        
        if (enableTargetLogging)
        {
            Debug.Log($"TargetCapability: {gameObject.name} took {actualDamage} damage (from {originalDamage}) - Health: {CurrentHealth}/{MaxHealth}");
        }
        
        return actualDamage;
    }
    
    /// <summary>
    /// Called when this target is attacked (IAttackable interface)
    /// </summary>
    public void OnAttacked(IAttacker attacker, int damage)
    {
        // Add temporary invulnerability to prevent spam attacks
        if (invulnerabilityDuration > 0f)
        {
            invulnerabilityTimer = invulnerabilityDuration;
        }
        
        if (enableTargetLogging)
        {
            Debug.Log($"TargetCapability: {gameObject.name} was attacked by {attacker?.GetDisplayInfo() ?? "unknown"} for {damage} damage");
        }
    }
    
    /// <summary>
    /// Called when this target dies from damage (IAttackable interface)
    /// </summary>
    public void OnDeath(IAttacker killer)
    {
        if (!isAlive) return; // Already dead
        
        isAlive = false;
        canBeTargeted = false;
        
        // Notify unit component of death
        if (unit != null)
        {
            unit.HandleDeath();
        }
        
        OnTargetDeath?.Invoke(killer);
        
        if (enableTargetLogging)
        {
            Debug.Log($"TargetCapability: {gameObject.name} was killed by {killer?.GetDisplayInfo() ?? "unknown"}");
        }
    }
    
    /// <summary>
    /// Validates if this target can be attacked by the specified attacker (IAttackable interface)
    /// </summary>
    public TargetValidationResult ValidateAsTarget(IAttacker attacker)
    {
        if (attacker == null)
        {
            return TargetValidationResult.Invalid("Attacker is null", attacker, this);
        }
        
        // Self state validation
        if (validateSelfState)
        {
            if (!CanBeTargeted)
            {
                string reason = !canBeTargeted ? "Target is not targetable" :
                               !isAlive ? "Target is dead" :
                               IsInvulnerable ? "Target is invulnerable" : "Cannot be targeted";
                return TargetValidationResult.Invalid(reason, attacker, this);
            }
        }
        
        // Attacker state validation
        if (validateAttackerState)
        {
            if (!attacker.CanAttack)
            {
                return TargetValidationResult.Invalid("Attacker cannot attack", attacker, this);
            }
        }
        
        // Team validation
        if (validateAttackerTeam && unit != null)
        {
            if (unit.Team == attacker.Team)
            {
                return TargetValidationResult.Invalid("Cannot target allies", attacker, this);
            }
        }
        
        // Self-targeting check
        if (Transform == attacker.Transform)
        {
            return TargetValidationResult.Invalid("Cannot target self", attacker, this);
        }
        
        return TargetValidationResult.Valid(attacker, this);
    }
    
    /// <summary>
    /// Gets display information for this target (IAttackable interface)
    /// </summary>
    public string GetDisplayInfo()
    {
        string teamName = unit != null ? unit.Team.ToString() : "Unknown";
        return $"{gameObject.name} ({teamName} Team Target - {currentHealth}/{maxHealth} HP)";
    }
    
    /// <summary>
    /// Heals this target for the specified amount
    /// </summary>
    public int Heal(int healAmount)
    {
        if (!isAlive || healAmount <= 0)
        {
            return 0;
        }
        
        int actualHeal = Mathf.Min(healAmount, maxHealth - currentHealth);
        currentHealth += actualHeal;
        
        OnHealthChanged?.Invoke(null, currentHealth);
        
        if (enableTargetLogging)
        {
            Debug.Log($"TargetCapability: {gameObject.name} healed for {actualHeal} - Health: {currentHealth}/{maxHealth}");
        }
        
        return actualHeal;
    }
    
    /// <summary>
    /// Sets the maximum health and optionally adjusts current health
    /// </summary>
    public void SetMaxHealth(int newMaxHealth, bool adjustCurrentHealth = true)
    {
        int oldMaxHealth = maxHealth;
        maxHealth = Mathf.Max(1, newMaxHealth);
        
        if (adjustCurrentHealth)
        {
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }
        
        if (enableTargetLogging)
        {
            Debug.Log($"TargetCapability: {gameObject.name} max health changed from {oldMaxHealth} to {maxHealth}");
        }
    }
    
    /// <summary>
    /// Sets invulnerability state
    /// </summary>
    public void SetInvulnerable(bool invulnerable, float duration = 0f)
    {
        isInvulnerable = invulnerable;
        if (invulnerable && duration > 0f)
        {
            invulnerabilityTimer = duration;
        }
        
        if (enableTargetLogging)
        {
            Debug.Log($"TargetCapability: {gameObject.name} invulnerability set to {invulnerable}" + 
                     (duration > 0f ? $" for {duration}s" : ""));
        }
    }
    
    /// <summary>
    /// Sets target capability state
    /// </summary>
    public void SetTargetEnabled(bool enabled)
    {
        canBeTargeted = enabled;
        
        if (enableTargetLogging)
        {
            Debug.Log($"TargetCapability: {gameObject.name} targeting {(enabled ? "enabled" : "disabled")}");
        }
    }
    
    /// <summary>
    /// Revives this target with specified health
    /// </summary>
    public void Revive(int reviveHealth = -1)
    {
        if (isAlive) return;
        
        isAlive = true;
        canBeTargeted = true;
        currentHealth = reviveHealth > 0 ? Mathf.Min(reviveHealth, maxHealth) : maxHealth;
        
        // Notify unit component of revival
        if (unit != null)
        {
            unit.HandleRevival();
        }
        
        OnHealthChanged?.Invoke(null, currentHealth);
        
        if (enableTargetLogging)
        {
            Debug.Log($"TargetCapability: {gameObject.name} revived with {currentHealth} health");
        }
    }
    
    /// <summary>
    /// Gets target capability information for debugging
    /// </summary>
    public string GetTargetInfo()
    {
        return $"Target Info - Health: {currentHealth}/{maxHealth} ({HealthPercentage:P0}), " +
               $"Alive: {isAlive}, Can Be Targeted: {CanBeTargeted}, Invulnerable: {IsInvulnerable}, " +
               $"Total Damage Taken: {totalDamageTaken}";
    }
    
    /// <summary>
    /// Unity Gizmos for debug visualization
    /// </summary>
    void OnDrawGizmos()
    {
        if (!enableTargetLogging) return;
        
        // Draw health indicator
        float healthPercent = HealthPercentage;
        Gizmos.color = Color.Lerp(Color.red, Color.green, healthPercent);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 1.5f, Vector3.one * 0.3f);
        
        // Draw invulnerability indicator
        if (IsInvulnerable)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 1.2f);
        }
        
        // Draw death indicator
        if (!isAlive)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 1.1f);
        }
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Clear event references
        OnDamageReceived = null;
        OnHealthChanged = null;
        OnTargetDeath = null;
        OnDamageReduced = null;
        OnDamageReflected = null;
    }
}