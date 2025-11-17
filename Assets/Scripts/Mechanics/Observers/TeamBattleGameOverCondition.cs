using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TeamBattleGameOverCondition : GameOverCondition
{
    private int _requiredSurvivors;
    private Dictionary<int, HashSet<Unit>> _teamUnits = new Dictionary<int, HashSet<Unit>>();
    private HashSet<int> _eliminatedTeams = new HashSet<int>();

    void Start()
    {
        // Get the number of winners from MenuSettings
        _requiredSurvivors = MenuSettings.Instance.numberOfWinners;

        Debug.Log($"Team Battle: Playing until {_requiredSurvivors} survivor(s) remain");
    }

    public override void RegisterUnit(Unit unit)
    {
        base.RegisterUnit(unit);

        // Track units by team
        if (!_teamUnits.ContainsKey(unit.teamID))
        {
            _teamUnits[unit.teamID] = new HashSet<Unit>();
        }
        _teamUnits[unit.teamID].Add(unit);
    }

    public override void UnregisterUnit(Unit unit)
    {
        _registeredUnits.Remove(unit);

        // Remove from team tracking
        if (_teamUnits.ContainsKey(unit.teamID))
        {
            _teamUnits[unit.teamID].Remove(unit);

            // Check if this team is now eliminated
            if (_teamUnits[unit.teamID].Count == 0)
            {
                _eliminatedTeams.Add(unit.teamID);
                Debug.Log($"Team {unit.teamID} has been eliminated!");
            }
            CheckGameOverCondition();
        }
    }

    protected override void CheckGameOverCondition()
    {
        // Count remaining teams (teams with at least one unit alive)
        int remainingTeams = 0;
        int totalRemainingUnits = 0;

        foreach (var teamPair in _teamUnits)
        {
            if (teamPair.Value.Count > 0 && !_eliminatedTeams.Contains(teamPair.Key))
            {
                remainingTeams++;
                totalRemainingUnits += teamPair.Value.Count;
            }
        }

        // Calculate expected units per team
        int totalPlayers = MenuSettings.Instance.playerNames.Length;
        int numberOfTeams = _teamUnits.Count;

        if (numberOfTeams <= 0)
        {
            Debug.LogError("Invalid number of teams in TeamBattleGameOverCondition");
            return;
        }

        int unitsPerTeam = totalPlayers / numberOfTeams;

        // Game over when remaining teams * units per team <= required survivors
        int potentialSurvivors = remainingTeams * unitsPerTeam;

        if (potentialSurvivors <= _requiredSurvivors)
        {
            Debug.Log($"Game Over: {remainingTeams} teams remaining with {potentialSurvivors} potential survivors (need {_requiredSurvivors})");
            TriggerGameOver();
        }
    }

    // Get all teams that are still alive
    public Dictionary<int, List<Unit>> GetRemainingTeams()
    {
        var remainingTeams = new Dictionary<int, List<Unit>>();

        foreach (var teamPair in _teamUnits)
        {
            if (teamPair.Value.Count > 0 && !_eliminatedTeams.Contains(teamPair.Key))
            {
                remainingTeams[teamPair.Key] = teamPair.Value.ToList();
            }
        }

        return remainingTeams;
    }

    public int GetRemainingTeamCount()
    {
        return _teamUnits.Count(pair => pair.Value.Count > 0 && !_eliminatedTeams.Contains(pair.Key));
    }

    public bool IsTeamEliminated(int teamId)
    {
        return _eliminatedTeams.Contains(teamId);
    }
}