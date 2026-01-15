using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Interfaces;

public interface IClosurePeriodService
{
    Task<IEnumerable<ClosurePeriodDto>> GetMerchantClosurePeriodsAsync(int merchantId);
    Task<ClosurePeriodDto?> GetClosurePeriodByIdAsync(int id);
    Task<ClosurePeriodDto> CreateClosurePeriodAsync(int merchantId, CreateClosurePeriodDto dto);
    Task<ClosurePeriodDto> UpdateClosurePeriodAsync(int id, int merchantId, UpdateClosurePeriodDto dto);
    Task DeleteClosurePeriodAsync(int id, int merchantId);
    Task<bool> IsClosedOnDateAsync(int merchantId, DateTime date);
}
