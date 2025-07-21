using UnityEngine;

/// <summary>
/// Handles mouse input processing and raycasting for the selection system.
/// Converts mouse position to world rays, detects selectable objects, and coordinates with SelectionManager.
/// Optimized for performance with configurable raycast settings and layer filtering.
/// </summary>
public class MouseInputHandler : MonoBehaviour
{
    [Header("Raycast Configuration")]
    [SerializeField] private LayerMask unitLayerMask = -1;
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private bool enableRaycastVisualization = false;
    [SerializeField] private float visualizationDuration = 0.1f;
    
    [Header("Input Settings")]
    [SerializeField] private KeyCode selectButton = KeyCode.Mouse0;
    [SerializeField] private KeyCode deselectButton = KeyCode.Mouse1;
    [SerializeField] private bool enableHoverFeedback = true;
    [SerializeField] private bool enableClickToDeselect = true;
    [SerializeField] private float inputCooldown = 0.05f;
    
    [Header("Performance Optimization")]
    [SerializeField] private bool enableRaycastCaching = true;
    [SerializeField] private float hoverUpdateRate = 30f; // Updates per second
    [SerializeField] private bool enableNullChecks = true;
    [SerializeField] private int maxRaycastsPerFrame = 1;
    
    [Header("Debug Configuration")]
    [SerializeField] private bool enableInputLogging = true;
    [SerializeField] private bool showRaycastInfo = true;
    [SerializeField] private bool enablePerformanceLogging = false;
    
    // System references
    private SelectionManager selectionManager;
    private Camera mainCamera;
    
    // Input state
    private float lastInputTime = 0f;
    private float lastHoverUpdateTime = 0f;
    private Vector3 lastMousePosition;
    private bool hasMouseMoved = false;
    
    // Raycast caching
    private RaycastHit lastHit;
    private bool lastHitValid = false;
    private Vector3 lastRaycastPosition;
    private ISelectable currentHoverTarget = null;
    
    // Performance tracking
    private int raycastsThisFrame = 0;
    private float frameStartTime;
    
    void Awake()
    {
        InitializeInputHandler();
    }
    
    void Start()
    {
        FindSystemReferences();
        ValidateConfiguration();
    }
    
    void Update()
    {
        frameStartTime = Time.realtimeSinceStartup;
        raycastsThisFrame = 0;
        
        UpdateMouseState();
        HandleMouseInput();
        
        if (enableHoverFeedback)
        {
            UpdateHoverDetection();
        }
        
        if (enablePerformanceLogging && Time.frameCount % 300 == 0) // Log every 5 seconds at 60 FPS
        {
            LogPerformanceStats();
        }
    }
    
    /// <summary>
    /// Initializes the input handler
    /// </summary>
    private void InitializeInputHandler()
    {
        lastMousePosition = Input.mousePosition;
        
        if (enableInputLogging)
        {
            Debug.Log("MouseInputHandler initialized");
        }
    }
    
    /// <summary>
    /// Finds references to required systems
    /// </summary>
    private void FindSystemReferences()
    {
        // Find SelectionManager
        selectionManager = GetComponent<SelectionManager>();
        if (selectionManager == null)
        {
            selectionManager = FindFirstObjectByType<SelectionManager>();
        }
        
        // Find main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (enableInputLogging)
        {
            Debug.Log($"MouseInputHandler found references - SelectionManager: {selectionManager != null}, Camera: {mainCamera != null}");
        }
    }
    
    /// <summary>
    /// Validates the input handler configuration
    /// </summary>
    private void ValidateConfiguration()
    {
        if (selectionManager == null)
        {
            Debug.LogError("MouseInputHandler: SelectionManager not found!");
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("MouseInputHandler: Main camera not found!");
        }
        
        if (raycastDistance <= 0)
        {
            Debug.LogWarning("MouseInputHandler: Raycast distance should be > 0");
            raycastDistance = 100f;
        }
        
        if (hoverUpdateRate <= 0)
        {
            Debug.LogWarning("MouseInputHandler: Hover update rate should be > 0");
            hoverUpdateRate = 30f;
        }
    }
    
    /// <summary>
    /// Updates mouse state tracking
    /// </summary>
    private void UpdateMouseState()
    {
        Vector3 currentMousePosition = Input.mousePosition;
        hasMouseMoved = Vector3.Distance(currentMousePosition, lastMousePosition) > 0.1f;
        
        if (hasMouseMoved)
        {
            lastMousePosition = currentMousePosition;
            
            // Invalidate raycast cache when mouse moves
            if (enableRaycastCaching)
            {
                lastHitValid = false;
            }
        }
    }
    
    /// <summary>
    /// Handles mouse input for selection
    /// </summary>
    private void HandleMouseInput()
    {
        if (!CanProcessInput()) return;
        
        // Handle selection input
        if (Input.GetKeyDown(selectButton))
        {
            ProcessSelectionInput();
            lastInputTime = Time.time;
        }
        
        // Handle deselection input
        if (enableClickToDeselect && Input.GetKeyDown(deselectButton))
        {
            ProcessDeselectionInput();
            lastInputTime = Time.time;
        }
    }
    
    /// <summary>
    /// Processes selection input (left click)
    /// </summary>
    private void ProcessSelectionInput()
    {
        RaycastHit hit;
        Vector3 mousePosition = Input.mousePosition;
        
        if (PerformRaycast(mousePosition, out hit))
        {
            ISelectable selectable = hit.collider.GetComponent<ISelectable>();
            
            if (selectable != null)
            {
                bool selected = selectionManager.TrySelectObject(selectable, hit.point);
                
                if (enableInputLogging)
                {
                    Debug.Log($"Selection attempt on {hit.collider.name}: {(selected ? "SUCCESS" : "FAILED")}");
                }
            }
            else
            {
                // Clicked on something that's not selectable
                if (enableClickToDeselect)
                {
                    selectionManager.ClearSelection();
                    
                    if (enableInputLogging)
                    {
                        Debug.Log("Clicked on non-selectable object, clearing selection");
                    }
                }
            }
        }
        else
        {
            // Clicked on empty space
            if (enableClickToDeselect)
            {
                selectionManager.ClearSelection();
                
                if (enableInputLogging)
                {
                    Debug.Log("Clicked on empty space, clearing selection");
                }
            }
        }
    }
    
    /// <summary>
    /// Processes deselection input (right click)
    /// </summary>
    private void ProcessDeselectionInput()
    {
        RaycastHit hit;
        Vector3 mousePosition = Input.mousePosition;
        
        if (PerformRaycast(mousePosition, out hit))
        {
            ISelectable selectable = hit.collider.GetComponent<ISelectable>();
            
            if (selectable != null && selectionManager.IsSelected(selectable))
            {
                selectionManager.DeselectObject(selectable);
                
                if (enableInputLogging)
                {
                    Debug.Log($"Deselected {hit.collider.name}");
                }
            }
        }
        else
        {
            // Right-clicked on empty space - clear all selection
            selectionManager.ClearSelection();
            
            if (enableInputLogging)
            {
                Debug.Log("Right-clicked empty space, clearing all selection");
            }
        }
    }
    
    /// <summary>
    /// Updates hover detection for visual feedback
    /// </summary>
    private void UpdateHoverDetection()
    {
        if (!ShouldUpdateHover()) return;
        
        RaycastHit hit;
        Vector3 mousePosition = Input.mousePosition;
        ISelectable newHoverTarget = null;
        
        if (PerformRaycast(mousePosition, out hit))
        {
            ISelectable selectable = hit.collider.GetComponent<ISelectable>();
            if (selectable != null && selectable.CanBeSelected)
            {
                // Validate hover based on team restrictions
                if (selectionManager.RestrictToPlayerTeam)
                {
                    if (selectable.ValidateSelection(selectionManager.PlayerTeam, true))
                    {
                        newHoverTarget = selectable;
                    }
                }
                else
                {
                    newHoverTarget = selectable;
                }
            }
        }
        
        // Update hover state if it changed
        if (newHoverTarget != currentHoverTarget)
        {
            currentHoverTarget = newHoverTarget;
            selectionManager.SetHoveredObject(currentHoverTarget);
            
            if (enableInputLogging && currentHoverTarget != null)
            {
                Debug.Log($"Hovering over: {currentHoverTarget.GetDisplayInfo()}");
            }
        }
        
        lastHoverUpdateTime = Time.time;
    }
    
    /// <summary>
    /// Performs a raycast from the camera through the mouse position
    /// </summary>
    private bool PerformRaycast(Vector3 screenPosition, out RaycastHit hit)
    {
        hit = default;
        
        if (mainCamera == null)
        {
            Debug.LogError("MouseInputHandler: Cannot raycast without a camera");
            return false;
        }
        
        if (raycastsThisFrame >= maxRaycastsPerFrame)
        {
            if (enablePerformanceLogging)
            {
                Debug.LogWarning($"Reached max raycasts per frame ({maxRaycastsPerFrame})");
            }
            return false;
        }
        
        // Check raycast cache
        if (enableRaycastCaching && lastHitValid && Vector3.Distance(screenPosition, lastRaycastPosition) < 1f)
        {
            hit = lastHit;
            return hit.collider != null;
        }
        
        // Perform new raycast
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        bool hitSomething = Physics.Raycast(ray, out hit, raycastDistance, unitLayerMask);
        
        raycastsThisFrame++;
        
        // Update cache
        if (enableRaycastCaching)
        {
            lastHit = hit;
            lastHitValid = true;
            lastRaycastPosition = screenPosition;
        }
        
        // Debug visualization
        if (enableRaycastVisualization)
        {
            Vector3 endPoint = hitSomething ? hit.point : ray.origin + ray.direction * raycastDistance;
            Color rayColor = hitSomething ? Color.green : Color.red;
            Debug.DrawLine(ray.origin, endPoint, rayColor, visualizationDuration);
        }
        
        if (showRaycastInfo && hitSomething)
        {
            Debug.Log($"Raycast hit: {hit.collider.name} at {hit.point}");
        }
        
        return hitSomething;
    }
    
    /// <summary>
    /// Checks if input can be processed (considering cooldowns)
    /// </summary>
    private bool CanProcessInput()
    {
        return Time.time >= lastInputTime + inputCooldown;
    }
    
    /// <summary>
    /// Checks if hover detection should be updated
    /// </summary>
    private bool ShouldUpdateHover()
    {
        if (!hasMouseMoved && currentHoverTarget != null)
            return false; // Don't update if mouse hasn't moved and we have a current target
        
        float timeSinceLastUpdate = Time.time - lastHoverUpdateTime;
        float updateInterval = 1f / hoverUpdateRate;
        
        return timeSinceLastUpdate >= updateInterval;
    }
    
    /// <summary>
    /// Gets the current mouse world position (on the ground plane)
    /// </summary>
    public Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null) return Vector3.zero;
        
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        // Raycast against ground plane (y = 0)
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;
        
        if (groundPlane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }
        
        return Vector3.zero;
    }
    
    /// <summary>
    /// Gets the current mouse grid coordinate
    /// </summary>
    public GridCoordinate GetMouseGridCoordinate()
    {
        Vector3 worldPosition = GetMouseWorldPosition();
        
        // Convert world position to grid coordinate
        GridManager gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager != null)
        {
            return gridManager.WorldToGrid(worldPosition);
        }
        
        // Fallback conversion
        return new GridCoordinate(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z));
    }
    
    /// <summary>
    /// Forces a raycast cache refresh
    /// </summary>
    public void RefreshRaycastCache()
    {
        lastHitValid = false;
        currentHoverTarget = null;
    }
    
    /// <summary>
    /// Gets input handler status information
    /// </summary>
    public string GetStatusInfo()
    {
        return $"MouseInputHandler - Raycasts this frame: {raycastsThisFrame}, " +
               $"Cache valid: {lastHitValid}, " +
               $"Current hover: {(currentHoverTarget?.GetDisplayInfo() ?? "None")}";
    }
    
    /// <summary>
    /// Logs performance statistics
    /// </summary>
    private void LogPerformanceStats()
    {
        float frameTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f; // Convert to milliseconds
        Debug.Log($"MouseInputHandler Performance - Frame time: {frameTime:F2}ms, Raycasts: {raycastsThisFrame}");
    }
    
    /// <summary>
    /// Unity Gizmos for debug visualization
    /// </summary>
    void OnDrawGizmos()
    {
        if (!enableRaycastVisualization || mainCamera == null) return;
        
        // Draw current mouse ray
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * raycastDistance);
        
        // Draw last raycast hit
        if (lastHitValid && lastHit.collider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(lastHit.point, 0.2f);
        }
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        currentHoverTarget = null;
        selectionManager = null;
        mainCamera = null;
    }
}