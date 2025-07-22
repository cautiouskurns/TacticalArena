using UnityEngine;

/// <summary>
/// Component that provides IAttacker capability to units in the tactical combat system.
/// Handles attack properties, validation, and integration with Unit component.
/// Part of the combat system foundation implementing the IAttacker interface.
/// </summary>
[RequireComponent(typeof(Unit))]
public class AttackCapability : MonoBehaviour, IAttacker
{
    [Header("Attack Configuration")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int attacksPerTurn = 1;
    [SerializeField] private bool canAttackDiagonally = false;
    
    [Header("Attack State")]
    [SerializeField] private bool canAttack = true;
    [SerializeField] private bool isStunned = false;
    [SerializeField] private float attackCooldown = 0f;
    
    [Header("Attack Validation")]
    [SerializeField] private bool validateTeamRestrictions = true;
    [SerializeField] private bool validateRange = true;
    [SerializeField] private bool validateState = true;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableAttackLogging = false;
    
    // Component references
    private Unit unit;
    private CombatManager combatManager;
    
    // Attack state tracking
    private int attacksUsedThisTurn = 0;
    private float lastAttackTime = 0f;
    
    // IAttacker interface properties
    public Transform Transform => transform;
    public Vector2Int GridPosition => unit != null ? unit.GridPosition : Vector2Int.zero;
    public UnitTeam Team => unit != null ? unit.Team : UnitTeam.Neutral;
    public int AttackDamage => attackDamage;
    public float AttackRange => attackRange;
    public int AttacksPerTurn => attacksPerTurn;
    public bool CanAttackDiagonally => canAttackDiagonally;
    public bool CanAttack => canAttack && !isStunned && attackCooldown <= 0f && IsAlive;
    
    // Additional properties
    public bool IsAlive => unit != null ? unit.IsAlive : true;
    public int AttacksUsedThisTurn => attacksUsedThisTurn;
    public int AttacksRemaining => Mathf.Max(0, attacksPerTurn - attacksUsedThisTurn);
    public float AttackCooldown => attackCooldown;
    
    void Awake()
    {
        InitializeAttackCapability();
    }
    
    void Start()
    {
        FindComponentReferences();
        RegisterWithCombatManager();
    }
    
    void Update()
    {
        UpdateAttackCooldown();
    }
    
    /// <summary>
    /// Initializes the attack capability
    /// </summary>
    private void InitializeAttackCapability()
    {
        if (enableAttackLogging)
        {
            Debug.Log($"AttackCapability initialized for {gameObject.name}");
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
            Debug.LogError($"AttackCapability: Unit component not found on {gameObject.name}");
        }
        
        // Team is handled by Unit component - no additional setup needed
        
        combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager == null)
        {
            Debug.LogWarning($"AttackCapability: CombatManager not found for {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Registers this attacker with the combat manager
    /// </summary>
    private void RegisterWithCombatManager()
    {
        if (combatManager != null)
        {
            combatManager.RegisterAttacker(this);
        }
    }
    
    /// <summary>
    /// Updates attack cooldown timer
    /// </summary>
    private void UpdateAttackCooldown()
    {
        if (attackCooldown > 0f)
        {
            attackCooldown -= Time.deltaTime;
            if (attackCooldown < 0f)
            {
                attackCooldown = 0f;
            }
        }
    }
    
    /// <summary>
    /// Validates if this attacker can attack the specified target (IAttacker interface)
    /// </summary>
    public AttackValidationResult ValidateAttack(IAttackable target)
    {
        if (target == null)
        {
            return AttackValidationResult.Invalid("Target is null", this, target);
        }
        
        // Basic state validation
        if (validateState)
        {
            if (!CanAttack)
            {
                string reason = !canAttack ? "Attack capability disabled" :
                               isStunned ? "Unit is stunned" :
                               attackCooldown > 0f ? "Attack on cooldown" :
                               !IsAlive ? "Unit is not alive" : "Cannot attack";
                return AttackValidationResult.Invalid(reason, this, target);
            }
            
            // Check attack count limits
            if (attacksUsedThisTurn >= attacksPerTurn)
            {
                return AttackValidationResult.Invalid("No attacks remaining this turn", this, target);
            }
        }
        
        // Team validation
        if (validateTeamRestrictions && unit != null)
        {
            if (unit.Team == target.Team)
            {
                return AttackValidationResult.Invalid("Cannot attack allies", this, target);
            }
        }
        
        // Range validation
        if (validateRange)
        {
            float distance = Vector2.Distance(GridPosition, target.GridPosition);
            if (distance > attackRange)
            {
                return AttackValidationResult.Invalid($"Target too far away ({distance:F1} > {attackRange})", this, target);
            }
        }
        
        // Self-attack check
        if (Transform == target.Transform)
        {
            return AttackValidationResult.Invalid("Cannot attack self", this, target);
        }
        
        return AttackValidationResult.Valid(this, target);
    }
    
    /// <summary>
    /// Called when this unit performs an attack (IAttacker interface)
    /// </summary>
    public void OnAttackPerformed(IAttackable target, int damage)
    {
        // Track attack usage
        attacksUsedThisTurn++;
        lastAttackTime = Time.time;
        
        // Set cooldown based on combat manager settings
        if (combatManager != null)
        {
            AttackExecutor executor = combatManager.GetComponent<AttackExecutor>();
            if (executor != null)
            {
                attackCooldown = executor.AttackAnimationDuration;
            }
        }
        
        if (enableAttackLogging)
        {
            Debug.Log($"AttackCapability: {gameObject.name} performed attack on {target.GetDisplayInfo()} for {damage} damage");
        }
    }
    
    /// <summary>
    /// Called when this unit's attack fails (IAttacker interface)
    /// </summary>
    public void OnAttackFailed(IAttackable target, string reason)
    {
        if (enableAttackLogging)
        {
            Debug.LogWarning($"AttackCapability: {gameObject.name} attack failed on {target?.GetDisplayInfo() ?? "unknown target"} - {reason}");
        }
    }
    
    /// <summary>
    /// Gets display information for this attacker (IAttacker interface)
    /// </summary>
    public string GetDisplayInfo()
    {
        string teamName = unit != null ? unit.Team.ToString() : "Unknown";
        return $"{gameObject.name} ({teamName} Team Attacker)";
    }
    
    /// <summary>
    /// Resets attack count for a new turn
    /// </summary>
    public void ResetAttacksForNewTurn()
    {
        attacksUsedThisTurn = 0;
        
        if (enableAttackLogging)
        {
            Debug.Log($"AttackCapability: {gameObject.name} attacks reset for new turn");
        }
    }
    
    /// <summary>
    /// Sets the stun state of this attacker
    /// </summary>
    public void SetStunned(bool stunned)
    {
        isStunned = stunned;
        
        if (enableAttackLogging)
        {
            Debug.Log($"AttackCapability: {gameObject.name} {(stunned ? "stunned" : "unstunned")}");
        }
    }
    
    /// <summary>
    /// Sets the attack capability state
    /// </summary>
    public void SetAttackEnabled(bool enabled)
    {
        canAttack = enabled;
        
        if (enableAttackLogging)
        {
            Debug.Log($"AttackCapability: {gameObject.name} attack capability {(enabled ? "enabled" : "disabled")}");
        }
    }
    
    /// <summary>
    /// Adds a temporary attack cooldown
    /// </summary>
    public void AddCooldown(float duration)
    {
        attackCooldown = Mathf.Max(attackCooldown, duration);
        
        if (enableAttackLogging)
        {
            Debug.Log($"AttackCapability: {gameObject.name} cooldown set to {attackCooldown:F1}s");
        }
    }
    
    /// <summary>
    /// Modifies attack damage temporarily or permanently
    /// </summary>
    public void ModifyAttackDamage(int newDamage)
    {
        int oldDamage = attackDamage;
        attackDamage = Mathf.Max(0, newDamage);
        
        if (enableAttackLogging)
        {
            Debug.Log($"AttackCapability: {gameObject.name} attack damage changed from {oldDamage} to {attackDamage}");
        }
    }
    
    /// <summary>
    /// Gets attack capability information for debugging
    /// </summary>
    public string GetAttackInfo()
    {
        return $"Attack Info - Damage: {attackDamage}, Range: {attackRange}, " +
               $"Attacks: {attacksUsedThisTurn}/{attacksPerTurn}, Cooldown: {attackCooldown:F1}s, " +
               $"Can Attack: {CanAttack}, Stunned: {isStunned}";
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Unregister from combat manager
        if (combatManager != null)
        {
            combatManager.UnregisterAttacker(this);
        }
    }
    
    /// <summary>
    /// Unity Gizmos for debug visualization
    /// </summary>
    void OnDrawGizmos()
    {
        if (!enableAttackLogging) return;
        
        // Draw attack range
        Gizmos.color = CanAttack ? Color.red : Color.gray;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw attack status indicator
        if (attacksUsedThisTurn > 0)
        {
            Gizmos.color = Color.orange;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.2f);
        }
    }
}