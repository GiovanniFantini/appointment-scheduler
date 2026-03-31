using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Interfaccia per la gestione degli eventi aziendali
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Recupera tutti gli eventi di un merchant, con filtri opzionali
    /// </summary>
    Task<List<EventDto>> GetMerchantEventsAsync(int merchantId, DateOnly? from, DateOnly? to, EventType? type);

    /// <summary>
    /// Recupera gli eventi di un dipendente in un merchant specifico
    /// </summary>
    Task<List<EventDto>> GetEmployeeEventsAsync(int employeeId, int merchantId, DateOnly? from, DateOnly? to);

    /// <summary>
    /// Recupera un evento per ID, verificando l'appartenenza al merchant
    /// </summary>
    Task<EventDto?> GetByIdAsync(int id, int merchantId);

    /// <summary>
    /// Crea un nuovo evento
    /// </summary>
    Task<EventDto> CreateAsync(int merchantId, int createdByUserId, CreateEventRequest request);

    /// <summary>
    /// Aggiorna un evento esistente
    /// </summary>
    Task<EventDto?> UpdateAsync(int id, int merchantId, UpdateEventRequest request);

    /// <summary>
    /// Elimina un evento
    /// </summary>
    Task<bool> DeleteAsync(int id, int merchantId);

    /// <summary>
    /// Clona un evento in un intervallo di date
    /// </summary>
    Task<List<EventDto>> CloneAsync(int id, int merchantId, CloneEventRequest request);
}
