using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Visual feedback management system for tactical battlefield interactions.
/// Handles hover effects, selection feedback, and dynamic visual responses.
/// </summary>
public class VisualFeedbackManager : MonoBehaviour
{
    [Header("Visual Feedback Configuration")]
    [SerializeField] private bool enableVisualFeedback = true;
    [SerializeField] private bool enableHoverEffects = true;
    [SerializeField] private bool enableSelectionEffects = true;
    [SerializeField] private float feedbackIntensity = 1.0f;
    [SerializeField] private float feedbackSpeed = 2.0f;
    
    [Header("Hover Effect Settings")]
    [SerializeField] private AnimationCurve hoverIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float hoverAnimationDuration = 0.5f;
    [SerializeField] private Color hoverGlowColor = Color.yellow;
    [SerializeField] private float hoverElevation = 0.02f;
    
    [Header("Selection Effect Settings")]
    [SerializeField] private AnimationCurve selectionPulseCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [SerializeField] private float selectionPulseDuration = 1.0f;
    [SerializeField] private Color selectionGlowColor = Color.green;
    [SerializeField] private float selectionElevation = 0.05f;
    [SerializeField] private bool enableSelectionPulse = true;
    
    [Header("Transition Settings")]
    [SerializeField] private float transitionSpeed = 5.0f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool enableSmoothTransitions = true;
    
    [Header("Performance Settings")]
    [SerializeField] private int maxConcurrentEffects = 16;
    [SerializeField] private float effectPoolingThreshold = 0.1f;
    [SerializeField] private bool useObjectPooling = true;
    
    [Header("Audio Integration")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip selectionSound;
    [SerializeField] private float audioVolume = 0.3f;
    [SerializeField] private bool enableAudioFeedback = false;
    
    // Component references
    private GridManager gridManager;
    private MaterialManager materialManager;
    private Camera mainCamera;
    private AudioSource audioSource;
    
    // Feedback tracking
    private Dictionary<GridCoordinate, TileFeedbackState> activeFeedbackStates;
    private Dictionary<GridCoordinate, Coroutine> activeAnimations;
    private Dictionary<GridCoordinate, Vector3> originalTilePositions;
    private Queue<GameObject> effectObjectPool;
    
    // Current interaction state
    private GridCoordinate currentHoveredTile = GridCoordinate.Invalid;
    private GridCoordinate currentSelectedTile = GridCoordinate.Invalid;
    private List<GridCoordinate> highlightedTiles = new List<GridCoordinate>();
    
    // Performance tracking
    private int activeEffectCount = 0;
    private float lastUpdateTime = 0f;
    
    // Events for feedback system
    public System.Action<GridCoordinate> OnTileHoverStart;
    public System.Action<GridCoordinate> OnTileHoverEnd;
    public System.Action<GridCoordinate> OnTileSelectionStart;
    public System.Action<GridCoordinate> OnTileSelectionEnd;
    
    // Public properties
    public bool VisualFeedbackEnabled => enableVisualFeedback;
    public bool HoverEffectsEnabled => enableHoverEffects;
    public bool SelectionEffectsEnabled => enableSelectionEffects;
    public float FeedbackIntensity => feedbackIntensity;
    public GridCoordinate CurrentHoveredTile => currentHoveredTile;
    public GridCoordinate CurrentSelectedTile => currentSelectedTile;
    public int ActiveEffectCount => activeEffectCount;
    
    void Awake()
    {
        InitializeFeedbackSystem();
    }
    
    void Start()
    {
        FindManagerReferences();
        RegisterEventListeners();
        SetupAudioFeedback();
    }
    
    void Update()
    {
        if (enableVisualFeedback && Time.time - lastUpdateTime >= 1f / 60f)
        {
            UpdateVisualFeedback();
            lastUpdateTime = Time.time;
        }
    }
    
    void OnDestroy()
    {
        UnregisterEventListeners();
        StopAllFeedbackEffects();
    }
    
    /// <summary>
    /// Initializes the visual feedback system
    /// </summary>
    private void InitializeFeedbackSystem()
    {
        activeFeedbackStates = new Dictionary<GridCoordinate, TileFeedbackState>();
        activeAnimations = new Dictionary<GridCoordinate, Coroutine>();
        originalTilePositions = new Dictionary<GridCoordinate, Vector3>();
        effectObjectPool = new Queue<GameObject>();
        
        // Initialize object pool if enabled
        if (useObjectPooling)
        {
            CreateEffectObjectPool();
        }
        
        Debug.Log("VisualFeedbackManager: System initialized");
    }
    
    /// <summary>
    /// Creates an object pool for visual effects
    /// </summary>
    private void CreateEffectObjectPool()
    {
        GameObject poolParent = new GameObject("Effect Pool");
        poolParent.transform.SetParent(transform);
        poolParent.SetActive(false);
        
        for (int i = 0; i < maxConcurrentEffects; i++)
        {
            GameObject effectObj = CreateEffectObject();
            effectObj.transform.SetParent(poolParent.transform);
            effectObj.SetActive(false);
            effectObjectPool.Enqueue(effectObj);
        }
    }
    
    /// <summary>
    /// Creates a reusable effect object
    /// </summary>
    private GameObject CreateEffectObject()
    {
        GameObject effectObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        effectObj.name = "FeedbackEffect";
        
        // Configure for visual feedback
        Renderer renderer = effectObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material effectMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            effectMaterial.color = new Color(1f, 1f, 1f, 0.5f);
            renderer.material = effectMaterial;
        }
        
        // Remove collider
        Collider collider = effectObj.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        return effectObj;
    }
    
    /// <summary>
    /// Finds references to other manager components
    /// </summary>
    private void FindManagerReferences()
    {
        GameObject gridSystem = GameObject.Find("Grid System");
        if (gridSystem != null)
        {
            gridManager = gridSystem.GetComponent<GridManager>();
        }
        
        GameObject materialSystem = GameObject.Find("Material Manager");
        if (materialSystem != null)
        {
            materialManager = materialSystem.GetComponent<MaterialManager>();
        }
        
        mainCamera = Camera.main;
        
        if (gridManager == null)
        {
            Debug.LogWarning("VisualFeedbackManager: GridManager not found");
        }
        
        if (materialManager == null)
        {
            Debug.LogWarning("VisualFeedbackManager: MaterialManager not found");
        }
    }
    
    /// <summary>
    /// Registers event listeners for grid interactions
    /// </summary>
    private void RegisterEventListeners()
    {
        if (gridManager != null)
        {
            gridManager.OnTileHovered += HandleTileHovered;
            gridManager.OnTileSelected += HandleTileSelected;
            gridManager.OnTileDeselected += HandleTileDeselected;
        }
    }
    
    /// <summary>
    /// Unregisters event listeners
    /// </summary>
    private void UnregisterEventListeners()
    {
        if (gridManager != null)
        {
            gridManager.OnTileHovered -= HandleTileHovered;
            gridManager.OnTileSelected -= HandleTileSelected;
            gridManager.OnTileDeselected -= HandleTileDeselected;
        }
    }
    
    /// <summary>
    /// Sets up audio feedback components
    /// </summary>
    private void SetupAudioFeedback()
    {
        if (enableAudioFeedback)
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            audioSource.volume = audioVolume;
            audioSource.playOnAwake = false;
        }
    }
    
    /// <summary>
    /// Handles tile hover events
    /// </summary>
    private void HandleTileHovered(GridCoordinate coordinate)
    {
        if (!enableVisualFeedback || !enableHoverEffects) return;
        
        // End previous hover effect
        if (currentHoveredTile.IsValid && currentHoveredTile != coordinate)
        {
            EndTileHoverEffect(currentHoveredTile);
        }
        
        // Start new hover effect
        if (coordinate.IsValid)
        {
            currentHoveredTile = coordinate;
            StartTileHoverEffect(coordinate);
            OnTileHoverStart?.Invoke(coordinate);
            
            PlayAudioFeedback(hoverSound);
        }
    }
    
    /// <summary>
    /// Handles tile selection events
    /// </summary>
    private void HandleTileSelected(GridCoordinate coordinate)
    {
        if (!enableVisualFeedback || !enableSelectionEffects) return;
        
        // End previous selection effect
        if (currentSelectedTile.IsValid)
        {
            EndTileSelectionEffect(currentSelectedTile);
        }
        
        // Start new selection effect
        if (coordinate.IsValid)
        {
            currentSelectedTile = coordinate;
            StartTileSelectionEffect(coordinate);
            OnTileSelectionStart?.Invoke(coordinate);
            
            PlayAudioFeedback(selectionSound);
        }
    }
    
    /// <summary>
    /// Handles tile deselection events
    /// </summary>
    private void HandleTileDeselected(GridCoordinate coordinate)
    {
        if (coordinate.IsValid && coordinate == currentSelectedTile)
        {
            EndTileSelectionEffect(coordinate);
            currentSelectedTile = GridCoordinate.Invalid;
            OnTileSelectionEnd?.Invoke(coordinate);
        }
    }
    
    /// <summary>
    /// Stores the original position of a tile for later restoration
    /// </summary>
    private void StoreOriginalTilePosition(GridCoordinate coordinate, GridTile tile)
    {
        if (!originalTilePositions.ContainsKey(coordinate))
        {
            originalTilePositions[coordinate] = tile.transform.position;
        }
    }
    
    /// <summary>
    /// Restores a tile to its original position
    /// </summary>
    private void RestoreTilePosition(GridCoordinate coordinate, GridTile tile)
    {
        if (originalTilePositions.ContainsKey(coordinate))
        {
            tile.transform.position = originalTilePositions[coordinate];
        }
    }
    
    /// <summary>
    /// Starts visual hover effect for a tile
    /// </summary>
    private void StartTileHoverEffect(GridCoordinate coordinate)
    {
        if (activeEffectCount >= maxConcurrentEffects) return;
        
        GridTile tile = gridManager?.GetTile(coordinate);
        if (tile == null) return;
        
        // Store original position before any animation
        StoreOriginalTilePosition(coordinate, tile);
        
        // Restore to original position before starting new animation
        RestoreTilePosition(coordinate, tile);
        
        // Update material if material manager is available
        if (materialManager != null)
        {
            materialManager.ApplyTileMaterial(tile, GridTileState.Hovered);
        }
        
        // Start hover animation
        if (activeAnimations.ContainsKey(coordinate))
        {
            StopCoroutine(activeAnimations[coordinate]);
        }
        
        Coroutine hoverAnimation = StartCoroutine(AnimateHoverEffect(coordinate, tile));
        activeAnimations[coordinate] = hoverAnimation;
        
        // Track feedback state
        activeFeedbackStates[coordinate] = new TileFeedbackState
        {
            coordinate = coordinate,
            effectType = FeedbackEffectType.Hover,
            startTime = Time.time,
            isActive = true
        };
        
        activeEffectCount++;
    }
    
    /// <summary>
    /// Ends visual hover effect for a tile
    /// </summary>
    private void EndTileHoverEffect(GridCoordinate coordinate)
    {
        GridTile tile = gridManager?.GetTile(coordinate);
        if (tile == null) return;
        
        // Restore tile to original position
        RestoreTilePosition(coordinate, tile);
        
        // Restore default material
        if (materialManager != null)
        {
            GridTileState targetState = (coordinate == currentSelectedTile) ? GridTileState.Selected : GridTileState.Normal;
            materialManager.ApplyTileMaterial(tile, targetState);
        }
        
        // Stop hover animation
        if (activeAnimations.ContainsKey(coordinate))
        {
            StopCoroutine(activeAnimations[coordinate]);
            activeAnimations.Remove(coordinate);
        }
        
        // Update feedback state
        if (activeFeedbackStates.ContainsKey(coordinate))
        {
            TileFeedbackState state = activeFeedbackStates[coordinate];
            state.isActive = false;
            activeFeedbackStates[coordinate] = state;
            activeFeedbackStates.Remove(coordinate);
        }
        
        activeEffectCount--;
        OnTileHoverEnd?.Invoke(coordinate);
    }
    
    /// <summary>
    /// Starts visual selection effect for a tile
    /// </summary>
    private void StartTileSelectionEffect(GridCoordinate coordinate)
    {
        if (activeEffectCount >= maxConcurrentEffects) return;
        
        GridTile tile = gridManager?.GetTile(coordinate);
        if (tile == null) return;
        
        // Store original position before any animation
        StoreOriginalTilePosition(coordinate, tile);
        
        // Restore to original position before starting new animation
        RestoreTilePosition(coordinate, tile);
        
        // Update material
        if (materialManager != null)
        {
            materialManager.ApplyTileMaterial(tile, GridTileState.Selected);
        }
        
        // Start selection animation
        if (activeAnimations.ContainsKey(coordinate))
        {
            StopCoroutine(activeAnimations[coordinate]);
        }
        
        Coroutine selectionAnimation = StartCoroutine(AnimateSelectionEffect(coordinate, tile));
        activeAnimations[coordinate] = selectionAnimation;
        
        // Track feedback state
        activeFeedbackStates[coordinate] = new TileFeedbackState
        {
            coordinate = coordinate,
            effectType = FeedbackEffectType.Selection,
            startTime = Time.time,
            isActive = true
        };
        
        activeEffectCount++;
    }
    
    /// <summary>
    /// Ends visual selection effect for a tile
    /// </summary>
    private void EndTileSelectionEffect(GridCoordinate coordinate)
    {
        GridTile tile = gridManager?.GetTile(coordinate);
        if (tile == null) return;
        
        // Restore tile to original position
        RestoreTilePosition(coordinate, tile);
        
        // Restore appropriate material
        if (materialManager != null)
        {
            GridTileState targetState = (coordinate == currentHoveredTile) ? GridTileState.Hovered : GridTileState.Normal;
            materialManager.ApplyTileMaterial(tile, targetState);
        }
        
        // Stop selection animation
        if (activeAnimations.ContainsKey(coordinate))
        {
            StopCoroutine(activeAnimations[coordinate]);
            activeAnimations.Remove(coordinate);
        }
        
        // Update feedback state
        if (activeFeedbackStates.ContainsKey(coordinate))
        {
            TileFeedbackState state = activeFeedbackStates[coordinate];
            state.isActive = false;
            activeFeedbackStates[coordinate] = state;
            activeFeedbackStates.Remove(coordinate);
        }
        
        activeEffectCount--;
    }
    
    /// <summary>
    /// Animates hover effect for a tile
    /// </summary>
    private IEnumerator AnimateHoverEffect(GridCoordinate coordinate, GridTile tile)
    {
        Vector3 originalPosition = tile.transform.position;
        Vector3 targetPosition = originalPosition + Vector3.up * hoverElevation;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < hoverAnimationDuration && enableHoverEffects)
        {
            float normalizedTime = elapsedTime / hoverAnimationDuration;
            float curveValue = hoverIntensityCurve.Evaluate(normalizedTime);
            
            // Animate position
            if (enableSmoothTransitions)
            {
                Vector3 currentPosition = Vector3.Lerp(originalPosition, targetPosition, curveValue * feedbackIntensity);
                tile.transform.position = currentPosition;
            }
            
            elapsedTime += Time.deltaTime * feedbackSpeed;
            yield return null;
        }
        
        // Maintain hover state
        while (coordinate == currentHoveredTile && enableHoverEffects)
        {
            // Optional: Add continuous hover effects here
            yield return null;
        }
        
        // Return to original position
        if (enableSmoothTransitions)
        {
            elapsedTime = 0f;
            Vector3 currentPos = tile.transform.position;
            
            while (elapsedTime < hoverAnimationDuration * 0.5f)
            {
                float normalizedTime = elapsedTime / (hoverAnimationDuration * 0.5f);
                tile.transform.position = Vector3.Lerp(currentPos, originalPosition, normalizedTime);
                
                elapsedTime += Time.deltaTime * feedbackSpeed;
                yield return null;
            }
        }
        
        tile.transform.position = originalPosition;
        activeAnimations.Remove(coordinate);
    }
    
    /// <summary>
    /// Animates selection effect for a tile
    /// </summary>
    private IEnumerator AnimateSelectionEffect(GridCoordinate coordinate, GridTile tile)
    {
        Vector3 originalPosition = tile.transform.position;
        Vector3 targetPosition = originalPosition + Vector3.up * selectionElevation;
        
        // Initial elevation animation
        float elapsedTime = 0f;
        while (elapsedTime < selectionPulseDuration * 0.2f)
        {
            float normalizedTime = elapsedTime / (selectionPulseDuration * 0.2f);
            Vector3 currentPosition = Vector3.Lerp(originalPosition, targetPosition, normalizedTime * feedbackIntensity);
            tile.transform.position = currentPosition;
            
            elapsedTime += Time.deltaTime * feedbackSpeed;
            yield return null;
        }
        
        // Continuous pulse effect
        while (coordinate == currentSelectedTile && enableSelectionEffects)
        {
            if (enableSelectionPulse)
            {
                elapsedTime = 0f;
                
                while (elapsedTime < selectionPulseDuration && coordinate == currentSelectedTile)
                {
                    float normalizedTime = elapsedTime / selectionPulseDuration;
                    float pulseValue = selectionPulseCurve.Evaluate(normalizedTime);
                    
                    // Apply pulse effect (could be used for material color changes)
                    // Currently maintains elevated position
                    
                    elapsedTime += Time.deltaTime * feedbackSpeed;
                    yield return null;
                }
            }
            else
            {
                yield return null;
            }
        }
        
        // Return to original position
        elapsedTime = 0f;
        Vector3 currentPos = tile.transform.position;
        
        while (elapsedTime < selectionPulseDuration * 0.2f)
        {
            float normalizedTime = elapsedTime / (selectionPulseDuration * 0.2f);
            tile.transform.position = Vector3.Lerp(currentPos, originalPosition, normalizedTime);
            
            elapsedTime += Time.deltaTime * feedbackSpeed;
            yield return null;
        }
        
        tile.transform.position = originalPosition;
        activeAnimations.Remove(coordinate);
    }
    
    /// <summary>
    /// Updates visual feedback system
    /// </summary>
    private void UpdateVisualFeedback()
    {
        // Clean up completed effects
        CleanupCompletedEffects();
        
        // Update active feedback states
        UpdateActiveFeedbackStates();
    }
    
    /// <summary>
    /// Cleans up completed or invalid effects
    /// </summary>
    private void CleanupCompletedEffects()
    {
        List<GridCoordinate> coordinatesToRemove = new List<GridCoordinate>();
        
        foreach (var kvp in activeFeedbackStates)
        {
            TileFeedbackState state = kvp.Value;
            
            if (!state.isActive || Time.time - state.startTime > 30f) // Timeout after 30 seconds
            {
                coordinatesToRemove.Add(kvp.Key);
            }
        }
        
        foreach (GridCoordinate coord in coordinatesToRemove)
        {
            if (activeFeedbackStates.ContainsKey(coord))
            {
                activeFeedbackStates.Remove(coord);
                activeEffectCount--;
            }
            
            if (activeAnimations.ContainsKey(coord))
            {
                StopCoroutine(activeAnimations[coord]);
                activeAnimations.Remove(coord);
            }
        }
    }
    
    /// <summary>
    /// Updates active feedback states
    /// </summary>
    private void UpdateActiveFeedbackStates()
    {
        // Validate current hover and selection states
        if (currentHoveredTile.IsValid && !activeFeedbackStates.ContainsKey(currentHoveredTile))
        {
            currentHoveredTile = GridCoordinate.Invalid;
        }
        
        if (currentSelectedTile.IsValid && !activeFeedbackStates.ContainsKey(currentSelectedTile))
        {
            currentSelectedTile = GridCoordinate.Invalid;
        }
    }
    
    /// <summary>
    /// Plays audio feedback for interactions
    /// </summary>
    private void PlayAudioFeedback(AudioClip clip)
    {
        if (enableAudioFeedback && audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, audioVolume);
        }
    }
    
    /// <summary>
    /// Highlights multiple tiles for tactical feedback
    /// </summary>
    public void HighlightTiles(List<GridCoordinate> coordinates, Color highlightColor)
    {
        if (!enableVisualFeedback) return;
        
        // Clear previous highlights
        ClearHighlightedTiles();
        
        // Apply new highlights
        foreach (GridCoordinate coord in coordinates)
        {
            if (gridManager != null && gridManager.IsValidCoordinate(coord))
            {
                highlightedTiles.Add(coord);
                GridTile tile = gridManager.GetTile(coord);
                if (tile != null && materialManager != null)
                {
                    materialManager.ApplyTileMaterial(tile, GridTileState.Highlighted);
                }
            }
        }
    }
    
    /// <summary>
    /// Clears all highlighted tiles
    /// </summary>
    public void ClearHighlightedTiles()
    {
        foreach (GridCoordinate coord in highlightedTiles)
        {
            GridTile tile = gridManager?.GetTile(coord);
            if (tile != null && materialManager != null)
            {
                GridTileState targetState = GridTileState.Normal;
                
                if (coord == currentSelectedTile)
                    targetState = GridTileState.Selected;
                else if (coord == currentHoveredTile)
                    targetState = GridTileState.Hovered;
                
                materialManager.ApplyTileMaterial(tile, targetState);
            }
        }
        
        highlightedTiles.Clear();
    }
    
    /// <summary>
    /// Stops all active feedback effects
    /// </summary>
    public void StopAllFeedbackEffects()
    {
        // Stop all animations
        foreach (var animation in activeAnimations.Values)
        {
            if (animation != null)
            {
                StopCoroutine(animation);
            }
        }
        
        // Restore all tiles to their original positions
        foreach (var kvp in originalTilePositions)
        {
            GridCoordinate coordinate = kvp.Key;
            Vector3 originalPosition = kvp.Value;
            
            GridTile tile = gridManager?.GetTile(coordinate);
            if (tile != null)
            {
                tile.transform.position = originalPosition;
            }
        }
        
        activeAnimations.Clear();
        activeFeedbackStates.Clear();
        originalTilePositions.Clear();
        activeEffectCount = 0;
        
        ClearHighlightedTiles();
        currentHoveredTile = GridCoordinate.Invalid;
        currentSelectedTile = GridCoordinate.Invalid;
        
        Debug.Log("VisualFeedbackManager: All effects stopped and positions restored");
    }
    
    /// <summary>
    /// Gets feedback system status information
    /// </summary>
    public FeedbackSystemInfo GetSystemInfo()
    {
        return new FeedbackSystemInfo
        {
            visualFeedbackEnabled = enableVisualFeedback,
            hoverEffectsEnabled = enableHoverEffects,
            selectionEffectsEnabled = enableSelectionEffects,
            activeEffectCount = activeEffectCount,
            maxConcurrentEffects = maxConcurrentEffects,
            currentHoveredTile = currentHoveredTile,
            currentSelectedTile = currentSelectedTile,
            highlightedTileCount = highlightedTiles.Count
        };
    }
}

/// <summary>
/// Structure for tracking individual tile feedback states
/// </summary>
[System.Serializable]
public struct TileFeedbackState
{
    public GridCoordinate coordinate;
    public FeedbackEffectType effectType;
    public float startTime;
    public bool isActive;
    public float intensity;
}

/// <summary>
/// Enumeration for different types of feedback effects
/// </summary>
public enum FeedbackEffectType
{
    None,
    Hover,
    Selection,
    Highlight,
    Animation
}

/// <summary>
/// Information structure for feedback system status
/// </summary>
[System.Serializable]
public struct FeedbackSystemInfo
{
    public bool visualFeedbackEnabled;
    public bool hoverEffectsEnabled;
    public bool selectionEffectsEnabled;
    public int activeEffectCount;
    public int maxConcurrentEffects;
    public GridCoordinate currentHoveredTile;
    public GridCoordinate currentSelectedTile;
    public int highlightedTileCount;
    
    public override string ToString()
    {
        return $"Feedback System - Active: {activeEffectCount}/{maxConcurrentEffects}, Hover: {currentHoveredTile}, Selected: {currentSelectedTile}";
    }
}