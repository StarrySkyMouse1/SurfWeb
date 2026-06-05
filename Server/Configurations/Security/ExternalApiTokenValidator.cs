using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SurfWeb.Core.Options;

namespace SurfWeb.Configurations.Security;

public sealed class ExternalApiTokenValidator(IOptions<SurfWebOptions> options) : IExternalApiTokenValidator
{
    public bool ValidateLatestRecordsToken(string? token)
    {
        var expected = options.Value.ExternalApi.LatestRecordsToken;
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(token))
            return false;

        return FixedTimeEquals(expected, token);
    }

    private static bool FixedTimeEquals(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        return expectedBytes.Length == providedBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
