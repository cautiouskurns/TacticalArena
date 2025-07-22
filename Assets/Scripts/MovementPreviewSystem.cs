using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Movement preview system that shows valid/invalid move highlights when units are selected.
/// Provides real-time visual feedback for tactical movement decisions with performance optimization.
/// Integrates with SelectionManager and MovementValidator for accurate move validation.
/// </summary>
public class MovementPreviewSystem : MonoBehaviour
{
    [Header("Preview Configuration")]
    [SerializeField] private bool enablePreview = true;
    [SerializeField] private float previewDelay = 0.2f;
    [SerializeField] private bool showInvalidMoves = true;
    [SerializeField] private bool showValidMovesOnly = false;
    
    [Header("Visual Materials")]
    [SerializeField] private Material validMoveMaterial;
    [SerializeField] private Material invalidMoveMaterial;
    [SerializeField] private Material previewMaterial;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeSpeed = 2.0f;
    [SerializeField] private bool enablePulsing = true;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 1.0f);
    
    [Header("Performance Settings")]
    [SerializeField] private int maxConcurrentPreviews = 16;
    [SerializeField] private bool useObjectPooling = true;
    [SerializeField] private bool optimizeForPerformance = true;
    [SerializeField] private float updateFrequency = 0.1f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogging = true;
    [SerializeField] private bool showPreviewBounds = false;
    
    // System references
    private SelectionManager selectionManager;
    private MovementManager movementManager;
    private MovementValidator movementValidator;
    private GridManager gridManager;
    
    // Preview state
    private IMovable currentSelectedUnit;
    private List<TileHighlighter> activeHighlights = new List<TileHighlighter>();
    private Dictionary<Vector2Int, HighlightType> currentPreviews = new Dictionary<Vector2Int, HighlightType>();
    private Coroutine previewUpdateCoroutine;
    
    // Object pooling
    private Queue<GameObject> highlightPool = new Queue<GameObject>();
    private List<GameObject> activeHighlightObjects = new List<GameObject>();
    
    // Events
    public System.Action<Vector2Int, HighlightType> OnTileHighlighted;
    public System.Action<Vector2Int> OnTileUnhighlighted;
    public System.Action<IMovable> OnPreviewStarted;
    public System.Action<IMovable> OnPreviewStopped;
    
    /// <summary>
    /// Preview highlight types
    /// </summary>
    public enum HighlightType
    {
        Valid,
        Invalid,
        Preview
    }
    
    void Awake()
    {
        InitializePreviewSystem();
    }
    
    void Start()
    {
        FindSystemReferences();
        SetupEventListeners();
        InitializeObjectPool();
    }
    
    void Update()
    {
        if (enablePreview)
        {
            UpdatePreviewSystem();
        }
    }
    
    /// <summary>
    /// Initializes the movement preview system
    /// </summary>
    private void InitializePreviewSystem()
    {
        if (enableDebugLogging)
        {
            Debug.Log("MovementPreviewSystem initialized");
        }
    }
    
    /// <summary>
    /// Finds references to other systems
    /// </summary>
    private void FindSystemReferences()
    {
        selectionManager = FindFirstObjectByType<SelectionManager>();
        if (selectionManager == null)
        {
            Debug.LogError("MovementPreviewSystem: SelectionManager not found!");
        }
        
        movementManager = GetComponent<MovementManager>();
        if (movementManager == null)
        {
            Debug.LogError("MovementPreviewSystem: MovementManager not found on same GameObject!");
        }
        
        movementValidator = GetComponent<MovementValidator>();
        if (movementValidator == null)
        {
            Debug.LogError("MovementPreviewSystem: MovementValidator not found on same GameObject!");
        }
        
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("MovementPreviewSystem: GridManager not found!");
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"MovementPreviewSystem found references - Selection: {selectionManager != null}, Movement: {movementManager != null}, Grid: {gridManager != null}");
        }
    }
    
    /// <summary>
    /// Sets up event listeners for selection changes
    /// </summary>
    private void SetupEventListeners()
    {
        if (selectionManager != null)
        {
            selectionManager.OnObjectSelected += OnUnitSelected;
            selectionManager.OnObjectDeselected += OnUnitDeselected;
            selectionManager.OnSelectionChanged += OnSelectionChanged;
        }
        
        if (movementManager != null)
        {
            movementManager.OnMovementStarted += OnMovementStarted;
            movementManager.OnMovementCompleted += OnMovementCompleted;
        }
    }
    
    /// <summary>
    /// Initializes object pool for performance optimization
    /// </summary>
    private void InitializeObjectPool()
    {
        if (!useObjectPooling) return;
        
        // Pre-create highlight objects for pooling
        for (int i = 0; i < maxConcurrentPreviews; i++)
        {
            GameObject highlightObj = CreateHighlightObject();
            highlightObj.SetActive(false);
            highlightPool.Enqueue(highlightObj);
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"MovementPreviewSystem: Initialized object pool with {maxConcurrentPreviews} highlight objects");
        }
    }
    
    /// <summary>
    /// Creates a highlight object for the pool
    /// </summary>
    private GameObject CreateHighlightObject()
    {
        GameObject highlightObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        highlightObj.name = "MovementHighlight";
        highlightObj.transform.SetParent(transform);
        
        // Configure for highlighting - match grid square size
        highlightObj.transform.localScale = Vector3.one * 0.1f;
        
        // Remove collider to avoid interference
        Collider collider = highlightObj.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        // Set up transparent material
        Renderer renderer = highlightObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create a new material using URP/Lit shader that supports transparency
            Material transparentMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            transparentMaterial.SetFloat("_Surface", 1); // Set to Transparent
            transparentMaterial.SetFloat("_Blend", 0); // Set to Alpha blend
            transparentMaterial.SetFloat("_AlphaClip", 0);
            transparentMaterial.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            transparentMaterial.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            transparentMaterial.SetFloat("_ZWrite", 0);
            transparentMaterial.DisableKeyword("_ALPHATEST_ON");
            transparentMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            transparentMaterial.EnableKeyword("_ALPHABLEND_ON");
            transparentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            transparentMaterial.renderQueue = 3000;
            
            renderer.material = transparentMaterial;
        }
        
        return highlightObj;
    }
    
    /// <summary>
    /// Updates the preview system
    /// </summary>
    private void UpdatePreviewSystem()
    {
        if (currentSelectedUnit != null && currentSelectedUnit.Transform != null)
        {
            // Check if we need to update previews
            if (Time.time % updateFrequency < Time.deltaTime)
            {
                RefreshMovementPreviews();
            }
        }
    }
    
    /// <summary>
    /// Called when a unit is selected
    /// </summary>
    private void OnUnitSelected(ISelectable selectable)
    {
        Debug.Log($"MovementPreviewSystem: Unit selected - {selectable}");
        if (selectable is IMovable movable)
        {
            currentSelectedUnit = movable;
            Debug.Log($"MovementPreviewSystem: Starting preview for movable unit at {movable.GridPosition}");
            StartPreviewDisplay();
        }
        else
        {
            Debug.Log("MovementPreviewSystem: Selected object is not IMovable");
        }
    }
    
    /// <summary>
    /// Called when a unit is deselected
    /// </summary>
    private void OnUnitDeselected(ISelectable selectable)
    {
        if (selectable is IMovable)
        {
            StopPreviewDisplay();
            currentSelectedUnit = null;
        }
    }
    
    /// <summary>
    /// Called when selection changes
    /// </summary>
    private void OnSelectionChanged(List<ISelectable> selectedObjects)
    {
        // Find the first movable unit in selection
        IMovable newSelectedUnit = null;
        foreach (ISelectable selectable in selectedObjects)
        {
            if (selectable is IMovable movable)
            {
                newSelectedUnit = movable;
                break;
            }
        }
        
        if (newSelectedUnit != currentSelectedUnit)
        {
            if (currentSelectedUnit != null)
            {
                StopPreviewDisplay();
            }
            
            currentSelectedUnit = newSelectedUnit;
            
            if (currentSelectedUnit != null)
            {
                StartPreviewDisplay();
            }
        }
    }
    
    /// <summary>
    /// Called when movement starts
    /// </summary>
    private void OnMovementStarted(IMovable unit, Vector2Int targetPosition)
    {
        if (unit == currentSelectedUnit)
        {
            // Temporarily hide previews during movement
            SetPreviewsVisible(false);
        }
    }
    
    /// <summary>
    /// Called when movement completes
    /// </summary>
    private void OnMovementCompleted(IMovable unit, Vector2Int finalPosition)
    {
        if (unit == currentSelectedUnit)
        {
            // Refresh previews for new position
            RefreshMovementPreviews();
            SetPreviewsVisible(true);
        }
    }
    
    /// <summary>
    /// Starts displaying movement previews for the selected unit
    /// </summary>
    private void StartPreviewDisplay()
    {
        if (!enablePreview || currentSelectedUnit == null) return;
        
        if (previewUpdateCoroutine != null)
        {
            StopCoroutine(previewUpdateCoroutine);
        }
        
        previewUpdateCoroutine = StartCoroutine(StartPreviewWithDelay());
        OnPreviewStarted?.Invoke(currentSelectedUnit);
        
        if (enableDebugLogging)
        {
            Debug.Log($"MovementPreviewSystem: Started preview for {currentSelectedUnit.GetDisplayInfo()}");
        }
    }
    
    /// <summary>
    /// Starts preview display with configured delay
    /// </summary>
    private IEnumerator StartPreviewWithDelay()
    {
        if (previewDelay > 0)
        {
            yield return new WaitForSeconds(previewDelay);
        }
        
        RefreshMovementPreviews();
    }
    
    /// <summary>
    /// Stops displaying movement previews
    /// </summary>
    private void StopPreviewDisplay()
    {
        if (previewUpdateCoroutine != null)
        {
            StopCoroutine(previewUpdateCoroutine);
            previewUpdateCoroutine = null;
        }
        
        ClearAllPreviews();
        OnPreviewStopped?.Invoke(currentSelectedUnit);
        
        if (enableDebugLogging)
        {
            Debug.Log("MovementPreviewSystem: Stopped preview display");
        }
    }
    
    /// <summary>
    /// Refreshes movement previews for current selected unit
    /// </summary>
    private void RefreshMovementPreviews()
    {
        Debug.Log($"MovementPreviewSystem: RefreshMovementPreviews called - enablePreview={enablePreview}, currentSelectedUnit={currentSelectedUnit != null}, gridManager={gridManager != null}, movementValidator={movementValidator != null}");
        
        if (!enablePreview || currentSelectedUnit == null || gridManager == null || movementValidator == null)
        {
            Debug.Log("MovementPreviewSystem: Skipping preview refresh due to missing requirements");
            return;
        }
        
        // Clear existing previews
        ClearAllPreviews();
        
        Vector2Int currentPosition = currentSelectedUnit.GridPosition;
        
        // Use MovementValidator to get properly bounded adjacent positions
        List<Vector2Int> validAdjacentPositions = movementValidator.GetValidAdjacentPositions(currentSelectedUnit);
        
        // Show valid positions
        foreach (Vector2Int targetPosition in validAdjacentPositions)
        {
            CreateMovementHighlight(targetPosition, HighlightType.Valid);
        }
        
        // Optionally show invalid moves if enabled
        if (showInvalidMoves)
        {
            List<Vector2Int> allAdjacentPositions = GetAdjacentPositions(currentPosition);
            foreach (Vector2Int targetPosition in allAdjacentPositions)
            {
                // Skip if already shown as valid
                if (validAdjacentPositions.Contains(targetPosition))
                    continue;
                    
                // Check if this is within grid bounds at least
                GridCoordinate targetCoord = new GridCoordinate(targetPosition.x, targetPosition.y);
                if (gridManager.IsValidCoordinate(targetCoord))
                {
                    MovementValidationResult validation = movementValidator.ValidateMovement(currentSelectedUnit, targetPosition);
                    if (!validation.isValid)
                    {
                        CreateMovementHighlight(targetPosition, HighlightType.Invalid);
                    }
                }
            }
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"MovementPreviewSystem: Refreshed {currentPreviews.Count} movement previews");
        }
    }
    
    /// <summary>
    /// Gets adjacent positions to a grid position
    /// </summary>
    private List<Vector2Int> GetAdjacentPositions(Vector2Int centerPosition)
    {
        List<Vector2Int> adjacentPositions = new List<Vector2Int>();
        
        // Add orthogonal neighbors (up, down, left, right)
        adjacentPositions.Add(centerPosition + Vector2Int.up);
        adjacentPositions.Add(centerPosition + Vector2Int.down);
        adjacentPositions.Add(centerPosition + Vector2Int.left);
        adjacentPositions.Add(centerPosition + Vector2Int.right);
        
        // Add diagonal neighbors (validation will filter them if not allowed)
        adjacentPositions.Add(centerPosition + new Vector2Int(1, 1));
        adjacentPositions.Add(centerPosition + new Vector2Int(1, -1));
        adjacentPositions.Add(centerPosition + new Vector2Int(-1, 1));
        adjacentPositions.Add(centerPosition + new Vector2Int(-1, -1));
        
        return adjacentPositions;
    }
    
    /// <summary>
    /// Creates a movement highlight at the specified position
    /// </summary>
    private void CreateMovementHighlight(Vector2Int gridPosition, HighlightType highlightType)
    {
        if (currentPreviews.ContainsKey(gridPosition))
        {
            return; // Already highlighted
        }
        
        // Check if we're at the limit
        if (currentPreviews.Count >= maxConcurrentPreviews)
        {
            if (enableDebugLogging)
            {
                Debug.LogWarning($"MovementPreviewSystem: Reached max concurrent previews ({maxConcurrentPreviews})");
            }
            return;
        }
        
        // Always use pooled highlight objects for reliable visualization
        CreatePooledHighlight(gridPosition, highlightType);
        
        currentPreviews[gridPosition] = highlightType;
        OnTileHighlighted?.Invoke(gridPosition, highlightType);
    }
    
    /// <summary>
    /// Creates a pooled highlight object
    /// </summary>
    private void CreatePooledHighlight(Vector2Int gridPosition, HighlightType highlightType)
    {
        Debug.Log($"MovementPreviewSystem: Creating pooled highlight at {gridPosition} with type {highlightType}");
        GameObject highlightObj;
        
        if (highlightPool.Count > 0)
        {
            highlightObj = highlightPool.Dequeue();
        }
        else
        {
            highlightObj = CreateHighlightObject();
        }
        
        // Position the highlight
        GridCoordinate gridCoord = new GridCoordinate(gridPosition.x, gridPosition.y);
        Vector3 worldPosition = gridManager.GridToWorld(gridCoord);
        worldPosition.y += 0.01f; // Slightly above the ground
        
        highlightObj.transform.position = worldPosition;
        highlightObj.SetActive(true);
        
        // Apply color directly to renderer - reliable approach
        Renderer renderer = highlightObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            switch (highlightType)
            {
                case HighlightType.Valid:
                    renderer.material.color = new Color(0f, 1f, 0f, 0.6f); // Semi-transparent green
                    break;
                case HighlightType.Invalid:
                    renderer.material.color = new Color(1f, 0f, 0f, 0.6f); // Semi-transparent red  
                    break;
                case HighlightType.Preview:
                    renderer.material.color = new Color(1f, 1f, 0f, 0.6f); // Semi-transparent yellow
                    break;
            }
        }
        
        // Add pulsing effect if enabled
        if (enablePulsing)
        {
            StartCoroutine(PulseHighlight(highlightObj));
        }
        
        activeHighlightObjects.Add(highlightObj);
    }
    
    /// <summary>
    /// Gets the appropriate material for a highlight type
    /// </summary>
    private Material GetMaterialForHighlightType(HighlightType highlightType)
    {
        switch (highlightType)
        {
            case HighlightType.Valid:
                return validMoveMaterial;
            case HighlightType.Invalid:
                return invalidMoveMaterial;
            case HighlightType.Preview:
                return previewMaterial;
            default:
                return validMoveMaterial;
        }
    }
    
    /// <summary>
    /// Pulses a highlight object
    /// </summary>
    private IEnumerator PulseHighlight(GameObject highlightObj)
    {
        Renderer renderer = highlightObj.GetComponent<Renderer>();
        if (renderer == null) yield break;
        
        Material originalMaterial = renderer.material;
        Color originalColor = originalMaterial.color;
        
        while (highlightObj.activeInHierarchy)
        {
            float time = Time.time * pulseSpeed;
            float alpha = pulseCurve.Evaluate((Mathf.Sin(time) + 1f) * 0.5f);
            
            Color newColor = originalColor;
            newColor.a = alpha;
            renderer.material.color = newColor;
            
            yield return null;
        }
        
        // Restore original color when done
        if (renderer != null && renderer.material != null)
        {
            renderer.material.color = originalColor;
        }
    }
    
    /// <summary>
    /// Clears all active previews
    /// </summary>
    private void ClearAllPreviews()
    {
        // Clear tile highlighters
        foreach (TileHighlighter highlighter in activeHighlights)
        {
            if (highlighter != null)
            {
                highlighter.ClearHighlight();
            }
        }
        activeHighlights.Clear();
        
        // Return pooled objects
        foreach (GameObject highlightObj in activeHighlightObjects)
        {
            if (highlightObj != null)
            {
                highlightObj.SetActive(false);
                highlightPool.Enqueue(highlightObj);
            }
        }
        activeHighlightObjects.Clear();
        
        // Clear tracking
        foreach (Vector2Int position in currentPreviews.Keys)
        {
            OnTileUnhighlighted?.Invoke(position);
        }
        currentPreviews.Clear();
    }
    
    /// <summary>
    /// Sets visibility of all current previews
    /// </summary>
    private void SetPreviewsVisible(bool visible)
    {
        // Set tile highlighter visibility
        foreach (TileHighlighter highlighter in activeHighlights)
        {
            if (highlighter != null)
            {
                highlighter.gameObject.SetActive(visible);
            }
        }
        
        // Set pooled object visibility
        foreach (GameObject highlightObj in activeHighlightObjects)
        {
            if (highlightObj != null)
            {
                highlightObj.SetActive(visible);
            }
        }
    }
    
    /// <summary>
    /// Gets current preview information for debugging
    /// </summary>
    public string GetPreviewInfo()
    {
        if (currentPreviews.Count == 0)
            return "No active movement previews";
        
        return $"Active Previews: {currentPreviews.Count}, Selected Unit: {currentSelectedUnit?.GetDisplayInfo() ?? "None"}";
    }
    
    /// <summary>
    /// Unity Gizmos for debug visualization
    /// </summary>
    void OnDrawGizmos()
    {
        if (!enableDebugLogging || !showPreviewBounds) return;
        
        // Draw preview bounds for active highlights
        foreach (var preview in currentPreviews)
        {
            Vector2Int gridPos = preview.Key;
            HighlightType type = preview.Value;
            
            GridCoordinate gridCoord = new GridCoordinate(gridPos.x, gridPos.y);
            if (gridManager != null)
            {
                Vector3 worldPos = gridManager.GridToWorld(gridCoord);
                
                // Set color based on highlight type
                switch (type)
                {
                    case HighlightType.Valid:
                        Gizmos.color = Color.green;
                        break;
                    case HighlightType.Invalid:
                        Gizmos.color = Color.red;
                        break;
                    case HighlightType.Preview:
                        Gizmos.color = Color.yellow;
                        break;
                }
                
                Gizmos.DrawWireCube(worldPos, Vector3.one * 0.8f);
            }
        }
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Stop coroutines
        if (previewUpdateCoroutine != null)
        {
            StopCoroutine(previewUpdateCoroutine);
        }
        
        // Clear previews
        ClearAllPreviews();
        
        // Clean up object pool
        while (highlightPool.Count > 0)
        {
            GameObject obj = highlightPool.Dequeue();
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        
        // Unregister from events
        if (selectionManager != null)
        {
            selectionManager.OnObjectSelected -= OnUnitSelected;
            selectionManager.OnObjectDeselected -= OnUnitDeselected;
            selectionManager.OnSelectionChanged -= OnSelectionChanged;
        }
        
        if (movementManager != null)
        {
            movementManager.OnMovementStarted -= OnMovementStarted;
            movementManager.OnMovementCompleted -= OnMovementCompleted;
        }
        
        // Clear event references
        OnTileHighlighted = null;
        OnTileUnhighlighted = null;
        OnPreviewStarted = null;
        OnPreviewStopped = null;
    }
}