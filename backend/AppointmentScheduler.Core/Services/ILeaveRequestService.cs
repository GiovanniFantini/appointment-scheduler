using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Interfaccia per la gestione delle richieste di permesso/ferie
/// </summary>
public interface ILeaveRequestService
{
    /// <summary>
    /// Recupera tutte le richieste di permesso di un merchant
    /// </summary>
    /// <param name="merchantId">ID del merchant</param>
    /// <returns>Lista di richieste permesso</returns>
    Task<IEnumerable<LeaveRequestDto>> GetMerchantLeaveRequestsAsync(int merchantId);

    /// <summary>
    /// Recupera le richieste di permesso di un dipendente
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <returns>Lista di richieste permesso del dipendente</returns>
    Task<IEnumerable<LeaveRequestDto>> GetEmployeeLeaveRequestsAsync(int employeeId);

    /// <summary>
    /// Recupera una richiesta di permesso per ID
    /// </summary>
    /// <param name="id">ID della richiesta</param>
    /// <returns>Dati della richiesta o null se non trovata</returns>
    Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(int id);

    /// <summary>
    /// Crea una nuova richiesta di permesso/ferie
    /// </summary>
    /// <param name="employeeId">ID del dipendente richiedente</param>
    /// <param name="request">Dati della richiesta</param>
    /// <returns>Richiesta creata</returns>
    Task<LeaveRequestDto> CreateLeaveRequestAsync(int employeeId, CreateLeaveRequest request);

    /// <summary>
    /// Risponde a una richiesta di permesso (approvazione/rifiuto da parte del merchant)
    /// </summary>
    /// <param name="leaveRequestId">ID della richiesta</param>
    /// <param name="merchantId">ID del merchant che risponde</param>
    /// <param name="request">Risposta</param>
    /// <returns>Richiesta aggiornata</returns>
    Task<LeaveRequestDto?> RespondToLeaveRequestAsync(int leaveRequestId, int merchantId, RespondToLeaveRequest request);

    /// <summary>
    /// Cancella una richiesta di permesso
    /// </summary>
    /// <param name="leaveRequestId">ID della richiesta da cancellare</param>
    /// <param name="employeeId">ID del dipendente richiedente</param>
    /// <returns>True se cancellata con successo</returns>
    Task<bool> CancelLeaveRequestAsync(int leaveRequestId, int employeeId);

    /// <summary>
    /// Recupera i saldi ferie di un dipendente per un anno specifico
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <param name="year">Anno di riferimento</param>
    /// <returns>Lista saldi ferie</returns>
    Task<IEnumerable<EmployeeLeaveBalanceDto>> GetEmployeeLeaveBalancesAsync(int employeeId, int year);

    /// <summary>
    /// Recupera tutti i saldi ferie dei dipendenti di un merchant
    /// </summary>
    /// <param name="merchantId">ID del merchant</param>
    /// <param name="year">Anno di riferimento</param>
    /// <returns>Lista saldi ferie di tutti i dipendenti</returns>
    Task<IEnumerable<EmployeeLeaveBalanceDto>> GetMerchantEmployeeLeaveBalancesAsync(int merchantId, int year);

    /// <summary>
    /// Crea o aggiorna il saldo ferie di un dipendente
    /// </summary>
    /// <param name="merchantId">ID del merchant</param>
    /// <param name="request">Dati del saldo</param>
    /// <returns>Saldo creato o aggiornato</returns>
    Task<EmployeeLeaveBalanceDto> UpsertEmployeeLeaveBalanceAsync(int merchantId, UpsertEmployeeLeaveBalanceRequest request);

    /// <summary>
    /// Elimina un saldo ferie
    /// </summary>
    /// <param name="balanceId">ID del saldo da eliminare</param>
    /// <param name="merchantId">ID del merchant</param>
    /// <returns>True se eliminato con successo</returns>
    Task<bool> DeleteEmployeeLeaveBalanceAsync(int balanceId, int merchantId);
}
