namespace SurfWeb.Domain.ValueObjects;

public readonly record struct PlayerId
{
    public PlayerId(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Player id must be positive.");
        }

        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString();
}
