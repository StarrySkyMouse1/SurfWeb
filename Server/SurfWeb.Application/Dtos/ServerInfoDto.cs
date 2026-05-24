namespace SurfWeb.Application.Dtos;

public sealed record ServerInfoDto(
    string Name,
    string Address,
    string? Map,
    int? Players,
    int? MaxPlayers,
    string? Note);
