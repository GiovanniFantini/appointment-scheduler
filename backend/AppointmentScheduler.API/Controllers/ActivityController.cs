using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.API.Controllers;

[ApiController]
[Route("api/activity")]
public sealed class ActivityController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List([FromQuery] long? before, [FromQuery] int? userId,
        [FromQuery] int? merchantId, [FromQuery] string? category, [FromQuery] string? outcome,
        [FromQuery] string? app, [FromQuery] string? action, [FromQuery] string? entityType,
        [FromQuery] string? entityId, [FromQuery] Guid? operationId, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] bool export, CancellationToken cancellationToken)
    {
        var query = db.ActivityEvents.AsNoTracking();
        if (!User.IsInRole("Admin"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Unauthorized();
            if (!int.TryParse(User.FindFirstValue("MerchantId"), out var tenant)) return Forbid();
            if (User.IsInRole("Merchant")) query = query.Where(e => e.MerchantId == tenant);
            else query = query.Where(e => e.MerchantId == tenant && e.UserId == actor && e.Category != "change");
        }
        if (before.HasValue) query = query.Where(e => e.Id < before.Value);
        if (userId.HasValue) query = query.Where(e => e.UserId == userId);
        if (merchantId.HasValue) query = query.Where(e => e.MerchantId == merchantId);
        if (category != null) query = query.Where(e => e.Category == category);
        if (outcome != null) query = query.Where(e => e.Outcome == outcome);
        if (app != null) query = query.Where(e => e.App == app);
        if (action != null) query = query.Where(e => e.Action == action);
        if (entityType != null) query = query.Where(e => e.EntityType == entityType);
        if (entityId != null) query = query.Where(e => e.EntityId == entityId);
        if (operationId.HasValue) query = query.Where(e => e.OperationId == operationId);
        if (from.HasValue) { var utc = AppointmentScheduler.Shared.Helpers.DateTimeUtc.Coerce(from.Value); query = query.Where(e => e.ReceivedAt >= utc); }
        if (to.HasValue) { var utc = AppointmentScheduler.Shared.Helpers.DateTimeUtc.Coerce(to.Value); query = query.Where(e => e.ReceivedAt <= utc); }
        var rows = await query.OrderByDescending(e => e.Id).Take(101).ToListAsync(cancellationToken);
        if (export)
        {
            db.Activity.Action = "Activity.Export";
            return File(JsonSerializer.SerializeToUtf8Bytes(rows.Take(100)), "application/json", "activity-page.json");
        }
        return Ok(new { items = rows.Take(100), nextCursor = rows.Count > 100 ? (long?)rows[99].Id : null });
    }

    [HttpPost("collect")]
    [AllowAnonymous]
    [EnableRateLimiting("activity")]
    [RequestSizeLimit(32768)]
    public async Task<IActionResult> Collect(ActivityBatch batch, CancellationToken cancellationToken)
    {
        if (batch.SessionId == Guid.Empty || batch.Events.Any(e => e.EventId == Guid.Empty || e.OperationId == Guid.Empty))
            return BadRequest(new { message = "Identificativi evento e sessione obbligatori." });
        var authenticated = User.Identity?.IsAuthenticated == true;
        var userId = authenticated && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var user) ? (int?)user : null;
        var merchantId = authenticated && int.TryParse(User.FindFirstValue("MerchantId"), out var merchant) ? (int?)merchant : null;
        var now = DateTime.UtcNow;
        var ids = batch.Events.Select(e => e.EventId).ToArray();
        var existing = await db.ActivityEvents.Where(e => ids.Contains(e.EventId)).Select(e => e.EventId).ToListAsync(cancellationToken);
        foreach (var item in batch.Events.DistinctBy(e => e.EventId).Where(e => !existing.Contains(e.EventId)))
        {
            db.ActivityEvents.Add(new ActivityEvent
            {
                EventId = item.EventId, UserId = userId, MerchantId = merchantId, App = batch.App,
                Source = "client", Category = "interaction", Action = item.Action, Outcome = "observed",
                OperationId = item.OperationId, SessionId = batch.SessionId,
                OccurredAt = now, ReceivedAt = now,
                // Anche i metadati ammessi sono dichiarazioni del client, mai prove di un'operazione riuscita.
                Details = JsonSerializer.Serialize(new { target = item.Target, page = item.Page, clientTime = item.ClientTime })
            });
        }
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            // Due ritrasmissioni concorrenti: il client riprova e recupera gli ID già persistiti.
            return Conflict();
        }
        return NoContent();
    }
}

public sealed class ActivityBatch
{
    [Required, RegularExpression("^(admin|merchant|employee)$")]
    public string App { get; set; } = "";
    public Guid SessionId { get; set; }
    [Required, MinLength(1), MaxLength(50)]
    public List<ClientActivity> Events { get; set; } = [];
}

public sealed class ClientActivity
{
    public Guid EventId { get; set; }
    public Guid OperationId { get; set; }
    [Required, RegularExpression("^(page.view|ui.click|form.start|form.change|form.submit|form.invalid|dialog.open|dialog.close|ui.error|session.end|context.change|telemetry.dropped)$")]
    public string Action { get; set; } = "";
    [Required, MaxLength(160), RegularExpression("^[a-zA-Z0-9_./:#-]+$")]
    public string Target { get; set; } = "";
    [Required, MaxLength(160), RegularExpression("^[a-zA-Z0-9_./:#-]+$")]
    public string Page { get; set; } = "";
    public long ClientTime { get; set; }
}
