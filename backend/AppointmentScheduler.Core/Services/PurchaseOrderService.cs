using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Core.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly ApplicationDbContext _context;

    public PurchaseOrderService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PurchaseOrderDto>> GetOrdersAsync(int merchantId, int? branchId = null, PurchaseOrderStatus? status = null)
    {
        var query = BuildOrderQuery(merchantId);

        if (branchId.HasValue)
            query = query.Where(o => o.BranchId == branchId.Value);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var orders = await query
            .OrderByDescending(o => o.OrderedAt)
            .ToListAsync();

        return orders.Select(MapOrder).ToList();
    }

    public async Task<PurchaseOrderDto?> GetOrderByIdAsync(int orderId, int merchantId)
    {
        var order = await BuildOrderQuery(merchantId)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        return order == null ? null : MapOrder(order);
    }

    public async Task<PurchaseOrderDto> CreateOrderAsync(int merchantId, int userId, CreatePurchaseOrderRequest request)
    {
        if (request.Lines == null || request.Lines.Count == 0)
            throw new InvalidOperationException("L'ordine acquisto deve contenere almeno una riga.");

        var branch = await _context.MerchantBranches
            .FirstOrDefaultAsync(b => b.Id == request.BranchId && b.MerchantId == merchantId && b.IsActive);
        if (branch == null)
            throw new InvalidOperationException("Filiale non trovata o non attiva.");

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId && s.MerchantId == merchantId && s.IsActive);
        if (supplier == null)
            throw new InvalidOperationException("Fornitore non trovato o non attivo.");

        var itemIds = request.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await _context.InventoryItems
            .Where(i => i.MerchantId == merchantId && itemIds.Contains(i.Id) && i.IsActive)
            .ToListAsync();

        if (items.Count != itemIds.Count)
            throw new InvalidOperationException("Una o più righe ordine fanno riferimento ad articoli non validi.");

        var order = new PurchaseOrder
        {
            MerchantId = merchantId,
            BranchId = branch.Id,
            SupplierId = supplier.Id,
            OrderNumber = GenerateDocumentNumber("PO"),
            Status = PurchaseOrderStatus.Draft,
            OrderedAt = DateTime.UtcNow,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            Notes = NormalizeOptional(request.Notes),
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            Lines = request.Lines.Select(line =>
            {
                if (line.QuantityOrdered <= 0)
                    throw new InvalidOperationException("La quantità ordinata deve essere maggiore di zero.");
                if (line.UnitCost < 0)
                    throw new InvalidOperationException("Il costo unitario non può essere negativo.");

                return new PurchaseOrderLine
                {
                    ItemId = line.ItemId,
                    QuantityOrdered = line.QuantityOrdered,
                    QuantityReceived = 0,
                    UnitCost = line.UnitCost,
                    LineTotal = line.QuantityOrdered * line.UnitCost
                };
            }).ToList()
        };

        _context.PurchaseOrders.Add(order);
        await _context.SaveChangesAsync();

        return (await GetOrderByIdAsync(order.Id, merchantId))!;
    }

    public async Task<PurchaseOrderDto?> MarkAsSentAsync(int orderId, int merchantId)
    {
        var order = await _context.PurchaseOrders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.MerchantId == merchantId);

        if (order == null)
            return null;

        if (order.Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Solo un ordine Draft può essere inviato.");

        order.Status = PurchaseOrderStatus.Sent;
        order.SentAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetOrderByIdAsync(orderId, merchantId);
    }

    public async Task<PurchaseOrderDto?> CancelOrderAsync(int orderId, int merchantId)
    {
        var order = await _context.PurchaseOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.MerchantId == merchantId);

        if (order == null)
            return null;

        if (order.Status == PurchaseOrderStatus.Closed)
            throw new InvalidOperationException("Un ordine chiuso non può essere annullato.");

        if (order.Lines.Any(l => l.QuantityReceived > 0))
            throw new InvalidOperationException("Un ordine già ricevuto anche solo parzialmente non può essere annullato.");

        order.Status = PurchaseOrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetOrderByIdAsync(orderId, merchantId);
    }

    public async Task<PurchaseOrderDto?> ReceiveOrderAsync(int orderId, int merchantId, int userId, CreateGoodsReceiptRequest request)
    {
        var order = await _context.PurchaseOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.MerchantId == merchantId);

        if (order == null)
            return null;

        if (order.Status == PurchaseOrderStatus.Cancelled)
            throw new InvalidOperationException("Un ordine annullato non può essere ricevuto.");

        if (order.Status == PurchaseOrderStatus.Closed)
            throw new InvalidOperationException("L'ordine è già chiuso.");

        if (request.Lines == null || request.Lines.Count == 0)
            throw new InvalidOperationException("La ricezione deve contenere almeno una riga.");

        var lineIds = request.Lines.Select(l => l.PurchaseOrderLineId).Distinct().ToList();
        var poLines = order.Lines.Where(l => lineIds.Contains(l.Id)).ToDictionary(l => l.Id);
        if (poLines.Count != lineIds.Count)
            throw new InvalidOperationException("Una o più righe di ricezione non appartengono all'ordine indicato.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var receipt = new GoodsReceipt
        {
            MerchantId = merchantId,
            BranchId = order.BranchId,
            SupplierId = order.SupplierId,
            PurchaseOrderId = order.Id,
            ReceiptNumber = GenerateDocumentNumber("GR"),
            ReceivedAt = DateTime.UtcNow,
            Notes = NormalizeOptional(request.Notes),
            ReceivedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.GoodsReceipts.Add(receipt);

        var touchedItemIds = new HashSet<int>();

        foreach (var lineRequest in request.Lines)
        {
            if (lineRequest.QuantityReceived <= 0)
                throw new InvalidOperationException("La quantità ricevuta deve essere maggiore di zero.");

            var poLine = poLines[lineRequest.PurchaseOrderLineId];
            var remaining = poLine.QuantityOrdered - poLine.QuantityReceived;
            if (lineRequest.QuantityReceived > remaining)
                throw new InvalidOperationException("La quantità ricevuta supera la quantità residua dell'ordine.");

            var unitCost = lineRequest.UnitCost ?? poLine.UnitCost;
            if (unitCost < 0)
                throw new InvalidOperationException("Il costo unitario non può essere negativo.");

            poLine.QuantityReceived += lineRequest.QuantityReceived;

            var receiptLine = new GoodsReceiptLine
            {
                GoodsReceipt = receipt,
                ItemId = poLine.ItemId,
                PurchaseOrderLineId = poLine.Id,
                QuantityReceived = lineRequest.QuantityReceived,
                UnitCost = unitCost,
                LineTotal = lineRequest.QuantityReceived * unitCost
            };
            _context.GoodsReceiptLines.Add(receiptLine);

            var balance = await GetOrCreateBalanceAsync(merchantId, order.BranchId, poLine.ItemId);
            var oldQty = balance.QuantityOnHand;
            var oldCost = balance.WeightedAverageCost;
            var newQty = oldQty + lineRequest.QuantityReceived;
            var newWeighted = newQty == 0
                ? 0
                : ((oldQty * oldCost) + (lineRequest.QuantityReceived * unitCost)) / newQty;

            balance.QuantityOnHand = newQty;
            balance.WeightedAverageCost = newWeighted;
            balance.InventoryValue = newQty * newWeighted;
            balance.UpdatedAt = DateTime.UtcNow;

            _context.InventoryMovements.Add(new InventoryMovement
            {
                MerchantId = merchantId,
                BranchId = order.BranchId,
                ItemId = poLine.ItemId,
                Type = InventoryMovementType.PurchaseReceipt,
                QuantityDelta = lineRequest.QuantityReceived,
                UnitCost = unitCost,
                TotalValue = lineRequest.QuantityReceived * unitCost,
                Reason = "Ricezione ordine acquisto",
                ReferenceNumber = order.OrderNumber,
                PurchaseOrder = order,
                GoodsReceipt = receipt,
                PerformedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            touchedItemIds.Add(poLine.ItemId);
        }

        order.Status = order.Lines.All(l => l.QuantityReceived >= l.QuantityOrdered)
            ? PurchaseOrderStatus.Closed
            : PurchaseOrderStatus.PartiallyReceived;
        order.ClosedAt = order.Status == PurchaseOrderStatus.Closed ? DateTime.UtcNow : null;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        foreach (var itemId in touchedItemIds)
            await RecalculateAverageUnitCostAsync(itemId);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetOrderByIdAsync(orderId, merchantId);
    }

    private IQueryable<PurchaseOrder> BuildOrderQuery(int merchantId)
    {
        return _context.PurchaseOrders
            .AsNoTracking()
            .Include(o => o.Branch)
            .Include(o => o.Supplier)
            .Include(o => o.Lines)
                .ThenInclude(l => l.Item)
            .Include(o => o.Receipts)
                .ThenInclude(r => r.Lines)
                    .ThenInclude(l => l.Item)
            .Where(o => o.MerchantId == merchantId);
    }

    private async Task<InventoryStockBalance> GetOrCreateBalanceAsync(int merchantId, int branchId, int itemId)
    {
        var balance = await _context.InventoryStockBalances
            .FirstOrDefaultAsync(b => b.MerchantId == merchantId && b.BranchId == branchId && b.ItemId == itemId);

        if (balance != null)
            return balance;

        balance = new InventoryStockBalance
        {
            MerchantId = merchantId,
            BranchId = branchId,
            ItemId = itemId,
            QuantityOnHand = 0,
            WeightedAverageCost = 0,
            InventoryValue = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.InventoryStockBalances.Add(balance);
        return balance;
    }

    private async Task RecalculateAverageUnitCostAsync(int itemId)
    {
        var item = await _context.InventoryItems.FirstAsync(i => i.Id == itemId);
        var totals = await _context.InventoryStockBalances
            .Where(b => b.ItemId == itemId)
            .GroupBy(_ => 1)
            .Select(g => new { Quantity = g.Sum(x => x.QuantityOnHand), Value = g.Sum(x => x.InventoryValue) })
            .FirstOrDefaultAsync();

        item.AverageUnitCost = totals == null || totals.Quantity == 0 ? 0 : totals.Value / totals.Quantity;
        item.UpdatedAt = DateTime.UtcNow;
    }

    private static PurchaseOrderDto MapOrder(PurchaseOrder order) => new()
    {
        Id = order.Id,
        MerchantId = order.MerchantId,
        BranchId = order.BranchId,
        BranchName = order.Branch.Name,
        SupplierId = order.SupplierId,
        SupplierName = order.Supplier.Name,
        OrderNumber = order.OrderNumber,
        Status = order.Status,
        OrderedAt = order.OrderedAt,
        ExpectedDeliveryDate = order.ExpectedDeliveryDate,
        SentAt = order.SentAt,
        ClosedAt = order.ClosedAt,
        CancelledAt = order.CancelledAt,
        Notes = order.Notes,
        TotalAmount = order.Lines.Sum(l => l.LineTotal),
        Lines = order.Lines.Select(MapOrderLine).ToList(),
        Receipts = order.Receipts.OrderByDescending(r => r.ReceivedAt).Select(r => MapReceipt(r, order.Branch.Name, order.Supplier.Name)).ToList()
    };

    private static PurchaseOrderLineDto MapOrderLine(PurchaseOrderLine line) => new()
    {
        Id = line.Id,
        ItemId = line.ItemId,
        ItemSku = line.Item.Sku,
        ItemName = line.Item.Name,
        QuantityOrdered = line.QuantityOrdered,
        QuantityReceived = line.QuantityReceived,
        UnitCost = line.UnitCost,
        LineTotal = line.LineTotal
    };

    private static GoodsReceiptDto MapReceipt(GoodsReceipt receipt, string branchName, string supplierName) => new()
    {
        Id = receipt.Id,
        MerchantId = receipt.MerchantId,
        BranchId = receipt.BranchId,
        BranchName = branchName,
        SupplierId = receipt.SupplierId,
        SupplierName = supplierName,
        PurchaseOrderId = receipt.PurchaseOrderId,
        ReceiptNumber = receipt.ReceiptNumber,
        ReceivedAt = receipt.ReceivedAt,
        Notes = receipt.Notes,
        TotalAmount = receipt.Lines.Sum(l => l.LineTotal),
        Lines = receipt.Lines.Select(line => new GoodsReceiptLineDto
        {
            Id = line.Id,
            ItemId = line.ItemId,
            ItemSku = line.Item.Sku,
            ItemName = line.Item.Name,
            PurchaseOrderLineId = line.PurchaseOrderLineId,
            QuantityReceived = line.QuantityReceived,
            UnitCost = line.UnitCost,
            LineTotal = line.LineTotal
        }).ToList()
    };

    private static string GenerateDocumentNumber(string prefix)
        => $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(1000, 9999)}";

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}