using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio per la gestione dei dipendenti
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _context;

    public EmployeeService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Recupera tutti i dipendenti di un merchant
    /// </summary>
    public async Task<IEnumerable<EmployeeDto>> GetMerchantEmployeesAsync(int merchantId)
    {
        var employees = await _context.Employees
            .Include(e => e.Merchant)
            .Where(e => e.MerchantId == merchantId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return employees.Select(MapToDto);
    }

    /// <summary>
    /// Recupera un dipendente per ID
    /// </summary>
    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.Merchant)
            .FirstOrDefaultAsync(e => e.Id == id);

        return employee == null ? null : MapToDto(employee);
    }

    /// <summary>
    /// Crea un nuovo dipendente
    /// Se l'email corrisponde a un User già registrato come Employee, lo collega automaticamente
    /// Altrimenti crea un "pending employee" che verrà collegato quando l'employee si registrerà
    /// </summary>
    public async Task<EmployeeDto> CreateEmployeeAsync(int merchantId, CreateEmployeeRequest request)
    {
        var normalizedEmail = request.Email.ToLower();

        // Cerca se esiste già un User registrato con questa email come Employee
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.IsEmployee && u.IsActive);

        var employee = new Employee
        {
            MerchantId = merchantId,
            UserId = existingUser?.Id, // Se esiste, collega; altrimenti null (pending)
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = normalizedEmail,
            PhoneNumber = request.PhoneNumber,
            BadgeCode = request.BadgeCode,
            Role = request.Role,
            ShiftsConfiguration = request.ShiftsConfiguration,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        await _context.Entry(employee).Reference(e => e.Merchant).LoadAsync();

        return MapToDto(employee);
    }

    /// <summary>
    /// Aggiorna un dipendente esistente
    /// </summary>
    public async Task<EmployeeDto?> UpdateEmployeeAsync(int employeeId, int merchantId, UpdateEmployeeRequest request)
    {
        var employee = await _context.Employees
            .Include(e => e.Merchant)
            .FirstOrDefaultAsync(e => e.Id == employeeId && e.MerchantId == merchantId);

        if (employee == null)
            return null;

        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.Email = request.Email.ToLower();
        employee.PhoneNumber = request.PhoneNumber;
        employee.BadgeCode = request.BadgeCode;
        employee.Role = request.Role;
        employee.ShiftsConfiguration = request.ShiftsConfiguration;
        employee.IsActive = request.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(employee);
    }

    /// <summary>
    /// Elimina un dipendente
    /// </summary>
    public async Task<bool> DeleteEmployeeAsync(int employeeId, int merchantId)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId && e.MerchantId == merchantId);

        if (employee == null)
            return false;

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();

        return true;
    }

    private EmployeeDto MapToDto(Employee employee)
    {
        return new EmployeeDto
        {
            Id = employee.Id,
            MerchantId = employee.MerchantId,
            MerchantName = employee.Merchant.BusinessName,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            FullName = $"{employee.FirstName} {employee.LastName}",
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            BadgeCode = employee.BadgeCode,
            Role = employee.Role,
            ShiftsConfiguration = employee.ShiftsConfiguration,
            IsActive = employee.IsActive,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }
}
