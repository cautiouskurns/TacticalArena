using UnityEngine;

/// <summary>
/// Interface for units that can perform attacks in the tactical combat system.
/// Defines the contract for attack capability including damage, range, and targeting rules.
/// Part of the combat system foundation for Task 2.1.1.
/// </summary>
public interface IAttacker
{
    /// <summary>
    /// The transform of the attacking unit for position calculations
    /// </summary>
    Transform Transform { get; }
    
    /// <summary>
    /// The current grid position of the attacker
    /// </summary>
    Vector2Int GridPosition { get; }
    
    /// <summary>
    /// The team of the attacker for friendly fire prevention
    /// </summary>
    UnitTeam Team { get; }
    
    /// <summary>
    /// The base damage this attacker deals
    /// </summary>
    int AttackDamage { get; }
    
    /// <summary>
    /// The maximum range for attacks
    /// </summary>
    float AttackRange { get; }
    
    /// <summary>
    /// Number of attacks this unit can make per turn
    /// </summary>
    int AttacksPerTurn { get; }
    
    /// <summary>
    /// Whether this unit can attack diagonally adjacent targets
    /// </summary>
    bool CanAttackDiagonally { get; }
    
    /// <summary>
    /// Whether this unit can currently attack (not stunned, alive, etc.)
    /// </summary>
    bool CanAttack { get; }
    
    /// <summary>
    /// Validates if this attacker can attack the specified target
    /// </summary>
    /// <param name="target">The potential target</param>
    /// <returns>Validation result with success/failure and reason</returns>
    AttackValidationResult ValidateAttack(IAttackable target);
    
    /// <summary>
    /// Called when this unit performs an attack
    /// </summary>
    /// <param name="target">The target being attacked</param>
    /// <param name="damage">The damage being dealt</param>
    void OnAttackPerformed(IAttackable target, int damage);
    
    /// <summary>
    /// Called when this unit's attack is blocked or fails
    /// </summary>
    /// <param name="target">The intended target</param>
    /// <param name="reason">Why the attack failed</param>
    void OnAttackFailed(IAttackable target, string reason);
    
    /// <summary>
    /// Gets display information for this attacker
    /// </summary>
    /// <returns>Human-readable string describing this attacker</returns>
    string GetDisplayInfo();
}

/// <summary>
/// Attack validation result structure
/// </summary>
[System.Serializable]
public class AttackValidationResult
{
    public bool isValid;
    public string failureReason;
    public IAttacker attacker;
    public IAttackable target;
    
    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static AttackValidationResult Valid(IAttacker attacker, IAttackable target)
    {
        return new AttackValidationResult
        {
            isValid = true,
            failureReason = null,
            attacker = attacker,
            target = target
        };
    }
    
    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static AttackValidationResult Invalid(string reason, IAttacker attacker, IAttackable target)
    {
        return new AttackValidationResult
        {
            isValid = false,
            failureReason = reason,
            attacker = attacker,
            target = target
        };
    }
}

/// <summary>
/// Attack execution result structure
/// </summary>
[System.Serializable]
public class AttackResult
{
    public bool success;
    public string message;
    public int damage;
    public IAttacker attacker;
    public IAttackable target;
    
    /// <summary>
    /// Creates a successful attack result
    /// </summary>
    public static AttackResult Success(int damage, string message = "Attack successful")
    {
        return new AttackResult
        {
            success = true,
            message = message,
            damage = damage
        };
    }
    
    /// <summary>
    /// Creates a failed attack result
    /// </summary>
    public static AttackResult Failed(string message)
    {
        return new AttackResult
        {
            success = false,
            message = message,
            damage = 0
        };
    }
}