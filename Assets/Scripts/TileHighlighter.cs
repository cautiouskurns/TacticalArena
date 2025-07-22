using UnityEngine;
using System.Collections;

/// <summary>
/// Individual tile highlighter component that manages visual effects for grid tiles.
/// Handles different highlight states (valid move, invalid move, preview) with smooth transitions.
/// Provides efficient highlighting with material property blocks and performance optimization.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class TileHighlighter : MonoBehaviour
{
    [Header("Highlight Materials")]
    [SerializeField] private Material validMoveMaterial;
    [SerializeField] private Material invalidMoveMaterial;
    [SerializeField] private Material previewMaterial;
    [SerializeField] private Material originalMaterial;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeSpeed = 2.0f;
    [SerializeField] private bool enablePulsing = true;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 1.0f);
    [SerializeField] private bool enableGlowEffect = true;
    [SerializeField] private float glowIntensity = 2.0f;
    
    [Header("Performance Settings")]
    [SerializeField] private bool useObjectPooling = true;
    [SerializeField] private bool optimizeForDistance = true;
    [SerializeField] private float maxHighlightDistance = 15f;
    [SerializeField] private bool useMaterialPropertyBlocks = true;
    
    [Header("Visual Effects")]
    [SerializeField] private bool enableElevationEffect = false;
    [SerializeField] private float elevationHeight = 0.05f;
    [SerializeField] private bool enableRimLighting = true;
    [SerializeField] private Color rimColor = Color.white;
    [SerializeField] private float rimPower = 2.0f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogging = false;
    [SerializeField] private bool showHighlightBounds = false;
    
    // Component references
    private Renderer tileRenderer;
    private GridTile gridTile;
    private Camera mainCamera;
    
    // Highlight state
    private MovementPreviewSystem.HighlightType currentHighlightType;
    private bool isHighlighted = false;
    private bool isVisible = true;
    private float highlightStartTime;
    private Vector3 originalPosition;
    private Color originalEmissionColor;
    
    // Animation coroutines
    private Coroutine fadeCoroutine;
    private Coroutine pulseCoroutine;
    private Coroutine elevationCoroutine;
    
    // Material property optimization
    private MaterialPropertyBlock propertyBlock;
    private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int AlphaProperty = Shader.PropertyToID("_Alpha");
    
    // Events
    public System.Action<TileHighlighter, MovementPreviewSystem.HighlightType> OnHighlightChanged;
    public System.Action<TileHighlighter> OnHighlightCleared;
    
    // Properties
    public bool IsHighlighted => isHighlighted;
    public MovementPreviewSystem.HighlightType CurrentHighlightType => currentHighlightType;
    public GridTile GridTile => gridTile;
    
    void Awake()
    {
        InitializeTileHighlighter();
    }
    
    void Start()
    {
        FindComponentReferences();
        SetupInitialState();
    }
    
    void Update()
    {
        if (optimizeForDistance)
        {
            UpdateVisibilityBasedOnDistance();
        }
    }
    
    /// <summary>
    /// Initializes the tile highlighter
    /// </summary>
    private void InitializeTileHighlighter()
    {
        // Initialize material property block for efficient highlighting
        if (useMaterialPropertyBlocks)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"TileHighlighter initialized for {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Finds component references
    /// </summary>
    private void FindComponentReferences()
    {
        tileRenderer = GetComponent<Renderer>();
        if (tileRenderer == null)
        {
            Debug.LogError($"TileHighlighter: Renderer component not found on {gameObject.name}");
        }
        
        gridTile = GetComponent<GridTile>();
        if (gridTile == null)
        {
            Debug.LogWarning($"TileHighlighter: GridTile component not found on {gameObject.name}");
        }
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
    }
    
    /// <summary>
    /// Sets up initial state
    /// </summary>
    private void SetupInitialState()
    {
        originalPosition = transform.position;
        
        // Store original material if not set
        if (originalMaterial == null && tileRenderer != null)
        {
            originalMaterial = tileRenderer.material;
        }
        
        // Store original emission color
        if (tileRenderer != null && tileRenderer.material.HasProperty(EmissionColorProperty))
        {
            originalEmissionColor = tileRenderer.material.GetColor(EmissionColorProperty);
        }
    }
    
    /// <summary>
    /// Updates visibility based on distance from camera
    /// </summary>
    private void UpdateVisibilityBasedOnDistance()
    {
        if (mainCamera == null) return;
        
        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
        bool shouldBeVisible = distance <= maxHighlightDistance;
        
        if (shouldBeVisible != isVisible)
        {
            isVisible = shouldBeVisible;
            
            // Enable/disable visual effects based on distance
            if (tileRenderer != null)
            {
                tileRenderer.enabled = isVisible;
            }
            
            if (!isVisible && isHighlighted)
            {
                // Pause effects when not visible
                StopAllAnimations();
            }
            else if (isVisible && isHighlighted)
            {
                // Resume effects when visible again
                StartHighlightAnimations();
            }
        }
    }
    
    /// <summary>
    /// Sets highlight state and material for this tile
    /// </summary>
    public void SetHighlight(MovementPreviewSystem.HighlightType highlightType, Material highlightMaterial = null)
    {
        if (!isVisible && optimizeForDistance) return;
        
        currentHighlightType = highlightType;
        isHighlighted = true;
        highlightStartTime = Time.time;
        
        // Store original material if not already stored
        if (originalMaterial == null && tileRenderer != null)
        {
            originalMaterial = tileRenderer.material;
        }
        
        // Apply color directly to material - simple and reliable approach
        if (tileRenderer != null && tileRenderer.material != null)
        {
            switch (highlightType)
            {
                case MovementPreviewSystem.HighlightType.Valid:
                    tileRenderer.material.color = new Color(0f, 1f, 0f, 0.6f); // Semi-transparent green
                    break;
                case MovementPreviewSystem.HighlightType.Invalid:
                    tileRenderer.material.color = new Color(1f, 0f, 0f, 0.6f); // Semi-transparent red  
                    break;
                case MovementPreviewSystem.HighlightType.Preview:
                    tileRenderer.material.color = new Color(1f, 1f, 0f, 0.6f); // Semi-transparent yellow
                    break;
            }
        }
        
        // Start simple animations if enabled
        if (enablePulsing)
        {
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(SimplePulseHighlight());
        }
        
        OnHighlightChanged?.Invoke(this, highlightType);
        
        if (enableDebugLogging)
        {
            Debug.Log($"TileHighlighter: Set highlight {highlightType} on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Clears highlight from this tile
    /// </summary>
    public void ClearHighlight()
    {
        if (!isHighlighted) return;
        
        isHighlighted = false;
        
        // Stop animations
        StopAllAnimations();
        
        // Restore original material color directly
        if (tileRenderer != null && originalMaterial != null)
        {
            tileRenderer.material.color = originalMaterial.color;
        }
        
        // Reset position to original
        transform.position = originalPosition;
        
        OnHighlightCleared?.Invoke(this);
        
        if (enableDebugLogging)
        {
            Debug.Log($"TileHighlighter: Cleared highlight on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Simple method to force immediate highlight refresh
    /// </summary>
    public void RefreshHighlight()
    {
        if (isHighlighted)
        {
            // Re-apply the current highlight
            SetHighlight(currentHighlightType);
        }
    }
    
    /// <summary>
    /// Starts highlight animations
    /// </summary>
    private void StartHighlightAnimations()
    {
        // Simple fade in - no coroutine needed with direct color
        
        // Pulsing effect
        if (enablePulsing)
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
            }
            pulseCoroutine = StartCoroutine(SimplePulseHighlight());
        }
        
        // Elevation effect
        if (enableElevationEffect)
        {
            if (elevationCoroutine != null)
            {
                StopCoroutine(elevationCoroutine);
            }
            elevationCoroutine = StartCoroutine(ElevateHighlight());
        }
    }
    
    /// <summary>
    /// Stops all highlight animations
    /// </summary>
    private void StopAllAnimations()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        if (elevationCoroutine != null)
        {
            StopCoroutine(elevationCoroutine);
            elevationCoroutine = null;
        }
    }
    
    
    /// <summary>
    /// Simple pulse effect using direct color manipulation
    /// </summary>
    private IEnumerator SimplePulseHighlight()
    {
        if (tileRenderer == null || tileRenderer.material == null) yield break;
        
        Color baseColor = tileRenderer.material.color;
        
        while (isHighlighted)
        {
            float time = Time.time * pulseSpeed;
            float pulseValue = (Mathf.Sin(time) + 1f) * 0.5f; // 0 to 1
            float intensity = Mathf.Lerp(0.4f, 1.0f, pulseValue);
            
            Color pulsedColor = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * intensity);
            tileRenderer.material.color = pulsedColor;
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Elevates the highlight
    /// </summary>
    private IEnumerator ElevateHighlight()
    {
        float elapsed = 0f;
        float duration = 0.3f;
        
        Vector3 targetPosition = originalPosition + Vector3.up * elevationHeight;
        
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            transform.position = Vector3.Lerp(originalPosition, targetPosition, progress);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = targetPosition;
    }
    
    
    /// <summary>
    /// Gets tile highlighter information for debugging
    /// </summary>
    public string GetHighlighterInfo()
    {
        return $"Tile: {gameObject.name}, Highlighted: {isHighlighted}, Type: {currentHighlightType}, Visible: {isVisible}";
    }
    
    /// <summary>
    /// Unity Gizmos for debug visualization
    /// </summary>
    void OnDrawGizmos()
    {
        if (!enableDebugLogging || !showHighlightBounds) return;
        
        if (isHighlighted)
        {
            // Set color based on highlight type
            switch (currentHighlightType)
            {
                case MovementPreviewSystem.HighlightType.Valid:
                    Gizmos.color = Color.green;
                    break;
                case MovementPreviewSystem.HighlightType.Invalid:
                    Gizmos.color = Color.red;
                    break;
                case MovementPreviewSystem.HighlightType.Preview:
                    Gizmos.color = Color.yellow;
                    break;
                default:
                    Gizmos.color = Color.white;
                    break;
            }
            
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.1f, Vector3.one * 0.9f);
        }
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Stop all animations
        StopAllAnimations();
        
        // Clear events
        OnHighlightChanged = null;
        OnHighlightCleared = null;
        
        // Reset position
        if (transform != null)
        {
            transform.position = originalPosition;
        }
    }
}