using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Interfaccia per la gestione dei dipendenti
/// </summary>
public interface IEmployeeService
{
    /// <summary>
    /// Recupera tutti i dipendenti di un merchant
    /// </summary>
    /// <param name="merchantId">ID del merchant</param>
    /// <returns>Lista di dipendenti del merchant</returns>
    Task<IEnumerable<EmployeeDto>> GetMerchantEmployeesAsync(int merchantId);

    /// <summary>
    /// Recupera un dipendente per ID
    /// </summary>
    /// <param name="id">ID del dipendente</param>
    /// <returns>Dati del dipendente o null se non trovato</returns>
    Task<EmployeeDto?> GetEmployeeByIdAsync(int id);

    /// <summary>
    /// Crea un nuovo dipendente
    /// </summary>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <param name="request">Dati del dipendente da creare</param>
    /// <returns>Dipendente creato</returns>
    Task<EmployeeDto> CreateEmployeeAsync(int merchantId, CreateEmployeeRequest request);

    /// <summary>
    /// Aggiorna un dipendente esistente
    /// </summary>
    /// <param name="employeeId">ID del dipendente da aggiornare</param>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <param name="request">Nuovi dati del dipendente</param>
    /// <returns>Dipendente aggiornato o null se non trovato o non autorizzato</returns>
    Task<EmployeeDto?> UpdateEmployeeAsync(int employeeId, int merchantId, UpdateEmployeeRequest request);

    /// <summary>
    /// Elimina un dipendente
    /// </summary>
    /// <param name="employeeId">ID del dipendente da eliminare</param>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <returns>True se eliminato con successo</returns>
    Task<bool> DeleteEmployeeAsync(int employeeId, int merchantId);
}
