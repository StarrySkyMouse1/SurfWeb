namespace SurfWeb.Repositories.Entities;

public sealed class User
{
    public int Auth { get; set; }
    public string? Name { get; set; }
    public int? Ip { get; set; }
    public int Firstlogin { get; set; }
    public int Lastlogin { get; set; }
    public float Points { get; set; }
    public float Playtime { get; set; }
}
