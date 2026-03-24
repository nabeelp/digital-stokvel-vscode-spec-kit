using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace DigitalStokvel.API.Middleware;

/// <summary>
/// Rate limiting configuration for API endpoints (100 req/min per user)
/// </summary>
public static class RateLimitingConfiguration
{
    public const string UserPolicy = "UserRateLimit";
    public const string GlobalPolicy = "GlobalRateLimit";

    public static void ConfigureRateLimiting(RateLimiterOptions options)
    {
        // Per-user rate limiting: 100 requests per minute
        options.AddPolicy(UserPolicy, context =>
        {
            var userId = context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: userId,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5
                });
        });

        // Global rate limiting: 10,000 requests per minute across all users
        options.AddPolicy(GlobalPolicy, context =>
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: "global",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10000,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 100
                });
        });

        // Customize rejection response
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too Many Requests",
                    message = "You're making too many requests. Please slow down and try again.",
                    retryAfter = retryAfter.TotalSeconds
                }, cancellationToken);
            }
           else
            {
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too Many Requests",
                    message = "You're making too many requests. Please slow down and try again."
                }, cancellationToken);
            }
        };
    }
}
