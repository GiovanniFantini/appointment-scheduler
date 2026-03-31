using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio per la gestione dei dipendenti tramite EmployeeMembership
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _context;

    public EmployeeService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Recupera tutti i dipendenti di un merchant tramite le membership attive
    /// </summary>
    public async Task<List<EmployeeDto>> GetMerchantEmployeesAsync(int merchantId)
    {
        var memberships = await _context.EmployeeMemberships
            .Include(m => m.Employee)
            .Include(m => m.Role)
                .ThenInclude(r => r.Features)
            .Where(m => m.MerchantId == merchantId && m.IsActive)
            .OrderBy(m => m.Employee.LastName)
            .ThenBy(m => m.Employee.FirstName)
            .ToListAsync();

        return memberships.Select(m => MapToDto(m.Employee, m)).ToList();
    }

    /// <summary>
    /// Recupera un dipendente per ID, verificando la membership al merchant
    /// </summary>
    public async Task<EmployeeDto?> GetByIdAsync(int employeeId, int merchantId)
    {
        var membership = await _context.EmployeeMemberships
            .Include(m => m.Employee)
            .Include(m => m.Role)
                .ThenInclude(r => r.Features)
            .FirstOrDefaultAsync(m => m.EmployeeId == employeeId && m.MerchantId == merchantId && m.IsActive);

        if (membership == null)
            return null;

        return MapToDto(membership.Employee, membership);
    }

    /// <summary>
    /// Crea un nuovo dipendente e la relativa membership.
    /// Se esiste già un Employee con questa email, riusa il record esistente.
    /// </summary>
    public async Task<EmployeeDto> CreateAsync(int merchantId, CreateEmployeeRequest request)
    {
        var normalizedEmail = request.Email.ToLower();

        // Check if the Employee already exists by email
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Email == normalizedEmail);

        if (employee == null)
        {
            // Try to link with an existing User account of type Employee
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail
                    && u.AccountType == AccountType.Employee
                    && u.IsActive);

            employee = new Employee
            {
                UserId = existingUser?.Id,
                Email = normalizedEmail,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        // Check for existing membership (even inactive)
        var existingMembership = await _context.EmployeeMemberships
            .FirstOrDefaultAsync(m => m.EmployeeId == employee.Id && m.MerchantId == merchantId);

        if (existingMembership != null)
        {
            // Reactivate existing membership
            existingMembership.IsActive = true;
            existingMembership.RoleId = request.RoleId;
        }
        else
        {
            var membership = new EmployeeMembership
            {
                EmployeeId = employee.Id,
                MerchantId = merchantId,
                RoleId = request.RoleId,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };
            _context.EmployeeMemberships.Add(membership);
        }

        await _context.SaveChangesAsync();

        // Return with full membership context
        return (await GetByIdAsync(employee.Id, merchantId))!;
    }

    /// <summary>
    /// Aggiorna i dati di un dipendente e/o la sua membership nel merchant
    /// </summary>
    public async Task<EmployeeDto?> UpdateAsync(int employeeId, int merchantId, UpdateEmployeeRequest request)
    {
        var membership = await _context.EmployeeMemberships
            .Include(m => m.Employee)
            .Include(m => m.Role)
                .ThenInclude(r => r.Features)
            .FirstOrDefaultAsync(m => m.EmployeeId == employeeId && m.MerchantId == merchantId && m.IsActive);

        if (membership == null)
            return null;

        var employee = membership.Employee;
        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.PhoneNumber = request.PhoneNumber;
        employee.IsActive = request.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;

        membership.RoleId = request.RoleId;
        membership.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        // Reload role with features
        await _context.Entry(membership).Reference(m => m.Role).LoadAsync();
        await _context.Entry(membership.Role).Collection(r => r.Features).LoadAsync();

        return MapToDto(employee, membership);
    }

    /// <summary>
    /// Rimuove un dipendente dal merchant disattivando la membership
    /// </summary>
    public async Task<bool> RemoveFromMerchantAsync(int employeeId, int merchantId)
    {
        var membership = await _context.EmployeeMemberships
            .FirstOrDefaultAsync(m => m.EmployeeId == employeeId && m.MerchantId == merchantId && m.IsActive);

        if (membership == null)
            return false;

        membership.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    private static EmployeeDto MapToDto(Employee employee, EmployeeMembership membership)
    {
        var activeFeatures = membership.Role?.Features
            .Where(f => f.IsEnabled)
            .Select(f => f.Feature.ToString())
            .ToList() ?? new List<string>();

        return new EmployeeDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            IsActive = employee.IsActive,
            HasUserAccount = employee.UserId.HasValue,
            CreatedAt = employee.CreatedAt,
            RoleId = membership.RoleId,
            RoleName = membership.Role?.Name,
            ActiveFeatures = activeFeatures
        };
    }
}
