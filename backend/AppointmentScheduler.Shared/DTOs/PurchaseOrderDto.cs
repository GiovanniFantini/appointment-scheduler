using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

public class PurchaseOrderDto
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public PurchaseOrderStatus Status { get; set; }
    public DateTime OrderedAt { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }
    public List<PurchaseOrderLineDto> Lines { get; set; } = new();
    public List<GoodsReceiptDto> Receipts { get; set; } = new();
}

public class PurchaseOrderLineDto
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string ItemSku { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}

public class CreatePurchaseOrderRequest
{
    public int BranchId { get; set; }
    public int SupplierId { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? Notes { get; set; }
    public List<CreatePurchaseOrderLineRequest> Lines { get; set; } = new();
}

public class CreatePurchaseOrderLineRequest
{
    public int ItemId { get; set; }
    public decimal QuantityOrdered { get; set; }
    public decimal UnitCost { get; set; }
}

public class GoodsReceiptDto
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int? PurchaseOrderId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }
    public List<GoodsReceiptLineDto> Lines { get; set; } = new();
}

public class GoodsReceiptLineDto
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string ItemSku { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int? PurchaseOrderLineId { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}

public class CreateGoodsReceiptRequest
{
    public string? Notes { get; set; }
    public List<CreateGoodsReceiptLineRequest> Lines { get; set; } = new();
}

public class CreateGoodsReceiptLineRequest
{
    public int PurchaseOrderLineId { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal? UnitCost { get; set; }
}