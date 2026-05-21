namespace AppointmentScheduler.Shared.DTOs;

public class InventoryBranchOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsHomeBranch { get; set; }
    public bool IsActive { get; set; }
}

public class EmployeeInventoryOverviewDto
{
    public int HomeBranchId { get; set; }
    public int? SelectedBranchId { get; set; }
    public List<InventoryBranchOptionDto> AccessibleBranches { get; set; } = new();
    public InventoryDashboardDto Dashboard { get; set; } = new();
}

public class InventoryDashboardDto
{
    public int TotalItems { get; set; }
    public int ActiveSuppliers { get; set; }
    public int OpenPurchaseOrders { get; set; }
    public decimal TotalStockValue { get; set; }
    public int LowStockItems { get; set; }
}

public class InventoryValuationReportRowDto
{
    public int ItemId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal WeightedAverageCost { get; set; }
    public decimal InventoryValue { get; set; }
}

public class LowStockReportRowDto
{
    public int ItemId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal SuggestedReorderQuantity { get; set; }
}