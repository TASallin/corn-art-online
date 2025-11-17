using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TeamBattleLeaderboard : LeaderboardBase
{
    private float _gameStartTime;
    private Dictionary<int, TeamScore> _teamScores = new Dictionary<int, TeamScore>();
    private Dictionary<int, HashSet<Unit>> _teamUnits = new Dictionary<int, HashSet<Unit>>();
    private HashSet<int> _survivingTeams = new HashSet<int>();

    private class TeamScore
    {
        public int teamId;
        public float survivalTime;
        public bool survived;
        public List<Unit> members = new List<Unit>();

        public TeamScore(int id)
        {
            teamId = id;
            survivalTime = 0f;
            survived = false;
        }

        public float GetTotalScore()
        {
            return survived ? survivalTime + 100f : survivalTime;
        }
    }

    // Observe unit created, destroyed, and game over events
    public override EventType[] ObservedEventTypes => new[]
    {
        EventType.UnitCreated,
        EventType.UnitDestroyed,
        EventType.GameOver
    };

    protected override void OnEnable()
    {
        base.OnEnable();
        _gameStartTime = Time.time;
        Observable.AddObserver(this, ObservedEventTypes);
    }

    public override void OnEventReceived(EventData data)
    {
        if (!_isObserving)
            return;

        switch (data.Type)
        {
            case EventType.UnitCreated:
                RegisterUnit(data.SourceUnit);
                break;

            case EventType.UnitDestroyed:
                HandleUnitDestroyed(data.TargetUnit, data.SourceUnit);
                break;

            case EventType.GameOver:
                CalculateFinalScores();
                break;
        }
    }

    private void RegisterUnit(Unit unit)
    {
        // Track unit in ranking
        if (!_unitsInRank.Contains(unit))
        {
            _unitsInRank.Add(unit);
        }

        // Track unit by team
        if (!_teamUnits.ContainsKey(unit.teamID))
        {
            _teamUnits[unit.teamID] = new HashSet<Unit>();
            _teamScores[unit.teamID] = new TeamScore(unit.teamID);
        }

        _teamUnits[unit.teamID].Add(unit);
        _teamScores[unit.teamID].members.Add(unit);

        // Initialize individual score
        int unitId = GetUnitId(unit);
        if (!_scores.ContainsKey(unitId))
        {
            _scores[unitId] = new UnitScore(unit);
            _scores[unitId].SetScore(0f);
        }

        Debug.Log($"Registered unit {unit.unitName} (ID: {unitId}) for Team {unit.teamID}");
    }

    protected override void HandleUnitDestroyed(Unit unit, Unit killer)
    {
        // Call base implementation for elimination tracking
        base.HandleUnitDestroyed(unit, killer);

        // Remove from team tracking
        if (_teamUnits.ContainsKey(unit.teamID))
        {
            _teamUnits[unit.teamID].Remove(unit);

            // Check if entire team is eliminated
            if (_teamUnits[unit.teamID].Count == 0)
            {
                float teamSurvivalTime = Time.time - _gameStartTime;
                _teamScores[unit.teamID].survivalTime = teamSurvivalTime;
                _teamScores[unit.teamID].survived = false;

                Debug.Log($"Team {unit.teamID} eliminated after {teamSurvivalTime:F1} seconds");
            }
        }
    }

    private void CalculateFinalScores()
    {
        float finalTime = Time.time - _gameStartTime;
        Debug.Log($"Calculating final scores. Team Battle lasted {finalTime:F1} seconds");

        // Determine which teams survived
        foreach (var teamPair in _teamUnits)
        {
            if (teamPair.Value.Count > 0)
            {
                _survivingTeams.Add(teamPair.Key);
                _teamScores[teamPair.Key].survivalTime = finalTime;
                _teamScores[teamPair.Key].survived = true;

                Debug.Log($"Team {teamPair.Key} survived to the end!");
            }
        }

        // Apply team scores to all team members (including eliminated ones)
        foreach (var teamScore in _teamScores.Values)
        {
            float score = teamScore.GetTotalScore();

            foreach (var unit in teamScore.members)
            {
                int unitId = GetUnitId(unit);
                if (_scores.ContainsKey(unitId))
                {
                    _scores[unitId].SetScore(score);
                }
            }
        }

        UpdateRanking();
        DisplayFinalResults();
    }

    protected override void UpdateRanking()
    {
        // Sort units, but group by team
        _unitsInRank.Sort((a, b) =>
        {
            // First compare team scores
            float aTeamScore = _teamScores.ContainsKey(a.teamID) ? _teamScores[a.teamID].GetTotalScore() : 0f;
            float bTeamScore = _teamScores.ContainsKey(b.teamID) ? _teamScores[b.teamID].GetTotalScore() : 0f;

            int teamComparison = bTeamScore.CompareTo(aTeamScore);
            if (teamComparison != 0)
                return teamComparison;

            // If same team or same score, sort by unit name
            return string.Compare(a.unitName, b.unitName);
        });
    }

    public override void StopObserving()
    {
        // Make sure to calculate final scores before stopping
        if (_isObserving)
        {
            CalculateFinalScores();
        }
        base.StopObserving();
    }

    public override void DisplayFinalResults()
    {
        Debug.Log("===== TEAM BATTLE RESULTS =====");

        // Display by team
        var teamsSorted = _teamScores.Values.OrderByDescending(t => t.GetTotalScore()).ToList();

        for (int i = 0; i < teamsSorted.Count; i++)
        {
            var team = teamsSorted[i];
            string status = team.survived ? "(WINNERS)" : "(ELIMINATED)";
            Debug.Log($"Rank #{i + 1}: Team {team.teamId} - Score: {team.GetTotalScore():F1} {status}");

            // List team members
            foreach (var unit in team.members)
            {
                string playerName = unit.playerName;
                string aliveStatus = unit.GetAlive() ? "Alive" : "KIA";
                Debug.Log($"  - {playerName} ({aliveStatus})");
            }
        }

        Debug.Log("=================================");

        // Also track winners/eliminated for MenuSettings
        MenuSettings.Instance.winners.Clear();
        MenuSettings.Instance.eliminated.Clear();

        foreach (var team in teamsSorted)
        {
            foreach (var unit in team.members)
            {
                if (team.survived)
                {
                    MenuSettings.Instance.winners.Add(unit.playerName);
                } else
                {
                    MenuSettings.Instance.eliminated.Add(unit.playerName);
                }
            }
        }
    }
}