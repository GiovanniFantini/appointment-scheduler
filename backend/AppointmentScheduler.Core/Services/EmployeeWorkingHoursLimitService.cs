using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio per la gestione dei limiti orari dei dipendenti
/// </summary>
public class EmployeeWorkingHoursLimitService : IEmployeeWorkingHoursLimitService
{
    private readonly ApplicationDbContext _context;

    public EmployeeWorkingHoursLimitService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EmployeeWorkingHoursLimitDto>> GetEmployeeLimitsAsync(int employeeId)
    {
        var limits = await _context.EmployeeWorkingHoursLimits
            .Include(l => l.Employee)
            .Where(l => l.EmployeeId == employeeId && l.IsActive)
            .OrderByDescending(l => l.ValidFrom)
            .ToListAsync();

        return limits.Select(MapToDto);
    }

    public async Task<EmployeeWorkingHoursLimitDto?> GetActiveEmployeeLimitAsync(int employeeId, DateTime date)
    {
        var limit = await _context.EmployeeWorkingHoursLimits
            .Include(l => l.Employee)
            .Where(l => l.EmployeeId == employeeId
                && l.IsActive
                && l.ValidFrom <= date
                && (l.ValidTo == null || l.ValidTo >= date))
            .OrderByDescending(l => l.ValidFrom)
            .FirstOrDefaultAsync();

        return limit == null ? null : MapToDto(limit);
    }

    public async Task<EmployeeWorkingHoursLimitDto?> GetLimitByIdAsync(int id)
    {
        var limit = await _context.EmployeeWorkingHoursLimits
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == id);

        return limit == null ? null : MapToDto(limit);
    }

    public async Task<EmployeeWorkingHoursLimitDto> CreateLimitAsync(int merchantId, CreateEmployeeWorkingHoursLimitRequest request)
    {
        // Verifica che il dipendente appartenga al merchant
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.MerchantId == merchantId);

        if (employee == null)
            throw new InvalidOperationException("Dipendente non trovato");

        var limit = new EmployeeWorkingHoursLimit
        {
            EmployeeId = request.EmployeeId,
            MerchantId = merchantId,
            MaxHoursPerDay = request.MaxHoursPerDay,
            MaxHoursPerWeek = request.MaxHoursPerWeek,
            MaxHoursPerMonth = request.MaxHoursPerMonth,
            MinHoursPerWeek = request.MinHoursPerWeek,
            MinHoursPerMonth = request.MinHoursPerMonth,
            AllowOvertime = request.AllowOvertime,
            MaxOvertimeHoursPerWeek = request.MaxOvertimeHoursPerWeek,
            MaxOvertimeHoursPerMonth = request.MaxOvertimeHoursPerMonth,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmployeeWorkingHoursLimits.Add(limit);
        await _context.SaveChangesAsync();

        await _context.Entry(limit).Reference(l => l.Employee).LoadAsync();

        return MapToDto(limit);
    }

    public async Task<EmployeeWorkingHoursLimitDto?> UpdateLimitAsync(int limitId, int merchantId, UpdateEmployeeWorkingHoursLimitRequest request)
    {
        var limit = await _context.EmployeeWorkingHoursLimits
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == limitId && l.MerchantId == merchantId);

        if (limit == null)
            return null;

        limit.MaxHoursPerDay = request.MaxHoursPerDay;
        limit.MaxHoursPerWeek = request.MaxHoursPerWeek;
        limit.MaxHoursPerMonth = request.MaxHoursPerMonth;
        limit.MinHoursPerWeek = request.MinHoursPerWeek;
        limit.MinHoursPerMonth = request.MinHoursPerMonth;
        limit.AllowOvertime = request.AllowOvertime;
        limit.MaxOvertimeHoursPerWeek = request.MaxOvertimeHoursPerWeek;
        limit.MaxOvertimeHoursPerMonth = request.MaxOvertimeHoursPerMonth;
        limit.ValidFrom = request.ValidFrom;
        limit.ValidTo = request.ValidTo;
        limit.IsActive = request.IsActive;
        limit.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(limit);
    }

    public async Task<bool> DeleteLimitAsync(int limitId, int merchantId)
    {
        var limit = await _context.EmployeeWorkingHoursLimits
            .FirstOrDefaultAsync(l => l.Id == limitId && l.MerchantId == merchantId);

        if (limit == null)
            return false;

        limit.IsActive = false;
        limit.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    private static EmployeeWorkingHoursLimitDto MapToDto(EmployeeWorkingHoursLimit limit)
    {
        return new EmployeeWorkingHoursLimitDto
        {
            Id = limit.Id,
            EmployeeId = limit.EmployeeId,
            EmployeeName = $"{limit.Employee.FirstName} {limit.Employee.LastName}",
            MerchantId = limit.MerchantId,
            MaxHoursPerDay = limit.MaxHoursPerDay,
            MaxHoursPerWeek = limit.MaxHoursPerWeek,
            MaxHoursPerMonth = limit.MaxHoursPerMonth,
            MinHoursPerWeek = limit.MinHoursPerWeek,
            MinHoursPerMonth = limit.MinHoursPerMonth,
            AllowOvertime = limit.AllowOvertime,
            MaxOvertimeHoursPerWeek = limit.MaxOvertimeHoursPerWeek,
            MaxOvertimeHoursPerMonth = limit.MaxOvertimeHoursPerMonth,
            ValidFrom = limit.ValidFrom,
            ValidTo = limit.ValidTo,
            IsActive = limit.IsActive,
            CreatedAt = limit.CreatedAt,
            UpdatedAt = limit.UpdatedAt
        };
    }
}
