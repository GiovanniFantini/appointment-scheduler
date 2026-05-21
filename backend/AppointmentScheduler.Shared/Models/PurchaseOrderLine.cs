namespace AppointmentScheduler.Shared.Models;

public class PurchaseOrderLine
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ItemId { get; set; }
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public InventoryItem Item { get; set; } = null!;
    public ICollection<GoodsReceiptLine> ReceiptLines { get; set; } = new List<GoodsReceiptLine>();
}