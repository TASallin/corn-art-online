using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BattleRoyaleLeaderboard : LeaderboardBase
{
    private float _gameStartTime;
    private HashSet<int> _survivedUnits = new HashSet<int>(); // Track units that survived to the end

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
        // In Battle Royale, all units are on team 0
        if (!_unitsInRank.Contains(unit))
        {
            _unitsInRank.Add(unit);
        }

        int unitId = GetUnitId(unit);
        if (!_scores.ContainsKey(unitId))
        {
            _scores[unitId] = new UnitScore(unit);
            _scores[unitId].SetScore(0f); // Initialize with 0
            Debug.Log($"Registered unit {unit.unitName} (ID: {unitId}) for battle royale");
        }
    }

    protected override void HandleUnitDestroyed(Unit unit, Unit killer)
    {
        // Call base implementation for elimination tracking
        base.HandleUnitDestroyed(unit, killer);

        // Calculate survival score for this unit
        float survivedTime = Time.time - _gameStartTime;
        int unitId = GetUnitId(unit);

        if (_scores.ContainsKey(unitId))
        {
            // Score is just the number of seconds survived
            _scores[unitId].SetScore(survivedTime);
            Debug.Log($"{unit.unitName} survived {survivedTime:F1} seconds (eliminated)");
        }

        // Make sure the unit is in the ranking list
        if (!_unitsInRank.Contains(unit))
        {
            _unitsInRank.Add(unit);
        }
    }

    private void CalculateFinalScores()
    {
        float finalTime = Time.time - _gameStartTime;
        Debug.Log($"Calculating final scores. Battle Royale lasted {finalTime:F1} seconds");

        // Award final scores to all units
        foreach (var unit in _unitsInRank)
        {
            int unitId = GetUnitId(unit);
            if (_scores.ContainsKey(unitId))
            {
                if (unit.GetAlive())
                {
                    // Survived to the end: time + 100 bonus
                    _scores[unitId].SetScore(finalTime + 100f);
                    _survivedUnits.Add(unitId);

                    //string playerName = unit.HasPlayerName() ? unit.GetPlayerName() : unit.unitName;
                    //Debug.Log($"{playerName} survived to the end! Score: {finalTime:F1} + 100 = {finalTime + 100f:F1}");
                }
                // Units that died already have their survival time set in HandleUnitDestroyed
            }
        }

        UpdateRanking();
        DisplayFinalResults();
    }

    protected override void UpdateRanking()
    {
        // Sort by score (higher is better - survivors have highest scores)
        _unitsInRank.Sort((a, b) =>
        {
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

    public override void DisplayFinalResults()
    {
        Debug.Log("===== BATTLE ROYALE RESULTS =====");

        for (int i = 0; i < _unitsInRank.Count; i++)
        {
            Unit unit = _unitsInRank[i];
            int unitId = GetUnitId(unit);

            int rank = i + 1;
            float score = _scores.ContainsKey(unitId) ? _scores[unitId].TotalScore : 0f;
            //string playerName = unit.HasPlayerName() ? unit.GetPlayerName() : unit.unitName;
            //string status = _survivedUnits.Contains(unitId) ? "(WINNER)" : "(ELIMINATED)";

            //Debug.Log($"Rank #{rank}: {playerName} - Score: {score:F1} {status}");
        }

        Debug.Log("=================================");
    }
}