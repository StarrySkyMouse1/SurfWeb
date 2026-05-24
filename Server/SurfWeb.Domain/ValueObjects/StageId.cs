namespace SurfWeb.Domain.ValueObjects;

public readonly record struct StageId
{
    public StageId(byte value)
    {
        if (value == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Stage id must be greater than zero.");
        }

        Value = value;
    }

    public byte Value { get; }

    public override string ToString() => Value.ToString();
}
