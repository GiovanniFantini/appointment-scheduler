using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

public interface IEmployeeInventoryService
{
    // ── Consultazione (livello ReadOnly) ───────────────────────────────────
    Task<EmployeeInventoryOverviewDto> GetOverviewAsync(int employeeId, int merchantId, int? branchId = null);
    Task<List<InventoryItemDto>> GetItemsAsync(int employeeId, int merchantId, int? branchId = null, string? search = null);
    Task<List<InventoryMovementDto>> GetMovementsAsync(int employeeId, int merchantId, int? branchId = null, int? itemId = null, DateOnly? from = null, DateOnly? to = null);
    Task<List<LowStockReportRowDto>> GetLowStockAsync(int employeeId, int merchantId, int? branchId = null);

    // ── Anagrafica fornitori (lettura ReadOnly) ─────────────────────────────
    Task<List<SupplierDto>> GetSuppliersAsync(int employeeId, int merchantId, bool includeInactive = true);

    // ── Ordini di acquisto (lettura ReadOnly) ───────────────────────────────
    Task<List<PurchaseOrderDto>> GetPurchaseOrdersAsync(int employeeId, int merchantId, int? branchId = null, PurchaseOrderStatus? status = null);
    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(int employeeId, int merchantId, int orderId);

    // ── Operatività quotidiana (livello Operator) ───────────────────────────
    Task<InventoryMovementDto> CreateAdjustmentAsync(int employeeId, int merchantId, int userId, CreateInventoryAdjustmentRequest request);
    Task<PurchaseOrderDto?> ReceiveOrderAsync(int employeeId, int merchantId, int userId, int orderId, CreateGoodsReceiptRequest request);

    // ── Anagrafiche e gestione ordini (livello Manager) ─────────────────────
    Task<InventoryItemDto> CreateItemAsync(int employeeId, int merchantId, CreateInventoryItemRequest request);
    Task<InventoryItemDto?> UpdateItemAsync(int employeeId, int merchantId, int itemId, UpdateInventoryItemRequest request);
    Task<SupplierDto> CreateSupplierAsync(int employeeId, int merchantId, CreateSupplierRequest request);
    Task<SupplierDto?> UpdateSupplierAsync(int employeeId, int merchantId, int supplierId, UpdateSupplierRequest request);
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(int employeeId, int merchantId, int userId, CreatePurchaseOrderRequest request);
    Task<PurchaseOrderDto?> MarkPurchaseOrderSentAsync(int employeeId, int merchantId, int orderId);
    Task<PurchaseOrderDto?> CancelPurchaseOrderAsync(int employeeId, int merchantId, int orderId);
}