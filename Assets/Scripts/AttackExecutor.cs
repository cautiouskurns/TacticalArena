using UnityEngine;
using System.Collections;

/// <summary>
/// Attack execution system that handles attack animation and damage application.
/// Coordinates with CombatManager to perform validated attacks with visual feedback.
/// Manages attack timing, animation, and damage application for tactical combat.
/// </summary>
public class AttackExecutor : MonoBehaviour
{
    [Header("Execution Configuration")]
    [SerializeField] private float attackAnimationDuration = 0.5f;
    [SerializeField] private float attackCooldownDuration = 0.2f;
    [SerializeField] private bool enableAttackEffects = true;
    [SerializeField] private bool enableDamageNumbers = true;
    
    [Header("Animation Settings")]
    [SerializeField] private AnimationCurve attackAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float attackLungeDistance = 0.3f;
    [SerializeField] private float attackRecoilDistance = 0.1f;
    [SerializeField] private bool enableAttackerAnimation = true;
    [SerializeField] private bool enableTargetAnimation = true;
    
    [Header("Visual Effects")]
    [SerializeField] private Color attackEffectColor = Color.red;
    [SerializeField] private float effectDuration = 0.3f;
    [SerializeField] private float effectIntensity = 1.5f;
    [SerializeField] private bool enableScreenShake = false;
    [SerializeField] private float shakeIntensity = 0.1f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableExecutionLogging = true;
    
    // System references
    private CombatManager combatManager;
    
    // Execution state
    private bool executingAttack = false;
    private Coroutine currentAttackCoroutine;
    
    // Events
    public System.Action<IAttacker, IAttackable, int> OnAttackExecuted;
    public System.Action<IAttacker, IAttackable, string> OnAttackFailed;
    public System.Action<IAttacker, IAttackable> OnAttackStarted;
    public System.Action<IAttacker, IAttackable> OnAttackCompleted;
    
    // Properties
    public bool IsExecutingAttack => executingAttack;
    public float AttackAnimationDuration => attackAnimationDuration;
    
    void Awake()
    {
        InitializeExecutor();
    }
    
    void Start()
    {
        FindSystemReferences();
    }
    
    /// <summary>
    /// Initializes the attack executor
    /// </summary>
    private void InitializeExecutor()
    {
        if (enableExecutionLogging)
        {
            Debug.Log("AttackExecutor initialized - Combat execution system ready");
        }
    }
    
    /// <summary>
    /// Finds references to other systems
    /// </summary>
    private void FindSystemReferences()
    {
        combatManager = GetComponent<CombatManager>();
        if (combatManager == null)
        {
            Debug.LogWarning("AttackExecutor: CombatManager not found on same GameObject");
        }
        
        if (enableExecutionLogging)
        {
            Debug.Log($"AttackExecutor found references - CombatManager: {combatManager != null}");
        }
    }
    
    /// <summary>
    /// Executes an attack from attacker to target with the specified damage
    /// </summary>
    public AttackResult ExecuteAttack(IAttacker attacker, IAttackable target, int damage)
    {
        if (executingAttack)
        {
            return AttackResult.Failed("Another attack is already executing");
        }
        
        if (attacker == null)
        {
            return AttackResult.Failed("Attacker is null");
        }
        
        if (target == null)
        {
            return AttackResult.Failed("Target is null");
        }
        
        if (enableExecutionLogging)
        {
            Debug.Log($"AttackExecutor: Executing attack from {attacker.GetDisplayInfo()} to {target.GetDisplayInfo()} for {damage} damage");
        }
        
        // Start attack execution coroutine
        if (currentAttackCoroutine != null)
        {
            StopCoroutine(currentAttackCoroutine);
        }
        currentAttackCoroutine = StartCoroutine(ExecuteAttackCoroutine(attacker, target, damage));
        
        return AttackResult.Success(damage, "Attack execution started");
    }
    
    /// <summary>
    /// Coroutine that handles the complete attack execution sequence
    /// </summary>
    private IEnumerator ExecuteAttackCoroutine(IAttacker attacker, IAttackable target, int damage)
    {
        executingAttack = true;
        OnAttackStarted?.Invoke(attacker, target);
        
        bool attackSuccessful = true;
        string errorMessage = "";
        
        // Phase 1: Pre-attack preparation
        yield return StartCoroutine(PreAttackPhase(attacker, target));
        
        // Phase 2: Attack animation and execution
        if (attackSuccessful)
        {
            yield return StartCoroutine(AttackPhase(attacker, target, damage));
        }
        
        // Phase 3: Post-attack effects and cleanup
        if (attackSuccessful)
        {
            yield return StartCoroutine(PostAttackPhase(attacker, target));
        }
        
        // Handle results
        if (attackSuccessful)
        {
            // Notify successful completion
            OnAttackExecuted?.Invoke(attacker, target, damage);
            
            if (enableExecutionLogging)
            {
                Debug.Log($"AttackExecutor: Attack completed successfully - {damage} damage dealt");
            }
        }
        else
        {
            // Handle any errors during execution
            OnAttackFailed?.Invoke(attacker, target, errorMessage);
            
            if (enableExecutionLogging)
            {
                Debug.LogError($"AttackExecutor: Attack execution failed - {errorMessage}");
            }
        }
        
        // Cleanup
        executingAttack = false;
        OnAttackCompleted?.Invoke(attacker, target);
        currentAttackCoroutine = null;
    }
    
    /// <summary>
    /// Pre-attack preparation phase
    /// </summary>
    private IEnumerator PreAttackPhase(IAttacker attacker, IAttackable target)
    {
        // Brief pause for anticipation
        yield return new WaitForSeconds(0.1f);
        
        // Prepare attacker for attack (visual indication, etc.)
        if (enableAttackerAnimation && attacker.Transform != null)
        {
            // Slight forward lean or preparation animation
            Vector3 originalPos = attacker.Transform.position;
            Vector3 direction = (target.Transform.position - attacker.Transform.position).normalized;
            Vector3 prepPosition = originalPos + direction * (attackRecoilDistance * -0.5f);
            
            float elapsed = 0f;
            float prepDuration = attackAnimationDuration * 0.2f;
            
            while (elapsed < prepDuration)
            {
                float t = elapsed / prepDuration;
                t = attackAnimationCurve.Evaluate(t);
                
                if (attacker.Transform != null)
                {
                    attacker.Transform.position = Vector3.Lerp(originalPos, prepPosition, t);
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (attacker.Transform != null)
            {
                attacker.Transform.position = prepPosition;
            }
        }
    }
    
    /// <summary>
    /// Main attack execution phase
    /// </summary>
    private IEnumerator AttackPhase(IAttacker attacker, IAttackable target, int damage)
    {
        Vector3 attackerOriginalPos = attacker.Transform.position;
        Vector3 targetOriginalPos = target.Transform.position;
        
        // Calculate attack direction
        Vector3 attackDirection = (target.Transform.position - attacker.Transform.position).normalized;
        Vector3 lungePosition = attackerOriginalPos + attackDirection * attackLungeDistance;
        
        float elapsed = 0f;
        float mainAnimationDuration = attackAnimationDuration * 0.6f;
        bool damageApplied = false;
        
        while (elapsed < mainAnimationDuration)
        {
            float t = elapsed / mainAnimationDuration;
            float animationT = attackAnimationCurve.Evaluate(t);
            
            // Attacker animation (lunge forward)
            if (enableAttackerAnimation && attacker.Transform != null)
            {
                attacker.Transform.position = Vector3.Lerp(attackerOriginalPos, lungePosition, animationT);
            }
            
            // Apply damage at the peak of the animation (halfway through)
            if (!damageApplied && t >= 0.5f)
            {
                ApplyDamageToTarget(attacker, target, damage);
                damageApplied = true;
                
                // Trigger visual effects
                if (enableAttackEffects)
                {
                    StartCoroutine(PlayAttackEffects(target));
                }
                
                // Screen shake if enabled
                if (enableScreenShake)
                {
                    StartCoroutine(CameraShake());
                }
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure damage is applied even if timing was off
        if (!damageApplied)
        {
            ApplyDamageToTarget(attacker, target, damage);
        }
        
        // Return attacker to original position
        if (enableAttackerAnimation && attacker.Transform != null)
        {
            elapsed = 0f;
            float returnDuration = attackAnimationDuration * 0.2f;
            Vector3 startPos = attacker.Transform.position;
            
            while (elapsed < returnDuration)
            {
                float t = elapsed / returnDuration;
                
                if (attacker.Transform != null)
                {
                    attacker.Transform.position = Vector3.Lerp(startPos, attackerOriginalPos, t);
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (attacker.Transform != null)
            {
                attacker.Transform.position = attackerOriginalPos;
            }
        }
    }
    
    /// <summary>
    /// Post-attack cleanup and effects phase
    /// </summary>
    private IEnumerator PostAttackPhase(IAttacker attacker, IAttackable target)
    {
        // Attack cooldown period
        yield return new WaitForSeconds(attackCooldownDuration);
        
        // Notify attacker that attack was performed
        attacker.OnAttackPerformed(target, combatManager?.BaseDamage ?? 1);
    }
    
    /// <summary>
    /// Applies damage to the target and handles the result
    /// </summary>
    private void ApplyDamageToTarget(IAttacker attacker, IAttackable target, int damage)
    {
        Debug.Log($"AttackExecutor: Applying {damage} damage from {attacker.GetDisplayInfo()} to {target.GetDisplayInfo()}");
        Debug.Log($"AttackExecutor: Target health before damage - {target.CurrentHealth}/{target.MaxHealth}");
        
        // Apply damage to target
        int actualDamage = target.TakeDamage(damage, attacker);
        
        Debug.Log($"AttackExecutor: Actual damage applied - {actualDamage}");
        Debug.Log($"AttackExecutor: Target health after damage - {target.CurrentHealth}/{target.MaxHealth}");
        
        // Notify target that it was attacked
        target.OnAttacked(attacker, actualDamage);
        
        // Check if target was killed
        if (!target.IsAlive)
        {
            target.OnDeath(attacker);
            
            if (enableExecutionLogging)
            {
                Debug.Log($"AttackExecutor: Target {target.GetDisplayInfo()} was killed by {attacker.GetDisplayInfo()}");
            }
        }
        
        if (enableExecutionLogging)
        {
            Debug.Log($"AttackExecutor: Applied {actualDamage} damage to {target.GetDisplayInfo()}");
        }
    }
    
    /// <summary>
    /// Plays visual attack effects on the target
    /// </summary>
    private IEnumerator PlayAttackEffects(IAttackable target)
    {
        if (!enableAttackEffects || target.Transform == null) yield break;
        
        // Get target renderer for color effects
        Renderer targetRenderer = target.Transform.GetComponent<Renderer>();
        if (targetRenderer == null) yield break;
        
        Color originalColor = targetRenderer.material.color;
        Color effectColor = Color.Lerp(originalColor, attackEffectColor, 0.7f);
        
        float elapsed = 0f;
        
        // Flash effect
        while (elapsed < effectDuration)
        {
            float t = elapsed / effectDuration;
            float intensity = Mathf.Sin(t * Mathf.PI) * effectIntensity;
            
            Color currentColor = Color.Lerp(originalColor, effectColor, intensity);
            targetRenderer.material.color = currentColor;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Restore original color
        targetRenderer.material.color = originalColor;
    }
    
    /// <summary>
    /// Simple camera shake effect
    /// </summary>
    private IEnumerator CameraShake()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) yield break;
        
        Vector3 originalPosition = mainCamera.transform.position;
        float elapsed = 0f;
        float shakeDuration = 0.15f;
        
        while (elapsed < shakeDuration)
        {
            float t = 1.0f - (elapsed / shakeDuration);
            Vector3 randomOffset = Random.insideUnitSphere * shakeIntensity * t;
            randomOffset.z = 0; // Keep camera on same depth
            
            mainCamera.transform.position = originalPosition + randomOffset;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        mainCamera.transform.position = originalPosition;
    }
    
    /// <summary>
    /// Cancels the current attack execution if running
    /// </summary>
    public void CancelCurrentAttack()
    {
        if (currentAttackCoroutine != null)
        {
            StopCoroutine(currentAttackCoroutine);
            currentAttackCoroutine = null;
        }
        
        executingAttack = false;
        
        if (enableExecutionLogging)
        {
            Debug.Log("AttackExecutor: Current attack canceled");
        }
    }
    
    /// <summary>
    /// Gets execution info for debugging
    /// </summary>
    public string GetExecutionInfo()
    {
        return $"Execution State: {(executingAttack ? "Executing Attack" : "Ready")}, " +
               $"Animation Duration: {attackAnimationDuration}s, " +
               $"Cooldown: {attackCooldownDuration}s, " +
               $"Effects: {enableAttackEffects}";
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Stop any running attack
        CancelCurrentAttack();
        
        // Clear event references
        OnAttackExecuted = null;
        OnAttackFailed = null;
        OnAttackStarted = null;
        OnAttackCompleted = null;
    }
}