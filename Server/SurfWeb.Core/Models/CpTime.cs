namespace SurfWeb.Core.Models;

public sealed class CpTime
{
    public byte Style { get; set; }
    public byte Track { get; set; }
    public string Map { get; set; } = "";
    public byte Checkpoint { get; set; }
    public int Auth { get; set; }
    public float Time { get; set; }
    public float StageTime { get; set; }
    public short Attempts { get; set; }
}
