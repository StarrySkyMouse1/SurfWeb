namespace SurfWeb.Services.IServices;

public interface IUserService
{
    /// <summary>????????? Steam auth????? users.name??</summary>
    Task<IReadOnlyDictionary<string, int>> GetAuthsByNamesAsync(
        IReadOnlyList<string> playerNames,
        CancellationToken ct = default);
}
