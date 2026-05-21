using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

public interface IEmployeeInventoryService
{
    Task<EmployeeInventoryOverviewDto> GetOverviewAsync(int employeeId, int merchantId, int? branchId = null);
    Task<List<InventoryItemDto>> GetItemsAsync(int employeeId, int merchantId, int? branchId = null, string? search = null);
    Task<List<InventoryMovementDto>> GetMovementsAsync(int employeeId, int merchantId, int? branchId = null, int? itemId = null, DateOnly? from = null, DateOnly? to = null);
    Task<List<LowStockReportRowDto>> GetLowStockAsync(int employeeId, int merchantId, int? branchId = null);
}