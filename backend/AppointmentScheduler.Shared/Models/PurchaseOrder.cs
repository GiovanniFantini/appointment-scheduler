using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

public class PurchaseOrder
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public int BranchId { get; set; }
    public int SupplierId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? Notes { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public MerchantBranch Branch { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
    public ICollection<GoodsReceipt> Receipts { get; set; } = new List<GoodsReceipt>();
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
}