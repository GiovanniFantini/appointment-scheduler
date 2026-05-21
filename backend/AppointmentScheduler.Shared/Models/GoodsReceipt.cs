namespace AppointmentScheduler.Shared.Models;

public class GoodsReceipt
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public int BranchId { get; set; }
    public int SupplierId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public int ReceivedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public MerchantBranch Branch { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public PurchaseOrder? PurchaseOrder { get; set; }
    public ICollection<GoodsReceiptLine> Lines { get; set; } = new List<GoodsReceiptLine>();
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
}