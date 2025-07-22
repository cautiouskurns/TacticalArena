using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Targeting system that manages target selection and visual feedback for attacks.
/// Handles target highlighting, attack range display, and target selection input.
/// Integrates with CombatManager and AttackValidator for tactical target selection.
/// </summary>
public class TargetingSystem : MonoBehaviour
{
    [Header("Targeting Configuration")]
    [SerializeField] private bool enableTargetPreview = true;
    [SerializeField] private bool showAttackRange = true;
    [SerializeField] private LayerMask targetLayerMask = -1;
    [SerializeField] private float targetSelectionRadius = 0.5f;
    
    [Header("Visual Feedback")]
    [SerializeField] private Material targetHighlightMaterial;
    [SerializeField] private Color validTargetColor = Color.red;
    [SerializeField] private Color invalidTargetColor = Color.gray;
    [SerializeField] private Color attackRangeColor = new Color(1f, 0.5f, 0f, 0.3f);
    [SerializeField] private float targetHighlightDuration = 1.0f;
    [SerializeField] private bool enableTargetPulsing = true;
    [SerializeField] private float pulseSpeed = 2.0f;
    
    [Header("Range Visualization")]
    [SerializeField] private bool showRangeIndicators = true;
    [SerializeField] private Material rangeIndicatorMaterial;
    [SerializeField] private float rangeIndicatorHeight = 0.05f;
    [SerializeField] private int rangeIndicatorSegments = 16;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableTargetingLogging = true;
    
    // System references
    private CombatManager combatManager;
    private AttackValidator attackValidator;
    private GridManager gridManager;
    private Camera mainCamera;
    
    // Targeting state
    private bool targetingActive = false;
    private IAttacker currentAttacker;
    private List<IAttackable> validTargets = new List<IAttackable>();
    private List<IAttackable> highlightedTargets = new List<IAttackable>();
    private GameObject rangeIndicator;
    
    // Visual effects
    private Dictionary<IAttackable, Coroutine> pulseCoroutines = new Dictionary<IAttackable, Coroutine>();
    private Dictionary<IAttackable, Color> originalColors = new Dictionary<IAttackable, Color>();
    
    // Events
    public System.Action<IAttacker, IAttackable> OnTargetSelected;
    public System.Action<IAttacker, IAttackable> OnTargetHovered;
    public System.Action OnTargetingCanceled;
    public System.Action<IAttacker> OnTargetingStarted;
    public System.Action<IAttacker> OnTargetingStopped;
    
    // Properties
    public bool IsTargeting => targetingActive;
    public IAttacker CurrentAttacker => currentAttacker;
    public int ValidTargetCount => validTargets.Count;
    
    void Awake()
    {
        InitializeTargetingSystem();
    }
    
    void Start()
    {
        FindSystemReferences();
        SetupTargetingMaterials();
    }
    
    void Update()
    {
        if (targetingActive)
        {
            HandleTargetingInput();
        }
    }
    
    /// <summary>
    /// Initializes the targeting system
    /// </summary>
    private void InitializeTargetingSystem()
    {
        if (enableTargetingLogging)
        {
            Debug.Log("TargetingSystem initialized - Target selection system ready");
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
            Debug.LogWarning("TargetingSystem: CombatManager not found on same GameObject");
        }
        
        attackValidator = GetComponent<AttackValidator>();
        if (attackValidator == null)
        {
            Debug.LogError("TargetingSystem: AttackValidator not found!");
        }
        
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("TargetingSystem: GridManager not found!");
        }
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (enableTargetingLogging)
        {
            Debug.Log($"TargetingSystem found references - Combat: {combatManager != null}, " +
                     $"Validator: {attackValidator != null}, Grid: {gridManager != null}, Camera: {mainCamera != null}");
        }
    }
    
    /// <summary>
    /// Sets up targeting materials if not assigned
    /// </summary>
    private void SetupTargetingMaterials()
    {
        if (targetHighlightMaterial == null)
        {
            targetHighlightMaterial = CreateTargetHighlightMaterial();
        }
        
        if (rangeIndicatorMaterial == null)
        {
            rangeIndicatorMaterial = CreateRangeIndicatorMaterial();
        }
    }
    
    /// <summary>
    /// Creates a default target highlight material
    /// </summary>
    private Material CreateTargetHighlightMaterial()
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = "TargetHighlight";
        
        // Configure for highlighting
        material.SetFloat("_Surface", 1); // Transparent
        material.SetFloat("_Blend", 0); // Alpha blend
        material.SetFloat("_AlphaClip", 0);
        material.color = new Color(validTargetColor.r, validTargetColor.g, validTargetColor.b, 0.7f);
        
        // Enable emission
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", validTargetColor * 0.5f);
        
        material.renderQueue = 3000; // Transparent queue
        
        return material;
    }
    
    /// <summary>
    /// Creates a default range indicator material
    /// </summary>
    private Material CreateRangeIndicatorMaterial()
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = "RangeIndicator";
        
        // Configure for transparency
        material.SetFloat("_Surface", 1); // Transparent
        material.SetFloat("_Blend", 0); // Alpha blend
        material.color = attackRangeColor;
        material.renderQueue = 3000;
        
        return material;
    }
    
    /// <summary>
    /// Starts targeting mode for the specified attacker
    /// </summary>
    public void StartTargeting(IAttacker attacker)
    {
        if (targetingActive || attacker == null)
        {
            return;
        }
        
        currentAttacker = attacker;
        targetingActive = true;
        
        // Find valid targets for this attacker
        RefreshValidTargets();
        
        // Show visual feedback
        if (enableTargetPreview)
        {
            HighlightValidTargets();
        }
        
        if (showAttackRange)
        {
            ShowAttackRange();
        }
        
        OnTargetingStarted?.Invoke(attacker);
        
        if (enableTargetingLogging)
        {
            Debug.Log($"TargetingSystem: Started targeting for {attacker.GetDisplayInfo()} - {validTargets.Count} valid targets");
        }
    }
    
    /// <summary>
    /// Stops targeting mode
    /// </summary>
    public void StopTargeting()
    {
        if (!targetingActive)
        {
            return;
        }
        
        // Clear visual feedback
        ClearTargetHighlights();
        HideAttackRange();
        
        // Reset state
        IAttacker previousAttacker = currentAttacker;
        currentAttacker = null;
        targetingActive = false;
        validTargets.Clear();
        
        OnTargetingStopped?.Invoke(previousAttacker);
        
        if (enableTargetingLogging)
        {
            Debug.Log("TargetingSystem: Stopped targeting");
        }
    }
    
    /// <summary>
    /// Handles targeting input (mouse clicks, etc.)
    /// </summary>
    private void HandleTargetingInput()
    {
        if (!targetingActive || mainCamera == null)
        {
            return;
        }
        
        // Handle mouse input for target selection
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = Input.mousePosition;
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, targetLayerMask))
            {
                // Check if hit object has an attackable component
                IAttackable target = hit.collider.GetComponent<IAttackable>();
                if (target != null && validTargets.Contains(target))
                {
                    SelectTarget(target);
                    return;
                }
            }
        }
        
        // Handle right-click or escape to cancel targeting
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelTargeting();
        }
        
        // Handle mouse hover for target preview
        if (enableTargetPreview)
        {
            HandleTargetHover();
        }
    }
    
    /// <summary>
    /// Handles target hover effects
    /// </summary>
    private void HandleTargetHover()
    {
        if (mainCamera == null) return;
        
        Vector3 mousePosition = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, targetLayerMask))
        {
            IAttackable target = hit.collider.GetComponent<IAttackable>();
            if (target != null && validTargets.Contains(target))
            {
                OnTargetHovered?.Invoke(currentAttacker, target);
            }
        }
    }
    
    /// <summary>
    /// Selects a target for attack
    /// </summary>
    private void SelectTarget(IAttackable target)
    {
        if (target == null || !validTargets.Contains(target))
        {
            return;
        }
        
        if (enableTargetingLogging)
        {
            Debug.Log($"TargetingSystem: Target selected - {target.GetDisplayInfo()}");
        }
        
        // Notify listeners
        OnTargetSelected?.Invoke(currentAttacker, target);
        
        // Stop targeting mode
        StopTargeting();
    }
    
    /// <summary>
    /// Cancels targeting mode
    /// </summary>
    public void CancelTargeting()
    {
        if (!targetingActive)
        {
            return;
        }
        
        if (enableTargetingLogging)
        {
            Debug.Log("TargetingSystem: Targeting canceled by user");
        }
        
        OnTargetingCanceled?.Invoke();
        StopTargeting();
    }
    
    /// <summary>
    /// Refreshes the list of valid targets for the current attacker
    /// </summary>
    private void RefreshValidTargets()
    {
        validTargets.Clear();
        
        if (currentAttacker == null || attackValidator == null)
        {
            return;
        }
        
        validTargets = attackValidator.GetValidTargets(currentAttacker);
        
        if (enableTargetingLogging)
        {
            Debug.Log($"TargetingSystem: Found {validTargets.Count} valid targets for {currentAttacker.GetDisplayInfo()}");
        }
    }
    
    /// <summary>
    /// Highlights all valid targets
    /// </summary>
    private void HighlightValidTargets()
    {
        foreach (IAttackable target in validTargets)
        {
            HighlightTarget(target, true);
        }
    }
    
    /// <summary>
    /// Highlights a specific target
    /// </summary>
    private void HighlightTarget(IAttackable target, bool isValid)
    {
        if (target?.Transform == null) return;
        
        Renderer targetRenderer = target.Transform.GetComponent<Renderer>();
        if (targetRenderer == null) return;
        
        // Store original color
        if (!originalColors.ContainsKey(target))
        {
            originalColors[target] = targetRenderer.material.color;
        }
        
        // Apply highlight color
        Color highlightColor = isValid ? validTargetColor : invalidTargetColor;
        targetRenderer.material.color = highlightColor;
        
        // Add to highlighted targets list
        if (!highlightedTargets.Contains(target))
        {
            highlightedTargets.Add(target);
        }
        
        // Start pulsing effect if enabled
        if (enableTargetPulsing)
        {
            if (pulseCoroutines.ContainsKey(target))
            {
                StopCoroutine(pulseCoroutines[target]);
            }
            pulseCoroutines[target] = StartCoroutine(PulseTarget(target, highlightColor));
        }
    }
    
    /// <summary>
    /// Clears all target highlights
    /// </summary>
    private void ClearTargetHighlights()
    {
        foreach (IAttackable target in highlightedTargets.ToArray())
        {
            ClearTargetHighlight(target);
        }
        
        highlightedTargets.Clear();
        originalColors.Clear();
    }
    
    /// <summary>
    /// Clears highlight from a specific target
    /// </summary>
    private void ClearTargetHighlight(IAttackable target)
    {
        if (target?.Transform == null) return;
        
        // Stop pulsing coroutine
        if (pulseCoroutines.ContainsKey(target))
        {
            StopCoroutine(pulseCoroutines[target]);
            pulseCoroutines.Remove(target);
        }
        
        // Restore original color
        if (originalColors.ContainsKey(target))
        {
            Renderer targetRenderer = target.Transform.GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                targetRenderer.material.color = originalColors[target];
            }
            originalColors.Remove(target);
        }
        
        highlightedTargets.Remove(target);
    }
    
    /// <summary>
    /// Pulse effect coroutine for target highlighting
    /// </summary>
    private IEnumerator PulseTarget(IAttackable target, Color baseColor)
    {
        if (target?.Transform == null) yield break;
        
        Renderer targetRenderer = target.Transform.GetComponent<Renderer>();
        if (targetRenderer == null) yield break;
        
        Color originalColor = originalColors.ContainsKey(target) ? originalColors[target] : targetRenderer.material.color;
        
        while (targetingActive && highlightedTargets.Contains(target))
        {
            float time = Time.time * pulseSpeed;
            float pulse = (Mathf.Sin(time) + 1f) * 0.5f; // 0 to 1
            
            Color pulsedColor = Color.Lerp(baseColor, originalColor, pulse * 0.3f);
            targetRenderer.material.color = pulsedColor;
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Shows the attack range indicator
    /// </summary>
    private void ShowAttackRange()
    {
        if (!showRangeIndicators || currentAttacker?.Transform == null || gridManager == null)
        {
            return;
        }
        
        HideAttackRange(); // Clear any existing indicator
        
        // Create range indicator object
        rangeIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rangeIndicator.name = "AttackRangeIndicator";
        
        // Remove collider to avoid interference
        Collider collider = rangeIndicator.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        // Position and scale the indicator
        Vector3 attackerPos = currentAttacker.Transform.position;
        rangeIndicator.transform.position = new Vector3(attackerPos.x, attackerPos.y + rangeIndicatorHeight, attackerPos.z);
        
        float rangeScale = currentAttacker.AttackRange * 2f;
        rangeIndicator.transform.localScale = new Vector3(rangeScale, rangeIndicatorHeight * 2f, rangeScale);
        
        // Apply material
        Renderer rangeRenderer = rangeIndicator.GetComponent<Renderer>();
        if (rangeRenderer != null && rangeIndicatorMaterial != null)
        {
            rangeRenderer.material = rangeIndicatorMaterial;
        }
        
        if (enableTargetingLogging)
        {
            Debug.Log($"TargetingSystem: Showing attack range indicator with radius {currentAttacker.AttackRange}");
        }
    }
    
    /// <summary>
    /// Hides the attack range indicator
    /// </summary>
    private void HideAttackRange()
    {
        if (rangeIndicator != null)
        {
            DestroyImmediate(rangeIndicator);
            rangeIndicator = null;
        }
    }
    
    /// <summary>
    /// Gets targeting info for debugging
    /// </summary>
    public string GetTargetingInfo()
    {
        return $"Targeting State: {(targetingActive ? "Active" : "Inactive")}, " +
               $"Current Attacker: {currentAttacker?.GetDisplayInfo() ?? "None"}, " +
               $"Valid Targets: {validTargets.Count}, " +
               $"Highlighted Targets: {highlightedTargets.Count}";
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Stop targeting if active
        StopTargeting();
        
        // Stop all pulse coroutines
        foreach (var coroutine in pulseCoroutines.Values)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        pulseCoroutines.Clear();
        
        // Clean up range indicator
        HideAttackRange();
        
        // Clear event references
        OnTargetSelected = null;
        OnTargetHovered = null;
        OnTargetingCanceled = null;
        OnTargetingStarted = null;
        OnTargetingStopped = null;
    }
}