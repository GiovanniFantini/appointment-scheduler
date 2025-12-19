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
    /// Recupera tutti i merchant in attesa di approvazione
    /// </summary>
    public async Task<IEnumerable<MerchantDto>> GetPendingMerchantsAsync()
    {
        var merchants = await _context.Merchants
            .Include(m => m.User)
            .Where(m => !m.IsApproved)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return merchants.Select(m => new MerchantDto
        {
            Id = m.Id,
            UserId = m.UserId,
            BusinessName = m.BusinessName,
            Description = m.Description,
            Address = m.Address,
            City = m.City,
            VatNumber = m.VatNumber,
            IsApproved = m.IsApproved,
            CreatedAt = m.CreatedAt,
            ApprovedAt = m.ApprovedAt,
            User = new UserDto
            {
                Id = m.User.Id,
                Email = m.User.Email,
                FirstName = m.User.FirstName,
                LastName = m.User.LastName,
                PhoneNumber = m.User.PhoneNumber
            }
        });
    }

    /// <summary>
    /// Recupera tutti i merchant
    /// </summary>
    public async Task<IEnumerable<MerchantDto>> GetAllMerchantsAsync()
    {
        var merchants = await _context.Merchants
            .Include(m => m.User)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return merchants.Select(m => new MerchantDto
        {
            Id = m.Id,
            UserId = m.UserId,
            BusinessName = m.BusinessName,
            Description = m.Description,
            Address = m.Address,
            City = m.City,
            VatNumber = m.VatNumber,
            IsApproved = m.IsApproved,
            CreatedAt = m.CreatedAt,
            ApprovedAt = m.ApprovedAt,
            User = new UserDto
            {
                Id = m.User.Id,
                Email = m.User.Email,
                FirstName = m.User.FirstName,
                LastName = m.User.LastName,
                PhoneNumber = m.User.PhoneNumber
            }
        });
    }

    /// <summary>
    /// Recupera un merchant per ID
    /// </summary>
    public async Task<MerchantDto?> GetMerchantByIdAsync(int id)
    {
        var merchant = await _context.Merchants
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (merchant == null)
            return null;

        return new MerchantDto
        {
            Id = merchant.Id,
            UserId = merchant.UserId,
            BusinessName = merchant.BusinessName,
            Description = merchant.Description,
            Address = merchant.Address,
            City = merchant.City,
            VatNumber = merchant.VatNumber,
            IsApproved = merchant.IsApproved,
            CreatedAt = merchant.CreatedAt,
            ApprovedAt = merchant.ApprovedAt,
            User = new UserDto
            {
                Id = merchant.User.Id,
                Email = merchant.User.Email,
                FirstName = merchant.User.FirstName,
                LastName = merchant.User.LastName,
                PhoneNumber = merchant.User.PhoneNumber
            }
        };
    }

    /// <summary>
    /// Approva un merchant
    /// </summary>
    public async Task<bool> ApproveMerchantAsync(int merchantId)
    {
        var merchant = await _context.Merchants.FindAsync(merchantId);
        if (merchant == null)
            return false;

        merchant.IsApproved = true;
        merchant.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Rifiuta o disabilita un merchant
    /// </summary>
    public async Task<bool> RejectMerchantAsync(int merchantId)
    {
        var merchant = await _context.Merchants.FindAsync(merchantId);
        if (merchant == null)
            return false;

        merchant.IsApproved = false;
        merchant.ApprovedAt = null;

        await _context.SaveChangesAsync();
        return true;
    }
}
