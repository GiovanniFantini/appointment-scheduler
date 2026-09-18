using AppointmentScheduler.Data;

namespace AppointmentScheduler.API.Middleware;

public sealed class ActivityRecorder(IServiceScopeFactory scopes, ActivityContext activity, ILogger<ActivityRecorder> logger)
{
    public async Task RecordAsync(string action, string outcome, Guid attempt)
    {
        try
        {
            using var scope = scopes.CreateScope();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var item = activity.Create("external", action, outcome);
            item.Details = System.Text.Json.JsonSerializer.Serialize(new { attempt });
            db.ActivityEvents.Add(item);
            await db.SaveChangesAsync(timeout.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit operazione esterna non disponibile: {Action}, {Attempt}", action, attempt);
        }
    }

    public async Task<T> RunAsync<T>(string action, Func<Task<T>> execute)
    {
        var attempt = Guid.NewGuid();
        await RecordAsync(action, "started", attempt);
        T result;
        try { result = await execute(); }
        catch { await RecordAsync(action, "unknown", attempt); throw; }
        // L'esito provider rimane distinto dalla consegna email o dalla lettura effettiva di un file.
        await RecordAsync(action, "completed", attempt);
        return result;
    }

    public async Task RunAsync(string action, Func<Task> execute)
        => await RunAsync(action, async () => { await execute(); return true; });
}
