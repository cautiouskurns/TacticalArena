using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Health event broadcasting system for tactical combat.
/// Coordinates health-related events across all systems including UI, audio, effects, and game state.
/// Provides centralized event management for health, damage, death, and win condition events.
/// Part of Task 2.1.3 - Health & Damage System.
/// </summary>
public class HealthEventBroadcaster : MonoBehaviour
{
    [Header("Event Broadcasting Configuration")]
    [SerializeField] private bool enableHealthEvents = true;
    [SerializeField] private bool enableDamageEvents = true;
    [SerializeField] private bool enableDeathEvents = true;
    [SerializeField] private bool enableWinConditionEvents = true;
    [SerializeField] private bool enableTeamEvents = true;
    
    [Header("Event Filtering")]
    [SerializeField] private bool filterDuplicateEvents = true;
    [SerializeField] private float duplicateEventTimeWindow = 0.1f;
    [SerializeField] private bool enableEventPriority = true;
    [SerializeField] private int maxEventsPerFrame = 10;
    [SerializeField] private bool queueOverflowEvents = true;
    
    [Header("Audio Integration")]
    [SerializeField] private bool enableAudioEvents = true;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip healingSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip defeatSound;
    [SerializeField] private float audioVolume = 0.7f;
    
    [Header("Visual Effects Integration")]
    [SerializeField] private bool enableVisualEffectEvents = true;
    [SerializeField] private GameObject damageEffectPrefab;
    [SerializeField] private GameObject healingEffectPrefab;
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float effectDuration = 2.0f;
    
    [Header("UI Integration")]
    [SerializeField] private bool enableUIEvents = true;
    [SerializeField] private bool updateHealthBarsOnHealthChange = true;
    [SerializeField] private bool showDamageNumbers = true;
    [SerializeField] private bool showHealingNumbers = true;
    [SerializeField] private bool updateGameStatusOnWinCondition = true;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableEventDebugging = true;
    [SerializeField] private bool logAllEvents = false;
    [SerializeField] private bool logHighPriorityEvents = true;
    [SerializeField] private bool visualizeEventFlow = false;
    [SerializeField] private bool trackEventStatistics = true;
    
    // System references
    private HealthManager healthManager;
    private DeathHandler deathHandler;
    private WinConditionChecker winConditionChecker;
    private AudioSource audioSource;
    
    // Event management
    private Queue<HealthEvent> eventQueue;
    private List<HealthEvent> recentEvents;
    private Dictionary<HealthEventType, int> eventStatistics;
    private bool isProcessingEvents = false;
    private int eventsProcessedThisFrame = 0;
    private float lastEventProcessTime = 0f;
    
    // Event tracking
    private int totalEventsProcessed = 0;
    private int duplicateEventsFiltered = 0;
    private int eventsQueuedThisSession = 0;
    
    // Properties
    public int TotalEventsProcessed => totalEventsProcessed;
    public int PendingEvents => eventQueue?.Count ?? 0;
    public int DuplicateEventsFiltered => duplicateEventsFiltered;
    public bool IsProcessingEvents => isProcessingEvents;
    
    // Event types
    public enum HealthEventType
    {
        HealthChanged = 0,
        UnitDamaged = 1,
        UnitHealed = 2,
        UnitDied = 3,
        TeamEliminated = 4,
        GameEnded = 5,
        WinCondition = 6,
        HealthRestored = 7,
        CriticalHealth = 8,
        LastUnitRemaining = 9
    }
    
    public enum EventPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }
    
    // Event structure
    private struct HealthEvent
    {
        public HealthEventType type;
        public EventPriority priority;
        public float timestamp;
        public object data;
        public string description;
        
        public HealthEvent(HealthEventType t, EventPriority p, object d, string desc)
        {
            type = t;
            priority = p;
            timestamp = Time.time;
            data = d;
            description = desc;
        }
    }
    
    // Event data structures
    public struct HealthChangedEventData
    {
        public Unit unit;
        public int oldHealth;
        public int newHealth;
        public float healthPercentage;
    }
    
    public struct UnitDamagedEventData
    {
        public Unit unit;
        public int damage;
        public IAttacker attacker;
        public bool wasCritical;
    }
    
    public struct UnitHealedEventData
    {
        public Unit unit;
        public int healAmount;
        public bool wasFullyHealed;
    }
    
    public struct UnitDiedEventData
    {
        public Unit unit;
        public IAttacker killer;
        public UnitTeam team;
    }
    
    public struct TeamEliminatedEventData
    {
        public UnitTeam team;
        public List<Unit> eliminatedUnits;
        public UnitTeam survivingTeam;
    }
    
    public struct GameEndedEventData
    {
        public WinConditionChecker.WinConditionResult result;
        public UnitTeam winningTeam;
        public string reason;
        public float gameDuration;
    }
    
    void Awake()
    {
        InitializeEventBroadcaster();
    }
    
    void Start()
    {
        FindSystemReferences();
        SubscribeToSystemEvents();
        StartEventProcessing();
    }
    
    void Update()
    {
        // Reset frame counter
        if (Time.time - lastEventProcessTime >= Time.fixedDeltaTime)
        {
            eventsProcessedThisFrame = 0;
            lastEventProcessTime = Time.time;
        }
        
        // Process event queue
        if (eventQueue.Count > 0 && !isProcessingEvents)
        {
            ProcessEventQueue();
        }
    }
    
    /// <summary>
    /// Initializes the event broadcaster
    /// </summary>
    private void InitializeEventBroadcaster()
    {
        eventQueue = new Queue<HealthEvent>();
        recentEvents = new List<HealthEvent>();
        eventStatistics = new Dictionary<HealthEventType, int>();
        
        // Initialize statistics
        foreach (HealthEventType eventType in System.Enum.GetValues(typeof(HealthEventType)))
        {
            eventStatistics[eventType] = 0;
        }
        
        if (enableEventDebugging)
        {
            Debug.Log("HealthEventBroadcaster initialized - Tactical event system ready");
        }
    }
    
    /// <summary>
    /// Finds references to other system components
    /// </summary>
    private void FindSystemReferences()
    {
        healthManager = GetComponent<HealthManager>();
        deathHandler = GetComponent<DeathHandler>();
        winConditionChecker = GetComponent<WinConditionChecker>();
        
        // Get or create audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && enableAudioEvents)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        if (enableEventDebugging)
        {
            Debug.Log($"HealthEventBroadcaster found references - Health Manager: {healthManager != null}, " +
                     $"Death Handler: {deathHandler != null}, Win Checker: {winConditionChecker != null}, " +
                     $"Audio Source: {audioSource != null}");
        }
    }
    
    /// <summary>
    /// Subscribes to system events
    /// </summary>
    private void SubscribeToSystemEvents()
    {
        // Subscribe to health manager events
        if (healthManager != null)
        {
            healthManager.OnUnitHealthChanged += HandleUnitHealthChanged;
            healthManager.OnUnitDamaged += HandleUnitDamaged;
            healthManager.OnUnitHealed += HandleUnitHealed;
            healthManager.OnUnitDied += HandleUnitDied;
        }
        
        // Subscribe to death handler events
        if (deathHandler != null)
        {
            deathHandler.OnTeamEliminated += HandleTeamEliminated;
            deathHandler.OnLastUnitRemaining += HandleLastUnitRemaining;
        }
        
        // Subscribe to win condition checker events
        if (winConditionChecker != null)
        {
            winConditionChecker.OnWinConditionMet += HandleWinConditionMet;
            winConditionChecker.OnTeamVictory += HandleTeamVictory;
            winConditionChecker.OnTeamDefeat += HandleTeamDefeat;
            winConditionChecker.OnGameDraw += HandleGameDraw;
        }
    }
    
    /// <summary>
    /// Starts event processing
    /// </summary>
    private void StartEventProcessing()
    {
        StartCoroutine(EventProcessingCoroutine());
    }
    
    #region Event Handlers
    
    /// <summary>
    /// Handles unit health changed events
    /// </summary>
    private void HandleUnitHealthChanged(Unit unit, int oldHealth, int newHealth)
    {
        if (!enableHealthEvents || unit == null) return;
        
        HealthChangedEventData eventData = new HealthChangedEventData
        {
            unit = unit,
            oldHealth = oldHealth,
            newHealth = newHealth,
            healthPercentage = healthManager?.GetUnitHealthPercentage(unit) ?? 0f
        };
        
        EventPriority priority = EventPriority.Normal;
        if (eventData.healthPercentage <= 0.25f)
        {
            priority = EventPriority.High; // Critical health
        }
        
        QueueEvent(HealthEventType.HealthChanged, priority, eventData, 
                  $"{unit.name} health: {oldHealth} → {newHealth}");
    }
    
    /// <summary>
    /// Handles unit damaged events
    /// </summary>
    private void HandleUnitDamaged(Unit unit, int damage, IAttacker attacker)
    {
        if (!enableDamageEvents || unit == null) return;
        
        UnitDamagedEventData eventData = new UnitDamagedEventData
        {
            unit = unit,
            damage = damage,
            attacker = attacker,
            wasCritical = false // Would need integration with damage calculator to detect crits
        };
        
        EventPriority priority = damage > 1 ? EventPriority.High : EventPriority.Normal;
        
        QueueEvent(HealthEventType.UnitDamaged, priority, eventData,
                  $"{unit.name} took {damage} damage from {attacker?.GetDisplayInfo() ?? "unknown"}");
    }
    
    /// <summary>
    /// Handles unit healed events
    /// </summary>
    private void HandleUnitHealed(Unit unit, int healAmount)
    {
        if (!enableHealthEvents || unit == null) return;
        
        UnitHealedEventData eventData = new UnitHealedEventData
        {
            unit = unit,
            healAmount = healAmount,
            wasFullyHealed = healthManager != null ? healthManager.GetUnitHealthPercentage(unit) >= 1.0f : false
        };
        
        QueueEvent(HealthEventType.UnitHealed, EventPriority.Normal, eventData,
                  $"{unit.name} healed for {healAmount} HP");
    }
    
    /// <summary>
    /// Handles unit died events
    /// </summary>
    private void HandleUnitDied(Unit unit, IAttacker killer)
    {
        if (!enableDeathEvents || unit == null) return;
        
        UnitDiedEventData eventData = new UnitDiedEventData
        {
            unit = unit,
            killer = killer,
            team = unit.Team
        };
        
        QueueEvent(HealthEventType.UnitDied, EventPriority.High, eventData,
                  $"{unit.name} ({unit.Team}) killed by {killer?.GetDisplayInfo() ?? "unknown"}");
    }
    
    /// <summary>
    /// Handles team eliminated events
    /// </summary>
    private void HandleTeamEliminated(UnitTeam team, List<Unit> eliminatedUnits)
    {
        if (!enableTeamEvents) return;
        
        // Find surviving team
        UnitTeam survivingTeam = UnitTeam.Blue;
        foreach (UnitTeam t in System.Enum.GetValues(typeof(UnitTeam)))
        {
            if (t != team)
            {
                survivingTeam = t;
                break;
            }
        }
        
        TeamEliminatedEventData eventData = new TeamEliminatedEventData
        {
            team = team,
            eliminatedUnits = eliminatedUnits,
            survivingTeam = survivingTeam
        };
        
        QueueEvent(HealthEventType.TeamEliminated, EventPriority.Critical, eventData,
                  $"Team {team} eliminated! {eliminatedUnits.Count} units destroyed");
    }
    
    /// <summary>
    /// Handles last unit remaining events
    /// </summary>
    private void HandleLastUnitRemaining(Unit lastUnit)
    {
        if (!enableDeathEvents || lastUnit == null) return;
        
        QueueEvent(HealthEventType.LastUnitRemaining, EventPriority.Critical, lastUnit,
                  $"{lastUnit.name} is the last unit remaining!");
    }
    
    /// <summary>
    /// Handles win condition met events
    /// </summary>
    private void HandleWinConditionMet(WinConditionChecker.WinConditionResult result, UnitTeam winningTeam, string reason)
    {
        if (!enableWinConditionEvents) return;
        
        GameEndedEventData eventData = new GameEndedEventData
        {
            result = result,
            winningTeam = winningTeam,
            reason = reason,
            gameDuration = winConditionChecker?.GameDuration ?? 0f
        };
        
        QueueEvent(HealthEventType.GameEnded, EventPriority.Critical, eventData,
                  $"Game ended: {result} - {reason}");
    }
    
    /// <summary>
    /// Handles team victory events
    /// </summary>
    private void HandleTeamVictory(UnitTeam victoriousTeam)
    {
        if (!enableWinConditionEvents) return;
        
        QueueEvent(HealthEventType.WinCondition, EventPriority.Critical, victoriousTeam,
                  $"Team {victoriousTeam} victory!");
    }
    
    /// <summary>
    /// Handles team defeat events
    /// </summary>
    private void HandleTeamDefeat(UnitTeam defeatedTeam)
    {
        if (!enableWinConditionEvents) return;
        
        QueueEvent(HealthEventType.WinCondition, EventPriority.High, defeatedTeam,
                  $"Team {defeatedTeam} defeated!");
    }
    
    /// <summary>
    /// Handles game draw events
    /// </summary>
    private void HandleGameDraw()
    {
        if (!enableWinConditionEvents) return;
        
        QueueEvent(HealthEventType.GameEnded, EventPriority.Critical, null,
                  "Game ended in a draw!");
    }
    
    #endregion
    
    #region Event Queue Management
    
    /// <summary>
    /// Queues an event for processing
    /// </summary>
    private void QueueEvent(HealthEventType type, EventPriority priority, object data, string description)
    {
        // Filter duplicate events if enabled
        if (filterDuplicateEvents && IsDuplicateEvent(type, data, description))
        {
            duplicateEventsFiltered++;
            return;
        }
        
        HealthEvent healthEvent = new HealthEvent(type, priority, data, description);
        
        // Handle queue overflow
        if (eventQueue.Count >= maxEventsPerFrame && queueOverflowEvents)
        {
            // Remove lowest priority events to make room
            RemoveLowestPriorityEvents();
        }
        
        eventQueue.Enqueue(healthEvent);
        eventsQueuedThisSession++;
        
        // Add to recent events for duplicate filtering
        recentEvents.Add(healthEvent);
        CleanupRecentEvents();
        
        if (logAllEvents || (logHighPriorityEvents && priority >= EventPriority.High))
        {
            Debug.Log($"HealthEventBroadcaster: Queued {type} event - {description}");
        }
    }
    
    /// <summary>
    /// Checks if an event is a duplicate
    /// </summary>
    private bool IsDuplicateEvent(HealthEventType type, object data, string description)
    {
        float currentTime = Time.time;
        
        foreach (HealthEvent recentEvent in recentEvents)
        {
            if (currentTime - recentEvent.timestamp <= duplicateEventTimeWindow &&
                recentEvent.type == type &&
                recentEvent.description == description)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Removes lowest priority events from queue
    /// </summary>
    private void RemoveLowestPriorityEvents()
    {
        // Convert queue to list, remove lowest priority items, convert back
        List<HealthEvent> eventList = new List<HealthEvent>(eventQueue);
        eventList.Sort((a, b) => b.priority.CompareTo(a.priority));
        
        // Remove bottom 25% of events
        int removeCount = Mathf.Max(1, eventList.Count / 4);
        eventList.RemoveRange(eventList.Count - removeCount, removeCount);
        
        eventQueue.Clear();
        foreach (HealthEvent evt in eventList)
        {
            eventQueue.Enqueue(evt);
        }
    }
    
    /// <summary>
    /// Cleans up old recent events
    /// </summary>
    private void CleanupRecentEvents()
    {
        float currentTime = Time.time;
        recentEvents.RemoveAll(evt => currentTime - evt.timestamp > duplicateEventTimeWindow);
    }
    
    /// <summary>
    /// Processes the event queue
    /// </summary>
    private void ProcessEventQueue()
    {
        if (isProcessingEvents) return;
        
        isProcessingEvents = true;
        int processedCount = 0;
        
        while (eventQueue.Count > 0 && processedCount < maxEventsPerFrame)
        {
            HealthEvent healthEvent = eventQueue.Dequeue();
            ProcessEvent(healthEvent);
            processedCount++;
            eventsProcessedThisFrame++;
        }
        
        isProcessingEvents = false;
    }
    
    /// <summary>
    /// Processes a single event
    /// </summary>
    private void ProcessEvent(HealthEvent healthEvent)
    {
        totalEventsProcessed++;
        
        // Update statistics
        if (trackEventStatistics)
        {
            eventStatistics[healthEvent.type]++;
        }
        
        // Process based on event type
        switch (healthEvent.type)
        {
            case HealthEventType.HealthChanged:
                ProcessHealthChangedEvent((HealthChangedEventData)healthEvent.data);
                break;
                
            case HealthEventType.UnitDamaged:
                ProcessUnitDamagedEvent((UnitDamagedEventData)healthEvent.data);
                break;
                
            case HealthEventType.UnitHealed:
                ProcessUnitHealedEvent((UnitHealedEventData)healthEvent.data);
                break;
                
            case HealthEventType.UnitDied:
                ProcessUnitDiedEvent((UnitDiedEventData)healthEvent.data);
                break;
                
            case HealthEventType.TeamEliminated:
                ProcessTeamEliminatedEvent((TeamEliminatedEventData)healthEvent.data);
                break;
                
            case HealthEventType.GameEnded:
                ProcessGameEndedEvent((GameEndedEventData)healthEvent.data);
                break;
                
            case HealthEventType.WinCondition:
                ProcessWinConditionEvent(healthEvent.data);
                break;
                
            case HealthEventType.LastUnitRemaining:
                ProcessLastUnitRemainingEvent((Unit)healthEvent.data);
                break;
        }
        
        if (logAllEvents)
        {
            Debug.Log($"HealthEventBroadcaster: Processed {healthEvent.type} event - {healthEvent.description}");
        }
    }
    
    #endregion
    
    #region Event Processing
    
    /// <summary>
    /// Processes health changed events
    /// </summary>
    private void ProcessHealthChangedEvent(HealthChangedEventData eventData)
    {
        // Update UI - will be implemented in Task 2.1.4
        if (enableUIEvents && updateHealthBarsOnHealthChange)
        {
            // uiManager.UpdateUnitHealthBar(eventData.unit, eventData.healthPercentage);
        }
        
        // Check for critical health
        if (eventData.healthPercentage <= 0.25f && eventData.healthPercentage > 0f)
        {
            ProcessCriticalHealthEvent(eventData.unit);
        }
    }
    
    /// <summary>
    /// Processes unit damaged events
    /// </summary>
    private void ProcessUnitDamagedEvent(UnitDamagedEventData eventData)
    {
        // Play audio
        if (enableAudioEvents && damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSound, audioVolume);
        }
        
        // Show visual effect
        if (enableVisualEffectEvents && damageEffectPrefab != null && eventData.unit != null)
        {
            GameObject effect = Instantiate(damageEffectPrefab, eventData.unit.transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        // Show damage numbers - will be implemented in Task 2.1.4
        if (enableUIEvents && showDamageNumbers)
        {
            // uiManager.ShowDamageNumber(eventData.unit, eventData.damage, eventData.wasCritical);
        }
    }
    
    /// <summary>
    /// Processes unit healed events
    /// </summary>
    private void ProcessUnitHealedEvent(UnitHealedEventData eventData)
    {
        // Play audio
        if (enableAudioEvents && healingSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(healingSound, audioVolume);
        }
        
        // Show visual effect
        if (enableVisualEffectEvents && healingEffectPrefab != null && eventData.unit != null)
        {
            GameObject effect = Instantiate(healingEffectPrefab, eventData.unit.transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        // Show healing numbers - will be implemented in Task 2.1.4
        if (enableUIEvents && showHealingNumbers)
        {
            // uiManager.ShowHealingNumber(eventData.unit, eventData.healAmount);
        }
    }
    
    /// <summary>
    /// Processes unit died events
    /// </summary>
    private void ProcessUnitDiedEvent(UnitDiedEventData eventData)
    {
        // Play audio
        if (enableAudioEvents && deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound, audioVolume);
        }
        
        // Show visual effect
        if (enableVisualEffectEvents && deathEffectPrefab != null && eventData.unit != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, eventData.unit.transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        // Update UI - will be implemented in Task 2.1.4
        if (enableUIEvents)
        {
            // uiManager.UpdateUnitDeathStatus(eventData.unit, eventData.team);
        }
    }
    
    /// <summary>
    /// Processes team eliminated events
    /// </summary>
    private void ProcessTeamEliminatedEvent(TeamEliminatedEventData eventData)
    {
        // Update UI - will be implemented in Task 2.1.4
        if (enableUIEvents)
        {
            // uiManager.ShowTeamEliminationMessage(eventData.team, eventData.eliminatedUnits.Count);
        }
    }
    
    /// <summary>
    /// Processes game ended events
    /// </summary>
    private void ProcessGameEndedEvent(GameEndedEventData eventData)
    {
        // Play appropriate audio
        if (enableAudioEvents && audioSource != null)
        {
            AudioClip clipToPlay = null;
            
            switch (eventData.result)
            {
                case WinConditionChecker.WinConditionResult.BlueWins:
                case WinConditionChecker.WinConditionResult.RedWins:
                    clipToPlay = victorySound;
                    break;
                    
                case WinConditionChecker.WinConditionResult.Draw:
                case WinConditionChecker.WinConditionResult.Timeout:
                    clipToPlay = defeatSound; // Or use a draw sound if available
                    break;
            }
            
            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay, audioVolume);
            }
        }
        
        // Update UI - will be implemented in Task 2.1.4
        if (enableUIEvents && updateGameStatusOnWinCondition)
        {
            // uiManager.ShowGameEndScreen(eventData.result, eventData.winningTeam, eventData.reason);
        }
    }
    
    /// <summary>
    /// Processes win condition events
    /// </summary>
    private void ProcessWinConditionEvent(object data)
    {
        // Handle different win condition types
        if (data is UnitTeam team)
        {
            if (enableUIEvents)
            {
                // uiManager.ShowWinConditionMessage(team);
            }
        }
    }
    
    /// <summary>
    /// Processes last unit remaining events
    /// </summary>
    private void ProcessLastUnitRemainingEvent(Unit lastUnit)
    {
        if (enableUIEvents)
        {
            // uiManager.ShowLastUnitRemainingMessage(lastUnit);
        }
    }
    
    /// <summary>
    /// Processes critical health events
    /// </summary>
    private void ProcessCriticalHealthEvent(Unit unit)
    {
        QueueEvent(HealthEventType.CriticalHealth, EventPriority.High, unit,
                  $"{unit.name} has critical health!");
        
        if (enableUIEvents)
        {
            // uiManager.ShowCriticalHealthWarning(unit);
        }
    }
    
    #endregion
    
    #region Public Broadcasting Interface
    
    /// <summary>
    /// Broadcasts unit health changed event
    /// </summary>
    public void BroadcastHealthChanged(Unit unit, int oldHealth, int newHealth)
    {
        HandleUnitHealthChanged(unit, oldHealth, newHealth);
    }
    
    /// <summary>
    /// Broadcasts unit damaged event
    /// </summary>
    public void BroadcastUnitDamaged(Unit unit, int damage, IAttacker attacker)
    {
        HandleUnitDamaged(unit, damage, attacker);
    }
    
    /// <summary>
    /// Broadcasts unit healed event
    /// </summary>
    public void BroadcastUnitHealed(Unit unit, int healAmount)
    {
        HandleUnitHealed(unit, healAmount);
    }
    
    /// <summary>
    /// Broadcasts unit died event
    /// </summary>
    public void BroadcastUnitDied(Unit unit, IAttacker killer)
    {
        HandleUnitDied(unit, killer);
    }
    
    /// <summary>
    /// Broadcasts team eliminated event
    /// </summary>
    public void BroadcastTeamEliminated(UnitTeam team, List<Unit> eliminatedUnits)
    {
        HandleTeamEliminated(team, eliminatedUnits);
    }
    
    /// <summary>
    /// Broadcasts game end event
    /// </summary>
    public void BroadcastGameEnd(WinConditionChecker.WinConditionResult result, UnitTeam winningTeam, string reason)
    {
        HandleWinConditionMet(result, winningTeam, reason);
    }
    
    #endregion
    
    #region Coroutines
    
    /// <summary>
    /// Event processing coroutine
    /// </summary>
    private IEnumerator EventProcessingCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.05f); // 20 FPS event processing
            
            if (eventQueue.Count > 0 && eventsProcessedThisFrame < maxEventsPerFrame)
            {
                ProcessEventQueue();
            }
        }
    }
    
    #endregion
    
    #region Public Utility Methods
    
    /// <summary>
    /// Gets event broadcasting statistics
    /// </summary>
    public string GetEventStatistics()
    {
        string stats = $"Event Stats - Total: {totalEventsProcessed}, " +
                      $"Pending: {PendingEvents}, " +
                      $"Duplicates Filtered: {duplicateEventsFiltered}, " +
                      $"Queued This Session: {eventsQueuedThisSession}";
        
        if (trackEventStatistics)
        {
            stats += "\nEvent Type Breakdown:";
            foreach (var kvp in eventStatistics)
            {
                if (kvp.Value > 0)
                {
                    stats += $"\n  {kvp.Key}: {kvp.Value}";
                }
            }
        }
        
        return stats;
    }
    
    /// <summary>
    /// Forces processing of all queued events
    /// </summary>
    public void FlushEventQueue()
    {
        while (eventQueue.Count > 0)
        {
            HealthEvent healthEvent = eventQueue.Dequeue();
            ProcessEvent(healthEvent);
        }
        
        if (enableEventDebugging)
        {
            Debug.Log("HealthEventBroadcaster: Flushed all queued events");
        }
    }
    
    /// <summary>
    /// Clears all event data
    /// </summary>
    public void ClearEventData()
    {
        eventQueue.Clear();
        recentEvents.Clear();
        
        foreach (HealthEventType eventType in new List<HealthEventType>(eventStatistics.Keys))
        {
            eventStatistics[eventType] = 0;
        }
        
        totalEventsProcessed = 0;
        duplicateEventsFiltered = 0;
        eventsQueuedThisSession = 0;
        
        if (enableEventDebugging)
        {
            Debug.Log("HealthEventBroadcaster: Cleared all event data");
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
            healthManager.OnUnitHealthChanged -= HandleUnitHealthChanged;
            healthManager.OnUnitDamaged -= HandleUnitDamaged;
            healthManager.OnUnitHealed -= HandleUnitHealed;
            healthManager.OnUnitDied -= HandleUnitDied;
        }
        
        if (deathHandler != null)
        {
            deathHandler.OnTeamEliminated -= HandleTeamEliminated;
            deathHandler.OnLastUnitRemaining -= HandleLastUnitRemaining;
        }
        
        if (winConditionChecker != null)
        {
            winConditionChecker.OnWinConditionMet -= HandleWinConditionMet;
            winConditionChecker.OnTeamVictory -= HandleTeamVictory;
            winConditionChecker.OnTeamDefeat -= HandleTeamDefeat;
            winConditionChecker.OnGameDraw -= HandleGameDraw;
        }
        
        // Clear collections
        eventQueue?.Clear();
        recentEvents?.Clear();
        eventStatistics?.Clear();
    }
}