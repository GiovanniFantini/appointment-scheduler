using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

public class InventoryMovement
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public int BranchId { get; set; }
    public int ItemId { get; set; }
    public InventoryMovementType Type { get; set; }
    public decimal QuantityDelta { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public string? Reason { get; set; }
    public string? ReferenceNumber { get; set; }
    public int? PurchaseOrderId { get; set; }
    public int? GoodsReceiptId { get; set; }
    public int? PerformedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public MerchantBranch Branch { get; set; } = null!;
    public InventoryItem Item { get; set; } = null!;
    public PurchaseOrder? PurchaseOrder { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }
}