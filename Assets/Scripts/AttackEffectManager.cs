using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Attack visual effects coordination with particles and animation.
/// Manages attack effects, particles, screen shake, and visual feedback for combat actions.
/// Part of Task 2.1.4 - Combat Visual Feedback - COMPLETES SUB-MILESTONE 2.1
/// </summary>
public class AttackEffectManager : MonoBehaviour
{
    [Header("Attack Effect Configuration")]
    [SerializeField] private GameObject attackEffectPrefab;
    [SerializeField] private float effectDuration = 1.0f;
    [SerializeField] private bool enableParticles = true;
    [SerializeField] private bool enableScreenShake = false;
    [SerializeField] private float screenShakeIntensity = 0.1f;
    [SerializeField] private float screenShakeDuration = 0.3f;
    [SerializeField] private Color effectColor = Color.yellow;
    [SerializeField] private bool enableSound = true;
    
    [Header("Damage Effect Configuration")]
    [SerializeField] private GameObject damageEffectPrefab;
    [SerializeField] private float damageEffectDuration = 0.8f;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private bool enableDamageNumbers = true;
    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private float damageNumberLifetime = 2.0f;
    
    [Header("Critical Hit Effects")]
    [SerializeField] private bool enableCriticalHitEffects = true;
    [SerializeField] private GameObject criticalHitEffectPrefab;
    [SerializeField] private Color criticalHitColor = Color.orange;
    [SerializeField] private float criticalHitIntensity = 1.5f;
    [SerializeField] private AudioClip criticalHitSound;
    
    [Header("Audio Configuration")]
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private float audioVolume = 0.7f;
    [SerializeField] private bool varyPitch = true;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.8f, 1.2f);
    
    [Header("Performance Settings")]
    [SerializeField] private int maxSimultaneousEffects = 10;
    [SerializeField] private bool enableEffectPooling = true;
    [SerializeField] private int effectPoolSize = 20;
    [SerializeField] private bool enableLODOptimization = true;
    [SerializeField] private float cullingDistance = 30.0f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugging = false;
    [SerializeField] private bool logEffectTriggers = false;
    [SerializeField] private bool showEffectBounds = false;
    
    // Component references
    private Camera mainCamera;
    private AudioSource audioSource;
    private CombatVisualManager visualManager;
    
    // Effect management
    private Queue<GameObject> effectPool;
    private List<GameObject> activeEffects;
    private int effectsCreatedThisFrame = 0;
    private float lastFrameTime = 0f;
    
    // Screen shake state
    private bool isScreenShaking = false;
    private Vector3 originalCameraPosition;
    private Coroutine screenShakeCoroutine;
    
    // Events
    public System.Action<Vector3, string> OnEffectTriggered;
    public System.Action<IAttacker, IAttackable, AttackResult> OnAttackEffectCompleted;
    
    // Properties
    public bool IsInitialized { get; private set; } = false;
    public int ActiveEffectsCount => activeEffects?.Count ?? 0;
    public int PooledEffectsCount => effectPool?.Count ?? 0;
    
    /// <summary>
    /// Initializes the attack effect manager
    /// </summary>
    public void Initialize(CombatVisualManager manager)
    {
        visualManager = manager;
        
        FindComponents();
        InitializeAudio();
        InitializeEffectPool();
        
        IsInitialized = true;
        
        if (enableDebugging)
        {
            Debug.Log("AttackEffectManager initialized - Visual combat effects ready");
        }
    }
    
    void Update()
    {
        // Reset frame counter
        if (Time.time != lastFrameTime)
        {
            effectsCreatedThisFrame = 0;
            lastFrameTime = Time.time;
        }
        
        // Clean up finished effects
        CleanupFinishedEffects();
        
        // Update LOD if enabled
        if (enableLODOptimization)
        {
            UpdateEffectLOD();
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
        
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.localPosition;
        }
        
        if (enableDebugging)
        {
            Debug.Log($"AttackEffectManager found camera: {mainCamera != null}");
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
            Debug.Log("AttackEffectManager audio system initialized");
        }
    }
    
    /// <summary>
    /// Initializes effect pooling system
    /// </summary>
    private void InitializeEffectPool()
    {
        if (!enableEffectPooling) return;
        
        effectPool = new Queue<GameObject>();
        activeEffects = new List<GameObject>();
        
        // Pre-populate pool if we have prefabs
        if (attackEffectPrefab != null)
        {
            for (int i = 0; i < effectPoolSize / 2; i++)
            {
                GameObject effect = Instantiate(attackEffectPrefab);
                effect.SetActive(false);
                effectPool.Enqueue(effect);
            }
        }
        
        if (damageEffectPrefab != null)
        {
            for (int i = 0; i < effectPoolSize / 2; i++)
            {
                GameObject effect = Instantiate(damageEffectPrefab);
                effect.SetActive(false);
                effectPool.Enqueue(effect);
            }
        }
        
        if (enableDebugging)
        {
            Debug.Log($"AttackEffectManager effect pool initialized with {effectPool.Count} effects");
        }
    }
    
    #region Public Interface
    
    /// <summary>
    /// Plays attack effect between attacker and target
    /// </summary>
    public void PlayAttackEffect(IAttacker attacker, IAttackable target, AttackResult result)
    {
        if (!IsInitialized || attacker?.Transform == null || target?.Transform == null)
        {
            return;
        }
        
        Vector3 attackerPos = attacker.Transform.position;
        Vector3 targetPos = target.Transform.position;
        
        // Play attack effect at attacker position
        PlayEffectAtPosition(attackEffectPrefab, attackerPos, effectDuration, effectColor);
        
        // Play damage effect at target position
        if (result.success && result.damage > 0)
        {
            PlayDamageEffect(target, result.damage);
        }
        
        // Play audio
        PlayAttackAudio(false); // No critical hit support in basic AttackResult
        
        // Screen shake if enabled
        if (enableScreenShake && result.success)
        {
            TriggerScreenShake();
        }
        
        // Create line effect between attacker and target
        StartCoroutine(CreateAttackLine(attackerPos, targetPos));
        
        OnEffectTriggered?.Invoke(targetPos, "AttackEffect");
        
        if (logEffectTriggers)
        {
            Debug.Log($"AttackEffectManager: Attack effect played - {attacker.GetDisplayInfo()} → {target.GetDisplayInfo()}");
        }
        
        // Notify completion after effect duration
        StartCoroutine(NotifyEffectCompletion(attacker, target, result, effectDuration));
    }
    
    /// <summary>
    /// Plays damage effect for a unit
    /// </summary>
    public void PlayDamageEffect(IAttackable target, int damage)
    {
        if (!IsInitialized || target?.Transform == null) return;
        
        Vector3 position = target.Transform.position;
        
        // Play damage effect
        PlayEffectAtPosition(damageEffectPrefab, position, damageEffectDuration, damageColor);
        
        // Show damage numbers if enabled
        if (enableDamageNumbers)
        {
            ShowDamageNumber(position, damage, false);
        }
        
        // Play damage audio
        PlayAudio(damageSound);
        
        OnEffectTriggered?.Invoke(position, "DamageEffect");
        
        if (logEffectTriggers)
        {
            Debug.Log($"AttackEffectManager: Damage effect played - {damage} damage at {position}");
        }
    }
    
    /// <summary>
    /// Plays critical hit effect
    /// </summary>
    public void PlayCriticalHitEffect(Vector3 position)
    {
        if (!IsInitialized || !enableCriticalHitEffects) return;
        
        // Play enhanced effect for critical hit
        PlayEffectAtPosition(criticalHitEffectPrefab, position, effectDuration * criticalHitIntensity, criticalHitColor);
        
        // Play critical hit audio
        PlayAudio(criticalHitSound);
        
        // Enhanced screen shake for critical hits
        if (enableScreenShake)
        {
            TriggerScreenShake(screenShakeIntensity * criticalHitIntensity, screenShakeDuration * 1.5f);
        }
        
        OnEffectTriggered?.Invoke(position, "CriticalHitEffect");
        
        if (logEffectTriggers)
        {
            Debug.Log($"AttackEffectManager: Critical hit effect played at {position}");
        }
    }
    
    /// <summary>
    /// Shows damage number at position
    /// </summary>
    public void ShowDamageNumber(Vector3 position, int damage, bool isCritical)
    {
        if (!enableDamageNumbers || damageNumberPrefab == null) return;
        
        GameObject damageNumber = GetPooledEffect();
        if (damageNumber == null)
        {
            damageNumber = Instantiate(damageNumberPrefab);
        }
        
        damageNumber.transform.position = position + Vector3.up * 0.5f;
        damageNumber.SetActive(true);
        
        // Configure damage number
        ConfigureDamageNumber(damageNumber, damage, isCritical);
        
        // Return to pool after lifetime
        StartCoroutine(ReturnEffectToPool(damageNumber, damageNumberLifetime));
        
        if (logEffectTriggers)
        {
            Debug.Log($"AttackEffectManager: Damage number shown - {damage} at {position}");
        }
    }
    
    /// <summary>
    /// Triggers screen shake effect
    /// </summary>
    public void TriggerScreenShake(float intensity = -1f, float duration = -1f)
    {
        if (!enableScreenShake || mainCamera == null || isScreenShaking) return;
        
        float shakeIntensity = intensity > 0 ? intensity : screenShakeIntensity;
        float shakeDuration = duration > 0 ? duration : screenShakeDuration;
        
        if (screenShakeCoroutine != null)
        {
            StopCoroutine(screenShakeCoroutine);
        }
        
        screenShakeCoroutine = StartCoroutine(ScreenShakeCoroutine(shakeIntensity, shakeDuration));
        
        if (logEffectTriggers)
        {
            Debug.Log($"AttackEffectManager: Screen shake triggered - Intensity: {shakeIntensity}, Duration: {shakeDuration}");
        }
    }
    
    #endregion
    
    #region Effect Implementation
    
    /// <summary>
    /// Plays effect at specific position
    /// </summary>
    private void PlayEffectAtPosition(GameObject effectPrefab, Vector3 position, float duration, Color color)
    {
        if (effectPrefab == null || effectsCreatedThisFrame >= maxSimultaneousEffects) return;
        
        // Check distance culling
        if (enableLODOptimization && mainCamera != null)
        {
            float distance = Vector3.Distance(position, mainCamera.transform.position);
            if (distance > cullingDistance) return;
        }
        
        GameObject effect = GetPooledEffect();
        if (effect == null)
        {
            effect = Instantiate(effectPrefab);
        }
        
        effect.transform.position = position;
        effect.SetActive(true);
        
        // Configure effect color and properties
        ConfigureEffect(effect, color, duration);
        
        activeEffects.Add(effect);
        effectsCreatedThisFrame++;
        
        // Return to pool after duration
        StartCoroutine(ReturnEffectToPool(effect, duration));
    }
    
    /// <summary>
    /// Configures effect properties
    /// </summary>
    private void ConfigureEffect(GameObject effect, Color color, float duration)
    {
        // Configure particle systems
        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            var main = ps.main;
            main.startColor = color;
            main.startLifetime = duration;
            
            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0.0f, (short)(30 * (duration / effectDuration)))
            });
        }
        
        // Configure renderers
        Renderer[] renderers = effect.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material != null)
            {
                renderer.material.color = color;
            }
        }
    }
    
    /// <summary>
    /// Configures damage number display
    /// </summary>
    private void ConfigureDamageNumber(GameObject damageNumber, int damage, bool isCritical)
    {
        // Configure text component
        UnityEngine.UI.Text text = damageNumber.GetComponent<UnityEngine.UI.Text>();
        if (text != null)
        {
            text.text = damage.ToString();
            text.color = isCritical ? criticalHitColor : damageColor;
            text.fontSize = isCritical ? 24 : 18;
        }
        
        // Configure TextMesh component
        TextMesh textMesh = damageNumber.GetComponent<TextMesh>();
        if (textMesh != null)
        {
            textMesh.text = damage.ToString();
            textMesh.color = isCritical ? criticalHitColor : damageColor;
            textMesh.fontSize = isCritical ? 48 : 36;
        }
        
        // Start floating animation
        StartCoroutine(AnimateDamageNumber(damageNumber));
    }
    
    /// <summary>
    /// Creates attack line effect between positions
    /// </summary>
    private IEnumerator CreateAttackLine(Vector3 startPos, Vector3 endPos)
    {
        LineRenderer line = new GameObject("AttackLine").AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.material.color = effectColor;
        line.widthMultiplier = 0.1f;
        line.positionCount = 2;
        line.useWorldSpace = true;
        
        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);
        
        // Animate line
        float elapsedTime = 0f;
        float lineDuration = 0.2f;
        
        while (elapsedTime < lineDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1f - (elapsedTime / lineDuration);
            Color lineColor = effectColor;
            lineColor.a = alpha;
            line.material.color = lineColor;
            
            yield return null;
        }
        
        if (line != null && line.gameObject != null)
        {
            Destroy(line.gameObject);
        }
    }
    
    /// <summary>
    /// Animates floating damage number
    /// </summary>
    private IEnumerator AnimateDamageNumber(GameObject damageNumber)
    {
        Vector3 startPos = damageNumber.transform.position;
        Vector3 endPos = startPos + Vector3.up * 2.0f + Random.insideUnitSphere * 0.5f;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < damageNumberLifetime && damageNumber != null)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / damageNumberLifetime;
            
            // Move upward
            damageNumber.transform.position = Vector3.Lerp(startPos, endPos, progress);
            
            // Fade out
            float alpha = 1f - progress;
            
            // Apply alpha to text component
            UnityEngine.UI.Text text = damageNumber.GetComponent<UnityEngine.UI.Text>();
            if (text != null)
            {
                Color color = text.color;
                color.a = alpha;
                text.color = color;
            }
            
            // Apply alpha to TextMesh component
            TextMesh textMesh = damageNumber.GetComponent<TextMesh>();
            if (textMesh != null)
            {
                Color color = textMesh.color;
                color.a = alpha;
                textMesh.color = color;
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Screen shake coroutine
    /// </summary>
    private IEnumerator ScreenShakeCoroutine(float intensity, float duration)
    {
        isScreenShaking = true;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration && mainCamera != null)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float currentIntensity = intensity * (1f - progress);
            
            Vector3 randomOffset = Random.insideUnitSphere * currentIntensity;
            randomOffset.z = 0; // Keep camera on same Z plane
            
            mainCamera.transform.localPosition = originalCameraPosition + randomOffset;
            
            yield return null;
        }
        
        // Return to original position
        if (mainCamera != null)
        {
            mainCamera.transform.localPosition = originalCameraPosition;
        }
        
        isScreenShaking = false;
        screenShakeCoroutine = null;
    }
    
    /// <summary>
    /// Notifies effect completion
    /// </summary>
    private IEnumerator NotifyEffectCompletion(IAttacker attacker, IAttackable target, AttackResult result, float delay)
    {
        yield return new WaitForSeconds(delay);
        OnAttackEffectCompleted?.Invoke(attacker, target, result);
    }
    
    #endregion
    
    #region Audio Methods
    
    /// <summary>
    /// Plays attack audio
    /// </summary>
    private void PlayAttackAudio(bool isCritical)
    {
        AudioClip clipToPlay = isCritical && criticalHitSound != null ? criticalHitSound : attackSound;
        PlayAudio(clipToPlay);
    }
    
    /// <summary>
    /// Plays audio clip
    /// </summary>
    private void PlayAudio(AudioClip clip)
    {
        if (!enableSound || audioSource == null || clip == null) return;
        
        audioSource.pitch = varyPitch ? Random.Range(pitchRange.x, pitchRange.y) : 1f;
        audioSource.PlayOneShot(clip, audioVolume);
    }
    
    #endregion
    
    #region Effect Pool Management
    
    /// <summary>
    /// Gets pooled effect object
    /// </summary>
    private GameObject GetPooledEffect()
    {
        if (!enableEffectPooling || effectPool.Count == 0) return null;
        
        return effectPool.Dequeue();
    }
    
    /// <summary>
    /// Returns effect to pool after delay
    /// </summary>
    private IEnumerator ReturnEffectToPool(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (effect != null)
        {
            effect.SetActive(false);
            
            if (enableEffectPooling && effectPool.Count < effectPoolSize)
            {
                effectPool.Enqueue(effect);
            }
            else
            {
                Destroy(effect);
            }
            
            activeEffects.Remove(effect);
        }
    }
    
    /// <summary>
    /// Cleans up finished effects
    /// </summary>
    private void CleanupFinishedEffects()
    {
        if (activeEffects == null) return;
        
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i] == null || !activeEffects[i].activeInHierarchy)
            {
                activeEffects.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// Updates effect LOD based on distance
    /// </summary>
    private void UpdateEffectLOD()
    {
        if (mainCamera == null || activeEffects == null) return;
        
        Vector3 cameraPos = mainCamera.transform.position;
        
        foreach (GameObject effect in activeEffects)
        {
            if (effect == null) continue;
            
            float distance = Vector3.Distance(effect.transform.position, cameraPos);
            bool shouldBeActive = distance <= cullingDistance;
            
            if (effect.activeInHierarchy != shouldBeActive)
            {
                effect.SetActive(shouldBeActive);
            }
        }
    }
    
    #endregion
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Stop all coroutines
        if (screenShakeCoroutine != null)
        {
            StopCoroutine(screenShakeCoroutine);
        }
        
        // Clean up effect pool
        if (effectPool != null)
        {
            while (effectPool.Count > 0)
            {
                GameObject effect = effectPool.Dequeue();
                if (effect != null)
                {
                    Destroy(effect);
                }
            }
        }
        
        // Clean up active effects
        if (activeEffects != null)
        {
            foreach (GameObject effect in activeEffects)
            {
                if (effect != null)
                {
                    Destroy(effect);
                }
            }
            activeEffects.Clear();
        }
        
        // Clear event references
        OnEffectTriggered = null;
        OnAttackEffectCompleted = null;
        
        if (enableDebugging)
        {
            Debug.Log("AttackEffectManager destroyed - Effects cleaned up");
        }
    }
}