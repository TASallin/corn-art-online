// LeaderboardBase.cs (updated)
using System.Collections.Generic;
using UnityEngine;

public abstract class LeaderboardBase : MonoBehaviour, IObserver
{
    protected Dictionary<int, UnitScore> _scores = new Dictionary<int, UnitScore>();
    protected List<Unit> _unitsInRank = new List<Unit>();
    protected bool _isObserving = true;
    protected Dictionary<int, EliminatorData> _eliminatedByMap = new Dictionary<int, EliminatorData>();
    protected Dictionary<int, List<EnemyDefeatData>> _defeatedEnemiesMap = new Dictionary<int, List<EnemyDefeatData>>();

    public virtual EventType[] ObservedEventTypes => new[] { EventType.UnitScored, EventType.UnitDestroyed, EventType.GameOver };

    protected virtual void OnEnable()
    {
        // Initialize collections
        if (_eliminatedByMap == null)
        {
            _eliminatedByMap = new Dictionary<int, EliminatorData>();
        }

        if (_defeatedEnemiesMap == null)
        {
            _defeatedEnemiesMap = new Dictionary<int, List<EnemyDefeatData>>();
        }

        Observable.AddObserver(this, ObservedEventTypes);
    }

    protected virtual void OnDisable()
    {
        Observable.RemoveObserver(this);
    }

    public virtual void OnEventReceived(EventData data)
    {
        if (!_isObserving)
            return;

        switch (data.Type)
        {
            case EventType.UnitScored:
                if (data.SourceUnit != null)
                {
                    RecordScore(data.SourceUnit, data.Value);
                }
                break;

            case EventType.UnitDestroyed:
                if (data.SourceUnit != null)
                {
                    HandleUnitDestroyed(data.TargetUnit, data.SourceUnit);
                }
                break;
        }
    }

    protected virtual void RecordScore(Unit unit, float scoreValue)
    {
        int unitId = unit.unitID; // Use unitID consistently

        if (!_scores.ContainsKey(unitId))
            _scores[unitId] = new UnitScore(unit);

        _scores[unitId].AddScore(scoreValue);

        Debug.Log($"Recorded score {scoreValue} for unit {unit.unitName} (ID: {unitId}), total: {_scores[unitId].TotalScore}");

        UpdateRanking();
    }

    protected virtual void HandleUnitDestroyed(Unit unit, Unit killer)
    {
        if (unit == null)
        {
            Debug.LogError("Destroyed unit is null in HandleUnitDestroyed");
            return;
        }

        // Add unit to ranking if not already present
        if (!_unitsInRank.Contains(unit))
        {
            _unitsInRank.Add(unit);
        }

        // Track who eliminated this unit
        if (killer != null)
        {
            int unitId = unit.unitID;
            int killerId = killer.unitID;

            // Store elimination data for the defeated unit
            _eliminatedByMap[unitId] = new EliminatorData(killer);

            // Track this defeat for the killer
            if (!_defeatedEnemiesMap.ContainsKey(killerId))
            {
                _defeatedEnemiesMap[killerId] = new List<EnemyDefeatData>();
            }

            _defeatedEnemiesMap[killerId].Add(new EnemyDefeatData(unit));

            Debug.Log($"Recorded defeat: {killer.unitName} (ID: {killerId}) defeated {unit.unitName} (ID: {unitId})");
        }
    }

    public virtual Dictionary<int, EliminatorData> GetEliminationTracker()
    {
        if (_eliminatedByMap == null)
        {
            Debug.LogWarning("Elimination tracker is null in LeaderboardBase");
            return new Dictionary<int, EliminatorData>();
        }

        Debug.Log($"Returning elimination data with {_eliminatedByMap.Count} entries");
        foreach (var entry in _eliminatedByMap)
        {
            Debug.Log($"Unit ID {entry.Key} was eliminated by {entry.Value.Name}");
        }

        return new Dictionary<int, EliminatorData>(_eliminatedByMap);
    }

    public virtual Dictionary<int, List<EnemyDefeatData>> GetDefeatedEnemiesTracker()
    {
        if (_defeatedEnemiesMap == null)
        {
            Debug.LogWarning("Defeated enemies tracker is null in LeaderboardBase");
            return new Dictionary<int, List<EnemyDefeatData>>();
        }

        return new Dictionary<int, List<EnemyDefeatData>>(_defeatedEnemiesMap);
    }

    protected virtual void UpdateRanking()
    {
        // Default implementation - override in subclasses
    }

    public virtual void DisplayFinalResults()
    {
        Debug.Log("===== FINAL RANKINGS =====");

        for (int i = 0; i < _unitsInRank.Count; i++)
        {
            Unit unit = _unitsInRank[i];
            int unitId = GetUnitId(unit);
            float score = _scores.ContainsKey(unitId) ? _scores[unitId].TotalScore : 0f;

            Debug.Log($"Rank #{i + 1}: Unit ID {unitId} (Player {unit.teamID}) - Score: {score}");
        }

        Debug.Log("=========================");
    }

    public virtual void StopObserving()
    {
        _isObserving = false;
    }

    protected virtual int GetUnitId(Unit unit)
    {
        return unit.unitID;
    }

    // Make the UnitScore class public instead of protected
    public class UnitScore
    {
        public Unit Unit { get; private set; }
        public float TotalScore { get; private set; }
        public int Kills { get; private set; }

        public UnitScore(Unit unit)
        {
            Unit = unit;
            TotalScore = 0;
            Kills = 0;
        }

        public void AddScore(float score)
        {
            TotalScore += score;
        }

        public void AddKill()
        {
            Kills++;
            TotalScore += 100; // Example: 100 points per kill
        }

        public void SetScore(float score)
        {
            TotalScore = score;
        }
    }

    public virtual List<Unit> GetUnitsInRank()
    {
        return new List<Unit>(_unitsInRank);
    }

    public virtual Dictionary<int, UnitScore> GetScores()
    {
        return new Dictionary<int, UnitScore>(_scores);
    }
}