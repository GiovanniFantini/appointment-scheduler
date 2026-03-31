using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Interfaccia per la gestione dei dipendenti
/// </summary>
public interface IEmployeeService
{
    /// <summary>
    /// Recupera tutti i dipendenti di un merchant tramite EmployeeMembership
    /// </summary>
    Task<List<EmployeeDto>> GetMerchantEmployeesAsync(int merchantId);

    /// <summary>
    /// Recupera un dipendente per ID, verificando l'appartenenza al merchant
    /// </summary>
    Task<EmployeeDto?> GetByIdAsync(int employeeId, int merchantId);

    /// <summary>
    /// Crea un nuovo dipendente e la relativa membership.
    /// Se l'email corrisponde a un Employee già esistente, riusa il record.
    /// </summary>
    Task<EmployeeDto> CreateAsync(int merchantId, CreateEmployeeRequest request);

    /// <summary>
    /// Aggiorna un dipendente esistente
    /// </summary>
    Task<EmployeeDto?> UpdateAsync(int employeeId, int merchantId, UpdateEmployeeRequest request);

    /// <summary>
    /// Rimuove un dipendente dal merchant (disattiva la membership)
    /// </summary>
    Task<bool> RemoveFromMerchantAsync(int employeeId, int merchantId);
}
