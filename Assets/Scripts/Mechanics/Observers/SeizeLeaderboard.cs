using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SeizeLeaderboard : LeaderboardBase
{
    [SerializeField] private int _playerTeamId = 1;

    private SeizeGameOverCondition _seizeCondition;
    private Dictionary<int, float> _proximityScores = new Dictionary<int, float>();

    private const float MAX_POINTS = 1000.0f;
    private const float CAPTURE_BONUS = 500.0f;
    private const float MAX_SCORING_DISTANCE = 30.0f; // Max distance for scoring

    // Observe game events
    public override EventType[] ObservedEventTypes => new[]
    {
        EventType.UnitCreated,
        EventType.UnitDestroyed,
        EventType.GameOver
    };

    void Start()
    {
        _seizeCondition = FindObjectOfType<SeizeGameOverCondition>();

        if (_seizeCondition == null)
        {
            Debug.LogError("SeizeLeaderboard requires SeizeGameOverCondition in scene!");
        }
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
            }
        }
    }

    protected override void HandleUnitDestroyed(Unit unit, Unit killer)
    {
        base.HandleUnitDestroyed(unit, killer);

        // Include player units in the ranking even if they're destroyed
        if (unit.teamID == _playerTeamId && !_unitsInRank.Contains(unit))
        {
            _unitsInRank.Add(unit);
            int unitId = GetUnitId(unit);
            if (!_scores.ContainsKey(unitId))
            {
                _scores[unitId] = new UnitScore(unit);
            }
        }
    }

    private void CalculateFinalScores()
    {
        if (_seizeCondition == null)
            return;

        var distances = _seizeCondition.GetUnitDistances();
        float maxPossibleDistance = _seizeCondition.GetMaxPossibleDistance();

        // Set scores directly from distances
        foreach (var unit in _unitsInRank)
        {
            if (unit.teamID != _playerTeamId)
                continue;

            int unitId = GetUnitId(unit);
            if (!_scores.ContainsKey(unitId))
                continue;

            // Find the matching unit in the distances dictionary
            Unit matchingUnit = distances.Keys.FirstOrDefault(u => u.unitID == unit.unitID);
            float distance = matchingUnit != null ?
                distances[matchingUnit] :
                maxPossibleDistance;

            _scores[unitId].AddScore(distance);
            Debug.Log($"{unit.unitName} final distance set to: {distance}");
        }

        UpdateRanking();
    }

    protected override void UpdateRanking()
    {
        // Sort by distance (lower is better)
        _unitsInRank.Sort((a, b) =>
        {
            // Filter non-player teams to bottom
            if (a.teamID != _playerTeamId && b.teamID == _playerTeamId) return 1;
            if (a.teamID == _playerTeamId && b.teamID != _playerTeamId) return -1;

            int aId = GetUnitId(a);
            int bId = GetUnitId(b);

            float aDistance = _scores.ContainsKey(aId) ? _scores[aId].TotalScore : float.MaxValue;
            float bDistance = _scores.ContainsKey(bId) ? _scores[bId].TotalScore : float.MaxValue;

            // Lower distances rank higher
            return aDistance.CompareTo(bDistance);
        });

        // Debug rankings
        Debug.Log("===== SEIZE FINAL RANKINGS =====");
        for (int i = 0; i < _unitsInRank.Count; i++)
        {
            if (_unitsInRank[i].teamID == _playerTeamId)
            {
                int unitId = GetUnitId(_unitsInRank[i]);
                float distance = _scores[unitId].TotalScore;
                Debug.Log($"Rank {i + 1}: {_unitsInRank[i].unitName} - Distance: {distance:F2}");
            }
        }
        Debug.Log("==============================");
    }

    public override void StopObserving()
    {
        base.StopObserving();
        CalculateFinalScores();
    }

    public override List<Unit> GetUnitsInRank()
    {
        // Only return player team units
        return _unitsInRank.Where(unit => unit.teamID == _playerTeamId).ToList();
    }

    public override Dictionary<int, UnitScore> GetScores()
    {
        // Only return scores for player team units
        Dictionary<int, UnitScore> playerScores = new Dictionary<int, UnitScore>();
        foreach (Unit unit in _unitsInRank)
        {
            if (unit.teamID == _playerTeamId)
            {
                int unitId = GetUnitId(unit);
                if (_scores.ContainsKey(unitId))
                {
                    playerScores[unitId] = _scores[unitId];
                }
            }
        }
        return playerScores;
    }
}