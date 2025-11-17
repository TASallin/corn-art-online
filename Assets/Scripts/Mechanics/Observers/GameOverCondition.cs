using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameOverCondition : MonoBehaviour, IObserver
{
    public event Action OnGameOver;
    protected List<Unit> _registeredUnits = new List<Unit>();
    protected bool _gameOverTriggered = false;

    public virtual EventType[] ObservedEventTypes => GetObservedEventTypes();

    protected virtual EventType[] GetObservedEventTypes()
    {
        return new[] { EventType.UnitCreated, EventType.UnitDestroyed };
    }

    protected virtual void OnEnable()
    {
        Observable.AddObserver(this, ObservedEventTypes);
    }

    protected virtual void OnDisable()
    {
        Observable.RemoveObserver(this);
    }

    public virtual void RegisterUnit(Unit unit)
    {
        if (!_registeredUnits.Contains(unit))
            _registeredUnits.Add(unit);
    }

    public virtual void UnregisterUnit(Unit unit)
    {
        _registeredUnits.Remove(unit);
        CheckGameOverCondition();
    }

    public virtual void OnEventReceived(EventData data)
    {
        switch (data.Type)
        {
            case EventType.UnitCreated:
                RegisterUnit(data.SourceUnit);
                break;

            case EventType.UnitDestroyed:
                UnregisterUnit(data.TargetUnit);
                break;
        }
    }

    protected abstract void CheckGameOverCondition();

    protected virtual void TriggerGameOver()
    {
        if (_gameOverTriggered)
            return;

        _gameOverTriggered = true;
        OnGameOver?.Invoke();
    }

    public List<Unit> GetRegisteredUnits()
    {
        return _registeredUnits;
    }
}