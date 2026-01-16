using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Interfaccia per la gestione dei limiti orari dei dipendenti
/// </summary>
public interface IEmployeeWorkingHoursLimitService
{
    /// <summary>
    /// Recupera tutti i limiti orari di un dipendente
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <returns>Lista di limiti orari del dipendente</returns>
    Task<IEnumerable<EmployeeWorkingHoursLimitDto>> GetEmployeeLimitsAsync(int employeeId);

    /// <summary>
    /// Recupera il limite orario attivo per un dipendente in una data specifica
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <param name="date">Data di riferimento</param>
    /// <returns>Limite attivo o null se non presente</returns>
    Task<EmployeeWorkingHoursLimitDto?> GetActiveEmployeeLimitAsync(int employeeId, DateTime date);

    /// <summary>
    /// Recupera un limite orario per ID
    /// </summary>
    /// <param name="id">ID del limite</param>
    /// <returns>Dati del limite o null se non trovato</returns>
    Task<EmployeeWorkingHoursLimitDto?> GetLimitByIdAsync(int id);

    /// <summary>
    /// Crea un nuovo limite orario per un dipendente
    /// </summary>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <param name="request">Dati del limite da creare</param>
    /// <returns>Limite creato</returns>
    Task<EmployeeWorkingHoursLimitDto> CreateLimitAsync(int merchantId, CreateEmployeeWorkingHoursLimitRequest request);

    /// <summary>
    /// Aggiorna un limite orario esistente
    /// </summary>
    /// <param name="limitId">ID del limite da aggiornare</param>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <param name="request">Nuovi dati del limite</param>
    /// <returns>Limite aggiornato o null se non trovato o non autorizzato</returns>
    Task<EmployeeWorkingHoursLimitDto?> UpdateLimitAsync(int limitId, int merchantId, UpdateEmployeeWorkingHoursLimitRequest request);

    /// <summary>
    /// Elimina un limite orario
    /// </summary>
    /// <param name="limitId">ID del limite da eliminare</param>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <returns>True se eliminato con successo</returns>
    Task<bool> DeleteLimitAsync(int limitId, int merchantId);
}
