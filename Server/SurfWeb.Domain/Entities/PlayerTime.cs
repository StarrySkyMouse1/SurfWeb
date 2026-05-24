namespace SurfWeb.Domain.Entities;

public sealed class PlayerTime
{
    public int Id { get; set; }
    public byte Style { get; set; }
    public byte Track { get; set; }
    public float Time { get; set; }
    public int? Auth { get; set; }
    public string Map { get; set; } = "";
    public float Points { get; set; }
    public int? Jumps { get; set; }
    public int? Date { get; set; }
    public int? Strafes { get; set; }
    public float? Sync { get; set; }
    public float Perfs { get; set; }
    public short? Completions { get; set; }
    public float Startvel { get; set; }
    public float Endvel { get; set; }

    public User? User { get; set; }
}
