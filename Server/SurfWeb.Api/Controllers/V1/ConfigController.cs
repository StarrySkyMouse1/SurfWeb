using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SurfWeb.Application.Abstractions;
using SurfWeb.Application.Common;
using SurfWeb.Application.Dtos;
using SurfWeb.Application.Options;

namespace SurfWeb.Api.Controllers.V1;

[ApiController]
[Route("api/v1/config")]
public sealed class ConfigController(
    IOptions<SurfWebOptions> options,
    IServerQueryService serverQuery) : ControllerBase
{
    [HttpGet("styles")]
    public ActionResult<ApiResponse<IReadOnlyList<StyleConfigDto>>> Styles()
    {
        var styles = options.Value.Styles
            .Select(s => new StyleConfigDto(s.Id, s.Name, s.Default))
            .ToList();
        return Ok(ApiResponse<IReadOnlyList<StyleConfigDto>>.Ok(styles));
    }

    [HttpGet("map-images")]
    public ActionResult<ApiResponse<MapImageConfigDto>> MapImages()
    {
        var img = options.Value.MapImages;
        var baseUrl = string.IsNullOrWhiteSpace(img.BaseUrl) ? null : img.BaseUrl.Trim();
        return Ok(ApiResponse<MapImageConfigDto>.Ok(new MapImageConfigDto(baseUrl, img.Extension ?? "")));
    }

    [HttpGet("servers")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ServerInfoDto>>>> Servers(CancellationToken ct)
    {
        var statuses = await serverQuery.GetStatusesAsync(ct);
        var list = statuses
            .Select(s => new ServerInfoDto(
                s.Name,
                s.Address,
                s.Map,
                s.Players,
                s.MaxPlayers > 0 ? s.MaxPlayers : null,
                s.Note))
            .ToList();
        return Ok(ApiResponse<IReadOnlyList<ServerInfoDto>>.Ok(list));
    }
}
