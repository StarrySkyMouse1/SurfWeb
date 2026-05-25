namespace SurfWeb.Utils.Common;

public static class TimeFormatter
{
    public static string Format(float seconds)
    {
        if (seconds < 0) seconds = 0;
        var totalMs = (int)Math.Round(seconds * 1000, MidpointRounding.AwayFromZero);
        var ms = totalMs % 1000;
        var totalSec = totalMs / 1000;
        var sec = totalSec % 60;
        var min = totalSec / 60;
        return $"{min:D2}:{sec:D2}.{ms:D3}";
    }

    public static DateTimeOffset? FromUnixSeconds(int? unix)
    {
        if (unix is null or <= 0) return null;
        return DateTimeOffset.FromUnixTimeSeconds(unix.Value);
    }

    /// <summary>
    /// 将在线时长（秒）格式化为 <c>h:mm:ss</c>。
    /// </summary>
    /// <param name="totalSeconds">时长（秒）。</param>
    /// <returns>格式化后的字符串。</returns>
    public static string FormatDuration(double totalSeconds)
    {
        if (totalSeconds < 0) totalSeconds = 0;
        var ts = TimeSpan.FromSeconds(totalSeconds);
        var hours = (int)ts.TotalHours;
        return $"{hours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}