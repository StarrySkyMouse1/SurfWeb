namespace SurfWeb.Configurations.Security;

public interface IExternalApiTokenValidator
{
    bool ValidateLatestRecordsToken(string? token);
}
