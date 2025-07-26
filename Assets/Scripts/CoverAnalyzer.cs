using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tactical analysis system for cover opportunities and positioning strategy.
/// Analyzes battlefield terrain and obstacles to provide AI and player feedback
/// on tactical positioning, cover effectiveness, and strategic movement options.
/// </summary>
public class CoverAnalyzer : MonoBehaviour
{
    [Header("Analysis Configuration")]
    [SerializeField] private LayerMask obstacleLayerMask = -1;
    [SerializeField] private LayerMask unitLayerMask = -1;
    [SerializeField] private float coverAnalysisRadius = 5f;
    [SerializeField] private float minimumCoverHeight = 0.5f;
    [SerializeField] private bool enableContinuousAnalysis = true;
    
    [Header("Cover Quality Settings")]
    [SerializeField] private float fullCoverThreshold = 0.8f;
    [SerializeField] private float partialCoverThreshold = 0.4f;
    [SerializeField] private int coverAnalysisRays = 8;
    [SerializeField] private float coverRaySpread = 45f;
    
    [Header("Tactical Positioning")]
    [SerializeField] private bool analyzeFlankingOpportunities = true;
    [SerializeField] private bool analyzeCrossfire = true;
    [SerializeField] private bool analyzeEscapeRoutes = true;
    [SerializeField] private float flankingDistance = 3f;
    [SerializeField] private float crossfireAngle = 90f;
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showCoverAnalysis = false;
    [SerializeField] private bool showFlankingRoutes = false;
    [SerializeField] private bool showEscapeRoutes = false;
    [SerializeField] private Color fullCoverColor = Color.green;
    [SerializeField] private Color partialCoverColor = Color.yellow;
    [SerializeField] private Color noCoverColor = Color.red;
    
    [Header("Performance Settings")]
    [SerializeField] private float analysisUpdateInterval = 0.5f;
    [SerializeField] private int maxAnalysisPerFrame = 5;
    [SerializeField] private bool enableCaching = true;
    [SerializeField] private float cacheValidityDuration = 2f;
    
    // System references
    private LineOfSightManager lineOfSightManager;
    private GridManager gridManager;
    
    // Analysis state
    private Dictionary<Vector3, CoverAnalysisResult> coverCache;
    private Dictionary<Vector3, TacticalAnalysisResult> tacticalCache;
    private List<Vector3> knownCoverPositions;
    private List<TacticalPosition> tacticalPositions;
    
    // Analysis tracking
    private float lastAnalysisTime;
    private Queue<Vector3> analysisQueue;
    
    // Properties
    public float CoverAnalysisRadius => coverAnalysisRadius;
    public bool EnableContinuousAnalysis => enableContinuousAnalysis;
    
    // Data structures
    [System.Serializable]
    public struct CoverAnalysisResult
    {
        public Vector3 position;
        public CoverQuality coverQuality;
        public float coverPercentage;
        public Vector3[] protectedDirections;
        public Vector3[] exposedDirections;
        public GameObject[] coverObjects;
        public float analysisTime;
        
        public bool IsValid(float validityDuration)
        {
            return Time.time - analysisTime < validityDuration;
        }
    }
    
    [System.Serializable]
    public struct TacticalAnalysisResult
    {
        public Vector3 position;
        public float tacticalValue;
        public FlankingOpportunity[] flankingOpportunities;
        public CrossfirePosition[] crossfirePositions;
        public EscapeRoute[] escapeRoutes;
        public float analysisTime;
        
        public bool IsValid(float validityDuration)
        {
            return Time.time - analysisTime < validityDuration;
        }
    }
    
    [System.Serializable]
    public struct FlankingOpportunity
    {
        public Vector3 flankingPosition;
        public Vector3 targetPosition;
        public float effectiveness;
        public float distance;
        public bool requiresMovement;
    }
    
    [System.Serializable]
    public struct CrossfirePosition
    {
        public Vector3 position;
        public Vector3[] targetPositions;
        public float crossfireAngle;
        public float effectiveness;
    }
    
    [System.Serializable]
    public struct EscapeRoute
    {
        public Vector3 startPosition;
        public Vector3 endPosition;
        public Vector3[] waypoints;
        public float safety;
        public float distance;
    }
    
    [System.Serializable]
    public struct TacticalPosition
    {
        public Vector3 position;
        public float tacticalScore;
        public CoverQuality coverQuality;
        public bool hasFlankingOpportunity;
        public bool isChokepointControl;
        public bool hasEscapeRoutes;
    }
    
    public enum CoverQuality
    {
        None = 0,
        Light = 1,
        Partial = 2,
        Heavy = 3,
        Full = 4
    }
    
    void Awake()
    {
        InitializeCoverAnalyzer();
    }
    
    void Start()
    {
        FindSystemReferences();
        StartAnalysisSystem();
    }
    
    void Update()
    {
        if (enableContinuousAnalysis && Time.time - lastAnalysisTime >= analysisUpdateInterval)
        {
            UpdateContinuousAnalysis();
            lastAnalysisTime = Time.time;
        }
    }
    
    /// <summary>
    /// Initializes the cover analyzer
    /// </summary>
    private void InitializeCoverAnalyzer()
    {
        coverCache = new Dictionary<Vector3, CoverAnalysisResult>();
        tacticalCache = new Dictionary<Vector3, TacticalAnalysisResult>();
        knownCoverPositions = new List<Vector3>();
        tacticalPositions = new List<TacticalPosition>();
        analysisQueue = new Queue<Vector3>();
        
        Debug.Log("CoverAnalyzer initialized - Tactical analysis system ready");
    }
    
    /// <summary>
    /// Finds references to other systems
    /// </summary>
    private void FindSystemReferences()
    {
        lineOfSightManager = FindFirstObjectByType<LineOfSightManager>();
        gridManager = FindFirstObjectByType<GridManager>();
        
        if (lineOfSightManager == null)
        {
            Debug.LogWarning("CoverAnalyzer: LineOfSightManager not found - cover analysis may be limited");
        }
        
        if (gridManager == null)
        {
            Debug.LogWarning("CoverAnalyzer: GridManager not found - grid-based analysis unavailable");
        }
        
        Debug.Log($"CoverAnalyzer found references - LOS Manager: {lineOfSightManager != null}, Grid: {gridManager != null}");
    }
    
    /// <summary>
    /// Starts the analysis system
    /// </summary>
    private void StartAnalysisSystem()
    {
        // Perform initial battlefield analysis
        if (gridManager != null)
        {
            AnalyzeBattlefield();
        }
        
        Debug.Log("CoverAnalyzer: Analysis system started");
    }
    
    #region Public Analysis Interface
    
    /// <summary>
    /// Analyzes cover quality at a specific position
    /// </summary>
    public CoverAnalysisResult AnalyzeCoverAtPosition(Vector3 position)
    {
        Vector3 gridPos = RoundToGrid(position);
        
        // Check cache first
        if (enableCaching && coverCache.ContainsKey(gridPos))
        {
            CoverAnalysisResult cached = coverCache[gridPos];
            if (cached.IsValid(cacheValidityDuration))
            {
                return cached;
            }
            else
            {
                coverCache.Remove(gridPos);
            }
        }
        
        // Perform analysis
        CoverAnalysisResult result = PerformCoverAnalysis(position);
        
        // Cache result
        if (enableCaching)
        {
            coverCache[gridPos] = result;
        }
        
        return result;
    }
    
    /// <summary>
    /// Analyzes tactical value of a position including flanking and crossfire opportunities
    /// </summary>
    public TacticalAnalysisResult AnalyzeTacticalPosition(Vector3 position)
    {
        Vector3 gridPos = RoundToGrid(position);
        
        // Check cache first
        if (enableCaching && tacticalCache.ContainsKey(gridPos))
        {
            TacticalAnalysisResult cached = tacticalCache[gridPos];
            if (cached.IsValid(cacheValidityDuration))
            {
                return cached;
            }
            else
            {
                tacticalCache.Remove(gridPos);
            }
        }
        
        // Perform tactical analysis
        TacticalAnalysisResult result = PerformTacticalAnalysis(position);
        
        // Cache result
        if (enableCaching)
        {
            tacticalCache[gridPos] = result;
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets the best cover positions within a radius
    /// </summary>
    public List<Vector3> GetBestCoverPositions(Vector3 centerPosition, float radius, int maxPositions = 5)
    {
        List<Vector3> bestPositions = new List<Vector3>();
        List<System.Tuple<Vector3, float>> scoredPositions = new List<System.Tuple<Vector3, float>>();
        
        // Analyze positions in radius
        float step = 1f; // Grid step size
        for (float x = -radius; x <= radius; x += step)
        {
            for (float z = -radius; z <= radius; z += step)
            {
                Vector3 testPos = centerPosition + new Vector3(x, 0, z);
                if (Vector3.Distance(centerPosition, testPos) > radius) continue;
                
                CoverAnalysisResult cover = AnalyzeCoverAtPosition(testPos);
                if (cover.coverQuality > CoverQuality.None)
                {
                    float score = CalculateCoverScore(cover);
                    scoredPositions.Add(new System.Tuple<Vector3, float>(testPos, score));
                }
            }
        }
        
        // Sort by score and return best positions
        scoredPositions.Sort((a, b) => b.Item2.CompareTo(a.Item2));
        
        for (int i = 0; i < Mathf.Min(maxPositions, scoredPositions.Count); i++)
        {
            bestPositions.Add(scoredPositions[i].Item1);
        }
        
        return bestPositions;
    }
    
    /// <summary>
    /// Finds flanking opportunities from a position against target positions
    /// </summary>
    public FlankingOpportunity[] FindFlankingOpportunities(Vector3 fromPosition, Vector3[] targetPositions)
    {
        if (!analyzeFlankingOpportunities) return new FlankingOpportunity[0];
        
        List<FlankingOpportunity> opportunities = new List<FlankingOpportunity>();
        
        foreach (Vector3 targetPos in targetPositions)
        {
            // Find positions that allow flanking the target
            Vector3[] flankingPositions = GetFlankingPositions(targetPos, flankingDistance);
            
            foreach (Vector3 flankPos in flankingPositions)
            {
                if (Vector3.Distance(fromPosition, flankPos) > flankingDistance * 2) continue;
                
                // Check if this position provides a flanking advantage
                float effectiveness = CalculateFlankingEffectiveness(flankPos, targetPos, fromPosition);
                
                if (effectiveness > 0.3f) // Minimum effectiveness threshold
                {
                    FlankingOpportunity opportunity = new FlankingOpportunity
                    {
                        flankingPosition = flankPos,
                        targetPosition = targetPos,
                        effectiveness = effectiveness,
                        distance = Vector3.Distance(fromPosition, flankPos),
                        requiresMovement = Vector3.Distance(fromPosition, flankPos) > 0.5f
                    };
                    
                    opportunities.Add(opportunity);
                }
            }
        }
        
        return opportunities.ToArray();
    }
    
    /// <summary>
    /// Analyzes crossfire opportunities from multiple positions
    /// </summary>
    public CrossfirePosition[] AnalyzeCrossfireOpportunities(Vector3[] friendlyPositions, Vector3[] enemyPositions)
    {
        if (!analyzeCrossfire) return new CrossfirePosition[0];
        
        List<CrossfirePosition> crossfirePositions = new List<CrossfirePosition>();
        
        foreach (Vector3 enemyPos in enemyPositions)
        {
            List<Vector3> validPositions = new List<Vector3>();
            
            // Find positions that can target the enemy
            foreach (Vector3 friendlyPos in friendlyPositions)
            {
                if (lineOfSightManager != null && lineOfSightManager.HasLineOfSight(friendlyPos, enemyPos))
                {
                    validPositions.Add(friendlyPos);
                }
            }
            
            // Check for crossfire angles
            if (validPositions.Count >= 2)
            {
                for (int i = 0; i < validPositions.Count - 1; i++)
                {
                    for (int j = i + 1; j < validPositions.Count; j++)
                    {
                        Vector3 pos1 = validPositions[i];
                        Vector3 pos2 = validPositions[j];
                        
                        float angle = CalculateAngleBetweenLines(pos1, enemyPos, pos2, enemyPos);
                        
                        if (angle >= crossfireAngle * 0.7f) // Allow some tolerance
                        {
                            CrossfirePosition crossfire = new CrossfirePosition
                            {
                                position = enemyPos,
                                targetPositions = new Vector3[] { pos1, pos2 },
                                crossfireAngle = angle,
                                effectiveness = angle / 180f // Normalize to 0-1
                            };
                            
                            crossfirePositions.Add(crossfire);
                        }
                    }
                }
            }
        }
        
        return crossfirePositions.ToArray();
    }
    
    /// <summary>
    /// Finds safe escape routes from a position
    /// </summary>
    public EscapeRoute[] FindEscapeRoutes(Vector3 fromPosition, Vector3[] threatPositions, int maxRoutes = 3)
    {
        if (!analyzeEscapeRoutes) return new EscapeRoute[0];
        
        List<EscapeRoute> escapeRoutes = new List<EscapeRoute>();
        
        // Find positions away from threats
        Vector3[] escapeDirections = GetEscapeDirections(fromPosition, threatPositions);
        
        foreach (Vector3 direction in escapeDirections)
        {
            Vector3 escapePosition = fromPosition + direction * 3f;
            
            // Check if this escape route is safe
            float safety = CalculateRouteSafety(fromPosition, escapePosition, threatPositions);
            
            if (safety > 0.4f) // Minimum safety threshold
            {
                EscapeRoute route = new EscapeRoute
                {
                    startPosition = fromPosition,
                    endPosition = escapePosition,
                    waypoints = new Vector3[] { fromPosition, escapePosition },
                    safety = safety,
                    distance = Vector3.Distance(fromPosition, escapePosition)
                };
                
                escapeRoutes.Add(route);
            }
        }
        
        // Sort by safety and return best routes
        escapeRoutes.Sort((a, b) => b.safety.CompareTo(a.safety));
        
        if (escapeRoutes.Count > maxRoutes)
        {
            escapeRoutes.RemoveRange(maxRoutes, escapeRoutes.Count - maxRoutes);
        }
        
        return escapeRoutes.ToArray();
    }
    
    #endregion
    
    #region Private Analysis Implementation
    
    /// <summary>
    /// Performs detailed cover analysis at a position
    /// </summary>
    private CoverAnalysisResult PerformCoverAnalysis(Vector3 position)
    {
        List<Vector3> protectedDirections = new List<Vector3>();
        List<Vector3> exposedDirections = new List<Vector3>();
        List<GameObject> coverObjects = new List<GameObject>();
        
        int protectedRays = 0;
        
        // Cast rays in multiple directions to analyze cover
        for (int i = 0; i < coverAnalysisRays; i++)
        {
            float angle = (360f / coverAnalysisRays) * i;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
            
            RaycastHit hit;
            Vector3 rayStart = position + Vector3.up * 0.5f; // Adjust for unit height
            
            if (Physics.Raycast(rayStart, direction, out hit, coverAnalysisRadius, obstacleLayerMask))
            {
                if (hit.distance <= coverAnalysisRadius && hit.collider.bounds.size.y >= minimumCoverHeight)
                {
                    protectedDirections.Add(direction);
                    protectedRays++;
                    
                    if (!coverObjects.Contains(hit.collider.gameObject))
                    {
                        coverObjects.Add(hit.collider.gameObject);
                    }
                }
                else
                {
                    exposedDirections.Add(direction);
                }
            }
            else
            {
                exposedDirections.Add(direction);
            }
        }
        
        // Calculate cover quality
        float coverPercentage = (float)protectedRays / coverAnalysisRays;
        CoverQuality quality = CalculateCoverQuality(coverPercentage);
        
        return new CoverAnalysisResult
        {
            position = position,
            coverQuality = quality,
            coverPercentage = coverPercentage,
            protectedDirections = protectedDirections.ToArray(),
            exposedDirections = exposedDirections.ToArray(),
            coverObjects = coverObjects.ToArray(),
            analysisTime = Time.time
        };
    }
    
    /// <summary>
    /// Performs tactical analysis including flanking and positioning
    /// </summary>
    private TacticalAnalysisResult PerformTacticalAnalysis(Vector3 position)
    {
        // Get cover analysis first
        CoverAnalysisResult coverResult = AnalyzeCoverAtPosition(position);
        
        // Find nearby units for tactical analysis
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        List<Vector3> enemyPositions = new List<Vector3>();
        List<Vector3> friendlyPositions = new List<Vector3>();
        
        foreach (Unit unit in allUnits)
        {
            float distance = Vector3.Distance(position, unit.transform.position);
            if (distance <= coverAnalysisRadius * 2)
            {
                // For analysis purposes, assume we're analyzing from perspective of first team found
                if (enemyPositions.Count == 0 && friendlyPositions.Count == 0)
                {
                    friendlyPositions.Add(unit.transform.position);
                }
                else if (friendlyPositions.Count > 0)
                {
                    // Determine if same team as first unit (simplified)
                    enemyPositions.Add(unit.transform.position);
                }
            }
        }
        
        // Analyze flanking opportunities
        FlankingOpportunity[] flankingOpportunities = FindFlankingOpportunities(position, enemyPositions.ToArray());
        
        // Analyze crossfire positions
        CrossfirePosition[] crossfirePositions = AnalyzeCrossfireOpportunities(
            new Vector3[] { position }, enemyPositions.ToArray());
        
        // Analyze escape routes
        EscapeRoute[] escapeRoutes = FindEscapeRoutes(position, enemyPositions.ToArray());
        
        // Calculate tactical value
        float tacticalValue = CalculateTacticalValue(coverResult, flankingOpportunities, crossfirePositions, escapeRoutes);
        
        return new TacticalAnalysisResult
        {
            position = position,
            tacticalValue = tacticalValue,
            flankingOpportunities = flankingOpportunities,
            crossfirePositions = crossfirePositions,
            escapeRoutes = escapeRoutes,
            analysisTime = Time.time
        };
    }
    
    /// <summary>
    /// Analyzes the entire battlefield for tactical positions
    /// </summary>
    private void AnalyzeBattlefield()
    {
        if (gridManager == null) return;
        
        tacticalPositions.Clear();
        
        // Analyze each grid position
        for (int x = 0; x < 4; x++) // Assuming 4x4 grid
        {
            for (int z = 0; z < 4; z++)
            {
                Vector3 gridPos = new Vector3(x, 0, z);
                Vector3 worldPos = gridManager.GridToWorld(new GridCoordinate((int)gridPos.x, (int)gridPos.z));
                
                CoverAnalysisResult cover = AnalyzeCoverAtPosition(worldPos);
                TacticalAnalysisResult tactical = AnalyzeTacticalPosition(worldPos);
                
                TacticalPosition tacticalPos = new TacticalPosition
                {
                    position = worldPos,
                    tacticalScore = tactical.tacticalValue,
                    coverQuality = cover.coverQuality,
                    hasFlankingOpportunity = tactical.flankingOpportunities.Length > 0,
                    isChokepointControl = IsChokepointPosition(worldPos),
                    hasEscapeRoutes = tactical.escapeRoutes.Length > 0
                };
                
                tacticalPositions.Add(tacticalPos);
            }
        }
        
        Debug.Log($"CoverAnalyzer: Analyzed {tacticalPositions.Count} battlefield positions");
    }
    
    /// <summary>
    /// Updates continuous analysis for dynamic battlefield changes
    /// </summary>
    private void UpdateContinuousAnalysis()
    {
        // Process queued analysis requests
        int processed = 0;
        while (analysisQueue.Count > 0 && processed < maxAnalysisPerFrame)
        {
            Vector3 position = analysisQueue.Dequeue();
            AnalyzeCoverAtPosition(position);
            processed++;
        }
        
        // Queue positions near units for re-analysis
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in units)
        {
            Vector3 gridPos = RoundToGrid(unit.transform.position);
            if (!analysisQueue.Contains(gridPos))
            {
                analysisQueue.Enqueue(gridPos);
            }
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    private Vector3 RoundToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x),
            Mathf.Round(position.y),
            Mathf.Round(position.z));
    }
    
    private CoverQuality CalculateCoverQuality(float coverPercentage)
    {
        if (coverPercentage >= fullCoverThreshold)
            return CoverQuality.Full;
        else if (coverPercentage >= partialCoverThreshold)
            return CoverQuality.Partial;
        else if (coverPercentage > 0.2f)
            return CoverQuality.Light;
        else
            return CoverQuality.None;
    }
    
    private float CalculateCoverScore(CoverAnalysisResult cover)
    {
        float score = cover.coverPercentage * 100f;
        score += (int)cover.coverQuality * 25f;
        score += cover.coverObjects.Length * 10f;
        return score;
    }
    
    private Vector3[] GetFlankingPositions(Vector3 targetPosition, float distance)
    {
        List<Vector3> positions = new List<Vector3>();
        
        // Generate positions around the target
        for (int i = 0; i < 8; i++)
        {
            float angle = (360f / 8f) * i;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
            Vector3 position = targetPosition + direction * distance;
            positions.Add(position);
        }
        
        return positions.ToArray();
    }
    
    private float CalculateFlankingEffectiveness(Vector3 flankingPos, Vector3 targetPos, Vector3 currentPos)
    {
        // Calculate angle advantage
        Vector3 currentToTarget = (targetPos - currentPos).normalized;
        Vector3 flankToTarget = (targetPos - flankingPos).normalized;
        
        float angle = Vector3.Angle(currentToTarget, flankToTarget);
        float effectiveness = angle / 180f; // Normalize to 0-1
        
        // Bonus for cover at flanking position
        CoverAnalysisResult cover = AnalyzeCoverAtPosition(flankingPos);
        effectiveness += (int)cover.coverQuality * 0.1f;
        
        return Mathf.Clamp01(effectiveness);
    }
    
    private float CalculateAngleBetweenLines(Vector3 pos1, Vector3 center, Vector3 pos2, Vector3 center2)
    {
        Vector3 dir1 = (center - pos1).normalized;
        Vector3 dir2 = (center2 - pos2).normalized;
        return Vector3.Angle(dir1, dir2);
    }
    
    private Vector3[] GetEscapeDirections(Vector3 fromPos, Vector3[] threatPositions)
    {
        List<Vector3> escapeDirections = new List<Vector3>();
        
        // Calculate average threat direction
        Vector3 averageThreatDir = Vector3.zero;
        foreach (Vector3 threat in threatPositions)
        {
            averageThreatDir += (threat - fromPos).normalized;
        }
        averageThreatDir /= threatPositions.Length;
        
        // Escape in opposite direction
        Vector3 primaryEscape = -averageThreatDir;
        escapeDirections.Add(primaryEscape);
        
        // Add perpendicular escape routes
        Vector3 perpendicular1 = Vector3.Cross(primaryEscape, Vector3.up).normalized;
        Vector3 perpendicular2 = -perpendicular1;
        
        escapeDirections.Add(perpendicular1);
        escapeDirections.Add(perpendicular2);
        
        return escapeDirections.ToArray();
    }
    
    private float CalculateRouteSafety(Vector3 start, Vector3 end, Vector3[] threats)
    {
        float safety = 1f;
        
        // Check distance from threats
        foreach (Vector3 threat in threats)
        {
            float distanceToRoute = DistanceFromPointToLineSegment(threat, start, end);
            float threatReduction = Mathf.Clamp01(distanceToRoute / 5f); // 5 unit safe distance
            safety *= threatReduction;
        }
        
        // Check for cover along route
        if (lineOfSightManager != null)
        {
            bool hasCover = false;
            foreach (Vector3 threat in threats)
            {
                if (!lineOfSightManager.HasLineOfSight(threat, end))
                {
                    hasCover = true;
                    break;
                }
            }
            
            if (hasCover)
            {
                safety += 0.2f;
            }
        }
        
        return Mathf.Clamp01(safety);
    }
    
    private float DistanceFromPointToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 line = lineEnd - lineStart;
        float lineLength = line.magnitude;
        
        if (lineLength == 0f)
            return Vector3.Distance(point, lineStart);
        
        float t = Mathf.Clamp01(Vector3.Dot(point - lineStart, line) / (lineLength * lineLength));
        Vector3 projection = lineStart + t * line;
        
        return Vector3.Distance(point, projection);
    }
    
    private bool IsChokepointPosition(Vector3 position)
    {
        // Simple chokepoint detection - position with limited movement options
        int blockedDirections = 0;
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        
        foreach (Vector3 dir in directions)
        {
            RaycastHit hit;
            if (Physics.Raycast(position, dir, out hit, 2f, obstacleLayerMask))
            {
                blockedDirections++;
            }
        }
        
        return blockedDirections >= 2; // At least 2 directions blocked
    }
    
    private float CalculateTacticalValue(CoverAnalysisResult cover, FlankingOpportunity[] flanking, 
                                       CrossfirePosition[] crossfire, EscapeRoute[] escapes)
    {
        float value = 0f;
        
        // Cover value
        value += (int)cover.coverQuality * 25f;
        value += cover.coverPercentage * 30f;
        
        // Flanking value
        value += flanking.Length * 20f;
        foreach (var flank in flanking)
        {
            value += flank.effectiveness * 15f;
        }
        
        // Crossfire value
        value += crossfire.Length * 25f;
        
        // Escape route value
        value += escapes.Length * 10f;
        
        return Mathf.Clamp(value, 0f, 200f);
    }
    
    #endregion
    
    #region Debug Visualization
    
    void OnDrawGizmos()
    {
        if (!showCoverAnalysis) return;
        
        foreach (var kvp in coverCache)
        {
            CoverAnalysisResult result = kvp.Value;
            
            Color gizmoColor = noCoverColor;
            switch (result.coverQuality)
            {
                case CoverQuality.Full:
                    gizmoColor = fullCoverColor;
                    break;
                case CoverQuality.Partial:
                    gizmoColor = partialCoverColor;
                    break;
                case CoverQuality.Light:
                    gizmoColor = Color.Lerp(partialCoverColor, noCoverColor, 0.5f);
                    break;
            }
            
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(result.position + Vector3.up * 0.1f, Vector3.one * 0.8f);
            
            // Draw protected directions
            Gizmos.color = Color.green;
            foreach (Vector3 dir in result.protectedDirections)
            {
                Gizmos.DrawRay(result.position, dir * 0.5f);
            }
        }
    }
    
    #endregion
    
    /// <summary>
    /// Gets analysis statistics for debugging
    /// </summary>
    public string GetAnalysisStats()
    {
        return $"Cover Analysis - Cache: {coverCache.Count}, Tactical: {tacticalCache.Count}, " +
               $"Positions: {tacticalPositions.Count}, Queue: {analysisQueue.Count}";
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        coverCache?.Clear();
        tacticalCache?.Clear();
        knownCoverPositions?.Clear();
        tacticalPositions?.Clear();
        analysisQueue?.Clear();
    }
}