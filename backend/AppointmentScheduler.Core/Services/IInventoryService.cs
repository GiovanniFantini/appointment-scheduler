using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

public interface IInventoryService
{
    Task<List<InventoryItemDto>> GetItemsAsync(int merchantId, int? branchId = null, string? search = null, bool includeInactive = true);
    Task<InventoryItemDto?> GetItemByIdAsync(int itemId, int merchantId, int? branchId = null);
    Task<InventoryItemDto> CreateItemAsync(int merchantId, CreateInventoryItemRequest request);
    Task<InventoryItemDto?> UpdateItemAsync(int itemId, int merchantId, UpdateInventoryItemRequest request);
    Task<List<InventoryMovementDto>> GetMovementsAsync(int merchantId, int? branchId = null, int? itemId = null, DateOnly? from = null, DateOnly? to = null);
    Task<InventoryMovementDto> CreateAdjustmentAsync(int merchantId, int userId, CreateInventoryAdjustmentRequest request);
}