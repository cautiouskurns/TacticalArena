using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Performance optimization and monitoring system for the tactical arena.
/// Tracks frame rates, optimizes rendering, and provides performance analytics.
/// </summary>
public class PerformanceOptimizer : MonoBehaviour
{
    [Header("Performance Monitoring")]
    [SerializeField] private bool enablePerformanceMonitoring = true;
    [SerializeField] private float monitoringInterval = 1.0f;
    [SerializeField] private int frameRateHistorySize = 60;
    [SerializeField] private bool displayPerformanceOverlay = false;
    
    [Header("Target Performance Settings")]
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private float targetFrameTime = 16.67f; // 60 FPS = 16.67ms
    [SerializeField] private bool enableVSync = true;
    [SerializeField] private bool enableAdaptivePerformance = true;
    
    [Header("Optimization Settings")]
    [SerializeField] private bool enableBatching = true;
    [SerializeField] private bool optimizeShadows = true;
    [SerializeField] private bool enableCulling = true;
    [SerializeField] private bool enableLODOptimization = false;
    
    [Header("Quality Management")]
    [SerializeField] private bool enableDynamicQuality = true;
    [SerializeField] private float qualityAdjustmentThreshold = 45.0f; // FPS threshold for quality reduction
    [SerializeField] private float qualityRecoveryThreshold = 55.0f; // FPS threshold for quality increase
    [SerializeField] private int qualityLevel = 3; // 0-5, where 5 is highest quality
    
    [Header("Memory Management")]
    [SerializeField] private bool enableMemoryOptimization = true;
    [SerializeField] private float memoryCleanupInterval = 30.0f;
    [SerializeField] private bool enableTextureStreaming = false;
    [SerializeField] private long targetMemoryUsage = 512 * 1024 * 1024; // 512 MB
    
    [Header("Rendering Optimization")]
    [SerializeField] private bool enableFrustumCulling = true;
    [SerializeField] private bool enableOcclusionCulling = false;
    [SerializeField] private float cullingDistance = 50.0f;
    [SerializeField] private int maxDrawCalls = 200;
    
    // Performance tracking data
    private Queue<float> frameTimeHistory;
    private Queue<float> frameRateHistory;
    private float currentFrameRate = 0f;
    private float averageFrameRate = 0f;
    private float currentFrameTime = 0f;
    private float averageFrameTime = 0f;
    
    // Performance statistics
    private int drawCallCount = 0;
    private int triangleCount = 0;
    private long memoryUsage = 0;
    private float lastMonitoringTime = 0f;
    private float lastMemoryCleanupTime = 0f;
    
    // Dynamic quality management
    private int originalQualityLevel;
    private float qualityAdjustmentCooldown = 5.0f;
    private float lastQualityAdjustment = 0f;
    
    // Component references
    private Camera mainCamera;
    private Light[] sceneLights;
    private Renderer[] sceneRenderers;
    
    // Optimization state
    private bool isOptimizationActive = false;
    private Dictionary<string, bool> optimizationFlags;
    
    // Events for performance changes
    public System.Action<float> OnFrameRateChanged;
    public System.Action<int> OnQualityLevelChanged;
    public System.Action<PerformanceMetrics> OnPerformanceMetricsUpdated;
    
    // Public properties
    public float CurrentFrameRate => currentFrameRate;
    public float AverageFrameRate => averageFrameRate;
    public float CurrentFrameTime => currentFrameTime;
    public int DrawCallCount => drawCallCount;
    public long MemoryUsage => memoryUsage;
    public int CurrentQualityLevel => qualityLevel;
    public bool OptimizationActive => isOptimizationActive;
    
    void Awake()
    {
        InitializePerformanceSystem();
    }
    
    void Start()
    {
        SetupPerformanceOptimization();
        StartPerformanceMonitoring();
    }
    
    void Update()
    {
        if (enablePerformanceMonitoring)
        {
            UpdatePerformanceMetrics();
            
            if (Time.time - lastMonitoringTime >= monitoringInterval)
            {
                AnalyzePerformance();
                lastMonitoringTime = Time.time;
            }
        }
        
        if (enableMemoryOptimization && Time.time - lastMemoryCleanupTime >= memoryCleanupInterval)
        {
            OptimizeMemoryUsage();
            lastMemoryCleanupTime = Time.time;
        }
    }
    
    void OnGUI()
    {
        if (displayPerformanceOverlay)
        {
            DrawPerformanceOverlay();
        }
    }
    
    /// <summary>
    /// Initializes the performance optimization system
    /// </summary>
    private void InitializePerformanceSystem()
    {
        frameTimeHistory = new Queue<float>();
        frameRateHistory = new Queue<float>();
        optimizationFlags = new Dictionary<string, bool>();
        
        originalQualityLevel = QualitySettings.GetQualityLevel();
        qualityLevel = originalQualityLevel;
        
        Debug.Log("PerformanceOptimizer: System initialized");
    }
    
    /// <summary>
    /// Sets up initial performance optimization
    /// </summary>
    private void SetupPerformanceOptimization()
    {
        // Find component references
        mainCamera = Camera.main;
        sceneLights = FindObjectsOfType<Light>();
        sceneRenderers = FindObjectsOfType<Renderer>();
        
        // Apply target frame rate
        Application.targetFrameRate = targetFrameRate;
        
        // Configure VSync
        QualitySettings.vSyncCount = enableVSync ? 1 : 0;
        
        // Apply batching settings
        if (enableBatching)
        {
            EnableStaticBatching();
            EnableDynamicBatching();
        }
        
        // Configure shadow optimization
        if (optimizeShadows)
        {
            OptimizeShadowSettings();
        }
        
        // Setup culling
        if (enableCulling)
        {
            SetupCullingOptimization();
        }
        
        isOptimizationActive = true;
        Debug.Log($"PerformanceOptimizer: Initial optimization applied, target FPS: {targetFrameRate}");
    }
    
    /// <summary>
    /// Starts performance monitoring coroutines
    /// </summary>
    private void StartPerformanceMonitoring()
    {
        if (enablePerformanceMonitoring)
        {
            StartCoroutine(MonitorPerformanceCoroutine());
        }
    }
    
    /// <summary>
    /// Updates real-time performance metrics
    /// </summary>
    private void UpdatePerformanceMetrics()
    {
        // Calculate frame rate and frame time
        currentFrameTime = Time.unscaledDeltaTime * 1000f; // Convert to milliseconds
        currentFrameRate = 1f / Time.unscaledDeltaTime;
        
        // Update history
        frameTimeHistory.Enqueue(currentFrameTime);
        frameRateHistory.Enqueue(currentFrameRate);
        
        // Maintain history size
        if (frameTimeHistory.Count > frameRateHistorySize)
        {
            frameTimeHistory.Dequeue();
        }
        
        if (frameRateHistory.Count > frameRateHistorySize)
        {
            frameRateHistory.Dequeue();
        }
        
        // Calculate averages
        CalculateAverageMetrics();
        
        // Update render statistics
        UpdateRenderStatistics();
    }
    
    /// <summary>
    /// Calculates average performance metrics
    /// </summary>
    private void CalculateAverageMetrics()
    {
        if (frameRateHistory.Count > 0)
        {
            float totalFrameRate = 0f;
            foreach (float frameRate in frameRateHistory)
            {
                totalFrameRate += frameRate;
            }
            averageFrameRate = totalFrameRate / frameRateHistory.Count;
        }
        
        if (frameTimeHistory.Count > 0)
        {
            float totalFrameTime = 0f;
            foreach (float frameTime in frameTimeHistory)
            {
                totalFrameTime += frameTime;
            }
            averageFrameTime = totalFrameTime / frameTimeHistory.Count;
        }
    }
    
    /// <summary>
    /// Updates rendering statistics
    /// </summary>
    private void UpdateRenderStatistics()
    {
        // Get draw call count (approximation based on active renderers)
        drawCallCount = 0;
        triangleCount = 0;
        
        foreach (Renderer renderer in sceneRenderers)
        {
            if (renderer != null && renderer.isVisible && renderer.enabled)
            {
                drawCallCount++;
                
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.mesh != null)
                {
                    triangleCount += meshFilter.mesh.triangles.Length / 3;
                }
            }
        }
        
        // Get memory usage
        memoryUsage = System.GC.GetTotalMemory(false);
    }
    
    /// <summary>
    /// Analyzes performance and triggers optimizations
    /// </summary>
    private void AnalyzePerformance()
    {
        PerformanceMetrics metrics = GetCurrentMetrics();
        OnPerformanceMetricsUpdated?.Invoke(metrics);
        
        // Check if dynamic quality adjustment is needed
        if (enableDynamicQuality && Time.time - lastQualityAdjustment >= qualityAdjustmentCooldown)
        {
            CheckQualityAdjustment();
        }
        
        // Check for performance warnings
        CheckPerformanceWarnings();
        
        // Trigger adaptive optimizations
        if (enableAdaptivePerformance)
        {
            ApplyAdaptiveOptimizations();
        }
    }
    
    /// <summary>
    /// Checks if quality level adjustment is needed
    /// </summary>
    private void CheckQualityAdjustment()
    {
        if (averageFrameRate < qualityAdjustmentThreshold && qualityLevel > 0)
        {
            // Reduce quality
            qualityLevel--;
            ApplyQualityLevel(qualityLevel);
            lastQualityAdjustment = Time.time;
            
            Debug.Log($"PerformanceOptimizer: Quality reduced to level {qualityLevel} due to low FPS ({averageFrameRate:F1})");
            OnQualityLevelChanged?.Invoke(qualityLevel);
        }
        else if (averageFrameRate > qualityRecoveryThreshold && qualityLevel < originalQualityLevel)
        {
            // Increase quality
            qualityLevel++;
            ApplyQualityLevel(qualityLevel);
            lastQualityAdjustment = Time.time;
            
            Debug.Log($"PerformanceOptimizer: Quality increased to level {qualityLevel} due to good FPS ({averageFrameRate:F1})");
            OnQualityLevelChanged?.Invoke(qualityLevel);
        }
    }
    
    /// <summary>
    /// Checks for performance warnings
    /// </summary>
    private void CheckPerformanceWarnings()
    {
        if (averageFrameRate < targetFrameRate * 0.8f)
        {
            Debug.LogWarning($"PerformanceOptimizer: Frame rate below target ({averageFrameRate:F1} < {targetFrameRate})");
        }
        
        if (drawCallCount > maxDrawCalls)
        {
            Debug.LogWarning($"PerformanceOptimizer: Draw call count high ({drawCallCount} > {maxDrawCalls})");
        }
        
        if (memoryUsage > targetMemoryUsage)
        {
            Debug.LogWarning($"PerformanceOptimizer: Memory usage high ({memoryUsage / (1024 * 1024)} MB > {targetMemoryUsage / (1024 * 1024)} MB)");
        }
    }
    
    /// <summary>
    /// Applies adaptive performance optimizations
    /// </summary>
    private void ApplyAdaptiveOptimizations()
    {
        // Disable expensive effects if performance is poor
        if (averageFrameRate < targetFrameRate * 0.7f)
        {
            if (!optimizationFlags.ContainsKey("ReducedEffects"))
            {
                ReduceVisualEffects();
                optimizationFlags["ReducedEffects"] = true;
            }
        }
        else if (averageFrameRate > targetFrameRate * 0.9f)
        {
            if (optimizationFlags.ContainsKey("ReducedEffects"))
            {
                RestoreVisualEffects();
                optimizationFlags.Remove("ReducedEffects");
            }
        }
    }
    
    /// <summary>
    /// Applies a specific quality level
    /// </summary>
    private void ApplyQualityLevel(int level)
    {
        QualitySettings.SetQualityLevel(level, true);
        
        // Apply custom quality adjustments based on level
        switch (level)
        {
            case 0: // Lowest quality
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.antiAliasing = 0;
                break;
            case 1: // Low quality
                QualitySettings.shadows = ShadowQuality.HardOnly;
                QualitySettings.antiAliasing = 0;
                break;
            case 2: // Medium quality
                QualitySettings.shadows = ShadowQuality.HardOnly;
                QualitySettings.antiAliasing = 2;
                break;
            case 3: // High quality
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.antiAliasing = 4;
                break;
            default: // Ultra quality
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.antiAliasing = 8;
                break;
        }
    }
    
    /// <summary>
    /// Enables static batching for better performance
    /// </summary>
    private void EnableStaticBatching()
    {
        // Static batching is enabled in PlayerSettings by default
        // This method could mark appropriate objects as static
        GameObject[] staticObjects = GameObject.FindGameObjectsWithTag("Static");
        foreach (GameObject obj in staticObjects)
        {
            StaticBatchingUtility.Combine(obj);
        }
    }
    
    /// <summary>
    /// Enables dynamic batching settings
    /// </summary>
    private void EnableDynamicBatching()
    {
        // Dynamic batching is controlled in PlayerSettings
        // This method could optimize objects for dynamic batching
        optimizationFlags["DynamicBatching"] = true;
    }
    
    /// <summary>
    /// Optimizes shadow rendering settings
    /// </summary>
    private void OptimizeShadowSettings()
    {
        QualitySettings.shadowResolution = ShadowResolution.Medium;
        QualitySettings.shadowDistance = 25f;
        QualitySettings.shadowCascades = 2;
        
        // Optimize individual lights
        foreach (Light light in sceneLights)
        {
            if (light != null && light.type == LightType.Directional)
            {
                light.shadowStrength = 0.5f;
                light.shadowBias = 0.05f;
            }
        }
        
        optimizationFlags["ShadowOptimization"] = true;
        Debug.Log("PerformanceOptimizer: Shadow settings optimized");
    }
    
    /// <summary>
    /// Sets up culling optimization
    /// </summary>
    private void SetupCullingOptimization()
    {
        if (mainCamera != null)
        {
            if (enableFrustumCulling)
            {
                mainCamera.useOcclusionCulling = enableOcclusionCulling;
            }
            
            // Set appropriate culling distances
            float[] distances = new float[32];
            for (int i = 0; i < distances.Length; i++)
            {
                distances[i] = cullingDistance;
            }
            mainCamera.layerCullDistances = distances;
        }
        
        optimizationFlags["CullingOptimization"] = true;
    }
    
    /// <summary>
    /// Reduces visual effects for better performance
    /// </summary>
    private void ReduceVisualEffects()
    {
        // Disable or reduce particle effects
        ParticleSystem[] particles = FindObjectsOfType<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            if (ps != null)
            {
                var main = ps.main;
                main.maxParticles = Mathf.Max(10, main.maxParticles / 2);
            }
        }
        
        Debug.Log("PerformanceOptimizer: Visual effects reduced for performance");
    }
    
    /// <summary>
    /// Restores visual effects when performance improves
    /// </summary>
    private void RestoreVisualEffects()
    {
        // Restore particle effects
        ParticleSystem[] particles = FindObjectsOfType<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            if (ps != null)
            {
                var main = ps.main;
                main.maxParticles = main.maxParticles * 2;
            }
        }
        
        Debug.Log("PerformanceOptimizer: Visual effects restored");
    }
    
    /// <summary>
    /// Optimizes memory usage
    /// </summary>
    private void OptimizeMemoryUsage()
    {
        // Force garbage collection
        System.GC.Collect();
        
        // Unload unused assets
        Resources.UnloadUnusedAssets();
        
        // Update memory usage
        memoryUsage = System.GC.GetTotalMemory(false);
        
        Debug.Log($"PerformanceOptimizer: Memory optimized, current usage: {memoryUsage / (1024 * 1024)} MB");
    }
    
    /// <summary>
    /// Performance monitoring coroutine
    /// </summary>
    private IEnumerator MonitorPerformanceCoroutine()
    {
        while (enablePerformanceMonitoring)
        {
            yield return new WaitForSeconds(monitoringInterval);
            
            // Notify frame rate changes
            OnFrameRateChanged?.Invoke(averageFrameRate);
        }
    }
    
    /// <summary>
    /// Draws performance overlay GUI
    /// </summary>
    private void DrawPerformanceOverlay()
    {
        GUI.Box(new Rect(10, 10, 250, 150), "Performance Metrics");
        
        GUI.Label(new Rect(15, 30, 240, 20), $"FPS: {currentFrameRate:F1} (Avg: {averageFrameRate:F1})");
        GUI.Label(new Rect(15, 50, 240, 20), $"Frame Time: {currentFrameTime:F2}ms");
        GUI.Label(new Rect(15, 70, 240, 20), $"Draw Calls: {drawCallCount}");
        GUI.Label(new Rect(15, 90, 240, 20), $"Triangles: {triangleCount}");
        GUI.Label(new Rect(15, 110, 240, 20), $"Memory: {memoryUsage / (1024 * 1024)}MB");
        GUI.Label(new Rect(15, 130, 240, 20), $"Quality Level: {qualityLevel}");
        
        // Performance status color
        Color statusColor = averageFrameRate >= targetFrameRate ? Color.green : 
                           averageFrameRate >= targetFrameRate * 0.8f ? Color.yellow : Color.red;
        
        Color originalColor = GUI.color;
        GUI.color = statusColor;
        GUI.Label(new Rect(15, 145, 240, 20), $"Status: {(averageFrameRate >= targetFrameRate ? "Good" : "Poor")}");
        GUI.color = originalColor;
    }
    
    /// <summary>
    /// Gets current performance metrics
    /// </summary>
    public PerformanceMetrics GetCurrentMetrics()
    {
        return new PerformanceMetrics
        {
            currentFrameRate = currentFrameRate,
            averageFrameRate = averageFrameRate,
            currentFrameTime = currentFrameTime,
            averageFrameTime = averageFrameTime,
            drawCallCount = drawCallCount,
            triangleCount = triangleCount,
            memoryUsage = memoryUsage,
            qualityLevel = qualityLevel,
            targetFrameRate = targetFrameRate,
            optimizationActive = isOptimizationActive
        };
    }
    
    /// <summary>
    /// Enables or disables performance monitoring
    /// </summary>
    public void SetPerformanceMonitoring(bool enabled)
    {
        enablePerformanceMonitoring = enabled;
        if (enabled)
        {
            StartPerformanceMonitoring();
        }
        else
        {
            StopAllCoroutines();
        }
    }
    
    /// <summary>
    /// Manually triggers performance optimization
    /// </summary>
    public void OptimizePerformance()
    {
        SetupPerformanceOptimization();
        OptimizeMemoryUsage();
        Debug.Log("PerformanceOptimizer: Manual optimization triggered");
    }
    
    /// <summary>
    /// Resets performance settings to defaults
    /// </summary>
    public void ResetPerformanceSettings()
    {
        QualitySettings.SetQualityLevel(originalQualityLevel, true);
        Application.targetFrameRate = targetFrameRate;
        qualityLevel = originalQualityLevel;
        optimizationFlags.Clear();
        
        Debug.Log("PerformanceOptimizer: Settings reset to defaults");
    }
}

/// <summary>
/// Structure containing comprehensive performance metrics
/// </summary>
[System.Serializable]
public struct PerformanceMetrics
{
    public float currentFrameRate;
    public float averageFrameRate;
    public float currentFrameTime;
    public float averageFrameTime;
    public int drawCallCount;
    public int triangleCount;
    public long memoryUsage;
    public int qualityLevel;
    public int targetFrameRate;
    public bool optimizationActive;
    
    public override string ToString()
    {
        return $"Performance - FPS: {averageFrameRate:F1}/{targetFrameRate}, Draws: {drawCallCount}, Memory: {memoryUsage / (1024 * 1024)}MB, Quality: {qualityLevel}";
    }
}