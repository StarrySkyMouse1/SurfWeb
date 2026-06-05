namespace SurfWeb.Core.Enums;

public static class RealtimeRecentRecordScopeParser
{
    public static bool TryParse(string? value, out RealtimeRecentRecordScope? scope, out string? error)
    {
        scope = null;
        error = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var trimmed = value.Trim();
        if (int.TryParse(trimmed, out var numeric) && Enum.IsDefined(typeof(RealtimeRecentRecordScope), numeric))
        {
            scope = (RealtimeRecentRecordScope)numeric;
            return true;
        }

        if (Enum.TryParse<RealtimeRecentRecordScope>(trimmed, ignoreCase: true, out var named))
        {
            scope = named;
            return true;
        }

        scope = trimmed.ToLowerInvariant() switch
        {
            "all" => RealtimeRecentRecordScope.All,
            "main" => RealtimeRecentRecordScope.Main,
            "bonus" => RealtimeRecentRecordScope.Bonus,
            "stage" => RealtimeRecentRecordScope.Stage,
            _ => null,
        };
        if (scope is not null)
            return true;

        error = "type 须为 all / main / bonus / stage（或 0–3、All/Main/Bonus/Stage）。";
        return false;
    }
}
