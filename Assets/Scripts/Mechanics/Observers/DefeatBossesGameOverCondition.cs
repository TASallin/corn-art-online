// DefeatBossesGameOverCondition.cs
using System.Collections.Generic;
using UnityEngine;

public class DefeatBossesGameOverCondition : GameOverCondition
{
    [SerializeField] private int _targetTeamId = 2; // Team with bosses that need to be defeated

    private List<Unit> _bossUnits = new List<Unit>();
    private int _initialBossCount = 0;
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
        if (unit.teamID == _targetTeamId && unit.IsBoss())
        {
            if (!_bossUnits.Contains(unit))
            {
                _bossUnits.Add(unit);
                _initialBossCount++;
                Debug.Log($"Registered boss unit: {unit.unitName} on team {unit.teamID}. Total bosses: {_bossUnits.Count}");
            }
        }
    }

    public override void UnregisterUnit(Unit unit)
    {
        base.UnregisterUnit(unit);

        // Check if this was a boss unit
        if (_bossUnits.Contains(unit))
        {
            _bossUnits.Remove(unit);
            Debug.Log($"Boss unit destroyed: {unit.unitName}. Remaining bosses: {_bossUnits.Count}");
            CheckGameOverCondition();
        }
    }

    protected override void CheckGameOverCondition()
    {
        if (_initialBossCount == 0)
            return;

        if (_bossUnits.Count == 0)
        {
            // Notify camera controller about the final boss defeat
            // Find the last defeated boss if available
            var cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null) // 'unit' from UnregisterUnit
            {
                cameraController.ShowCriticalEvent(lastEvent.SourceUnit, lastEvent.TargetUnit, 5f); // Show for 5 seconds
            }

            Debug.Log("All bosses defeated! Victory!");
            TriggerGameOver();
        }
    }

    // Optional: Public methods to get boss status
    public int GetRemainingBossCount() => _bossUnits.Count;
    public int GetInitialBossCount() => _initialBossCount;
    public float GetBossDefeatProgress() => _initialBossCount > 0 ? 1f - ((float)_bossUnits.Count / _initialBossCount) : 0f;
}