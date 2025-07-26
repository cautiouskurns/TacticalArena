using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Individual unit health bar display and animation system.
/// Provides real-time health visualization with smooth animations and color coding
/// for clear tactical information display.
/// Part of Task 2.1.4 - Combat Visual Feedback - COMPLETES SUB-MILESTONE 2.1
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("Health Bar Configuration")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private Color criticalHealthColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private float lowHealthThreshold = 0.33f;
    [SerializeField] private float criticalHealthThreshold = 0.1f;
    
    [Header("Animation Settings")]
    [SerializeField] private bool enableAnimation = true;
    [SerializeField] private float animationSpeed = 5.0f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool enablePulseOnDamage = true;
    [SerializeField] private float pulseDuration = 0.5f;
    [SerializeField] private float pulseIntensity = 1.2f;
    
    [Header("Display Settings")]
    [SerializeField] private bool showHealthText = false;
    [SerializeField] private Text healthText;
    [SerializeField] private bool autoHideWhenFull = false;
    [SerializeField] private float hideDelay = 2.0f;
    [SerializeField] private bool alwaysVisible = true;
    [SerializeField] private float fadeSpeed = 3.0f;
    
    [Header("World Space Settings")]
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private Vector3 worldOffset = Vector3.up * 1.5f;
    [SerializeField] private bool scaleWithDistance = false;
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1.5f;
    [SerializeField] private float scaleDistance = 20.0f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugging = true;
    [SerializeField] private bool showHealthValues = true;
    
    // Component references
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Camera mainCamera;
    
    // Health tracking
    private Unit targetUnit;
    private HealthComponent healthComponent;
    private int currentHealth;
    private int maxHealth;
    private float currentHealthPercentage = 1.0f;
    private float targetHealthPercentage = 1.0f;
    
    // Animation state
    private bool isAnimating = false;
    private Coroutine animationCoroutine;
    private Coroutine pulseCoroutine;
    private Coroutine hideCoroutine;
    private Vector3 originalScale;
    
    // Events
    public System.Action<HealthBarUI, float> OnHealthPercentageChanged;
    public System.Action<HealthBarUI> OnHealthBarHidden;
    public System.Action<HealthBarUI> OnHealthBarShown;
    
    // Properties
    public Unit TargetUnit => targetUnit;
    public float HealthPercentage => currentHealthPercentage;
    public bool IsVisible => canvasGroup?.alpha > 0f;
    public bool IsAnimating => isAnimating;
    
    void Awake()
    {
        InitializeHealthBar();
    }
    
    void Start()
    {
        FindComponents();
        SetupHealthBar();
        
        if (enableDebugging)
        {
            Debug.Log($"HealthBarUI initialized for {targetUnit?.name ?? "unknown unit"}");
        }
    }
    
    void Update()
    {
        if (faceCamera && mainCamera != null)
        {
            UpdateCameraFacing();
        }
        
        if (scaleWithDistance && mainCamera != null)
        {
            UpdateDistanceScaling();
        }
        
        UpdateHealthDisplay();
    }
    
    /// <summary>
    /// Initializes the health bar components
    /// </summary>
    private void InitializeHealthBar()
    {
        // Get or create canvas
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100; // Ensure it renders on top
        }
        
        // Get or create canvas group for fading
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        rectTransform = GetComponent<RectTransform>();
        originalScale = transform.localScale;
        
        // Find health fill image if not assigned
        if (healthFillImage == null)
        {
            healthFillImage = GetComponentInChildren<Image>();
        }
        
        // Create visual elements if they don't exist
        if (healthFillImage == null)
        {
            CreateHealthBarElements();
        }
        
        // Ensure fill image is configured correctly
        if (healthFillImage != null)
        {
            healthFillImage.type = Image.Type.Filled;
            healthFillImage.fillMethod = Image.FillMethod.Horizontal;
            healthFillImage.fillAmount = 1.0f;
            healthFillImage.color = fullHealthColor;
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
        
        // Set camera on canvas if needed
        if (canvas != null && canvas.worldCamera == null && mainCamera != null)
        {
            canvas.worldCamera = mainCamera;
        }
        
        // Find target unit (parent or assigned)
        if (targetUnit == null)
        {
            targetUnit = GetComponentInParent<Unit>();
        }
        
        // Find health component
        if (targetUnit != null)
        {
            healthComponent = targetUnit.GetComponent<HealthComponent>();
        }
        
        if (enableDebugging)
        {
            Debug.Log($"HealthBarUI components found - Camera: {mainCamera != null}, " +
                     $"Unit: {targetUnit != null}, Health: {healthComponent != null}");
        }
    }
    
    /// <summary>
    /// Sets up the health bar for the target unit
    /// </summary>
    private void SetupHealthBar()
    {
        if (healthComponent == null) return;
        
        // Initialize health values
        maxHealth = healthComponent.MaxHealth;
        currentHealth = healthComponent.CurrentHealth;
        currentHealthPercentage = (float)currentHealth / maxHealth;
        targetHealthPercentage = currentHealthPercentage;
        
        // Set initial display
        UpdateHealthBarDisplay();
        
        // Subscribe to health events
        if (healthComponent != null)
        {
            healthComponent.OnHealthChanged += HandleHealthChanged;
            healthComponent.OnDamaged += HandleUnitDamaged;
            healthComponent.OnHealed += HandleUnitHealed;
            healthComponent.OnDied += HandleUnitDied;
        }
        
        // Set world position if specified
        if (targetUnit != null)
        {
            transform.position = targetUnit.transform.position + worldOffset;
        }
        
        if (enableDebugging)
        {
            Debug.Log($"HealthBarUI setup complete - Health: {currentHealth}/{maxHealth}");
        }
    }
    
    /// <summary>
    /// Updates camera facing
    /// </summary>
    private void UpdateCameraFacing()
    {
        if (mainCamera == null) return;
        
        Vector3 directionToCamera = mainCamera.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(-directionToCamera, Vector3.up);
    }
    
    /// <summary>
    /// Updates distance-based scaling
    /// </summary>
    private void UpdateDistanceScaling()
    {
        if (mainCamera == null) return;
        
        float distance = Vector3.Distance(transform.position, mainCamera.transform.position);
        float normalizedDistance = Mathf.Clamp01(distance / scaleDistance);
        float scale = Mathf.Lerp(minScale, maxScale, normalizedDistance);
        
        transform.localScale = originalScale * scale;
    }
    
    /// <summary>
    /// Updates health display
    /// </summary>
    private void UpdateHealthDisplay()
    {
        if (healthComponent == null) return;
        
        // Update current health if changed
        int newHealth = healthComponent.CurrentHealth;
        if (newHealth != currentHealth)
        {
            currentHealth = newHealth;
            float newPercentage = (float)currentHealth / maxHealth;
            SetHealthPercentage(newPercentage);
        }
        
        // Update text display
        if (showHealthText && healthText != null)
        {
            healthText.text = showHealthValues ? $"{currentHealth}/{maxHealth}" : $"{currentHealthPercentage:P0}";
        }
    }
    
    #region Health Event Handlers
    
    /// <summary>
    /// Handles health changed events
    /// </summary>
    private void HandleHealthChanged(int oldHealth, int newHealth)
    {
        float newPercentage = (float)newHealth / maxHealth;
        SetHealthPercentage(newPercentage);
        
        if (enableDebugging)
        {
            Debug.Log($"HealthBarUI: Health changed - {oldHealth} → {newHealth} ({newPercentage:P0})");
        }
    }
    
    /// <summary>
    /// Handles unit damaged events
    /// </summary>
    private void HandleUnitDamaged(int damage, IAttacker attacker)
    {
        if (enablePulseOnDamage)
        {
            StartPulseAnimation();
        }
        
        // Show health bar if it was hidden
        if (!alwaysVisible && !IsVisible)
        {
            ShowHealthBar();
        }
        
        if (enableDebugging)
        {
            Debug.Log($"HealthBarUI: Unit damaged - {damage} damage taken");
        }
    }
    
    /// <summary>
    /// Handles unit healed events
    /// </summary>
    private void HandleUnitHealed(int healAmount)
    {
        if (enableDebugging)
        {
            Debug.Log($"HealthBarUI: Unit healed - {healAmount} HP restored");
        }
    }
    
    /// <summary>
    /// Handles unit died events
    /// </summary>
    private void HandleUnitDied(IAttacker killer)
    {
        SetHealthPercentage(0f);
        
        // Start fade out animation
        StartCoroutine(FadeOutAndDestroy());
        
        if (enableDebugging)
        {
            Debug.Log($"HealthBarUI: Unit died - starting fade out, killed by {killer?.GetDisplayInfo() ?? "unknown"}");
        }
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Sets the target unit for this health bar
    /// </summary>
    public void SetTargetUnit(Unit unit)
    {
        // Unsubscribe from old unit events
        if (healthComponent != null)
        {
            healthComponent.OnHealthChanged -= HandleHealthChanged;
            healthComponent.OnDamaged -= HandleUnitDamaged;
            healthComponent.OnHealed -= HandleUnitHealed;
            healthComponent.OnDied -= HandleUnitDied;
        }
        
        targetUnit = unit;
        
        if (targetUnit != null)
        {
            healthComponent = targetUnit.GetComponent<HealthComponent>();
            SetupHealthBar();
        }
        
        if (enableDebugging)
        {
            Debug.Log($"HealthBarUI: Target unit set to {unit?.name ?? "null"}");
        }
    }
    
    /// <summary>
    /// Sets health percentage with animation
    /// </summary>
    public void SetHealthPercentage(float percentage)
    {
        if (enableDebugging)
        {
            Debug.Log($"HealthBarUI: SetHealthPercentage called for {gameObject.name} - {percentage:P0} (was {currentHealthPercentage:P0})");
        }
        
        percentage = Mathf.Clamp01(percentage);
        targetHealthPercentage = percentage;
        
        if (enableAnimation)
        {
            StartHealthAnimation();
        }
        else
        {
            currentHealthPercentage = percentage;
            UpdateHealthBarDisplay();
        }
        
        OnHealthPercentageChanged?.Invoke(this, percentage);
        
        // Auto-hide if at full health and enabled
        if (autoHideWhenFull && percentage >= 1.0f)
        {
            StartAutoHide();
        }
    }
    
    /// <summary>
    /// Shows the health bar
    /// </summary>
    public void ShowHealthBar()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        
        StartCoroutine(FadeIn());
        OnHealthBarShown?.Invoke(this);
    }
    
    /// <summary>
    /// Hides the health bar
    /// </summary>
    public void HideHealthBar()
    {
        StartCoroutine(FadeOut());
        OnHealthBarHidden?.Invoke(this);
    }
    
    /// <summary>
    /// Forces immediate health update
    /// </summary>
    public void ForceUpdateHealth()
    {
        if (healthComponent != null)
        {
            currentHealth = healthComponent.CurrentHealth;
            maxHealth = healthComponent.MaxHealth;
            float percentage = (float)currentHealth / maxHealth;
            SetHealthPercentage(percentage);
        }
    }
    
    #endregion
    
    #region Animation Methods
    
    /// <summary>
    /// Starts health percentage animation
    /// </summary>
    private void StartHealthAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        animationCoroutine = StartCoroutine(AnimateHealthPercentage());
    }
    
    /// <summary>
    /// Animates health percentage change
    /// </summary>
    private IEnumerator AnimateHealthPercentage()
    {
        isAnimating = true;
        float startPercentage = currentHealthPercentage;
        float elapsedTime = 0f;
        float duration = Mathf.Abs(targetHealthPercentage - startPercentage) / animationSpeed;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float curveValue = animationCurve.Evaluate(progress);
            
            currentHealthPercentage = Mathf.Lerp(startPercentage, targetHealthPercentage, curveValue);
            UpdateHealthBarDisplay();
            
            yield return null;
        }
        
        currentHealthPercentage = targetHealthPercentage;
        UpdateHealthBarDisplay();
        isAnimating = false;
        animationCoroutine = null;
    }
    
    /// <summary>
    /// Starts pulse animation on damage
    /// </summary>
    private void StartPulseAnimation()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        
        pulseCoroutine = StartCoroutine(PulseAnimation());
    }
    
    /// <summary>
    /// Pulse animation coroutine
    /// </summary>
    private IEnumerator PulseAnimation()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * pulseIntensity;
        
        float elapsedTime = 0f;
        float halfDuration = pulseDuration * 0.5f;
        
        // Scale up
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfDuration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            yield return null;
        }
        
        elapsedTime = 0f;
        
        // Scale down
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfDuration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            yield return null;
        }
        
        transform.localScale = originalScale;
        pulseCoroutine = null;
    }
    
    /// <summary>
    /// Starts auto-hide timer
    /// </summary>
    private void StartAutoHide()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        
        hideCoroutine = StartCoroutine(AutoHideCoroutine());
    }
    
    /// <summary>
    /// Auto-hide coroutine
    /// </summary>
    private IEnumerator AutoHideCoroutine()
    {
        yield return new WaitForSeconds(hideDelay);
        HideHealthBar();
        hideCoroutine = null;
    }
    
    /// <summary>
    /// Fade in animation
    /// </summary>
    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsedTime < 1f / fadeSpeed)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime * fadeSpeed;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, progress);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    /// <summary>
    /// Fade out animation
    /// </summary>
    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsedTime < 1f / fadeSpeed)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime * fadeSpeed;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// Fade out and destroy health bar
    /// </summary>
    private IEnumerator FadeOutAndDestroy()
    {
        yield return StartCoroutine(FadeOut());
        
        // Wait a bit before destroying
        yield return new WaitForSeconds(0.5f);
        
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
    
    #endregion
    
    /// <summary>
    /// Updates the visual health bar display
    /// </summary>
    private void UpdateHealthBarDisplay()
    {
        if (healthFillImage == null) 
        {
            if (enableDebugging)
            {
                Debug.LogWarning($"HealthBarUI: healthFillImage is null for {gameObject.name}");
            }
            return;
        }
        
        // Update fill amount
        healthFillImage.fillAmount = currentHealthPercentage;
        
        // Update color based on health percentage
        Color targetColor = GetHealthColor(currentHealthPercentage);
        healthFillImage.color = targetColor;
        
        if (enableDebugging && showHealthValues)
        {
            Debug.Log($"HealthBarUI: Updated {gameObject.name} health bar to {currentHealthPercentage:P0} (fill: {healthFillImage.fillAmount})");
        }
    }
    
    /// <summary>
    /// Gets appropriate color for health percentage
    /// </summary>
    private Color GetHealthColor(float percentage)
    {
        if (percentage <= criticalHealthThreshold)
        {
            return criticalHealthColor;
        }
        else if (percentage <= lowHealthThreshold)
        {
            // Lerp between critical and low health colors
            float lerpValue = (percentage - criticalHealthThreshold) / (lowHealthThreshold - criticalHealthThreshold);
            return Color.Lerp(criticalHealthColor, lowHealthColor, lerpValue);
        }
        else
        {
            // Lerp between low and full health colors
            float lerpValue = (percentage - lowHealthThreshold) / (1f - lowHealthThreshold);
            return Color.Lerp(lowHealthColor, fullHealthColor, lerpValue);
        }
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    /// <summary>
    /// Creates the visual health bar elements
    /// </summary>
    private void CreateHealthBarElements()
    {
        // Set canvas size
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(2f, 0.4f);
        
        // Create background
        GameObject bgObject = new GameObject("Background");
        bgObject.transform.SetParent(transform);
        RectTransform bgRect = bgObject.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.localScale = Vector3.one;
        
        backgroundImage = bgObject.AddComponent<Image>();
        backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark gray background
        
        // Create health fill
        GameObject fillObject = new GameObject("HealthFill");
        fillObject.transform.SetParent(transform);
        RectTransform fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.localScale = Vector3.one;
        
        healthFillImage = fillObject.AddComponent<Image>();
        healthFillImage.color = fullHealthColor;
        healthFillImage.type = Image.Type.Filled;
        healthFillImage.fillMethod = Image.FillMethod.Horizontal;
        healthFillImage.fillOrigin = 0;
        healthFillImage.fillAmount = 1.0f;
        
        if (enableDebugging)
        {
            Debug.Log($"HealthBarUI: Created visual elements for {gameObject.name}");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (healthComponent != null)
        {
            healthComponent.OnHealthChanged -= HandleHealthChanged;
            healthComponent.OnDamaged -= HandleUnitDamaged;
            healthComponent.OnHealed -= HandleUnitHealed;
            healthComponent.OnDied -= HandleUnitDied;
        }
        
        // Stop all coroutines
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        
        // Clear event references
        OnHealthPercentageChanged = null;
        OnHealthBarHidden = null;
        OnHealthBarShown = null;
        
        if (enableDebugging)
        {
            Debug.Log("HealthBarUI destroyed");
        }
    }
}