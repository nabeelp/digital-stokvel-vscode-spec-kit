using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace DigitalStokvel.API.Middleware;

/// <summary>
/// Global exception handler that returns RFC 7807 ProblemDetails responses
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        var problemDetails = CreateProblemDetails(context, exception);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var (status, title, detail) = exception switch
        {
            ArgumentException => (HttpStatusCode.BadRequest, "Invalid Request", exception.Message),
            InvalidOperationException => (HttpStatusCode.Conflict, "Operation Failed", exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized", "You don't have permission to perform this action"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource Not Found", "The requested resource was not found"),
            _ => (HttpStatusCode.InternalServerError, "An Error Occurred", GetUserFriendlyMessage(exception))
        };

        var problemDetails = new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{(int)status}"
        };

        // Include exception details only in development
        if (_environment.IsDevelopment() && exception != null)
        {
            problemDetails.Extensions["exception"] = new
            {
                type = exception.GetType().Name,
                message = exception.Message,
                stackTrace = exception.StackTrace
            };
        }

        // Add trace identifier for diagnostics
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        return problemDetails;
    }

    private string GetUserFriendlyMessage(Exception exception)
    {
        // Return encouraging error messages per FR-051
        return exception switch
        {
            DbUpdateException => "We couldn't save your changes this time—let's try again",
            TimeoutException => "The request took too long—please try again",
            HttpRequestException => "We're having trouble connecting—please check your network",
            _ => "Something went wrong. Please try again."
        };
    }
}

/// <summary>
/// Extension method to register error handling middleware
/// </summary>
public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
