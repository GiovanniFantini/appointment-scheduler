using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Interfaccia per la gestione dei turni
/// </summary>
public interface IShiftService
{
    /// <summary>
    /// Recupera tutti i turni di un merchant in un range di date
    /// </summary>
    /// <param name="merchantId">ID del merchant</param>
    /// <param name="startDate">Data inizio</param>
    /// <param name="endDate">Data fine</param>
    /// <returns>Lista di turni del merchant nel periodo</returns>
    Task<IEnumerable<ShiftDto>> GetMerchantShiftsAsync(int merchantId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Recupera tutti i turni di un dipendente in un range di date
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <param name="startDate">Data inizio</param>
    /// <param name="endDate">Data fine</param>
    /// <returns>Lista di turni del dipendente nel periodo</returns>
    Task<IEnumerable<ShiftDto>> GetEmployeeShiftsAsync(int employeeId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Recupera un turno per ID
    /// </summary>
    /// <param name="id">ID del turno</param>
    /// <returns>Dati del turno o null se non trovato</returns>
    Task<ShiftDto?> GetShiftByIdAsync(int id);

    /// <summary>
    /// Crea un nuovo turno
    /// </summary>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <param name="request">Dati del turno da creare</param>
    /// <returns>Turno creato</returns>
    Task<ShiftDto> CreateShiftAsync(int merchantId, CreateShiftRequest request);

    /// <summary>
    /// Crea turni multipli da un template
    /// </summary>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <param name="request">Dati per la creazione massiva</param>
    /// <returns>Lista di turni creati</returns>
    Task<IEnumerable<ShiftDto>> CreateShiftsFromTemplateAsync(int merchantId, CreateShiftsFromTemplateRequest request);

    /// <summary>
    /// Aggiorna un turno esistente
    /// </summary>
    /// <param name="shiftId">ID del turno da aggiornare</param>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <param name="request">Nuovi dati del turno</param>
    /// <returns>Turno aggiornato o null se non trovato o non autorizzato</returns>
    Task<ShiftDto?> UpdateShiftAsync(int shiftId, int merchantId, UpdateShiftRequest request);

    /// <summary>
    /// Assegna un turno a un dipendente
    /// </summary>
    /// <param name="shiftId">ID del turno</param>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <param name="request">Dati assegnazione</param>
    /// <returns>Turno aggiornato</returns>
    Task<ShiftDto?> AssignShiftAsync(int shiftId, int merchantId, AssignShiftRequest request);

    /// <summary>
    /// Elimina un turno
    /// </summary>
    /// <param name="shiftId">ID del turno da eliminare</param>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <returns>True se eliminato con successo</returns>
    Task<bool> DeleteShiftAsync(int shiftId, int merchantId);

    /// <summary>
    /// Recupera statistiche turni per un dipendente
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <returns>Statistiche turni</returns>
    Task<EmployeeShiftStatsDto> GetEmployeeShiftStatsAsync(int employeeId);

    /// <summary>
    /// Verifica se un turno va in conflitto con altri turni del dipendente
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <param name="date">Data del turno</param>
    /// <param name="startTime">Ora inizio</param>
    /// <param name="endTime">Ora fine</param>
    /// <param name="excludeShiftId">ID del turno da escludere (per update)</param>
    /// <returns>True se ci sono conflitti</returns>
    Task<bool> HasShiftConflictAsync(int employeeId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeShiftId = null);

    /// <summary>
    /// Verifica se il turno supera i limiti orari del dipendente
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <param name="date">Data del turno</param>
    /// <param name="hours">Ore del turno</param>
    /// <returns>True se supera i limiti</returns>
    Task<bool> ExceedsWorkingHoursLimitAsync(int employeeId, DateTime date, decimal hours);
}
