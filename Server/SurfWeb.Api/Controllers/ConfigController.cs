using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SurfWeb.Configurations.Common;
using SurfWeb.Services.IServices;
using SurfWeb.Data.Dtos;
using SurfWeb.Configurations;

namespace SurfWeb.Api.Controllers;

[ApiController]
[Route("api/v1/config")]
public sealed class ConfigController(
    IOptions<SurfWebOptions> options,
    IServerService serverQuery) : ControllerBase
{
    /// <summary>
    /// 获取地图封面图床配置。
    /// </summary>
    /// <returns>图床 BaseUrl 与扩展名。</returns>
    [HttpGet("map-images")]
    public ActionResult<ApiResponse<MapImageConfigDto>> MapImages()
    {
        var img = options.Value.MapImages;
        var baseUrl = string.IsNullOrWhiteSpace(img.BaseUrl) ? null : img.BaseUrl.Trim();
        return Ok(ApiResponse<MapImageConfigDto>.Ok(new MapImageConfigDto(baseUrl, img.Extension ?? "")));
    }

    /// <summary>
    /// 获取配置中的服务器列表（静态信息，不含实时状态）。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>服务器名称、地址、占位地图等。</returns>
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