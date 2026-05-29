namespace SurfWeb.Configurations.Common;

public sealed class ApiResponse<T>
{
    public T? Data { get; init; }
    public ApiMeta? Meta { get; init; }
    public ApiError? Error { get; init; }

    public static ApiResponse<T> Ok(T data, ApiMeta? meta = null) => new() { Data = data, Meta = meta };
    /// <summary>
    /// 返回失败响应；<paramref name="message"/> 为返回给前端的
    /// </summary>
    public static ApiResponse<T> Fail(ApiErrorCode code, string? message = null) =>
        new() { Error = new ApiError(code.GetCode(), message ?? code.GetMessage()) };
}

public sealed record ApiMeta(int Page, int PageSize, int Total);

public sealed record ApiError(string Code, string Message);
