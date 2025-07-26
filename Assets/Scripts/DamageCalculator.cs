using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Damage calculation system for tactical combat.
/// Handles damage computation with variations, critical hits, resistances, and tactical modifiers.
/// Integrates with health system to provide realistic damage values for combat.
/// Part of Task 2.1.3 - Health & Damage System.
/// </summary>
public class DamageCalculator : MonoBehaviour
{
    [Header("Base Damage Configuration")]
    [SerializeField] private int baseDamageAmount = 1;
    [SerializeField] private bool enableDamageVariation = false;
    [SerializeField] private int minDamageVariation = 0;
    [SerializeField] private int maxDamageVariation = 0;
    [SerializeField] private bool ensureMinimumDamage = true;
    [SerializeField] private int absoluteMinimumDamage = 1;
    
    [Header("Critical Hit System")]
    [SerializeField] private bool enableCriticalHits = false;
    [SerializeField] private float baseCriticalChance = 0.0f;
    [SerializeField] private float criticalDamageMultiplier = 1.0f;
    [SerializeField] private bool allowCriticalVariation = false;
    [SerializeField] private float criticalVariationRange = 0.0f;
    
    [Header("Tactical Modifiers")]
    [SerializeField] private bool enablePositionalModifiers = false;
    [SerializeField] private float flankingDamageBonus = 0.5f;
    [SerializeField] private float rearAttackDamageBonus = 1.0f;
    [SerializeField] private float coverDamageReduction = 0.3f;
    [SerializeField] private float elevationDamageBonus = 0.2f;
    
    [Header("Team and Unit Modifiers")]
    [SerializeField] private bool enableTeamModifiers = false;
    [SerializeField] private float friendlyFireReduction = 0.5f;
    [SerializeField] private bool enableUnitTypeModifiers = false;
    [SerializeField] private float unitTypeModifierRange = 0.3f;
    
    [Header("Environmental Factors")]
    [SerializeField] private bool enableEnvironmentalModifiers = false;
    [SerializeField] private float weatherDamageModifier = 1.0f;
    [SerializeField] private float terrainDamageModifier = 1.0f;
    [SerializeField] private bool enableDistanceModifiers = false;
    [SerializeField] private float optimalDamageDistance = 1.0f;
    [SerializeField] private float distanceDamageFalloff = 0.0f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDamageDebugging = true;
    [SerializeField] private bool logDamageCalculations = true;
    [SerializeField] private bool logCriticalHits = true;
    [SerializeField] private bool logTacticalModifiers = true;
    [SerializeField] private bool visualizeDamageCalculation = false;
    
    // System references
    private GridManager gridManager;
    private CoverAnalyzer coverAnalyzer;
    private LineOfSightManager lineOfSightManager;
    
    // Damage calculation cache
    private Dictionary<string, CachedDamageResult> damageCache;
    private float cacheValidityDuration = 0.5f;
    private bool enableDamageCache = true;
    
    // Damage statistics
    private DamageStatistics damageStats;
    
    // Events
    public System.Action<DamageCalculationResult> OnDamageCalculated;
    public System.Action<IAttacker, IAttackable, int> OnCriticalHit;
    public System.Action<IAttacker, IAttackable, float> OnTacticalModifierApplied;
    
    // Properties
    public int BaseDamageAmount => baseDamageAmount;
    public bool EnableCriticalHits => enableCriticalHits;
    public float BaseCriticalChance => baseCriticalChance;
    public DamageStatistics Statistics => damageStats;
    
    // Cached damage result structure
    private struct CachedDamageResult
    {
        public int damage;
        public float timestamp;
        public DamageCalculationResult result;
        
        public bool IsValid(float validityDuration)
        {
            return Time.time - timestamp < validityDuration;
        }
        
        public CachedDamageResult(int dmg, DamageCalculationResult res)
        {
            damage = dmg;
            timestamp = Time.time;
            result = res;
        }
    }
    
    void Awake()
    {
        InitializeDamageCalculator();
    }
    
    void Start()
    {
        FindSystemReferences();
        InitializeDamageStatistics();
    }
    
    /// <summary>
    /// Initializes the damage calculator
    /// </summary>
    private void InitializeDamageCalculator()
    {
        if (enableDamageCache)
        {
            damageCache = new Dictionary<string, CachedDamageResult>();
        }
        
        if (enableDamageDebugging)
        {
            Debug.Log("DamageCalculator initialized - Tactical damage system ready");
        }
    }
    
    /// <summary>
    /// Finds references to other systems
    /// </summary>
    private void FindSystemReferences()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        coverAnalyzer = FindFirstObjectByType<CoverAnalyzer>();
        lineOfSightManager = FindFirstObjectByType<LineOfSightManager>();
        
        if (enableDamageDebugging)
        {
            Debug.Log($"DamageCalculator found references - Grid: {gridManager != null}, " +
                     $"Cover Analyzer: {coverAnalyzer != null}, LOS Manager: {lineOfSightManager != null}");
        }
    }
    
    /// <summary>
    /// Initializes damage statistics tracking
    /// </summary>
    private void InitializeDamageStatistics()
    {
        damageStats = new DamageStatistics();
    }
    
    #region Main Damage Calculation
    
    /// <summary>
    /// Calculates damage for an attack
    /// </summary>
    public int CalculateDamage(int baseDamage, IAttacker attacker, IAttackable target)
    {
        DamageCalculationResult result = CalculateDamageWithDetails(baseDamage, attacker, target);
        return result.finalDamage;
    }
    
    /// <summary>
    /// Calculates damage with detailed breakdown
    /// </summary>
    public DamageCalculationResult CalculateDamageWithDetails(int baseDamage, IAttacker attacker, IAttackable target)
    {
        if (attacker == null || target == null)
        {
            if (enableDamageDebugging)
            {
                Debug.LogWarning("DamageCalculator: Null attacker or target provided");
            }
            return DamageCalculationResult.Invalid("Null attacker or target");
        }
        
        // Check cache first
        string cacheKey = GetDamageCacheKey(baseDamage, attacker, target);
        if (enableDamageCache && damageCache.ContainsKey(cacheKey))
        {
            CachedDamageResult cached = damageCache[cacheKey];
            if (cached.IsValid(cacheValidityDuration))
            {
                if (logDamageCalculations)
                {
                    Debug.Log($"DamageCalculator: Using cached damage result for {cacheKey}");
                }
                return cached.result;
            }
            else
            {
                damageCache.Remove(cacheKey);
            }
        }
        
        // Start with base damage
        float calculatedDamage = baseDamage > 0 ? baseDamage : baseDamageAmount;
        DamageCalculationResult result = new DamageCalculationResult
        {
            attacker = attacker,
            target = target,
            baseDamage = (int)calculatedDamage,
            finalDamage = (int)calculatedDamage,
            isCritical = false,
            appliedModifiers = new List<DamageModifier>()
        };
        
        if (logDamageCalculations)
        {
            Debug.Log($"DamageCalculator: Starting calculation - Base: {calculatedDamage}");
        }
        
        // Apply damage variation
        if (enableDamageVariation)
        {
            float variation = ApplyDamageVariation(calculatedDamage);
            if (variation != calculatedDamage)
            {
                result.appliedModifiers.Add(new DamageModifier
                {
                    name = "Damage Variation",
                    multiplier = variation / calculatedDamage,
                    additive = 0,
                    description = $"Random variation: {variation - calculatedDamage:+0;-0}"
                });
                calculatedDamage = variation;
            }
        }
        
        // Apply critical hit
        if (enableCriticalHits)
        {
            bool isCritical = RollForCriticalHit(attacker, target);
            if (isCritical)
            {
                float criticalDamage = ApplyCriticalHit(calculatedDamage);
                result.isCritical = true;
                result.appliedModifiers.Add(new DamageModifier
                {
                    name = "Critical Hit",
                    multiplier = criticalDamage / calculatedDamage,
                    additive = 0,
                    description = $"Critical strike! x{criticalDamageMultiplier}"
                });
                calculatedDamage = criticalDamage;
                
                OnCriticalHit?.Invoke(attacker, target, (int)criticalDamage);
                
                if (logCriticalHits)
                {
                    Debug.Log($"DamageCalculator: Critical hit! {result.baseDamage} → {criticalDamage}");
                }
            }
        }
        
        // Apply tactical modifiers
        if (enablePositionalModifiers)
        {
            float tacticalModifier = CalculateTacticalModifiers(attacker, target);
            if (tacticalModifier != 1.0f)
            {
                float modifiedDamage = calculatedDamage * tacticalModifier;
                result.appliedModifiers.Add(new DamageModifier
                {
                    name = "Tactical Position",
                    multiplier = tacticalModifier,
                    additive = 0,
                    description = GetTacticalModifierDescription(tacticalModifier)
                });
                calculatedDamage = modifiedDamage;
                
                OnTacticalModifierApplied?.Invoke(attacker, target, tacticalModifier);
                
                if (logTacticalModifiers)
                {
                    Debug.Log($"DamageCalculator: Tactical modifier applied: x{tacticalModifier:F2}");
                }
            }
        }
        
        // Apply team modifiers
        if (enableTeamModifiers)
        {
            float teamModifier = CalculateTeamModifiers(attacker, target);
            if (teamModifier != 1.0f)
            {
                float modifiedDamage = calculatedDamage * teamModifier;
                result.appliedModifiers.Add(new DamageModifier
                {
                    name = "Team Relations",
                    multiplier = teamModifier,
                    additive = 0,
                    description = GetTeamModifierDescription(attacker, target, teamModifier)
                });
                calculatedDamage = modifiedDamage;
            }
        }
        
        // Apply environmental modifiers
        if (enableEnvironmentalModifiers)
        {
            float environmentalModifier = CalculateEnvironmentalModifiers(attacker, target);
            if (environmentalModifier != 1.0f)
            {
                float modifiedDamage = calculatedDamage * environmentalModifier;
                result.appliedModifiers.Add(new DamageModifier
                {
                    name = "Environmental",
                    multiplier = environmentalModifier,
                    additive = 0,
                    description = "Environmental conditions"
                });
                calculatedDamage = modifiedDamage;
            }
        }
        
        // Ensure minimum damage
        if (ensureMinimumDamage && calculatedDamage < absoluteMinimumDamage)
        {
            calculatedDamage = absoluteMinimumDamage;
            result.appliedModifiers.Add(new DamageModifier
            {
                name = "Minimum Damage",
                multiplier = 1.0f,
                additive = Mathf.RoundToInt(absoluteMinimumDamage - calculatedDamage),
                description = $"Minimum damage enforced: {absoluteMinimumDamage}"
            });
        }
        
        // Finalize result
        result.finalDamage = Mathf.RoundToInt(calculatedDamage);
        result.totalMultiplier = result.finalDamage / (float)result.baseDamage;
        
        // Cache result
        if (enableDamageCache)
        {
            damageCache[cacheKey] = new CachedDamageResult(result.finalDamage, result);
        }
        
        // Update statistics
        damageStats.RecordDamage(result);
        
        // Trigger events
        OnDamageCalculated?.Invoke(result);
        
        if (logDamageCalculations)
        {
            Debug.Log($"DamageCalculator: Final damage calculation - {result.baseDamage} → {result.finalDamage} " +
                     $"(x{result.totalMultiplier:F2}, Critical: {result.isCritical}, Modifiers: {result.appliedModifiers.Count})");
        }
        
        return result;
    }
    
    #endregion
    
    #region Damage Calculation Components
    
    /// <summary>
    /// Applies damage variation
    /// </summary>
    private float ApplyDamageVariation(float baseDamage)
    {
        if (!enableDamageVariation) return baseDamage;
        
        int variation = Random.Range(minDamageVariation, maxDamageVariation + 1);
        return Mathf.Max(0, baseDamage + variation);
    }
    
    /// <summary>
    /// Rolls for critical hit
    /// </summary>
    private bool RollForCriticalHit(IAttacker attacker, IAttackable target)
    {
        if (!enableCriticalHits) return false;
        
        float criticalChance = baseCriticalChance;
        
        // Modify critical chance based on attacker properties
        // This could be expanded with attacker-specific critical modifiers
        
        return Random.Range(0f, 1f) < criticalChance;
    }
    
    /// <summary>
    /// Applies critical hit damage
    /// </summary>
    private float ApplyCriticalHit(float baseDamage)
    {
        float criticalDamage = baseDamage * criticalDamageMultiplier;
        
        if (allowCriticalVariation)
        {
            float variation = Random.Range(-criticalVariationRange, criticalVariationRange);
            criticalDamage *= (1f + variation);
        }
        
        return criticalDamage;
    }
    
    /// <summary>
    /// Calculates tactical modifiers based on positioning
    /// </summary>
    private float CalculateTacticalModifiers(IAttacker attacker, IAttackable target)
    {
        float modifier = 1.0f;
        
        if (attacker.Transform == null || target.Transform == null)
        {
            return modifier;
        }
        
        Vector3 attackerPos = attacker.Transform.position;
        Vector3 targetPos = target.Transform.position;
        
        // Flanking bonus
        if (IsFlankingPosition(attackerPos, targetPos))
        {
            modifier += flankingDamageBonus;
        }
        
        // Rear attack bonus
        if (IsRearAttack(attackerPos, targetPos))
        {
            modifier += rearAttackDamageBonus;
        }
        
        // Cover reduction
        if (IsTargetInCover(target, attackerPos))
        {
            modifier -= coverDamageReduction;
        }
        
        // Elevation bonus
        if (HasElevationAdvantage(attackerPos, targetPos))
        {
            modifier += elevationDamageBonus;
        }
        
        return Mathf.Max(0.1f, modifier); // Ensure we don't go below 10% damage
    }
    
    /// <summary>
    /// Calculates team-based modifiers
    /// </summary>
    private float CalculateTeamModifiers(IAttacker attacker, IAttackable target)
    {
        if (!enableTeamModifiers) return 1.0f;
        
        // Friendly fire reduction
        if (attacker.Team == target.Team)
        {
            return 1.0f - friendlyFireReduction;
        }
        
        // Could add team-specific bonuses/penalties here
        
        return 1.0f;
    }
    
    /// <summary>
    /// Calculates environmental modifiers
    /// </summary>
    private float CalculateEnvironmentalModifiers(IAttacker attacker, IAttackable target)
    {
        if (!enableEnvironmentalModifiers) return 1.0f;
        
        float modifier = 1.0f;
        
        // Weather effects
        modifier *= weatherDamageModifier;
        
        // Terrain effects
        modifier *= terrainDamageModifier;
        
        // Distance effects
        if (enableDistanceModifiers)
        {
            float distance = Vector3.Distance(attacker.Transform.position, target.Transform.position);
            float distanceModifier = CalculateDistanceModifier(distance);
            modifier *= distanceModifier;
        }
        
        return modifier;
    }
    
    /// <summary>
    /// Calculates distance-based damage modifier
    /// </summary>
    private float CalculateDistanceModifier(float distance)
    {
        if (Mathf.Approximately(distance, optimalDamageDistance))
        {
            return 1.0f;
        }
        
        float distanceDifference = Mathf.Abs(distance - optimalDamageDistance);
        float falloff = distanceDifference * distanceDamageFalloff;
        
        return Mathf.Max(0.2f, 1.0f - falloff);
    }
    
    #endregion
    
    #region Tactical Position Analysis
    
    /// <summary>
    /// Checks if attacker is in flanking position
    /// </summary>
    private bool IsFlankingPosition(Vector3 attackerPos, Vector3 targetPos)
    {
        // Simple flanking detection - could be enhanced with cover analyzer
        if (coverAnalyzer != null)
        {
            var flankingOpportunities = coverAnalyzer.FindFlankingOpportunities(attackerPos, new Vector3[] { targetPos });
            return flankingOpportunities.Length > 0;
        }
        
        // Fallback: basic side attack detection
        Vector3 toAttacker = (attackerPos - targetPos).normalized;
        float angle = Vector3.Angle(Vector3.forward, toAttacker);
        return angle > 45f && angle < 135f; // Side angles
    }
    
    /// <summary>
    /// Checks if attack is from behind
    /// </summary>
    private bool IsRearAttack(Vector3 attackerPos, Vector3 targetPos)
    {
        Vector3 toAttacker = (attackerPos - targetPos).normalized;
        float angle = Vector3.Angle(Vector3.forward, toAttacker);
        return angle > 135f; // Behind target
    }
    
    /// <summary>
    /// Checks if target is in cover from attacker
    /// </summary>
    private bool IsTargetInCover(IAttackable target, Vector3 attackerPos)
    {
        if (coverAnalyzer != null && target.Transform != null)
        {
            var coverResult = coverAnalyzer.AnalyzeCoverAtPosition(target.Transform.position);
            return coverResult.coverQuality > CoverAnalyzer.CoverQuality.None;
        }
        
        // Fallback: check line of sight
        if (lineOfSightManager != null && target.Transform != null)
        {
            return !lineOfSightManager.HasLineOfSight(attackerPos, target.Transform.position);
        }
        
        return false;
    }
    
    /// <summary>
    /// Checks if attacker has elevation advantage
    /// </summary>
    private bool HasElevationAdvantage(Vector3 attackerPos, Vector3 targetPos)
    {
        return attackerPos.y > targetPos.y + 0.5f; // At least 0.5 units higher
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Generates cache key for damage calculation
    /// </summary>
    private string GetDamageCacheKey(int baseDamage, IAttacker attacker, IAttackable target)
    {
        Vector3 attackerPos = attacker.Transform?.position ?? Vector3.zero;
        Vector3 targetPos = target.Transform?.position ?? Vector3.zero;
        
        return $"{baseDamage}_{attacker.GetHashCode()}_{target.GetHashCode()}_{attackerPos}_{targetPos}";
    }
    
    /// <summary>
    /// Gets description for tactical modifier
    /// </summary>
    private string GetTacticalModifierDescription(float modifier)
    {
        if (modifier > 1.0f)
        {
            return $"Tactical advantage: +{(modifier - 1.0f) * 100f:F0}%";
        }
        else if (modifier < 1.0f)
        {
            return $"Tactical disadvantage: -{(1.0f - modifier) * 100f:F0}%";
        }
        else
        {
            return "No tactical modifier";
        }
    }
    
    /// <summary>
    /// Gets description for team modifier
    /// </summary>
    private string GetTeamModifierDescription(IAttacker attacker, IAttackable target, float modifier)
    {
        if (attacker.Team == target.Team)
        {
            return $"Friendly fire: -{(1.0f - modifier) * 100f:F0}% damage";
        }
        else if (modifier > 1.0f)
        {
            return $"Team bonus: +{(modifier - 1.0f) * 100f:F0}%";
        }
        else if (modifier < 1.0f)
        {
            return $"Team penalty: -{(1.0f - modifier) * 100f:F0}%";
        }
        else
        {
            return "No team modifier";
        }
    }
    
    /// <summary>
    /// Updates damage calculation settings at runtime
    /// </summary>
    public void UpdateDamageSettings(int newBaseDamage, float newCriticalChance, float newCriticalMultiplier)
    {
        baseDamageAmount = newBaseDamage;
        baseCriticalChance = newCriticalChance;
        criticalDamageMultiplier = newCriticalMultiplier;
        
        // Clear cache when settings change
        if (enableDamageCache)
        {
            damageCache.Clear();
        }
        
        if (enableDamageDebugging)
        {
            Debug.Log($"DamageCalculator: Settings updated - Base: {baseDamageAmount}, " +
                     $"Critical: {baseCriticalChance:P0} x{criticalDamageMultiplier}");
        }
    }
    
    /// <summary>
    /// Gets damage calculation statistics
    /// </summary>
    public string GetDamageStats()
    {
        if (damageStats == null)
        {
            return "No damage statistics available";
        }
        
        return $"Damage Stats - Total: {damageStats.totalDamageCalculations}, " +
               $"Avg Damage: {damageStats.averageDamage:F1}, " +
               $"Critical Rate: {damageStats.criticalHitRate:P1}, " +
               $"Max Damage: {damageStats.maxDamageDealt}";
    }
    
    /// <summary>
    /// Clears damage cache
    /// </summary>
    public void ClearDamageCache()
    {
        if (enableDamageCache)
        {
            damageCache.Clear();
            
            if (enableDamageDebugging)
            {
                Debug.Log("DamageCalculator: Cache cleared");
            }
        }
    }
    
    #endregion
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Clear cache
        damageCache?.Clear();
        
        // Clear event references
        OnDamageCalculated = null;
        OnCriticalHit = null;
        OnTacticalModifierApplied = null;
    }
}

/// <summary>
/// Damage calculation result with detailed breakdown
/// </summary>
[System.Serializable]
public class DamageCalculationResult
{
    public IAttacker attacker;
    public IAttackable target;
    public int baseDamage;
    public int finalDamage;
    public float totalMultiplier;
    public bool isCritical;
    public List<DamageModifier> appliedModifiers;
    public bool isValid = true;
    public string errorMessage;
    
    /// <summary>
    /// Creates an invalid damage result
    /// </summary>
    public static DamageCalculationResult Invalid(string error)
    {
        return new DamageCalculationResult
        {
            baseDamage = 0,
            finalDamage = 0,
            totalMultiplier = 0f,
            isCritical = false,
            appliedModifiers = new List<DamageModifier>(),
            isValid = false,
            errorMessage = error
        };
    }
}

/// <summary>
/// Individual damage modifier applied during calculation
/// </summary>
[System.Serializable]
public class DamageModifier
{
    public string name;
    public float multiplier;
    public int additive;
    public string description;
}

/// <summary>
/// Damage calculation statistics tracking
/// </summary>
[System.Serializable]
public class DamageStatistics
{
    public int totalDamageCalculations = 0;
    public int totalDamageDealt = 0;
    public int criticalHitsDealt = 0;
    public int maxDamageDealt = 0;
    public int minDamageDealt = int.MaxValue;
    
    public float averageDamage => totalDamageCalculations > 0 ? (float)totalDamageDealt / totalDamageCalculations : 0f;
    public float criticalHitRate => totalDamageCalculations > 0 ? (float)criticalHitsDealt / totalDamageCalculations : 0f;
    
    /// <summary>
    /// Records a damage calculation result
    /// </summary>
    public void RecordDamage(DamageCalculationResult result)
    {
        if (!result.isValid) return;
        
        totalDamageCalculations++;
        totalDamageDealt += result.finalDamage;
        
        if (result.isCritical)
        {
            criticalHitsDealt++;
        }
        
        if (result.finalDamage > maxDamageDealt)
        {
            maxDamageDealt = result.finalDamage;
        }
        
        if (result.finalDamage < minDamageDealt)
        {
            minDamageDealt = result.finalDamage;
        }
    }
    
    /// <summary>
    /// Resets all statistics
    /// </summary>
    public void Reset()
    {
        totalDamageCalculations = 0;
        totalDamageDealt = 0;
        criticalHitsDealt = 0;
        maxDamageDealt = 0;
        minDamageDealt = int.MaxValue;
    }
}