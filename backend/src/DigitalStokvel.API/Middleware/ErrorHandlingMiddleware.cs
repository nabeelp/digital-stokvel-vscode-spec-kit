using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using DigitalStokvel.Core.Interfaces;

namespace DigitalStokvel.API.Middleware;

/// <summary>
/// Global exception handler that returns RFC 7807 ProblemDetails responses with localized messages
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;
    private readonly ILocalizationService _localizationService;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IHostEnvironment environment,
        ILocalizationService localizationService)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
        _localizationService = localizationService;
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

        var language = GetUserLanguage(context);
        var problemDetails = CreateProblemDetails(context, exception, language);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private string GetUserLanguage(HttpContext context)
    {
        // Try to get language from Accept-Language header
        var acceptLanguage = context.Request.Headers["Accept-Language"].FirstOrDefault();
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            // Extract primary language code (e.g., "en-US" -> "en", "zu-ZA" -> "zu")
            var languageCode = acceptLanguage.Split(',').FirstOrDefault()?.Split('-', ';').FirstOrDefault()?.Trim().ToLower();
            if (!string.IsNullOrEmpty(languageCode) && _localizationService.IsLanguageSupported(languageCode))
            {
                return languageCode;
            }
        }
        
        // Default to English
        return "en";
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception, string language)
    {
        var (status, titleKey, detailKey) = exception switch
        {
            ArgumentException => (HttpStatusCode.BadRequest, "error.title.badrequest", "error.badrequest"),
            InvalidOperationException => (HttpStatusCode.Conflict, "error.title.conflict", "error.conflict"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "error.title.unauthorized", "error.unauthorized"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "error.title.notfound", "error.notfound"),
            DbUpdateException => (HttpStatusCode.InternalServerError, "error.title.server", "error.database"),
            TimeoutException => (HttpStatusCode.RequestTimeout, "error.title.server", "error.timeout"),
            HttpRequestException => (HttpStatusCode.BadGateway, "error.title.server", "error.network"),
            _ => (HttpStatusCode.InternalServerError, "error.title.server", "error.general")
        };

        var title = _localizationService.GetString(titleKey, language);
        var detail = _localizationService.GetString(detailKey, language);

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
