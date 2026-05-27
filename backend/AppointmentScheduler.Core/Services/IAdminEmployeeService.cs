using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio sys-admin per la consultazione degli employee (anche quelli pre-caricati senza User).
/// Read-only: la gestione anagrafica resta al merchant.
/// </summary>
public interface IAdminEmployeeService
{
    Task<AdminEmployeeListResponse> GetEmployeesAsync(AdminEmployeeListQuery query);
    Task<AdminEmployeeDetailDto?> GetByIdAsync(int id);
}
