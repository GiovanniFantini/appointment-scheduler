using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Core.Services;

public class SupplierService : ISupplierService
{
    private readonly ApplicationDbContext _context;

    public SupplierService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SupplierDto>> GetSuppliersAsync(int merchantId, bool includeInactive = true)
    {
        var query = _context.Suppliers
            .AsNoTracking()
            .Where(s => s.MerchantId == merchantId);

        if (!includeInactive)
            query = query.Where(s => s.IsActive);

        var suppliers = await query.OrderBy(s => s.Name).ToListAsync();
        return suppliers.Select(MapSupplier).ToList();
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(int supplierId, int merchantId)
    {
        var supplier = await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId && s.MerchantId == merchantId);

        return supplier == null ? null : MapSupplier(supplier);
    }

    public async Task<SupplierDto> CreateSupplierAsync(int merchantId, CreateSupplierRequest request)
    {
        var name = NormalizeRequired(request.Name, "Il nome fornitore è obbligatorio.");

        var exists = await _context.Suppliers
            .AnyAsync(s => s.MerchantId == merchantId && s.Name == name);
        if (exists)
            throw new InvalidOperationException($"Esiste già un fornitore con nome '{name}'.");

        var supplier = new Supplier
        {
            MerchantId = merchantId,
            Name = name,
            ContactName = NormalizeOptional(request.ContactName),
            Email = NormalizeOptional(request.Email),
            Phone = NormalizeOptional(request.Phone),
            VatNumber = NormalizeOptional(request.VatNumber),
            Notes = NormalizeOptional(request.Notes),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return MapSupplier(supplier);
    }

    public async Task<SupplierDto?> UpdateSupplierAsync(int supplierId, int merchantId, UpdateSupplierRequest request)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == supplierId && s.MerchantId == merchantId);
        if (supplier == null)
            return null;

        var name = NormalizeRequired(request.Name, "Il nome fornitore è obbligatorio.");

        if (!string.Equals(supplier.Name, name, StringComparison.Ordinal))
        {
            var conflict = await _context.Suppliers
                .AnyAsync(s => s.MerchantId == merchantId && s.Id != supplierId && s.Name == name);
            if (conflict)
                throw new InvalidOperationException($"Esiste già un fornitore con nome '{name}'.");
        }

        supplier.Name = name;
        supplier.ContactName = NormalizeOptional(request.ContactName);
        supplier.Email = NormalizeOptional(request.Email);
        supplier.Phone = NormalizeOptional(request.Phone);
        supplier.VatNumber = NormalizeOptional(request.VatNumber);
        supplier.Notes = NormalizeOptional(request.Notes);
        supplier.IsActive = request.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapSupplier(supplier);
    }

    private static SupplierDto MapSupplier(Supplier supplier) => new()
    {
        Id = supplier.Id,
        MerchantId = supplier.MerchantId,
        Name = supplier.Name,
        ContactName = supplier.ContactName,
        Email = supplier.Email,
        Phone = supplier.Phone,
        VatNumber = supplier.VatNumber,
        Notes = supplier.Notes,
        IsActive = supplier.IsActive,
        CreatedAt = supplier.CreatedAt,
        UpdatedAt = supplier.UpdatedAt
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
}