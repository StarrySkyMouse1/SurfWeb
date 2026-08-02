namespace SurfWeb.Utils.Servers;

public static class ServerEndpointParser
{
    public static bool TryResolve(string address, string? host, int? port, out string resolvedHost, out int resolvedPort)
    {
        resolvedHost = "";
        resolvedPort = 0;

        if (!string.IsNullOrWhiteSpace(host) && port is > 0 and <= 65535)
        {
            // 容错：Host 误写成 "example.com:27015" 时拆掉端口，优先用配置的 Port
            if (!TrySplitHostPort(host.Trim(), out resolvedHost, out _) || string.IsNullOrWhiteSpace(resolvedHost))
                resolvedHost = host.Trim();
            resolvedPort = port.Value;
            return !string.IsNullOrWhiteSpace(resolvedHost);
        }

        return TrySplitHostPort(address, out resolvedHost, out resolvedPort);
    }

    private static bool TrySplitHostPort(string raw, out string resolvedHost, out int resolvedPort)
    {
        resolvedHost = "";
        resolvedPort = 0;

        var text = raw.Trim();
        if (text.StartsWith("connect ", StringComparison.OrdinalIgnoreCase))
            text = text["connect ".Length..].Trim();

        var colon = text.LastIndexOf(':');
        if (colon <= 0 || colon >= text.Length - 1)
            return false;

        var hostPart = text[..colon].Trim();
        if (string.IsNullOrWhiteSpace(hostPart))
            return false;

        if (!int.TryParse(text[(colon + 1)..].Trim(), out resolvedPort) || resolvedPort is <= 0 or > 65535)
            return false;

        resolvedHost = hostPart;
        return true;
    }
}