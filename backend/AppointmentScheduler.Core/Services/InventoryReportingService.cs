using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Core.Services;

public class InventoryReportingService : IInventoryReportingService
{
    private readonly ApplicationDbContext _context;

    public InventoryReportingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryDashboardDto> GetDashboardAsync(int merchantId, int? branchId = null)
    {
        var balances = _context.InventoryStockBalances.Where(b => b.MerchantId == merchantId);
        if (branchId.HasValue)
            balances = balances.Where(b => b.BranchId == branchId.Value);

        var totalItems = await _context.InventoryItems.CountAsync(i => i.MerchantId == merchantId && i.IsActive);
        var activeSuppliers = await _context.Suppliers.CountAsync(s => s.MerchantId == merchantId && s.IsActive);

        var openStatuses = new[]
        {
            Shared.Enums.PurchaseOrderStatus.Draft,
            Shared.Enums.PurchaseOrderStatus.Sent,
            Shared.Enums.PurchaseOrderStatus.PartiallyReceived
        };

        var openPurchaseOrdersQuery = _context.PurchaseOrders
            .Where(o => o.MerchantId == merchantId && openStatuses.Contains(o.Status));
        if (branchId.HasValue)
            openPurchaseOrdersQuery = openPurchaseOrdersQuery.Where(o => o.BranchId == branchId.Value);

        var totalStockValue = await balances.SumAsync(b => (decimal?)b.InventoryValue) ?? 0;

        var lowStockQuery = from balance in balances
                            join item in _context.InventoryItems on balance.ItemId equals item.Id
                            where item.ReorderPoint > 0 && balance.QuantityOnHand <= item.ReorderPoint
                            select balance.Id;

        return new InventoryDashboardDto
        {
            TotalItems = totalItems,
            ActiveSuppliers = activeSuppliers,
            OpenPurchaseOrders = await openPurchaseOrdersQuery.CountAsync(),
            TotalStockValue = totalStockValue,
            LowStockItems = await lowStockQuery.CountAsync()
        };
    }

    public async Task<List<InventoryValuationReportRowDto>> GetValuationAsync(int merchantId, int? branchId = null)
    {
        var query = _context.InventoryStockBalances
            .AsNoTracking()
            .Include(b => b.Branch)
            .Include(b => b.Item)
            .Where(b => b.MerchantId == merchantId);

        if (branchId.HasValue)
            query = query.Where(b => b.BranchId == branchId.Value);

        var balances = await query
            .OrderBy(b => b.Branch.Name)
            .ThenBy(b => b.Item.Name)
            .ToListAsync();

        return balances.Select(balance => new InventoryValuationReportRowDto
        {
            ItemId = balance.ItemId,
            Sku = balance.Item.Sku,
            ItemName = balance.Item.Name,
            BranchId = balance.BranchId,
            BranchName = balance.Branch.Name,
            QuantityOnHand = balance.QuantityOnHand,
            WeightedAverageCost = balance.WeightedAverageCost,
            InventoryValue = balance.InventoryValue
        }).ToList();
    }

    public async Task<List<LowStockReportRowDto>> GetLowStockAsync(int merchantId, int? branchId = null)
    {
        var query = _context.InventoryStockBalances
            .AsNoTracking()
            .Include(b => b.Branch)
            .Include(b => b.Item)
            .Where(b => b.MerchantId == merchantId && b.Item.ReorderPoint > 0 && b.QuantityOnHand <= b.Item.ReorderPoint);

        if (branchId.HasValue)
            query = query.Where(b => b.BranchId == branchId.Value);

        var rows = await query
            .OrderBy(b => b.Item.Name)
            .ToListAsync();

        return rows.Select(balance => new LowStockReportRowDto
        {
            ItemId = balance.ItemId,
            Sku = balance.Item.Sku,
            ItemName = balance.Item.Name,
            BranchId = balance.BranchId,
            BranchName = balance.Branch.Name,
            QuantityOnHand = balance.QuantityOnHand,
            ReorderPoint = balance.Item.ReorderPoint,
            SuggestedReorderQuantity = balance.Item.ReorderPoint - balance.QuantityOnHand
        }).ToList();
    }
}