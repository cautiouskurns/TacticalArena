using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

/// <summary>
/// Win condition detection and management system for tactical combat.
/// Monitors team elimination, unit counts, and victory/defeat conditions.
/// Integrates with health system and game manager for complete match resolution.
/// Part of Task 2.1.3 - Health & Damage System.
/// </summary>
public class WinConditionChecker : MonoBehaviour
{
    [Header("Win Condition Configuration")]
    [SerializeField] private WinConditionType primaryWinCondition = WinConditionType.EliminateAllEnemies;
    [SerializeField] private bool enableMultipleWinConditions = false;
    [SerializeField] private WinConditionType[] alternativeWinConditions;
    [SerializeField] private bool requireAllConditionsForWin = false;
    
    [Header("Team Elimination Settings")]
    [SerializeField] private bool enableTeamElimination = true;
    [SerializeField] private bool countDeadUnitsOnly = true;
    [SerializeField] private bool requireCompleteElimination = true;
    [SerializeField] private int minimumUnitsForTeamSurvival = 1;
    
    [Header("Health-Based Win Conditions")]
    [SerializeField] private bool enableHealthPercentageWin = false;
    [SerializeField] private float healthPercentageThreshold = 0.2f;
    [SerializeField] private bool enableLastUnitStanding = true;
    [SerializeField] private bool enableMutualDestruction = true;
    
    [Header("Time-Based Win Conditions")]
    [SerializeField] private bool enableTimeLimit = false;
    [SerializeField] private float timeLimitInSeconds = 300f;
    [SerializeField] private WinConditionResult timeLimitOutcome = WinConditionResult.Draw;
    [SerializeField] private bool enableSuddenDeath = false;
    [SerializeField] private float suddenDeathDuration = 60f;
    
    [Header("Objective-Based Win Conditions")]
    [SerializeField] private bool enableObjectiveWin = false;
    [SerializeField] private int objectivesRequiredToWin = 3;
    [SerializeField] private bool enableControlPointWin = false;
    [SerializeField] private float controlPointHoldTime = 30f;
    
    [Header("Detection Settings")]
    [SerializeField] private bool enableAutomaticChecking = true;
    [SerializeField] private float checkInterval = 0.5f;
    [SerializeField] private bool enableInstantWinDetection = true;
    [SerializeField] private float winDetectionDelay = 1.0f;
    [SerializeField] private bool preventImmediateWin = false;
    [SerializeField] private float minimumMatchDuration = 10f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableWinConditionDebugging = true;
    [SerializeField] private bool logWinConditionChecks = false;
    [SerializeField] private bool logTeamStates = false;
    [SerializeField] private bool visualizeWinConditions = false;
    [SerializeField] private bool enableTestWinConditions = false;
    
    // System references
    private HealthManager healthManager;
    private DeathHandler deathHandler;
    private HealthEventBroadcaster eventBroadcaster;
    
    // Win condition state
    private bool isCheckingWinConditions = false;
    private bool gameHasEnded = false;
    private float gameStartTime = 0f;
    private float lastWinConditionCheck = 0f;
    private Dictionary<UnitTeam, TeamStatus> teamStates;
    private List<WinConditionResult> pendingWinResults;
    
    // Win condition tracking
    private WinConditionResult currentWinResult;
    private UnitTeam winningTeam;
    private string winReason;
    private int totalWinConditionChecks = 0;
    
    // Events
    public System.Action<WinConditionResult, UnitTeam, string> OnWinConditionMet;
    public System.Action<UnitTeam> OnTeamVictory;
    public System.Action<UnitTeam> OnTeamDefeat;
    public System.Action OnGameDraw;
    public System.Action<float> OnTimeWarning; // Remaining time
    public System.Action OnSuddenDeathActivated;
    public System.Action<UnitTeam, TeamStatus> OnTeamStatusChanged;
    
    // Properties
    public bool GameHasEnded => gameHasEnded;
    public WinConditionResult CurrentWinResult => currentWinResult;
    public UnitTeam WinningTeam => winningTeam;
    public string WinReason => winReason;
    public float GameDuration => Time.time - gameStartTime;
    public float RemainingTime => enableTimeLimit ? Mathf.Max(0, timeLimitInSeconds - GameDuration) : -1f;
    public bool IsInSuddenDeath => enableSuddenDeath && GameDuration > timeLimitInSeconds;
    
    // Enums
    public enum WinConditionType
    {
        EliminateAllEnemies = 0,
        LastTeamStanding = 1,
        HealthPercentage = 2,
        TimeLimit = 3,
        ObjectiveControl = 4,
        ControlPoints = 5,
        SuddenDeath = 6,
        Custom = 7
    }
    
    public enum WinConditionResult
    {
        InProgress = 0,
        BlueWins = 1,
        RedWins = 2,
        Draw = 3,
        Timeout = 4,
        Error = 5
    }
    
    public enum TeamStatus
    {
        Active = 0,
        Weakened = 1,
        CriticallyWeakened = 2,
        Eliminated = 3,
        Victorious = 4
    }
    
    void Awake()
    {
        InitializeWinConditionChecker();
    }
    
    void Start()
    {
        FindSystemReferences();
        InitializeTeamStates();
        StartWinConditionChecking();
        RecordGameStart();
    }
    
    void Update()
    {
        if (gameHasEnded) return;
        
        // Automatic win condition checking
        if (enableAutomaticChecking && Time.time - lastWinConditionCheck >= checkInterval)
        {
            CheckWinConditions();
            lastWinConditionCheck = Time.time;
        }
        
        // Time-based updates
        if (enableTimeLimit)
        {
            UpdateTimeBasedConditions();
        }
    }
    
    /// <summary>
    /// Initializes the win condition checker
    /// </summary>
    private void InitializeWinConditionChecker()
    {
        teamStates = new Dictionary<UnitTeam, TeamStatus>();
        pendingWinResults = new List<WinConditionResult>();
        currentWinResult = WinConditionResult.InProgress;
        
        if (enableWinConditionDebugging)
        {
            Debug.Log("WinConditionChecker initialized - Tactical victory system ready");
        }
    }
    
    /// <summary>
    /// Finds references to other system components
    /// </summary>
    private void FindSystemReferences()
    {
        healthManager = GetComponent<HealthManager>();
        deathHandler = GetComponent<DeathHandler>();
        eventBroadcaster = GetComponent<HealthEventBroadcaster>();
        
        if (enableWinConditionDebugging)
        {
            Debug.Log($"WinConditionChecker found references - Health Manager: {healthManager != null}, " +
                     $"Death Handler: {deathHandler != null}, " +
                     $"Event Broadcaster: {eventBroadcaster != null}");
        }
    }
    
    /// <summary>
    /// Initializes team states
    /// </summary>
    private void InitializeTeamStates()
    {
        foreach (UnitTeam team in System.Enum.GetValues(typeof(UnitTeam)))
        {
            teamStates[team] = TeamStatus.Active;
        }
        
        UpdateAllTeamStates();
    }
    
    /// <summary>
    /// Starts win condition checking system
    /// </summary>
    private void StartWinConditionChecking()
    {
        if (enableAutomaticChecking)
        {
            StartCoroutine(WinConditionCheckingCoroutine());
        }
    }
    
    /// <summary>
    /// Records game start time
    /// </summary>
    private void RecordGameStart()
    {
        gameStartTime = Time.time;
        
        if (enableWinConditionDebugging)
        {
            Debug.Log($"WinConditionChecker: Game started at {gameStartTime}");
        }
    }
    
    #region Main Win Condition Checking
    
    /// <summary>
    /// Main win condition checking method
    /// </summary>
    public void CheckWinConditions()
    {
        if (gameHasEnded || isCheckingWinConditions)
        {
            return;
        }
        
        // Prevent immediate wins if configured
        if (preventImmediateWin && GameDuration < minimumMatchDuration)
        {
            return;
        }
        
        isCheckingWinConditions = true;
        totalWinConditionChecks++;
        
        if (logWinConditionChecks)
        {
            Debug.Log($"WinConditionChecker: Checking win conditions (check #{totalWinConditionChecks})");
        }
        
        // Update team states first
        UpdateAllTeamStates();
        
        // Check primary win condition
        WinConditionResult primaryResult = CheckWinCondition(primaryWinCondition);
        
        // Check alternative win conditions if enabled
        List<WinConditionResult> alternativeResults = new List<WinConditionResult>();
        if (enableMultipleWinConditions && alternativeWinConditions != null)
        {
            foreach (WinConditionType altCondition in alternativeWinConditions)
            {
                WinConditionResult altResult = CheckWinCondition(altCondition);
                if (altResult != WinConditionResult.InProgress)
                {
                    alternativeResults.Add(altResult);
                }
            }
        }
        
        // Determine final result
        WinConditionResult finalResult = DetermineFinalWinResult(primaryResult, alternativeResults);
        
        // Process win condition if met
        if (finalResult != WinConditionResult.InProgress)
        {
            ProcessWinCondition(finalResult);
        }
        
        isCheckingWinConditions = false;
    }
    
    /// <summary>
    /// Checks a specific win condition type
    /// </summary>
    private WinConditionResult CheckWinCondition(WinConditionType conditionType)
    {
        switch (conditionType)
        {
            case WinConditionType.EliminateAllEnemies:
                return CheckEliminationWinCondition();
                
            case WinConditionType.LastTeamStanding:
                return CheckLastTeamStandingWinCondition();
                
            case WinConditionType.HealthPercentage:
                return CheckHealthPercentageWinCondition();
                
            case WinConditionType.TimeLimit:
                return CheckTimeLimitWinCondition();
                
            case WinConditionType.ObjectiveControl:
                return CheckObjectiveWinCondition();
                
            case WinConditionType.ControlPoints:
                return CheckControlPointWinCondition();
                
            case WinConditionType.SuddenDeath:
                return CheckSuddenDeathWinCondition();
                
            case WinConditionType.Custom:
                return CheckCustomWinCondition();
                
            default:
                return WinConditionResult.InProgress;
        }
    }
    
    #endregion
    
    #region Specific Win Condition Implementations
    
    /// <summary>
    /// Checks elimination win condition (eliminate all enemies)
    /// </summary>
    private WinConditionResult CheckEliminationWinCondition()
    {
        if (!enableTeamElimination) return WinConditionResult.InProgress;
        
        List<UnitTeam> activeTeams = GetActiveTeams();
        
        if (activeTeams.Count == 0)
        {
            // All teams eliminated - draw
            winReason = "All teams eliminated";
            return WinConditionResult.Draw;
        }
        else if (activeTeams.Count == 1)
        {
            // One team remaining - victory
            winningTeam = activeTeams[0];
            winReason = $"Team {winningTeam} eliminated all enemies";
            return GetTeamWinResult(winningTeam);
        }
        
        return WinConditionResult.InProgress;
    }
    
    /// <summary>
    /// Checks last team standing win condition
    /// </summary>
    private WinConditionResult CheckLastTeamStandingWinCondition()
    {
        return CheckEliminationWinCondition(); // Same logic for this tactical game
    }
    
    /// <summary>
    /// Checks health percentage win condition
    /// </summary>
    private WinConditionResult CheckHealthPercentageWinCondition()
    {
        if (!enableHealthPercentageWin) return WinConditionResult.InProgress;
        
        Dictionary<UnitTeam, float> teamHealthPercentages = GetTeamHealthPercentages();
        
        UnitTeam strongestTeam = UnitTeam.Blue;
        float highestHealthPercentage = 0f;
        
        foreach (var kvp in teamHealthPercentages)
        {
            if (kvp.Value > highestHealthPercentage)
            {
                highestHealthPercentage = kvp.Value;
                strongestTeam = kvp.Key;
            }
        }
        
        // Check if any team has fallen below threshold
        foreach (var kvp in teamHealthPercentages)
        {
            if (kvp.Key != strongestTeam && kvp.Value <= healthPercentageThreshold)
            {
                winningTeam = strongestTeam;
                winReason = $"Team {kvp.Key} health fell below {healthPercentageThreshold:P0}";
                return GetTeamWinResult(strongestTeam);
            }
        }
        
        return WinConditionResult.InProgress;
    }
    
    /// <summary>
    /// Checks time limit win condition
    /// </summary>
    private WinConditionResult CheckTimeLimitWinCondition()
    {
        if (!enableTimeLimit) return WinConditionResult.InProgress;
        
        if (GameDuration >= timeLimitInSeconds)
        {
            winReason = $"Time limit reached ({timeLimitInSeconds}s)";
            
            // Determine winner based on current state
            if (timeLimitOutcome == WinConditionResult.Draw)
            {
                return WinConditionResult.Draw;
            }
            else
            {
                // Find team with best condition
                UnitTeam bestTeam = DetermineStrongestTeam();
                winningTeam = bestTeam;
                winReason += $" - Team {bestTeam} had advantage";
                return GetTeamWinResult(bestTeam);
            }
        }
        
        // Check for time warnings
        float remainingTime = RemainingTime;
        if (remainingTime <= 30f && remainingTime > 29f) // Warning at 30 seconds
        {
            OnTimeWarning?.Invoke(remainingTime);
        }
        
        return WinConditionResult.InProgress;
    }
    
    /// <summary>
    /// Checks objective win condition
    /// </summary>
    private WinConditionResult CheckObjectiveWinCondition()
    {
        if (!enableObjectiveWin) return WinConditionResult.InProgress;
        
        // Placeholder for objective-based win conditions
        // This would integrate with objective system if implemented
        
        return WinConditionResult.InProgress;
    }
    
    /// <summary>
    /// Checks control point win condition
    /// </summary>
    private WinConditionResult CheckControlPointWinCondition()
    {
        if (!enableControlPointWin) return WinConditionResult.InProgress;
        
        // Placeholder for control point win conditions
        // This would integrate with control point system if implemented
        
        return WinConditionResult.InProgress;
    }
    
    /// <summary>
    /// Checks sudden death win condition
    /// </summary>
    private WinConditionResult CheckSuddenDeathWinCondition()
    {
        if (!enableSuddenDeath) return WinConditionResult.InProgress;
        
        if (IsInSuddenDeath)
        {
            float suddenDeathTime = GameDuration - timeLimitInSeconds;
            
            // Trigger sudden death notification once
            if (suddenDeathTime < 1f && suddenDeathTime > 0f)
            {
                OnSuddenDeathActivated?.Invoke();
            }
            
            // Check if sudden death duration exceeded
            if (suddenDeathTime >= suddenDeathDuration)
            {
                winReason = "Sudden death time limit exceeded";
                return WinConditionResult.Draw;
            }
            
            // In sudden death, any kill wins immediately
            List<UnitTeam> activeTeams = GetActiveTeams();
            if (activeTeams.Count == 1)
            {
                winningTeam = activeTeams[0];
                winReason = $"Team {winningTeam} won in sudden death";
                return GetTeamWinResult(winningTeam);
            }
        }
        
        return WinConditionResult.InProgress;
    }
    
    /// <summary>
    /// Checks custom win condition
    /// </summary>
    private WinConditionResult CheckCustomWinCondition()
    {
        // Placeholder for custom win conditions
        // Can be extended for specific game modes
        
        return WinConditionResult.InProgress;
    }
    
    #endregion
    
    #region Team State Management
    
    /// <summary>
    /// Updates all team states
    /// </summary>
    private void UpdateAllTeamStates()
    {
        foreach (UnitTeam team in System.Enum.GetValues(typeof(UnitTeam)))
        {
            UpdateTeamState(team);
        }
    }
    
    /// <summary>
    /// Updates a specific team's state
    /// </summary>
    private void UpdateTeamState(UnitTeam team)
    {
        TeamStatus oldStatus = teamStates[team];
        TeamStatus newStatus = CalculateTeamStatus(team);
        
        if (oldStatus != newStatus)
        {
            teamStates[team] = newStatus;
            OnTeamStatusChanged?.Invoke(team, newStatus);
            
            if (logTeamStates)
            {
                Debug.Log($"WinConditionChecker: Team {team} status changed from {oldStatus} to {newStatus}");
            }
        }
    }
    
    /// <summary>
    /// Calculates a team's current status
    /// </summary>
    private TeamStatus CalculateTeamStatus(UnitTeam team)
    {
        List<Unit> teamUnits = GetTeamUnits(team);
        if (teamUnits.Count == 0) return TeamStatus.Eliminated;
        
        List<Unit> aliveUnits = GetAliveTeamUnits(team);
        if (aliveUnits.Count == 0) return TeamStatus.Eliminated;
        
        float teamHealthPercentage = GetTeamHealthPercentage(team);
        
        if (teamHealthPercentage <= 0.25f)
            return TeamStatus.CriticallyWeakened;
        else if (teamHealthPercentage <= 0.5f)
            return TeamStatus.Weakened;
        else
            return TeamStatus.Active;
    }
    
    /// <summary>
    /// Gets active teams (not eliminated)
    /// </summary>
    private List<UnitTeam> GetActiveTeams()
    {
        List<UnitTeam> activeTeams = new List<UnitTeam>();
        
        foreach (var kvp in teamStates)
        {
            if (kvp.Value != TeamStatus.Eliminated)
            {
                List<Unit> aliveTeamUnits = GetAliveTeamUnits(kvp.Key);
                if (aliveTeamUnits.Count >= minimumUnitsForTeamSurvival)
                {
                    activeTeams.Add(kvp.Key);
                }
            }
        }
        
        return activeTeams;
    }
    
    /// <summary>
    /// Gets all units for a team
    /// </summary>
    private List<Unit> GetTeamUnits(UnitTeam team)
    {
        List<Unit> teamUnits = new List<Unit>();
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        foreach (Unit unit in allUnits)
        {
            if (unit.Team == team)
            {
                teamUnits.Add(unit);
            }
        }
        
        return teamUnits;
    }
    
    /// <summary>
    /// Gets alive units for a team
    /// </summary>
    private List<Unit> GetAliveTeamUnits(UnitTeam team)
    {
        List<Unit> aliveUnits = new List<Unit>();
        
        if (healthManager != null)
        {
            aliveUnits = healthManager.GetAliveUnitsByTeam(team);
        }
        else
        {
            // Fallback method
            List<Unit> teamUnits = GetTeamUnits(team);
            foreach (Unit unit in teamUnits)
            {
                HealthComponent healthComponent = unit.GetComponent<HealthComponent>();
                if (healthComponent != null && healthComponent.IsAlive)
                {
                    aliveUnits.Add(unit);
                }
            }
        }
        
        return aliveUnits;
    }
    
    /// <summary>
    /// Gets team health percentages
    /// </summary>
    private Dictionary<UnitTeam, float> GetTeamHealthPercentages()
    {
        Dictionary<UnitTeam, float> healthPercentages = new Dictionary<UnitTeam, float>();
        
        foreach (UnitTeam team in System.Enum.GetValues(typeof(UnitTeam)))
        {
            healthPercentages[team] = GetTeamHealthPercentage(team);
        }
        
        return healthPercentages;
    }
    
    /// <summary>
    /// Gets health percentage for a specific team
    /// </summary>
    private float GetTeamHealthPercentage(UnitTeam team)
    {
        List<Unit> teamUnits = GetTeamUnits(team);
        if (teamUnits.Count == 0) return 0f;
        
        int totalCurrentHealth = 0;
        int totalMaxHealth = 0;
        
        foreach (Unit unit in teamUnits)
        {
            if (healthManager != null)
            {
                totalCurrentHealth += healthManager.GetUnitHealth(unit);
                totalMaxHealth += healthManager.GetUnitMaxHealth(unit);
            }
            else
            {
                HealthComponent healthComponent = unit.GetComponent<HealthComponent>();
                if (healthComponent != null)
                {
                    totalCurrentHealth += healthComponent.CurrentHealth;
                    totalMaxHealth += healthComponent.MaxHealth;
                }
            }
        }
        
        return totalMaxHealth > 0 ? (float)totalCurrentHealth / totalMaxHealth : 0f;
    }
    
    /// <summary>
    /// Determines the strongest team based on current conditions
    /// </summary>
    private UnitTeam DetermineStrongestTeam()
    {
        Dictionary<UnitTeam, float> teamHealthPercentages = GetTeamHealthPercentages();
        
        UnitTeam strongestTeam = UnitTeam.Blue;
        float highestScore = 0f;
        
        foreach (var kvp in teamHealthPercentages)
        {
            List<Unit> aliveUnits = GetAliveTeamUnits(kvp.Key);
            float score = kvp.Value * 0.7f + (aliveUnits.Count * 0.3f); // Weighted score
            
            if (score > highestScore)
            {
                highestScore = score;
                strongestTeam = kvp.Key;
            }
        }
        
        return strongestTeam;
    }
    
    #endregion
    
    #region Win Processing
    
    /// <summary>
    /// Determines final win result from multiple conditions
    /// </summary>
    private WinConditionResult DetermineFinalWinResult(WinConditionResult primaryResult, List<WinConditionResult> alternativeResults)
    {
        // If primary condition is met, use it
        if (primaryResult != WinConditionResult.InProgress)
        {
            return primaryResult;
        }
        
        // Check alternative conditions
        if (alternativeResults.Count > 0)
        {
            if (requireAllConditionsForWin)
            {
                // All alternative conditions must agree
                WinConditionResult firstResult = alternativeResults[0];
                if (alternativeResults.All(result => result == firstResult))
                {
                    return firstResult;
                }
            }
            else
            {
                // Any alternative condition can trigger win
                return alternativeResults[0];
            }
        }
        
        return WinConditionResult.InProgress;
    }
    
    /// <summary>
    /// Processes a met win condition
    /// </summary>
    private void ProcessWinCondition(WinConditionResult result)
    {
        if (gameHasEnded) return;
        
        currentWinResult = result;
        gameHasEnded = true;
        
        if (enableInstantWinDetection)
        {
            TriggerGameEnd();
        }
        else
        {
            StartCoroutine(DelayedGameEnd());
        }
    }
    
    /// <summary>
    /// Triggers immediate game end
    /// </summary>
    private void TriggerGameEnd()
    {
        // Trigger events based on result
        OnWinConditionMet?.Invoke(currentWinResult, winningTeam, winReason);
        
        switch (currentWinResult)
        {
            case WinConditionResult.BlueWins:
                OnTeamVictory?.Invoke(UnitTeam.Blue);
                OnTeamDefeat?.Invoke(UnitTeam.Red);
                break;
                
            case WinConditionResult.RedWins:
                OnTeamVictory?.Invoke(UnitTeam.Red);
                OnTeamDefeat?.Invoke(UnitTeam.Blue);
                break;
                
            case WinConditionResult.Draw:
            case WinConditionResult.Timeout:
                OnGameDraw?.Invoke();
                break;
        }
        
        // Broadcast event
        if (eventBroadcaster != null)
        {
            eventBroadcaster.BroadcastGameEnd(currentWinResult, winningTeam, winReason);
        }
        
        // Game manager integration would go here when available
        // gameManager.HandleGameEnd(currentWinResult, winningTeam, winReason);
        
        if (enableWinConditionDebugging)
        {
            Debug.Log($"WinConditionChecker: Game ended - {currentWinResult} " +
                     $"(Winner: {winningTeam}, Reason: {winReason})");
        }
    }
    
    /// <summary>
    /// Delayed game end coroutine
    /// </summary>
    private IEnumerator DelayedGameEnd()
    {
        yield return new WaitForSeconds(winDetectionDelay);
        TriggerGameEnd();
    }
    
    #endregion
    
    #region Time-Based Updates
    
    /// <summary>
    /// Updates time-based win conditions
    /// </summary>
    private void UpdateTimeBasedConditions()
    {
        float remainingTime = RemainingTime;
        
        // Time warnings
        if (remainingTime <= 60f && remainingTime > 59f)
        {
            OnTimeWarning?.Invoke(remainingTime);
        }
        else if (remainingTime <= 10f && remainingTime > 9f)
        {
            OnTimeWarning?.Invoke(remainingTime);
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Gets team win result enum from team
    /// </summary>
    private WinConditionResult GetTeamWinResult(UnitTeam team)
    {
        switch (team)
        {
            case UnitTeam.Blue:
                return WinConditionResult.BlueWins;
            case UnitTeam.Red:
                return WinConditionResult.RedWins;
            default:
                return WinConditionResult.Draw;
        }
    }
    
    /// <summary>
    /// Win condition checking coroutine
    /// </summary>
    private IEnumerator WinConditionCheckingCoroutine()
    {
        while (!gameHasEnded)
        {
            yield return new WaitForSeconds(checkInterval);
            
            if (enableAutomaticChecking)
            {
                CheckWinConditions();
            }
        }
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Forces immediate win condition check
    /// </summary>
    public void ForceWinConditionCheck()
    {
        CheckWinConditions();
    }
    
    /// <summary>
    /// Forces a specific team to win
    /// </summary>
    public void ForceTeamWin(UnitTeam team, string reason = "Forced win")
    {
        if (gameHasEnded) return;
        
        winningTeam = team;
        winReason = reason;
        currentWinResult = GetTeamWinResult(team);
        
        ProcessWinCondition(currentWinResult);
    }
    
    /// <summary>
    /// Forces a draw
    /// </summary>
    public void ForceDraw(string reason = "Forced draw")
    {
        if (gameHasEnded) return;
        
        winReason = reason;
        currentWinResult = WinConditionResult.Draw;
        
        ProcessWinCondition(currentWinResult);
    }
    
    /// <summary>
    /// Gets win condition statistics
    /// </summary>
    public string GetWinConditionStats()
    {
        List<UnitTeam> activeTeams = GetActiveTeams();
        Dictionary<UnitTeam, float> healthPercentages = GetTeamHealthPercentages();
        
        string stats = $"Win Condition Stats - Game Duration: {GameDuration:F1}s, " +
                      $"Active Teams: {activeTeams.Count}, " +
                      $"Checks: {totalWinConditionChecks}";
        
        foreach (var kvp in healthPercentages)
        {
            List<Unit> aliveUnits = GetAliveTeamUnits(kvp.Key);
            stats += $"\n  {kvp.Key}: {aliveUnits.Count} units, {kvp.Value:P0} health";
        }
        
        if (enableTimeLimit)
        {
            stats += $"\n  Time Remaining: {RemainingTime:F1}s";
        }
        
        return stats;
    }
    
    /// <summary>
    /// Resets win condition checker
    /// </summary>
    public void ResetWinConditions()
    {
        gameHasEnded = false;
        isCheckingWinConditions = false;
        currentWinResult = WinConditionResult.InProgress;
        winningTeam = UnitTeam.Blue;
        winReason = "";
        gameStartTime = Time.time;
        totalWinConditionChecks = 0;
        
        InitializeTeamStates();
        
        if (enableWinConditionDebugging)
        {
            Debug.Log("WinConditionChecker: Reset all win conditions");
        }
    }
    
    #endregion
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        // Clear event references
        OnWinConditionMet = null;
        OnTeamVictory = null;
        OnTeamDefeat = null;
        OnGameDraw = null;
        OnTimeWarning = null;
        OnSuddenDeathActivated = null;
        OnTeamStatusChanged = null;
        
        // Clear collections
        teamStates?.Clear();
        pendingWinResults?.Clear();
    }
}