using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Exceptions;
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
        // Ensure dates are in UTC to avoid PostgreSQL timezone issues
        var utcStartDate = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
        var utcEndDate = DateTime.SpecifyKind(endDate.Date, DateTimeKind.Utc);

        var shifts = await _context.Shifts
            .Include(s => s.Employee)
            .Include(s => s.ShiftTemplate)
            .Include(s => s.ShiftEmployees)
                .ThenInclude(se => se.Employee)
            .Where(s => s.MerchantId == merchantId
                && s.Date >= utcStartDate
                && s.Date <= utcEndDate
                && s.IsActive)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        return shifts.Select(MapToDto);
    }

    public async Task<IEnumerable<ShiftDto>> GetEmployeeShiftsAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        // Ensure dates are in UTC to avoid PostgreSQL timezone issues
        var utcStartDate = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
        var utcEndDate = DateTime.SpecifyKind(endDate.Date, DateTimeKind.Utc);

        // Cerca turni sia nella relazione legacy (EmployeeId) che nella nuova (ShiftEmployees)
        var shifts = await _context.Shifts
            .Include(s => s.Employee)
            .Include(s => s.ShiftTemplate)
            .Include(s => s.ShiftEmployees)
                .ThenInclude(se => se.Employee)
            .Where(s => (s.EmployeeId == employeeId || s.ShiftEmployees.Any(se => se.EmployeeId == employeeId))
                && s.Date >= utcStartDate
                && s.Date <= utcEndDate
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
            .Include(s => s.ShiftEmployees)
                .ThenInclude(se => se.Employee)
            .FirstOrDefaultAsync(s => s.Id == id);

        return shift == null ? null : MapToDto(shift);
    }

    public async Task<ShiftDto> CreateShiftAsync(int merchantId, CreateShiftRequest request)
    {
        // Ensure date is in UTC to avoid PostgreSQL timezone issues
        var shiftDate = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);

        // Determina lista dipendenti (supporta backward compatibility)
        var employeeIds = new List<int>();
        if (request.EmployeeIds?.Any() == true)
            employeeIds = request.EmployeeIds;
        else if (request.EmployeeId.HasValue)
            employeeIds.Add(request.EmployeeId.Value);

        // Verifica conflitti con ferie/permessi
        if (!request.ForceCreate && employeeIds.Any())
        {
            var leaveConflicts = await GetLeaveConflictsAsync(employeeIds, shiftDate);
            if (leaveConflicts.Any())
            {
                throw new LeaveConflictException(
                    "Alcuni dipendenti hanno ferie/permessi in questa data",
                    leaveConflicts);
            }
        }

        // Verifica conflitti e limiti per ogni dipendente
        var hours = CalculateTotalHours(request.StartTime, request.EndTime, request.BreakDurationMinutes);
        foreach (var employeeId in employeeIds)
        {
            var hasConflict = await HasShiftConflictAsync(
                employeeId,
                shiftDate,
                request.StartTime,
                request.EndTime);

            if (hasConflict)
                throw new InvalidOperationException($"Il turno va in conflitto con altri turni del dipendente ID {employeeId}");

            var exceedsLimit = await ExceedsWorkingHoursLimitAsync(employeeId, shiftDate, hours);

            if (exceedsLimit)
                throw new InvalidOperationException($"Il turno supera i limiti orari del dipendente ID {employeeId}");
        }

        var shift = new Shift
        {
            MerchantId = merchantId,
            ShiftTemplateId = request.ShiftTemplateId,
            EmployeeId = request.EmployeeId, // Mantieni per backward compatibility
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

        // Crea le associazioni ShiftEmployee
        foreach (var employeeId in employeeIds)
        {
            var shiftEmployee = new ShiftEmployee
            {
                ShiftId = shift.Id,
                EmployeeId = employeeId,
                CreatedAt = DateTime.UtcNow
            };
            _context.ShiftEmployees.Add(shiftEmployee);
        }
        await _context.SaveChangesAsync();

        await _context.Entry(shift).Reference(s => s.Employee).LoadAsync();
        await _context.Entry(shift).Reference(s => s.ShiftTemplate).LoadAsync();
        await _context.Entry(shift).Collection(s => s.ShiftEmployees).LoadAsync();
        foreach (var se in shift.ShiftEmployees)
        {
            await _context.Entry(se).Reference(se => se.Employee).LoadAsync();
        }

        return MapToDto(shift);
    }

    public async Task<IEnumerable<ShiftDto>> CreateShiftsFromTemplateAsync(int merchantId, CreateShiftsFromTemplateRequest request)
    {
        var template = await _context.ShiftTemplates
            .FirstOrDefaultAsync(st => st.Id == request.ShiftTemplateId && st.MerchantId == merchantId);

        if (template == null)
            throw new InvalidOperationException("Template non trovato");

        // Determina lista dipendenti (supporta backward compatibility)
        var employeeIds = new List<int>();
        if (request.EmployeeIds?.Any() == true)
            employeeIds = request.EmployeeIds;
        else if (request.EmployeeId.HasValue)
            employeeIds.Add(request.EmployeeId.Value);

        // Verifica conflitti con ferie/permessi per l'intero range di date
        if (!request.ForceCreate && employeeIds.Any())
        {
            var leaveConflicts = await GetLeaveConflictsForDateRangeAsync(employeeIds, request.StartDate, request.EndDate);
            if (leaveConflicts.Any())
            {
                throw new LeaveConflictException(
                    "Alcuni dipendenti hanno ferie/permessi nel periodo selezionato",
                    leaveConflicts);
            }
        }

        var shifts = new List<Shift>();
        var shiftEmployeeMappings = new List<(Shift shift, List<int> employeeIds)>();

        // Ensure dates are in UTC to avoid PostgreSQL timezone issues
        var startDate = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Utc);
        var endDate = DateTime.SpecifyKind(request.EndDate.Date, DateTimeKind.Utc);
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            // Controlla se il giorno della settimana è incluso
            if (request.DaysOfWeek == null || request.DaysOfWeek.Contains(currentDate.DayOfWeek))
            {
                bool canCreateShift = true;
                var validEmployees = new List<int>();

                // Verifica conflitti per ogni dipendente
                if (employeeIds.Any())
                {
                    foreach (var employeeId in employeeIds)
                    {
                        var hasConflict = await HasShiftConflictAsync(
                            employeeId,
                            currentDate,
                            template.StartTime,
                            template.EndTime);

                        if (!hasConflict)
                            validEmployees.Add(employeeId);
                    }

                    // Crea turno solo se almeno un dipendente è valido
                    canCreateShift = validEmployees.Any();
                }

                if (canCreateShift)
                {
                    var shift = new Shift
                    {
                        MerchantId = merchantId,
                        ShiftTemplateId = template.Id,
                        EmployeeId = request.EmployeeId, // Backward compatibility
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
                    shiftEmployeeMappings.Add((shift, validEmployees));
                }
            }

            currentDate = currentDate.AddDays(1);
        }

        _context.Shifts.AddRange(shifts);
        await _context.SaveChangesAsync();

        // Crea le associazioni ShiftEmployee
        foreach (var (shift, empIds) in shiftEmployeeMappings)
        {
            foreach (var employeeId in empIds)
            {
                var shiftEmployee = new ShiftEmployee
                {
                    ShiftId = shift.Id,
                    EmployeeId = employeeId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ShiftEmployees.Add(shiftEmployee);
            }
        }
        await _context.SaveChangesAsync();

        // Carica le navigation properties
        foreach (var shift in shifts)
        {
            await _context.Entry(shift).Reference(s => s.Employee).LoadAsync();
            await _context.Entry(shift).Reference(s => s.ShiftTemplate).LoadAsync();
            await _context.Entry(shift).Collection(s => s.ShiftEmployees).LoadAsync();
            foreach (var se in shift.ShiftEmployees)
            {
                await _context.Entry(se).Reference(se => se.Employee).LoadAsync();
            }
        }

        return shifts.Select(MapToDto);
    }

    public async Task<ShiftDto?> UpdateShiftAsync(int shiftId, int merchantId, UpdateShiftRequest request)
    {
        var shift = await _context.Shifts
            .Include(s => s.Employee)
            .Include(s => s.ShiftTemplate)
            .Include(s => s.ShiftEmployees)
            .FirstOrDefaultAsync(s => s.Id == shiftId && s.MerchantId == merchantId);

        if (shift == null)
            return null;

        // Ensure date is in UTC to avoid PostgreSQL timezone issues
        var shiftDate = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);

        // Determina lista dipendenti (supporta backward compatibility)
        var employeeIds = new List<int>();
        if (request.EmployeeIds?.Any() == true)
            employeeIds = request.EmployeeIds;
        else if (request.EmployeeId.HasValue)
            employeeIds.Add(request.EmployeeId.Value);

        // Verifica conflitti con ferie/permessi
        if (!request.ForceCreate && employeeIds.Any())
        {
            var leaveConflicts = await GetLeaveConflictsAsync(employeeIds, shiftDate);
            if (leaveConflicts.Any())
            {
                throw new LeaveConflictException(
                    "Alcuni dipendenti hanno ferie/permessi in questa data",
                    leaveConflicts);
            }
        }

        // Verifica conflitti e limiti per ogni dipendente
        var hours = CalculateTotalHours(request.StartTime, request.EndTime, request.BreakDurationMinutes);
        foreach (var employeeId in employeeIds)
        {
            var hasConflict = await HasShiftConflictAsync(
                employeeId,
                shiftDate,
                request.StartTime,
                request.EndTime,
                shiftId);

            if (hasConflict)
                throw new InvalidOperationException($"Il turno va in conflitto con altri turni del dipendente ID {employeeId}");

            var exceedsLimit = await ExceedsWorkingHoursLimitAsync(employeeId, shiftDate, hours);

            if (exceedsLimit)
                throw new InvalidOperationException($"Il turno supera i limiti orari del dipendente ID {employeeId}");
        }

        shift.EmployeeId = request.EmployeeId; // Backward compatibility
        shift.Date = shiftDate;
        shift.StartTime = request.StartTime;
        shift.EndTime = request.EndTime;
        shift.BreakDurationMinutes = request.BreakDurationMinutes;
        shift.ShiftType = request.ShiftType;
        shift.Color = request.Color;
        shift.Notes = request.Notes;
        shift.IsActive = request.IsActive;
        shift.UpdatedAt = DateTime.UtcNow;

        // Rimuovi le associazioni esistenti e crea quelle nuove
        var existingAssignments = shift.ShiftEmployees.ToList();
        _context.ShiftEmployees.RemoveRange(existingAssignments);

        foreach (var employeeId in employeeIds)
        {
            var shiftEmployee = new ShiftEmployee
            {
                ShiftId = shift.Id,
                EmployeeId = employeeId,
                CreatedAt = DateTime.UtcNow
            };
            _context.ShiftEmployees.Add(shiftEmployee);
        }

        await _context.SaveChangesAsync();

        // Ricarica le navigation properties
        await _context.Entry(shift).Collection(s => s.ShiftEmployees).LoadAsync();
        foreach (var se in shift.ShiftEmployees)
        {
            await _context.Entry(se).Reference(se => se.Employee).LoadAsync();
        }

        return MapToDto(shift);
    }

    public async Task<ShiftDto?> AssignShiftAsync(int shiftId, int merchantId, AssignShiftRequest request)
    {
        var shift = await _context.Shifts
            .Include(s => s.Employee)
            .Include(s => s.ShiftTemplate)
            .Include(s => s.ShiftEmployees)
            .FirstOrDefaultAsync(s => s.Id == shiftId && s.MerchantId == merchantId);

        if (shift == null)
            return null;

        // Determina lista dipendenti (supporta backward compatibility)
        var employeeIds = new List<int>();
        if (request.EmployeeIds?.Any() == true)
            employeeIds = request.EmployeeIds;
        else if (request.EmployeeId.HasValue)
            employeeIds.Add(request.EmployeeId.Value);

        // Verifica conflitti con ferie/permessi
        if (!request.ForceCreate && employeeIds.Any())
        {
            var leaveConflicts = await GetLeaveConflictsAsync(employeeIds, shift.Date);
            if (leaveConflicts.Any())
            {
                throw new LeaveConflictException(
                    "Alcuni dipendenti hanno ferie/permessi in questa data",
                    leaveConflicts);
            }
        }

        // Verifica che tutti i dipendenti appartengano allo stesso merchant
        foreach (var employeeId in employeeIds)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId && e.MerchantId == merchantId);

            if (employee == null)
                throw new InvalidOperationException($"Dipendente ID {employeeId} non trovato");

            // Verifica conflitti
            var hasConflict = await HasShiftConflictAsync(
                employeeId,
                shift.Date,
                shift.StartTime,
                shift.EndTime,
                shiftId);

            if (hasConflict)
                throw new InvalidOperationException($"Il turno va in conflitto con altri turni del dipendente ID {employeeId}");
        }

        shift.EmployeeId = request.EmployeeId; // Backward compatibility
        if (!string.IsNullOrWhiteSpace(request.Notes))
            shift.Notes = request.Notes;
        shift.UpdatedAt = DateTime.UtcNow;

        // Rimuovi le associazioni esistenti e crea quelle nuove
        var existingAssignments = shift.ShiftEmployees.ToList();
        _context.ShiftEmployees.RemoveRange(existingAssignments);

        foreach (var employeeId in employeeIds)
        {
            var shiftEmployee = new ShiftEmployee
            {
                ShiftId = shift.Id,
                EmployeeId = employeeId,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };
            _context.ShiftEmployees.Add(shiftEmployee);
        }

        await _context.SaveChangesAsync();

        // Ricarica le navigation properties
        await _context.Entry(shift).Collection(s => s.ShiftEmployees).LoadAsync();
        foreach (var se in shift.ShiftEmployees)
        {
            await _context.Entry(se).Reference(se => se.Employee).LoadAsync();
        }

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
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var endOfLastMonth = startOfMonth;

        // Cerca turni sia nella relazione legacy (EmployeeId) che nella nuova (ShiftEmployees)
        var shiftsThisWeek = await _context.Shifts
            .Include(s => s.ShiftEmployees)
            .Where(s => (s.EmployeeId == employeeId || s.ShiftEmployees.Any(se => se.EmployeeId == employeeId))
                && s.Date >= startOfWeek
                && s.Date < endOfWeek
                && s.IsActive)
            .ToListAsync();

        var shiftsThisMonth = await _context.Shifts
            .Include(s => s.ShiftEmployees)
            .Where(s => (s.EmployeeId == employeeId || s.ShiftEmployees.Any(se => se.EmployeeId == employeeId))
                && s.Date >= startOfMonth
                && s.Date < endOfMonth
                && s.IsActive)
            .ToListAsync();

        var shiftsLastMonth = await _context.Shifts
            .Include(s => s.ShiftEmployees)
            .Where(s => (s.EmployeeId == employeeId || s.ShiftEmployees.Any(se => se.EmployeeId == employeeId))
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
        // Controlla conflitti sia nella relazione legacy (EmployeeId) che nella nuova (ShiftEmployees)
        var shiftsOnDate = await _context.Shifts
            .Include(s => s.ShiftEmployees)
            .Where(s => (s.EmployeeId == employeeId || s.ShiftEmployees.Any(se => se.EmployeeId == employeeId))
                && s.Date == DateTime.SpecifyKind(date.Date, DateTimeKind.Utc)
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
            var startOfMonth = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
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

    /// <summary>
    /// Verifica se i dipendenti hanno ferie/permessi approvati o in attesa per una data specifica
    /// </summary>
    public async Task<List<LeaveConflictInfo>> GetLeaveConflictsAsync(List<int> employeeIds, DateTime date)
    {
        if (!employeeIds.Any())
            return new List<LeaveConflictInfo>();

        var utcDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

        var conflicts = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Where(lr => employeeIds.Contains(lr.EmployeeId)
                && lr.StartDate <= utcDate
                && lr.EndDate >= utcDate
                && (lr.Status == LeaveRequestStatus.Pending || lr.Status == LeaveRequestStatus.Approved))
            .Select(lr => new LeaveConflictInfo
            {
                EmployeeId = lr.EmployeeId,
                EmployeeName = lr.Employee.FirstName + " " + lr.Employee.LastName,
                LeaveRequestId = lr.Id,
                LeaveTypeName = lr.LeaveType.ToString(),
                StatusName = lr.Status == LeaveRequestStatus.Approved ? "Approvata" : "In attesa",
                StartDate = lr.StartDate,
                EndDate = lr.EndDate
            })
            .ToListAsync();

        return conflicts;
    }

    /// <summary>
    /// Verifica conflitti ferie per un range di date e ritorna i conflitti raggruppati per data
    /// </summary>
    public async Task<List<LeaveConflictInfo>> GetLeaveConflictsForDateRangeAsync(List<int> employeeIds, DateTime startDate, DateTime endDate)
    {
        if (!employeeIds.Any())
            return new List<LeaveConflictInfo>();

        var utcStartDate = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
        var utcEndDate = DateTime.SpecifyKind(endDate.Date, DateTimeKind.Utc);

        var conflicts = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Where(lr => employeeIds.Contains(lr.EmployeeId)
                && lr.StartDate <= utcEndDate
                && lr.EndDate >= utcStartDate
                && (lr.Status == LeaveRequestStatus.Pending || lr.Status == LeaveRequestStatus.Approved))
            .Select(lr => new LeaveConflictInfo
            {
                EmployeeId = lr.EmployeeId,
                EmployeeName = lr.Employee.FirstName + " " + lr.Employee.LastName,
                LeaveRequestId = lr.Id,
                LeaveTypeName = lr.LeaveType.ToString(),
                StatusName = lr.Status == LeaveRequestStatus.Approved ? "Approvata" : "In attesa",
                StartDate = lr.StartDate,
                EndDate = lr.EndDate
            })
            .ToListAsync();

        return conflicts;
    }

    private static bool TimesOverlap(TimeSpan start1, TimeSpan end1, TimeSpan start2, TimeSpan end2)
    {
        return start1 < end2 && end1 > start2;
    }

    private static ShiftDto MapToDto(Shift shift)
    {
        var totalHours = CalculateTotalHours(shift.StartTime, shift.EndTime, shift.BreakDurationMinutes);

        var employees = shift.ShiftEmployees?.Select(se => new ShiftEmployeeDto
        {
            Id = se.Id,
            ShiftId = se.ShiftId,
            EmployeeId = se.EmployeeId,
            EmployeeName = se.Employee != null ? $"{se.Employee.FirstName} {se.Employee.LastName}" : string.Empty,
            IsConfirmed = se.IsConfirmed,
            IsCheckedIn = se.IsCheckedIn,
            CheckInTime = se.CheckInTime,
            IsCheckedOut = se.IsCheckedOut,
            CheckOutTime = se.CheckOutTime,
            CheckInLocation = se.CheckInLocation,
            CheckOutLocation = se.CheckOutLocation,
            Notes = se.Notes
        }).ToList() ?? new List<ShiftEmployeeDto>();

        return new ShiftDto
        {
            Id = shift.Id,
            MerchantId = shift.MerchantId,
            ShiftTemplateId = shift.ShiftTemplateId,
            ShiftTemplateName = shift.ShiftTemplate?.Name,
            EmployeeId = shift.EmployeeId, // Backward compatibility
            EmployeeName = shift.Employee != null ? $"{shift.Employee.FirstName} {shift.Employee.LastName}" : null, // Backward compatibility
            Employees = employees,
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
            ValidationStatus = shift.ValidationStatus,
            ValidatedAt = shift.ValidatedAt,
            ValidatedBy = shift.ValidatedBy,
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
