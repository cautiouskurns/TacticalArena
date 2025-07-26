using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attack validation system for the tactical arena combat system.
/// Handles all attack legality checking including adjacency, team restrictions, line-of-sight, and game rules.
/// Integrates with CombatManager for comprehensive attack validation pipeline.
/// </summary>
public class AttackValidator : MonoBehaviour
{
    [Header("Validation Configuration")]
    [SerializeField] private bool enableValidation = true;
    [SerializeField] private bool checkAdjacency = true;
    [SerializeField] private bool allowDiagonalAttacks = false;
    [SerializeField] private bool preventFriendlyFire = true;
    [SerializeField] private float maxAttackRange = 1.5f;
    
    [Header("Advanced Validation")]
    [SerializeField] private bool requireLineOfSight = false;
    [SerializeField] private bool checkAttackerState = true;
    [SerializeField] private bool checkTargetState = true;
    [SerializeField] private bool validateGridPositions = true;
    [SerializeField] private bool useAdvancedLineOfSight = true;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableValidationLogging = true;
    
    // System references
    private GridManager gridManager;
    private CombatManager combatManager;
    private LineOfSightValidator lineOfSightValidator;
    
    // Obstacle tracking for line-of-sight
    private List<Vector2Int> obstaclePositions = new List<Vector2Int>();
    private bool obstaclesCached = false;
    
    void Awake()
    {
        InitializeValidator();
    }
    
    void Start()
    {
        FindSystemReferences();
        CacheObstaclePositions();
    }
    
    /// <summary>
    /// Initializes the attack validator
    /// </summary>
    private void InitializeValidator()
    {
        if (enableValidationLogging)
        {
            Debug.Log("AttackValidator initialized - Combat validation system ready");
        }
    }
    
    /// <summary>
    /// Finds references to other systems
    /// </summary>
    private void FindSystemReferences()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("AttackValidator: GridManager not found!");
        }
        
        combatManager = GetComponent<CombatManager>();
        if (combatManager == null)
        {
            Debug.LogWarning("AttackValidator: CombatManager not found on same GameObject");
        }
        
        lineOfSightValidator = GetComponent<LineOfSightValidator>();
        if (lineOfSightValidator == null && useAdvancedLineOfSight)
        {
            Debug.LogWarning("AttackValidator: LineOfSightValidator not found - advanced line-of-sight disabled");
            useAdvancedLineOfSight = false;
        }
        
        if (enableValidationLogging)
        {
            Debug.Log($"AttackValidator found references - GridManager: {gridManager != null}, " +
                     $"LineOfSightValidator: {lineOfSightValidator != null}");
        }
    }
    
    /// <summary>
    /// Caches obstacle positions for line-of-sight validation
    /// </summary>
    private void CacheObstaclePositions()
    {
        if (!requireLineOfSight || gridManager == null)
        {
            obstaclesCached = true;
            return;
        }
        
        obstaclePositions.Clear();
        
        // Find all obstacle objects in the scene
        Obstacle[] obstacles = FindObjectsByType<Obstacle>(FindObjectsSortMode.None);
        
        foreach (Obstacle obstacle in obstacles)
        {
            GridCoordinate gridCoord = gridManager.WorldToGrid(obstacle.transform.position);
            Vector2Int gridPos = new Vector2Int(gridCoord.x, gridCoord.z);
            if (!obstaclePositions.Contains(gridPos))
            {
                obstaclePositions.Add(gridPos);
            }
        }
        
        obstaclesCached = true;
        
        if (enableValidationLogging)
        {
            Debug.Log($"AttackValidator cached {obstaclePositions.Count} obstacle positions for line-of-sight");
        }
    }
    
    /// <summary>
    /// Validates an attack from attacker to target
    /// </summary>
    public AttackValidationResult ValidateAttack(IAttacker attacker, IAttackable target)
    {
        if (!enableValidation)
        {
            return AttackValidationResult.Valid(attacker, target);
        }
        
        if (attacker == null)
        {
            return AttackValidationResult.Invalid("Attacker is null", attacker, target);
        }
        
        if (target == null)
        {
            return AttackValidationResult.Invalid("Target is null", attacker, target);
        }
        
        if (enableValidationLogging)
        {
            Debug.Log($"AttackValidator: Validating attack from {attacker.GetDisplayInfo()} to {target.GetDisplayInfo()}");
        }
        
        // Run all validation checks
        AttackValidationResult result;
        
        // 1. Basic attacker validation
        result = ValidateAttacker(attacker, target);
        if (!result.isValid) return result;
        
        // 2. Basic target validation
        result = ValidateTarget(attacker, target);
        if (!result.isValid) return result;
        
        // 3. Team validation (friendly fire prevention)
        result = ValidateTeams(attacker, target);
        if (!result.isValid) return result;
        
        // 4. Position and adjacency validation
        result = ValidatePositions(attacker, target);
        if (!result.isValid) return result;
        
        // 5. Line-of-sight validation (if required)
        result = ValidateLineOfSight(attacker, target);
        if (!result.isValid) return result;
        
        // 6. Attacker-specific validation
        result = ValidateAttackerSpecific(attacker, target);
        if (!result.isValid) return result;
        
        // 7. Target-specific validation
        result = ValidateTargetSpecific(attacker, target);
        if (!result.isValid) return result;
        
        if (enableValidationLogging)
        {
            Debug.Log($"AttackValidator: Attack APPROVED from {attacker.GetDisplayInfo()} to {target.GetDisplayInfo()}");
        }
        
        return AttackValidationResult.Valid(attacker, target);
    }
    
    /// <summary>
    /// Validates the attacker's basic state and capability
    /// </summary>
    private AttackValidationResult ValidateAttacker(IAttacker attacker, IAttackable target)
    {
        if (!checkAttackerState)
        {
            return AttackValidationResult.Valid(attacker, target);
        }
        
        // Check if attacker can attack
        if (!attacker.CanAttack)
        {
            return AttackValidationResult.Invalid("Attacker cannot attack at this time", attacker, target);
        }
        
        // Check if trying to attack self
        if (attacker.Transform == target.Transform)
        {
            return AttackValidationResult.Invalid("Cannot attack self", attacker, target);
        }
        
        return AttackValidationResult.Valid(attacker, target);
    }
    
    /// <summary>
    /// Validates the target's basic state and capability
    /// </summary>
    private AttackValidationResult ValidateTarget(IAttacker attacker, IAttackable target)
    {
        if (!checkTargetState)
        {
            return AttackValidationResult.Valid(attacker, target);
        }
        
        // Check if target can be targeted
        if (!target.CanBeTargeted)
        {
            return AttackValidationResult.Invalid("Target cannot be attacked at this time", attacker, target);
        }
        
        // Check if target is alive
        if (!target.IsAlive)
        {
            return AttackValidationResult.Invalid("Target is already dead", attacker, target);
        }
        
        return AttackValidationResult.Valid(attacker, target);
    }
    
    /// <summary>
    /// Validates team restrictions and friendly fire prevention
    /// </summary>
    private AttackValidationResult ValidateTeams(IAttacker attacker, IAttackable target)
    {
        if (!preventFriendlyFire)
        {
            return AttackValidationResult.Valid(attacker, target);
        }
        
        // Prevent attacks on same team (friendly fire prevention)
        if (attacker.Team == target.Team)
        {
            return AttackValidationResult.Invalid("Cannot attack allies (friendly fire prevented)", attacker, target);
        }
        
        return AttackValidationResult.Valid(attacker, target);
    }
    
    /// <summary>
    /// Validates positions and adjacency requirements
    /// </summary>
    private AttackValidationResult ValidatePositions(IAttacker attacker, IAttackable target)
    {
        if (!validateGridPositions || !checkAdjacency)
        {
            return AttackValidationResult.Valid(attacker, target);
        }
        
        Vector2Int attackerPos = attacker.GridPosition;
        Vector2Int targetPos = target.GridPosition;
        
        // Calculate distance
        Vector2Int distance = targetPos - attackerPos;
        float actualDistance = Vector2.Distance(attackerPos, targetPos);
        
        // Check maximum range
        if (actualDistance > maxAttackRange)
        {
            return AttackValidationResult.Invalid($"Target too far away ({actualDistance:F1} > {maxAttackRange})", attacker, target);
        }
        
        // Check adjacency (only adjacent tiles can be attacked)
        bool isAdjacent = Mathf.Abs(distance.x) <= 1 && Mathf.Abs(distance.y) <= 1;
        if (!isAdjacent)
        {
            return AttackValidationResult.Invalid("Target must be adjacent to attacker", attacker, target);
        }
        
        // Check diagonal attack restrictions
        if (!allowDiagonalAttacks && !attacker.CanAttackDiagonally)
        {
            bool isDiagonal = distance.x != 0 && distance.y != 0;
            if (isDiagonal)
            {
                return AttackValidationResult.Invalid("Diagonal attacks not allowed", attacker, target);
            }
        }
        
        return AttackValidationResult.Valid(attacker, target);
    }
    
    /// <summary>
    /// Validates line-of-sight requirements (if enabled)
    /// </summary>
    private AttackValidationResult ValidateLineOfSight(IAttacker attacker, IAttackable target)
    {
        if (!requireLineOfSight)
        {
            return AttackValidationResult.Valid(attacker, target);
        }
        
        // Use advanced line-of-sight validation if available
        if (useAdvancedLineOfSight && lineOfSightValidator != null)
        {
            AttackValidationResult advancedResult = lineOfSightValidator.ValidateAttackWithLineOfSight(attacker, target);
            if (!advancedResult.isValid)
            {
                if (enableValidationLogging)
                {
                    Debug.Log($"AttackValidator: Advanced line-of-sight validation failed - {advancedResult.failureReason}");
                }
                return advancedResult;
            }
            
            if (enableValidationLogging)
            {
                Debug.Log("AttackValidator: Advanced line-of-sight validation passed");
            }
            return advancedResult;
        }
        
        // Fallback to basic line-of-sight validation
        return ValidateBasicLineOfSight(attacker, target);
    }
    
    /// <summary>
    /// Basic line-of-sight validation for fallback scenarios
    /// </summary>
    private AttackValidationResult ValidateBasicLineOfSight(IAttacker attacker, IAttackable target)
    {
        // Refresh obstacle cache if needed
        if (!obstaclesCached)
        {
            CacheObstaclePositions();
        }
        
        Vector2Int attackerPos = attacker.GridPosition;
        Vector2Int targetPos = target.GridPosition;
        
        // For adjacent attacks, line-of-sight is generally clear
        Vector2Int distance = targetPos - attackerPos;
        bool isAdjacent = Mathf.Abs(distance.x) <= 1 && Mathf.Abs(distance.y) <= 1;
        
        if (isAdjacent)
        {
            // For adjacent attacks, check if there's an obstacle directly between them
            // This is mainly for diagonal attacks where an obstacle might block
            if (distance.x != 0 && distance.y != 0)
            {
                // Check diagonal line-of-sight
                Vector2Int corner1 = attackerPos + new Vector2Int(distance.x, 0);
                Vector2Int corner2 = attackerPos + new Vector2Int(0, distance.y);
                
                if (obstaclePositions.Contains(corner1) && obstaclePositions.Contains(corner2))
                {
                    return AttackValidationResult.Invalid("Line of sight blocked by obstacles (basic check)", attacker, target);
                }
            }
        }
        
        // For longer range attacks (future expansion), implement full line-of-sight checking
        
        return AttackValidationResult.Valid(attacker, target);
    }
    
    /// <summary>
    /// Validates attacker-specific rules
    /// </summary>
    private AttackValidationResult ValidateAttackerSpecific(IAttacker attacker, IAttackable target)
    {
        // Use the attacker's own validation if available
        AttackValidationResult attackerValidation = attacker.ValidateAttack(target);
        if (!attackerValidation.isValid)
        {
            return attackerValidation;
        }
        
        return AttackValidationResult.Valid(attacker, target);
    }
    
    /// <summary>
    /// Validates target-specific rules
    /// </summary>
    private AttackValidationResult ValidateTargetSpecific(IAttacker attacker, IAttackable target)
    {
        // Use the target's own validation if available
        TargetValidationResult targetValidation = target.ValidateAsTarget(attacker);
        if (!targetValidation.isValid)
        {
            return AttackValidationResult.Invalid(targetValidation.failureReason, attacker, target);
        }
        
        return AttackValidationResult.Valid(attacker, target);
    }
    
    /// <summary>
    /// Gets all valid targets for the specified attacker
    /// </summary>
    public List<IAttackable> GetValidTargets(IAttacker attacker)
    {
        List<IAttackable> validTargets = new List<IAttackable>();
        
        if (attacker == null) return validTargets;
        
        // Find all potential targets
        TargetCapability[] allTargets = FindObjectsByType<TargetCapability>(FindObjectsSortMode.None);
        
        foreach (TargetCapability target in allTargets)
        {
            if (target == null) continue;
            
            AttackValidationResult validation = ValidateAttack(attacker, target);
            if (validation.isValid)
            {
                validTargets.Add(target);
            }
        }
        
        return validTargets;
    }
    
    /// <summary>
    /// Checks if a specific position is a valid attack target for the attacker
    /// </summary>
    public bool IsValidTargetPosition(IAttacker attacker, Vector2Int targetPosition)
    {
        if (attacker == null || !checkAdjacency) return false;
        
        Vector2Int attackerPos = attacker.GridPosition;
        Vector2Int distance = targetPosition - attackerPos;
        float actualDistance = Vector2.Distance(attackerPos, targetPosition);
        
        // Check maximum range
        if (actualDistance > maxAttackRange) return false;
        
        // Check adjacency
        bool isAdjacent = Mathf.Abs(distance.x) <= 1 && Mathf.Abs(distance.y) <= 1;
        if (!isAdjacent) return false;
        
        // Check diagonal restrictions
        if (!allowDiagonalAttacks && !attacker.CanAttackDiagonally)
        {
            bool isDiagonal = distance.x != 0 && distance.y != 0;
            if (isDiagonal) return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Refreshes cached obstacle positions
    /// </summary>
    public void RefreshObstacleCache()
    {
        obstaclesCached = false;
        CacheObstaclePositions();
    }
    
    /// <summary>
    /// Gets validation info for debugging
    /// </summary>
    public string GetValidationInfo()
    {
        string advancedLOSInfo = useAdvancedLineOfSight && lineOfSightValidator != null ? 
            " (Advanced)" : " (Basic)";
        
        return $"Validation Rules: Adjacency={checkAdjacency}, Diagonal={allowDiagonalAttacks}, " +
               $"Friendly Fire Prevention={preventFriendlyFire}, Line of Sight={requireLineOfSight}{advancedLOSInfo}, " +
               $"Max Range={maxAttackRange}, Obstacles Cached={obstaclePositions.Count}";
    }
    
    /// <summary>
    /// Quick line-of-sight check for external systems
    /// </summary>
    public bool HasLineOfSight(IAttacker attacker, IAttackable target)
    {
        if (!requireLineOfSight)
        {
            return true; // No line-of-sight restrictions
        }
        
        // Use advanced validator if available
        if (useAdvancedLineOfSight && lineOfSightValidator != null)
        {
            return lineOfSightValidator.HasLineOfSight(attacker, target);
        }
        
        // Fallback to basic validation
        AttackValidationResult result = ValidateBasicLineOfSight(attacker, target);
        return result.isValid;
    }
    
    /// <summary>
    /// Enable or disable validation
    /// </summary>
    public void SetValidationEnabled(bool enabled)
    {
        enableValidation = enabled;
        
        if (enableValidationLogging)
        {
            Debug.Log($"AttackValidator: Validation {(enabled ? "enabled" : "disabled")}");
        }
    }
    
    /// <summary>
    /// Unity Gizmos for debug visualization
    /// </summary>
    void OnDrawGizmos()
    {
        if (!enableValidationLogging || gridManager == null) return;
        
        // Draw obstacle positions
        Gizmos.color = Color.red;
        foreach (Vector2Int obstaclePos in obstaclePositions)
        {
            Vector3 worldPos = gridManager.GridToWorld(new GridCoordinate(obstaclePos.x, obstaclePos.y));
            Gizmos.DrawWireCube(worldPos, Vector3.one * 0.9f);
        }
    }
}