using UnityEngine;

/// <summary>
/// Enhanced attack validator that integrates line-of-sight validation with existing combat rules.
/// Ensures attacks are blocked when obstacles interfere, creating tactical depth through cover mechanics.
/// Works with existing AttackValidator to add line-of-sight requirements to combat validation.
/// </summary>
public class LineOfSightValidator : MonoBehaviour
{
    [Header("Line of Sight Validation")]
    [SerializeField] private bool requireLineOfSightForAttacks = true;
    [SerializeField] private bool allowPartialLineOfSight = false;
    [SerializeField] private float partialLineOfSightThreshold = 0.5f;
    [SerializeField] private bool ignoreFriendlyUnitsForLineOfSight = true;
    
    [Header("Diagonal Attack Handling")]
    [SerializeField] private bool allowDiagonalAttacks = true;
    [SerializeField] private bool strictDiagonalLineOfSight = false;
    [SerializeField] private float diagonalToleranceAngle = 15f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableValidationLogging = true;
    [SerializeField] private bool visualizeValidationChecks = false;
    [SerializeField] private float visualizationDuration = 2.0f;
    
    // System references
    private LineOfSightManager lineOfSightManager;
    private AttackValidator attackValidator;
    private CombatManager combatManager;
    
    // Validation state
    private LineOfSightResult lastValidationResult;
    
    // Events
    public System.Action<IAttacker, IAttackable, bool> OnLineOfSightValidated;
    public System.Action<IAttacker, IAttackable, GameObject> OnAttackBlockedByObstacle;
    public System.Action<IAttacker, IAttackable> OnLineOfSightCleared;
    
    // Properties
    public bool RequireLineOfSightForAttacks => requireLineOfSightForAttacks;
    public LineOfSightResult LastValidationResult => lastValidationResult;
    
    void Awake()
    {
        InitializeValidator();
    }
    
    void Start()
    {
        FindSystemReferences();
        IntegrateWithAttackValidator();
    }
    
    /// <summary>
    /// Initializes the line-of-sight validator
    /// </summary>
    private void InitializeValidator()
    {
        if (enableValidationLogging)
        {
            Debug.Log("LineOfSightValidator initialized - Enhanced attack validation with line-of-sight");
        }
    }
    
    /// <summary>
    /// Finds references to other systems
    /// </summary>
    private void FindSystemReferences()
    {
        lineOfSightManager = FindFirstObjectByType<LineOfSightManager>();
        if (lineOfSightManager == null)
        {
            Debug.LogError("LineOfSightValidator: LineOfSightManager not found! Line-of-sight validation will not work.");
        }
        
        attackValidator = GetComponent<AttackValidator>();
        if (attackValidator == null)
        {
            Debug.LogWarning("LineOfSightValidator: AttackValidator not found on same GameObject");
        }
        
        combatManager = GetComponent<CombatManager>();
        if (combatManager == null)
        {
            Debug.LogWarning("LineOfSightValidator: CombatManager not found on same GameObject");
        }
        
        if (enableValidationLogging)
        {
            Debug.Log($"LineOfSightValidator found references - LOS Manager: {lineOfSightManager != null}, " +
                     $"Attack Validator: {attackValidator != null}, Combat Manager: {combatManager != null}");
        }
    }
    
    /// <summary>
    /// Integrates with existing AttackValidator to add line-of-sight requirements
    /// </summary>
    private void IntegrateWithAttackValidator()
    {
        if (attackValidator != null)
        {
            // Subscribe to attack validation events if available
            // This would require modifications to AttackValidator to support additional validation steps
            if (enableValidationLogging)
            {
                Debug.Log("LineOfSightValidator: Integrated with AttackValidator for enhanced validation");
            }
        }
    }
    
    /// <summary>
    /// Validates an attack including line-of-sight requirements
    /// </summary>
    public AttackValidationResult ValidateAttackWithLineOfSight(IAttacker attacker, IAttackable target)
    {
        if (attacker == null)
        {
            return AttackValidationResult.Invalid("Attacker is null", attacker, target);
        }
        
        if (target == null)
        {
            return AttackValidationResult.Invalid("Target is null", attacker, target);
        }
        
        // First, validate using existing attack validation rules
        AttackValidationResult baseValidation = ValidateBaseAttackRules(attacker, target);
        if (!baseValidation.isValid)
        {
            if (enableValidationLogging)
            {
                Debug.Log($"LineOfSightValidator: Base validation failed - {baseValidation.failureReason}");
            }
            return baseValidation;
        }
        
        // Then validate line-of-sight if required
        if (requireLineOfSightForAttacks)
        {
            AttackValidationResult losValidation = ValidateLineOfSight(attacker, target);
            if (!losValidation.isValid)
            {
                if (enableValidationLogging)
                {
                    Debug.Log($"LineOfSightValidator: Line-of-sight validation failed - {losValidation.failureReason}");
                }
                
                // Trigger blocked event
                OnAttackBlockedByObstacle?.Invoke(attacker, target, lastValidationResult.blockingObject);
                
                return losValidation;
            }
        }
        
        // Both validations passed
        OnLineOfSightValidated?.Invoke(attacker, target, true);
        OnLineOfSightCleared?.Invoke(attacker, target);
        
        if (enableValidationLogging)
        {
            Debug.Log($"LineOfSightValidator: Attack validated successfully from {attacker.GetDisplayInfo()} to {target.GetDisplayInfo()}");
        }
        
        return AttackValidationResult.Valid(attacker, target);
    }
    
    /// <summary>
    /// Validates base attack rules using existing AttackValidator or fallback logic
    /// </summary>
    private AttackValidationResult ValidateBaseAttackRules(IAttacker attacker, IAttackable target)
    {
        // Use existing AttackValidator if available
        if (attackValidator != null)
        {
            return attackValidator.ValidateAttack(attacker, target);
        }
        
        // Fallback validation logic
        if (!attacker.CanAttack)
        {
            return AttackValidationResult.Invalid("Attacker cannot attack", attacker, target);
        }
        
        if (!target.CanBeTargeted)
        {
            return AttackValidationResult.Invalid("Target cannot be attacked", attacker, target);
        }
        
        if (attacker.Team == target.Team)
        {
            return AttackValidationResult.Invalid("Cannot attack same team", attacker, target);
        }
        
        // Check adjacency (assuming grid-based combat)
        if (!IsAdjacent(attacker, target))
        {
            return AttackValidationResult.Invalid("Target not adjacent", attacker, target);
        }
        
        return AttackValidationResult.Valid(attacker, target);
    }
    
    /// <summary>
    /// Validates line-of-sight between attacker and target
    /// </summary>
    private AttackValidationResult ValidateLineOfSight(IAttacker attacker, IAttackable target)
    {
        if (lineOfSightManager == null)
        {
            if (enableValidationLogging)
            {
                Debug.LogWarning("LineOfSightValidator: LineOfSightManager not available, allowing attack");
            }
            return AttackValidationResult.Valid(attacker, target);
        }
        
        // Get positions for line-of-sight check
        Vector3 attackerPos = GetAttackPosition(attacker);
        Vector3 targetPos = GetTargetPosition(target);
        
        // Perform line-of-sight check
        lastValidationResult = lineOfSightManager.GetLineOfSightDetails(attackerPos, targetPos);
        
        // Visualize the check if enabled
        if (visualizeValidationChecks)
        {
            lineOfSightManager.VisualizeLineOfSight(attackerPos, targetPos, visualizationDuration);
        }
        
        // Handle diagonal attacks with special rules
        if (IsDiagonalAttack(attackerPos, targetPos))
        {
            return ValidateDiagonalLineOfSight(attacker, target, attackerPos, targetPos);
        }
        
        // Standard line-of-sight validation
        if (lastValidationResult.isBlocked)
        {
            string reason = $"Line of sight blocked by {lastValidationResult.blockingObject?.name ?? "obstacle"}";
            return AttackValidationResult.Invalid(reason, attacker, target);
        }
        
        // Handle partial line-of-sight if enabled
        if (allowPartialLineOfSight)
        {
            float lineOfSightPercentage = CalculateLineOfSightPercentage(attackerPos, targetPos);
            if (lineOfSightPercentage < partialLineOfSightThreshold)
            {
                return AttackValidationResult.Invalid($"Insufficient line of sight ({lineOfSightPercentage:P0} < {partialLineOfSightThreshold:P0})", attacker, target);
            }
        }
        
        return AttackValidationResult.Valid(attacker, target);
    }
    
    /// <summary>
    /// Validates diagonal attacks with special line-of-sight rules
    /// </summary>
    private AttackValidationResult ValidateDiagonalLineOfSight(IAttacker attacker, IAttackable target, Vector3 attackerPos, Vector3 targetPos)
    {
        if (!allowDiagonalAttacks)
        {
            return AttackValidationResult.Invalid("Diagonal attacks not allowed", attacker, target);
        }
        
        if (strictDiagonalLineOfSight)
        {
            // Strict diagonal line-of-sight requires clear path
            if (lastValidationResult.isBlocked)
            {
                return AttackValidationResult.Invalid($"Diagonal line of sight blocked by {lastValidationResult.blockingObject?.name}", attacker, target);
            }
        }
        else
        {
            // Lenient diagonal line-of-sight allows attacks through small gaps
            Vector3 direction = (targetPos - attackerPos).normalized;
            
            // Check multiple rays within tolerance angle
            int clearRays = 0;
            int totalRays = 3;
            
            for (int i = 0; i < totalRays; i++)
            {
                float angle = (i - 1) * diagonalToleranceAngle;
                Vector3 adjustedDirection = Quaternion.AngleAxis(angle, Vector3.up) * direction;
                
                if (lineOfSightManager.HasLineOfSight(attackerPos, attackerPos + adjustedDirection * Vector3.Distance(attackerPos, targetPos)))
                {
                    clearRays++;
                }
            }
            
            float clearPercentage = (float)clearRays / totalRays;
            if (clearPercentage < 0.5f) // Require at least half the rays to be clear
            {
                return AttackValidationResult.Invalid($"Diagonal line of sight too obstructed ({clearPercentage:P0})", attacker, target);
            }
        }
        
        return AttackValidationResult.Valid(attacker, target);
    }
    
    /// <summary>
    /// Calculates the percentage of line-of-sight that is clear
    /// </summary>
    private float CalculateLineOfSightPercentage(Vector3 fromPos, Vector3 toPos)
    {
        // For partial line-of-sight, check multiple rays
        int clearRays = 0;
        int totalRays = 5;
        
        Vector3 direction = (toPos - fromPos).normalized;
        float distance = Vector3.Distance(fromPos, toPos);
        
        for (int i = 0; i < totalRays; i++)
        {
            float heightOffset = (i - 2) * 0.1f; // Check at different heights
            Vector3 adjustedFrom = fromPos + Vector3.up * heightOffset;
            Vector3 adjustedTo = toPos + Vector3.up * heightOffset;
            
            if (lineOfSightManager.HasLineOfSight(adjustedFrom, adjustedTo))
            {
                clearRays++;
            }
        }
        
        return (float)clearRays / totalRays;
    }
    
    /// <summary>
    /// Checks if an attack is diagonal based on positions
    /// </summary>
    private bool IsDiagonalAttack(Vector3 attackerPos, Vector3 targetPos)
    {
        Vector3 diff = targetPos - attackerPos;
        diff.y = 0; // Ignore vertical difference
        
        // Check if movement is purely horizontal, vertical, or diagonal
        float absX = Mathf.Abs(diff.x);
        float absZ = Mathf.Abs(diff.z);
        
        // Consider it diagonal if both X and Z components are significant
        return absX > 0.1f && absZ > 0.1f;
    }
    
    /// <summary>
    /// Checks if target is adjacent to attacker (fallback for basic validation)
    /// </summary>
    private bool IsAdjacent(IAttacker attacker, IAttackable target)
    {
        Vector3 attackerPos = GetAttackPosition(attacker);
        Vector3 targetPos = GetTargetPosition(target);
        
        float distance = Vector3.Distance(attackerPos, targetPos);
        return distance <= attacker.AttackRange + 0.1f; // Small tolerance for floating point
    }
    
    /// <summary>
    /// Gets the attack position from an attacker
    /// </summary>
    private Vector3 GetAttackPosition(IAttacker attacker)
    {
        if (attacker.Transform != null)
        {
            // Use center of renderer if available, otherwise transform position
            Renderer renderer = attacker.Transform.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds.center;
            }
            return attacker.Transform.position;
        }
        
        Debug.LogWarning("LineOfSightValidator: Attacker has no transform, using zero position");
        return Vector3.zero;
    }
    
    /// <summary>
    /// Gets the target position from an attackable target
    /// </summary>
    private Vector3 GetTargetPosition(IAttackable target)
    {
        if (target.Transform != null)
        {
            // Use center of renderer if available, otherwise transform position
            Renderer renderer = target.Transform.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds.center;
            }
            return target.Transform.position;
        }
        
        Debug.LogWarning("LineOfSightValidator: Target has no transform, using zero position");
        return Vector3.zero;
    }
    
    /// <summary>
    /// Quick line-of-sight check without detailed validation
    /// </summary>
    public bool HasLineOfSight(IAttacker attacker, IAttackable target)
    {
        if (lineOfSightManager == null || !requireLineOfSightForAttacks)
        {
            return true; // No line-of-sight restrictions
        }
        
        Vector3 attackerPos = GetAttackPosition(attacker);
        Vector3 targetPos = GetTargetPosition(target);
        
        return lineOfSightManager.HasLineOfSight(attackerPos, targetPos);
    }
    
    /// <summary>
    /// Updates line-of-sight validation settings at runtime
    /// </summary>
    public void UpdateValidationSettings(bool requireLOS, bool allowPartial, float partialThreshold)
    {
        requireLineOfSightForAttacks = requireLOS;
        allowPartialLineOfSight = allowPartial;
        partialLineOfSightThreshold = partialThreshold;
        
        if (enableValidationLogging)
        {
            Debug.Log($"LineOfSightValidator: Settings updated - Require LOS: {requireLOS}, Allow Partial: {allowPartial}");
        }
    }
    
    /// <summary>
    /// Gets validation information for debugging
    /// </summary>
    public string GetValidationInfo()
    {
        return $"LOS Validation - Required: {requireLineOfSightForAttacks}, " +
               $"Allow Partial: {allowPartialLineOfSight}, " +
               $"Diagonal Attacks: {allowDiagonalAttacks}, " +
               $"Last Result: {lastValidationResult}";
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Clear event references
        OnLineOfSightValidated = null;
        OnAttackBlockedByObstacle = null;
        OnLineOfSightCleared = null;
    }
}