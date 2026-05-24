using SurfWeb.Application.Servers;
using Xunit;

namespace SurfWeb.Application.Tests.Servers;

public sealed class SteamMapNameNormalizerTests
{
    [Theory]
    [InlineData("surf_kitsune", "surf_kitsune")]
    [InlineData("surf_kitsune.bsp", "surf_kitsune")]
    [InlineData("maps/surf_kitsune.bsp", "surf_kitsune")]
    public void Normalize_strips_bsp_and_path(string input, string expected)
    {
        Assert.Equal(expected, SteamMapNameNormalizer.Normalize(input));
    }
}
