using Stopwatch = System.Diagnostics.Stopwatch;
using System.Security.Claims;
using System.Text.Json;
using AppointmentScheduler.Data;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace AppointmentScheduler.API.Middleware;

public sealed class ActivityMiddleware(RequestDelegate next, ILogger<ActivityMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext http, ActivityContext activity, IServiceScopeFactory scopes)
    {
        if (!http.Request.Path.StartsWithSegments("/api") || http.Request.Path.StartsWithSegments("/api/activity/collect"))
        {
            await next(http);
            return;
        }
        activity.UserId = int.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
        activity.MerchantId = int.TryParse(http.User.FindFirstValue("MerchantId"), out var merchantId) ? merchantId : null;
        var app = http.Request.Headers["X-Scheduler-App"].ToString();
        activity.App = app is "admin" or "merchant" or "employee" ? app : "api";
        activity.SessionId = Guid.TryParse(http.Request.Headers["X-Session-Id"], out var session) ? session : null;
        activity.OperationId = Guid.TryParse(http.Request.Headers["X-Operation-Id"], out var operation) ? operation : Guid.NewGuid();
        activity.RequestId = http.TraceIdentifier;
        var descriptor = http.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        activity.Action = descriptor == null ? "api.unmatched" : $"{descriptor.ControllerName}.{descriptor.ActionName}";
        var watch = Stopwatch.StartNew();
        Exception? failure = null;
        try { await next(http); }
        catch (Exception ex) { failure = ex; throw; }
        finally
        {
            var status = failure == null ? http.Response.StatusCode : 500;
            var item = activity.Create("request", activity.Action,
                failure is OperationCanceledException ? "cancelled" : status >= 500 ? "error" : status >= 400 ? "rejected" : "completed");
            item.StatusCode = status;
            item.DurationMs = watch.ElapsedMilliseconds;
            // Nessun body, query string, header o messaggio di eccezione: possono contenere credenziali.
            var references = http.Request.RouteValues.Where(pair => long.TryParse(pair.Value?.ToString(), out _))
                .ToDictionary(pair => pair.Key, pair => pair.Value?.ToString());
            var trigger = http.Request.Headers["X-Activity-Trigger"].ToString() == "interaction" ? "client-interaction" : "unspecified-or-background";
            item.Details = JsonSerializer.Serialize(new { method = http.Request.Method, trigger, references, errorType = failure?.GetType().Name });
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.ActivityEvents.Add(item);
                await db.SaveChangesAsync(timeout.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Registrazione attività fallita: evento {EventId}, richiesta {RequestId}", item.EventId, item.RequestId);
            }
        }
    }
}
