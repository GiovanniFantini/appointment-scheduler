using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

public interface IInventoryReportingService
{
    Task<InventoryDashboardDto> GetDashboardAsync(int merchantId, int? branchId = null);
    Task<List<InventoryValuationReportRowDto>> GetValuationAsync(int merchantId, int? branchId = null);
    Task<List<LowStockReportRowDto>> GetLowStockAsync(int merchantId, int? branchId = null);
}