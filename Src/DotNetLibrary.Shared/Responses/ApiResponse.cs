using Microsoft.AspNetCore.Http;

namespace DotNetLibrary.Shared.Responses;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public IEnumerable<string>? Errors { get; init; }
    public int? StatusCode { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null, int? statusCode = null) => new()
    {
        Success = true,
        Data = data,
        Message = message,
        StatusCode = statusCode
    };

    public static ApiResponse<T> Fail(IEnumerable<string> errors, string? message = null, int? statusCode = null) => new()
    {
        Success = false,
        Errors = errors.ToArray(),
        Message = message,
        StatusCode = statusCode
    };

    public static ApiResponse<T> Fail(string error, string? message = null, int? statusCode = null) => Fail([error], message, statusCode);
}

public class PagedResponse<T> : ApiResponse<IEnumerable<T>>
{
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public static PagedResponse<T> Ok(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount, string? message = null) => new()
    {
        Success = true,
        Data = items,
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalCount = totalCount,
        Message = message,
        StatusCode = StatusCodes.Status200OK
    };

    public static PagedResponse<T> Fail(IEnumerable<string> errors, string? message = null, int? statusCode = null) => new()
    {
        Success = false,
        Errors = errors.ToArray(),
        Message = message,
        StatusCode = statusCode
    };
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}
