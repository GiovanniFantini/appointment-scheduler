using AppointmentScheduler.Data;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.API.Middleware;

public sealed class ActivityRetentionWorker(IServiceScopeFactory scopes, IConfiguration configuration,
    ILogger<ActivityRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var days = configuration.GetValue<int?>("Activity:TelemetryRetentionDays") ?? 30;
            if (days <= 0) continue;
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var threshold = DateTime.UtcNow.AddDays(-days);
                // Solo la telemetria: modifiche, richieste ed eventi esterni restano conservati.
                var ids = await db.ActivityEvents.Where(e => e.Category == "interaction" && e.ReceivedAt < threshold)
                    .OrderBy(e => e.Id).Select(e => e.Id).Take(10000).ToListAsync(stoppingToken);
                if (ids.Count == 0) continue;
                await using var transaction = await db.Database.BeginTransactionAsync(stoppingToken);
                var deleted = await db.ActivityEvents.Where(e => ids.Contains(e.Id)).ExecuteDeleteAsync(stoppingToken);
                var audit = db.Activity.Create("maintenance", "Activity.Retention", "committed");
                audit.Details = System.Text.Json.JsonSerializer.Serialize(new { deleted, days });
                db.ActivityEvents.Add(audit);
                await db.SaveChangesAsync(stoppingToken);
                await transaction.CommitAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Manutenzione registro attività fallita"); }
        }
    }
}
