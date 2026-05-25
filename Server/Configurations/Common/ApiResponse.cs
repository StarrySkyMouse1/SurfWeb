namespace SurfWeb.Configurations.Common;

public sealed class ApiResponse<T>
{
    public T? Data { get; init; }
    public ApiMeta? Meta { get; init; }
    public ApiError? Error { get; init; }

    public static ApiResponse<T> Ok(T data, ApiMeta? meta = null) => new() { Data = data, Meta = meta };

    public static ApiResponse<T> Fail(string code, string message) =>
        new() { Error = new ApiError(code, message) };
}

public sealed record ApiMeta(int Page, int PageSize, int Total);

public sealed record ApiError(string Code, string Message);