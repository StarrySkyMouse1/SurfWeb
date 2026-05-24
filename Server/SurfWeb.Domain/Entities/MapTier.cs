namespace SurfWeb.Domain.Entities;

public sealed class MapTier
{
    public string Map { get; set; } = "";
    public int Tier { get; set; } = 1;
    public float Maxvelocity { get; set; } = 3500;
}
