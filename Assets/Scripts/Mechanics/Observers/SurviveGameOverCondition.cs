using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SurviveGameOverCondition : GameOverCondition
{
    [SerializeField] private int _playerTeamId = 1; // Team trying to survive
    private List<Unit> _playerUnits = new List<Unit>();
    private int _numberOfWinners;

    void Start()
    {
        // Get number of winners from MenuSettings
        _numberOfWinners = 1; // Default
        if (MenuSettings.Instance != null)
        {
            _numberOfWinners = MenuSettings.Instance.numberOfWinners;
            if (_numberOfWinners < 1) _numberOfWinners = 1;
        }

        Debug.Log($"Survive mode: Game will end when {_numberOfWinners} or fewer players remain");
    }

    public override void RegisterUnit(Unit unit)
    {
        base.RegisterUnit(unit);

        // Track player units
        if (unit.teamID == _playerTeamId)
        {
            if (!_playerUnits.Contains(unit))
            {
                _playerUnits.Add(unit);
                Debug.Log($"Registered player unit: {unit.unitName}. Total players: {_playerUnits.Count}");
            }
        }
    }

    public override void UnregisterUnit(Unit unit)
    {
        base.UnregisterUnit(unit);

        // Check if this was a player unit
        if (_playerUnits.Contains(unit))
        {
            _playerUnits.Remove(unit);
            Debug.Log($"Player unit eliminated: {unit.unitName}. Remaining players: {_playerUnits.Count}");
            CheckGameOverCondition();
        }
    }

    protected override void CheckGameOverCondition()
    {
        // Get the count of alive player units
        int alivePlayerCount = _playerUnits.Count(unit => unit.GetAlive());

        // Game ends when alive players <= number of winners
        if (alivePlayerCount <= _numberOfWinners)
        {
            Debug.Log($"Survive mode game over! {alivePlayerCount} players remaining (target: {_numberOfWinners})");
            TriggerGameOver();
        }
    }

    // Public methods to get game state
    public int GetRemainingPlayerCount() => _playerUnits.Count(unit => unit.GetAlive());
    public int GetTotalPlayerCount() => _playerUnits.Count;
    public float GetSurvivalProgress() => _playerUnits.Count > 0 ?
        (float)GetRemainingPlayerCount() / _playerUnits.Count : 0f;
}