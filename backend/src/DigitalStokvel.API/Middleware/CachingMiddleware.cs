using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace DigitalStokvel.API.Middleware;

/// <summary>
/// Response caching middleware using Redis for group details and ledger queries
/// Implements intelligent cache invalidation on write operations
/// </summary>
public class CachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingMiddleware> _logger;

    // Cache duration constants
    private static readonly TimeSpan GroupDetailsCacheDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LedgerCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ConstitutionCacheDuration = TimeSpan.FromMinutes(15);

    // Cacheable endpoints with their durations
    private static readonly Dictionary<string, TimeSpan> CacheableEndpoints = new()
    {
        { "GET:/api/v1/groups/", GroupDetailsCacheDuration },
        { "GET:/api/v1/groups/{id}/ledger", LedgerCacheDuration },
        { "GET:/api/v1/groups/{id}/constitution", ConstitutionCacheDuration },
        { "GET:/api/v1/groups/{id}/wallet", GroupDetailsCacheDuration },
        { "GET:/api/v1/groups/{id}/interest-details", GroupDetailsCacheDuration },
        { "GET:/api/v1/groups/{id}/payouts", LedgerCacheDuration },
        { "GET:/api/v1/groups/{id}/votes", GroupDetailsCacheDuration },
        { "GET:/api/v1/groups/{id}/disputes", GroupDetailsCacheDuration }
    };

    // Write operations that invalidate caches
    private static readonly string[] WriteOperations = { "POST", "PUT", "PATCH", "DELETE" };

    public CachingMiddleware(
        RequestDelegate next,
        IDistributedCache cache,
        ILogger<CachingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only cache GET requests
        if (context.Request.Method != HttpMethods.Get)
        {
            // Invalidate cache for write operations
            if (WriteOperations.Contains(context.Request.Method))
            {
                await InvalidateCacheForPathAsync(context.Request.Path);
            }

            await _next(context);
            return;
        }

        // Check if this endpoint should be cached
        var (isCacheable, cacheDuration) = IsCacheableRequest(context.Request);
        if (!isCacheable)
        {
            await _next(context);
            return;
        }

        // Generate cache key from request path + query string
        var cacheKey = GenerateCacheKey(context.Request);

        // Try to get from cache
        var cachedResponse = await _cache.GetStringAsync(cacheKey);
        if (cachedResponse != null)
        {
            _logger.LogDebug("Cache HIT for key: {CacheKey}", cacheKey);
            
            context.Response.Headers.Append("X-Cache", "HIT");
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(cachedResponse);
            return;
        }

        _logger.LogDebug("Cache MISS for key: {CacheKey}", cacheKey);
        context.Response.Headers.Append("X-Cache", "MISS");

        // Capture the response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            // Only cache successful responses (200 OK)
            if (context.Response.StatusCode == StatusCodes.Status200OK)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(responseBody).ReadToEndAsync();

                // Cache the response
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = cacheDuration
                };
                await _cache.SetStringAsync(cacheKey, responseText, cacheOptions);

                _logger.LogDebug("Cached response for key: {CacheKey} (expires in {Duration})", 
                    cacheKey, cacheDuration);

                // Write response back to original stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            else
            {
                // Non-200 responses are not cached
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    /// <summary>
    /// Determines if the current request should be cached
    /// </summary>
    private (bool IsCacheable, TimeSpan CacheDuration) IsCacheableRequest(HttpRequest request)
    {
        var path = request.Path.Value ?? "";

        foreach (var endpoint in CacheableEndpoints)
        {
            var pattern = endpoint.Key.Split(':')[1];
            
            // Simple pattern matching (exact match or starts with for parameterized routes)
            if (path.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
                (pattern.Contains("{id}") && path.StartsWith(pattern.Split('{')[0], StringComparison.OrdinalIgnoreCase)))
            {
                return (true, endpoint.Value);
            }
        }

        return (false, TimeSpan.Zero);
    }

    /// <summary>
    /// Generates a unique cache key based on request path, query string, and user identity
    /// </summary>
    private string GenerateCacheKey(HttpRequest request)
    {
        var keyBuilder = new StringBuilder("cache:");
        
        // Add path
        keyBuilder.Append(request.Path.Value?.ToLowerInvariant() ?? "");

        // Add query string if present
        if (request.QueryString.HasValue)
        {
            keyBuilder.Append(request.QueryString.Value);
        }

        // Add user identity for user-specific caching
        if (request.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            var userId = request.HttpContext.User.Identity.Name ?? "";
            keyBuilder.Append($":user:{userId}");
        }

        return keyBuilder.ToString();
    }

    /// <summary>
    /// Invalidates all cached entries related to a specific path
    /// For example, POST /api/v1/contributions invalidates all ledger caches
    /// </summary>
    private async Task InvalidateCacheForPathAsync(PathString path)
    {
        // Extract group ID from path if present
        // Path format: /api/v1/groups/{groupId}/...
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        
        if (segments.Length >= 4 && segments[2] == "groups" && Guid.TryParse(segments[3], out var groupId))
        {
            // Invalidate all caches for this group
            var keysToInvalidate = new[]
            {
                $"cache:/api/v1/groups/{groupId}",
                $"cache:/api/v1/groups/{groupId}/ledger",
                $"cache:/api/v1/groups/{groupId}/constitution",
                $"cache:/api/v1/groups/{groupId}/wallet",
                $"cache:/api/v1/groups/{groupId}/interest-details",
                $"cache:/api/v1/groups/{groupId}/payouts",
                $"cache:/api/v1/groups/{groupId}/votes",
                $"cache:/api/v1/groups/{groupId}/disputes"
            };

            foreach (var key in keysToInvalidate)
            {
                try
                {
                    await _cache.RemoveAsync(key);
                    _logger.LogDebug("Invalidated cache key: {CacheKey}", key);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate cache key: {CacheKey}", key);
                }
            }

            _logger.LogInformation("Cache invalidation completed for group: {GroupId}", groupId);
        }
    }
}

/// <summary>
/// Extension method for registering caching middleware
/// </summary>
public static class CachingMiddlewareExtensions
{
    public static IApplicationBuilder UseCaching(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CachingMiddleware>();
    }
}
