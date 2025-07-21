using UnityEngine;
using System.Collections;
using System.Linq;

/// <summary>
/// Component that provides visual feedback for unit selection and hover states.
/// Uses material property blocks for efficient rendering and smooth transitions.
/// Designed to work with the ISelectable interface and SelectionManager system.
/// </summary>
public class SelectionHighlight : MonoBehaviour
{
    [Header("Highlight Materials")]
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private Material hoveredMaterial;
    [SerializeField] private Material originalMaterial;
    
    [Header("Highlight Configuration")]
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color hoveredColor = Color.white;
    [SerializeField] private float highlightIntensity = 1.5f;
    [SerializeField] private float rimWidth = 0.02f;
    
    [Header("Animation Settings")]
    [SerializeField] private bool enableSmoothTransitions = false; // Disabled for persistent highlighting
    [SerializeField] private float transitionDuration = 0.2f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool enablePulseEffect = false;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseIntensity = 0.3f;
    
    [Header("Performance Optimization")]
    [SerializeField] private bool usePropertyBlocks = false; // Disabled for direct material swapping
    [SerializeField] private bool cacheComponents = true;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogging = true;
    
    // Component references
    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;
    private ISelectable selectableComponent;
    
    // State tracking
    private HighlightState currentState = HighlightState.Normal;
    private HighlightState previousState = HighlightState.Normal;
    private bool isTransitioning = false;
    
    // Animation state
    private Coroutine transitionCoroutine;
    private float currentIntensity = 0f;
    private float targetIntensity = 0f;
    private float pulseTime = 0f;
    
    // Performance optimization
    private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int RimColorProperty = Shader.PropertyToID("_RimColor");
    private static readonly int RimPowerProperty = Shader.PropertyToID("_RimPower");
    
    /// <summary>
    /// Highlight state enumeration
    /// </summary>
    public enum HighlightState
    {
        Normal,
        Hovered,
        Selected
    }
    
    // Properties
    public HighlightState CurrentState => currentState;
    public bool IsTransitioning => isTransitioning;
    public Material OriginalMaterial => originalMaterial;
    
    void Awake()
    {
        InitializeComponent();
    }
    
    void Start()
    {
        SetupHighlightSystem();
        RegisterWithSelectable();
    }
    
    void Update()
    {
        if (enablePulseEffect && currentState == HighlightState.Selected)
        {
            UpdatePulseEffect();
        }
    }
    
    /// <summary>
    /// Initializes the highlight component
    /// </summary>
    private void InitializeComponent()
    {
        if (cacheComponents)
        {
            FindTargetRenderer();
            
            // Find ISelectable component (Unit implements this interface)
            selectableComponent = GetComponent<ISelectable>();
            
            if (enableDebugLogging)
            {
                Debug.Log($"SelectionHighlight on {gameObject.name}: Found Renderer = {targetRenderer != null}");
                if (targetRenderer != null)
                {
                    Debug.Log($"  - Renderer found on: {targetRenderer.gameObject.name}");
                    Debug.Log($"  - Renderer type: {targetRenderer.GetType().Name}");
                }
            }
        }
        
        if (usePropertyBlocks)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"SelectionHighlight initialized on {gameObject.name}");
            Debug.Log($"  - Renderer: {targetRenderer != null}");
            Debug.Log($"  - ISelectable: {selectableComponent != null}");
        }
    }
    
    /// <summary>
    /// Finds the target renderer component using multiple strategies
    /// </summary>
    private void FindTargetRenderer()
    {
        if (enableDebugLogging)
        {
            Debug.Log($"FindTargetRenderer: Searching for renderer on {gameObject.name}");
        }
        
        // Strategy 1: Direct component on same GameObject
        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"  Strategy 1 SUCCESS: Found {targetRenderer.GetType().Name} on self");
            }
            return;
        }
        
        // Strategy 2: Look for MeshRenderer specifically (most common)
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            targetRenderer = meshRenderer;
            if (enableDebugLogging)
            {
                Debug.Log($"  Strategy 2 SUCCESS: Found MeshRenderer on self");
            }
            return;
        }
        
        // Strategy 3: Look for SkinnedMeshRenderer (for animated units)
        SkinnedMeshRenderer skinnedRenderer = GetComponent<SkinnedMeshRenderer>();
        if (skinnedRenderer != null)
        {
            targetRenderer = skinnedRenderer;
            if (enableDebugLogging)
            {
                Debug.Log($"  Strategy 3 SUCCESS: Found SkinnedMeshRenderer on self");
            }
            return;
        }
        
        // Strategy 4: Search children (recursive)
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true); // Include inactive
        if (childRenderers.Length > 0)
        {
            // Prefer MeshRenderer over other types, and active over inactive
            targetRenderer = childRenderers.OrderBy(r => r.GetType() != typeof(MeshRenderer) ? 1 : 0)
                                          .ThenBy(r => r.gameObject.activeInHierarchy ? 0 : 1)
                                          .FirstOrDefault();
            
            if (targetRenderer != null)
            {
                if (enableDebugLogging)
                {
                    Debug.Log($"  Strategy 4 SUCCESS: Found {targetRenderer.GetType().Name} on child '{targetRenderer.gameObject.name}'");
                }
                return;
            }
        }
        
        // Strategy 5: Search parent hierarchy
        Transform currentParent = transform.parent;
        while (currentParent != null)
        {
            Renderer parentRenderer = currentParent.GetComponent<Renderer>();
            if (parentRenderer != null)
            {
                targetRenderer = parentRenderer;
                if (enableDebugLogging)
                {
                    Debug.Log($"  Strategy 5 SUCCESS: Found {targetRenderer.GetType().Name} on parent '{targetRenderer.gameObject.name}'");
                }
                return;
            }
            currentParent = currentParent.parent;
        }
        
        if (enableDebugLogging)
        {
            Debug.LogError($"  ALL STRATEGIES FAILED: No renderer found for {gameObject.name}");
            
            // Debug: List all components on this GameObject
            Component[] components = GetComponents<Component>();
            Debug.Log($"  Components on {gameObject.name}: {string.Join(", ", components.Select(c => c.GetType().Name))}");
            
            // Debug: List all renderers in the scene for comparison
            Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            Debug.Log($"  Total renderers in scene: {allRenderers.Length}");
        }
    }
    
    /// <summary>
    /// Sets up the highlight system with materials and colors
    /// </summary>
    private void SetupHighlightSystem()
    {
        // Try to find renderer if not already cached
        if (targetRenderer == null)
        {
            FindTargetRenderer();
        }
        
        if (targetRenderer == null)
        {
            Debug.LogError($"SelectionHighlight on {gameObject.name}: No Renderer found! Check unit hierarchy.");
            return;
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"SetupHighlightSystem on {gameObject.name}: Using renderer from {targetRenderer.gameObject.name}");
        }
        
        // Store original material if not already set
        if (originalMaterial == null)
        {
            originalMaterial = targetRenderer.material;
        }
        
        // Create highlight materials if not assigned
        CreateHighlightMaterialsIfNeeded();
        
        // Initialize property block with original values
        if (usePropertyBlocks)
        {
            targetRenderer.GetPropertyBlock(propertyBlock);
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"SelectionHighlight setup complete on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Registers event listeners with the ISelectable component
    /// </summary>
    private void RegisterWithSelectable()
    {
        // The Unit component implements ISelectable directly
        if (selectableComponent == null)
        {
            selectableComponent = GetComponent<ISelectable>();
        }
        
        if (selectableComponent != null)
        {
            selectableComponent.OnSelected += OnObjectSelected;
            selectableComponent.OnDeselected += OnObjectDeselected;
            selectableComponent.OnHoverChanged += OnHoverChanged;
            
            if (enableDebugLogging)
            {
                Debug.Log($"SelectionHighlight registered with ISelectable on {gameObject.name}");
            }
        }
        else
        {
            Debug.LogError($"SelectionHighlight on {gameObject.name}: No ISelectable component found!");
        }
    }
    
    /// <summary>
    /// Creates highlight materials if they don't exist
    /// </summary>
    private void CreateHighlightMaterialsIfNeeded()
    {
        if (selectedMaterial == null && originalMaterial != null)
        {
            selectedMaterial = CreateHighlightMaterial("Selected", selectedColor);
        }
        
        if (hoveredMaterial == null && originalMaterial != null)
        {
            hoveredMaterial = CreateHighlightMaterial("Hovered", hoveredColor);
        }
    }
    
    /// <summary>
    /// Creates a highlight material with the specified color
    /// </summary>
    private Material CreateHighlightMaterial(string suffix, Color color)
    {
        Material newMaterial = new Material(originalMaterial);
        newMaterial.name = $"{originalMaterial.name}_{suffix}";
        
        // Configure for highlighting
        newMaterial.EnableKeyword("_EMISSION");
        newMaterial.SetColor("_EmissionColor", color * highlightIntensity);
        
        // Add rim lighting if supported
        if (newMaterial.HasProperty("_RimColor"))
        {
            newMaterial.SetColor("_RimColor", color);
            newMaterial.SetFloat("_RimPower", 1f / rimWidth);
        }
        
        return newMaterial;
    }
    
    /// <summary>
    /// Called when the object is selected
    /// </summary>
    private void OnObjectSelected(ISelectable selectable)
    {
        SetHighlightState(HighlightState.Selected);
        
        if (enableDebugLogging)
        {
            Debug.Log($"SelectionHighlight: {gameObject.name} selected");
        }
    }
    
    /// <summary>
    /// Called when the object is deselected
    /// </summary>
    private void OnObjectDeselected(ISelectable selectable)
    {
        SetHighlightState(HighlightState.Normal);
        
        if (enableDebugLogging)
        {
            Debug.Log($"SelectionHighlight: {gameObject.name} deselected");
        }
    }
    
    /// <summary>
    /// Called when hover state changes
    /// </summary>
    private void OnHoverChanged(ISelectable selectable, bool isHovered)
    {
        if (currentState == HighlightState.Selected) return; // Don't change highlight when selected
        
        SetHighlightState(isHovered ? HighlightState.Hovered : HighlightState.Normal);
        
        if (enableDebugLogging)
        {
            Debug.Log($"SelectionHighlight: {gameObject.name} hover changed to {isHovered}");
        }
    }
    
    /// <summary>
    /// Sets the highlight state with optional animation
    /// </summary>
    public void SetHighlightState(HighlightState newState)
    {
        if (currentState == newState) return;
        
        previousState = currentState;
        currentState = newState;
        
        // Force immediate application - no transitions for persistent highlighting
        ApplyHighlightImmediate();
        
        if (enableDebugLogging)
        {
            Debug.Log($"SetHighlightState on {gameObject.name}: {previousState} -> {currentState} (Immediate)");
        }
    }
    
    /// <summary>
    /// Applies highlight immediately without animation
    /// </summary>
    private void ApplyHighlightImmediate()
    {
        switch (currentState)
        {
            case HighlightState.Normal:
                ApplyNormalState();
                break;
            case HighlightState.Hovered:
                ApplyHoveredState();
                break;
            case HighlightState.Selected:
                ApplySelectedState();
                break;
        }
    }
    
    /// <summary>
    /// Starts a smooth transition animation between states
    /// </summary>
    private void StartTransitionAnimation()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        
        transitionCoroutine = StartCoroutine(TransitionCoroutine());
    }
    
    /// <summary>
    /// Coroutine that handles smooth state transitions
    /// </summary>
    private IEnumerator TransitionCoroutine()
    {
        isTransitioning = true;
        
        float startIntensity = currentIntensity;
        targetIntensity = GetTargetIntensityForState(currentState);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / transitionDuration;
            float curveValue = transitionCurve.Evaluate(progress);
            
            currentIntensity = Mathf.Lerp(startIntensity, targetIntensity, curveValue);
            ApplyCurrentIntensity();
            
            yield return null;
        }
        
        currentIntensity = targetIntensity;
        ApplyCurrentIntensity();
        
        isTransitioning = false;
        transitionCoroutine = null;
    }
    
    /// <summary>
    /// Gets the target intensity for a given state
    /// </summary>
    private float GetTargetIntensityForState(HighlightState state)
    {
        switch (state)
        {
            case HighlightState.Normal:
                return 0f;
            case HighlightState.Hovered:
                return 0.5f;
            case HighlightState.Selected:
                return 1f;
            default:
                return 0f;
        }
    }
    
    /// <summary>
    /// Applies the current intensity value
    /// </summary>
    private void ApplyCurrentIntensity()
    {
        if (targetRenderer == null) 
        {
            if (enableDebugLogging)
            {
                Debug.LogError($"SelectionHighlight on {gameObject.name}: No renderer found!");
            }
            return;
        }
        
        Color highlightColor = GetColorForState(currentState);
        Color emissionColor = highlightColor * highlightIntensity * currentIntensity;
        
        if (enableDebugLogging)
        {
            Debug.Log($"ApplyCurrentIntensity on {gameObject.name}: State={currentState}, Intensity={currentIntensity}, Color={emissionColor}");
        }
        
        if (usePropertyBlocks)
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
            
            // Set both emission AND base color for maximum visibility
            propertyBlock.SetColor(EmissionColorProperty, emissionColor);
            
            // Also modify the base color for clear visual feedback
            Color baseColor = Color.white;
            if (currentIntensity > 0)
            {
                baseColor = Color.Lerp(Color.white, highlightColor, currentIntensity * 0.5f);
            }
            propertyBlock.SetColor(BaseColorProperty, baseColor);
            
            targetRenderer.SetPropertyBlock(propertyBlock);
            
            if (enableDebugLogging)
            {
                Debug.Log($"Applied property block - Emission: {emissionColor}, Base: {baseColor}");
            }
        }
        else
        {
            Material currentMaterial = GetMaterialForState(currentState);
            if (currentMaterial != null)
            {
                targetRenderer.material = currentMaterial;
                
                if (enableDebugLogging)
                {
                    Debug.Log($"Applied material: {currentMaterial.name}");
                }
            }
        }
    }
    
    /// <summary>
    /// Updates the pulse effect for selected objects
    /// </summary>
    private void UpdatePulseEffect()
    {
        pulseTime += Time.deltaTime * pulseSpeed;
        float pulseValue = (Mathf.Sin(pulseTime) + 1f) * 0.5f; // Normalize to 0-1
        float pulsedIntensity = targetIntensity + (pulseValue * pulseIntensity);
        
        Color highlightColor = GetColorForState(currentState);
        Color emissionColor = highlightColor * highlightIntensity * pulsedIntensity;
        
        if (usePropertyBlocks && targetRenderer != null)
        {
            propertyBlock.SetColor(EmissionColorProperty, emissionColor);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
    
    /// <summary>
    /// Applies normal (unselected) state
    /// </summary>
    private void ApplyNormalState()
    {
        if (targetRenderer == null) return;
        
        if (usePropertyBlocks)
        {
            propertyBlock.SetColor(EmissionColorProperty, Color.black);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
        else
        {
            targetRenderer.material = originalMaterial;
        }
        
        currentIntensity = 0f;
        targetIntensity = 0f;
    }
    
    /// <summary>
    /// Applies hovered state
    /// </summary>
    private void ApplyHoveredState()
    {
        if (targetRenderer == null) return;
        
        Color emissionColor = hoveredColor * highlightIntensity * 0.5f;
        
        if (usePropertyBlocks)
        {
            propertyBlock.SetColor(EmissionColorProperty, emissionColor);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
        else if (hoveredMaterial != null)
        {
            targetRenderer.material = hoveredMaterial;
        }
        
        currentIntensity = 0.5f;
        targetIntensity = 0.5f;
    }
    
    /// <summary>
    /// Applies selected state
    /// </summary>
    private void ApplySelectedState()
    {
        if (targetRenderer == null) return;
        
        Color emissionColor = selectedColor * highlightIntensity;
        
        if (usePropertyBlocks)
        {
            propertyBlock.SetColor(EmissionColorProperty, emissionColor);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
        else if (selectedMaterial != null)
        {
            targetRenderer.material = selectedMaterial;
        }
        
        currentIntensity = 1f;
        targetIntensity = 1f;
        pulseTime = 0f; // Reset pulse timer
    }
    
    /// <summary>
    /// Gets the color for a specific state
    /// </summary>
    private Color GetColorForState(HighlightState state)
    {
        switch (state)
        {
            case HighlightState.Hovered:
                return hoveredColor;
            case HighlightState.Selected:
                return selectedColor;
            default:
                return Color.black;
        }
    }
    
    /// <summary>
    /// Gets the material for a specific state
    /// </summary>
    private Material GetMaterialForState(HighlightState state)
    {
        switch (state)
        {
            case HighlightState.Hovered:
                return hoveredMaterial;
            case HighlightState.Selected:
                return selectedMaterial;
            default:
                return originalMaterial;
        }
    }
    
    /// <summary>
    /// Manually triggers a highlight state change (for testing)
    /// </summary>
    [ContextMenu("Test Selected State")]
    public void TestSelectedState()
    {
        SetHighlightState(HighlightState.Selected);
    }
    
    [ContextMenu("Test Hovered State")]
    public void TestHoveredState()
    {
        SetHighlightState(HighlightState.Hovered);
    }
    
    [ContextMenu("Test Normal State")]
    public void TestNormalState()
    {
        SetHighlightState(HighlightState.Normal);
    }
    
    /// <summary>
    /// Gets information about the current highlight state
    /// </summary>
    public string GetStateInfo()
    {
        return $"State: {currentState}, Intensity: {currentIntensity:F2}, Transitioning: {isTransitioning}";
    }
    
    /// <summary>
    /// Forces a refresh of the highlight system
    /// </summary>
    public void RefreshHighlight()
    {
        // Temporarily enable debug logging for troubleshooting
        bool originalDebugState = enableDebugLogging;
        enableDebugLogging = true;
        
        if (enableDebugLogging)
        {
            Debug.Log($"RefreshHighlight called on {gameObject.name}");
        }
        
        // Clear cached renderer to force re-search
        targetRenderer = null;
        
        SetupHighlightSystem();
        ApplyHighlightImmediate();
        
        // Restore original debug state
        enableDebugLogging = originalDebugState;
    }
    
    /// <summary>
    /// Unity Inspector GUI (only in editor builds)
    /// </summary>
    void OnValidate()
    {
        if (Application.isPlaying && targetRenderer != null)
        {
            ApplyHighlightImmediate();
        }
    }
    
    /// <summary>
    /// Cleanup when destroyed
    /// </summary>
    void OnDestroy()
    {
        // Stop any running coroutines
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        
        // Unregister from events
        if (selectableComponent != null)
        {
            selectableComponent.OnSelected -= OnObjectSelected;
            selectableComponent.OnDeselected -= OnObjectDeselected;
            selectableComponent.OnHoverChanged -= OnHoverChanged;
        }
        
        // Clear references
        targetRenderer = null;
        propertyBlock = null;
        selectableComponent = null;
    }
}