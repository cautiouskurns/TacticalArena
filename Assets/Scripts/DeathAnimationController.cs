using UnityEngine;
using System.Collections;

/// <summary>
/// Death animation sequences with visual effects and proper timing.
/// Manages death animation sequences, fade effects, particles, and cleanup
/// for smooth visual transitions when units are eliminated.
/// Part of Task 2.1.4 - Combat Visual Feedback - COMPLETES SUB-MILESTONE 2.1
/// </summary>
public class DeathAnimationController : MonoBehaviour
{
    [Header("Animation Configuration")]
    [SerializeField] private float animationDuration = 2.0f;
    [SerializeField] private bool enableParticles = true;
    [SerializeField] private bool enableFade = true;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private bool enableScaling = true;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.1f);
    [SerializeField] private bool enableRotation = false;
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 180, 0);
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject deathParticlePrefab;
    [SerializeField] private Color particleColor = Color.red;
    [SerializeField] private int particleCount = 50;
    [SerializeField] private float particleDuration = 3.0f;
    [SerializeField] private bool enableExplosionEffect = true;
    [SerializeField] private float explosionForce = 300f;
    [SerializeField] private float explosionRadius = 2f;
    
    [Header("Audio Configuration")]
    [SerializeField] private bool enableSound = true;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private float audioVolume = 0.8f;
    [SerializeField] private bool varyPitch = true;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.7f, 1.3f);
    
    [Header("Timing Configuration")]
    [SerializeField] private float delayBeforeRemoval = 1.0f;
    [SerializeField] private float fadeStartDelay = 0.5f;
    [SerializeField] private bool waitForParticlesToFinish = true;
    [SerializeField] private bool enableSlowMotion = false;
    [SerializeField] private float slowMotionScale = 0.3f;
    [SerializeField] private float slowMotionDuration = 1.0f;
    
    [Header("Performance Settings")]
    [SerializeField] private bool enableLODOptimization = true;
    [SerializeField] private float cullingDistance = 30.0f;
    [SerializeField] private bool enableEffectPooling = true;
    [SerializeField] private int maxSimultaneousDeaths = 5;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugging = false;
    [SerializeField] private bool logAnimationSteps = false;
    [SerializeField] private bool visualizeAnimationBounds = false;
    
    // Component references
    private CombatVisualManager visualManager;
    private Camera mainCamera;
    private AudioSource audioSource;
    
    // Animation tracking
    private int activeAnimations = 0;
    private System.Collections.Generic.List<DeathAnimation> runningAnimations;
    
    // Events
    public System.Action<Unit> OnDeathAnimationStarted;
    public System.Action<Unit> OnDeathAnimationCompleted;
    public System.Action<Unit> OnUnitReadyForRemoval;
    
    // Properties
    public bool IsInitialized { get; private set; } = false;
    public int ActiveAnimationsCount => activeAnimations;
    public bool CanStartNewAnimation => activeAnimations < maxSimultaneousDeaths;
    
    // Death animation data structure
    private class DeathAnimation
    {
        public Unit unit;
        public GameObject unitObject;
        public Renderer[] renderers;
        public Material[] originalMaterials;
        public Color[] originalColors;
        public Vector3 originalScale;
        public Vector3 originalRotation;
        public float startTime;
        public bool isCompleted;
        public Coroutine animationCoroutine;
        
        public DeathAnimation(Unit u)
        {
            unit = u;
            unitObject = u.gameObject;
            renderers = unitObject.GetComponentsInChildren<Renderer>();
            originalScale = unitObject.transform.localScale;
            originalRotation = unitObject.transform.eulerAngles;
            startTime = Time.time;
            isCompleted = false;
            
            // Store original materials and colors
            originalMaterials = new Material[renderers.Length];
            originalColors = new Color[renderers.Length];
            
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].material != null)
                {
                    originalMaterials[i] = renderers[i].material;
                    originalColors[i] = renderers[i].material.color;
                }
            }
        }
    }
    
    /// <summary>
    /// Initializes the death animation controller
    /// </summary>
    public void Initialize(CombatVisualManager manager)
    {
        visualManager = manager;
        
        FindComponents();
        InitializeAudio();
        InitializeAnimationTracking();
        
        IsInitialized = true;
        
        if (enableDebugging)
        {
            Debug.Log("DeathAnimationController initialized - Death animation system ready");
        }
    }
    
    /// <summary>
    /// Finds necessary components
    /// </summary>
    private void FindComponents()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (enableDebugging)
        {
            Debug.Log($"DeathAnimationController found camera: {mainCamera != null}");
        }
    }
    
    /// <summary>
    /// Initializes audio system
    /// </summary>
    private void InitializeAudio()
    {
        if (!enableSound) return;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.playOnAwake = false;
        audioSource.volume = audioVolume;
        
        if (enableDebugging)
        {
            Debug.Log("DeathAnimationController audio system initialized");
        }
    }
    
    /// <summary>
    /// Initializes animation tracking
    /// </summary>
    private void InitializeAnimationTracking()
    {
        runningAnimations = new System.Collections.Generic.List<DeathAnimation>();
        
        if (enableDebugging)
        {
            Debug.Log("DeathAnimationController animation tracking initialized");
        }
    }
    
    void Update()
    {
        // Clean up completed animations
        CleanupCompletedAnimations();
        
        // Update LOD if enabled
        if (enableLODOptimization)
        {
            UpdateAnimationLOD();
        }
    }
    
    #region Public Interface
    
    /// <summary>
    /// Plays death animation for a unit
    /// </summary>
    public void PlayDeathAnimation(Unit unit)
    {
        if (!IsInitialized || unit == null || !CanStartNewAnimation)
        {
            if (enableDebugging)
            {
                Debug.LogWarning($"DeathAnimationController: Cannot start death animation for {unit?.name ?? "null unit"}");
            }
            return;
        }
        
        // Check distance culling
        if (enableLODOptimization && mainCamera != null)
        {
            float distance = Vector3.Distance(unit.transform.position, mainCamera.transform.position);
            if (distance > cullingDistance)
            {
                // Skip animation for distant units, just mark for removal
                OnUnitReadyForRemoval?.Invoke(unit);
                return;
            }
        }
        
        DeathAnimation animation = new DeathAnimation(unit);
        runningAnimations.Add(animation);
        activeAnimations++;
        
        // Start animation coroutine
        animation.animationCoroutine = StartCoroutine(DeathAnimationSequence(animation));
        
        OnDeathAnimationStarted?.Invoke(unit);
        
        if (logAnimationSteps)
        {
            Debug.Log($"DeathAnimationController: Started death animation for {unit.name}");
        }
    }
    
    /// <summary>
    /// Forces completion of all running animations
    /// </summary>
    public void CompleteAllAnimations()
    {
        foreach (DeathAnimation animation in runningAnimations)
        {
            if (animation.animationCoroutine != null && !animation.isCompleted)
            {
                StopCoroutine(animation.animationCoroutine);
                CompleteAnimation(animation);
            }
        }
        
        if (enableDebugging)
        {
            Debug.Log("DeathAnimationController: All animations completed");
        }
    }
    
    /// <summary>
    /// Gets animation progress for a unit
    /// </summary>
    public float GetAnimationProgress(Unit unit)
    {
        foreach (DeathAnimation animation in runningAnimations)
        {
            if (animation.unit == unit)
            {
                float elapsed = Time.time - animation.startTime;
                return Mathf.Clamp01(elapsed / animationDuration);
            }
        }
        
        return 0f;
    }
    
    #endregion
    
    #region Animation Implementation
    
    /// <summary>
    /// Main death animation sequence
    /// </summary>
    private IEnumerator DeathAnimationSequence(DeathAnimation animation)
    {
        if (animation.unit == null || animation.unitObject == null)
        {
            CompleteAnimation(animation);
            yield break;
        }
        
        // Play death sound
        PlayDeathAudio();
        
        // Start slow motion if enabled
        if (enableSlowMotion)
        {
            StartCoroutine(SlowMotionEffect());
        }
        
        // Create particle effects
        if (enableParticles)
        {
            CreateDeathParticles(animation.unit.transform.position);
        }
        
        // Create explosion effect
        if (enableExplosionEffect)
        {
            CreateExplosionEffect(animation.unit.transform.position);
        }
        
        // Wait for fade start delay
        if (fadeStartDelay > 0)
        {
            yield return new WaitForSeconds(fadeStartDelay);
        }
        
        // Run main animation
        yield return StartCoroutine(MainDeathAnimation(animation));
        
        // Wait for additional delay before removal
        if (delayBeforeRemoval > 0)
        {
            yield return new WaitForSeconds(delayBeforeRemoval);
        }
        
        // Wait for particles to finish if enabled
        if (waitForParticlesToFinish)
        {
            yield return new WaitForSeconds(particleDuration);
        }
        
        CompleteAnimation(animation);
    }
    
    /// <summary>
    /// Main death animation with fade, scale, and rotation
    /// </summary>
    private IEnumerator MainDeathAnimation(DeathAnimation animation)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration && animation.unitObject != null)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            
            // Apply fade animation
            if (enableFade)
            {
                ApplyFadeAnimation(animation, progress);
            }
            
            // Apply scale animation
            if (enableScaling)
            {
                ApplyScaleAnimation(animation, progress);
            }
            
            // Apply rotation animation
            if (enableRotation)
            {
                ApplyRotationAnimation(animation, elapsedTime);
            }
            
            yield return null;
        }
        
        if (logAnimationSteps)
        {
            Debug.Log($"DeathAnimationController: Main animation completed for {animation.unit?.name}");
        }
    }
    
    /// <summary>
    /// Applies fade animation
    /// </summary>
    private void ApplyFadeAnimation(DeathAnimation animation, float progress)
    {
        float fadeValue = fadeCurve.Evaluate(progress);
        
        for (int i = 0; i < animation.renderers.Length; i++)
        {
            if (animation.renderers[i] == null || animation.renderers[i].material == null) continue;
            
            Color targetColor = animation.originalColors[i];
            targetColor.a = fadeValue;
            animation.renderers[i].material.color = targetColor;
        }
    }
    
    /// <summary>
    /// Applies scale animation
    /// </summary>
    private void ApplyScaleAnimation(DeathAnimation animation, float progress)
    {
        if (animation.unitObject == null) return;
        
        float scaleValue = scaleCurve.Evaluate(progress);
        Vector3 newScale = animation.originalScale * scaleValue;
        animation.unitObject.transform.localScale = newScale;
    }
    
    /// <summary>
    /// Applies rotation animation
    /// </summary>
    private void ApplyRotationAnimation(DeathAnimation animation, float elapsedTime)
    {
        if (animation.unitObject == null) return;
        
        Vector3 currentRotation = animation.originalRotation + (rotationSpeed * elapsedTime);
        animation.unitObject.transform.eulerAngles = currentRotation;
    }
    
    /// <summary>
    /// Creates death particle effects
    /// </summary>
    private void CreateDeathParticles(Vector3 position)
    {
        GameObject particles;
        
        if (deathParticlePrefab != null)
        {
            particles = Instantiate(deathParticlePrefab, position, Quaternion.identity);
        }
        else
        {
            // Create default particle system
            particles = new GameObject("DeathParticles");
            particles.transform.position = position;
            ParticleSystem ps = particles.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startColor = particleColor;
            main.startLifetime = particleDuration;
            main.startSpeed = 5f;
            main.maxParticles = particleCount;
            
            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0.0f, (short)particleCount)
            });
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;
        }
        
        // Destroy particles after duration
        Destroy(particles, particleDuration);
        
        if (logAnimationSteps)
        {
            Debug.Log($"DeathAnimationController: Death particles created at {position}");
        }
    }
    
    /// <summary>
    /// Creates explosion effect
    /// </summary>
    private void CreateExplosionEffect(Vector3 position)
    {
        // Find nearby rigidbodies and apply explosion force
        Collider[] nearbyColliders = Physics.OverlapSphere(position, explosionRadius);
        
        foreach (Collider collider in nearbyColliders)
        {
            Rigidbody rb = collider.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.AddExplosionForce(explosionForce, position, explosionRadius);
            }
        }
        
        if (logAnimationSteps)
        {
            Debug.Log($"DeathAnimationController: Explosion effect applied at {position}");
        }
    }
    
    /// <summary>
    /// Slow motion effect coroutine
    /// </summary>
    private IEnumerator SlowMotionEffect()
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = slowMotionScale;
        
        yield return new WaitForSecondsRealtime(slowMotionDuration);
        
        Time.timeScale = originalTimeScale;
        
        if (logAnimationSteps)
        {
            Debug.Log("DeathAnimationController: Slow motion effect completed");
        }
    }
    
    /// <summary>
    /// Completes animation and cleanup
    /// </summary>
    private void CompleteAnimation(DeathAnimation animation)
    {
        if (animation.isCompleted) return;
        
        animation.isCompleted = true;
        activeAnimations--;
        
        // Trigger events
        OnDeathAnimationCompleted?.Invoke(animation.unit);
        OnUnitReadyForRemoval?.Invoke(animation.unit);
        
        if (logAnimationSteps)
        {
            Debug.Log($"DeathAnimationController: Animation completed for {animation.unit?.name}");
        }
    }
    
    #endregion
    
    #region Audio Methods
    
    /// <summary>
    /// Plays death audio
    /// </summary>
    private void PlayDeathAudio()
    {
        if (!enableSound || audioSource == null || deathSound == null) return;
        
        audioSource.pitch = varyPitch ? Random.Range(pitchRange.x, pitchRange.y) : 1f;
        audioSource.PlayOneShot(deathSound, audioVolume);
        
        if (logAnimationSteps)
        {
            Debug.Log("DeathAnimationController: Death audio played");
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Cleans up completed animations
    /// </summary>
    private void CleanupCompletedAnimations()
    {
        if (runningAnimations != null)
        {
            for (int i = runningAnimations.Count - 1; i >= 0; i--)
            {
                if (runningAnimations[i].isCompleted || runningAnimations[i].unit == null)
                {
                    runningAnimations.RemoveAt(i);
                }
            }
        }
    }
    
    /// <summary>
    /// Updates animation LOD based on distance
    /// </summary>
    private void UpdateAnimationLOD()
    {
        if (mainCamera == null) return;
        
        Vector3 cameraPos = mainCamera.transform.position;
        
        foreach (DeathAnimation animation in runningAnimations)
        {
            if (animation.unit == null || animation.isCompleted) continue;
            
            float distance = Vector3.Distance(animation.unit.transform.position, cameraPos);
            
            // Skip distant animations
            if (distance > cullingDistance && !animation.isCompleted)
            {
                if (animation.animationCoroutine != null)
                {
                    StopCoroutine(animation.animationCoroutine);
                }
                CompleteAnimation(animation);
            }
        }
    }
    
    #endregion
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Stop all running animations
        if (runningAnimations != null)
        {
            foreach (DeathAnimation animation in runningAnimations)
            {
                if (animation.animationCoroutine != null)
                {
                    StopCoroutine(animation.animationCoroutine);
                }
            }
            runningAnimations.Clear();
        }
        
        // Clear event references
        OnDeathAnimationStarted = null;
        OnDeathAnimationCompleted = null;
        OnUnitReadyForRemoval = null;
        
        if (enableDebugging)
        {
            Debug.Log("DeathAnimationController destroyed - Animations cleaned up");
        }
    }
    
    #region Debug Visualization
    
    void OnDrawGizmos()
    {
        if (!visualizeAnimationBounds || !enableDebugging) return;
        
        // Draw culling distance
        if (enableLODOptimization)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, cullingDistance);
        }
        
        // Draw explosion radius for running animations
        Gizmos.color = Color.red;
        foreach (DeathAnimation animation in runningAnimations ?? new System.Collections.Generic.List<DeathAnimation>())
        {
            if (animation.unit != null && !animation.isCompleted)
            {
                Gizmos.DrawWireSphere(animation.unit.transform.position, explosionRadius);
            }
        }
    }
    
    #endregion
}