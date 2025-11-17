using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IObserver
{
    void OnEventReceived(EventData data);
    EventType[] ObservedEventTypes { get; } // Optional: let observers declare which events they care about
}

// Example implementation
public class GameOverObserver : MonoBehaviour, IObserver
{
    [SerializeField] private int _requiredDestroyedUnits = 10;
    private int _destroyedUnits = 0;

    public EventType[] ObservedEventTypes => new[] { EventType.UnitDestroyed, EventType.GameOver };

    private void OnEnable()
    {
        Observable.AddObserver(this, ObservedEventTypes);
    }

    private void OnDisable()
    {
        Observable.RemoveObserver(this);
    }

    public void OnEventReceived(EventData data)
    {
        if (data.Type == EventType.UnitDestroyed)
        {
            _destroyedUnits++;

            if (_destroyedUnits >= _requiredDestroyedUnits)
            {
                Observable.NotifyObservers(new EventData(EventType.GameOver));
            }
        }
    }
}