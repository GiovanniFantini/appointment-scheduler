using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Interfaccia per la gestione delle richieste di scambio turni
/// </summary>
public interface IShiftSwapService
{
    /// <summary>
    /// Recupera tutte le richieste di scambio di un merchant
    /// </summary>
    /// <param name="merchantId">ID del merchant</param>
    /// <returns>Lista di richieste scambio</returns>
    Task<IEnumerable<ShiftSwapRequestDto>> GetMerchantSwapRequestsAsync(int merchantId);

    /// <summary>
    /// Recupera le richieste di scambio inviate da un dipendente
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <returns>Lista di richieste scambio inviate</returns>
    Task<IEnumerable<ShiftSwapRequestDto>> GetEmployeeSwapRequestsAsync(int employeeId);

    /// <summary>
    /// Recupera le richieste di scambio ricevute da un dipendente
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <returns>Lista di richieste scambio ricevute</returns>
    Task<IEnumerable<ShiftSwapRequestDto>> GetEmployeeReceivedSwapRequestsAsync(int employeeId);

    /// <summary>
    /// Recupera una richiesta di scambio per ID
    /// </summary>
    /// <param name="id">ID della richiesta</param>
    /// <returns>Dati della richiesta o null se non trovata</returns>
    Task<ShiftSwapRequestDto?> GetSwapRequestByIdAsync(int id);

    /// <summary>
    /// Crea una nuova richiesta di scambio turno
    /// </summary>
    /// <param name="requestingEmployeeId">ID del dipendente richiedente</param>
    /// <param name="request">Dati della richiesta</param>
    /// <returns>Richiesta creata</returns>
    Task<ShiftSwapRequestDto> CreateSwapRequestAsync(int requestingEmployeeId, CreateShiftSwapRequest request);

    /// <summary>
    /// Risponde a una richiesta di scambio (da parte del dipendente target o merchant)
    /// </summary>
    /// <param name="swapRequestId">ID della richiesta</param>
    /// <param name="respondingUserId">ID dell'utente che risponde</param>
    /// <param name="request">Risposta</param>
    /// <returns>Richiesta aggiornata</returns>
    Task<ShiftSwapRequestDto?> RespondToSwapRequestAsync(int swapRequestId, int respondingUserId, RespondToShiftSwapRequest request);

    /// <summary>
    /// Cancella una richiesta di scambio
    /// </summary>
    /// <param name="swapRequestId">ID della richiesta da cancellare</param>
    /// <param name="employeeId">ID del dipendente richiedente</param>
    /// <returns>True se cancellata con successo</returns>
    Task<bool> CancelSwapRequestAsync(int swapRequestId, int employeeId);
}
