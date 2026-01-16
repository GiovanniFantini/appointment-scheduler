using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio per la gestione dei turni
/// </summary>
public class ShiftService : IShiftService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmployeeWorkingHoursLimitService _limitsService;

    public ShiftService(ApplicationDbContext context, IEmployeeWorkingHoursLimitService limitsService)
    {
        _context = context;
        _limitsService = limitsService;
    }

    public async Task<IEnumerable<ShiftDto>> GetMerchantShiftsAsync(int merchantId, DateTime startDate, DateTime endDate)
    {
        var shifts = await _context.Shifts
            .Include(s => s.Employee)
            .Include(s => s.ShiftTemplate)
            .Where(s => s.MerchantId == merchantId
                && s.Date >= startDate.Date
                && s.Date <= endDate.Date
                && s.IsActive)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        return shifts.Select(MapToDto);
    }

    public async Task<IEnumerable<ShiftDto>> GetEmployeeShiftsAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        var shifts = await _context.Shifts
            .Include(s => s.Employee)
            .Include(s => s.ShiftTemplate)
            .Where(s => s.EmployeeId == employeeId
                && s.Date >= startDate.Date
                && s.Date <= endDate.Date
                && s.IsActive)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        return shifts.Select(MapToDto);
    }

    public async Task<ShiftDto?> GetShiftByIdAsync(int id)
    {
        var shift = await _context.Shifts
            .Include(s => s.Employee)
            .Include(s => s.ShiftTemplate)
            .FirstOrDefaultAsync(s => s.Id == id);

        return shift == null ? null : MapToDto(shift);
    }

    public async Task<ShiftDto> CreateShiftAsync(int merchantId, CreateShiftRequest request)
    {
        // Ensure date is in UTC to avoid PostgreSQL timezone issues
        var shiftDate = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);

        // Verifica conflitti se c'è un dipendente assegnato
        if (request.EmployeeId.HasValue)
        {
            var hasConflict = await HasShiftConflictAsync(
                request.EmployeeId.Value,
                shiftDate,
                request.StartTime,
                request.EndTime);

            if (hasConflict)
                throw new InvalidOperationException("Il turno va in conflitto con altri turni del dipendente");

            // Verifica limiti orari
            var hours = CalculateTotalHours(request.StartTime, request.EndTime, request.BreakDurationMinutes);
            var exceedsLimit = await ExceedsWorkingHoursLimitAsync(request.EmployeeId.Value, shiftDate, hours);

            if (exceedsLimit)
                throw new InvalidOperationException("Il turno supera i limiti orari del dipendente");
        }

        var shift = new Shift
        {
            MerchantId = merchantId,
            ShiftTemplateId = request.ShiftTemplateId,
            EmployeeId = request.EmployeeId,
            Date = shiftDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            BreakDurationMinutes = request.BreakDurationMinutes,
            ShiftType = request.ShiftType,
            Color = request.Color,
            Notes = request.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();

        await _context.Entry(shift).Reference(s => s.Employee).LoadAsync();
        await _context.Entry(shift).Reference(s => s.ShiftTemplate).LoadAsync();

        return MapToDto(shift);
    }

    public async Task<IEnumerable<ShiftDto>> CreateShiftsFromTemplateAsync(int merchantId, CreateShiftsFromTemplateRequest request)
    {
        var template = await _context.ShiftTemplates
            .FirstOrDefaultAsync(st => st.Id == request.ShiftTemplateId && st.MerchantId == merchantId);

        if (template == null)
            throw new InvalidOperationException("Template non trovato");

        var shifts = new List<Shift>();

        // Ensure dates are in UTC to avoid PostgreSQL timezone issues
        var startDate = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Utc);
        var endDate = DateTime.SpecifyKind(request.EndDate.Date, DateTimeKind.Utc);
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            // Controlla se il giorno della settimana è incluso
            if (request.DaysOfWeek == null || request.DaysOfWeek.Contains(currentDate.DayOfWeek))
            {
                // Verifica conflitti se c'è un dipendente
                if (request.EmployeeId.HasValue)
                {
                    var hasConflict = await HasShiftConflictAsync(
                        request.EmployeeId.Value,
                        currentDate,
                        template.StartTime,
                        template.EndTime);

                    if (!hasConflict)
                    {
                        var shift = new Shift
                        {
                            MerchantId = merchantId,
                            ShiftTemplateId = template.Id,
                            EmployeeId = request.EmployeeId,
                            Date = currentDate,
                            StartTime = template.StartTime,
                            EndTime = template.EndTime,
                            BreakDurationMinutes = template.BreakDurationMinutes,
                            ShiftType = template.ShiftType,
                            Color = template.Color,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        shifts.Add(shift);
                    }
                }
                else
                {
                    var shift = new Shift
                    {
                        MerchantId = merchantId,
                        ShiftTemplateId = template.Id,
                        Date = currentDate,
                        StartTime = template.StartTime,
                        EndTime = template.EndTime,
                        BreakDurationMinutes = template.BreakDurationMinutes,
                        ShiftType = template.ShiftType,
                        Color = template.Color,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    shifts.Add(shift);
                }
            }

            currentDate = currentDate.AddDays(1);
        }

        _context.Shifts.AddRange(shifts);
        await _context.SaveChangesAsync();

        // Carica le navigation properties
        foreach (var shift in shifts)
        {
            await _context.Entry(shift).Reference(s => s.Employee).LoadAsync();
            await _context.Entry(shift).Reference(s => s.ShiftTemplate).LoadAsync();
        }

        return shifts.Select(MapToDto);
    }

    public async Task<ShiftDto?> UpdateShiftAsync(int shiftId, int merchantId, UpdateShiftRequest request)
    {
        var shift = await _context.Shifts
            .Include(s => s.Employee)
            .Include(s => s.ShiftTemplate)
            .FirstOrDefaultAsync(s => s.Id == shiftId && s.MerchantId == merchantId);

        if (shift == null)
            return null;

        // Ensure date is in UTC to avoid PostgreSQL timezone issues
        var shiftDate = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);

        // Verifica conflitti se c'è un dipendente
        if (request.EmployeeId.HasValue)
        {
            var hasConflict = await HasShiftConflictAsync(
                request.EmployeeId.Value,
                shiftDate,
                request.StartTime,
                request.EndTime,
                shiftId);

            if (hasConflict)
                throw new InvalidOperationException("Il turno va in conflitto con altri turni del dipendente");

            // Verifica limiti orari
            var hours = CalculateTotalHours(request.StartTime, request.EndTime, request.BreakDurationMinutes);
            var exceedsLimit = await ExceedsWorkingHoursLimitAsync(request.EmployeeId.Value, shiftDate, hours);

            if (exceedsLimit)
                throw new InvalidOperationException("Il turno supera i limiti orari del dipendente");
        }

        shift.EmployeeId = request.EmployeeId;
        shift.Date = shiftDate;
        shift.StartTime = request.StartTime;
        shift.EndTime = request.EndTime;
        shift.BreakDurationMinutes = request.BreakDurationMinutes;
        shift.ShiftType = request.ShiftType;
        shift.Color = request.Color;
        shift.Notes = request.Notes;
        shift.IsActive = request.IsActive;
        shift.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(shift);
    }

    public async Task<ShiftDto?> AssignShiftAsync(int shiftId, int merchantId, AssignShiftRequest request)
    {
        var shift = await _context.Shifts
            .Include(s => s.Employee)
            .Include(s => s.ShiftTemplate)
            .FirstOrDefaultAsync(s => s.Id == shiftId && s.MerchantId == merchantId);

        if (shift == null)
            return null;

        // Verifica che il dipendente appartenga allo stesso merchant
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.MerchantId == merchantId);

        if (employee == null)
            throw new InvalidOperationException("Dipendente non trovato");

        // Verifica conflitti
        var hasConflict = await HasShiftConflictAsync(
            request.EmployeeId,
            shift.Date,
            shift.StartTime,
            shift.EndTime,
            shiftId);

        if (hasConflict)
            throw new InvalidOperationException("Il turno va in conflitto con altri turni del dipendente");

        shift.EmployeeId = request.EmployeeId;
        if (!string.IsNullOrWhiteSpace(request.Notes))
            shift.Notes = request.Notes;
        shift.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(shift);
    }

    public async Task<bool> DeleteShiftAsync(int shiftId, int merchantId)
    {
        var shift = await _context.Shifts
            .FirstOrDefaultAsync(s => s.Id == shiftId && s.MerchantId == merchantId);

        if (shift == null)
            return false;

        shift.IsActive = false;
        shift.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<EmployeeShiftStatsDto> GetEmployeeShiftStatsAsync(int employeeId)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
            throw new InvalidOperationException("Dipendente non trovato");

        var now = DateTime.UtcNow;
        var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(7);
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var endOfLastMonth = startOfMonth;

        var shiftsThisWeek = await _context.Shifts
            .Where(s => s.EmployeeId == employeeId
                && s.Date >= startOfWeek
                && s.Date < endOfWeek
                && s.IsActive)
            .ToListAsync();

        var shiftsThisMonth = await _context.Shifts
            .Where(s => s.EmployeeId == employeeId
                && s.Date >= startOfMonth
                && s.Date < endOfMonth
                && s.IsActive)
            .ToListAsync();

        var shiftsLastMonth = await _context.Shifts
            .Where(s => s.EmployeeId == employeeId
                && s.Date >= startOfLastMonth
                && s.Date < endOfLastMonth
                && s.IsActive)
            .ToListAsync();

        var totalHoursThisWeek = shiftsThisWeek.Sum(s => (decimal)CalculateTotalHours(s.StartTime, s.EndTime, s.BreakDurationMinutes));
        var totalHoursThisMonth = shiftsThisMonth.Sum(s => (decimal)CalculateTotalHours(s.StartTime, s.EndTime, s.BreakDurationMinutes));
        var totalHoursLastMonth = shiftsLastMonth.Sum(s => (decimal)CalculateTotalHours(s.StartTime, s.EndTime, s.BreakDurationMinutes));

        // Recupera i limiti attivi
        var activeLimit = await _limitsService.GetActiveEmployeeLimitAsync(employeeId, now);

        var stats = new EmployeeShiftStatsDto
        {
            EmployeeId = employeeId,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            TotalHoursThisWeek = totalHoursThisWeek,
            TotalHoursThisMonth = totalHoursThisMonth,
            TotalHoursLastMonth = totalHoursLastMonth,
            TotalShiftsThisWeek = shiftsThisWeek.Count,
            TotalShiftsThisMonth = shiftsThisMonth.Count,
            TotalShiftsLastMonth = shiftsLastMonth.Count,
            AverageHoursPerShift = shiftsThisMonth.Count > 0 ? totalHoursThisMonth / shiftsThisMonth.Count : 0,
            MaxHoursPerWeek = activeLimit?.MaxHoursPerWeek,
            MaxHoursPerMonth = activeLimit?.MaxHoursPerMonth,
            RemainingHoursThisWeek = activeLimit?.MaxHoursPerWeek.HasValue == true
                ? Math.Max(0, activeLimit.MaxHoursPerWeek.Value - totalHoursThisWeek)
                : 0,
            RemainingHoursThisMonth = activeLimit?.MaxHoursPerMonth.HasValue == true
                ? Math.Max(0, activeLimit.MaxHoursPerMonth.Value - totalHoursThisMonth)
                : 0,
            IsOverLimit = (activeLimit?.MaxHoursPerWeek.HasValue == true && totalHoursThisWeek > activeLimit.MaxHoursPerWeek.Value) ||
                         (activeLimit?.MaxHoursPerMonth.HasValue == true && totalHoursThisMonth > activeLimit.MaxHoursPerMonth.Value)
        };

        return stats;
    }

    public async Task<bool> HasShiftConflictAsync(int employeeId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeShiftId = null)
    {
        var shiftsOnDate = await _context.Shifts
            .Where(s => s.EmployeeId == employeeId
                && s.Date == date.Date
                && s.IsActive
                && (excludeShiftId == null || s.Id != excludeShiftId))
            .ToListAsync();

        foreach (var shift in shiftsOnDate)
        {
            // Verifica sovrapposizione oraria
            if (TimesOverlap(startTime, endTime, shift.StartTime, shift.EndTime))
                return true;
        }

        return false;
    }

    public async Task<bool> ExceedsWorkingHoursLimitAsync(int employeeId, DateTime date, decimal hours)
    {
        var activeLimit = await _limitsService.GetActiveEmployeeLimitAsync(employeeId, date);

        if (activeLimit == null)
            return false;

        // Verifica limite giornaliero
        if (activeLimit.MaxHoursPerDay.HasValue && hours > activeLimit.MaxHoursPerDay.Value)
            return true;

        // Verifica limite settimanale
        if (activeLimit.MaxHoursPerWeek.HasValue)
        {
            var startOfWeek = date.Date.AddDays(-(int)date.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7);

            var weekHours = await _context.Shifts
                .Where(s => s.EmployeeId == employeeId
                    && s.Date >= startOfWeek
                    && s.Date < endOfWeek
                    && s.IsActive)
                .ToListAsync();

            var totalWeekHours = weekHours.Sum(s => (decimal)CalculateTotalHours(s.StartTime, s.EndTime, s.BreakDurationMinutes));

            if (totalWeekHours + hours > activeLimit.MaxHoursPerWeek.Value)
                return true;
        }

        // Verifica limite mensile
        if (activeLimit.MaxHoursPerMonth.HasValue)
        {
            var startOfMonth = new DateTime(date.Year, date.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            var monthShifts = await _context.Shifts
                .Where(s => s.EmployeeId == employeeId
                    && s.Date >= startOfMonth
                    && s.Date < endOfMonth
                    && s.IsActive)
                .ToListAsync();

            var totalMonthHours = monthShifts.Sum(s => (decimal)CalculateTotalHours(s.StartTime, s.EndTime, s.BreakDurationMinutes));

            if (totalMonthHours + hours > activeLimit.MaxHoursPerMonth.Value)
                return true;
        }

        return false;
    }

    private static bool TimesOverlap(TimeSpan start1, TimeSpan end1, TimeSpan start2, TimeSpan end2)
    {
        return start1 < end2 && end1 > start2;
    }

    private static ShiftDto MapToDto(Shift shift)
    {
        var totalHours = CalculateTotalHours(shift.StartTime, shift.EndTime, shift.BreakDurationMinutes);

        return new ShiftDto
        {
            Id = shift.Id,
            MerchantId = shift.MerchantId,
            ShiftTemplateId = shift.ShiftTemplateId,
            ShiftTemplateName = shift.ShiftTemplate?.Name,
            EmployeeId = shift.EmployeeId,
            EmployeeName = shift.Employee != null ? $"{shift.Employee.FirstName} {shift.Employee.LastName}" : null,
            Date = shift.Date,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            BreakDurationMinutes = shift.BreakDurationMinutes,
            TotalHours = totalHours,
            ShiftType = shift.ShiftType,
            ShiftTypeName = shift.ShiftType.ToString(),
            Color = shift.Color,
            Notes = shift.Notes,
            IsConfirmed = shift.IsConfirmed,
            IsCheckedIn = shift.IsCheckedIn,
            CheckInTime = shift.CheckInTime,
            IsCheckedOut = shift.IsCheckedOut,
            CheckOutTime = shift.CheckOutTime,
            IsActive = shift.IsActive,
            CreatedAt = shift.CreatedAt,
            UpdatedAt = shift.UpdatedAt
        };
    }

    private static decimal CalculateTotalHours(TimeSpan startTime, TimeSpan endTime, int breakMinutes)
    {
        var duration = endTime - startTime;
        if (duration.TotalMinutes < 0) // Turno attraversa mezzanotte
            duration = duration.Add(TimeSpan.FromDays(1));

        var workMinutes = duration.TotalMinutes - breakMinutes;
        return (decimal)(workMinutes / 60.0);
    }
}
