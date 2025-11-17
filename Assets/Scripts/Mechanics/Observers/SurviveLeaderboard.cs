using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SurviveLeaderboard : LeaderboardBase
{
    [SerializeField] private int _playerTeamId = 1; // Team trying to survive
    private float _gameStartTime;
    private HashSet<int> _survivedUnits = new HashSet<int>(); // Track units that survived to the end

    // Override to observe GameOver events as well
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
                RegisterPlayerUnit(data.SourceUnit);
                break;

            case EventType.UnitDestroyed:
                HandleUnitDestroyed(data.TargetUnit, data.SourceUnit);
                break;

            case EventType.GameOver:
                CalculateFinalScores();
                break;
        }
    }

    private void RegisterPlayerUnit(Unit unit)
    {
        // Only track player team units
        if (unit.teamID == _playerTeamId)
        {
            if (!_unitsInRank.Contains(unit))
            {
                _unitsInRank.Add(unit);
            }

            int unitId = GetUnitId(unit);
            if (!_scores.ContainsKey(unitId))
            {
                _scores[unitId] = new UnitScore(unit);
                // Initialize with 0 - will be updated when the unit dies or game ends
                _scores[unitId].SetScore(0f);
                Debug.Log($"Registered player unit {unit.unitName} (ID: {unitId}) for survive mode");
            }
        }
    }

    protected override void HandleUnitDestroyed(Unit unit, Unit killer)
    {
        // Call base implementation for tracking
        base.HandleUnitDestroyed(unit, killer);

        // Only care about player team units for scoring
        if (unit.teamID == _playerTeamId)
        {
            // Calculate survival score for this unit
            float survivedTime = Time.time - _gameStartTime;
            int unitId = GetUnitId(unit);

            if (_scores.ContainsKey(unitId))
            {
                // Score is just the number of seconds survived
                _scores[unitId].SetScore(survivedTime);
                Debug.Log($"Player {unit.unitName} survived {survivedTime:F1} seconds (eliminated)");
            }

            // Make sure the unit is in the ranking list
            if (!_unitsInRank.Contains(unit))
            {
                _unitsInRank.Add(unit);
            }
        }
    }

    private void CalculateFinalScores()
    {
        float finalTime = Time.time - _gameStartTime;
        Debug.Log($"Calculating final scores. Game lasted {finalTime:F1} seconds");

        // Award final scores to all player units
        foreach (var unit in _unitsInRank)
        {
            if (unit.teamID == _playerTeamId)
            {
                int unitId = GetUnitId(unit);
                if (_scores.ContainsKey(unitId))
                {
                    if (unit.GetAlive())
                    {
                        // Survived to the end: time + 100 bonus
                        _scores[unitId].SetScore(finalTime + 100f);
                        _survivedUnits.Add(unitId);
                        Debug.Log($"Player {unit.unitName} survived to the end! Score: {finalTime:F1} + 100 = {finalTime + 100f:F1}");
                    }
                    // Units that died already have their survival time set in HandleUnitDestroyed
                    else
                    {
                        Debug.Log($"Player {unit.unitName} final score: {_scores[unitId].TotalScore:F1} (eliminated during game)");
                    }
                }
            }
        }

        UpdateRanking();

        // Debug final rankings
        Debug.Log("===== SURVIVE MODE FINAL SCORES =====");
        for (int i = 0; i < _unitsInRank.Count; i++)
        {
            Unit unit = _unitsInRank[i];
            if (unit.teamID == _playerTeamId)
            {
                int unitId = GetUnitId(unit);
                float score = _scores.ContainsKey(unitId) ? _scores[unitId].TotalScore : 0f;
                string status = _survivedUnits.Contains(unitId) ? "(SURVIVED)" : "(ELIMINATED)";
                Debug.Log($"Rank #{i + 1}: {unit.unitName} - Score: {score:F1} {status}");
            }
        }
        Debug.Log("=====================================");
    }

    protected override void UpdateRanking()
    {
        // Sort by score (higher is better)
        _unitsInRank.Sort((a, b) =>
        {
            // Only consider player team units
            if (a.teamID != _playerTeamId && b.teamID == _playerTeamId) return 1;
            if (a.teamID == _playerTeamId && b.teamID != _playerTeamId) return -1;

            int aId = GetUnitId(a);
            int bId = GetUnitId(b);

            float aScore = _scores.ContainsKey(aId) ? _scores[aId].TotalScore : 0f;
            float bScore = _scores.ContainsKey(bId) ? _scores[bId].TotalScore : 0f;

            // Higher scores rank higher
            return bScore.CompareTo(aScore);
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

    public override List<Unit> GetUnitsInRank()
    {
        // Only return player team units, properly sorted
        return _unitsInRank.Where(unit => unit.teamID == _playerTeamId).ToList();
    }

    public override Dictionary<int, UnitScore> GetScores()
    {
        // Only return scores for player team units
        Dictionary<int, UnitScore> playerScores = new Dictionary<int, UnitScore>();
        foreach (Unit unit in GetUnitsInRank())
        {
            int unitId = GetUnitId(unit);
            if (_scores.ContainsKey(unitId))
            {
                playerScores[unitId] = _scores[unitId];
            }
        }
        return playerScores;
    }
}