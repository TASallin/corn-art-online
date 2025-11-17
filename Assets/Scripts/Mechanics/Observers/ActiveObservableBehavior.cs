using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveObservableBehavior : UnitObservableBehavior
{
    [SerializeField] private bool _reportCreation = true;
    [SerializeField] private bool _reportDestruction = true;
    [SerializeField] private bool _reportDamage = true;
    [SerializeField] private bool _reportScore = true;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        if (_reportCreation)
            ReportUnitCreated();
    }

    public override void ReportUnitCreated()
    {
        Observable.NotifyObservers(new EventData(EventType.UnitCreated, _unit));
    }

    public override void ReportDamage(Unit source, float damage)
    {
        if (_reportDamage)
            Observable.NotifyObservers(new EventData(EventType.UnitDamaged, source, _unit, damage));
    }

    public override void ReportScore(float points)
    {
        if (_reportScore)
            Observable.NotifyObservers(new EventData(EventType.UnitScored, _unit, null, points));
    }

    private void OnDestroy()
    {
        //if (_reportDestruction)
            //ReportUnitDestroyed();
    }

    public override void ReportUnitDestroyed(Unit source)
    {
        if (_reportDestruction)
            Observable.NotifyObservers(new EventData(EventType.UnitDestroyed, source, _unit));
    }
}
