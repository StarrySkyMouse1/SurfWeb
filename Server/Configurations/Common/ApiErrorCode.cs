using System.Collections.Frozen;
using System.Reflection;
using System.Runtime.Serialization;

namespace SurfWeb.Configurations.Common;

/// <summary>
/// 标注 API 错误码对
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ApiErrorDescriptionAttribute(string message) : Attribute
{
    public string Message { get; } = message;
}

/// <summary>
/// 统一 API 错误码；序列化名为 snake_case（<see cref="EnumMemberAttribute"/>）。
/// </summary>
public enum ApiErrorCode
{
    [EnumMember(Value = "unknown")]
    [ApiErrorDescription("操作失败。")]
    Unknown,

    [EnumMember(Value = "not_found")]
    [ApiErrorDescription("未找到请求的资源。")]
    NotFound,

    [EnumMember(Value = "bad_request")]
    [ApiErrorDescription("请求参数无效。")]
    BadRequest,

    [EnumMember(Value = "server_error")]
    [ApiErrorDescription("服务器发生错误，请稍后重试。")]
    ServerError,

    [EnumMember(Value = "database_unavailable")]
    [ApiErrorDescription("数据库暂不可用，请稍后重试。")]
    DatabaseUnavailable,
}

public static class ApiErrorCodeExtensions
{
    private static readonly FrozenDictionary<ApiErrorCode, (string Code, string Message)> Metadata =
        BuildMetadata();

    private static readonly FrozenDictionary<string, ApiErrorCode> CodeLookup =
        Metadata.ToFrozenDictionary(x => x.Value.Code, x => x.Key, StringComparer.OrdinalIgnoreCase);

    public static string GetCode(this ApiErrorCode code) => Metadata[code].Code;

    public static string GetMessage(this ApiErrorCode code) => Metadata[code].Message;

    public static bool TryFromCode(string? code, out ApiErrorCode errorCode)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            errorCode = ApiErrorCode.Unknown;
            return false;
        }

        return CodeLookup.TryGetValue(code, out errorCode);
    }

    public static string MessageForCode(string? code) =>
        TryFromCode(code, out var errorCode) ? errorCode.GetMessage() : ApiErrorCode.Unknown.GetMessage();

    private static FrozenDictionary<ApiErrorCode, (string Code, string Message)> BuildMetadata()
    {
        var map = new Dictionary<ApiErrorCode, (string, string)>();
        foreach (var field in typeof(ApiErrorCode).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not ApiErrorCode value)
                continue;

            var wireCode = field.GetCustomAttribute<EnumMemberAttribute>()?.Value
                           ?? ToSnakeCase(field.Name);
            var message = field.GetCustomAttribute<ApiErrorDescriptionAttribute>()?.Message
                          ?? "操作失败。";
            map[value] = (wireCode, message);
        }

        return map.ToFrozenDictionary();
    }

    private static string ToSnakeCase(string name) =>
        string.Concat(name.Select((ch, i) =>
            i > 0 && char.IsUpper(ch) ? "_" + char.ToLowerInvariant(ch) : char.ToLowerInvariant(ch).ToString()));
}
