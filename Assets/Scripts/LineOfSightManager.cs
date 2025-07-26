using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Centralized line-of-sight validation and caching system for tactical combat.
/// Manages raycast-based obstacle detection with performance optimization and visual feedback.
/// Integrates with combat system to provide tactical depth through cover mechanics.
/// </summary>
public class LineOfSightManager : MonoBehaviour, ILineOfSightProvider
{
    [Header("Line of Sight Configuration")]
    [SerializeField] private LayerMask obstacleLayerMask = -1;
    [SerializeField] private float raycastHeight = 0.5f;
    [SerializeField] private float raycastRadius = 0.1f;
    [SerializeField] private bool useSphereCast = true;
    [SerializeField] private bool enableDebugging = true;
    
    [Header("Attack Integration")]
    [SerializeField] private bool requireLineOfSightForAttacks = true;
    [SerializeField] private bool allowAttackThroughDiagonalGaps = false;
    [SerializeField] private float lineOfSightTolerance = 0.1f;
    
    [Header("Performance Optimization")]
    [SerializeField] private bool cacheResults = true;
    [SerializeField] private int maxRaycastsPerFrame = 10;
    [SerializeField] private float updateInterval = 0.1f;
    [SerializeField] private int cacheMaxSize = 100;
    [SerializeField] private float cacheValidityDuration = 1.0f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableRaycastVisualization = false;
    [SerializeField] private bool logResults = false;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color clearLineColor = Color.green;
    [SerializeField] private Color blockedLineColor = Color.red;
    
    // System references
    private GridManager gridManager;
    private CombatManager combatManager;
    private LineOfSightVisualizer visualizer;
    private RaycastOptimizer optimizer;
    
    // Caching system
    private Dictionary<string, CachedLineOfSightResult> lineOfSightCache;
    private Queue<RaycastRequest> raycastQueue;
    private bool isProcessingRaycasts = false;
    
    // Events
    public System.Action<Vector3, Vector3, bool> OnLineOfSightChecked;
    public System.Action<Vector3, Vector3, GameObject> OnLineOfSightBlocked;
    public System.Action<Vector3, Vector3> OnLineOfSightCleared;
    
    // Properties
    public bool RequireLineOfSightForAttacks => requireLineOfSightForAttacks;
    public LayerMask ObstacleLayerMask => obstacleLayerMask;
    public float RaycastHeight => raycastHeight;
    public bool EnableDebugging => enableDebugging;
    
    // Cached result structure
    private struct CachedLineOfSightResult
    {
        public LineOfSightResult result;
        public float timestamp;
        public bool isValid => Time.time - timestamp < lineOfSightManager.cacheValidityDuration;
        
        private LineOfSightManager lineOfSightManager;
        
        public CachedLineOfSightResult(LineOfSightResult res, LineOfSightManager manager)
        {
            result = res;
            timestamp = Time.time;
            lineOfSightManager = manager;
        }
    }
    
    // Raycast request structure
    private struct RaycastRequest
    {
        public Vector3 fromPosition;
        public Vector3 toPosition;
        public System.Action<LineOfSightResult> callback;
        public string cacheKey;
        
        public RaycastRequest(Vector3 from, Vector3 to, System.Action<LineOfSightResult> cb, string key)
        {
            fromPosition = from;
            toPosition = to;
            callback = cb;
            cacheKey = key;
        }
    }
    
    void Awake()
    {
        InitializeLineOfSightManager();
    }
    
    void Start()
    {
        FindSystemReferences();
        StartRaycastProcessing();
    }
    
    void Update()
    {
        if (enableDebugging && Input.GetKeyDown(KeyCode.L))
        {
            TestLineOfSightFromSelectedUnit();
        }
    }
    
    /// <summary>
    /// Initializes the line-of-sight management system
    /// </summary>
    private void InitializeLineOfSightManager()
    {
        lineOfSightCache = new Dictionary<string, CachedLineOfSightResult>();
        raycastQueue = new Queue<RaycastRequest>();
        
        if (enableDebugging)
        {
            Debug.Log("LineOfSightManager initialized - Tactical line-of-sight system ready");
        }
    }
    
    /// <summary>
    /// Finds references to other systems
    /// </summary>
    private void FindSystemReferences()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        combatManager = FindFirstObjectByType<CombatManager>();
        visualizer = GetComponent<LineOfSightVisualizer>();
        optimizer = GetComponent<RaycastOptimizer>();
        
        if (enableDebugging)
        {
            Debug.Log($"LineOfSightManager found references - Grid: {gridManager != null}, " +
                     $"Combat: {combatManager != null}, Visualizer: {visualizer != null}, Optimizer: {optimizer != null}");
        }
    }
    
    /// <summary>
    /// Starts the raycast processing coroutine
    /// </summary>
    private void StartRaycastProcessing()
    {
        if (!isProcessingRaycasts)
        {
            StartCoroutine(ProcessRaycastQueue());
            isProcessingRaycasts = true;
        }
    }
    
    #region ILineOfSightProvider Implementation
    
    /// <summary>
    /// Checks if there is a clear line of sight between two world positions
    /// </summary>
    public bool HasLineOfSight(Vector3 fromPosition, Vector3 toPosition)
    {
        LineOfSightResult result = GetLineOfSightDetails(fromPosition, toPosition);
        return !result.isBlocked;
    }
    
    /// <summary>
    /// Checks if there is line of sight between two transforms
    /// </summary>
    public bool HasLineOfSight(Transform fromTransform, Transform toTransform)
    {
        if (fromTransform == null || toTransform == null)
        {
            if (logResults)
                Debug.LogWarning("LineOfSightManager: Null transform provided to HasLineOfSight");
            return false;
        }
        
        Vector3 fromPos = GetLineOfSightPosition(fromTransform);
        Vector3 toPos = GetLineOfSightPosition(toTransform);
        
        return HasLineOfSight(fromPos, toPos);
    }
    
    /// <summary>
    /// Gets detailed line-of-sight information including blocking objects
    /// </summary>
    public LineOfSightResult GetLineOfSightDetails(Vector3 fromPosition, Vector3 toPosition)
    {
        string cacheKey = GetCacheKey(fromPosition, toPosition);
        
        // Check cache first
        if (cacheResults && lineOfSightCache.ContainsKey(cacheKey))
        {
            CachedLineOfSightResult cached = lineOfSightCache[cacheKey];
            if (cached.isValid)
            {
                if (logResults)
                    Debug.Log($"LineOfSightManager: Using cached result for {cacheKey}");
                return cached.result;
            }
            else
            {
                lineOfSightCache.Remove(cacheKey);
            }
        }
        
        // Perform raycast
        LineOfSightResult result = PerformLineOfSightRaycast(fromPosition, toPosition);
        
        // Cache result
        if (cacheResults)
        {
            CacheLineOfSightResult(cacheKey, result);
        }
        
        // Trigger events
        OnLineOfSightChecked?.Invoke(fromPosition, toPosition, !result.isBlocked);
        if (result.isBlocked)
        {
            OnLineOfSightBlocked?.Invoke(fromPosition, toPosition, result.blockingObject);
        }
        else
        {
            OnLineOfSightCleared?.Invoke(fromPosition, toPosition);
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets the distance at which line of sight is blocked, or full distance if clear
    /// </summary>
    public float GetLineOfSightDistance(Vector3 fromPosition, Vector3 toPosition)
    {
        LineOfSightResult result = GetLineOfSightDetails(fromPosition, toPosition);
        return result.distance;
    }
    
    /// <summary>
    /// Checks if a specific object is blocking line of sight between two positions
    /// </summary>
    public bool IsObjectBlockingLineOfSight(Vector3 fromPosition, Vector3 toPosition, GameObject potentialBlocker)
    {
        LineOfSightResult result = GetLineOfSightDetails(fromPosition, toPosition);
        return result.isBlocked && result.blockingObject == potentialBlocker;
    }
    
    /// <summary>
    /// Gets all objects that are blocking line of sight between two positions
    /// </summary>
    public GameObject[] GetBlockingObjects(Vector3 fromPosition, Vector3 toPosition)
    {
        List<GameObject> blockingObjects = new List<GameObject>();
        
        Vector3 direction = (toPosition - fromPosition).normalized;
        float distance = Vector3.Distance(fromPosition, toPosition);
        
        RaycastHit[] hits;
        if (useSphereCast)
        {
            hits = Physics.SphereCastAll(fromPosition, raycastRadius, direction, distance, obstacleLayerMask);
        }
        else
        {
            hits = Physics.RaycastAll(fromPosition, direction, distance, obstacleLayerMask);
        }
        
        foreach (RaycastHit hit in hits)
        {
            if (!blockingObjects.Contains(hit.collider.gameObject))
            {
                blockingObjects.Add(hit.collider.gameObject);
            }
        }
        
        return blockingObjects.ToArray();
    }
    
    /// <summary>
    /// Visualizes line of sight for debugging and player feedback
    /// </summary>
    public void VisualizeLineOfSight(Vector3 fromPosition, Vector3 toPosition, float duration = 1.0f)
    {
        if (visualizer != null)
        {
            bool hasLineOfSight = HasLineOfSight(fromPosition, toPosition);
            visualizer.ShowLineOfSight(fromPosition, toPosition, hasLineOfSight, duration);
        }
        else if (enableRaycastVisualization)
        {
            bool hasLineOfSight = HasLineOfSight(fromPosition, toPosition);
            Color lineColor = hasLineOfSight ? clearLineColor : blockedLineColor;
            Debug.DrawLine(fromPosition, toPosition, lineColor, duration);
        }
    }
    
    #endregion
    
    #region Core Line of Sight Logic
    
    /// <summary>
    /// Performs the actual raycast for line-of-sight detection
    /// </summary>
    private LineOfSightResult PerformLineOfSightRaycast(Vector3 fromPosition, Vector3 toPosition)
    {
        // Adjust positions to raycast height
        Vector3 adjustedFrom = new Vector3(fromPosition.x, fromPosition.y + raycastHeight, fromPosition.z);
        Vector3 adjustedTo = new Vector3(toPosition.x, toPosition.y + raycastHeight, toPosition.z);
        
        Vector3 direction = (adjustedTo - adjustedFrom).normalized;
        float distance = Vector3.Distance(adjustedFrom, adjustedTo);
        
        // Apply line-of-sight tolerance
        distance -= lineOfSightTolerance;
        if (distance <= 0)
        {
            return LineOfSightResult.Clear(Vector3.Distance(fromPosition, toPosition));
        }
        
        RaycastHit hit;
        bool hasHit;
        
        if (useSphereCast)
        {
            hasHit = Physics.SphereCast(adjustedFrom, raycastRadius, direction, out hit, distance, obstacleLayerMask);
        }
        else
        {
            hasHit = Physics.Raycast(adjustedFrom, direction, out hit, distance, obstacleLayerMask);
        }
        
        if (logResults)
        {
            Debug.Log($"LineOfSightManager: Raycast from {fromPosition} to {toPosition} - Hit: {hasHit}");
        }
        
        if (enableRaycastVisualization)
        {
            Color rayColor = hasHit ? blockedLineColor : clearLineColor;
            Debug.DrawLine(adjustedFrom, adjustedTo, rayColor, 1.0f);
            
            if (hasHit)
            {
                // Debug.DrawWireSphere is not available - use Gizmos in OnDrawGizmos instead
                Debug.DrawLine(hit.point + Vector3.up * 0.1f, hit.point - Vector3.up * 0.1f, Color.yellow, 1.0f);
                Debug.DrawLine(hit.point + Vector3.right * 0.1f, hit.point - Vector3.right * 0.1f, Color.yellow, 1.0f);
                Debug.DrawLine(hit.point + Vector3.forward * 0.1f, hit.point - Vector3.forward * 0.1f, Color.yellow, 1.0f);
            }
        }
        
        if (hasHit)
        {
            string blockingReason = $"Blocked by {hit.collider.gameObject.name}";
            return LineOfSightResult.Blocked(hit.distance, hit.point, hit.collider.gameObject, blockingReason);
        }
        else
        {
            return LineOfSightResult.Clear(distance);
        }
    }
    
    /// <summary>
    /// Gets the appropriate position for line-of-sight checks from a transform
    /// </summary>
    private Vector3 GetLineOfSightPosition(Transform transform)
    {
        // For units, use center position; for grid positions, use transform position
        Renderer renderer = transform.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.center;
        }
        
        return transform.position;
    }
    
    /// <summary>
    /// Generates a cache key for line-of-sight results
    /// </summary>
    private string GetCacheKey(Vector3 fromPosition, Vector3 toPosition)
    {
        // Round positions to avoid floating point precision issues
        Vector3 roundedFrom = new Vector3(
            Mathf.Round(fromPosition.x * 10f) / 10f,
            Mathf.Round(fromPosition.y * 10f) / 10f,
            Mathf.Round(fromPosition.z * 10f) / 10f);
        
        Vector3 roundedTo = new Vector3(
            Mathf.Round(toPosition.x * 10f) / 10f,
            Mathf.Round(toPosition.y * 10f) / 10f,
            Mathf.Round(toPosition.z * 10f) / 10f);
        
        return $"{roundedFrom}→{roundedTo}";
    }
    
    /// <summary>
    /// Caches a line-of-sight result
    /// </summary>
    private void CacheLineOfSightResult(string cacheKey, LineOfSightResult result)
    {
        // Manage cache size
        if (lineOfSightCache.Count >= cacheMaxSize)
        {
            // Remove oldest entries
            List<string> keysToRemove = new List<string>();
            foreach (var kvp in lineOfSightCache)
            {
                if (!kvp.Value.isValid)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (string key in keysToRemove)
            {
                lineOfSightCache.Remove(key);
            }
            
            // If still too large, remove some valid entries
            if (lineOfSightCache.Count >= cacheMaxSize)
            {
                var oldestKey = "";
                float oldestTime = float.MaxValue;
                
                foreach (var kvp in lineOfSightCache)
                {
                    if (kvp.Value.timestamp < oldestTime)
                    {
                        oldestTime = kvp.Value.timestamp;
                        oldestKey = kvp.Key;
                    }
                }
                
                if (!string.IsNullOrEmpty(oldestKey))
                {
                    lineOfSightCache.Remove(oldestKey);
                }
            }
        }
        
        lineOfSightCache[cacheKey] = new CachedLineOfSightResult(result, this);
    }
    
    #endregion
    
    #region Raycast Queue Processing
    
    /// <summary>
    /// Coroutine that processes raycast requests to spread load across frames
    /// </summary>
    private IEnumerator ProcessRaycastQueue()
    {
        while (true)
        {
            int raycastsThisFrame = 0;
            
            while (raycastQueue.Count > 0 && raycastsThisFrame < maxRaycastsPerFrame)
            {
                RaycastRequest request = raycastQueue.Dequeue();
                LineOfSightResult result = GetLineOfSightDetails(request.fromPosition, request.toPosition);
                request.callback?.Invoke(result);
                raycastsThisFrame++;
            }
            
            yield return new WaitForSeconds(updateInterval);
        }
    }
    
    /// <summary>
    /// Queues a line-of-sight check for asynchronous processing
    /// </summary>
    public void QueueLineOfSightCheck(Vector3 fromPosition, Vector3 toPosition, System.Action<LineOfSightResult> callback)
    {
        string cacheKey = GetCacheKey(fromPosition, toPosition);
        RaycastRequest request = new RaycastRequest(fromPosition, toPosition, callback, cacheKey);
        raycastQueue.Enqueue(request);
    }
    
    #endregion
    
    #region Public Utility Methods
    
    /// <summary>
    /// Clears the line-of-sight cache
    /// </summary>
    public void ClearCache()
    {
        lineOfSightCache.Clear();
        if (enableDebugging)
        {
            Debug.Log("LineOfSightManager: Cache cleared");
        }
    }
    
    /// <summary>
    /// Gets cache statistics for debugging
    /// </summary>
    public string GetCacheStats()
    {
        int validEntries = 0;
        int expiredEntries = 0;
        
        foreach (var kvp in lineOfSightCache)
        {
            if (kvp.Value.isValid)
                validEntries++;
            else
                expiredEntries++;
        }
        
        return $"Cache: {validEntries} valid, {expiredEntries} expired, {raycastQueue.Count} queued";
    }
    
    /// <summary>
    /// Tests line of sight from currently selected unit to all other units
    /// </summary>
    private void TestLineOfSightFromSelectedUnit()
    {
        SelectionManager selectionManager = FindFirstObjectByType<SelectionManager>();
        if (selectionManager == null) return;
        
        var selectedObjects = selectionManager.GetSelectedObjects<ISelectable>();
        if (selectedObjects.Count == 0) return;
        
        MonoBehaviour selectedUnit = selectedObjects[0] as MonoBehaviour;
        if (selectedUnit == null) return;
        
        Debug.Log($"=== Testing Line of Sight from {selectedUnit.name} ===");
        
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in allUnits)
        {
            if (unit.gameObject == selectedUnit.gameObject) continue;
            
            bool hasLOS = HasLineOfSight(selectedUnit.transform, unit.transform);
            Debug.Log($"  → {unit.name}: {(hasLOS ? "CLEAR" : "BLOCKED")}");
            
            VisualizeLineOfSight(selectedUnit.transform.position, unit.transform.position, 3.0f);
        }
    }
    
    /// <summary>
    /// Updates line-of-sight settings at runtime
    /// </summary>
    public void UpdateSettings(LayerMask newObstacleLayerMask, float newRaycastHeight, float newRaycastRadius)
    {
        obstacleLayerMask = newObstacleLayerMask;
        raycastHeight = newRaycastHeight;
        raycastRadius = newRaycastRadius;
        
        // Clear cache when settings change
        ClearCache();
        
        if (enableDebugging)
        {
            Debug.Log("LineOfSightManager: Settings updated, cache cleared");
        }
    }
    
    #endregion
    
    #region Gizmos and Debug Visualization
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        // Draw raycast visualization for selected objects
        SelectionManager selectionManager = FindFirstObjectByType<SelectionManager>();
        if (selectionManager == null) return;
        
        var selectedObjects = selectionManager.GetSelectedObjects<ISelectable>();
        if (selectedObjects.Count == 0) return;
        
        MonoBehaviour selectedUnit = selectedObjects[0] as MonoBehaviour;
        if (selectedUnit == null) return;
        
        // Draw line-of-sight lines to all other units
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in allUnits)
        {
            if (unit.gameObject == selectedUnit.gameObject) continue;
            
            bool hasLOS = HasLineOfSight(selectedUnit.transform, unit.transform);
            Gizmos.color = hasLOS ? clearLineColor : blockedLineColor;
            
            Vector3 fromPos = GetLineOfSightPosition(selectedUnit.transform);
            Vector3 toPos = GetLineOfSightPosition(unit.transform);
            
            Gizmos.DrawLine(fromPos, toPos);
            
            // Draw sphere at target
            Gizmos.DrawWireSphere(toPos, 0.2f);
        }
    }
    
    #endregion
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Stop processing
        isProcessingRaycasts = false;
        
        // Clear cache
        lineOfSightCache?.Clear();
        raycastQueue?.Clear();
        
        // Clear event references
        OnLineOfSightChecked = null;
        OnLineOfSightBlocked = null;
        OnLineOfSightCleared = null;
    }
}