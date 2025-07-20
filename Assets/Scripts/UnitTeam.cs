using UnityEngine;
using System;

/// <summary>
/// Enumeration defining different teams in the tactical arena
/// </summary>
public enum UnitTeam
{
    Blue = 0,
    Red = 1,
    Neutral = 2
}

/// <summary>
/// Data structure containing team-specific configuration and properties
/// </summary>
[System.Serializable]
public struct TeamData
{
    [Header("Team Identity")]
    public UnitTeam team;
    public string teamName;
    public Color teamColor;
    public Material teamMaterial;
    
    [Header("Team Properties")]
    public int maxUnits;
    public bool isPlayerControlled;
    public bool isAIControlled;
    
    [Header("Tactical Properties")]
    public GridCoordinate[] startingPositions;
    public float teamMoraleBonus;
    public bool enableTeamCoordination;
    
    /// <summary>
    /// Gets default team data for the specified team
    /// </summary>
    public static TeamData GetDefault(UnitTeam team)
    {
        switch (team)
        {
            case UnitTeam.Blue:
                return new TeamData
                {
                    team = UnitTeam.Blue,
                    teamName = "Blue Team",
                    teamColor = new Color(0.2f, 0.4f, 0.8f, 1f),
                    maxUnits = 4,
                    isPlayerControlled = true,
                    isAIControlled = false,
                    startingPositions = new GridCoordinate[]
                    {
                        new GridCoordinate(0, 0),
                        new GridCoordinate(1, 0),
                        new GridCoordinate(0, 1),
                        new GridCoordinate(1, 1)
                    },
                    teamMoraleBonus = 1.0f,
                    enableTeamCoordination = true
                };
                
            case UnitTeam.Red:
                return new TeamData
                {
                    team = UnitTeam.Red,
                    teamName = "Red Team",
                    teamColor = new Color(0.8f, 0.2f, 0.2f, 1f),
                    maxUnits = 4,
                    isPlayerControlled = false,
                    isAIControlled = true,
                    startingPositions = new GridCoordinate[]
                    {
                        new GridCoordinate(2, 3),
                        new GridCoordinate(3, 3),
                        new GridCoordinate(2, 2),
                        new GridCoordinate(3, 2)
                    },
                    teamMoraleBonus = 1.0f,
                    enableTeamCoordination = true
                };
                
            case UnitTeam.Neutral:
                return new TeamData
                {
                    team = UnitTeam.Neutral,
                    teamName = "Neutral",
                    teamColor = Color.gray,
                    maxUnits = 0,
                    isPlayerControlled = false,
                    isAIControlled = false,
                    startingPositions = new GridCoordinate[0],
                    teamMoraleBonus = 1.0f,
                    enableTeamCoordination = false
                };
                
            default:
                return GetDefault(UnitTeam.Neutral);
        }
    }
    
    /// <summary>
    /// Checks if this team is allied with another team
    /// </summary>
    public bool IsAlliedWith(UnitTeam otherTeam)
    {
        if (team == UnitTeam.Neutral || otherTeam == UnitTeam.Neutral)
            return false;
            
        return team == otherTeam;
    }
    
    /// <summary>
    /// Checks if this team is hostile to another team
    /// </summary>
    public bool IsHostileTo(UnitTeam otherTeam)
    {
        if (team == UnitTeam.Neutral || otherTeam == UnitTeam.Neutral)
            return false;
            
        return team != otherTeam;
    }
    
    /// <summary>
    /// Gets the opposite team (for two-team scenarios)
    /// </summary>
    public UnitTeam GetOpposingTeam()
    {
        switch (team)
        {
            case UnitTeam.Blue:
                return UnitTeam.Red;
            case UnitTeam.Red:
                return UnitTeam.Blue;
            default:
                return UnitTeam.Neutral;
        }
    }
}

/// <summary>
/// Utility class for team-related operations and calculations
/// </summary>
public static class TeamUtilities
{
    /// <summary>
    /// Gets all available team types
    /// </summary>
    public static UnitTeam[] GetAllTeams()
    {
        return new UnitTeam[] { UnitTeam.Blue, UnitTeam.Red, UnitTeam.Neutral };
    }
    
    /// <summary>
    /// Gets all combat teams (excludes neutral)
    /// </summary>
    public static UnitTeam[] GetCombatTeams()
    {
        return new UnitTeam[] { UnitTeam.Blue, UnitTeam.Red };
    }
    
    /// <summary>
    /// Converts team enum to display string
    /// </summary>
    public static string GetTeamDisplayName(UnitTeam team)
    {
        TeamData data = TeamData.GetDefault(team);
        return data.teamName;
    }
    
    /// <summary>
    /// Gets team color for UI and visual representation
    /// </summary>
    public static Color GetTeamColor(UnitTeam team)
    {
        TeamData data = TeamData.GetDefault(team);
        return data.teamColor;
    }
    
    /// <summary>
    /// Checks if a team is player-controlled by default
    /// </summary>
    public static bool IsPlayerTeam(UnitTeam team)
    {
        TeamData data = TeamData.GetDefault(team);
        return data.isPlayerControlled;
    }
    
    /// <summary>
    /// Checks if a team is AI-controlled by default
    /// </summary>
    public static bool IsAITeam(UnitTeam team)
    {
        TeamData data = TeamData.GetDefault(team);
        return data.isAIControlled;
    }
    
    /// <summary>
    /// Gets default starting positions for a team
    /// </summary>
    public static GridCoordinate[] GetStartingPositions(UnitTeam team)
    {
        TeamData data = TeamData.GetDefault(team);
        return data.startingPositions;
    }
    
    /// <summary>
    /// Calculates distance between two teams' starting areas
    /// </summary>
    public static float GetTeamSeparationDistance(UnitTeam team1, UnitTeam team2)
    {
        GridCoordinate[] positions1 = GetStartingPositions(team1);
        GridCoordinate[] positions2 = GetStartingPositions(team2);
        
        if (positions1.Length == 0 || positions2.Length == 0)
            return 0f;
        
        float minDistance = float.MaxValue;
        
        foreach (GridCoordinate pos1 in positions1)
        {
            foreach (GridCoordinate pos2 in positions2)
            {
                float distance = pos1.DistanceTo(pos2);
                if (distance < minDistance)
                    minDistance = distance;
            }
        }
        
        return minDistance;
    }
    
    /// <summary>
    /// Validates team configuration for tactical balance
    /// </summary>
    public static bool ValidateTeamBalance(UnitTeam[] teams)
    {
        if (teams == null || teams.Length < 2)
            return false;
        
        // Check that there are at least two different teams
        bool hasBlue = false;
        bool hasRed = false;
        
        foreach (UnitTeam team in teams)
        {
            if (team == UnitTeam.Blue) hasBlue = true;
            if (team == UnitTeam.Red) hasRed = true;
        }
        
        return hasBlue && hasRed;
    }
    
    /// <summary>
    /// Gets team priority for turn order (lower number goes first)
    /// </summary>
    public static int GetTeamTurnPriority(UnitTeam team)
    {
        switch (team)
        {
            case UnitTeam.Blue:
                return 1; // Player team goes first
            case UnitTeam.Red:
                return 2; // AI team goes second
            case UnitTeam.Neutral:
                return 999; // Neutral last (if ever)
            default:
                return 999;
        }
    }
}

/// <summary>
/// Component for managing team-specific events and coordination
/// </summary>
public class TeamCoordinator : MonoBehaviour
{
    [Header("Team Configuration")]
    [SerializeField] private UnitTeam team;
    [SerializeField] private TeamData teamData;
    
    [Header("Team State")]
    [SerializeField] private bool isActive = true;
    [SerializeField] private float teamMorale = 100f;
    [SerializeField] private int activeUnits = 0;
    
    // Events for team coordination
    public System.Action<UnitTeam> OnTeamActivated;
    public System.Action<UnitTeam> OnTeamDeactivated;
    public System.Action<UnitTeam, float> OnTeamMoraleChanged;
    public System.Action<UnitTeam, int> OnTeamUnitCountChanged;
    
    // Public properties
    public UnitTeam Team => team;
    public TeamData TeamData => teamData;
    public bool IsActive => isActive;
    public float TeamMorale => teamMorale;
    public int ActiveUnits => activeUnits;
    
    void Awake()
    {
        InitializeTeam();
    }
    
    void Start()
    {
        UpdateUnitCount();
    }
    
    /// <summary>
    /// Initializes team coordinator
    /// </summary>
    private void InitializeTeam()
    {
        if (teamData.team == UnitTeam.Neutral)
        {
            teamData = TeamData.GetDefault(team);
        }
        
        Debug.Log($"TeamCoordinator initialized for {teamData.teamName}");
    }
    
    /// <summary>
    /// Sets the team for this coordinator
    /// </summary>
    public void SetTeam(UnitTeam newTeam)
    {
        team = newTeam;
        teamData = TeamData.GetDefault(newTeam);
        
        Debug.Log($"Team coordinator set to {teamData.teamName}");
    }
    
    /// <summary>
    /// Activates or deactivates the team
    /// </summary>
    public void SetTeamActive(bool active)
    {
        if (isActive == active) return;
        
        isActive = active;
        
        if (active)
        {
            OnTeamActivated?.Invoke(team);
        }
        else
        {
            OnTeamDeactivated?.Invoke(team);
        }
        
        Debug.Log($"Team {teamData.teamName} {(active ? "activated" : "deactivated")}");
    }
    
    /// <summary>
    /// Updates team morale
    /// </summary>
    public void ModifyMorale(float change)
    {
        float previousMorale = teamMorale;
        teamMorale = Mathf.Clamp(teamMorale + change, 0f, 100f);
        
        if (Mathf.Abs(teamMorale - previousMorale) > 0.1f)
        {
            OnTeamMoraleChanged?.Invoke(team, teamMorale);
        }
    }
    
    /// <summary>
    /// Updates the count of active units
    /// </summary>
    public void UpdateUnitCount()
    {
        // Count units belonging to this team
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        int count = 0;
        
        foreach (Unit unit in allUnits)
        {
            if (unit.Team == team && unit.IsAlive)
            {
                count++;
            }
        }
        
        if (count != activeUnits)
        {
            activeUnits = count;
            OnTeamUnitCountChanged?.Invoke(team, activeUnits);
        }
    }
    
    /// <summary>
    /// Checks if team has any active units
    /// </summary>
    public bool HasActiveUnits()
    {
        UpdateUnitCount();
        return activeUnits > 0;
    }
    
    /// <summary>
    /// Gets all units belonging to this team
    /// </summary>
    public Unit[] GetTeamUnits()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        System.Collections.Generic.List<Unit> teamUnits = new System.Collections.Generic.List<Unit>();
        
        foreach (Unit unit in allUnits)
        {
            if (unit.Team == team)
            {
                teamUnits.Add(unit);
            }
        }
        
        return teamUnits.ToArray();
    }
    
    /// <summary>
    /// Gets team status information
    /// </summary>
    public TeamStatusInfo GetTeamStatus()
    {
        return new TeamStatusInfo
        {
            team = team,
            teamName = teamData.teamName,
            isActive = isActive,
            teamMorale = teamMorale,
            activeUnits = activeUnits,
            maxUnits = teamData.maxUnits,
            isPlayerControlled = teamData.isPlayerControlled,
            isAIControlled = teamData.isAIControlled
        };
    }
}

/// <summary>
/// Information structure for team status
/// </summary>
[System.Serializable]
public struct TeamStatusInfo
{
    public UnitTeam team;
    public string teamName;
    public bool isActive;
    public float teamMorale;
    public int activeUnits;
    public int maxUnits;
    public bool isPlayerControlled;
    public bool isAIControlled;
    
    public override string ToString()
    {
        return $"{teamName}: {activeUnits}/{maxUnits} units, {teamMorale:F0}% morale, {(isActive ? "Active" : "Inactive")}";
    }
}