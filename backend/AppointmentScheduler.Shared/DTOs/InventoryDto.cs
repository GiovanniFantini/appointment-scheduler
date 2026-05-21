using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

public class InventoryItemDto
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
    public InventoryValuationMethod ValuationMethod { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalQuantityOnHand { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public List<InventoryStockBalanceDto> Balances { get; set; } = new();
    public List<InventoryMovementDto> RecentMovements { get; set; } = new();
}

public class InventoryStockBalanceDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal WeightedAverageCost { get; set; }
    public decimal InventoryValue { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class InventoryMovementDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public InventoryMovementType Type { get; set; }
    public decimal QuantityDelta { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public string? Reason { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateInventoryItemRequest
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal ReorderPoint { get; set; }
}

public class UpdateInventoryItemRequest
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal ReorderPoint { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateInventoryAdjustmentRequest
{
    public int BranchId { get; set; }
    public int ItemId { get; set; }
    public decimal QuantityDelta { get; set; }
    public decimal? UnitCost { get; set; }
    public string Reason { get; set; } = string.Empty;
}