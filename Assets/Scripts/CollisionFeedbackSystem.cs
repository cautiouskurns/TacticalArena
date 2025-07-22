using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Collision feedback system that provides visual feedback for blocked moves and obstacle collisions.
/// Shows clear visual indication when movement attempts are blocked by obstacles, boundaries, or other units.
/// Provides bounce effects, particles, and visual cues to enhance player understanding of movement restrictions.
/// </summary>
public class CollisionFeedbackSystem : MonoBehaviour
{
    [Header("Collision Feedback Configuration")]
    [SerializeField] private bool enableCollisionFeedback = true;
    [SerializeField] private bool enableBounceEffect = true;
    [SerializeField] private bool enableObstacleFeedback = true;
    [SerializeField] private bool enableBoundaryFeedback = true;
    [SerializeField] private bool enableOccupancyFeedback = true;
    
    [Header("Bounce Effect Settings")]
    [SerializeField] private float bounceIntensity = 0.2f;
    [SerializeField] private float bounceDuration = 0.3f;
    [SerializeField] private AnimationCurve bounceCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private bool enableRotationBounce = true;
    [SerializeField] private float rotationBounceAmount = 15f;
    
    [Header("Visual Feedback Materials")]
    [SerializeField] private Material invalidMoveMaterial;
    [SerializeField] private Material collisionIndicatorMaterial;
    [SerializeField] private Color collisionColor = Color.red;
    [SerializeField] private float indicatorDuration = 1.0f;
    
    [Header("Particle Effects")]
    [SerializeField] private bool enableParticles = true;
    [SerializeField] private Color particleColor = Color.red;
    [SerializeField] private float visualEffectIntensity = 1.0f;
    [SerializeField] private int particleCount = 20;
    [SerializeField] private float particleLifetime = 1.0f;
    
    [Header("Audio Feedback")]
    [SerializeField] private bool enableAudioFeedback = false;
    [SerializeField] private AudioClip bounceSound;
    [SerializeField] private AudioClip obstacleHitSound;
    [SerializeField] private AudioClip boundaryHitSound;
    [SerializeField] private float audioVolume = 0.5f;
    
    [Header("Performance Settings")]
    [SerializeField] private int maxConcurrentFeedback = 8;
    [SerializeField] private bool useObjectPooling = true;
    [SerializeField] private float feedbackCullingDistance = 15f;
    [SerializeField] private bool optimizeForPerformance = true;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogging = false;
    [SerializeField] private bool showCollisionGizmos = false;
    
    // System references
    private MovementManager movementManager;
    private MovementValidator movementValidator;
    private GridManager gridManager;
    private Camera mainCamera;
    private AudioSource audioSource;
    
    // Feedback state
    private Dictionary<Transform, CollisionFeedbackState> activeFeedback = new Dictionary<Transform, CollisionFeedbackState>();
    private Queue<ParticleSystem> particlePool = new Queue<ParticleSystem>();
    private Queue<GameObject> indicatorPool = new Queue<GameObject>();
    private List<GameObject> activeIndicators = new List<GameObject>();
    
    // Events
    public System.Action<Transform, CollisionType, Vector2Int> OnCollisionFeedback;
    public System.Action<Transform> OnFeedbackComplete;
    
    /// <summary>
    /// Types of collision feedback
    /// </summary>
    public enum CollisionType
    {
        Obstacle,
        Boundary,
        UnitOccupied,
        InvalidMove,
        OutOfRange
    }
    
    /// <summary>
    /// State tracking for collision feedback
    /// </summary>
    private class CollisionFeedbackState
    {
        public Transform unit;
        public CollisionType collisionType;
        public Vector2Int targetPosition;
        public Vector3 originalPosition;
        public Quaternion originalRotation;
        public Vector3 originalScale;
        public float startTime;
        public Coroutine feedbackCoroutine;
        public ParticleSystem particles;
        public GameObject indicator;
        public bool isActive;
    }
    
    void Awake()
    {
        InitializeCollisionSystem();
    }
    
    void Start()
    {
        FindSystemReferences();
        SetupEventListeners();
        InitializeObjectPools();
        SetupAudioSource();
    }
    
    void Update()
    {
        if (optimizeForPerformance)
        {
            CullDistantFeedback();
        }
    }
    
    /// <summary>
    /// Initializes the collision feedback system
    /// </summary>
    private void InitializeCollisionSystem()
    {
        if (enableDebugLogging)
        {
            Debug.Log("CollisionFeedbackSystem initialized");
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
            Debug.LogError("CollisionFeedbackSystem: MovementManager not found on same GameObject!");
        }
        
        movementValidator = GetComponent<MovementValidator>();
        if (movementValidator == null)
        {
            Debug.LogError("CollisionFeedbackSystem: MovementValidator not found on same GameObject!");
        }
        
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("CollisionFeedbackSystem: GridManager not found!");
        }
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"CollisionFeedbackSystem found references - Movement: {movementManager != null}, Validator: {movementValidator != null}, Grid: {gridManager != null}");
        }
    }
    
    /// <summary>
    /// Sets up event listeners
    /// </summary>
    private void SetupEventListeners()
    {
        if (movementManager != null)
        {
            movementManager.OnMovementFailed += OnMovementFailed;
        }
    }
    
    /// <summary>
    /// Sets up audio source
    /// </summary>
    private void SetupAudioSource()
    {
        if (!enableAudioFeedback) return;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.volume = audioVolume;
        audioSource.playOnAwake = false;
    }
    
    /// <summary>
    /// Initializes object pools
    /// </summary>
    private void InitializeObjectPools()
    {
        if (!useObjectPooling) return;
        
        // Initialize particle pool
        if (enableParticles)
        {
            for (int i = 0; i < maxConcurrentFeedback; i++)
            {
                ParticleSystem particles = CreatePooledParticleSystem();
                particles.gameObject.SetActive(false);
                particlePool.Enqueue(particles);
            }
        }
        
        // Initialize indicator pool
        for (int i = 0; i < maxConcurrentFeedback; i++)
        {
            GameObject indicator = CreatePooledIndicator();
            indicator.SetActive(false);
            indicatorPool.Enqueue(indicator);
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"CollisionFeedbackSystem: Initialized pools - Particles: {particlePool.Count}, Indicators: {indicatorPool.Count}");
        }
    }
    
    /// <summary>
    /// Creates a pooled particle system for collision effects
    /// </summary>
    private ParticleSystem CreatePooledParticleSystem()
    {
        GameObject particleObj = new GameObject("CollisionParticles");
        particleObj.transform.SetParent(transform);
        
        ParticleSystem particles = particleObj.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startColor = particleColor;
        main.startSize = 0.15f * visualEffectIntensity;
        main.startSpeed = 3f * visualEffectIntensity;
        main.maxParticles = particleCount;
        main.startLifetime = particleLifetime;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = particles.emission;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, (short)(particleCount * visualEffectIntensity))
        });
        emission.rateOverTime = 0; // Burst only
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;
        
        var velocityOverLifetime = particles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(2f * visualEffectIntensity);
        
        return particles;
    }
    
    /// <summary>
    /// Creates a pooled collision indicator
    /// </summary>
    private GameObject CreatePooledIndicator()
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        indicator.name = "CollisionIndicator";
        indicator.transform.SetParent(transform);
        
        // Remove collider to avoid interference
        Collider collider = indicator.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        // Configure for display
        indicator.transform.localScale = Vector3.one * 0.5f;
        indicator.layer = LayerMask.NameToLayer("UI"); // Avoid raycast interference
        
        // Apply material
        Renderer renderer = indicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (collisionIndicatorMaterial != null)
            {
                renderer.material = collisionIndicatorMaterial;
            }
            else
            {
                // Create basic material if none provided
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = collisionColor;
                mat.SetFloat("_Surface", 1); // Transparent
                renderer.material = mat;
            }
        }
        
        return indicator;
    }
    
    /// <summary>
    /// Culls distant feedback for performance
    /// </summary>
    private void CullDistantFeedback()
    {
        if (mainCamera == null) return;
        
        Vector3 cameraPosition = mainCamera.transform.position;
        List<Transform> toRemove = new List<Transform>();
        
        foreach (var kvp in activeFeedback)
        {
            Transform unit = kvp.Key;
            if (unit == null)
            {
                toRemove.Add(unit);
                continue;
            }
            
            float distance = Vector3.Distance(cameraPosition, unit.position);
            if (distance > feedbackCullingDistance)
            {
                // Cancel distant feedback
                CancelCollisionFeedback(unit);
                toRemove.Add(unit);
            }
        }
        
        // Clean up
        foreach (Transform unit in toRemove)
        {
            activeFeedback.Remove(unit);
        }
    }
    
    /// <summary>
    /// Called when movement fails
    /// </summary>
    private void OnMovementFailed(IMovable unit, string reason)
    {
        if (!enableCollisionFeedback || unit?.Transform == null) return;
        
        // Determine collision type from reason
        CollisionType collisionType = DetermineCollisionType(reason);
        
        // Get target position (approximate from current position for feedback)
        Vector2Int targetPosition = unit.GridPosition; // Default to current position
        
        ShowCollisionFeedback(unit.Transform, collisionType, targetPosition);
    }
    
    /// <summary>
    /// Determines collision type from failure reason
    /// </summary>
    private CollisionType DetermineCollisionType(string reason)
    {
        if (reason.Contains("obstacle") || reason.Contains("blocked"))
            return CollisionType.Obstacle;
        if (reason.Contains("boundary") || reason.Contains("bounds"))
            return CollisionType.Boundary;
        if (reason.Contains("occupied") || reason.Contains("unit"))
            return CollisionType.UnitOccupied;
        if (reason.Contains("range"))
            return CollisionType.OutOfRange;
        
        return CollisionType.InvalidMove;
    }
    
    /// <summary>
    /// Shows collision feedback for a unit
    /// </summary>
    public void ShowCollisionFeedback(Transform unit, CollisionType collisionType, Vector2Int targetPosition)
    {
        if (!enableCollisionFeedback || unit == null) return;
        
        // Check if we're at the limit
        if (activeFeedback.Count >= maxConcurrentFeedback)
        {
            if (enableDebugLogging)
            {
                Debug.LogWarning($"CollisionFeedbackSystem: Reached max concurrent feedback ({maxConcurrentFeedback})");
            }
            return;
        }
        
        // Cancel existing feedback for this unit
        if (activeFeedback.ContainsKey(unit))
        {
            CancelCollisionFeedback(unit);
        }
        
        StartCollisionFeedback(unit, collisionType, targetPosition);
    }
    
    /// <summary>
    /// Starts collision feedback for a unit
    /// </summary>
    private void StartCollisionFeedback(Transform unit, CollisionType collisionType, Vector2Int targetPosition)
    {
        // Create feedback state
        CollisionFeedbackState state = new CollisionFeedbackState
        {
            unit = unit,
            collisionType = collisionType,
            targetPosition = targetPosition,
            originalPosition = unit.position,
            originalRotation = unit.rotation,
            originalScale = unit.localScale,
            startTime = Time.time,
            isActive = true
        };
        
        // Setup particle effect
        if (enableParticles)
        {
            state.particles = GetPooledParticleSystem();
            if (state.particles != null)
            {
                state.particles.transform.position = unit.position + Vector3.up * 0.5f;
                state.particles.gameObject.SetActive(true);
                state.particles.Play();
            }
        }
        
        // Setup collision indicator
        if (gridManager != null)
        {
            state.indicator = GetPooledIndicator();
            if (state.indicator != null)
            {
                GridCoordinate gridCoord = new GridCoordinate(targetPosition.x, targetPosition.y);
                Vector3 indicatorPosition = gridManager.GridToWorld(gridCoord);
                indicatorPosition.y += 0.1f; // Slightly above ground
                
                state.indicator.transform.position = indicatorPosition;
                state.indicator.SetActive(true);
                activeIndicators.Add(state.indicator);
                
                // Start indicator animation
                StartCoroutine(AnimateIndicator(state.indicator));
            }
        }
        
        activeFeedback[unit] = state;
        
        // Start feedback sequence
        state.feedbackCoroutine = StartCoroutine(RunCollisionFeedbackSequence(state));
        
        // Play audio feedback
        PlayAudioFeedback(collisionType);
        
        OnCollisionFeedback?.Invoke(unit, collisionType, targetPosition);
        
        if (enableDebugLogging)
        {
            Debug.Log($"CollisionFeedbackSystem: Started feedback for {unit.name}, type: {collisionType}");
        }
    }
    
    /// <summary>
    /// Runs the collision feedback sequence
    /// </summary>
    private IEnumerator RunCollisionFeedbackSequence(CollisionFeedbackState state)
    {
        if (state?.unit == null) yield break;
        
        // Bounce effect
        if (enableBounceEffect)
        {
            yield return StartCoroutine(RunBounceEffect(state));
        }
        
        // Wait for indicator to finish
        yield return new WaitForSeconds(indicatorDuration);
        
        CompleteCollisionFeedback(state.unit);
    }
    
    /// <summary>
    /// Runs the bounce effect
    /// </summary>
    private IEnumerator RunBounceEffect(CollisionFeedbackState state)
    {
        if (state?.unit == null) yield break;
        
        float elapsed = 0f;
        Vector3 startPosition = state.originalPosition;
        Quaternion startRotation = state.originalRotation;
        Vector3 startScale = state.originalScale;
        
        while (elapsed < bounceDuration)
        {
            if (state.unit == null) yield break;
            
            float progress = elapsed / bounceDuration;
            float bounceValue = bounceCurve.Evaluate(progress) * bounceIntensity;
            
            // Position bounce (slight backward movement)
            Vector3 bounceDirection = -state.unit.forward * bounceValue * 0.3f;
            bounceDirection += Vector3.up * bounceValue * 0.1f; // Small vertical component
            state.unit.position = startPosition + bounceDirection;
            
            // Rotation bounce
            if (enableRotationBounce)
            {
                float rotationAmount = bounceValue * rotationBounceAmount;
                Vector3 randomAxis = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-0.5f, 0.5f),
                    Random.Range(-1f, 1f)
                ).normalized;
                
                state.unit.rotation = startRotation * Quaternion.AngleAxis(rotationAmount, randomAxis);
            }
            
            // Scale bounce
            float scaleMultiplier = 1f + bounceValue * 0.2f;
            state.unit.localScale = startScale * scaleMultiplier;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Restore original transform
        state.unit.position = startPosition;
        state.unit.rotation = startRotation;
        state.unit.localScale = startScale;
    }
    
    /// <summary>
    /// Animates collision indicator
    /// </summary>
    private IEnumerator AnimateIndicator(GameObject indicator)
    {
        if (indicator == null) yield break;
        
        Renderer renderer = indicator.GetComponent<Renderer>();
        if (renderer == null) yield break;
        
        Material material = renderer.material;
        Color originalColor = material.color;
        Vector3 originalScale = indicator.transform.localScale;
        
        float elapsed = 0f;
        while (elapsed < indicatorDuration && indicator.activeInHierarchy)
        {
            float progress = elapsed / indicatorDuration;
            
            // Fade out
            Color currentColor = originalColor;
            currentColor.a = Mathf.Lerp(1f, 0f, progress);
            material.color = currentColor;
            
            // Scale up slightly
            float scaleMultiplier = 1f + progress * 0.5f;
            indicator.transform.localScale = originalScale * scaleMultiplier;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset and deactivate
        if (indicator != null)
        {
            material.color = originalColor;
            indicator.transform.localScale = originalScale;
            indicator.SetActive(false);
        }
    }
    
    /// <summary>
    /// Plays audio feedback for collision type
    /// </summary>
    private void PlayAudioFeedback(CollisionType collisionType)
    {
        if (!enableAudioFeedback || audioSource == null) return;
        
        AudioClip clipToPlay = null;
        switch (collisionType)
        {
            case CollisionType.Obstacle:
                clipToPlay = obstacleHitSound;
                break;
            case CollisionType.Boundary:
                clipToPlay = boundaryHitSound;
                break;
            case CollisionType.UnitOccupied:
            case CollisionType.InvalidMove:
            case CollisionType.OutOfRange:
                clipToPlay = bounceSound;
                break;
        }
        
        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay, audioVolume);
        }
    }
    
    /// <summary>
    /// Completes collision feedback for a unit
    /// </summary>
    private void CompleteCollisionFeedback(Transform unit)
    {
        if (!activeFeedback.ContainsKey(unit)) return;
        
        CleanupFeedback(unit);
        OnFeedbackComplete?.Invoke(unit);
        
        if (enableDebugLogging)
        {
            Debug.Log($"CollisionFeedbackSystem: Completed feedback for {unit.name}");
        }
    }
    
    /// <summary>
    /// Cancels collision feedback for a unit
    /// </summary>
    public void CancelCollisionFeedback(Transform unit)
    {
        if (!activeFeedback.ContainsKey(unit)) return;
        
        CollisionFeedbackState state = activeFeedback[unit];
        if (state.feedbackCoroutine != null)
        {
            StopCoroutine(state.feedbackCoroutine);
        }
        
        CleanupFeedback(unit);
    }
    
    /// <summary>
    /// Cleans up feedback resources for a unit
    /// </summary>
    private void CleanupFeedback(Transform unit)
    {
        if (!activeFeedback.ContainsKey(unit)) return;
        
        CollisionFeedbackState state = activeFeedback[unit];
        
        // Return particles to pool
        if (state.particles != null)
        {
            state.particles.gameObject.SetActive(false);
            if (useObjectPooling)
            {
                particlePool.Enqueue(state.particles);
            }
        }
        
        // Return indicator to pool
        if (state.indicator != null)
        {
            state.indicator.SetActive(false);
            activeIndicators.Remove(state.indicator);
            if (useObjectPooling)
            {
                indicatorPool.Enqueue(state.indicator);
            }
        }
        
        // Ensure unit is restored to original state
        if (unit != null)
        {
            unit.position = state.originalPosition;
            unit.rotation = state.originalRotation;
            unit.localScale = state.originalScale;
        }
        
        activeFeedback.Remove(unit);
    }
    
    /// <summary>
    /// Gets pooled particle system
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
    /// Gets pooled indicator
    /// </summary>
    private GameObject GetPooledIndicator()
    {
        if (indicatorPool.Count > 0)
        {
            return indicatorPool.Dequeue();
        }
        
        return CreatePooledIndicator();
    }
    
    /// <summary>
    /// Gets collision feedback info for debugging
    /// </summary>
    public string GetCollisionFeedbackInfo()
    {
        if (activeFeedback.Count == 0)
            return "No active collision feedback";
        
        return $"Active Feedback: {activeFeedback.Count}/{maxConcurrentFeedback}";
    }
    
    /// <summary>
    /// Unity Gizmos for debug visualization
    /// </summary>
    void OnDrawGizmos()
    {
        if (!enableDebugLogging || !showCollisionGizmos) return;
        
        // Draw collision feedback indicators
        foreach (var kvp in activeFeedback)
        {
            Transform unit = kvp.Key;
            CollisionFeedbackState state = kvp.Value;
            
            if (unit == null) continue;
            
            // Set color based on collision type
            switch (state.collisionType)
            {
                case CollisionType.Obstacle:
                    Gizmos.color = Color.red;
                    break;
                case CollisionType.Boundary:
                    Gizmos.color = Color.blue;
                    break;
                case CollisionType.UnitOccupied:
                    Gizmos.color = Color.yellow;
                    break;
                case CollisionType.OutOfRange:
                    Gizmos.color = Color.magenta;
                    break;
                default:
                    Gizmos.color = Color.gray;
                    break;
            }
            
            Gizmos.DrawWireCube(unit.position + Vector3.up * 1.5f, Vector3.one * 0.3f);
        }
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Cancel all active feedback
        List<Transform> unitsToCleanup = new List<Transform>(activeFeedback.Keys);
        foreach (Transform unit in unitsToCleanup)
        {
            CleanupFeedback(unit);
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
        
        while (indicatorPool.Count > 0)
        {
            GameObject indicator = indicatorPool.Dequeue();
            if (indicator != null)
            {
                DestroyImmediate(indicator);
            }
        }
        
        // Clean up active indicators
        foreach (GameObject indicator in activeIndicators)
        {
            if (indicator != null)
            {
                DestroyImmediate(indicator);
            }
        }
        
        // Unregister from events
        if (movementManager != null)
        {
            movementManager.OnMovementFailed -= OnMovementFailed;
        }
        
        // Clear event references
        OnCollisionFeedback = null;
        OnFeedbackComplete = null;
    }
}