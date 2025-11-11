namespace Arista_ZebraTablet.Shared.Application.Common;

/// <summary>
/// Represents a standard response structure for service operations that do not return data.
/// </summary>
/// <remarks>
/// Use this class to indicate success or failure of an operation, along with optional messages,
/// error codes, and validation or business rule errors.
/// </remarks>
public partial class ServiceResponse
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// A human-readable message describing the result of the operation.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// An optional error code that can be used for diagnostics or localization.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// A list of detailed error messages, typically used for validation failures.
    /// </summary>
    public IReadOnlyList<string>? Errors { get; init; }

    /// <summary>
    /// Creates a successful response with an optional message.
    /// </summary>
    /// <param name="message">An optional success message.</param>
    /// <returns>A <see cref="ServiceResponse"/> indicating success.</returns>
    public static ServiceResponse Ok(string? message = null)
        => new() { Success = true, Message = message };

    /// <summary>
    /// Creates a failed response with a message, optional error code, and optional error list.
    /// </summary>
    /// <param name="message">A message describing the failure.</param>
    /// <param name="errorCode">An optional error code for diagnostics.</param>
    /// <param name="errors">An optional list of detailed error messages.</param>
    /// <returns>A <see cref="ServiceResponse"/> indicating failure.</returns>
    public static ServiceResponse Fail(string message, string? errorCode = null, IEnumerable<string>? errors = null)
        => new() { Success = false, Message = message, ErrorCode = errorCode, Errors = errors?.ToArray() };
}

/// <summary>
/// Represents a standard response structure for service operations that return data.
/// </summary>
/// <typeparam name="T">The type of data returned by the operation.</typeparam>
/// <remarks>
/// Use this class to wrap both the result data and metadata about the operation's success or failure.
/// </remarks>
public partial class ServiceResponse<T>
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// A human-readable message describing the result of the operation.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// An optional error code that can be used for diagnostics or localization.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// A list of detailed error messages, typically used for validation failures.
    /// </summary>
    public IReadOnlyList<string>? Errors { get; init; }

    /// <summary>
    /// The data returned by the operation, if successful.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Creates a successful response containing the specified data and optional message.
    /// </summary>
    /// <param name="data">The data returned by the operation.</param>
    /// <param name="message">An optional success message.</param>
    /// <returns>A <see cref="ServiceResponse{T}"/> indicating success and containing the data.</returns>
    public static ServiceResponse<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    /// <summary>
    /// Creates a failed response with a message, optional error code, and optional error list.
    /// </summary>
    /// <param name="message">A message describing the failure.</param>
    /// <param name="errorCode">An optional error code for diagnostics.</param>
    /// <param name="errors">An optional list of detailed error messages.</param>
    /// <returns>A <see cref="ServiceResponse{T}"/> indicating failure.</returns>
    public static ServiceResponse<T> Fail(string message, string? errorCode = null, IEnumerable<string>? errors = null)
        => new() { Success = false, Message = message, ErrorCode = errorCode, Errors = errors?.ToArray() };
}