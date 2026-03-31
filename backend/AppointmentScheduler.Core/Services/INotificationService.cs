using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Interfaccia per la gestione delle notifiche utente
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Recupera un riepilogo: ultime 5 notifiche e conteggio non lette
    /// </summary>
    Task<NotificationSummaryDto> GetSummaryAsync(int userId);

    /// <summary>
    /// Recupera tutte le notifiche dell'utente
    /// </summary>
    Task<List<NotificationDto>> GetAllAsync(int userId);

    /// <summary>
    /// Segna una singola notifica come letta
    /// </summary>
    Task MarkReadAsync(int notificationId, int userId);

    /// <summary>
    /// Segna tutte le notifiche dell'utente come lette
    /// </summary>
    Task MarkAllReadAsync(int userId);

    /// <summary>
    /// Crea una nuova notifica per l'utente specificato
    /// </summary>
    Task CreateAsync(int userId, string title, string message, NotificationType type, int? relatedEntityId = null);
}
