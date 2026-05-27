using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Implementazione del servizio sys-admin di consultazione employee.
/// </summary>
public class AdminEmployeeService : IAdminEmployeeService
{
    private readonly IApplicationDbContext _context;

    public AdminEmployeeService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminEmployeeListResponse> GetEmployeesAsync(AdminEmployeeListQuery query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 25 : Math.Min(query.PageSize, 200);

        IQueryable<Employee> employees = _context.Employees;

        if (query.IsActive.HasValue)
            employees = employees.Where(e => e.IsActive == query.IsActive.Value);

        if (query.Kind.HasValue)
            employees = employees.Where(e => e.Kind == query.Kind.Value);

        if (query.HasAccount == true)
            employees = employees.Where(e => e.UserId != null);
        else if (query.HasAccount == false)
            employees = employees.Where(e => e.UserId == null);

        if (query.MerchantId.HasValue)
        {
            var merchantId = query.MerchantId.Value;
            employees = employees.Where(e => e.Memberships.Any(m => m.MerchantId == merchantId));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            employees = employees.Where(e =>
                e.Email.ToLower().Contains(search) ||
                e.FirstName.ToLower().Contains(search) ||
                e.LastName.ToLower().Contains(search));
        }

        var total = await employees.CountAsync();

        var items = await employees
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AdminEmployeeListItemDto
            {
                Id = e.Id,
                Email = e.Email,
                FirstName = e.FirstName,
                LastName = e.LastName,
                PhoneNumber = e.PhoneNumber,
                Kind = e.Kind,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                MerchantCount = e.Memberships.Count(),
                PrimaryMerchantId = e.Memberships.Count() == 1
                    ? (int?)e.Memberships.First().MerchantId
                    : null,
                PrimaryMerchantName = e.Memberships.Count() == 1
                    ? e.Memberships.First().Merchant.CompanyName
                    : null,
                HasAccount = e.UserId != null,
                UserId = e.UserId
            })
            .ToListAsync();

        return new AdminEmployeeListResponse
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminEmployeeDetailDto?> GetByIdAsync(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.Memberships).ThenInclude(m => m.Merchant)
            .Include(e => e.Memberships).ThenInclude(m => m.Role)
            .Include(e => e.Memberships).ThenInclude(m => m.HomeBranch)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            return null;

        return new AdminEmployeeDetailDto
        {
            Id = employee.Id,
            Email = employee.Email,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            PhoneNumber = employee.PhoneNumber,
            Kind = employee.Kind,
            ContractType = employee.ContractType,
            AgencyName = employee.AgencyName,
            HourlyRate = employee.HourlyRate,
            ExternalNotes = employee.ExternalNotes,
            IsActive = employee.IsActive,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt,
            UserId = employee.UserId,
            HasAccount = employee.UserId != null,
            Memberships = employee.Memberships.Select(m => new AdminEmployeeMembershipDto
            {
                Id = m.Id,
                MerchantId = m.MerchantId,
                MerchantName = m.Merchant?.CompanyName ?? string.Empty,
                MerchantApproved = m.Merchant?.IsApproved ?? false,
                RoleId = m.RoleId,
                RoleName = m.Role?.Name ?? string.Empty,
                HomeBranchId = m.HomeBranchId,
                HomeBranchName = m.HomeBranch?.Name ?? string.Empty,
                IsActive = m.IsActive,
                JoinedAt = m.JoinedAt
            }).ToList()
        };
    }
}
