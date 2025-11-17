using UnityEngine;
using System.Collections.Generic;

public class HotPotatoLeaderboard : LeaderboardBase
{
    private float _gameStartTime;
    private Dictionary<int, float> _unitPotatoTime = new Dictionary<int, float>(); // Total time each unit held potatoes
    private Dictionary<int, float> _unitLastPotatoStartTime = new Dictionary<int, float>(); // When current potato session started
    private Dictionary<int, float> _unitDeathTime = new Dictionary<int, float>(); // When each unit died (if they died)
    private HashSet<int> _unitsTriggeredByPotato = new HashSet<int>(); // Units killed by potato explosion

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
        if (!_unitsInRank.Contains(unit))
        {
            _unitsInRank.Add(unit);
        }

        int unitId = GetUnitId(unit);
        if (!_scores.ContainsKey(unitId))
        {
            _scores[unitId] = new UnitScore(unit);
            _scores[unitId].SetScore(0f);

            // Initialize tracking data
            _unitPotatoTime[unitId] = 0f;
            _unitLastPotatoStartTime[unitId] = -1f; // -1 means no potato currently

            Debug.Log($"Registered unit {unit.unitName} (ID: {unitId}) for hot potato");
        }
    }

    protected override void HandleUnitDestroyed(Unit unit, Unit killer)
    {
        base.HandleUnitDestroyed(unit, killer);

        int unitId = GetUnitId(unit);
        float currentTime = Time.time;

        // Record when this unit died
        _unitDeathTime[unitId] = currentTime;

        // If this unit currently has a potato, update their potato time
        if (_unitLastPotatoStartTime.ContainsKey(unitId) && _unitLastPotatoStartTime[unitId] >= 0)
        {
            float sessionTime = currentTime - _unitLastPotatoStartTime[unitId];
            _unitPotatoTime[unitId] += sessionTime;
            _unitLastPotatoStartTime[unitId] = -1f; // No longer has potato
        }

        // Check if this was triggered by a potato explosion (killer would be null for potato deaths)
        if (killer == null)
        {
            _unitsTriggeredByPotato.Add(unitId);
            Debug.Log($"{unit.playerName} was eliminated by potato explosion");
        }

        if (!_unitsInRank.Contains(unit))
        {
            _unitsInRank.Add(unit);
        }
    }

    // Call this when a potato gets transferred TO a unit
    public void OnPotatoTransferredTo(Unit unit)
    {
        int unitId = GetUnitId(unit);
        if (_unitLastPotatoStartTime.ContainsKey(unitId))
        {
            _unitLastPotatoStartTime[unitId] = Time.time;
            Debug.Log($"{unit.playerName} received potato at time {Time.time - _gameStartTime:F1}s");
        }
    }

    // Call this when a potato gets transferred FROM a unit
    public void OnPotatoTransferredFrom(Unit unit)
    {
        int unitId = GetUnitId(unit);
        if (_unitLastPotatoStartTime.ContainsKey(unitId) && _unitLastPotatoStartTime[unitId] >= 0)
        {
            float sessionTime = Time.time - _unitLastPotatoStartTime[unitId];
            _unitPotatoTime[unitId] += sessionTime;
            _unitLastPotatoStartTime[unitId] = -1f;
            Debug.Log($"{unit.playerName} passed potato after holding it for {sessionTime:F1}s (total: {_unitPotatoTime[unitId]:F1}s)");
        }
    }

    // Call this when a unit initially gets a potato at game start
    public void OnPotatoInitiallyAssigned(Unit unit)
    {
        OnPotatoTransferredTo(unit);
    }

    private void CalculateFinalScores()
    {
        float finalTime = Time.time - _gameStartTime;
        Debug.Log($"Calculating final scores. Hot Potato game lasted {finalTime:F1} seconds");

        // Update potato time for any units still holding potatoes
        foreach (var kvp in _unitLastPotatoStartTime)
        {
            int unitId = kvp.Key;
            float startTime = kvp.Value;

            if (startTime >= 0) // Unit still has a potato
            {
                float sessionTime = finalTime - (startTime - _gameStartTime);
                _unitPotatoTime[unitId] += sessionTime;
            }
        }

        foreach (var unit in _unitsInRank)
        {
            int unitId = GetUnitId(unit);
            if (_scores.ContainsKey(unitId))
            {
                float score = CalculateScore(unit, unitId, finalTime);
                _scores[unitId].SetScore(score);
            }
        }

        UpdateRanking();
        DisplayFinalResults();
    }

    private float CalculateScore(Unit unit, int unitId, float totalGameTime)
    {
        bool isAlive = unit.GetAlive();
        bool triggeredByPotato = _unitsTriggeredByPotato.Contains(unitId);

        // Get survival time
        float survivalTime;
        if (isAlive)
        {
            survivalTime = totalGameTime;
        } else if (_unitDeathTime.ContainsKey(unitId))
        {
            survivalTime = _unitDeathTime[unitId] - _gameStartTime;
        } else
        {
            survivalTime = 0f;
        }

        // Get potato time
        float potatoTime = _unitPotatoTime.ContainsKey(unitId) ? _unitPotatoTime[unitId] : 0f;

        // Calculate time without potato
        float timeWithoutPotato = survivalTime - potatoTime;

        // Base score: survival time + time without potato
        float score = survivalTime + timeWithoutPotato;

        // Bonus for units NOT killed by potato explosion (includes survivors and other deaths)
        if (!triggeredByPotato)
        {
            score += 5f * totalGameTime;
        }

        Debug.Log($"{unit.playerName}: Survival={survivalTime:F1}s, Potato={potatoTime:F1}s, " +
                 $"NoPotato={timeWithoutPotato:F1}s, Alive={isAlive}, PotatoKill={triggeredByPotato}, Score={score:F1}");

        return score;
    }

    protected override void UpdateRanking()
    {
        _unitsInRank.Sort((a, b) =>
        {
            int aId = GetUnitId(a);
            int bId = GetUnitId(b);

            float aScore = _scores.ContainsKey(aId) ? _scores[aId].TotalScore : 0f;
            float bScore = _scores.ContainsKey(bId) ? _scores[bId].TotalScore : 0f;

            return bScore.CompareTo(aScore); // Higher scores rank higher
        });
    }

    public override void DisplayFinalResults()
    {
        Debug.Log("===== HOT POTATO RESULTS =====");

        for (int i = 0; i < _unitsInRank.Count; i++)
        {
            Unit unit = _unitsInRank[i];
            int unitId = GetUnitId(unit);

            int rank = i + 1;
            float score = _scores.ContainsKey(unitId) ? _scores[unitId].TotalScore : 0f;
            bool triggeredByPotato = _unitsTriggeredByPotato.Contains(unitId);
            float potatoTime = _unitPotatoTime.ContainsKey(unitId) ? _unitPotatoTime[unitId] : 0f;

            string status;
            if (unit.GetAlive())
                status = "(SURVIVED)";
            else if (triggeredByPotato)
                status = "(POTATO)";
            else
                status = "(ELIMINATED)";

            Debug.Log($"Rank #{rank}: {unit.unitName} - Score: {score:F1} - Potato Time: {potatoTime:F1}s {status}");
        }

        Debug.Log("===============================");
    }
}