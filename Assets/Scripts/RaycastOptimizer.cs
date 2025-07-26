using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Performance optimization system for line-of-sight raycast operations.
/// Manages raycast batching, caching, and frame-spreading to maintain smooth gameplay
/// while providing real-time line-of-sight validation for tactical combat.
/// </summary>
public class RaycastOptimizer : MonoBehaviour
{
    [Header("Performance Configuration")]
    [SerializeField] private int maxRaycastsPerFrame = 10;
    [SerializeField] private bool enableOptimization = true;
    [SerializeField] private float updateInterval = 0.1f;
    [SerializeField] private int batchSize = 5;
    
    [Header("Caching Settings")]
    [SerializeField] private bool enableCaching = true;
    [SerializeField] private int maxCacheSize = 200;
    [SerializeField] private float cacheValidityDuration = 0.5f;
    [SerializeField] private bool smartCacheInvalidation = true;
    
    [Header("Distance Culling")]
    [SerializeField] private bool enableDistanceCulling = true;
    [SerializeField] private float maxRaycastDistance = 20f;
    [SerializeField] private float cullDistanceThreshold = 15f;
    [SerializeField] private bool useHierarchicalCulling = true;
    
    [Header("Adaptive Performance")]
    [SerializeField] private bool enableAdaptivePerformance = true;
    [SerializeField] private float targetFrameTime = 0.016f; // 60 FPS
    [SerializeField] private float performanceThreshold = 0.02f; // 50 FPS
    [SerializeField] private int minRaycastsPerFrame = 2;
    [SerializeField] private int maxAdaptiveRaycasts = 20;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableOptimizationLogging = false;
    [SerializeField] private bool showPerformanceStats = false;
    [SerializeField] private bool profileRaycastTimes = false;
    
    // System references
    private LineOfSightManager lineOfSightManager;
    
    // Optimization state
    private Queue<RaycastRequest> raycastQueue;
    private Dictionary<string, CachedRaycastResult> raycastCache;
    private HashSet<Transform> trackedTransforms;
    private Dictionary<Transform, Vector3> lastKnownPositions;
    
    // Performance tracking
    private float averageFrameTime;
    private int currentMaxRaycasts;
    private int raycastsThisFrame;
    private float frameStartTime;
    private List<float> recentRaycastTimes;
    
    // Statistics
    private int totalRaycastsPerformed;
    private int cacheHits;
    private int cacheMisses;
    private float totalRaycastTime;
    
    // Request structure
    private struct RaycastRequest
    {
        public Vector3 fromPosition;
        public Vector3 toPosition;
        public System.Action<bool> callback;
        public System.Action<LineOfSightResult> detailedCallback;
        public float priority;
        public float requestTime;
        public string cacheKey;
        
        public RaycastRequest(Vector3 from, Vector3 to, System.Action<bool> cb, float prio)
        {
            fromPosition = from;
            toPosition = to;
            callback = cb;
            detailedCallback = null;
            priority = prio;
            requestTime = Time.time;
            cacheKey = GenerateCacheKey(from, to);
        }
        
        public RaycastRequest(Vector3 from, Vector3 to, System.Action<LineOfSightResult> cb, float prio)
        {
            fromPosition = from;
            toPosition = to;
            callback = null;
            detailedCallback = cb;
            priority = prio;
            requestTime = Time.time;
            cacheKey = GenerateCacheKey(from, to);
        }
        
        private static string GenerateCacheKey(Vector3 from, Vector3 to)
        {
            return $"{from.x:F1},{from.y:F1},{from.z:F1}→{to.x:F1},{to.y:F1},{to.z:F1}";
        }
    }
    
    // Cached result structure
    private struct CachedRaycastResult
    {
        public bool hasLineOfSight;
        public LineOfSightResult detailedResult;
        public float timestamp;
        public Vector3 fromPosition;
        public Vector3 toPosition;
        
        public bool IsValid(float validityDuration)
        {
            return Time.time - timestamp < validityDuration;
        }
        
        public CachedRaycastResult(bool los, LineOfSightResult detailed, Vector3 from, Vector3 to)
        {
            hasLineOfSight = los;
            detailedResult = detailed;
            timestamp = Time.time;
            fromPosition = from;
            toPosition = to;
        }
    }
    
    // Properties
    public int MaxRaycastsPerFrame => currentMaxRaycasts;
    public bool EnableOptimization => enableOptimization;
    public float AverageFrameTime => averageFrameTime;
    public int QueuedRequests => raycastQueue?.Count ?? 0;
    public int CacheSize => raycastCache?.Count ?? 0;
    
    void Awake()
    {
        InitializeOptimizer();
    }
    
    void Start()
    {
        FindSystemReferences();
        StartOptimizationSystem();
    }
    
    void Update()
    {
        if (enableOptimization)
        {
            UpdatePerformanceTracking();
            UpdateAdaptivePerformance();
            TrackTransformMovement();
        }
    }
    
    void LateUpdate()
    {
        if (enableOptimization)
        {
            ProcessPerformanceStats();
        }
    }
    
    /// <summary>
    /// Initializes the raycast optimizer
    /// </summary>
    private void InitializeOptimizer()
    {
        raycastQueue = new Queue<RaycastRequest>();
        raycastCache = new Dictionary<string, CachedRaycastResult>();
        trackedTransforms = new HashSet<Transform>();
        lastKnownPositions = new Dictionary<Transform, Vector3>();
        recentRaycastTimes = new List<float>();
        
        currentMaxRaycasts = maxRaycastsPerFrame;
        averageFrameTime = targetFrameTime;
        
        if (enableOptimizationLogging)
        {
            Debug.Log("RaycastOptimizer initialized - Performance optimization system ready");
        }
    }
    
    /// <summary>
    /// Finds references to other systems
    /// </summary>
    private void FindSystemReferences()
    {
        lineOfSightManager = GetComponent<LineOfSightManager>();
        if (lineOfSightManager == null)
        {
            lineOfSightManager = FindFirstObjectByType<LineOfSightManager>();
        }
        
        if (lineOfSightManager == null)
        {
            Debug.LogError("RaycastOptimizer: LineOfSightManager not found!");
        }
        
        if (enableOptimizationLogging)
        {
            Debug.Log($"RaycastOptimizer found references - LOS Manager: {lineOfSightManager != null}");
        }
    }
    
    /// <summary>
    /// Starts the optimization system
    /// </summary>
    private void StartOptimizationSystem()
    {
        StartCoroutine(ProcessRaycastQueue());
        StartCoroutine(CleanupCache());
        
        if (enableOptimizationLogging)
        {
            Debug.Log("RaycastOptimizer: Optimization system started");
        }
    }
    
    #region Public Interface
    
    /// <summary>
    /// Requests an optimized line-of-sight check
    /// </summary>
    public void RequestLineOfSight(Vector3 fromPosition, Vector3 toPosition, System.Action<bool> callback, float priority = 1.0f)
    {
        if (!enableOptimization)
        {
            // Direct execution when optimization is disabled
            bool result = PerformDirectRaycast(fromPosition, toPosition);
            callback?.Invoke(result);
            return;
        }
        
        // Check distance culling
        if (enableDistanceCulling && Vector3.Distance(fromPosition, toPosition) > maxRaycastDistance)
        {
            callback?.Invoke(false);
            return;
        }
        
        string cacheKey = GenerateCacheKey(fromPosition, toPosition);
        
        // Check cache first
        if (enableCaching && raycastCache.ContainsKey(cacheKey))
        {
            CachedRaycastResult cached = raycastCache[cacheKey];
            if (cached.IsValid(cacheValidityDuration))
            {
                callback?.Invoke(cached.hasLineOfSight);
                cacheHits++;
                return;
            }
            else
            {
                raycastCache.Remove(cacheKey);
            }
        }
        
        // Queue for processing
        RaycastRequest request = new RaycastRequest(fromPosition, toPosition, callback, priority);
        QueueRequest(request);
        cacheMisses++;
    }
    
    /// <summary>
    /// Requests an optimized detailed line-of-sight check
    /// </summary>
    public void RequestDetailedLineOfSight(Vector3 fromPosition, Vector3 toPosition, System.Action<LineOfSightResult> callback, float priority = 1.0f)
    {
        if (!enableOptimization)
        {
            // Direct execution when optimization is disabled
            LineOfSightResult result = PerformDetailedDirectRaycast(fromPosition, toPosition);
            callback?.Invoke(result);
            return;
        }
        
        // Check distance culling
        if (enableDistanceCulling && Vector3.Distance(fromPosition, toPosition) > maxRaycastDistance)
        {
            callback?.Invoke(LineOfSightResult.Blocked(0, Vector3.zero, null, "Distance culled"));
            return;
        }
        
        string cacheKey = GenerateCacheKey(fromPosition, toPosition);
        
        // Check cache first
        if (enableCaching && raycastCache.ContainsKey(cacheKey))
        {
            CachedRaycastResult cached = raycastCache[cacheKey];
            if (cached.IsValid(cacheValidityDuration))
            {
                callback?.Invoke(cached.detailedResult);
                cacheHits++;
                return;
            }
            else
            {
                raycastCache.Remove(cacheKey);
            }
        }
        
        // Queue for processing
        RaycastRequest request = new RaycastRequest(fromPosition, toPosition, callback, priority);
        QueueRequest(request);
        cacheMisses++;
    }
    
    /// <summary>
    /// Registers a transform for movement tracking and cache invalidation
    /// </summary>
    public void RegisterTransform(Transform transform)
    {
        if (transform != null && !trackedTransforms.Contains(transform))
        {
            trackedTransforms.Add(transform);
            lastKnownPositions[transform] = transform.position;
            
            if (enableOptimizationLogging)
            {
                Debug.Log($"RaycastOptimizer: Registered transform {transform.name} for tracking");
            }
        }
    }
    
    /// <summary>
    /// Unregisters a transform from movement tracking
    /// </summary>
    public void UnregisterTransform(Transform transform)
    {
        if (transform != null)
        {
            trackedTransforms.Remove(transform);
            lastKnownPositions.Remove(transform);
            
            if (smartCacheInvalidation)
            {
                InvalidateCacheForTransform(transform);
            }
        }
    }
    
    /// <summary>
    /// Clears the raycast cache
    /// </summary>
    public void ClearCache()
    {
        raycastCache.Clear();
        if (enableOptimizationLogging)
        {
            Debug.Log("RaycastOptimizer: Cache cleared");
        }
    }
    
    /// <summary>
    /// Sets adaptive performance parameters
    /// </summary>
    public void SetPerformanceParameters(int maxRaycasts, float updateInt, float targetFPS)
    {
        maxRaycastsPerFrame = maxRaycasts;
        updateInterval = updateInt;
        targetFrameTime = 1f / targetFPS;
        currentMaxRaycasts = maxRaycasts;
        
        if (enableOptimizationLogging)
        {
            Debug.Log($"RaycastOptimizer: Performance parameters updated - Max: {maxRaycasts}, Interval: {updateInt}, Target FPS: {targetFPS}");
        }
    }
    
    #endregion
    
    #region Private Implementation
    
    /// <summary>
    /// Queues a raycast request with priority sorting
    /// </summary>
    private void QueueRequest(RaycastRequest request)
    {
        // For now, simple FIFO queue. Could implement priority queue for better optimization
        raycastQueue.Enqueue(request);
    }
    
    /// <summary>
    /// Generates a cache key for position pair
    /// </summary>
    private string GenerateCacheKey(Vector3 fromPosition, Vector3 toPosition)
    {
        // Round to reduce cache fragmentation
        Vector3 roundedFrom = new Vector3(
            Mathf.Round(fromPosition.x * 2f) / 2f,
            Mathf.Round(fromPosition.y * 2f) / 2f,
            Mathf.Round(fromPosition.z * 2f) / 2f);
        
        Vector3 roundedTo = new Vector3(
            Mathf.Round(toPosition.x * 2f) / 2f,
            Mathf.Round(toPosition.y * 2f) / 2f,
            Mathf.Round(toPosition.z * 2f) / 2f);
        
        return $"{roundedFrom.x:F1},{roundedFrom.y:F1},{roundedFrom.z:F1}→{roundedTo.x:F1},{roundedTo.y:F1},{roundedTo.z:F1}";
    }
    
    /// <summary>
    /// Performs a direct raycast without optimization
    /// </summary>
    private bool PerformDirectRaycast(Vector3 fromPosition, Vector3 toPosition)
    {
        if (lineOfSightManager != null)
        {
            return lineOfSightManager.HasLineOfSight(fromPosition, toPosition);
        }
        
        // Fallback raycast
        Vector3 direction = (toPosition - fromPosition).normalized;
        float distance = Vector3.Distance(fromPosition, toPosition);
        
        return !Physics.Raycast(fromPosition, direction, distance, lineOfSightManager?.ObstacleLayerMask ?? -1);
    }
    
    /// <summary>
    /// Performs a detailed direct raycast without optimization
    /// </summary>
    private LineOfSightResult PerformDetailedDirectRaycast(Vector3 fromPosition, Vector3 toPosition)
    {
        if (lineOfSightManager != null)
        {
            return lineOfSightManager.GetLineOfSightDetails(fromPosition, toPosition);
        }
        
        // Fallback raycast
        Vector3 direction = (toPosition - fromPosition).normalized;
        float distance = Vector3.Distance(fromPosition, toPosition);
        
        RaycastHit hit;
        if (Physics.Raycast(fromPosition, direction, out hit, distance, lineOfSightManager?.ObstacleLayerMask ?? -1))
        {
            return LineOfSightResult.Blocked(hit.distance, hit.point, hit.collider.gameObject, "Direct raycast blocked");
        }
        else
        {
            return LineOfSightResult.Clear(distance);
        }
    }
    
    /// <summary>
    /// Updates performance tracking metrics
    /// </summary>
    private void UpdatePerformanceTracking()
    {
        if (frameStartTime == 0)
        {
            frameStartTime = Time.realtimeSinceStartup;
        }
        
        float currentFrameTime = Time.unscaledDeltaTime;
        
        // Update rolling average
        averageFrameTime = Mathf.Lerp(averageFrameTime, currentFrameTime, 0.1f);
        
        // Reset frame counters
        raycastsThisFrame = 0;
    }
    
    /// <summary>
    /// Updates adaptive performance settings
    /// </summary>
    private void UpdateAdaptivePerformance()
    {
        if (!enableAdaptivePerformance) return;
        
        // Adjust raycast limits based on performance
        if (averageFrameTime > performanceThreshold)
        {
            // Performance is poor, reduce raycasts
            currentMaxRaycasts = Mathf.Max(minRaycastsPerFrame, currentMaxRaycasts - 1);
        }
        else if (averageFrameTime < targetFrameTime * 0.8f)
        {
            // Performance is good, increase raycasts
            currentMaxRaycasts = Mathf.Min(maxAdaptiveRaycasts, currentMaxRaycasts + 1);
        }
        
        // Clamp to configured limits
        currentMaxRaycasts = Mathf.Clamp(currentMaxRaycasts, minRaycastsPerFrame, maxRaycastsPerFrame);
    }
    
    /// <summary>
    /// Tracks transform movement for smart cache invalidation
    /// </summary>
    private void TrackTransformMovement()
    {
        if (!smartCacheInvalidation) return;
        
        List<Transform> movedTransforms = new List<Transform>();
        
        foreach (Transform transform in trackedTransforms)
        {
            if (transform == null)
            {
                movedTransforms.Add(transform);
                continue;
            }
            
            if (lastKnownPositions.ContainsKey(transform))
            {
                Vector3 lastPos = lastKnownPositions[transform];
                Vector3 currentPos = transform.position;
                
                if (Vector3.Distance(lastPos, currentPos) > 0.1f)
                {
                    lastKnownPositions[transform] = currentPos;
                    InvalidateCacheForTransform(transform);
                }
            }
            else
            {
                lastKnownPositions[transform] = transform.position;
            }
        }
        
        // Clean up null transforms
        foreach (Transform nullTransform in movedTransforms)
        {
            trackedTransforms.Remove(nullTransform);
            lastKnownPositions.Remove(nullTransform);
        }
    }
    
    /// <summary>
    /// Invalidates cache entries involving a specific transform
    /// </summary>
    private void InvalidateCacheForTransform(Transform transform)
    {
        if (transform == null) return;
        
        Vector3 pos = transform.position;
        List<string> keysToRemove = new List<string>();
        
        foreach (var kvp in raycastCache)
        {
            CachedRaycastResult result = kvp.Value;
            
            // Check if this cache entry involves the moved transform
            if (Vector3.Distance(result.fromPosition, pos) < 2f || Vector3.Distance(result.toPosition, pos) < 2f)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (string key in keysToRemove)
        {
            raycastCache.Remove(key);
        }
        
        if (enableOptimizationLogging && keysToRemove.Count > 0)
        {
            Debug.Log($"RaycastOptimizer: Invalidated {keysToRemove.Count} cache entries for {transform.name}");
        }
    }
    
    /// <summary>
    /// Processes performance statistics
    /// </summary>
    private void ProcessPerformanceStats()
    {
        if (showPerformanceStats && Time.time % 1f < Time.unscaledDeltaTime)
        {
            float hitRate = (cacheHits + cacheMisses) > 0 ? (float)cacheHits / (cacheHits + cacheMisses) : 0f;
            float avgRaycastTime = totalRaycastsPerformed > 0 ? totalRaycastTime / totalRaycastsPerformed : 0f;
            
            Debug.Log($"RaycastOptimizer Stats - Queue: {raycastQueue.Count}, Cache: {raycastCache.Count}, " +
                     $"Hit Rate: {hitRate:P1}, Avg Frame: {averageFrameTime * 1000:F1}ms, " +
                     $"Max Raycasts: {currentMaxRaycasts}, Avg Raycast: {avgRaycastTime * 1000:F2}ms");
        }
    }
    
    #endregion
    
    #region Coroutines
    
    /// <summary>
    /// Coroutine that processes the raycast queue
    /// </summary>
    private IEnumerator ProcessRaycastQueue()
    {
        while (true)
        {
            int raycastsThisBatch = 0;
            float batchStartTime = Time.realtimeSinceStartup;
            
            while (raycastQueue.Count > 0 && raycastsThisBatch < currentMaxRaycasts)
            {
                RaycastRequest request = raycastQueue.Dequeue();
                
                // Check if request has timed out
                if (Time.time - request.requestTime > 1f)
                {
                    continue; // Skip timed out requests
                }
                
                float raycastStartTime = Time.realtimeSinceStartup;
                
                // Perform the raycast
                if (request.detailedCallback != null)
                {
                    LineOfSightResult result = PerformDetailedDirectRaycast(request.fromPosition, request.toPosition);
                    
                    // Cache the result
                    if (enableCaching)
                    {
                        CacheResult(request.cacheKey, result.isBlocked, result, request.fromPosition, request.toPosition);
                    }
                    
                    request.detailedCallback.Invoke(result);
                }
                else if (request.callback != null)
                {
                    bool result = PerformDirectRaycast(request.fromPosition, request.toPosition);
                    
                    // Cache the result
                    if (enableCaching)
                    {
                        CacheResult(request.cacheKey, result, LineOfSightResult.Clear(0), request.fromPosition, request.toPosition);
                    }
                    
                    request.callback.Invoke(result);
                }
                
                // Track performance
                if (profileRaycastTimes)
                {
                    float raycastTime = Time.realtimeSinceStartup - raycastStartTime;
                    totalRaycastTime += raycastTime;
                    recentRaycastTimes.Add(raycastTime);
                    
                    if (recentRaycastTimes.Count > 100)
                    {
                        recentRaycastTimes.RemoveAt(0);
                    }
                }
                
                totalRaycastsPerformed++;
                raycastsThisBatch++;
                raycastsThisFrame++;
            }
            
            yield return new WaitForSeconds(updateInterval);
        }
    }
    
    /// <summary>
    /// Coroutine that periodically cleans up the cache
    /// </summary>
    private IEnumerator CleanupCache()
    {
        while (true)
        {
            yield return new WaitForSeconds(cacheValidityDuration);
            
            if (!enableCaching) continue;
            
            List<string> expiredKeys = new List<string>();
            
            foreach (var kvp in raycastCache)
            {
                if (!kvp.Value.IsValid(cacheValidityDuration))
                {
                    expiredKeys.Add(kvp.Key);
                }
            }
            
            foreach (string key in expiredKeys)
            {
                raycastCache.Remove(key);
            }
            
            // Also limit cache size
            if (raycastCache.Count > maxCacheSize)
            {
                int toRemove = raycastCache.Count - maxCacheSize;
                List<string> oldestKeys = new List<string>();
                
                foreach (var kvp in raycastCache)
                {
                    oldestKeys.Add(kvp.Key);
                    if (oldestKeys.Count >= toRemove) break;
                }
                
                foreach (string key in oldestKeys)
                {
                    raycastCache.Remove(key);
                }
            }
            
            if (enableOptimizationLogging && expiredKeys.Count > 0)
            {
                Debug.Log($"RaycastOptimizer: Cleaned up {expiredKeys.Count} expired cache entries");
            }
        }
    }
    
    #endregion
    
    /// <summary>
    /// Caches a raycast result
    /// </summary>
    private void CacheResult(string cacheKey, bool hasLineOfSight, LineOfSightResult detailedResult, Vector3 fromPos, Vector3 toPos)
    {
        if (raycastCache.Count >= maxCacheSize)
        {
            // Remove oldest entry
            string oldestKey = "";
            float oldestTime = float.MaxValue;
            
            foreach (var kvp in raycastCache)
            {
                if (kvp.Value.timestamp < oldestTime)
                {
                    oldestTime = kvp.Value.timestamp;
                    oldestKey = kvp.Key;
                }
            }
            
            if (!string.IsNullOrEmpty(oldestKey))
            {
                raycastCache.Remove(oldestKey);
            }
        }
        
        raycastCache[cacheKey] = new CachedRaycastResult(hasLineOfSight, detailedResult, fromPos, toPos);
    }
    
    /// <summary>
    /// Gets optimization statistics for debugging
    /// </summary>
    public string GetOptimizationStats()
    {
        float hitRate = (cacheHits + cacheMisses) > 0 ? (float)cacheHits / (cacheHits + cacheMisses) : 0f;
        
        return $"Optimization Stats - Queue: {raycastQueue.Count}, Cache: {raycastCache.Count}, " +
               $"Hit Rate: {hitRate:P1}, Total Raycasts: {totalRaycastsPerformed}, " +
               $"Avg Frame Time: {averageFrameTime * 1000:F1}ms, Current Max: {currentMaxRaycasts}";
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Clear all data structures
        raycastQueue?.Clear();
        raycastCache?.Clear();
        trackedTransforms?.Clear();
        lastKnownPositions?.Clear();
        recentRaycastTimes?.Clear();
    }
}