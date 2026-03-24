namespace DigitalStokvel.API.DTOs;

/// <summary>
/// Base response DTO with common properties
/// </summary>
public record BaseResponse
{
    public bool Success { get; init; } = true;
    public string? Message { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Generic response wrapper for API responses
/// </summary>
/// <typeparam name="T">Data type</typeparam>
public record ApiResponse<T> : BaseResponse
{
    public T? Data { get; init; }
}

/// <summary>
/// Error response DTO
/// </summary>
public record ErrorResponse : BaseResponse
{
    public ErrorResponse()
    {
        Success = false;
    }

    public string? ErrorCode { get; init; }
    public Dictionary<string, string[]>? Errors { get; init; }
}

/// <summary>
/// Paginated response wrapper
/// </summary>
/// <typeparam name="T">Item type</typeparam>
public record PagedResponse<T> : BaseResponse
{
    public IEnumerable<T> Data { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
