using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Movement animation enhancer that adds anticipation and follow-through effects to existing movement animations.
/// Provides professional game feel through subtle animation polish including scale effects, rotation, and particles.
/// Integrates with existing MovementAnimator while preserving grid snapping precision.
/// </summary>
public class MovementAnimationEnhancer : MonoBehaviour
{
    [Header("Anticipation Settings")]
    [SerializeField] private bool enableAnticipation = true;
    [SerializeField] private float anticipationScale = 1.1f;
    [SerializeField] private float anticipationDuration = 0.1f;
    [SerializeField] private AnimationCurve anticipationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool enableRotationAnticipation = false;
    [SerializeField] private float rotationAnticipation = 5f;
    
    [Header("Follow-Through Settings")]
    [SerializeField] private bool enableFollowThrough = true;
    [SerializeField] private float bounceIntensity = 0.2f;
    [SerializeField] private float bounceDuration = 0.3f;
    [SerializeField] private AnimationCurve bounceCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private bool enableSettleEffect = true;
    [SerializeField] private float settleDuration = 0.2f;
    
    [Header("Visual Effects")]
    [SerializeField] private bool enableParticles = true;
    [SerializeField] private bool enableTrails = false;
    [SerializeField] private float visualEffectIntensity = 1.0f;
    [SerializeField] private Color effectColor = Color.white;
    [SerializeField] private bool enableScreenShake = false;
    [SerializeField] private float screenShakeIntensity = 0.1f;
    
    [Header("Performance Settings")]
    [SerializeField] private int maxConcurrentEnhancements = 8;
    [SerializeField] private bool optimizeForPerformance = true;
    [SerializeField] private bool useObjectPooling = true;
    [SerializeField] private float effectCullingDistance = 20f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogging = false;
    [SerializeField] private bool showEnhancementGizmos = false;
    
    // System references
    private MovementManager movementManager;
    private MovementAnimator movementAnimator;
    private Camera mainCamera;
    
    // Enhancement state
    private Dictionary<Transform, EnhancementState> activeEnhancements = new Dictionary<Transform, EnhancementState>();
    private Queue<ParticleSystem> particlePool = new Queue<ParticleSystem>();
    private Queue<TrailRenderer> trailPool = new Queue<TrailRenderer>();
    
    // Events
    public System.Action<Transform> OnEnhancementStarted;
    public System.Action<Transform> OnEnhancementCompleted;
    public System.Action<Transform, EnhancementPhase> OnPhaseChanged;
    
    /// <summary>
    /// Enhancement phases for animation lifecycle
    /// </summary>
    public enum EnhancementPhase
    {
        Anticipation,
        Movement,
        FollowThrough,
        Settle,
        Complete
    }
    
    /// <summary>
    /// State tracking for individual unit enhancements
    /// </summary>
    private class EnhancementState
    {
        public Transform unit;
        public Vector3 originalScale;
        public Quaternion originalRotation;
        public Vector3 originalPosition;
        public EnhancementPhase currentPhase;
        public float phaseStartTime;
        public Coroutine enhancementCoroutine;
        public ParticleSystem particles;
        public TrailRenderer trail;
        public bool isActive;
    }
    
    void Awake()
    {
        InitializeEnhancer();
    }
    
    void Start()
    {
        FindSystemReferences();
        SetupEventListeners();
        InitializeObjectPools();
    }
    
    void Update()
    {
        if (optimizeForPerformance)
        {
            CullDistantEffects();
        }
    }
    
    /// <summary>
    /// Initializes the movement animation enhancer
    /// </summary>
    private void InitializeEnhancer()
    {
        if (enableDebugLogging)
        {
            Debug.Log("MovementAnimationEnhancer initialized");
        }
    }
    
    /// <summary>
    /// Finds references to other systems
    /// </summary>
    private void FindSystemReferences()
    {
        movementManager = GetComponent<MovementManager>();
        if (movementManager == null)
        {
            Debug.LogError("MovementAnimationEnhancer: MovementManager not found on same GameObject!");
        }
        
        movementAnimator = GetComponent<MovementAnimator>();
        if (movementAnimator == null)
        {
            Debug.LogError("MovementAnimationEnhancer: MovementAnimator not found on same GameObject!");
        }
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"MovementAnimationEnhancer found references - Movement: {movementManager != null}, Animator: {movementAnimator != null}, Camera: {mainCamera != null}");
        }
    }
    
    /// <summary>
    /// Sets up event listeners for movement events
    /// </summary>
    private void SetupEventListeners()
    {
        if (movementManager != null)
        {
            movementManager.OnMovementStarted += OnMovementStarted;
            movementManager.OnMovementCompleted += OnMovementCompleted;
        }
        
        if (movementAnimator != null)
        {
            movementAnimator.OnAnimationComplete += OnAnimationCompleted;
        }
    }
    
    /// <summary>
    /// Initializes object pools for performance optimization
    /// </summary>
    private void InitializeObjectPools()
    {
        if (!useObjectPooling) return;
        
        // Initialize particle pool
        if (enableParticles)
        {
            for (int i = 0; i < maxConcurrentEnhancements; i++)
            {
                ParticleSystem particles = CreatePooledParticleSystem();
                particles.gameObject.SetActive(false);
                particlePool.Enqueue(particles);
            }
        }
        
        // Initialize trail pool
        if (enableTrails)
        {
            for (int i = 0; i < maxConcurrentEnhancements; i++)
            {
                TrailRenderer trail = CreatePooledTrailRenderer();
                trail.gameObject.SetActive(false);
                trailPool.Enqueue(trail);
            }
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"MovementAnimationEnhancer: Initialized pools - Particles: {particlePool.Count}, Trails: {trailPool.Count}");
        }
    }
    
    /// <summary>
    /// Creates a pooled particle system
    /// </summary>
    private ParticleSystem CreatePooledParticleSystem()
    {
        GameObject particleObj = new GameObject("MovementParticles");
        particleObj.transform.SetParent(transform);
        
        ParticleSystem particles = particleObj.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startColor = effectColor;
        main.startSize = 0.1f * visualEffectIntensity;
        main.startSpeed = 2f * visualEffectIntensity;
        main.maxParticles = Mathf.RoundToInt(50 * visualEffectIntensity);
        main.startLifetime = 1f;
        
        var emission = particles.emission;
        emission.rateOverTime = 20f * visualEffectIntensity;
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        
        return particles;
    }
    
    /// <summary>
    /// Creates a pooled trail renderer
    /// </summary>
    private TrailRenderer CreatePooledTrailRenderer()
    {
        GameObject trailObj = new GameObject("MovementTrail");
        trailObj.transform.SetParent(transform);
        
        TrailRenderer trail = trailObj.AddComponent<TrailRenderer>();
        trail.material = new Material(Shader.Find("Sprites/Default"));
        
        // Set up gradient for trail color
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(effectColor, 0.0f), new GradientColorKey(effectColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        trail.colorGradient = gradient;
        
        trail.time = 0.5f;
        trail.startWidth = 0.1f * visualEffectIntensity;
        trail.endWidth = 0.05f * visualEffectIntensity;
        
        return trail;
    }
    
    /// <summary>
    /// Culls distant effects for performance
    /// </summary>
    private void CullDistantEffects()
    {
        if (mainCamera == null) return;
        
        Vector3 cameraPosition = mainCamera.transform.position;
        List<Transform> toRemove = new List<Transform>();
        
        foreach (var kvp in activeEnhancements)
        {
            Transform unit = kvp.Key;
            if (unit == null)
            {
                toRemove.Add(unit);
                continue;
            }
            
            float distance = Vector3.Distance(cameraPosition, unit.position);
            if (distance > effectCullingDistance)
            {
                // Disable effects for distant units
                EnhancementState state = kvp.Value;
                if (state.particles != null && state.particles.gameObject.activeSelf)
                {
                    state.particles.gameObject.SetActive(false);
                }
                if (state.trail != null && state.trail.gameObject.activeSelf)
                {
                    state.trail.gameObject.SetActive(false);
                }
            }
        }
        
        // Clean up null references
        foreach (Transform unit in toRemove)
        {
            activeEnhancements.Remove(unit);
        }
    }
    
    /// <summary>
    /// Called when movement starts
    /// </summary>
    private void OnMovementStarted(IMovable movableUnit, Vector2Int targetPosition)
    {
        if (movableUnit?.Transform == null) return;
        
        Transform unitTransform = movableUnit.Transform;
        
        // Check if we're at the limit
        if (activeEnhancements.Count >= maxConcurrentEnhancements)
        {
            if (enableDebugLogging)
            {
                Debug.LogWarning($"MovementAnimationEnhancer: Reached max concurrent enhancements ({maxConcurrentEnhancements})");
            }
            return;
        }
        
        StartMovementEnhancement(unitTransform);
    }
    
    /// <summary>
    /// Called when movement completes
    /// </summary>
    private void OnMovementCompleted(IMovable movableUnit, Vector2Int finalPosition)
    {
        if (movableUnit?.Transform == null) return;
        
        Transform unitTransform = movableUnit.Transform;
        
        if (activeEnhancements.ContainsKey(unitTransform))
        {
            StartFollowThroughPhase(unitTransform);
        }
    }
    
    /// <summary>
    /// Called when animation completes from MovementAnimator
    /// </summary>
    private void OnAnimationCompleted(Transform unitTransform, Vector3 finalPosition)
    {
        if (unitTransform == null) return;
        
        if (activeEnhancements.ContainsKey(unitTransform))
        {
            StartFollowThroughPhase(unitTransform);
        }
    }
    
    /// <summary>
    /// Starts movement enhancement for a unit
    /// </summary>
    private void StartMovementEnhancement(Transform unitTransform)
    {
        if (unitTransform == null) return;
        
        // Clean up any existing enhancement
        if (activeEnhancements.ContainsKey(unitTransform))
        {
            CleanupEnhancement(unitTransform);
        }
        
        // Create enhancement state
        EnhancementState state = new EnhancementState
        {
            unit = unitTransform,
            originalScale = unitTransform.localScale,
            originalRotation = unitTransform.rotation,
            originalPosition = unitTransform.position,
            currentPhase = EnhancementPhase.Anticipation,
            phaseStartTime = Time.time,
            isActive = true
        };
        
        // Setup particles if enabled
        if (enableParticles)
        {
            state.particles = GetPooledParticleSystem();
            if (state.particles != null)
            {
                state.particles.transform.position = unitTransform.position;
                state.particles.gameObject.SetActive(true);
            }
        }
        
        // Setup trail if enabled
        if (enableTrails)
        {
            state.trail = GetPooledTrailRenderer();
            if (state.trail != null)
            {
                state.trail.transform.SetParent(unitTransform);
                state.trail.transform.localPosition = Vector3.zero;
                state.trail.gameObject.SetActive(true);
            }
        }
        
        activeEnhancements[unitTransform] = state;
        
        // Start enhancement coroutine
        state.enhancementCoroutine = StartCoroutine(RunEnhancementSequence(state));
        
        OnEnhancementStarted?.Invoke(unitTransform);
        
        if (enableDebugLogging)
        {
            Debug.Log($"MovementAnimationEnhancer: Started enhancement for {unitTransform.name}");
        }
    }
    
    /// <summary>
    /// Runs the complete enhancement sequence
    /// </summary>
    private IEnumerator RunEnhancementSequence(EnhancementState state)
    {
        if (state?.unit == null) yield break;
        
        // Phase 1: Anticipation
        if (enableAnticipation)
        {
            yield return StartCoroutine(RunAnticipationPhase(state));
        }
        
        // Phase 2: Movement (handled by MovementAnimator, we just track)
        state.currentPhase = EnhancementPhase.Movement;
        state.phaseStartTime = Time.time;
        OnPhaseChanged?.Invoke(state.unit, EnhancementPhase.Movement);
        
        // Wait for movement to complete (this will be triggered by OnMovementCompleted event)
        while (state.currentPhase == EnhancementPhase.Movement && state.isActive)
        {
            yield return null;
        }
    }
    
    /// <summary>
    /// Runs the anticipation phase
    /// </summary>
    private IEnumerator RunAnticipationPhase(EnhancementState state)
    {
        if (state?.unit == null) yield break;
        
        OnPhaseChanged?.Invoke(state.unit, EnhancementPhase.Anticipation);
        
        float elapsed = 0f;
        Vector3 startScale = state.originalScale;
        Vector3 targetScale = startScale * anticipationScale;
        Quaternion startRotation = state.originalRotation;
        
        while (elapsed < anticipationDuration)
        {
            if (state.unit == null) yield break;
            
            float progress = elapsed / anticipationDuration;
            float curveValue = anticipationCurve.Evaluate(progress);
            
            // Scale animation
            state.unit.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
            
            // Rotation anticipation
            if (enableRotationAnticipation)
            {
                float rotationAmount = Mathf.Sin(progress * Mathf.PI) * rotationAnticipation;
                state.unit.rotation = startRotation * Quaternion.Euler(0, rotationAmount, 0);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Return to original scale and rotation before movement
        state.unit.localScale = state.originalScale;
        state.unit.rotation = state.originalRotation;
        
        if (enableDebugLogging)
        {
            Debug.Log($"MovementAnimationEnhancer: Completed anticipation phase for {state.unit.name}");
        }
    }
    
    /// <summary>
    /// Starts the follow-through phase
    /// </summary>
    private void StartFollowThroughPhase(Transform unitTransform)
    {
        if (!activeEnhancements.ContainsKey(unitTransform)) return;
        
        EnhancementState state = activeEnhancements[unitTransform];
        if (state.enhancementCoroutine != null)
        {
            StopCoroutine(state.enhancementCoroutine);
        }
        
        state.enhancementCoroutine = StartCoroutine(RunFollowThroughSequence(state));
    }
    
    /// <summary>
    /// Runs the follow-through sequence
    /// </summary>
    private IEnumerator RunFollowThroughSequence(EnhancementState state)
    {
        if (state?.unit == null) yield break;
        
        // Phase 3: Follow-Through (bounce effect)
        if (enableFollowThrough)
        {
            yield return StartCoroutine(RunFollowThroughPhase(state));
        }
        
        // Phase 4: Settle
        if (enableSettleEffect)
        {
            yield return StartCoroutine(RunSettlePhase(state));
        }
        
        // Phase 5: Complete
        CompleteEnhancement(state.unit);
    }
    
    /// <summary>
    /// Runs the follow-through phase (bounce effect)
    /// </summary>
    private IEnumerator RunFollowThroughPhase(EnhancementState state)
    {
        if (state?.unit == null) yield break;
        
        state.currentPhase = EnhancementPhase.FollowThrough;
        state.phaseStartTime = Time.time;
        OnPhaseChanged?.Invoke(state.unit, EnhancementPhase.FollowThrough);
        
        float elapsed = 0f;
        Vector3 originalScale = state.originalScale;
        Vector3 currentPosition = state.unit.position;
        
        // Screen shake if enabled
        if (enableScreenShake && mainCamera != null)
        {
            StartCoroutine(ApplyScreenShake());
        }
        
        while (elapsed < bounceDuration)
        {
            if (state.unit == null) yield break;
            
            float progress = elapsed / bounceDuration;
            float bounceValue = bounceCurve.Evaluate(progress) * bounceIntensity;
            
            // Scale bounce
            float scaleMultiplier = 1f + bounceValue * 0.5f;
            state.unit.localScale = originalScale * scaleMultiplier;
            
            // Position bounce (slight vertical)
            Vector3 bounceOffset = Vector3.up * (bounceValue * 0.1f);
            state.unit.position = currentPosition + bounceOffset;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Restore original transform
        state.unit.localScale = originalScale;
        state.unit.position = currentPosition;
        
        if (enableDebugLogging)
        {
            Debug.Log($"MovementAnimationEnhancer: Completed follow-through phase for {state.unit.name}");
        }
    }
    
    /// <summary>
    /// Runs the settle phase
    /// </summary>
    private IEnumerator RunSettlePhase(EnhancementState state)
    {
        if (state?.unit == null) yield break;
        
        state.currentPhase = EnhancementPhase.Settle;
        state.phaseStartTime = Time.time;
        OnPhaseChanged?.Invoke(state.unit, EnhancementPhase.Settle);
        
        // Simple settle - just wait for a brief moment
        yield return new WaitForSeconds(settleDuration);
        
        if (enableDebugLogging)
        {
            Debug.Log($"MovementAnimationEnhancer: Completed settle phase for {state.unit.name}");
        }
    }
    
    /// <summary>
    /// Applies screen shake effect
    /// </summary>
    private IEnumerator ApplyScreenShake()
    {
        if (mainCamera == null) yield break;
        
        Vector3 originalPosition = mainCamera.transform.position;
        float elapsed = 0f;
        float duration = 0.1f;
        
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float intensity = screenShakeIntensity * (1f - progress); // Fade out
            
            Vector3 randomOffset = Random.insideUnitSphere * intensity;
            randomOffset.z = 0; // Keep camera depth consistent
            
            mainCamera.transform.position = originalPosition + randomOffset;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        mainCamera.transform.position = originalPosition;
    }
    
    /// <summary>
    /// Completes enhancement for a unit
    /// </summary>
    private void CompleteEnhancement(Transform unitTransform)
    {
        if (!activeEnhancements.ContainsKey(unitTransform)) return;
        
        EnhancementState state = activeEnhancements[unitTransform];
        state.currentPhase = EnhancementPhase.Complete;
        OnPhaseChanged?.Invoke(unitTransform, EnhancementPhase.Complete);
        
        CleanupEnhancement(unitTransform);
        OnEnhancementCompleted?.Invoke(unitTransform);
        
        if (enableDebugLogging)
        {
            Debug.Log($"MovementAnimationEnhancer: Completed enhancement for {unitTransform.name}");
        }
    }
    
    /// <summary>
    /// Cleans up enhancement for a unit
    /// </summary>
    private void CleanupEnhancement(Transform unitTransform)
    {
        if (!activeEnhancements.ContainsKey(unitTransform)) return;
        
        EnhancementState state = activeEnhancements[unitTransform];
        
        // Stop coroutine
        if (state.enhancementCoroutine != null)
        {
            StopCoroutine(state.enhancementCoroutine);
            state.enhancementCoroutine = null;
        }
        
        // Return effects to pool
        if (state.particles != null)
        {
            state.particles.gameObject.SetActive(false);
            if (useObjectPooling)
            {
                particlePool.Enqueue(state.particles);
            }
        }
        
        if (state.trail != null)
        {
            state.trail.gameObject.SetActive(false);
            if (useObjectPooling)
            {
                trailPool.Enqueue(state.trail);
            }
        }
        
        // Ensure unit is restored to original state
        if (unitTransform != null)
        {
            unitTransform.localScale = state.originalScale;
            unitTransform.rotation = state.originalRotation;
        }
        
        activeEnhancements.Remove(unitTransform);
    }
    
    /// <summary>
    /// Gets a pooled particle system
    /// </summary>
    private ParticleSystem GetPooledParticleSystem()
    {
        if (particlePool.Count > 0)
        {
            return particlePool.Dequeue();
        }
        
        return CreatePooledParticleSystem();
    }
    
    /// <summary>
    /// Gets a pooled trail renderer
    /// </summary>
    private TrailRenderer GetPooledTrailRenderer()
    {
        if (trailPool.Count > 0)
        {
            return trailPool.Dequeue();
        }
        
        return CreatePooledTrailRenderer();
    }
    
    /// <summary>
    /// Gets enhancement information for debugging
    /// </summary>
    public string GetEnhancementInfo()
    {
        if (activeEnhancements.Count == 0)
            return "No active enhancements";
        
        return $"Active Enhancements: {activeEnhancements.Count}/{maxConcurrentEnhancements}";
    }
    
    /// <summary>
    /// Unity Gizmos for debug visualization
    /// </summary>
    void OnDrawGizmos()
    {
        if (!enableDebugLogging || !showEnhancementGizmos) return;
        
        // Draw enhancement indicators
        foreach (var kvp in activeEnhancements)
        {
            Transform unit = kvp.Key;
            EnhancementState state = kvp.Value;
            
            if (unit == null) continue;
            
            // Set color based on phase
            switch (state.currentPhase)
            {
                case EnhancementPhase.Anticipation:
                    Gizmos.color = Color.yellow;
                    break;
                case EnhancementPhase.Movement:
                    Gizmos.color = Color.blue;
                    break;
                case EnhancementPhase.FollowThrough:
                    Gizmos.color = Color.green;
                    break;
                case EnhancementPhase.Settle:
                    Gizmos.color = Color.white;
                    break;
                default:
                    Gizmos.color = Color.gray;
                    break;
            }
            
            Gizmos.DrawWireSphere(unit.position + Vector3.up * 2f, 0.2f);
        }
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Stop all active enhancements
        List<Transform> unitsToCleanup = new List<Transform>(activeEnhancements.Keys);
        foreach (Transform unit in unitsToCleanup)
        {
            CleanupEnhancement(unit);
        }
        
        // Clean up object pools
        while (particlePool.Count > 0)
        {
            ParticleSystem particles = particlePool.Dequeue();
            if (particles != null && particles.gameObject != null)
            {
                DestroyImmediate(particles.gameObject);
            }
        }
        
        while (trailPool.Count > 0)
        {
            TrailRenderer trail = trailPool.Dequeue();
            if (trail != null && trail.gameObject != null)
            {
                DestroyImmediate(trail.gameObject);
            }
        }
        
        // Unregister from events
        if (movementManager != null)
        {
            movementManager.OnMovementStarted -= OnMovementStarted;
            movementManager.OnMovementCompleted -= OnMovementCompleted;
        }
        
        if (movementAnimator != null)
        {
            movementAnimator.OnAnimationComplete -= OnAnimationCompleted;
        }
        
        // Clear event references
        OnEnhancementStarted = null;
        OnEnhancementCompleted = null;
        OnPhaseChanged = null;
    }
}