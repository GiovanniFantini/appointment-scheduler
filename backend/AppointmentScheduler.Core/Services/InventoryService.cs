using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Core.Services;

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _context;

    public InventoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<InventoryItemDto>> GetItemsAsync(int merchantId, int? branchId = null, string? search = null, bool includeInactive = true)
    {
        var query = _context.InventoryItems
            .AsNoTracking()
            .Where(i => i.MerchantId == merchantId);

        if (!includeInactive)
            query = query.Where(i => i.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i =>
                i.Sku.Contains(term) ||
                i.Name.Contains(term) ||
                (i.Barcode != null && i.Barcode.Contains(term)));
        }

        var items = await query
            .OrderBy(i => i.Name)
            .ToListAsync();

        var itemIds = items.Select(i => i.Id).ToList();
        var balancesQuery = _context.InventoryStockBalances
            .AsNoTracking()
            .Include(b => b.Branch)
            .Where(b => itemIds.Contains(b.ItemId));

        if (branchId.HasValue)
            balancesQuery = balancesQuery.Where(b => b.BranchId == branchId.Value);

        var balances = await balancesQuery.ToListAsync();
        var balancesByItem = balances.GroupBy(b => b.ItemId).ToDictionary(g => g.Key, g => g.ToList());

        return items.Select(item => MapItem(item, balancesByItem.GetValueOrDefault(item.Id) ?? new List<InventoryStockBalance>(), new List<InventoryMovement>())).ToList();
    }

    public async Task<InventoryItemDto?> GetItemByIdAsync(int itemId, int merchantId, int? branchId = null)
    {
        var item = await _context.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == itemId && i.MerchantId == merchantId);

        if (item == null)
            return null;

        var balancesQuery = _context.InventoryStockBalances
            .AsNoTracking()
            .Include(b => b.Branch)
            .Where(b => b.ItemId == itemId && b.MerchantId == merchantId);

        if (branchId.HasValue)
            balancesQuery = balancesQuery.Where(b => b.BranchId == branchId.Value);

        var movementsQuery = _context.InventoryMovements
            .AsNoTracking()
            .Include(m => m.Branch)
            .Where(m => m.ItemId == itemId && m.MerchantId == merchantId);

        if (branchId.HasValue)
            movementsQuery = movementsQuery.Where(m => m.BranchId == branchId.Value);

        var balances = await balancesQuery.ToListAsync();
        var movements = await movementsQuery
            .OrderByDescending(m => m.CreatedAt)
            .Take(30)
            .ToListAsync();

        return MapItem(item, balances, movements);
    }

    public async Task<InventoryItemDto> CreateItemAsync(int merchantId, CreateInventoryItemRequest request)
    {
        var sku = NormalizeRequired(request.Sku, "Lo SKU è obbligatorio.");
        var name = NormalizeRequired(request.Name, "Il nome articolo è obbligatorio.");

        var exists = await _context.InventoryItems
            .AnyAsync(i => i.MerchantId == merchantId && i.Sku == sku);
        if (exists)
            throw new InvalidOperationException($"Esiste già un articolo con SKU '{sku}'.");

        var item = new InventoryItem
        {
            MerchantId = merchantId,
            Sku = sku,
            Name = name,
            Barcode = NormalizeOptional(request.Barcode),
            Description = NormalizeOptional(request.Description),
            UnitOfMeasure = NormalizeUnitOfMeasure(request.UnitOfMeasure),
            ReorderPoint = EnsureNonNegative(request.ReorderPoint, "La soglia di riordino non può essere negativa."),
            AverageUnitCost = 0,
            ValuationMethod = InventoryValuationMethod.WeightedAverage,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync();

        return MapItem(item, new List<InventoryStockBalance>(), new List<InventoryMovement>());
    }

    public async Task<InventoryItemDto?> UpdateItemAsync(int itemId, int merchantId, UpdateInventoryItemRequest request)
    {
        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.MerchantId == merchantId);

        if (item == null)
            return null;

        var sku = NormalizeRequired(request.Sku, "Lo SKU è obbligatorio.");
        var name = NormalizeRequired(request.Name, "Il nome articolo è obbligatorio.");

        if (!string.Equals(item.Sku, sku, StringComparison.Ordinal))
        {
            var conflict = await _context.InventoryItems
                .AnyAsync(i => i.MerchantId == merchantId && i.Id != itemId && i.Sku == sku);
            if (conflict)
                throw new InvalidOperationException($"Esiste già un articolo con SKU '{sku}'.");
        }

        item.Sku = sku;
        item.Name = name;
        item.Barcode = NormalizeOptional(request.Barcode);
        item.Description = NormalizeOptional(request.Description);
        item.UnitOfMeasure = NormalizeUnitOfMeasure(request.UnitOfMeasure);
        item.ReorderPoint = EnsureNonNegative(request.ReorderPoint, "La soglia di riordino non può essere negativa.");
        item.IsActive = request.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetItemByIdAsync(itemId, merchantId);
    }

    public async Task<List<InventoryMovementDto>> GetMovementsAsync(int merchantId, int? branchId = null, int? itemId = null, DateOnly? from = null, DateOnly? to = null)
    {
        var query = _context.InventoryMovements
            .AsNoTracking()
            .Include(m => m.Branch)
            .Where(m => m.MerchantId == merchantId);

        if (branchId.HasValue)
            query = query.Where(m => m.BranchId == branchId.Value);

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

    public async Task<InventoryMovementDto> CreateAdjustmentAsync(int merchantId, int userId, CreateInventoryAdjustmentRequest request)
    {
        if (request.QuantityDelta == 0)
            throw new InvalidOperationException("La quantità di rettifica deve essere diversa da zero.");

        var reason = NormalizeRequired(request.Reason, "Il motivo della rettifica è obbligatorio.");

        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.MerchantId == merchantId);
        if (item == null)
            throw new InvalidOperationException("Articolo non trovato.");

        var branchExists = await _context.MerchantBranches
            .AnyAsync(b => b.Id == request.BranchId && b.MerchantId == merchantId && b.IsActive);
        if (!branchExists)
            throw new InvalidOperationException("Filiale non trovata o non attiva.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var balance = await GetOrCreateBalanceAsync(merchantId, request.BranchId, item.Id);
        var oldQty = balance.QuantityOnHand;
        var oldCost = balance.WeightedAverageCost;
        var delta = request.QuantityDelta;

        if (oldQty + delta < 0)
            throw new InvalidOperationException("La rettifica porterebbe lo stock sotto zero.");

        decimal unitCost;
        decimal newWeightedAverage;
        if (delta > 0)
        {
            unitCost = EnsureNonNegative(request.UnitCost ?? balance.WeightedAverageCost, "Il costo unitario non può essere negativo.");
            var newQty = oldQty + delta;
            // newQty è sempre > 0 qui (oldQty >= 0 e delta > 0): nessun ramo a zero.
            newWeightedAverage = ((oldQty * oldCost) + (delta * unitCost)) / newQty;
        }
        else
        {
            // Una rettifica negativa non altera il costo medio: lo scarico avviene
            // al costo medio corrente. Il costo storico va conservato anche quando
            // la giacenza scende a zero, così un futuro carico senza costo esplicito
            // riparte dall'ultimo valore noto invece che da zero.
            unitCost = balance.WeightedAverageCost > 0 ? balance.WeightedAverageCost : item.AverageUnitCost;
            newWeightedAverage = oldCost;
        }

        balance.QuantityOnHand = oldQty + delta;
        balance.WeightedAverageCost = newWeightedAverage;
        balance.InventoryValue = balance.QuantityOnHand * balance.WeightedAverageCost;
        balance.UpdatedAt = DateTime.UtcNow;

        var movement = new InventoryMovement
        {
            MerchantId = merchantId,
            BranchId = request.BranchId,
            ItemId = item.Id,
            Type = delta > 0 ? InventoryMovementType.ManualAdjustmentIncrease : InventoryMovementType.ManualAdjustmentDecrease,
            QuantityDelta = delta,
            UnitCost = unitCost,
            TotalValue = delta * unitCost,
            Reason = reason,
            PerformedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.InventoryMovements.Add(movement);
        await _context.SaveChangesAsync();

        await RecalculateAverageUnitCostAsync(item.Id);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        await _context.Entry(movement).Reference(m => m.Branch).LoadAsync();
        return MapMovement(movement);
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
            .Select(g => new
            {
                Quantity = g.Sum(x => x.QuantityOnHand),
                Value = g.Sum(x => x.InventoryValue)
            })
            .FirstOrDefaultAsync();

        item.AverageUnitCost = totals == null || totals.Quantity == 0 ? 0 : totals.Value / totals.Quantity;
        item.UpdatedAt = DateTime.UtcNow;
    }

    private static InventoryItemDto MapItem(InventoryItem item, List<InventoryStockBalance> balances, List<InventoryMovement> movements)
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
            Balances = balances.OrderBy(b => b.Branch.Name).Select(MapBalance).ToList(),
            RecentMovements = movements.Select(MapMovement).ToList()
        };
    }

    private static InventoryStockBalanceDto MapBalance(InventoryStockBalance balance) => new()
    {
        Id = balance.Id,
        BranchId = balance.BranchId,
        BranchName = balance.Branch.Name,
        ItemId = balance.ItemId,
        QuantityOnHand = balance.QuantityOnHand,
        WeightedAverageCost = balance.WeightedAverageCost,
        InventoryValue = balance.InventoryValue,
        UpdatedAt = balance.UpdatedAt
    };

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
        CreatedAt = movement.CreatedAt
    };

    private static string NormalizeRequired(string? value, string error)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException(error);

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeUnitOfMeasure(string? unitOfMeasure)
    {
        var normalized = unitOfMeasure?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "pz" : normalized;
    }

    private static decimal EnsureNonNegative(decimal value, string error)
    {
        if (value < 0)
            throw new InvalidOperationException(error);

        return value;
    }
}