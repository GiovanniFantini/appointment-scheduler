using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

public interface ISkillService
{
    Task<List<SkillDto>> GetAllByMerchantAsync(int merchantId);
    Task<SkillDto?> GetByIdAsync(int id, int merchantId);
    Task<SkillDto> CreateAsync(int merchantId, CreateSkillRequest request);
    Task<SkillDto?> UpdateAsync(int id, int merchantId, UpdateSkillRequest request);
    Task<bool> DeleteAsync(int id, int merchantId);
    Task<List<EmployeeDto>> GetEmployeesBySkillAsync(int skillId, int merchantId);

    /// <summary>
    /// Dipendenti del merchant che possiedono la mansione richiesta, ordinati
    /// per disponibilità (liberi prima, in conflitto dopo con motivo).
    /// </summary>
    Task<List<SuggestedEmployeeDto>> GetSuggestedEmployeesAsync(
        int merchantId,
        int skillId,
        DateOnly date,
        TimeOnly? startTime,
        TimeOnly? endTime,
        int? excludeEventId = null);
}
