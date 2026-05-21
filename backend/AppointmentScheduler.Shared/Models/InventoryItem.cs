using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public string UnitOfMeasure { get; set; } = "pz";
    public decimal ReorderPoint { get; set; }
    public decimal AverageUnitCost { get; set; }
    public InventoryValuationMethod ValuationMethod { get; set; } = InventoryValuationMethod.WeightedAverage;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<InventoryStockBalance> StockBalances { get; set; } = new List<InventoryStockBalance>();
    public ICollection<InventoryMovement> Movements { get; set; } = new List<InventoryMovement>();
    public ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();
    public ICollection<GoodsReceiptLine> GoodsReceiptLines { get; set; } = new List<GoodsReceiptLine>();
}