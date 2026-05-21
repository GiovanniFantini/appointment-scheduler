using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Core.Services;

public class EmployeeInventoryService : IEmployeeInventoryService
{
    private readonly ApplicationDbContext _context;

    public EmployeeInventoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeInventoryOverviewDto> GetOverviewAsync(int employeeId, int merchantId, int? branchId = null)
    {
        var scope = await ResolveScopeAsync(employeeId, merchantId, branchId);

        var balances = _context.InventoryStockBalances
            .AsNoTracking()
            .Where(b => b.MerchantId == merchantId && scope.FilteredBranchIds.Contains(b.BranchId));

        var openStatuses = new[]
        {
            Shared.Enums.PurchaseOrderStatus.Draft,
            Shared.Enums.PurchaseOrderStatus.Sent,
            Shared.Enums.PurchaseOrderStatus.PartiallyReceived,
        };

        var purchaseOrders = _context.PurchaseOrders
            .AsNoTracking()
            .Where(o => o.MerchantId == merchantId && scope.FilteredBranchIds.Contains(o.BranchId));

        var supplierIds = await purchaseOrders
            .Select(o => o.SupplierId)
            .Distinct()
            .ToListAsync();

        var lowStockQuery = from balance in balances
                            join item in _context.InventoryItems on balance.ItemId equals item.Id
                            where item.ReorderPoint > 0 && balance.QuantityOnHand <= item.ReorderPoint
                            select balance.Id;

        return new EmployeeInventoryOverviewDto
        {
            HomeBranchId = scope.HomeBranchId,
            SelectedBranchId = scope.SelectedBranchId,
            AccessibleBranches = scope.Branches,
            Dashboard = new InventoryDashboardDto
            {
                TotalItems = await balances.Select(b => b.ItemId).Distinct().CountAsync(),
                ActiveSuppliers = supplierIds.Count == 0
                    ? 0
                    : await _context.Suppliers.CountAsync(s => supplierIds.Contains(s.Id) && s.IsActive),
                OpenPurchaseOrders = await purchaseOrders.CountAsync(o => openStatuses.Contains(o.Status)),
                TotalStockValue = await balances.SumAsync(b => (decimal?)b.InventoryValue) ?? 0,
                LowStockItems = await lowStockQuery.CountAsync(),
            }
        };
    }

    public async Task<List<InventoryItemDto>> GetItemsAsync(int employeeId, int merchantId, int? branchId = null, string? search = null)
    {
        var scope = await ResolveScopeAsync(employeeId, merchantId, branchId);

        var balancesQuery = _context.InventoryStockBalances
            .AsNoTracking()
            .Include(b => b.Branch)
            .Where(b => b.MerchantId == merchantId && scope.FilteredBranchIds.Contains(b.BranchId));

        var itemIds = await balancesQuery
            .Select(b => b.ItemId)
            .Distinct()
            .ToListAsync();

        var itemsQuery = _context.InventoryItems
            .AsNoTracking()
            .Where(i => i.MerchantId == merchantId && i.IsActive && itemIds.Contains(i.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            itemsQuery = itemsQuery.Where(i =>
                i.Sku.Contains(term) ||
                i.Name.Contains(term) ||
                (i.Barcode != null && i.Barcode.Contains(term)));
        }

        var items = await itemsQuery
            .OrderBy(i => i.Name)
            .ToListAsync();

        var balances = await balancesQuery
            .Where(b => items.Select(i => i.Id).Contains(b.ItemId))
            .ToListAsync();

        var balancesByItem = balances.GroupBy(b => b.ItemId).ToDictionary(g => g.Key, g => g.ToList());

        return items.Select(item => MapItem(item, balancesByItem.GetValueOrDefault(item.Id) ?? new List<InventoryStockBalance>())).ToList();
    }

    public async Task<List<InventoryMovementDto>> GetMovementsAsync(int employeeId, int merchantId, int? branchId = null, int? itemId = null, DateOnly? from = null, DateOnly? to = null)
    {
        var scope = await ResolveScopeAsync(employeeId, merchantId, branchId);

        var query = _context.InventoryMovements
            .AsNoTracking()
            .Include(m => m.Branch)
            .Where(m => m.MerchantId == merchantId && scope.FilteredBranchIds.Contains(m.BranchId));

        if (itemId.HasValue)
            query = query.Where(m => m.ItemId == itemId.Value);

        if (from.HasValue)
        {
            var fromDate = from.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(m => m.CreatedAt >= fromDate);
        }

        if (to.HasValue)
        {
            var toDate = to.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(m => m.CreatedAt <= toDate);
        }

        var movements = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(200)
            .ToListAsync();

        return movements.Select(MapMovement).ToList();
    }

    public async Task<List<LowStockReportRowDto>> GetLowStockAsync(int employeeId, int merchantId, int? branchId = null)
    {
        var scope = await ResolveScopeAsync(employeeId, merchantId, branchId);

        var query = _context.InventoryStockBalances
            .AsNoTracking()
            .Include(b => b.Branch)
            .Include(b => b.Item)
            .Where(b =>
                b.MerchantId == merchantId &&
                scope.FilteredBranchIds.Contains(b.BranchId) &&
                b.Item.ReorderPoint > 0 &&
                b.QuantityOnHand <= b.Item.ReorderPoint);

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
            SuggestedReorderQuantity = balance.Item.ReorderPoint - balance.QuantityOnHand,
        }).ToList();
    }

    private async Task<EmployeeInventoryScope> ResolveScopeAsync(int employeeId, int merchantId, int? requestedBranchId)
    {
        var membership = await _context.EmployeeMemberships
            .AsNoTracking()
            .Include(m => m.BranchAccess)
            .FirstOrDefaultAsync(m => m.EmployeeId == employeeId && m.MerchantId == merchantId && m.IsActive);

        if (membership == null)
            throw new InvalidOperationException("Membership dipendente non trovato.");

        var accessibleBranchIds = membership.BranchAccess
            .Select(a => a.BranchId)
            .Append(membership.HomeBranchId)
            .Distinct()
            .ToHashSet();

        if (requestedBranchId.HasValue && !accessibleBranchIds.Contains(requestedBranchId.Value))
            throw new InvalidOperationException("Filiale non accessibile per il dipendente.");

        var branches = await _context.MerchantBranches
            .AsNoTracking()
            .Where(b => b.MerchantId == merchantId && accessibleBranchIds.Contains(b.Id))
            .OrderByDescending(b => b.Id == membership.HomeBranchId)
            .ThenBy(b => b.Name)
            .Select(b => new InventoryBranchOptionDto
            {
                Id = b.Id,
                Name = b.Name,
                IsHomeBranch = b.Id == membership.HomeBranchId,
                IsActive = b.IsActive,
            })
            .ToListAsync();

        int? selectedBranchId = requestedBranchId ?? membership.HomeBranchId;
        var filteredBranchIds = selectedBranchId.HasValue
            ? new HashSet<int> { selectedBranchId.Value }
            : accessibleBranchIds;

        return new EmployeeInventoryScope(
            membership.HomeBranchId,
            selectedBranchId,
            filteredBranchIds,
            branches);
    }

    private static InventoryItemDto MapItem(InventoryItem item, List<InventoryStockBalance> balances)
    {
        var totalQuantity = balances.Sum(b => b.QuantityOnHand);
        var totalValue = balances.Sum(b => b.InventoryValue);

        return new InventoryItemDto
        {
            Id = item.Id,
            MerchantId = item.MerchantId,
            Sku = item.Sku,
            Name = item.Name,
            Barcode = item.Barcode,
            Description = item.Description,
            UnitOfMeasure = item.UnitOfMeasure,
            ReorderPoint = item.ReorderPoint,
            AverageUnitCost = item.AverageUnitCost,
            ValuationMethod = item.ValuationMethod,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            TotalQuantityOnHand = totalQuantity,
            TotalInventoryValue = totalValue,
            Balances = balances.OrderBy(b => b.Branch.Name).Select(balance => new InventoryStockBalanceDto
            {
                Id = balance.Id,
                BranchId = balance.BranchId,
                BranchName = balance.Branch.Name,
                ItemId = balance.ItemId,
                QuantityOnHand = balance.QuantityOnHand,
                WeightedAverageCost = balance.WeightedAverageCost,
                InventoryValue = balance.InventoryValue,
                UpdatedAt = balance.UpdatedAt,
            }).ToList(),
            RecentMovements = new List<InventoryMovementDto>(),
        };
    }

    private static InventoryMovementDto MapMovement(InventoryMovement movement) => new()
    {
        Id = movement.Id,
        BranchId = movement.BranchId,
        BranchName = movement.Branch.Name,
        ItemId = movement.ItemId,
        Type = movement.Type,
        QuantityDelta = movement.QuantityDelta,
        UnitCost = movement.UnitCost,
        TotalValue = movement.TotalValue,
        Reason = movement.Reason,
        ReferenceNumber = movement.ReferenceNumber,
        CreatedAt = movement.CreatedAt,
    };

    private sealed record EmployeeInventoryScope(
        int HomeBranchId,
        int? SelectedBranchId,
        HashSet<int> FilteredBranchIds,
        List<InventoryBranchOptionDto> Branches);
}