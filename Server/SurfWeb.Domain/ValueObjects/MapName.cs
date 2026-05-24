namespace SurfWeb.Domain.ValueObjects;

public readonly record struct MapName
{
    public MapName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Map name is required.", nameof(value));
        }

        Value = value.Trim().ToLowerInvariant();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
