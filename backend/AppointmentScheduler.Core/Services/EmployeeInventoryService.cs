using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Core.Services;

public class EmployeeInventoryService : IEmployeeInventoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly ISupplierService _supplierService;
    private readonly IPurchaseOrderService _purchaseOrderService;

    public EmployeeInventoryService(
        ApplicationDbContext context,
        IInventoryService inventoryService,
        ISupplierService supplierService,
        IPurchaseOrderService purchaseOrderService)
    {
        _context = context;
        _inventoryService = inventoryService;
        _supplierService = supplierService;
        _purchaseOrderService = purchaseOrderService;
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
                // Conta gli articoli attivi del merchant, coerente con la tab
                // Articoli: include anche gli articoli senza saldi di filiale.
                TotalItems = await _context.InventoryItems
                    .CountAsync(i => i.MerchantId == merchantId && i.IsActive),
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

        // La lista parte dagli articoli del merchant, NON dai saldi: un articolo
        // appena creato non ha ancora alcun saldo di filiale e deve comunque
        // comparire nel catalogo (con quantità a zero).
        var itemsQuery = _context.InventoryItems
            .AsNoTracking()
            .Where(i => i.MerchantId == merchantId && i.IsActive);

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

        // I saldi mostrati restano limitati alle filiali accessibili al dipendente.
        var itemIds = items.Select(i => i.Id).ToList();
        var balances = await _context.InventoryStockBalances
            .AsNoTracking()
            .Include(b => b.Branch)
            .Where(b => b.MerchantId == merchantId
                && scope.FilteredBranchIds.Contains(b.BranchId)
                && itemIds.Contains(b.ItemId))
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

    // ── Letture aggiuntive ──────────────────────────────────────────────────

    public async Task<List<SupplierDto>> GetSuppliersAsync(int employeeId, int merchantId, bool includeInactive = true)
    {
        await EnsureMembershipAsync(employeeId, merchantId);
        return await _supplierService.GetSuppliersAsync(merchantId, includeInactive);
    }

    public async Task<List<PurchaseOrderDto>> GetPurchaseOrdersAsync(int employeeId, int merchantId, int? branchId = null, PurchaseOrderStatus? status = null)
    {
        var accessibleBranchIds = await ResolveAccessibleBranchIdsAsync(employeeId, merchantId);

        if (branchId.HasValue)
        {
            EnsureBranchAccessible(branchId.Value, accessibleBranchIds);
            return await _purchaseOrderService.GetOrdersAsync(merchantId, branchId.Value, status);
        }

        // Senza filtro esplicito: restituisce solo gli ordini delle filiali accessibili.
        var orders = await _purchaseOrderService.GetOrdersAsync(merchantId, null, status);
        return orders.Where(o => accessibleBranchIds.Contains(o.BranchId)).ToList();
    }

    public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(int employeeId, int merchantId, int orderId)
    {
        var accessibleBranchIds = await ResolveAccessibleBranchIdsAsync(employeeId, merchantId);
        var order = await _purchaseOrderService.GetOrderByIdAsync(orderId, merchantId);
        if (order == null)
            return null;
        EnsureBranchAccessible(order.BranchId, accessibleBranchIds);
        return order;
    }

    // ── Operatività quotidiana (Operator) ───────────────────────────────────

    public async Task<InventoryMovementDto> CreateAdjustmentAsync(int employeeId, int merchantId, int userId, CreateInventoryAdjustmentRequest request)
    {
        var accessibleBranchIds = await ResolveAccessibleBranchIdsAsync(employeeId, merchantId);
        EnsureBranchAccessible(request.BranchId, accessibleBranchIds);
        return await _inventoryService.CreateAdjustmentAsync(merchantId, userId, request);
    }

    public async Task<PurchaseOrderDto?> ReceiveOrderAsync(int employeeId, int merchantId, int userId, int orderId, CreateGoodsReceiptRequest request)
    {
        var accessibleBranchIds = await ResolveAccessibleBranchIdsAsync(employeeId, merchantId);
        var order = await _purchaseOrderService.GetOrderByIdAsync(orderId, merchantId);
        if (order == null)
            return null;
        EnsureBranchAccessible(order.BranchId, accessibleBranchIds);
        return await _purchaseOrderService.ReceiveOrderAsync(orderId, merchantId, userId, request);
    }

    // ── Anagrafiche e gestione ordini (Manager) ─────────────────────────────

    public async Task<InventoryItemDto> CreateItemAsync(int employeeId, int merchantId, CreateInventoryItemRequest request)
    {
        await EnsureMembershipAsync(employeeId, merchantId);
        return await _inventoryService.CreateItemAsync(merchantId, request);
    }

    public async Task<InventoryItemDto?> UpdateItemAsync(int employeeId, int merchantId, int itemId, UpdateInventoryItemRequest request)
    {
        await EnsureMembershipAsync(employeeId, merchantId);
        return await _inventoryService.UpdateItemAsync(itemId, merchantId, request);
    }

    public async Task<SupplierDto> CreateSupplierAsync(int employeeId, int merchantId, CreateSupplierRequest request)
    {
        await EnsureMembershipAsync(employeeId, merchantId);
        return await _supplierService.CreateSupplierAsync(merchantId, request);
    }

    public async Task<SupplierDto?> UpdateSupplierAsync(int employeeId, int merchantId, int supplierId, UpdateSupplierRequest request)
    {
        await EnsureMembershipAsync(employeeId, merchantId);
        return await _supplierService.UpdateSupplierAsync(supplierId, merchantId, request);
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(int employeeId, int merchantId, int userId, CreatePurchaseOrderRequest request)
    {
        var accessibleBranchIds = await ResolveAccessibleBranchIdsAsync(employeeId, merchantId);
        EnsureBranchAccessible(request.BranchId, accessibleBranchIds);
        return await _purchaseOrderService.CreateOrderAsync(merchantId, userId, request);
    }

    public async Task<PurchaseOrderDto?> MarkPurchaseOrderSentAsync(int employeeId, int merchantId, int orderId)
    {
        var accessibleBranchIds = await ResolveAccessibleBranchIdsAsync(employeeId, merchantId);
        var order = await _purchaseOrderService.GetOrderByIdAsync(orderId, merchantId);
        if (order == null)
            return null;
        EnsureBranchAccessible(order.BranchId, accessibleBranchIds);
        return await _purchaseOrderService.MarkAsSentAsync(orderId, merchantId);
    }

    public async Task<PurchaseOrderDto?> CancelPurchaseOrderAsync(int employeeId, int merchantId, int orderId)
    {
        var accessibleBranchIds = await ResolveAccessibleBranchIdsAsync(employeeId, merchantId);
        var order = await _purchaseOrderService.GetOrderByIdAsync(orderId, merchantId);
        if (order == null)
            return null;
        EnsureBranchAccessible(order.BranchId, accessibleBranchIds);
        return await _purchaseOrderService.CancelOrderAsync(orderId, merchantId);
    }

    // ── Validazione perimetro ───────────────────────────────────────────────

    /// <summary>
    /// Verifica che il dipendente abbia una membership attiva sul merchant.
    /// Usato dalle operazioni non legate a una filiale (articoli, fornitori).
    /// </summary>
    private async Task EnsureMembershipAsync(int employeeId, int merchantId)
    {
        var exists = await _context.EmployeeMemberships
            .AnyAsync(m => m.EmployeeId == employeeId && m.MerchantId == merchantId && m.IsActive);
        if (!exists)
            throw new InvalidOperationException("Membership dipendente non trovato.");
    }

    /// <summary>
    /// Restituisce gli ID delle filiali su cui il dipendente può operare
    /// (filiale principale + accessi aggiuntivi).
    /// </summary>
    private async Task<HashSet<int>> ResolveAccessibleBranchIdsAsync(int employeeId, int merchantId)
    {
        var membership = await _context.EmployeeMemberships
            .AsNoTracking()
            .Include(m => m.BranchAccess)
            .FirstOrDefaultAsync(m => m.EmployeeId == employeeId && m.MerchantId == merchantId && m.IsActive);

        if (membership == null)
            throw new InvalidOperationException("Membership dipendente non trovato.");

        return membership.BranchAccess
            .Select(a => a.BranchId)
            .Append(membership.HomeBranchId)
            .Distinct()
            .ToHashSet();
    }

    private static void EnsureBranchAccessible(int branchId, HashSet<int> accessibleBranchIds)
    {
        if (!accessibleBranchIds.Contains(branchId))
            throw new InvalidOperationException("Filiale non accessibile per il dipendente.");
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