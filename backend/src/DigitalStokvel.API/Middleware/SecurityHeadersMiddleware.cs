using Microsoft.AspNetCore.Http;

namespace DigitalStokvel.API.Middleware;

/// <summary>
/// Middleware to add security headers to all API responses
/// Implements OWASP best practices for API security
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Content Security Policy: prevent XSS attacks
        // Restrict resource loading to trusted sources only
        context.Response.Headers.Append(
            "Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self' https://api.azure.com; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'");

        // Strict-Transport-Security (HSTS): enforce HTTPS for 1 year
        // includeSubDomains: apply to all subdomains
        // preload: allow browser preload list inclusion
        context.Response.Headers.Append(
            "Strict-Transport-Security",
            "max-age=31536000; includeSubDomains; preload");

        // X-Content-Type-Options: prevent MIME type sniffing
        // Forces browser to respect declared content type
        context.Response.Headers.Append(
            "X-Content-Type-Options",
            "nosniff");

        // X-Frame-Options: prevent clickjacking attacks
        // DENY: page cannot be displayed in frame/iframe
        context.Response.Headers.Append(
            "X-Frame-Options",
            "DENY");

        // X-XSS-Protection: enable browser XSS filter (legacy browsers)
        // Modern browsers use CSP, but this adds defense-in-depth
        context.Response.Headers.Append(
            "X-XSS-Protection",
            "1; mode=block");

        // Referrer-Policy: control referrer information leakage
        // no-referrer-when-downgrade: send referrer on HTTPS→HTTPS only
        context.Response.Headers.Append(
            "Referrer-Policy",
            "no-referrer-when-downgrade");

        // Permissions-Policy: disable unnecessary browser features
        // Prevents unauthorized access to camera, microphone, geolocation, etc.
        context.Response.Headers.Append(
            "Permissions-Policy",
            "camera=(), microphone=(), geolocation=(), payment=()");

        // X-Permitted-Cross-Domain-Policies: prevent Adobe Flash/PDF attacks
        context.Response.Headers.Append(
            "X-Permitted-Cross-Domain-Policies",
            "none");

        // Cache-Control: prevent caching of sensitive API responses
        // For endpoints returning sensitive data
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.Headers.Append(
                "Cache-Control",
                "no-store, no-cache, must-revalidate, proxy-revalidate");
            context.Response.Headers.Append(
                "Pragma",
                "no-cache");
            context.Response.Headers.Append(
                "Expires",
                "0");
        }

        // Remove server identity headers (security through obscurity)
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");
        context.Response.Headers.Remove("X-AspNetMvc-Version");

        await _next(context);
    }
}

/// <summary>
/// Extension method to add SecurityHeadersMiddleware to the pipeline
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
