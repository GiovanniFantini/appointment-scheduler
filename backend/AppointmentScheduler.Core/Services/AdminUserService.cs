using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Implementazione del servizio sys-admin di gestione utenti.
/// </summary>
public class AdminUserService : IAdminUserService
{
    private readonly IApplicationDbContext _context;
    private readonly IUtcClock _clock;

    public AdminUserService(IApplicationDbContext context, IUtcClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<AdminUserListResponse> GetUsersAsync(AdminUserListQuery query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 25 : Math.Min(query.PageSize, 200);

        IQueryable<Shared.Models.User> users = _context.Users;

        if (query.AccountType.HasValue)
            users = users.Where(u => u.AccountType == query.AccountType.Value);

        if (query.IsActive.HasValue)
            users = users.Where(u => u.IsActive == query.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            users = users.Where(u =>
                u.Email.ToLower().Contains(search) ||
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search));
        }

        if (query.MerchantId.HasValue)
        {
            var merchantId = query.MerchantId.Value;
            users = users.Where(u =>
                (u.Merchant != null && u.Merchant.Id == merchantId) ||
                (u.Employee != null && u.Employee.Memberships.Any(m => m.MerchantId == merchantId)));
        }

        var total = await users.CountAsync();

        var items = await users
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserListItemDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                AccountType = u.AccountType,
                IsActive = u.IsActive,
                MerchantId = u.Merchant != null ? u.Merchant.Id : (int?)null,
                MerchantName = u.Merchant != null ? u.Merchant.CompanyName : null,
                EmployeeId = u.Employee != null ? u.Employee.Id : (int?)null,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return new AdminUserListResponse
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminUserDetailDto?> GetByIdAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.Merchant)
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return null;

        return new AdminUserDetailDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            AccountType = user.AccountType,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Merchant = user.Merchant == null ? null : new AdminMerchantLinkDto
            {
                Id = user.Merchant.Id,
                CompanyName = user.Merchant.CompanyName,
                IsApproved = user.Merchant.IsApproved,
                IsActive = user.Merchant.IsActive
            },
            Employee = user.Employee == null ? null : new AdminEmployeeLinkDto
            {
                Id = user.Employee.Id,
                Email = user.Employee.Email,
                Kind = user.Employee.Kind,
                IsActive = user.Employee.IsActive
            }
        };
    }

    public async Task<bool> ActivateAsync(int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return false;

        if (!user.IsActive)
        {
            user.IsActive = true;
            user.UpdatedAt = _clock.UtcNow;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<DeactivateResult> DeactivateAsync(int id, int currentAdminUserId)
    {
        if (id == currentAdminUserId)
            return DeactivateResult.CannotDeactivateSelf;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return DeactivateResult.NotFound;

        if (user.IsActive)
        {
            user.IsActive = false;
            user.UpdatedAt = _clock.UtcNow;
            await _context.SaveChangesAsync();
        }

        return DeactivateResult.Success;
    }
}
