namespace AppointmentScheduler.Shared.Models;

public class GoodsReceiptLine
{
    public int Id { get; set; }
    public int GoodsReceiptId { get; set; }
    public int ItemId { get; set; }
    public int? PurchaseOrderLineId { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }

    public GoodsReceipt GoodsReceipt { get; set; } = null!;
    public InventoryItem Item { get; set; } = null!;
    public PurchaseOrderLine? PurchaseOrderLine { get; set; }
}