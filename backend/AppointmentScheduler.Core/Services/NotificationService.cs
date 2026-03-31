using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio per la gestione delle notifiche utente
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Recupera le ultime 5 notifiche e il conteggio delle non lette
    /// </summary>
    public async Task<NotificationSummaryDto> GetSummaryAsync(int userId)
    {
        var unreadCount = await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        var recent = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .Select(n => MapToDto(n))
            .ToListAsync();

        return new NotificationSummaryDto
        {
            UnreadCount = unreadCount,
            Recent = recent
        };
    }

    /// <summary>
    /// Recupera tutte le notifiche dell'utente, dalla più recente
    /// </summary>
    public async Task<List<NotificationDto>> GetAllAsync(int userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => MapToDto(n))
            .ToListAsync();
    }

    /// <summary>
    /// Segna una singola notifica come letta, verificando che appartenga all'utente
    /// </summary>
    public async Task MarkReadAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return;

        notification.IsRead = true;
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Segna tutte le notifiche non lette dell'utente come lette
    /// </summary>
    public async Task MarkAllReadAsync(int userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unread)
            notification.IsRead = true;

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Crea una nuova notifica per l'utente specificato
    /// </summary>
    public async Task CreateAsync(int userId, string title, string message, NotificationType type, int? relatedEntityId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            RelatedEntityId = relatedEntityId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    private static NotificationDto MapToDto(Notification n)
    {
        return new NotificationDto
        {
            Id = n.Id,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            IsRead = n.IsRead,
            RelatedEntityId = n.RelatedEntityId,
            CreatedAt = n.CreatedAt
        };
    }
}
