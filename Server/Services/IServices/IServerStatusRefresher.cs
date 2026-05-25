namespace SurfWeb.Services.IServices;

public interface IServerStatusRefresher
{
    Task RefreshAsync(CancellationToken ct = default);
}
