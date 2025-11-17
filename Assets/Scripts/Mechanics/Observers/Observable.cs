using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class Observable
{
    private static Dictionary<EventType, List<IObserver>> _observersByEventType = new Dictionary<EventType, List<IObserver>>();

    public static void AddObserver(IObserver observer, params EventType[] eventTypes)
    {
        // If no event types specified, register for all events
        if (eventTypes == null || eventTypes.Length == 0)
        {
            foreach (EventType eventType in Enum.GetValues(typeof(EventType)))
            {
                RegisterForEventType(observer, eventType);
            }
        } else
        {
            foreach (EventType eventType in eventTypes)
            {
                RegisterForEventType(observer, eventType);
            }
        }
    }

    private static void RegisterForEventType(IObserver observer, EventType eventType)
    {
        if (!_observersByEventType.ContainsKey(eventType))
        {
            _observersByEventType[eventType] = new List<IObserver>();
        }

        if (!_observersByEventType[eventType].Contains(observer))
        {
            _observersByEventType[eventType].Add(observer);
        }
    }

    public static void RemoveObserver(IObserver observer, params EventType[] eventTypes)
    {
        // If no event types specified, remove from all events
        if (eventTypes == null || eventTypes.Length == 0)
        {
            foreach (var observers in _observersByEventType.Values)
            {
                observers.Remove(observer);
            }
        } else
        {
            foreach (EventType eventType in eventTypes)
            {
                if (_observersByEventType.ContainsKey(eventType))
                {
                    _observersByEventType[eventType].Remove(observer);
                }
            }
        }
    }

    public static void NotifyObservers(EventData data)
    {
        if (_observersByEventType.ContainsKey(data.Type))
        {
            // Use a copy to prevent issues if collection is modified during iteration
            var observersToNotify = _observersByEventType[data.Type].ToArray();
            foreach (var observer in observersToNotify)
            {
                observer.OnEventReceived(data);
            }
        }
    }
}
