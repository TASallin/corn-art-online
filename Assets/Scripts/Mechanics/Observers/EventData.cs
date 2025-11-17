public class EventData
{
    public EventType Type { get; private set; }
    public Unit SourceUnit { get; private set; }
    public Unit TargetUnit { get; private set; }
    public float Value { get; private set; }

    public EventData(EventType type, Unit sourceUnit = null, Unit targetUnit = null, float value = 0f)
    {
        Type = type;
        SourceUnit = sourceUnit;
        TargetUnit = targetUnit;
        Value = value;
    }
}