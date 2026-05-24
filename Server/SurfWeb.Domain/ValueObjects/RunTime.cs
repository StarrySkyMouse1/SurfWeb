namespace SurfWeb.Domain.ValueObjects;

public readonly record struct RunTime
{
    public RunTime(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Run time must be a positive finite number.");
        }

        Value = value;
    }

    public double Value { get; }

    public bool IsFasterThan(RunTime other) => Value < other.Value;

    public override string ToString() => Value.ToString("0.###");
}
