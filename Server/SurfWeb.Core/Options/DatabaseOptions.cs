namespace SurfWeb.Core.Options;

/// <summary>
/// 成绩库 EF Core 提供程序；连接串配置键仍为 <c>ConnectionStrings:Shavit</c>。
/// </summary>
public sealed class DatabaseOptions
{
    public const string MySql = "MySql";
    public const string Sqlite = "Sqlite";

    /// <summary>默认生产/开发均使用 Shavit MySQL。</summary>
    public string Provider { get; init; } = MySql;

    public static bool IsSqlite(string? provider) =>
        string.Equals(provider, Sqlite, StringComparison.OrdinalIgnoreCase);
}
