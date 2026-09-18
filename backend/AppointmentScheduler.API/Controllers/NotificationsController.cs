using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione delle notifiche utente
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out userId);
    }

    /// <summary>
    /// Recupera tutte le notifiche dell'utente corrente
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetAll()
    {
        if (!TryGetUserId(out int userId))
            return BadRequest(new { message = "User ID non trovato nel token" });

        var notifications = await _notificationService.GetAllAsync(userId);
        return Ok(VisibleNotifications(notifications));
    }

    /// <summary>
    /// Recupera il riepilogo: ultime 5 notifiche e conteggio non lette
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<NotificationSummaryDto>> GetSummary()
    {
        if (!TryGetUserId(out int userId))
            return BadRequest(new { message = "User ID non trovato nel token" });

        if (HttpContext.Items[typeof(SubscriptionAccessDto)] is SubscriptionAccessDto)
        {
            var visible = VisibleNotifications(await _notificationService.GetAllAsync(userId));
            return Ok(new NotificationSummaryDto { UnreadCount = visible.Count(n => !n.IsRead), Recent = visible.Take(5).ToList() });
        }
        var summary = await _notificationService.GetSummaryAsync(userId);
        return Ok(summary);
    }

    /// <summary>
    /// Segna una notifica come letta
    /// </summary>
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        if (!TryGetUserId(out int userId))
            return BadRequest(new { message = "User ID non trovato nel token" });

        await _notificationService.MarkReadAsync(id, userId);
        return Ok(new { message = "Notifica segnata come letta" });
    }

    /// <summary>
    /// Segna tutte le notifiche dell'utente corrente come lette
    /// </summary>
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        if (!TryGetUserId(out int userId))
            return BadRequest(new { message = "User ID non trovato nel token" });

        await _notificationService.MarkAllReadAsync(userId);
        return Ok(new { message = "Tutte le notifiche segnate come lette" });
    }

    private List<NotificationDto> VisibleNotifications(List<NotificationDto> notifications)
    {
        if (HttpContext.Items[typeof(SubscriptionAccessDto)] is not SubscriptionAccessDto state) return notifications;
        return notifications.Where(n =>
        {
            var feature = n.Type switch
            {
                NotificationType.EventCreated or NotificationType.EventUpdated or NotificationType.EventDeleted => "Calendario",
                NotificationType.DocumentPublished => "Documenti",
                // Le notifiche storiche dei giustificativi usano lo stesso enum delle richieste, con titolo generato dal server.
                NotificationType.RequestSubmitted or NotificationType.RequestApproved or NotificationType.RequestRejected
                    => n.Title.StartsWith("Giustificativo timbratura", StringComparison.Ordinal) ? "Timbratura" : "Richieste",
                _ => null
            };
            return feature == null || state.ActiveFeatures.Contains(feature);
        }).ToList();
    }
}
