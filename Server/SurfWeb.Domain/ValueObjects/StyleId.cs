namespace SurfWeb.Domain.ValueObjects;

public readonly record struct StyleId
{
    public StyleId(byte value)
    {
        if (value == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Style id must be greater than zero.");
        }

        Value = value;
    }

    public byte Value { get; }

    public override string ToString() => Value.ToString();
}
