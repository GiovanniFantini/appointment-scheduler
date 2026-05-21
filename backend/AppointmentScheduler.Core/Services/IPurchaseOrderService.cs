using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

public interface IPurchaseOrderService
{
    Task<List<PurchaseOrderDto>> GetOrdersAsync(int merchantId, int? branchId = null, PurchaseOrderStatus? status = null);
    Task<PurchaseOrderDto?> GetOrderByIdAsync(int orderId, int merchantId);
    Task<PurchaseOrderDto> CreateOrderAsync(int merchantId, int userId, CreatePurchaseOrderRequest request);
    Task<PurchaseOrderDto?> MarkAsSentAsync(int orderId, int merchantId);
    Task<PurchaseOrderDto?> CancelOrderAsync(int orderId, int merchantId);
    Task<PurchaseOrderDto?> ReceiveOrderAsync(int orderId, int merchantId, int userId, CreateGoodsReceiptRequest request);
}