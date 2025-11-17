// DefeatBossesGameOverCondition.cs
using System.Collections.Generic;
using UnityEngine;

public class RouteGameOverCondition : GameOverCondition
{
    [SerializeField] private int _targetTeamId = 2; // Team with enemies that need to be defeated

    private List<Unit> _enemyUnits = new List<Unit>();
    private int _initialEnemyCount = 0;
    private EventData lastEvent;

    // Only observe unit created and destroyed events
    protected override EventType[] GetObservedEventTypes()
    {
        return new[] { EventType.UnitCreated, EventType.UnitDestroyed };
    }

    public override void OnEventReceived(EventData data)
    {
        switch (data.Type)
        {
            case EventType.UnitCreated:
                RegisterUnit(data.SourceUnit);
                break;

            case EventType.UnitDestroyed:
                lastEvent = data;
                UnregisterUnit(data.TargetUnit);
                break;
        }
    }

    public override void RegisterUnit(Unit unit)
    {
        base.RegisterUnit(unit);

        // Check if this unit is a boss on the target team
        if (unit.teamID == _targetTeamId)
        {
            if (!_enemyUnits.Contains(unit))
            {
                _enemyUnits.Add(unit);
                _initialEnemyCount++;
                //Debug.Log($"Registered boss unit: {unit.unitName} on team {unit.teamID}. Total bosses: {_bossUnits.Count}");
            }
        }
    }

    public override void UnregisterUnit(Unit unit)
    {
        base.UnregisterUnit(unit);

        // Check if this was a boss unit
        if (_enemyUnits.Contains(unit))
        {
            _enemyUnits.Remove(unit);
            //Debug.Log($"Boss unit destroyed: {unit.unitName}. Remaining bosses: {_bossUnits.Count}");
            CheckGameOverCondition();
        }
    }

    protected override void CheckGameOverCondition()
    {
        if (_initialEnemyCount == 0)
            return;

        if (_enemyUnits.Count == 0)
        {
            // Notify camera controller about the final boss defeat
            // Find the last defeated boss if available
            var cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null) // 'unit' from UnregisterUnit
            {
                cameraController.ShowCriticalEvent(lastEvent.SourceUnit, lastEvent.TargetUnit, 5f); // Show for 5 seconds
            }

            Debug.Log("All enemies defeated! Victory!");
            TriggerGameOver();
        }
    }

    // Optional: Public methods to get boss status
    public int GetRemainingEnemyCount() => _enemyUnits.Count;
    public int GetInitialEnemyCount() => _initialEnemyCount;
    public float GetEnemyDefeatProgress() => _initialEnemyCount > 0 ? 1f - ((float)_enemyUnits.Count / _initialEnemyCount) : 0f;
}