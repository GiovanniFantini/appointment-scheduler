using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio per la gestione dei merchant
/// </summary>
public class MerchantService : IMerchantService
{
    private readonly ApplicationDbContext _context;

    public MerchantService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Recupera tutti i merchant con dati dell'owner e conteggio dipendenti
    /// </summary>
    public async Task<List<MerchantDto>> GetAllAsync()
    {
        var merchants = await _context.Merchants
            .Include(m => m.User)
            .Include(m => m.EmployeeMemberships)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return merchants.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Recupera i merchant in attesa di approvazione
    /// </summary>
    public async Task<List<MerchantDto>> GetPendingAsync()
    {
        var merchants = await _context.Merchants
            .Include(m => m.User)
            .Include(m => m.EmployeeMemberships)
            .Where(m => !m.IsApproved)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return merchants.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Recupera un merchant per ID
    /// </summary>
    public async Task<MerchantDto?> GetByIdAsync(int id)
    {
        var merchant = await _context.Merchants
            .Include(m => m.User)
            .Include(m => m.EmployeeMemberships)
            .FirstOrDefaultAsync(m => m.Id == id);

        return merchant == null ? null : MapToDto(merchant);
    }

    /// <summary>
    /// Approva un merchant
    /// </summary>
    public async Task<bool> ApproveAsync(int id)
    {
        var merchant = await _context.Merchants.FindAsync(id);
        if (merchant == null)
            return false;

        merchant.IsApproved = true;
        merchant.IsActive = true;
        merchant.ApprovedAt = DateTime.UtcNow;
        merchant.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Rifiuta o disabilita un merchant
    /// </summary>
    public async Task<bool> RejectAsync(int id)
    {
        var merchant = await _context.Merchants.FindAsync(id);
        if (merchant == null)
            return false;

        merchant.IsApproved = false;
        merchant.IsActive = false;
        merchant.ApprovedAt = null;
        merchant.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Aggiorna i dati di un merchant
    /// </summary>
    public async Task<MerchantDto?> UpdateAsync(int id, UpdateMerchantRequest request)
    {
        var merchant = await _context.Merchants
            .Include(m => m.User)
            .Include(m => m.EmployeeMemberships)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (merchant == null)
            return null;

        merchant.CompanyName = request.CompanyName;
        merchant.VatNumber = request.VatNumber;
        merchant.Address = request.Address;
        merchant.City = request.City;
        merchant.PostalCode = request.PostalCode;
        merchant.Country = request.Country;
        merchant.Phone = request.Phone;
        merchant.BusinessEmail = request.BusinessEmail;
        merchant.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(merchant);
    }

    private static MerchantDto MapToDto(Shared.Models.Merchant m)
    {
        return new MerchantDto
        {
            Id = m.Id,
            UserId = m.UserId,
            CompanyName = m.CompanyName,
            VatNumber = m.VatNumber,
            Address = m.Address,
            City = m.City,
            PostalCode = m.PostalCode,
            Country = m.Country,
            Phone = m.Phone,
            BusinessEmail = m.BusinessEmail,
            IsApproved = m.IsApproved,
            IsActive = m.IsActive,
            CreatedAt = m.CreatedAt,
            ApprovedAt = m.ApprovedAt,
            Owner = m.User != null ? new UserDto
            {
                Id = m.User.Id,
                Email = m.User.Email,
                FirstName = m.User.FirstName,
                LastName = m.User.LastName,
                PhoneNumber = m.User.PhoneNumber
            } : null,
            EmployeeCount = m.EmployeeMemberships.Count(em => em.IsActive)
        };
    }
}
