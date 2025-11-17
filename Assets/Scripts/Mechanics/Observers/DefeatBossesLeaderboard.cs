using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DefeatBossesLeaderboard : LeaderboardBase
{
    [SerializeField] private int _bossTeamId = 2; // Team ID for the bosses
    [SerializeField] private int _playerTeamId = 1; // Team ID for the players

    // Track total damage dealt by each player to all bosses
    private Dictionary<int, float> _totalDamageDealtToBosses = new Dictionary<int, float>();
    // Track which bosses have been defeated and who killed them
    private List<(int killerId, string bossName)> _defeatedBosses = new List<(int, string)>();
    // Track boss IDs
    private HashSet<int> _registeredBossIds = new HashSet<int>();

    private const float POINTS_FOR_DAMAGE_CONTRIBUTION = 100.0f; // Total points awarded for all damage contribution
    private const float POINTS_PER_BOSS_KILL = 100.0f; // Points awarded per boss killing blow

    // Observe unit created, destroyed, and damaged events
    public override EventType[] ObservedEventTypes => new[]
    {
        EventType.UnitCreated,
        EventType.UnitDestroyed,
        EventType.UnitDamaged,
        EventType.GameOver
    };

    protected override void OnEnable()
    {
        base.OnEnable();
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

            case EventType.UnitDamaged:
                if (data.SourceUnit && data.TargetUnit)
                {
                    HandleUnitDamaged(data.SourceUnit, data.TargetUnit, data.Value);
                }
                break;

            case EventType.GameOver:
                CalculateFinalScores();
                break;
        }
    }

    private void RegisterUnit(Unit unit)
    {
        // If this is a boss unit, track it
        if (unit.teamID == _bossTeamId && unit.IsBoss())
        {
            int bossId = GetUnitId(unit);
            _registeredBossIds.Add(bossId);
        }
        // If this is a player unit, add it to the ranking
        else if (unit.teamID == _playerTeamId)
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

            if (!_totalDamageDealtToBosses.ContainsKey(unitId))
            {
                _totalDamageDealtToBosses[unitId] = 0f;
            }
        }
    }

    private void HandleUnitDamaged(Unit damageDealer, Unit damagedUnit, float damageAmount)
    {
        // Only track damage if the dealer is on the player team and the target is a boss
        if (damageDealer.teamID == _playerTeamId &&
            damagedUnit.teamID == _bossTeamId &&
            damagedUnit.IsBoss())
        {
            int dealerId = GetUnitId(damageDealer);
            int bossId = GetUnitId(damagedUnit);

            if (_registeredBossIds.Contains(bossId))
            {
                // Track damage dealt by this player to all bosses
                if (!_totalDamageDealtToBosses.ContainsKey(dealerId))
                {
                    _totalDamageDealtToBosses[dealerId] = 0f;
                }

                _totalDamageDealtToBosses[dealerId] += damageAmount;
            }
        }
    }

    protected override void HandleUnitDestroyed(Unit unit, Unit killer)
    {
        // Call the base implementation to handle elimination tracking
        base.HandleUnitDestroyed(unit, killer);

        // Continue with the boss-specific logic
        if (unit.teamID == _bossTeamId && unit.IsBoss())
        {
            if (killer != null && killer.teamID == _playerTeamId)
            {
                int killerId = GetUnitId(killer);
                _defeatedBosses.Add((killerId, unit.unitName));

                // Award kill points immediately
                if (_scores.ContainsKey(killerId))
                {
                    _scores[killerId].AddScore(POINTS_PER_BOSS_KILL);
                    Debug.Log($"Player {killer.unitName} killed boss {unit.unitName}, earning {POINTS_PER_BOSS_KILL} points");
                }
            } else
            {
                // If no killer (e.g., died to environment), just record the boss defeat
                _defeatedBosses.Add((-1, unit.unitName));
            }
        }

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
        // Calculate total damage dealt by all players
        float totalDamageToAllBosses = _totalDamageDealtToBosses.Values.Sum();

        if (totalDamageToAllBosses > 0)
        {
            // Distribute POINTS_FOR_DAMAGE_CONTRIBUTION based on damage contribution
            foreach (var playerDamagePair in _totalDamageDealtToBosses)
            {
                int playerId = playerDamagePair.Key;
                float playerDamage = playerDamagePair.Value;

                float damagePercentage = playerDamage / totalDamageToAllBosses;
                float scoreToAdd = damagePercentage * POINTS_FOR_DAMAGE_CONTRIBUTION;

                if (_scores.ContainsKey(playerId))
                {
                    _scores[playerId].AddScore(scoreToAdd);
                    Debug.Log($"Player {_scores[playerId].Unit.unitName} dealt {playerDamage}/{totalDamageToAllBosses} ({damagePercentage * 100}%) damage to all bosses, earning {scoreToAdd} points");
                }
            }
        }

        UpdateRanking();

        // Debug summary
        Debug.Log("===== FINAL SCORE SUMMARY =====");
        Debug.Log($"Total bosses defeated: {_defeatedBosses.Count}");
        Debug.Log($"Total damage points distributed: {POINTS_FOR_DAMAGE_CONTRIBUTION}");
        Debug.Log($"Total kill bonus points: {_defeatedBosses.Count * POINTS_PER_BOSS_KILL}");
        Debug.Log($"Expected total score: {POINTS_FOR_DAMAGE_CONTRIBUTION + (_defeatedBosses.Count * POINTS_PER_BOSS_KILL)}");

        float actualTotalScore = 0f;
        foreach (var unit in _unitsInRank)
        {
            if (unit.teamID == _playerTeamId)
            {
                int unitId = GetUnitId(unit);
                if (_scores.ContainsKey(unitId))
                {
                    actualTotalScore += _scores[unitId].TotalScore;
                }
            }
        }
        Debug.Log($"Actual total score: {actualTotalScore}");
        Debug.Log("==============================");
    }

    protected override void UpdateRanking()
    {
        // Sort by total score
        _unitsInRank.Sort((a, b) =>
        {
            if (a.teamID != _playerTeamId && b.teamID == _playerTeamId) return 1;
            if (a.teamID == _playerTeamId && b.teamID != _playerTeamId) return -1;

            int aId = GetUnitId(a);
            int bId = GetUnitId(b);

            float aScore = _scores.ContainsKey(aId) ? _scores[aId].TotalScore : 0f;
            float bScore = _scores.ContainsKey(bId) ? _scores[bId].TotalScore : 0f;

            // Higher scores rank higher (so reverse the comparison)
            return bScore.CompareTo(aScore);
        });
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