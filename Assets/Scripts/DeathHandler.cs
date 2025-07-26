using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Death handling system for tactical combat units.
/// Manages unit death detection, cleanup processes, visual effects, and game state updates.
/// Coordinates with health system and win condition checking for complete death management.
/// Part of Task 2.1.3 - Health & Damage System.
/// </summary>
public class DeathHandler : MonoBehaviour
{
    [Header("Death Detection")]
    [SerializeField] private bool enableAutomaticDeathDetection = true;
    [SerializeField] private float deathDetectionInterval = 0.1f;
    [SerializeField] private bool preventDeadUnitActions = true;
    [SerializeField] private bool removeDeadUnitsFromSelection = true;
    
    [Header("Death Processing")]
    [SerializeField] private bool processDeathImmediately = true;
    [SerializeField] private float deathProcessingDelay = 0.5f;
    [SerializeField] private bool enableDeathQueue = true;
    [SerializeField] private int maxDeathsPerFrame = 3;
    
    [Header("Unit Cleanup")]
    [SerializeField] private bool destroyDeadUnits = false;
    [SerializeField] private float destroyDelay = 2.0f;
    [SerializeField] private bool disableDeadUnitColliders = true;
    [SerializeField] private bool disableDeadUnitRenderers = false;
    [SerializeField] private bool moveDeadUnitsToLayer = true;
    [SerializeField] private int deadUnitLayer = 31;
    
    [Header("Visual Effects")]
    [SerializeField] private bool enableDeathEffects = true;
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float deathEffectDuration = 2.0f;
    [SerializeField] private bool enableDeathAnimation = true;
    [SerializeField] private float deathAnimationDuration = 1.0f;
    
    [Header("Audio Effects")]
    [SerializeField] private bool enableDeathAudio = false;
    [SerializeField] private AudioClip deathSoundEffect;
    [SerializeField] private float deathAudioVolume = 0.7f;
    [SerializeField] private bool varyDeathAudioPitch = true;
    [SerializeField] private Vector2 audioRitchRange = new Vector2(0.8f, 1.2f);
    
    [Header("Team Management")]
    [SerializeField] private bool trackTeamDeaths = true;
    [SerializeField] private bool notifyTeamElimination = true;
    [SerializeField] private bool enableLastUnitSpecialHandling = true;
    [SerializeField] private float teamEliminationCheckDelay = 0.5f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDeathDebugging = true;
    [SerializeField] private bool logDeathEvents = true;
    [SerializeField] private bool logTeamElimination = true;
    [SerializeField] private bool visualizeDeathProcessing = false;
    
    // System references
    private HealthManager healthManager;
    private WinConditionChecker winConditionChecker;
    private SelectionManager selectionManager;
    private HealthEventBroadcaster eventBroadcaster;
    
    // Death processing state
    private Queue<DeathRequest> deathQueue;
    private List<Unit> deadUnits;
    private Dictionary<UnitTeam, List<Unit>> teamDeathCounts;
    private bool isProcessingDeaths = false;
    private float lastDeathDetectionTime = 0f;
    
    // Death tracking
    private int totalDeathsProcessed = 0;
    private Dictionary<UnitTeam, int> deathsByTeam;
    
    // Events
    public System.Action<Unit, IAttacker> OnUnitDeath;
    public System.Action<Unit> OnUnitDeathProcessingStarted;
    public System.Action<Unit> OnUnitDeathProcessingCompleted;
    public System.Action<UnitTeam, List<Unit>> OnTeamEliminated;
    public System.Action<Unit> OnLastUnitRemaining;
    public System.Action OnAllUnitsDestroyed;
    
    // Properties
    public int TotalDeathsProcessed => totalDeathsProcessed;
    public int PendingDeaths => deathQueue?.Count ?? 0;
    public bool IsProcessingDeaths => isProcessingDeaths;
    public List<Unit> DeadUnits => new List<Unit>(deadUnits);
    
    // Death request structure
    private struct DeathRequest
    {
        public Unit unit;
        public IAttacker killer;
        public float timestamp;
        public DeathCause cause;
        
        public DeathRequest(Unit u, IAttacker k, DeathCause c)
        {
            unit = u;
            killer = k;
            timestamp = Time.time;
            cause = c;
        }
    }
    
    public enum DeathCause
    {
        Combat,
        Environmental,
        SelfDestruct,
        SystemKill,
        Unknown
    }
    
    void Awake()
    {
        InitializeDeathHandler();
    }
    
    void Start()
    {
        FindSystemReferences();
        StartDeathDetectionSystem();
        SubscribeToHealthEvents();
    }
    
    void Update()
    {
        if (enableAutomaticDeathDetection && Time.time - lastDeathDetectionTime >= deathDetectionInterval)
        {
            DetectDeadUnits();
            lastDeathDetectionTime = Time.time;
        }
        
        if (enableDeathQueue && deathQueue.Count > 0)
        {
            ProcessDeathQueue();
        }
    }
    
    /// <summary>
    /// Initializes the death handler system
    /// </summary>
    private void InitializeDeathHandler()
    {
        deathQueue = new Queue<DeathRequest>();
        deadUnits = new List<Unit>();
        teamDeathCounts = new Dictionary<UnitTeam, List<Unit>>();
        deathsByTeam = new Dictionary<UnitTeam, int>();
        
        // Initialize team death tracking
        foreach (UnitTeam team in System.Enum.GetValues(typeof(UnitTeam)))
        {
            teamDeathCounts[team] = new List<Unit>();
            deathsByTeam[team] = 0;
        }
        
        if (enableDeathDebugging)
        {
            Debug.Log("DeathHandler initialized - Tactical death management system ready");
        }
    }
    
    /// <summary>
    /// Finds references to other system components
    /// </summary>
    private void FindSystemReferences()
    {
        healthManager = GetComponent<HealthManager>();
        winConditionChecker = GetComponent<WinConditionChecker>();
        selectionManager = FindFirstObjectByType<SelectionManager>();
        eventBroadcaster = GetComponent<HealthEventBroadcaster>();
        
        if (enableDeathDebugging)
        {
            Debug.Log($"DeathHandler found references - Health Manager: {healthManager != null}, " +
                     $"Win Checker: {winConditionChecker != null}, Selection Manager: {selectionManager != null}, " +
                     $"Event Broadcaster: {eventBroadcaster != null}");
        }
    }
    
    /// <summary>
    /// Starts the death detection system
    /// </summary>
    private void StartDeathDetectionSystem()
    {
        if (enableDeathQueue)
        {
            StartCoroutine(DeathProcessingCoroutine());
        }
    }
    
    /// <summary>
    /// Subscribes to health system events
    /// </summary>
    private void SubscribeToHealthEvents()
    {
        if (healthManager != null)
        {
            healthManager.OnUnitDied += HandleUnitDied;
        }
    }
    
    #region Death Detection
    
    /// <summary>
    /// Detects dead units that haven't been processed yet
    /// </summary>
    private void DetectDeadUnits()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        foreach (Unit unit in allUnits)
        {
            if (unit == null) continue;
            
            HealthComponent healthComponent = unit.GetComponent<HealthComponent>();
            if (healthComponent != null && !healthComponent.IsAlive && !deadUnits.Contains(unit))
            {
                // Found a dead unit that hasn't been processed
                QueueUnitForDeath(unit, null, DeathCause.Unknown);
            }
        }
    }
    
    /// <summary>
    /// Handles unit death events from health manager
    /// </summary>
    private void HandleUnitDied(Unit unit, IAttacker killer)
    {
        if (unit == null) return;
        
        QueueUnitForDeath(unit, killer, DeathCause.Combat);
        
        if (logDeathEvents)
        {
            Debug.Log($"DeathHandler: Unit death event received - {unit.name} killed by {killer?.GetDisplayInfo() ?? "unknown"}");
        }
    }
    
    /// <summary>
    /// Queues a unit for death processing
    /// </summary>
    private void QueueUnitForDeath(Unit unit, IAttacker killer, DeathCause cause)
    {
        if (unit == null || deadUnits.Contains(unit)) return;
        
        DeathRequest request = new DeathRequest(unit, killer, cause);
        deathQueue.Enqueue(request);
        
        if (processDeathImmediately && !isProcessingDeaths)
        {
            StartCoroutine(ProcessDeathImmediately(request));
        }
        
        if (enableDeathDebugging)
        {
            Debug.Log($"DeathHandler: Queued {unit.name} for death processing (cause: {cause})");
        }
    }
    
    #endregion
    
    #region Death Processing
    
    /// <summary>
    /// Main death processing method
    /// </summary>
    public void HandleUnitDeath(Unit unit, IAttacker killer)
    {
        if (unit == null) return;
        
        QueueUnitForDeath(unit, killer, DeathCause.Combat);
    }
    
    /// <summary>
    /// Processes the death queue
    /// </summary>
    private void ProcessDeathQueue()
    {
        if (isProcessingDeaths) return;
        
        int deathsProcessedThisFrame = 0;
        
        while (deathQueue.Count > 0 && deathsProcessedThisFrame < maxDeathsPerFrame)
        {
            DeathRequest request = deathQueue.Dequeue();
            ProcessUnitDeath(request);
            deathsProcessedThisFrame++;
        }
    }
    
    /// <summary>
    /// Processes a single unit death
    /// </summary>
    private void ProcessUnitDeath(DeathRequest request)
    {
        if (request.unit == null || deadUnits.Contains(request.unit))
        {
            return;
        }
        
        isProcessingDeaths = true;
        
        // Trigger event
        OnUnitDeath?.Invoke(request.unit, request.killer);
        OnUnitDeathProcessingStarted?.Invoke(request.unit);
        
        // Add to dead units list
        deadUnits.Add(request.unit);
        
        // Update team death counts
        if (trackTeamDeaths)
        {
            UpdateTeamDeathCounts(request.unit);
        }
        
        // Handle visual and audio effects
        if (enableDeathEffects)
        {
            StartCoroutine(PlayDeathEffects(request.unit));
        }
        
        // Handle unit cleanup
        StartCoroutine(ProcessUnitCleanup(request.unit, request.killer));
        
        // Update statistics
        totalDeathsProcessed++;
        deathsByTeam[request.unit.Team]++;
        
        // Broadcast event
        if (eventBroadcaster != null)
        {
            eventBroadcaster.BroadcastUnitDied(request.unit, request.killer);
        }
        
        if (logDeathEvents)
        {
            Debug.Log($"DeathHandler: Processed death of {request.unit.name} " +
                     $"(killer: {request.killer?.GetDisplayInfo() ?? "unknown"}, cause: {request.cause})");
        }
        
        isProcessingDeaths = false;
    }
    
    /// <summary>
    /// Processes unit cleanup after death
    /// </summary>
    private IEnumerator ProcessUnitCleanup(Unit unit, IAttacker killer)
    {
        // Wait for death processing delay
        if (deathProcessingDelay > 0)
        {
            yield return new WaitForSeconds(deathProcessingDelay);
        }
        
        // Remove from selection if applicable
        if (removeDeadUnitsFromSelection && selectionManager != null)
        {
            RemoveFromSelection(unit);
        }
        
        // Disable components as configured
        if (disableDeadUnitColliders)
        {
            DisableUnitColliders(unit);
        }
        
        if (disableDeadUnitRenderers)
        {
            DisableUnitRenderers(unit);
        }
        
        // Move to dead unit layer
        if (moveDeadUnitsToLayer)
        {
            MoveUnitToDeadLayer(unit);
        }
        
        // Prevent actions
        if (preventDeadUnitActions)
        {
            DisableUnitActions(unit);
        }
        
        // Schedule destruction if configured
        if (destroyDeadUnits)
        {
            StartCoroutine(DestroyUnitAfterDelay(unit, destroyDelay));
        }
        
        // Check for team elimination
        if (trackTeamDeaths && notifyTeamElimination)
        {
            StartCoroutine(CheckTeamEliminationAfterDelay(unit.Team));
        }
        
        // Trigger completion event
        OnUnitDeathProcessingCompleted?.Invoke(unit);
        
        if (logDeathEvents)
        {
            Debug.Log($"DeathHandler: Completed cleanup for {unit.name}");
        }
    }
    
    #endregion
    
    #region Unit Cleanup Methods
    
    /// <summary>
    /// Removes unit from selection
    /// </summary>
    private void RemoveFromSelection(Unit unit)
    {
        if (selectionManager == null) return;
        
        ISelectable selectable = unit.GetComponent<ISelectable>();
        if (selectable != null)
        {
            selectionManager.DeselectObject(selectable);
        }
        
        if (enableDeathDebugging)
        {
            Debug.Log($"DeathHandler: Removed {unit.name} from selection");
        }
    }
    
    /// <summary>
    /// Disables unit colliders
    /// </summary>
    private void DisableUnitColliders(Unit unit)
    {
        Collider[] colliders = unit.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        if (enableDeathDebugging)
        {
            Debug.Log($"DeathHandler: Disabled {colliders.Length} colliders for {unit.name}");
        }
    }
    
    /// <summary>
    /// Disables unit renderers
    /// </summary>
    private void DisableUnitRenderers(Unit unit)
    {
        Renderer[] renderers = unit.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
        
        if (enableDeathDebugging)
        {
            Debug.Log($"DeathHandler: Disabled {renderers.Length} renderers for {unit.name}");
        }
    }
    
    /// <summary>
    /// Moves unit to dead unit layer
    /// </summary>
    private void MoveUnitToDeadLayer(Unit unit)
    {
        unit.gameObject.layer = deadUnitLayer;
        
        // Also move child objects
        Transform[] childTransforms = unit.GetComponentsInChildren<Transform>();
        foreach (Transform child in childTransforms)
        {
            child.gameObject.layer = deadUnitLayer;
        }
        
        if (enableDeathDebugging)
        {
            Debug.Log($"DeathHandler: Moved {unit.name} to dead unit layer {deadUnitLayer}");
        }
    }
    
    /// <summary>
    /// Disables unit actions and behaviors
    /// </summary>
    private void DisableUnitActions(Unit unit)
    {
        // Disable any movement-related components (check for common movement script names)
        MonoBehaviour[] movementComponents = unit.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in movementComponents)
        {
            if (component.GetType().Name.Contains("Movement") || 
                component.GetType().Name.Contains("Move") ||
                component.GetType().Name.Contains("Navigation"))
            {
                component.enabled = false;
            }
        }
        
        // Disable attack capability
        IAttacker attacker = unit.GetComponent<IAttacker>();
        if (attacker != null)
        {
            // This would disable attack capability - implementation depends on IAttacker interface
        }
        
        // Disable AI and other controller components
        MonoBehaviour[] aiComponents = unit.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in aiComponents)
        {
            if (component.GetType().Name.Contains("AI") || component.GetType().Name.Contains("Controller"))
            {
                component.enabled = false;
            }
        }
        
        if (enableDeathDebugging)
        {
            Debug.Log($"DeathHandler: Disabled actions for {unit.name}");
        }
    }
    
    /// <summary>
    /// Destroys unit after delay
    /// </summary>
    private IEnumerator DestroyUnitAfterDelay(Unit unit, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (unit != null)
        {
            // Remove from tracking
            deadUnits.Remove(unit);
            
            // Unregister from health manager
            if (healthManager != null)
            {
                healthManager.UnregisterUnit(unit);
            }
            
            if (logDeathEvents)
            {
                Debug.Log($"DeathHandler: Destroying {unit.name} after {delay}s delay");
            }
            
            Destroy(unit.gameObject);
        }
    }
    
    #endregion
    
    #region Visual and Audio Effects
    
    /// <summary>
    /// Plays death effects for a unit
    /// </summary>
    private IEnumerator PlayDeathEffects(Unit unit)
    {
        // Play visual effect
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, unit.transform.position, unit.transform.rotation);
            Destroy(effect, deathEffectDuration);
        }
        
        // Play audio effect
        if (enableDeathAudio && deathSoundEffect != null)
        {
            PlayDeathAudio(unit);
        }
        
        // Play death animation
        if (enableDeathAnimation)
        {
            yield return StartCoroutine(PlayDeathAnimation(unit));
        }
    }
    
    /// <summary>
    /// Plays death audio
    /// </summary>
    private void PlayDeathAudio(Unit unit)
    {
        AudioSource audioSource = unit.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = unit.gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.clip = deathSoundEffect;
        audioSource.volume = deathAudioVolume;
        
        if (varyDeathAudioPitch)
        {
            audioSource.pitch = Random.Range(audioRitchRange.x, audioRitchRange.y);
        }
        
        audioSource.Play();
    }
    
    /// <summary>
    /// Plays death animation
    /// </summary>
    private IEnumerator PlayDeathAnimation(Unit unit)
    {
        // Simple fade/scale death animation
        Renderer unitRenderer = unit.GetComponent<Renderer>();
        if (unitRenderer != null)
        {
            Color originalColor = unitRenderer.material.color;
            Vector3 originalScale = unit.transform.localScale;
            
            float elapsedTime = 0f;
            
            while (elapsedTime < deathAnimationDuration)
            {
                float progress = elapsedTime / deathAnimationDuration;
                
                // Fade out
                Color currentColor = originalColor;
                currentColor.a = Mathf.Lerp(1f, 0.2f, progress);
                unitRenderer.material.color = currentColor;
                
                // Scale down slightly
                float scale = Mathf.Lerp(1f, 0.8f, progress);
                unit.transform.localScale = originalScale * scale;
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        
        if (enableDeathDebugging)
        {
            Debug.Log($"DeathHandler: Completed death animation for {unit.name}");
        }
    }
    
    #endregion
    
    #region Team Management
    
    /// <summary>
    /// Updates team death counts
    /// </summary>
    private void UpdateTeamDeathCounts(Unit unit)
    {
        if (!teamDeathCounts.ContainsKey(unit.Team))
        {
            teamDeathCounts[unit.Team] = new List<Unit>();
        }
        
        teamDeathCounts[unit.Team].Add(unit);
    }
    
    /// <summary>
    /// Checks for team elimination after delay
    /// </summary>
    private IEnumerator CheckTeamEliminationAfterDelay(UnitTeam team)
    {
        yield return new WaitForSeconds(teamEliminationCheckDelay);
        
        if (IsTeamEliminated(team))
        {
            HandleTeamElimination(team);
        }
        
        // Check for last unit remaining
        if (enableLastUnitSpecialHandling)
        {
            CheckForLastUnitRemaining();
        }
    }
    
    /// <summary>
    /// Checks if a team is eliminated
    /// </summary>
    private bool IsTeamEliminated(UnitTeam team)
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        foreach (Unit unit in allUnits)
        {
            if (unit.Team == team)
            {
                HealthComponent healthComponent = unit.GetComponent<HealthComponent>();
                if (healthComponent != null && healthComponent.IsAlive)
                {
                    return false; // Found a living unit on this team
                }
            }
        }
        
        return true; // No living units found on this team
    }
    
    /// <summary>
    /// Handles team elimination
    /// </summary>
    private void HandleTeamElimination(UnitTeam eliminatedTeam)
    {
        List<Unit> eliminatedUnits = teamDeathCounts[eliminatedTeam];
        
        OnTeamEliminated?.Invoke(eliminatedTeam, eliminatedUnits);
        
        if (eventBroadcaster != null)
        {
            eventBroadcaster.BroadcastTeamEliminated(eliminatedTeam, eliminatedUnits);
        }
        
        // Trigger win condition check
        if (winConditionChecker != null)
        {
            winConditionChecker.CheckWinConditions();
        }
        
        if (logTeamElimination)
        {
            Debug.Log($"DeathHandler: Team {eliminatedTeam} has been eliminated! ({eliminatedUnits.Count} units destroyed)");
        }
    }
    
    /// <summary>
    /// Checks for last unit remaining scenario
    /// </summary>
    private void CheckForLastUnitRemaining()
    {
        Unit[] aliveUnits = GetAliveUnits();
        
        if (aliveUnits.Length == 1)
        {
            OnLastUnitRemaining?.Invoke(aliveUnits[0]);
            
            if (enableDeathDebugging)
            {
                Debug.Log($"DeathHandler: Last unit remaining - {aliveUnits[0].name}");
            }
        }
        else if (aliveUnits.Length == 0)
        {
            OnAllUnitsDestroyed?.Invoke();
            
            if (enableDeathDebugging)
            {
                Debug.Log("DeathHandler: All units have been destroyed!");
            }
        }
    }
    
    /// <summary>
    /// Gets all alive units
    /// </summary>
    private Unit[] GetAliveUnits()
    {
        List<Unit> aliveUnits = new List<Unit>();
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        foreach (Unit unit in allUnits)
        {
            HealthComponent healthComponent = unit.GetComponent<HealthComponent>();
            if (healthComponent != null && healthComponent.IsAlive)
            {
                aliveUnits.Add(unit);
            }
        }
        
        return aliveUnits.ToArray();
    }
    
    #endregion
    
    #region Coroutines
    
    /// <summary>
    /// Death processing coroutine
    /// </summary>
    private IEnumerator DeathProcessingCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            
            if (enableDeathQueue && deathQueue.Count > 0 && !isProcessingDeaths)
            {
                ProcessDeathQueue();
            }
        }
    }
    
    /// <summary>
    /// Processes death immediately for critical cases
    /// </summary>
    private IEnumerator ProcessDeathImmediately(DeathRequest request)
    {
        yield return new WaitForEndOfFrame();
        
        if (!deadUnits.Contains(request.unit))
        {
            ProcessUnitDeath(request);
        }
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Forces immediate death processing for a unit
    /// </summary>
    public void ForceProcessUnitDeath(Unit unit, IAttacker killer = null)
    {
        if (unit == null) return;
        
        DeathRequest request = new DeathRequest(unit, killer, DeathCause.SystemKill);
        ProcessUnitDeath(request);
    }
    
    /// <summary>
    /// Gets death statistics
    /// </summary>
    public string GetDeathStatistics()
    {
        int totalAlive = GetAliveUnits().Length;
        return $"Death Stats - Total Deaths: {totalDeathsProcessed}, " +
               $"Alive: {totalAlive}, Pending: {PendingDeaths}, " +
               $"Processing: {isProcessingDeaths}";
    }
    
    /// <summary>
    /// Gets team death counts
    /// </summary>
    public Dictionary<UnitTeam, int> GetTeamDeathCounts()
    {
        return new Dictionary<UnitTeam, int>(deathsByTeam);
    }
    
    /// <summary>
    /// Clears all death tracking data
    /// </summary>
    public void ClearDeathData()
    {
        deadUnits.Clear();
        deathQueue.Clear();
        
        foreach (UnitTeam team in teamDeathCounts.Keys)
        {
            teamDeathCounts[team].Clear();
            deathsByTeam[team] = 0;
        }
        
        totalDeathsProcessed = 0;
        isProcessingDeaths = false;
        
        if (enableDeathDebugging)
        {
            Debug.Log("DeathHandler: Cleared all death tracking data");
        }
    }
    
    #endregion
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Unsubscribe from events
        if (healthManager != null)
        {
            healthManager.OnUnitDied -= HandleUnitDied;
        }
        
        // Clear event references
        OnUnitDeath = null;
        OnUnitDeathProcessingStarted = null;
        OnUnitDeathProcessingCompleted = null;
        OnTeamEliminated = null;
        OnLastUnitRemaining = null;
        OnAllUnitsDestroyed = null;
        
        // Clear collections
        deathQueue?.Clear();
        deadUnits?.Clear();
        teamDeathCounts?.Clear();
        deathsByTeam?.Clear();
    }
}