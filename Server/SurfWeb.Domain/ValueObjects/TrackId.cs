namespace SurfWeb.Domain.ValueObjects;

public readonly record struct TrackId
{
    public TrackId(byte value)
    {
        Value = value;
    }

    public byte Value { get; }

    public override string ToString() => Value.ToString();
}
