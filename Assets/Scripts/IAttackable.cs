using UnityEngine;

/// <summary>
/// Interface for units that can be targeted and damaged in the tactical combat system.
/// Defines the contract for attack target capability including damage reception and state management.
/// Part of the combat system foundation for Task 2.1.1.
/// </summary>
public interface IAttackable
{
    /// <summary>
    /// The transform of the target for position calculations
    /// </summary>
    Transform Transform { get; }
    
    /// <summary>
    /// The current grid position of the target
    /// </summary>
    Vector2Int GridPosition { get; }
    
    /// <summary>
    /// The team of the target for friendly fire prevention
    /// </summary>
    UnitTeam Team { get; }
    
    /// <summary>
    /// Current health points of the target
    /// </summary>
    int CurrentHealth { get; }
    
    /// <summary>
    /// Maximum health points of the target
    /// </summary>
    int MaxHealth { get; }
    
    /// <summary>
    /// Whether this target can currently be attacked (alive, not invulnerable, etc.)
    /// </summary>
    bool CanBeTargeted { get; }
    
    /// <summary>
    /// Whether this target is currently alive
    /// </summary>
    bool IsAlive { get; }
    
    /// <summary>
    /// Receives damage from an attack
    /// </summary>
    /// <param name="damage">Amount of damage to take</param>
    /// <param name="attacker">The unit that attacked this target</param>
    /// <returns>The actual damage taken after any resistances</returns>
    int TakeDamage(int damage, IAttacker attacker);
    
    /// <summary>
    /// Called when this target is attacked (for visual feedback, sounds, etc.)
    /// </summary>
    /// <param name="attacker">The attacking unit</param>
    /// <param name="damage">The damage being dealt</param>
    void OnAttacked(IAttacker attacker, int damage);
    
    /// <summary>
    /// Called when this target dies from damage
    /// </summary>
    /// <param name="killer">The unit that dealt the killing blow</param>
    void OnDeath(IAttacker killer);
    
    /// <summary>
    /// Validates if this target can be attacked by the specified attacker
    /// </summary>
    /// <param name="attacker">The potential attacker</param>
    /// <returns>Validation result with success/failure and reason</returns>
    TargetValidationResult ValidateAsTarget(IAttacker attacker);
    
    /// <summary>
    /// Gets display information for this target
    /// </summary>
    /// <returns>Human-readable string describing this target</returns>
    string GetDisplayInfo();
}

/// <summary>
/// Target validation result structure
/// </summary>
[System.Serializable]
public class TargetValidationResult
{
    public bool isValid;
    public string failureReason;
    public IAttacker attacker;
    public IAttackable target;
    
    /// <summary>
    /// Creates a successful target validation result
    /// </summary>
    public static TargetValidationResult Valid(IAttacker attacker, IAttackable target)
    {
        return new TargetValidationResult
        {
            isValid = true,
            failureReason = null,
            attacker = attacker,
            target = target
        };
    }
    
    /// <summary>
    /// Creates a failed target validation result
    /// </summary>
    public static TargetValidationResult Invalid(string reason, IAttacker attacker, IAttackable target)
    {
        return new TargetValidationResult
        {
            isValid = false,
            failureReason = reason,
            attacker = attacker,
            target = target
        };
    }
}

/// <summary>
/// Damage result structure for tracking damage application
/// </summary>
[System.Serializable]
public class DamageResult
{
    public int damageDealt;
    public int healthRemaining;
    public bool wasKilled;
    public bool wasBlocked;
    public string message;
    
    /// <summary>
    /// Creates a successful damage result
    /// </summary>
    public static DamageResult Success(int damage, int healthRemaining, bool wasKilled = false)
    {
        return new DamageResult
        {
            damageDealt = damage,
            healthRemaining = healthRemaining,
            wasKilled = wasKilled,
            wasBlocked = false,
            message = wasKilled ? "Target destroyed" : $"Dealt {damage} damage"
        };
    }
    
    /// <summary>
    /// Creates a blocked damage result
    /// </summary>
    public static DamageResult Blocked(string reason = "Damage blocked")
    {
        return new DamageResult
        {
            damageDealt = 0,
            healthRemaining = -1,
            wasKilled = false,
            wasBlocked = true,
            message = reason
        };
    }
}