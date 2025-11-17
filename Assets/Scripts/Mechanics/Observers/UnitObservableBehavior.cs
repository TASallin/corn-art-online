using UnityEngine;

public abstract class UnitObservableBehavior : MonoBehaviour
{
    protected Unit _unit;

    protected virtual void Awake()
    {
        _unit = GetComponent<Unit>();
    }

    // Common event reporting methods with empty implementations by default
    public virtual void ReportUnitCreated() { }
    public virtual void ReportDamage(Unit source, float damage) { }
    public virtual void ReportScore(float points) { }
    public virtual void ReportUnitDestroyed(Unit source) { }
}