using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

public interface IEmployeeRequestService
{
    /// <summary>
    /// Recupera tutte le richieste del merchant, con filtro opzionale per status
    /// </summary>
    Task<List<EmployeeRequestDto>> GetMerchantRequestsAsync(int merchantId, RequestStatus? status = null);

    /// <summary>
    /// Recupera una singola richiesta, verificando l'appartenenza al merchant
    /// </summary>
    Task<EmployeeRequestDto?> GetByIdAsync(int id, int merchantId);

    /// <summary>
    /// Crea una nuova richiesta da parte di un dipendente
    /// </summary>
    Task<EmployeeRequestDto> CreateAsync(int employeeId, int merchantId, CreateEmployeeRequestRequest request);

    /// <summary>
    /// Approva una richiesta
    /// </summary>
    Task<EmployeeRequestDto?> ApproveAsync(int id, int merchantId, int reviewerUserId, ReviewEmployeeRequestRequest? request = null);

    /// <summary>
    /// Rifiuta una richiesta
    /// </summary>
    Task<EmployeeRequestDto?> RejectAsync(int id, int merchantId, int reviewerUserId, ReviewEmployeeRequestRequest? request = null);

    /// <summary>
    /// Recupera le richieste di un dipendente nel merchant
    /// </summary>
    Task<List<EmployeeRequestDto>> GetEmployeeRequestsAsync(int employeeId, int merchantId, RequestStatus? status = null);
}
