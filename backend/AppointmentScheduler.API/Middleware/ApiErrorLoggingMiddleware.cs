using System.Security.Claims;

namespace AppointmentScheduler.API.Middleware;

public sealed class ApiErrorLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiErrorLoggingMiddleware> _logger;

    public ApiErrorLoggingMiddleware(RequestDelegate next, ILogger<ApiErrorLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled API exception. Method {Method}, Path {Path}, TraceId {TraceId}, UserId {UserId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier,
                GetUserId(context));

            throw;
        }

        var statusCode = context.Response.StatusCode;
        if (statusCode < StatusCodes.Status400BadRequest)
            return;

        var message = "API request completed with error status {StatusCode}. Method {Method}, Path {Path}, TraceId {TraceId}, UserId {UserId}";

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                message,
                statusCode,
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier,
                GetUserId(context));
            return;
        }

        _logger.LogWarning(
            message,
            statusCode,
            context.Request.Method,
            context.Request.Path,
            context.TraceIdentifier,
            GetUserId(context));
    }

    private static string? GetUserId(HttpContext context)
    {
        return context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User?.FindFirstValue("sub");
    }
}
