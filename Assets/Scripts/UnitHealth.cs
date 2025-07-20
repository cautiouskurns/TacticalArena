using UnityEngine;
using System;

/// <summary>
/// Component responsible for unit health tracking, damage handling, and health-related
/// visual feedback. Provides foundation for combat mechanics and unit state management.
/// </summary>
public class UnitHealth : MonoBehaviour
{
    [Header("Health Configuration")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth = 3;
    [SerializeField] private bool isInvulnerable = false;
    [SerializeField] private bool canRegenerate = false;
    
    [Header("Regeneration Settings")]
    [SerializeField] private float regenerationRate = 1f; // Health per second
    [SerializeField] private float regenerationDelay = 5f; // Delay after taking damage
    [SerializeField] private int maxRegenerationHealth = 3;
    
    [Header("Damage Resistance")]
    [SerializeField] private float damageReduction = 0f; // 0.0 to 1.0 (percentage)
    [SerializeField] private bool hasArmor = false;
    [SerializeField] private int armorPoints = 0;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool enableVisualDamage = true;
    [SerializeField] private Color healthyColor = Color.white;
    [SerializeField] private Color damagedColor = Color.red;
    [SerializeField] private Color criticalColor = Color.darkRed;
    [SerializeField] private float damageFlashDuration = 0.5f;
    
    [Header("Audio Feedback")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private float audioVolume = 0.7f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogging = false;
    [SerializeField] private bool showHealthBar = false;
    
    // Component references
    private Unit unit;
    private Renderer unitRenderer;
    private AudioSource audioSource;
    private Material originalMaterial;
    
    // Health state tracking
    private float lastDamageTime = 0f;
    private bool isDead = false;
    private float healthPercentage = 1f;
    
    // Visual effect tracking
    private Coroutine damageFlashCoroutine;
    private bool isFlashing = false;
    
    // Events for health system integration
    public System.Action<int, int> OnHealthChanged; // currentHealth, maxHealth
    public System.Action<float> OnHealthPercentageChanged; // 0.0 to 1.0
    public System.Action<int> OnDamageTaken; // damage amount
    public System.Action<int> OnHealthRestored; // heal amount
    public System.Action OnUnitDeath;
    public System.Action OnUnitRevived;
    public System.Action<float> OnRegenerationTick; // amount regenerated
    
    // Public properties
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public float HealthPercentage => healthPercentage;
    public bool IsAlive => !isDead && currentHealth > 0;
    public bool IsDead => isDead;
    public bool IsFullHealth => currentHealth >= maxHealth;
    public bool IsCriticalHealth => healthPercentage <= 0.25f;
    public bool IsInvulnerable => isInvulnerable;
    public bool CanRegenerate => canRegenerate && IsAlive;
    
    void Awake()
    {
        InitializeHealthSystem();
    }
    
    void Start()
    {
        FindComponentReferences();
        SetupInitialHealth();
        SetupAudioSystem();
    }
    
    void Update()
    {
        if (canRegenerate && IsAlive)
        {
            HandleRegeneration();
        }
        
        UpdateVisualFeedback();
    }
    
    /// <summary>
    /// Initializes the health system
    /// </summary>
    private void InitializeHealthSystem()
    {
        // Ensure health values are valid
        if (maxHealth <= 0)
        {
            maxHealth = 1;
            Debug.LogWarning($"UnitHealth on {gameObject.name}: maxHealth was <= 0, set to 1");
        }
        
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }
        
        // Calculate initial health percentage
        UpdateHealthPercentage();
        
        if (enableDebugLogging)
        {
            Debug.Log($"UnitHealth initialized: {currentHealth}/{maxHealth} HP");
        }
    }
    
    /// <summary>
    /// Finds references to other components
    /// </summary>
    private void FindComponentReferences()
    {
        unit = GetComponent<Unit>();
        unitRenderer = GetComponent<Renderer>();
        
        if (unitRenderer != null)
        {
            originalMaterial = unitRenderer.material;
        }
        
        if (unit == null)
        {
            Debug.LogWarning($"UnitHealth on {gameObject.name}: Unit component not found");
        }
    }
    
    /// <summary>
    /// Sets up initial health state
    /// </summary>
    private void SetupInitialHealth()
    {
        // Set initial death state
        isDead = currentHealth <= 0;
        
        // Initialize regeneration settings
        if (canRegenerate && maxRegenerationHealth <= 0)
        {
            maxRegenerationHealth = maxHealth;
        }
        
        // Trigger initial health change event
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHealthPercentageChanged?.Invoke(healthPercentage);
    }
    
    /// <summary>
    /// Sets up audio system for health feedback
    /// </summary>
    private void SetupAudioSystem()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (damageSound != null || healSound != null || deathSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = audioVolume;
            audioSource.playOnAwake = false;
        }
    }
    
    /// <summary>
    /// Applies damage to the unit
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead || isInvulnerable || damage <= 0)
            return;
        
        // Calculate actual damage after reduction
        int actualDamage = CalculateActualDamage(damage);
        
        if (actualDamage <= 0)
            return;
        
        // Apply damage
        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - actualDamage);
        
        // Update tracking
        lastDamageTime = Time.time;
        UpdateHealthPercentage();
        
        // Trigger events
        OnDamageTaken?.Invoke(actualDamage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHealthPercentageChanged?.Invoke(healthPercentage);
        
        // Handle death
        if (currentHealth <= 0 && !isDead)
        {
            HandleDeath();
        }
        else
        {
            // Handle damage feedback
            PlayDamageEffects();
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"{gameObject.name} took {actualDamage} damage ({previousHealth} -> {currentHealth})");
        }
    }
    
    /// <summary>
    /// Calculates actual damage after armor and resistance
    /// </summary>
    private int CalculateActualDamage(int baseDamage)
    {
        float damage = baseDamage;
        
        // Apply armor reduction
        if (hasArmor && armorPoints > 0)
        {
            damage = Mathf.Max(1, damage - armorPoints);
        }
        
        // Apply damage reduction percentage
        if (damageReduction > 0)
        {
            damage *= (1f - Mathf.Clamp01(damageReduction));
        }
        
        return Mathf.RoundToInt(damage);
    }
    
    /// <summary>
    /// Restores health to the unit
    /// </summary>
    public void RestoreHealth(int healAmount)
    {
        if (isDead || healAmount <= 0)
            return;
        
        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        
        if (currentHealth != previousHealth)
        {
            UpdateHealthPercentage();
            
            // Trigger events
            OnHealthRestored?.Invoke(healAmount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnHealthPercentageChanged?.Invoke(healthPercentage);
            
            // Play heal effects
            PlayHealEffects();
            
            if (enableDebugLogging)
            {
                Debug.Log($"{gameObject.name} restored {healAmount} health ({previousHealth} -> {currentHealth})");
            }
        }
    }
    
    /// <summary>
    /// Sets health to a specific value
    /// </summary>
    public void SetHealth(int newHealth)
    {
        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        
        if (currentHealth != previousHealth)
        {
            UpdateHealthPercentage();
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnHealthPercentageChanged?.Invoke(healthPercentage);
            
            // Handle death if health set to 0
            if (currentHealth <= 0 && !isDead)
            {
                HandleDeath();
            }
            // Handle revival if health restored from 0
            else if (currentHealth > 0 && isDead)
            {
                HandleRevival();
            }
        }
    }
    
    /// <summary>
    /// Sets maximum health (and optionally current health)
    /// </summary>
    public void SetMaxHealth(int newMaxHealth, bool adjustCurrentHealth = false)
    {
        if (newMaxHealth <= 0)
        {
            Debug.LogWarning("Cannot set max health to 0 or negative");
            return;
        }
        
        maxHealth = newMaxHealth;
        
        if (adjustCurrentHealth)
        {
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }
        
        UpdateHealthPercentage();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHealthPercentageChanged?.Invoke(healthPercentage);
    }
    
    /// <summary>
    /// Handles regeneration logic
    /// </summary>
    private void HandleRegeneration()
    {
        if (currentHealth >= maxRegenerationHealth)
            return;
        
        if (Time.time - lastDamageTime < regenerationDelay)
            return;
        
        float regenAmount = regenerationRate * Time.deltaTime;
        float newHealth = currentHealth + regenAmount;
        
        if (newHealth > currentHealth + 0.99f) // Only when we can restore at least 1 HP
        {
            int healthToRestore = Mathf.FloorToInt(newHealth - currentHealth);
            RestoreHealth(healthToRestore);
            
            OnRegenerationTick?.Invoke(healthToRestore);
        }
    }
    
    /// <summary>
    /// Updates health percentage tracking
    /// </summary>
    private void UpdateHealthPercentage()
    {
        healthPercentage = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    }
    
    /// <summary>
    /// Handles unit death
    /// </summary>
    private void HandleDeath()
    {
        if (isDead) return;
        
        isDead = true;
        currentHealth = 0;
        UpdateHealthPercentage();
        
        // Play death effects
        PlayDeathEffects();
        
        // Notify unit component
        if (unit != null)
        {
            unit.HandleDeath();
        }
        
        OnUnitDeath?.Invoke();
        
        if (enableDebugLogging)
        {
            Debug.Log($"{gameObject.name} has died");
        }
    }
    
    /// <summary>
    /// Handles unit revival
    /// </summary>
    private void HandleRevival()
    {
        if (!isDead) return;
        
        isDead = false;
        UpdateHealthPercentage();
        
        // Notify unit component
        if (unit != null)
        {
            unit.HandleRevival();
        }
        
        OnUnitRevived?.Invoke();
        
        if (enableDebugLogging)
        {
            Debug.Log($"{gameObject.name} has been revived");
        }
    }
    
    /// <summary>
    /// Plays damage visual and audio effects
    /// </summary>
    private void PlayDamageEffects()
    {
        // Play damage sound
        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound, audioVolume);
        }
        
        // Start damage flash effect
        if (enableVisualDamage && unitRenderer != null)
        {
            StartDamageFlash();
        }
    }
    
    /// <summary>
    /// Plays heal visual and audio effects
    /// </summary>
    private void PlayHealEffects()
    {
        // Play heal sound
        if (audioSource != null && healSound != null)
        {
            audioSource.PlayOneShot(healSound, audioVolume);
        }
    }
    
    /// <summary>
    /// Plays death visual and audio effects
    /// </summary>
    private void PlayDeathEffects()
    {
        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound, audioVolume);
        }
    }
    
    /// <summary>
    /// Starts damage flash visual effect
    /// </summary>
    private void StartDamageFlash()
    {
        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
        }
        
        damageFlashCoroutine = StartCoroutine(DamageFlashCoroutine());
    }
    
    /// <summary>
    /// Damage flash coroutine
    /// </summary>
    private System.Collections.IEnumerator DamageFlashCoroutine()
    {
        isFlashing = true;
        
        Color flashColor = IsCriticalHealth ? criticalColor : damagedColor;
        Color originalColor = originalMaterial != null ? originalMaterial.color : Color.white;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < damageFlashDuration)
        {
            float normalizedTime = elapsedTime / damageFlashDuration;
            float flashIntensity = Mathf.Sin(normalizedTime * Mathf.PI);
            
            Color currentColor = Color.Lerp(originalColor, flashColor, flashIntensity);
            
            if (unitRenderer != null && unitRenderer.material != null)
            {
                unitRenderer.material.color = currentColor;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Restore original color
        if (unitRenderer != null && unitRenderer.material != null)
        {
            unitRenderer.material.color = originalColor;
        }
        
        isFlashing = false;
        damageFlashCoroutine = null;
    }
    
    /// <summary>
    /// Updates visual feedback based on health status
    /// </summary>
    private void UpdateVisualFeedback()
    {
        if (!enableVisualDamage || isFlashing || unitRenderer == null)
            return;
        
        // Update material color based on health status
        if (!isDead && originalMaterial != null)
        {
            Color targetColor = originalMaterial.color;
            
            if (IsCriticalHealth)
            {
                targetColor = Color.Lerp(originalMaterial.color, criticalColor, 0.3f);
            }
            else if (healthPercentage < 0.5f)
            {
                targetColor = Color.Lerp(originalMaterial.color, damagedColor, 0.2f);
            }
            
            if (unitRenderer.material.color != targetColor)
            {
                unitRenderer.material.color = Color.Lerp(unitRenderer.material.color, targetColor, Time.deltaTime * 2f);
            }
        }
    }
    
    /// <summary>
    /// Sets invulnerability state
    /// </summary>
    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
        
        if (enableDebugLogging)
        {
            Debug.Log($"{gameObject.name} invulnerability: {invulnerable}");
        }
    }
    
    /// <summary>
    /// Gets health status information
    /// </summary>
    public HealthStatusInfo GetHealthStatus()
    {
        return new HealthStatusInfo
        {
            currentHealth = currentHealth,
            maxHealth = maxHealth,
            healthPercentage = healthPercentage,
            isAlive = IsAlive,
            isDead = isDead,
            isFullHealth = IsFullHealth,
            isCriticalHealth = IsCriticalHealth,
            isInvulnerable = isInvulnerable,
            canRegenerate = canRegenerate,
            hasArmor = hasArmor,
            armorPoints = armorPoints,
            damageReduction = damageReduction
        };
    }
    
    void OnDrawGizmos()
    {
        if (!showHealthBar || !Application.isPlaying)
            return;
        
        // Draw health bar above unit
        Vector3 healthBarPosition = transform.position + Vector3.up * 2f;
        Vector3 healthBarSize = new Vector3(1f, 0.1f, 0f);
        
        // Background
        Gizmos.color = Color.black;
        Gizmos.DrawCube(healthBarPosition, healthBarSize);
        
        // Health fill
        Gizmos.color = IsCriticalHealth ? Color.red : (healthPercentage > 0.5f ? Color.green : Color.yellow);
        Vector3 fillSize = new Vector3(healthBarSize.x * healthPercentage, healthBarSize.y, healthBarSize.z);
        Vector3 fillPosition = healthBarPosition - Vector3.right * (healthBarSize.x - fillSize.x) * 0.5f;
        Gizmos.DrawCube(fillPosition, fillSize);
    }
}

/// <summary>
/// Information structure for health status
/// </summary>
[System.Serializable]
public struct HealthStatusInfo
{
    public int currentHealth;
    public int maxHealth;
    public float healthPercentage;
    public bool isAlive;
    public bool isDead;
    public bool isFullHealth;
    public bool isCriticalHealth;
    public bool isInvulnerable;
    public bool canRegenerate;
    public bool hasArmor;
    public int armorPoints;
    public float damageReduction;
    
    public override string ToString()
    {
        return $"Health: {currentHealth}/{maxHealth} ({healthPercentage:P0}) - {(isAlive ? "Alive" : "Dead")}";
    }
}