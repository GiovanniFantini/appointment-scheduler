using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

public interface IMerchantReportingService
{
    Task<MerchantReportDto> GetAsync(int merchantId, DateOnly from, DateOnly to, int? branchId, CancellationToken cancellationToken = default);
}
