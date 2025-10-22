namespace Arista_ZebraTablet.Shared.Application.Common;

public partial class ServiceResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? ErrorCode { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static ServiceResponse Ok(string? message = null)
        => new() { Success = true, Message = message };

    public static ServiceResponse Fail(string message, string? errorCode = null, IEnumerable<string>? errors = null)
        => new() { Success = false, Message = message, ErrorCode = errorCode, Errors = errors?.ToArray() };
}

public partial class ServiceResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? ErrorCode { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }
    public T? Data { get; init; }

    public static ServiceResponse<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static ServiceResponse<T> Fail(string message, string? errorCode = null, IEnumerable<string>? errors = null)
        => new() { Success = false, Message = message, ErrorCode = errorCode, Errors = errors?.ToArray() };
}