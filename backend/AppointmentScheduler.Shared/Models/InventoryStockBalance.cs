namespace AppointmentScheduler.Shared.Models;

public class InventoryStockBalance
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public int BranchId { get; set; }
    public int ItemId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal WeightedAverageCost { get; set; }
    public decimal InventoryValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public MerchantBranch Branch { get; set; } = null!;
    public InventoryItem Item { get; set; } = null!;
}