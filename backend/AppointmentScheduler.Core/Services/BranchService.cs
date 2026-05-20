using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Gestione di filiali e reparti. Le validazioni di integrità (non eliminare
/// l'unica filiale, non eliminare filiali con turni, ecc.) lanciano
/// InvalidOperationException — il controller la traduce in 400.
/// </summary>
public class BranchService : IBranchService
{
    private readonly ApplicationDbContext _context;

    public BranchService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Filiali ────────────────────────────────────────────────────────────

    public async Task<List<MerchantBranchDto>> GetBranchesAsync(int merchantId, bool includeInactive = true)
    {
        var query = _context.MerchantBranches
            .Include(b => b.Departments)
            .Where(b => b.MerchantId == merchantId);

        if (!includeInactive)
            query = query.Where(b => b.IsActive);

        var branches = await query
            .OrderByDescending(b => b.IsHeadquarters)
            .ThenBy(b => b.Name)
            .ToListAsync();

        // Conteggio dipendenti per filiale (HomeBranch).
        var counts = await _context.EmployeeMemberships
            .Where(m => m.MerchantId == merchantId && m.IsActive)
            .GroupBy(m => m.HomeBranchId)
            .Select(g => new { BranchId = g.Key, Count = g.Count() })
            .ToListAsync();
        var countMap = counts.ToDictionary(c => c.BranchId, c => c.Count);

        return branches.Select(b => MapBranch(b, countMap.GetValueOrDefault(b.Id))).ToList();
    }

    public async Task<MerchantBranchDto?> GetBranchByIdAsync(int branchId, int merchantId)
    {
        var branch = await _context.MerchantBranches
            .Include(b => b.Departments)
            .FirstOrDefaultAsync(b => b.Id == branchId && b.MerchantId == merchantId);

        if (branch == null)
            return null;

        var count = await _context.EmployeeMemberships
            .CountAsync(m => m.MerchantId == merchantId && m.IsActive && m.HomeBranchId == branchId);

        return MapBranch(branch, count);
    }

    public async Task<MerchantBranchDto> CreateBranchAsync(int merchantId, CreateBranchRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Il nome della filiale è obbligatorio.");

        var exists = await _context.MerchantBranches
            .AnyAsync(b => b.MerchantId == merchantId && b.Name == name);
        if (exists)
            throw new InvalidOperationException($"Esiste già una filiale con nome '{name}'.");

        // La prima filiale in assoluto diventa HQ.
        var isFirst = !await _context.MerchantBranches.AnyAsync(b => b.MerchantId == merchantId);

        var branch = new MerchantBranch
        {
            MerchantId = merchantId,
            Name = name,
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
            Address = request.Address,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country,
            Phone = request.Phone,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsHeadquarters = isFirst,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.MerchantBranches.Add(branch);
        await _context.SaveChangesAsync();

        return MapBranch(branch, 0);
    }

    public async Task<MerchantBranchDto?> UpdateBranchAsync(int branchId, int merchantId, UpdateBranchRequest request)
    {
        var branch = await _context.MerchantBranches
            .Include(b => b.Departments)
            .FirstOrDefaultAsync(b => b.Id == branchId && b.MerchantId == merchantId);

        if (branch == null)
            return null;

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Il nome della filiale è obbligatorio.");

        if (!string.Equals(branch.Name, name, StringComparison.Ordinal))
        {
            var conflict = await _context.MerchantBranches
                .AnyAsync(b => b.MerchantId == merchantId && b.Id != branchId && b.Name == name);
            if (conflict)
                throw new InvalidOperationException($"Esiste già una filiale con nome '{name}'.");
        }

        // Non si può disattivare la HQ né l'unica filiale attiva rimasta.
        if (!request.IsActive && branch.IsActive)
        {
            if (branch.IsHeadquarters)
                throw new InvalidOperationException("Non puoi disattivare la sede principale. Promuovi prima un'altra filiale.");

            var otherActive = await _context.MerchantBranches
                .CountAsync(b => b.MerchantId == merchantId && b.Id != branchId && b.IsActive);
            if (otherActive == 0)
                throw new InvalidOperationException("Non puoi disattivare l'unica filiale attiva.");
        }

        branch.Name = name;
        branch.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        branch.Address = request.Address;
        branch.City = request.City;
        branch.PostalCode = request.PostalCode;
        branch.Country = request.Country;
        branch.Phone = request.Phone;
        branch.Latitude = request.Latitude;
        branch.Longitude = request.Longitude;
        branch.IsActive = request.IsActive;
        branch.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var count = await _context.EmployeeMemberships
            .CountAsync(m => m.MerchantId == merchantId && m.IsActive && m.HomeBranchId == branchId);

        return MapBranch(branch, count);
    }

    public async Task<bool> DeleteBranchAsync(int branchId, int merchantId)
    {
        var branch = await _context.MerchantBranches
            .FirstOrDefaultAsync(b => b.Id == branchId && b.MerchantId == merchantId);

        if (branch == null)
            return false;

        if (branch.IsHeadquarters)
            throw new InvalidOperationException("Non puoi eliminare la sede principale. Promuovi prima un'altra filiale a sede principale.");

        var totalBranches = await _context.MerchantBranches.CountAsync(b => b.MerchantId == merchantId);
        if (totalBranches <= 1)
            throw new InvalidOperationException("Un merchant deve avere almeno una filiale.");

        // Filiale con turni: non eliminabile (FK Restrict). Indirizza a disattivazione.
        var hasEvents = await _context.Events.AnyAsync(e => e.BranchId == branchId);
        if (hasEvents)
            throw new InvalidOperationException("La filiale ha turni associati: disattivala invece di eliminarla.");

        // Filiale usata come sede primaria di qualche dipendente: non eliminabile.
        var hasEmployees = await _context.EmployeeMemberships.AnyAsync(m => m.HomeBranchId == branchId);
        if (hasEmployees)
            throw new InvalidOperationException("La filiale è sede primaria di alcuni dipendenti: riassegnali prima di eliminarla.");

        _context.MerchantBranches.Remove(branch);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetHeadquartersAsync(int branchId, int merchantId)
    {
        var branch = await _context.MerchantBranches
            .FirstOrDefaultAsync(b => b.Id == branchId && b.MerchantId == merchantId);

        if (branch == null)
            return false;

        if (!branch.IsActive)
            throw new InvalidOperationException("Una filiale disattivata non può essere la sede principale.");

        if (branch.IsHeadquarters)
            return true;

        var currentHq = await _context.MerchantBranches
            .Where(b => b.MerchantId == merchantId && b.IsHeadquarters)
            .ToListAsync();
        foreach (var hq in currentHq)
            hq.IsHeadquarters = false;

        branch.IsHeadquarters = true;
        branch.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    // ── Reparti ────────────────────────────────────────────────────────────

    public async Task<DepartmentDto> CreateDepartmentAsync(int branchId, int merchantId, CreateDepartmentRequest request)
    {
        var branch = await _context.MerchantBranches
            .FirstOrDefaultAsync(b => b.Id == branchId && b.MerchantId == merchantId);
        if (branch == null)
            throw new InvalidOperationException("Filiale non trovata.");

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Il nome del reparto è obbligatorio.");

        var exists = await _context.Departments
            .AnyAsync(d => d.BranchId == branchId && d.Name == name);
        if (exists)
            throw new InvalidOperationException($"Esiste già un reparto con nome '{name}' in questa filiale.");

        var department = new Department
        {
            BranchId = branchId,
            MerchantId = merchantId,
            Name = name,
            Color = string.IsNullOrWhiteSpace(request.Color) ? "#3b82f6" : request.Color,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        return MapDepartment(department);
    }

    public async Task<DepartmentDto?> UpdateDepartmentAsync(int departmentId, int merchantId, UpdateDepartmentRequest request)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == departmentId && d.MerchantId == merchantId);

        if (department == null)
            return null;

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Il nome del reparto è obbligatorio.");

        if (!string.Equals(department.Name, name, StringComparison.Ordinal))
        {
            var conflict = await _context.Departments
                .AnyAsync(d => d.BranchId == department.BranchId && d.Id != departmentId && d.Name == name);
            if (conflict)
                throw new InvalidOperationException($"Esiste già un reparto con nome '{name}' in questa filiale.");
        }

        department.Name = name;
        department.Color = string.IsNullOrWhiteSpace(request.Color) ? department.Color : request.Color;
        department.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return MapDepartment(department);
    }

    public async Task<bool> DeleteDepartmentAsync(int departmentId, int merchantId)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == departmentId && d.MerchantId == merchantId);

        if (department == null)
            return false;

        // Le FK verso Department sono SetNull: turni e dipendenti restano,
        // tornano semplicemente "senza reparto" (Jolly). Hard delete sicuro.
        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();
        return true;
    }

    // ── Mapping ────────────────────────────────────────────────────────────

    private static MerchantBranchDto MapBranch(MerchantBranch b, int employeeCount) => new()
    {
        Id = b.Id,
        MerchantId = b.MerchantId,
        Name = b.Name,
        Code = b.Code,
        Address = b.Address,
        City = b.City,
        PostalCode = b.PostalCode,
        Country = b.Country,
        Phone = b.Phone,
        IsHeadquarters = b.IsHeadquarters,
        IsActive = b.IsActive,
        CreatedAt = b.CreatedAt,
        Latitude = b.Latitude,
        Longitude = b.Longitude,
        EmployeeCount = employeeCount,
        Departments = b.Departments
            .OrderBy(d => d.Name)
            .Select(MapDepartment)
            .ToList()
    };

    private static DepartmentDto MapDepartment(Department d) => new()
    {
        Id = d.Id,
        BranchId = d.BranchId,
        MerchantId = d.MerchantId,
        Name = d.Name,
        Color = d.Color,
        IsActive = d.IsActive,
        CreatedAt = d.CreatedAt
    };
}
