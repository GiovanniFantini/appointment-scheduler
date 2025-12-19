using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Interfaccia per la gestione delle prenotazioni
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Recupera tutte le prenotazioni di un utente
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <returns>Lista di prenotazioni dell'utente</returns>
    Task<IEnumerable<BookingDto>> GetUserBookingsAsync(int userId);

    /// <summary>
    /// Recupera tutte le prenotazioni di un merchant
    /// </summary>
    /// <param name="merchantId">ID del merchant</param>
    /// <returns>Lista di prenotazioni del merchant</returns>
    Task<IEnumerable<BookingDto>> GetMerchantBookingsAsync(int merchantId);

    /// <summary>
    /// Recupera una prenotazione per ID
    /// </summary>
    /// <param name="bookingId">ID della prenotazione</param>
    /// <returns>Dati della prenotazione o null se non trovata</returns>
    Task<BookingDto?> GetBookingByIdAsync(int bookingId);

    /// <summary>
    /// Crea una nuova prenotazione
    /// </summary>
    /// <param name="userId">ID dell'utente che prenota</param>
    /// <param name="request">Dati della prenotazione</param>
    /// <returns>Prenotazione creata</returns>
    Task<BookingDto> CreateBookingAsync(int userId, CreateBookingRequest request);

    /// <summary>
    /// Conferma una prenotazione (solo merchant)
    /// </summary>
    /// <param name="bookingId">ID della prenotazione</param>
    /// <param name="merchantId">ID del merchant che conferma</param>
    /// <returns>True se confermata con successo</returns>
    Task<bool> ConfirmBookingAsync(int bookingId, int merchantId);

    /// <summary>
    /// Cancella una prenotazione
    /// </summary>
    /// <param name="bookingId">ID della prenotazione</param>
    /// <param name="userId">ID dell'utente che cancella</param>
    /// <returns>True se cancellata con successo</returns>
    Task<bool> CancelBookingAsync(int bookingId, int userId);

    /// <summary>
    /// Completa una prenotazione (solo merchant)
    /// </summary>
    /// <param name="bookingId">ID della prenotazione</param>
    /// <param name="merchantId">ID del merchant</param>
    /// <returns>True se completata con successo</returns>
    Task<bool> CompleteBookingAsync(int bookingId, int merchantId);
}
