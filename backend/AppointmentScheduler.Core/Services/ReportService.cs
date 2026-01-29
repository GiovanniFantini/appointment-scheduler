using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text;

namespace AppointmentScheduler.Core.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
        // Configura QuestPDF per uso community
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<EmployeeAttendanceReportDto> GetEmployeeAttendanceReportAsync(
        int employeeId,
        DateTime startDate,
        DateTime endDate,
        bool includeDetails = true)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId)
            ?? throw new InvalidOperationException("Dipendente non trovato");

        // Turni assegnati nel periodo
        var shifts = await _context.ShiftEmployees
            .Include(se => se.Shift)
            .Where(se => se.EmployeeId == employeeId &&
                        se.Shift.Date >= startDate &&
                        se.Shift.Date <= endDate &&
                        se.Shift.IsActive)
            .Select(se => se.Shift)
            .ToListAsync();

        // Anomalie nel periodo
        var anomalies = await _context.ShiftAnomalies
            .Include(a => a.Shift)
            .Where(a => a.Shift.Date >= startDate &&
                       a.Shift.Date <= endDate &&
                       _context.ShiftEmployees.Any(se => se.ShiftId == a.ShiftId && se.EmployeeId == employeeId))
            .ToListAsync();

        // Ferie nel periodo
        var leaveRequests = await _context.LeaveRequests
            .Where(lr => lr.EmployeeId == employeeId &&
                        lr.StartDate <= endDate &&
                        lr.EndDate >= startDate)
            .ToListAsync();

        // Saldo ferie attuale
        var currentYear = DateTime.UtcNow.Year;
        var leaveBalances = await _context.EmployeeLeaveBalances
            .Where(lb => lb.EmployeeId == employeeId && lb.Year == currentYear)
            .ToListAsync();

        // Calcoli statistiche
        var shiftsCompleted = shifts.Count(s => s.IsCheckedOut);
        var totalHoursScheduled = shifts.Sum(s => (decimal)(s.EndTime - s.StartTime).TotalHours - s.BreakDurationMinutes / 60m);
        var totalHoursWorked = shifts
            .Where(s => s.CheckInTime.HasValue && s.CheckOutTime.HasValue)
            .Sum(s => (decimal)(s.CheckOutTime!.Value - s.CheckInTime!.Value).TotalHours);

        // Overtime
        var overtimeRecords = await _context.OvertimeRecords
            .Where(o => o.EmployeeId == employeeId &&
                       o.Date >= startDate &&
                       o.Date <= endDate)
            .ToListAsync();
        var totalOvertimeHours = overtimeRecords.Sum(o => o.DurationMinutes / 60m);

        var report = new EmployeeAttendanceReportDto
        {
            EmployeeId = employeeId,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            Email = employee.Email,
            PeriodStart = startDate,
            PeriodEnd = endDate,
            TotalShiftsAssigned = shifts.Count,
            ShiftsCompleted = shiftsCompleted,
            ShiftsMissed = shifts.Count - shiftsCompleted,
            AttendanceRate = shifts.Count > 0 ? Math.Round((decimal)shiftsCompleted / shifts.Count * 100, 2) : 0,
            TotalHoursScheduled = Math.Round(totalHoursScheduled, 2),
            TotalHoursWorked = Math.Round(totalHoursWorked, 2),
            TotalOvertimeHours = Math.Round(totalOvertimeHours, 2),
            TotalAnomalies = anomalies.Count,
            LateArrivals = anomalies.Count(a => a.Type == AnomalyType.LateCheckIn),
            EarlyDepartures = anomalies.Count(a => a.Type == AnomalyType.EarlyCheckOut),
            UnauthorizedAbsences = anomalies.Count(a => a.Type == AnomalyType.MissingCheckIn),
            LeaveRequestsApproved = leaveRequests.Count(lr => lr.Status == LeaveRequestStatus.Approved),
            LeaveDaysTaken = leaveRequests
                .Where(lr => lr.Status == LeaveRequestStatus.Approved)
                .Sum(lr => lr.DaysRequested),
            LeaveDaysRemaining = leaveBalances.Sum(lb => lb.TotalDays - lb.UsedDays)
        };

        if (includeDetails)
        {
            report.DailyDetails = await GenerateDailyDetailsAsync(employeeId, startDate, endDate, shifts, anomalies, leaveRequests);
        }

        return report;
    }

    private async Task<List<DailyAttendanceDto>> GenerateDailyDetailsAsync(
        int employeeId,
        DateTime startDate,
        DateTime endDate,
        List<Shared.Models.Shift> shifts,
        List<Shared.Models.ShiftAnomaly> anomalies,
        List<Shared.Models.LeaveRequest> leaveRequests)
    {
        var details = new List<DailyAttendanceDto>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dayShifts = shifts.Where(s => s.Date.Date == date.Date).ToList();
            var dayAnomalies = anomalies.Where(a => a.Shift.Date.Date == date.Date).ToList();
            var dayLeave = leaveRequests.FirstOrDefault(lr =>
                lr.Status == LeaveRequestStatus.Approved &&
                date >= lr.StartDate && date <= lr.EndDate);

            string status = "Absent";
            if (dayLeave != null) status = "Leave";
            else if (dayShifts.Any(s => s.IsCheckedIn)) status = "Present";
            else if (dayShifts.Any()) status = "Scheduled";

            var mainShift = dayShifts.FirstOrDefault();
            var detail = new DailyAttendanceDto
            {
                Date = date,
                Status = status,
                ScheduledStart = mainShift?.StartTime,
                ScheduledEnd = mainShift?.EndTime,
                ActualCheckIn = mainShift?.CheckInTime,
                ActualCheckOut = mainShift?.CheckOutTime,
                HoursScheduled = dayShifts.Sum(s => (decimal)(s.EndTime - s.StartTime).TotalHours - s.BreakDurationMinutes / 60m),
                HoursWorked = dayShifts
                    .Where(s => s.CheckInTime.HasValue && s.CheckOutTime.HasValue)
                    .Sum(s => (decimal)(s.CheckOutTime!.Value - s.CheckInTime!.Value).TotalHours),
                HasAnomaly = dayAnomalies.Any(),
                AnomalyType = dayAnomalies.FirstOrDefault()?.Type.ToString(),
                Notes = dayLeave != null ? $"Ferie: {dayLeave.LeaveType}" : mainShift?.Notes
            };

            details.Add(detail);
        }

        return details;
    }

    public async Task<MerchantSummaryReportDto> GetMerchantSummaryReportAsync(
        int merchantId,
        DateTime startDate,
        DateTime endDate)
    {
        var merchant = await _context.Merchants
            .FirstOrDefaultAsync(m => m.Id == merchantId)
            ?? throw new InvalidOperationException("Merchant non trovato");

        var employees = await _context.Employees
            .Where(e => e.MerchantId == merchantId && e.IsActive)
            .ToListAsync();

        var shifts = await _context.Shifts
            .Include(s => s.ShiftEmployees)
            .Where(s => s.MerchantId == merchantId &&
                       s.Date >= startDate &&
                       s.Date <= endDate &&
                       s.IsActive)
            .ToListAsync();

        var anomalies = await _context.ShiftAnomalies
            .Include(a => a.Shift)
            .Where(a => a.Shift.MerchantId == merchantId &&
                       a.Shift.Date >= startDate &&
                       a.Shift.Date <= endDate)
            .ToListAsync();

        var leaveRequests = await _context.LeaveRequests
            .Where(lr => lr.MerchantId == merchantId &&
                        lr.StartDate <= endDate &&
                        lr.EndDate >= startDate)
            .ToListAsync();

        var bookings = await _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.Service.MerchantId == merchantId &&
                       b.BookingDate >= startDate &&
                       b.BookingDate <= endDate)
            .ToListAsync();

        var totalHoursScheduled = shifts.Sum(s => (decimal)(s.EndTime - s.StartTime).TotalHours - s.BreakDurationMinutes / 60m);
        var totalHoursWorked = shifts
            .Where(s => s.CheckInTime.HasValue && s.CheckOutTime.HasValue)
            .Sum(s => (decimal)(s.CheckOutTime!.Value - s.CheckInTime!.Value).TotalHours);

        var report = new MerchantSummaryReportDto
        {
            MerchantId = merchantId,
            BusinessName = merchant.BusinessName,
            PeriodStart = startDate,
            PeriodEnd = endDate,
            TotalEmployees = await _context.Employees.CountAsync(e => e.MerchantId == merchantId),
            ActiveEmployees = employees.Count,
            TotalShifts = shifts.Count,
            TotalHoursScheduled = Math.Round(totalHoursScheduled, 2),
            TotalHoursWorked = Math.Round(totalHoursWorked, 2),
            AverageAttendanceRate = shifts.Count > 0
                ? Math.Round((decimal)shifts.Count(s => s.IsCheckedOut) / shifts.Count * 100, 2)
                : 0,
            TotalAnomalies = anomalies.Count,
            PendingAnomalies = anomalies.Count(a => !a.IsResolved),
            ResolvedAnomalies = anomalies.Count(a => a.IsResolved),
            TotalLeaveRequests = leaveRequests.Count,
            PendingLeaveRequests = leaveRequests.Count(lr => lr.Status == LeaveRequestStatus.Pending),
            ApprovedLeaveRequests = leaveRequests.Count(lr => lr.Status == LeaveRequestStatus.Approved),
            RejectedLeaveRequests = leaveRequests.Count(lr => lr.Status == LeaveRequestStatus.Rejected),
            TotalBookings = bookings.Count,
            CompletedBookings = bookings.Count(b => b.Status == BookingStatus.Completed),
            CancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled)
        };

        // Riepilogo per dipendente
        foreach (var emp in employees)
        {
            var empShifts = shifts.Where(s => s.ShiftEmployees.Any(se => se.EmployeeId == emp.Id)).ToList();
            var empAnomalies = anomalies.Where(a => empShifts.Any(s => s.Id == a.ShiftId)).ToList();
            var empLeaves = leaveRequests.Where(lr => lr.EmployeeId == emp.Id && lr.Status == LeaveRequestStatus.Approved).ToList();

            var empHoursWorked = empShifts
                .Where(s => s.CheckInTime.HasValue && s.CheckOutTime.HasValue)
                .Sum(s => (decimal)(s.CheckOutTime!.Value - s.CheckInTime!.Value).TotalHours);

            var overtimeRecords = await _context.OvertimeRecords
                .Where(o => o.EmployeeId == emp.Id && o.Date >= startDate && o.Date <= endDate)
                .ToListAsync();

            report.EmployeeSummaries.Add(new EmployeeAttendanceSummaryDto
            {
                EmployeeId = emp.Id,
                EmployeeName = $"{emp.FirstName} {emp.LastName}",
                ShiftsAssigned = empShifts.Count,
                ShiftsCompleted = empShifts.Count(s => s.IsCheckedOut),
                HoursWorked = Math.Round(empHoursWorked, 2),
                OvertimeHours = Math.Round(overtimeRecords.Sum(o => o.DurationMinutes / 60m), 2),
                AttendanceRate = empShifts.Count > 0
                    ? Math.Round((decimal)empShifts.Count(s => s.IsCheckedOut) / empShifts.Count * 100, 2)
                    : 0,
                Anomalies = empAnomalies.Count,
                LeaveDaysTaken = empLeaves.Sum(lr => lr.DaysRequested)
            });
        }

        // Statistiche giornaliere
        report.DailyStats = await GenerateDailyStatsAsync(merchantId, startDate, endDate, shifts, anomalies, bookings, employees);

        // Statistiche settimanali
        report.WeeklyStats = GenerateWeeklyStats(startDate, endDate, shifts, anomalies);

        return report;
    }

    private async Task<List<DailyStatsDto>> GenerateDailyStatsAsync(
        int merchantId,
        DateTime startDate,
        DateTime endDate,
        List<Shared.Models.Shift> shifts,
        List<Shared.Models.ShiftAnomaly> anomalies,
        List<Shared.Models.Booking> bookings,
        List<Shared.Models.Employee> employees)
    {
        var stats = new List<DailyStatsDto>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dayShifts = shifts.Where(s => s.Date.Date == date.Date).ToList();
            var dayAnomalies = anomalies.Where(a => a.Shift.Date.Date == date.Date).ToList();
            var dayBookings = bookings.Where(b => b.BookingDate.Date == date.Date).ToList();

            var employeesPresent = dayShifts
                .Where(s => s.IsCheckedIn)
                .SelectMany(s => s.ShiftEmployees.Select(se => se.EmployeeId))
                .Distinct()
                .Count();

            var employeesOnLeave = await _context.LeaveRequests
                .Where(lr => lr.MerchantId == merchantId &&
                            lr.Status == LeaveRequestStatus.Approved &&
                            date >= lr.StartDate && date <= lr.EndDate)
                .Select(lr => lr.EmployeeId)
                .Distinct()
                .CountAsync();

            stats.Add(new DailyStatsDto
            {
                Date = date,
                DayOfWeek = date.ToString("dddd", new CultureInfo("it-IT")),
                EmployeesPresent = employeesPresent,
                EmployeesAbsent = employees.Count - employeesPresent - employeesOnLeave,
                EmployeesOnLeave = employeesOnLeave,
                TotalHoursWorked = Math.Round(dayShifts
                    .Where(s => s.CheckInTime.HasValue && s.CheckOutTime.HasValue)
                    .Sum(s => (decimal)(s.CheckOutTime!.Value - s.CheckInTime!.Value).TotalHours), 2),
                ShiftsCompleted = dayShifts.Count(s => s.IsCheckedOut),
                Anomalies = dayAnomalies.Count,
                Bookings = dayBookings.Count
            });
        }

        return stats;
    }

    private List<WeeklyStatsDto> GenerateWeeklyStats(
        DateTime startDate,
        DateTime endDate,
        List<Shared.Models.Shift> shifts,
        List<Shared.Models.ShiftAnomaly> anomalies)
    {
        var stats = new List<WeeklyStatsDto>();
        var culture = new CultureInfo("it-IT");
        var calendar = culture.Calendar;

        var currentWeekStart = startDate.AddDays(-(int)startDate.DayOfWeek + 1);
        while (currentWeekStart <= endDate)
        {
            var weekEnd = currentWeekStart.AddDays(6);
            var weekShifts = shifts.Where(s => s.Date >= currentWeekStart && s.Date <= weekEnd).ToList();
            var weekAnomalies = anomalies.Where(a => a.Shift.Date >= currentWeekStart && a.Shift.Date <= weekEnd).ToList();

            var totalHoursScheduled = weekShifts.Sum(s => (decimal)(s.EndTime - s.StartTime).TotalHours - s.BreakDurationMinutes / 60m);
            var totalHoursWorked = weekShifts
                .Where(s => s.CheckInTime.HasValue && s.CheckOutTime.HasValue)
                .Sum(s => (decimal)(s.CheckOutTime!.Value - s.CheckInTime!.Value).TotalHours);

            stats.Add(new WeeklyStatsDto
            {
                WeekNumber = calendar.GetWeekOfYear(currentWeekStart, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday),
                WeekStart = currentWeekStart,
                WeekEnd = weekEnd,
                TotalHoursWorked = Math.Round(totalHoursWorked, 2),
                TotalHoursScheduled = Math.Round(totalHoursScheduled, 2),
                ShiftsCompleted = weekShifts.Count(s => s.IsCheckedOut),
                TotalAnomalies = weekAnomalies.Count,
                AverageAttendanceRate = weekShifts.Count > 0
                    ? Math.Round((decimal)weekShifts.Count(s => s.IsCheckedOut) / weekShifts.Count * 100, 2)
                    : 0
            });

            currentWeekStart = currentWeekStart.AddDays(7);
        }

        return stats;
    }

    public async Task<AdminGlobalReportDto> GetAdminGlobalReportAsync(
        DateTime startDate,
        DateTime endDate)
    {
        var merchants = await _context.Merchants.ToListAsync();
        var employees = await _context.Employees.ToListAsync();
        var consumers = await _context.Users.Where(u => u.IsConsumer).CountAsync();

        var shifts = await _context.Shifts
            .Where(s => s.Date >= startDate && s.Date <= endDate && s.IsActive)
            .ToListAsync();

        var bookings = await _context.Bookings
            .Where(b => b.BookingDate >= startDate && b.BookingDate <= endDate)
            .ToListAsync();

        var totalHoursWorked = shifts
            .Where(s => s.CheckInTime.HasValue && s.CheckOutTime.HasValue)
            .Sum(s => (decimal)(s.CheckOutTime!.Value - s.CheckInTime!.Value).TotalHours);

        var report = new AdminGlobalReportDto
        {
            PeriodStart = startDate,
            PeriodEnd = endDate,
            GeneratedAt = DateTime.UtcNow,
            TotalMerchants = merchants.Count,
            ActiveMerchants = merchants.Count(m => m.IsApproved),
            TotalEmployees = employees.Count,
            TotalConsumers = consumers,
            TotalBookings = bookings.Count,
            TotalShifts = shifts.Count,
            TotalHoursWorked = Math.Round(totalHoursWorked, 2)
        };

        // Top merchants per attività
        var merchantStats = new List<MerchantActivityDto>();
        foreach (var m in merchants.Where(m => m.IsApproved).Take(10))
        {
            var mShifts = shifts.Where(s => s.MerchantId == m.Id).ToList();
            var mBookings = await _context.Bookings
                .Include(b => b.Service)
                .Where(b => b.Service.MerchantId == m.Id &&
                           b.BookingDate >= startDate &&
                           b.BookingDate <= endDate)
                .CountAsync();

            var mHoursWorked = mShifts
                .Where(s => s.CheckInTime.HasValue && s.CheckOutTime.HasValue)
                .Sum(s => (decimal)(s.CheckOutTime!.Value - s.CheckInTime!.Value).TotalHours);

            merchantStats.Add(new MerchantActivityDto
            {
                MerchantId = m.Id,
                BusinessName = m.BusinessName,
                EmployeeCount = employees.Count(e => e.MerchantId == m.Id),
                BookingCount = mBookings,
                TotalHoursWorked = Math.Round(mHoursWorked, 2),
                AverageAttendanceRate = mShifts.Count > 0
                    ? Math.Round((decimal)mShifts.Count(s => s.IsCheckedOut) / mShifts.Count * 100, 2)
                    : 0
            });
        }

        report.TopMerchants = merchantStats
            .OrderByDescending(m => m.BookingCount)
            .Take(10)
            .ToList();

        // Trend mensili
        report.MonthlyTrends = await GenerateMonthlyTrendsAsync(startDate, endDate);

        return report;
    }

    private async Task<List<MonthlyTrendDto>> GenerateMonthlyTrendsAsync(DateTime startDate, DateTime endDate)
    {
        var trends = new List<MonthlyTrendDto>();
        var culture = new CultureInfo("it-IT");

        var currentMonth = new DateTime(startDate.Year, startDate.Month, 1);
        while (currentMonth <= endDate)
        {
            var monthEnd = currentMonth.AddMonths(1).AddDays(-1);

            var newMerchants = await _context.Merchants
                .Where(m => m.CreatedAt >= currentMonth && m.CreatedAt <= monthEnd)
                .CountAsync();

            var newEmployees = await _context.Employees
                .Where(e => e.CreatedAt >= currentMonth && e.CreatedAt <= monthEnd)
                .CountAsync();

            var monthBookings = await _context.Bookings
                .Where(b => b.BookingDate >= currentMonth && b.BookingDate <= monthEnd)
                .CountAsync();

            var monthShifts = await _context.Shifts
                .Where(s => s.Date >= currentMonth && s.Date <= monthEnd &&
                           s.CheckInTime.HasValue && s.CheckOutTime.HasValue)
                .ToListAsync();

            var monthHours = monthShifts
                .Sum(s => (decimal)(s.CheckOutTime!.Value - s.CheckInTime!.Value).TotalHours);

            trends.Add(new MonthlyTrendDto
            {
                Year = currentMonth.Year,
                Month = currentMonth.Month,
                MonthName = currentMonth.ToString("MMMM yyyy", culture),
                NewMerchants = newMerchants,
                NewEmployees = newEmployees,
                TotalBookings = monthBookings,
                TotalHoursWorked = Math.Round(monthHours, 2)
            });

            currentMonth = currentMonth.AddMonths(1);
        }

        return trends;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(int? merchantId = null)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);

        IQueryable<Shared.Models.Shift> shiftsQuery = _context.Shifts.Where(s => s.IsActive);
        IQueryable<Shared.Models.ShiftAnomaly> anomaliesQuery = _context.ShiftAnomalies;
        IQueryable<Shared.Models.Booking> bookingsQuery = _context.Bookings;
        IQueryable<Shared.Models.LeaveRequest> leaveQuery = _context.LeaveRequests;

        if (merchantId.HasValue)
        {
            shiftsQuery = shiftsQuery.Where(s => s.MerchantId == merchantId);
            anomaliesQuery = anomaliesQuery.Where(a => a.Shift.MerchantId == merchantId);
            bookingsQuery = bookingsQuery.Where(b => b.Service.MerchantId == merchantId);
            leaveQuery = leaveQuery.Where(lr => lr.MerchantId == merchantId);
        }

        // Oggi
        var todayShifts = await shiftsQuery.Where(s => s.Date == today).ToListAsync();
        var todayAnomalies = await anomaliesQuery
            .Where(a => a.Shift.Date == today && !a.IsResolved)
            .CountAsync();
        var todayBookings = await bookingsQuery.Where(b => b.BookingDate == today).CountAsync();

        // Questa settimana
        var weekShifts = await shiftsQuery
            .Where(s => s.Date >= weekStart && s.Date <= today)
            .ToListAsync();
        var weekAnomalies = await anomaliesQuery
            .Where(a => a.Shift.Date >= weekStart && a.Shift.Date <= today)
            .CountAsync();
        var weekLeaves = await leaveQuery
            .Where(lr => lr.CreatedAt >= weekStart && lr.CreatedAt <= today)
            .CountAsync();

        // Questo mese
        var monthShifts = await shiftsQuery
            .Where(s => s.Date >= monthStart && s.Date <= today)
            .ToListAsync();

        // Mese scorso (per confronto)
        var lastMonthShifts = await shiftsQuery
            .Where(s => s.Date >= lastMonthStart && s.Date < monthStart)
            .ToListAsync();
        var lastMonthBookings = await bookingsQuery
            .Where(b => b.BookingDate >= lastMonthStart && b.BookingDate < monthStart)
            .CountAsync();

        // Calcoli ore
        decimal weekHoursWorked = weekShifts
            .Where(s => s.CheckInTime.HasValue && s.CheckOutTime.HasValue)
            .Sum(s => (decimal)(s.CheckOutTime!.Value - s.CheckInTime!.Value).TotalHours);

        decimal weekHoursScheduled = weekShifts
            .Sum(s => (decimal)(s.EndTime - s.StartTime).TotalHours - s.BreakDurationMinutes / 60m);

        decimal monthHoursWorked = monthShifts
            .Where(s => s.CheckInTime.HasValue && s.CheckOutTime.HasValue)
            .Sum(s => (decimal)(s.CheckOutTime!.Value - s.CheckInTime!.Value).TotalHours);

        decimal lastMonthHoursWorked = lastMonthShifts
            .Where(s => s.CheckInTime.HasValue && s.CheckOutTime.HasValue)
            .Sum(s => (decimal)(s.CheckOutTime!.Value - s.CheckInTime!.Value).TotalHours);

        // Attendance rate
        var monthAttendanceRate = monthShifts.Count > 0
            ? (decimal)monthShifts.Count(s => s.IsCheckedOut) / monthShifts.Count * 100
            : 0;
        var lastMonthAttendanceRate = lastMonthShifts.Count > 0
            ? (decimal)lastMonthShifts.Count(s => s.IsCheckedOut) / lastMonthShifts.Count * 100
            : 0;

        // Trend percentuali
        decimal hoursTrend = lastMonthHoursWorked > 0
            ? ((monthHoursWorked - lastMonthHoursWorked) / lastMonthHoursWorked) * 100
            : 0;
        decimal attendanceTrend = lastMonthAttendanceRate > 0
            ? monthAttendanceRate - lastMonthAttendanceRate
            : 0;

        var monthBookingsCount = await bookingsQuery
            .Where(b => b.BookingDate >= monthStart && b.BookingDate <= today)
            .CountAsync();
        decimal bookingsTrend = lastMonthBookings > 0
            ? ((decimal)(monthBookingsCount - lastMonthBookings) / lastMonthBookings) * 100
            : 0;

        // Prossimi eventi
        var upcomingShifts = await shiftsQuery
            .Include(s => s.ShiftEmployees)
            .ThenInclude(se => se.Employee)
            .Where(s => s.Date > today && s.Date <= today.AddDays(7))
            .OrderBy(s => s.Date)
            .Take(5)
            .ToListAsync();

        var upcomingLeaves = await leaveQuery
            .Include(lr => lr.Employee)
            .Where(lr => lr.Status == LeaveRequestStatus.Approved &&
                        lr.StartDate > today && lr.StartDate <= today.AddDays(14))
            .OrderBy(lr => lr.StartDate)
            .Take(5)
            .ToListAsync();

        var upcomingEvents = new List<UpcomingEventDto>();
        foreach (var shift in upcomingShifts)
        {
            var firstEmp = shift.ShiftEmployees.FirstOrDefault()?.Employee;
            upcomingEvents.Add(new UpcomingEventDto
            {
                EventType = "shift",
                Date = shift.Date,
                Title = $"Turno {shift.StartTime:hh\\:mm}-{shift.EndTime:hh\\:mm}",
                Description = shift.Notes ?? "",
                EmployeeId = firstEmp?.Id,
                EmployeeName = firstEmp != null ? $"{firstEmp.FirstName} {firstEmp.LastName}" : null
            });
        }
        foreach (var leave in upcomingLeaves)
        {
            upcomingEvents.Add(new UpcomingEventDto
            {
                EventType = "leave",
                Date = leave.StartDate,
                Title = $"Ferie: {leave.LeaveType}",
                Description = $"Dal {leave.StartDate:dd/MM} al {leave.EndDate:dd/MM}",
                EmployeeId = leave.EmployeeId,
                EmployeeName = $"{leave.Employee.FirstName} {leave.Employee.LastName}"
            });
        }

        return new DashboardStatsDto
        {
            GeneratedAt = DateTime.UtcNow,
            TodayShifts = todayShifts.Count,
            TodayEmployeesPresent = todayShifts.Count(s => s.IsCheckedIn),
            TodayEmployeesAbsent = todayShifts.Count(s => !s.IsCheckedIn),
            TodayPendingAnomalies = todayAnomalies,
            TodayBookings = todayBookings,
            WeekHoursWorked = Math.Round(weekHoursWorked, 2),
            WeekHoursScheduled = Math.Round(weekHoursScheduled, 2),
            WeekAnomalies = weekAnomalies,
            WeekLeaveRequests = weekLeaves,
            MonthHoursWorked = Math.Round(monthHoursWorked, 2),
            MonthAttendanceRate = Math.Round(monthAttendanceRate, 2),
            MonthTotalShifts = monthShifts.Count,
            HoursWorkedTrend = Math.Round(hoursTrend, 2),
            AttendanceRateTrend = Math.Round(attendanceTrend, 2),
            BookingsTrend = Math.Round(bookingsTrend, 2),
            UpcomingEvents = upcomingEvents.OrderBy(e => e.Date).Take(10).ToList()
        };
    }

    public async Task<DashboardStatsDto> GetEmployeeDashboardStatsAsync(int employeeId)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId)
            ?? throw new InvalidOperationException("Dipendente non trovato");

        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        // Query filtrate per dipendente
        var myShiftIds = await _context.ShiftEmployees
            .Where(se => se.EmployeeId == employeeId)
            .Select(se => se.ShiftId)
            .ToListAsync();

        var allMyShifts = await _context.Shifts
            .Where(s => myShiftIds.Contains(s.Id) && s.IsActive)
            .ToListAsync();

        // Oggi
        var todayShifts = allMyShifts.Where(s => s.Date == today).ToList();

        // Questa settimana
        var weekShifts = allMyShifts.Where(s => s.Date >= weekStart && s.Date <= today).ToList();

        // Questo mese
        var monthShifts = allMyShifts.Where(s => s.Date >= monthStart && s.Date <= today).ToList();

        // Anomalie
        var myAnomalies = await _context.ShiftAnomalies
            .Where(a => myShiftIds.Contains(a.ShiftId))
            .ToListAsync();

        var todayAnomalies = myAnomalies.Count(a => allMyShifts.Any(s => s.Id == a.ShiftId && s.Date == today) && !a.IsResolved);
        var weekAnomalies = myAnomalies.Count(a => allMyShifts.Any(s => s.Id == a.ShiftId && s.Date >= weekStart && s.Date <= today));

        // Ferie
        var weekLeaves = await _context.LeaveRequests
            .Where(lr => lr.EmployeeId == employeeId &&
                        lr.CreatedAt >= weekStart && lr.CreatedAt <= today)
            .CountAsync();

        // Calcoli ore
        decimal weekHoursWorked = weekShifts
            .Where(s => s.CheckInTime.HasValue && s.CheckOutTime.HasValue)
            .Sum(s => (decimal)(s.CheckOutTime!.Value - s.CheckInTime!.Value).TotalHours);

        decimal weekHoursScheduled = weekShifts
            .Sum(s => (decimal)(s.EndTime - s.StartTime).TotalHours - s.BreakDurationMinutes / 60m);

        decimal monthHoursWorked = monthShifts
            .Where(s => s.CheckInTime.HasValue && s.CheckOutTime.HasValue)
            .Sum(s => (decimal)(s.CheckOutTime!.Value - s.CheckInTime!.Value).TotalHours);

        var monthAttendanceRate = monthShifts.Count > 0
            ? (decimal)monthShifts.Count(s => s.IsCheckedOut) / monthShifts.Count * 100
            : 0;

        // Prossimi turni
        var upcomingShifts = allMyShifts
            .Where(s => s.Date > today && s.Date <= today.AddDays(7))
            .OrderBy(s => s.Date)
            .Take(5)
            .Select(s => new UpcomingEventDto
            {
                EventType = "shift",
                Date = s.Date,
                Title = $"Turno {s.StartTime:hh\\:mm}-{s.EndTime:hh\\:mm}",
                Description = s.Notes ?? ""
            })
            .ToList();

        return new DashboardStatsDto
        {
            GeneratedAt = DateTime.UtcNow,
            TodayShifts = todayShifts.Count,
            TodayEmployeesPresent = todayShifts.Count(s => s.IsCheckedIn),
            TodayEmployeesAbsent = todayShifts.Count(s => !s.IsCheckedIn),
            TodayPendingAnomalies = todayAnomalies,
            TodayBookings = 0,
            WeekHoursWorked = Math.Round(weekHoursWorked, 2),
            WeekHoursScheduled = Math.Round(weekHoursScheduled, 2),
            WeekAnomalies = weekAnomalies,
            WeekLeaveRequests = weekLeaves,
            MonthHoursWorked = Math.Round(monthHoursWorked, 2),
            MonthAttendanceRate = Math.Round(monthAttendanceRate, 2),
            MonthTotalShifts = monthShifts.Count,
            HoursWorkedTrend = 0,
            AttendanceRateTrend = 0,
            BookingsTrend = 0,
            UpcomingEvents = upcomingShifts
        };
    }

    public async Task<byte[]> ExportToPdfAsync(ReportExportRequest request)
    {
        // Fetch data asynchronously BEFORE PDF generation
        EmployeeAttendanceReportDto? attendanceReport = null;
        MerchantSummaryReportDto? merchantReport = null;

        if (request.ReportType == "attendance" && request.EmployeeId.HasValue)
        {
            attendanceReport = await GetEmployeeAttendanceReportAsync(
                request.EmployeeId.Value,
                request.StartDate,
                request.EndDate,
                request.IncludeDetails);
        }
        else if (request.ReportType == "merchant-summary" && request.MerchantId.HasValue)
        {
            merchantReport = await GetMerchantSummaryReportAsync(
                request.MerchantId.Value,
                request.StartDate,
                request.EndDate);
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, request, attendanceReport, merchantReport));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Pagina ");
                    x.CurrentPageNumber();
                    x.Span(" di ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Report Presenze/Assenze")
                    .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                column.Item().Text($"Generato il: {DateTime.UtcNow:dd/MM/yyyy HH:mm}")
                    .FontSize(10).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private void ComposeContent(
        IContainer container,
        ReportExportRequest request,
        EmployeeAttendanceReportDto? attendanceReport,
        MerchantSummaryReportDto? merchantReport)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            if (request.ReportType == "attendance" && attendanceReport != null)
            {
                // Info dipendente
                column.Item().Text($"Dipendente: {attendanceReport.EmployeeName}").FontSize(14).Bold();
                column.Item().Text($"Periodo: {attendanceReport.PeriodStart:dd/MM/yyyy} - {attendanceReport.PeriodEnd:dd/MM/yyyy}");
                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Statistiche
                column.Item().PaddingTop(10).Text("Riepilogo").FontSize(12).Bold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Cell().Text("Turni Assegnati:").Bold();
                    table.Cell().Text(attendanceReport.TotalShiftsAssigned.ToString());
                    table.Cell().Text("Turni Completati:").Bold();
                    table.Cell().Text(attendanceReport.ShiftsCompleted.ToString());
                    table.Cell().Text("Tasso Presenza:").Bold();
                    table.Cell().Text($"{attendanceReport.AttendanceRate}%");
                    table.Cell().Text("Ore Lavorate:").Bold();
                    table.Cell().Text($"{attendanceReport.TotalHoursWorked}h");
                    table.Cell().Text("Straordinari:").Bold();
                    table.Cell().Text($"{attendanceReport.TotalOvertimeHours}h");
                    table.Cell().Text("Anomalie:").Bold();
                    table.Cell().Text(attendanceReport.TotalAnomalies.ToString());
                });

                if (request.IncludeDetails && attendanceReport.DailyDetails.Any())
                {
                    column.Item().PaddingTop(15).Text("Dettaglio Giornaliero").FontSize(12).Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(50);
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Data").Bold();
                            header.Cell().Text("Stato").Bold();
                            header.Cell().Text("Ingresso").Bold();
                            header.Cell().Text("Uscita").Bold();
                            header.Cell().Text("Ore").Bold();
                            header.Cell().Text("Note").Bold();
                        });

                        foreach (var day in attendanceReport.DailyDetails.Take(31))
                        {
                            table.Cell().Text(day.Date.ToString("dd/MM"));
                            table.Cell().Text(day.Status);
                            table.Cell().Text(day.ActualCheckIn?.ToString("HH:mm") ?? "-");
                            table.Cell().Text(day.ActualCheckOut?.ToString("HH:mm") ?? "-");
                            table.Cell().Text($"{day.HoursWorked:F1}");
                            table.Cell().Text(day.Notes ?? "");
                        }
                    });
                }
            }
            else if (request.ReportType == "merchant-summary" && merchantReport != null)
            {
                column.Item().Text($"Azienda: {merchantReport.BusinessName}").FontSize(14).Bold();
                column.Item().Text($"Periodo: {merchantReport.PeriodStart:dd/MM/yyyy} - {merchantReport.PeriodEnd:dd/MM/yyyy}");
                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Statistiche generali
                column.Item().PaddingTop(10).Text("Statistiche Generali").FontSize(12).Bold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Cell().Text("Dipendenti Attivi:").Bold();
                    table.Cell().Text(merchantReport.ActiveEmployees.ToString());
                    table.Cell().Text("Turni Totali:").Bold();
                    table.Cell().Text(merchantReport.TotalShifts.ToString());
                    table.Cell().Text("Ore Programmate:").Bold();
                    table.Cell().Text($"{merchantReport.TotalHoursScheduled}h");
                    table.Cell().Text("Ore Lavorate:").Bold();
                    table.Cell().Text($"{merchantReport.TotalHoursWorked}h");
                    table.Cell().Text("Tasso Presenza:").Bold();
                    table.Cell().Text($"{merchantReport.AverageAttendanceRate}%");
                    table.Cell().Text("Anomalie:").Bold();
                    table.Cell().Text(merchantReport.TotalAnomalies.ToString());
                });

                // Riepilogo dipendenti
                if (merchantReport.EmployeeSummaries.Any())
                {
                    column.Item().PaddingTop(15).Text("Riepilogo per Dipendente").FontSize(12).Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(40);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Dipendente").Bold();
                            header.Cell().Text("Turni").Bold();
                            header.Cell().Text("Compl.").Bold();
                            header.Cell().Text("Ore").Bold();
                            header.Cell().Text("%").Bold();
                            header.Cell().Text("Anom.").Bold();
                        });

                        foreach (var emp in merchantReport.EmployeeSummaries)
                        {
                            table.Cell().Text(emp.EmployeeName);
                            table.Cell().Text(emp.ShiftsAssigned.ToString());
                            table.Cell().Text(emp.ShiftsCompleted.ToString());
                            table.Cell().Text($"{emp.HoursWorked:F1}");
                            table.Cell().Text($"{emp.AttendanceRate:F0}%");
                            table.Cell().Text(emp.Anomalies.ToString());
                        }
                    });
                }
            }
        });
    }

    public async Task<byte[]> ExportToExcelAsync(ReportExportRequest request)
    {
        using var workbook = new XLWorkbook();

        if (request.ReportType == "attendance" && request.EmployeeId.HasValue)
        {
            var report = await GetEmployeeAttendanceReportAsync(
                request.EmployeeId.Value,
                request.StartDate,
                request.EndDate,
                request.IncludeDetails);

            var ws = workbook.Worksheets.Add("Report Presenze");

            // Header
            ws.Cell(1, 1).Value = "Report Presenze Dipendente";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;

            ws.Cell(2, 1).Value = $"Dipendente: {report.EmployeeName}";
            ws.Cell(3, 1).Value = $"Periodo: {report.PeriodStart:dd/MM/yyyy} - {report.PeriodEnd:dd/MM/yyyy}";

            // Statistiche
            int row = 5;
            ws.Cell(row, 1).Value = "Statistica";
            ws.Cell(row, 2).Value = "Valore";
            ws.Range(row, 1, row, 2).Style.Font.Bold = true;
            ws.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.LightBlue;

            row++;
            ws.Cell(row, 1).Value = "Turni Assegnati";
            ws.Cell(row, 2).Value = report.TotalShiftsAssigned;
            row++;
            ws.Cell(row, 1).Value = "Turni Completati";
            ws.Cell(row, 2).Value = report.ShiftsCompleted;
            row++;
            ws.Cell(row, 1).Value = "Tasso Presenza (%)";
            ws.Cell(row, 2).Value = report.AttendanceRate;
            row++;
            ws.Cell(row, 1).Value = "Ore Programmate";
            ws.Cell(row, 2).Value = report.TotalHoursScheduled;
            row++;
            ws.Cell(row, 1).Value = "Ore Lavorate";
            ws.Cell(row, 2).Value = report.TotalHoursWorked;
            row++;
            ws.Cell(row, 1).Value = "Ore Straordinario";
            ws.Cell(row, 2).Value = report.TotalOvertimeHours;
            row++;
            ws.Cell(row, 1).Value = "Anomalie Totali";
            ws.Cell(row, 2).Value = report.TotalAnomalies;
            row++;
            ws.Cell(row, 1).Value = "Ritardi";
            ws.Cell(row, 2).Value = report.LateArrivals;
            row++;
            ws.Cell(row, 1).Value = "Uscite Anticipate";
            ws.Cell(row, 2).Value = report.EarlyDepartures;

            // Dettaglio giornaliero
            if (request.IncludeDetails && report.DailyDetails.Any())
            {
                row += 2;
                ws.Cell(row, 1).Value = "Dettaglio Giornaliero";
                ws.Cell(row, 1).Style.Font.Bold = true;
                row++;

                ws.Cell(row, 1).Value = "Data";
                ws.Cell(row, 2).Value = "Stato";
                ws.Cell(row, 3).Value = "Ingresso Previsto";
                ws.Cell(row, 4).Value = "Uscita Prevista";
                ws.Cell(row, 5).Value = "Ingresso Effettivo";
                ws.Cell(row, 6).Value = "Uscita Effettiva";
                ws.Cell(row, 7).Value = "Ore Programmate";
                ws.Cell(row, 8).Value = "Ore Lavorate";
                ws.Cell(row, 9).Value = "Anomalia";
                ws.Cell(row, 10).Value = "Note";
                ws.Range(row, 1, row, 10).Style.Font.Bold = true;
                ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.LightGray;

                foreach (var day in report.DailyDetails)
                {
                    row++;
                    ws.Cell(row, 1).Value = day.Date.ToString("dd/MM/yyyy");
                    ws.Cell(row, 2).Value = day.Status;
                    ws.Cell(row, 3).Value = day.ScheduledStart?.ToString(@"hh\:mm") ?? "-";
                    ws.Cell(row, 4).Value = day.ScheduledEnd?.ToString(@"hh\:mm") ?? "-";
                    ws.Cell(row, 5).Value = day.ActualCheckIn?.ToString("HH:mm") ?? "-";
                    ws.Cell(row, 6).Value = day.ActualCheckOut?.ToString("HH:mm") ?? "-";
                    ws.Cell(row, 7).Value = day.HoursScheduled;
                    ws.Cell(row, 8).Value = day.HoursWorked;
                    ws.Cell(row, 9).Value = day.HasAnomaly ? day.AnomalyType : "-";
                    ws.Cell(row, 10).Value = day.Notes ?? "";
                }
            }

            ws.Columns().AdjustToContents();
        }
        else if (request.ReportType == "merchant-summary" && request.MerchantId.HasValue)
        {
            var report = await GetMerchantSummaryReportAsync(
                request.MerchantId.Value,
                request.StartDate,
                request.EndDate);

            var ws = workbook.Worksheets.Add("Report Merchant");

            ws.Cell(1, 1).Value = $"Report Riepilogativo - {report.BusinessName}";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(2, 1).Value = $"Periodo: {report.PeriodStart:dd/MM/yyyy} - {report.PeriodEnd:dd/MM/yyyy}";

            int row = 4;
            // Statistiche generali
            ws.Cell(row, 1).Value = "Statistica";
            ws.Cell(row, 2).Value = "Valore";
            ws.Range(row, 1, row, 2).Style.Font.Bold = true;
            ws.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.LightBlue;

            row++;
            ws.Cell(row, 1).Value = "Dipendenti Totali";
            ws.Cell(row, 2).Value = report.TotalEmployees;
            row++;
            ws.Cell(row, 1).Value = "Dipendenti Attivi";
            ws.Cell(row, 2).Value = report.ActiveEmployees;
            row++;
            ws.Cell(row, 1).Value = "Turni Totali";
            ws.Cell(row, 2).Value = report.TotalShifts;
            row++;
            ws.Cell(row, 1).Value = "Ore Programmate";
            ws.Cell(row, 2).Value = report.TotalHoursScheduled;
            row++;
            ws.Cell(row, 1).Value = "Ore Lavorate";
            ws.Cell(row, 2).Value = report.TotalHoursWorked;
            row++;
            ws.Cell(row, 1).Value = "Tasso Presenza Medio (%)";
            ws.Cell(row, 2).Value = report.AverageAttendanceRate;
            row++;
            ws.Cell(row, 1).Value = "Anomalie Totali";
            ws.Cell(row, 2).Value = report.TotalAnomalies;
            row++;
            ws.Cell(row, 1).Value = "Anomalie Pendenti";
            ws.Cell(row, 2).Value = report.PendingAnomalies;
            row++;
            ws.Cell(row, 1).Value = "Richieste Ferie Totali";
            ws.Cell(row, 2).Value = report.TotalLeaveRequests;
            row++;
            ws.Cell(row, 1).Value = "Prenotazioni Totali";
            ws.Cell(row, 2).Value = report.TotalBookings;

            // Riepilogo dipendenti
            row += 2;
            ws.Cell(row, 1).Value = "Riepilogo Dipendenti";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            ws.Cell(row, 1).Value = "Dipendente";
            ws.Cell(row, 2).Value = "Turni Assegnati";
            ws.Cell(row, 3).Value = "Turni Completati";
            ws.Cell(row, 4).Value = "Ore Lavorate";
            ws.Cell(row, 5).Value = "Ore Straordinario";
            ws.Cell(row, 6).Value = "Tasso Presenza (%)";
            ws.Cell(row, 7).Value = "Anomalie";
            ws.Cell(row, 8).Value = "Giorni Ferie";
            ws.Range(row, 1, row, 8).Style.Font.Bold = true;
            ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.LightGray;

            foreach (var emp in report.EmployeeSummaries)
            {
                row++;
                ws.Cell(row, 1).Value = emp.EmployeeName;
                ws.Cell(row, 2).Value = emp.ShiftsAssigned;
                ws.Cell(row, 3).Value = emp.ShiftsCompleted;
                ws.Cell(row, 4).Value = emp.HoursWorked;
                ws.Cell(row, 5).Value = emp.OvertimeHours;
                ws.Cell(row, 6).Value = emp.AttendanceRate;
                ws.Cell(row, 7).Value = emp.Anomalies;
                ws.Cell(row, 8).Value = emp.LeaveDaysTaken;
            }

            ws.Columns().AdjustToContents();

            // Foglio statistiche giornaliere
            if (report.DailyStats.Any())
            {
                var wsDaily = workbook.Worksheets.Add("Statistiche Giornaliere");
                wsDaily.Cell(1, 1).Value = "Data";
                wsDaily.Cell(1, 2).Value = "Giorno";
                wsDaily.Cell(1, 3).Value = "Presenti";
                wsDaily.Cell(1, 4).Value = "Assenti";
                wsDaily.Cell(1, 5).Value = "In Ferie";
                wsDaily.Cell(1, 6).Value = "Ore Lavorate";
                wsDaily.Cell(1, 7).Value = "Turni Completati";
                wsDaily.Cell(1, 8).Value = "Anomalie";
                wsDaily.Cell(1, 9).Value = "Prenotazioni";
                wsDaily.Range(1, 1, 1, 9).Style.Font.Bold = true;
                wsDaily.Range(1, 1, 1, 9).Style.Fill.BackgroundColor = XLColor.LightBlue;

                int dailyRow = 2;
                foreach (var day in report.DailyStats)
                {
                    wsDaily.Cell(dailyRow, 1).Value = day.Date.ToString("dd/MM/yyyy");
                    wsDaily.Cell(dailyRow, 2).Value = day.DayOfWeek;
                    wsDaily.Cell(dailyRow, 3).Value = day.EmployeesPresent;
                    wsDaily.Cell(dailyRow, 4).Value = day.EmployeesAbsent;
                    wsDaily.Cell(dailyRow, 5).Value = day.EmployeesOnLeave;
                    wsDaily.Cell(dailyRow, 6).Value = day.TotalHoursWorked;
                    wsDaily.Cell(dailyRow, 7).Value = day.ShiftsCompleted;
                    wsDaily.Cell(dailyRow, 8).Value = day.Anomalies;
                    wsDaily.Cell(dailyRow, 9).Value = day.Bookings;
                    dailyRow++;
                }

                wsDaily.Columns().AdjustToContents();
            }
        }
        else if (request.ReportType == "admin-global")
        {
            var report = await GetAdminGlobalReportAsync(request.StartDate, request.EndDate);

            var ws = workbook.Worksheets.Add("Report Globale Admin");

            ws.Cell(1, 1).Value = "Report Globale Amministrazione";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(2, 1).Value = $"Periodo: {report.PeriodStart:dd/MM/yyyy} - {report.PeriodEnd:dd/MM/yyyy}";
            ws.Cell(3, 1).Value = $"Generato: {report.GeneratedAt:dd/MM/yyyy HH:mm}";

            int row = 5;
            ws.Cell(row, 1).Value = "Statistica";
            ws.Cell(row, 2).Value = "Valore";
            ws.Range(row, 1, row, 2).Style.Font.Bold = true;
            ws.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.LightBlue;

            row++;
            ws.Cell(row, 1).Value = "Merchant Totali";
            ws.Cell(row, 2).Value = report.TotalMerchants;
            row++;
            ws.Cell(row, 1).Value = "Merchant Attivi";
            ws.Cell(row, 2).Value = report.ActiveMerchants;
            row++;
            ws.Cell(row, 1).Value = "Dipendenti Totali";
            ws.Cell(row, 2).Value = report.TotalEmployees;
            row++;
            ws.Cell(row, 1).Value = "Consumatori Totali";
            ws.Cell(row, 2).Value = report.TotalConsumers;
            row++;
            ws.Cell(row, 1).Value = "Prenotazioni Totali";
            ws.Cell(row, 2).Value = report.TotalBookings;
            row++;
            ws.Cell(row, 1).Value = "Turni Totali";
            ws.Cell(row, 2).Value = report.TotalShifts;
            row++;
            ws.Cell(row, 1).Value = "Ore Lavorate Totali";
            ws.Cell(row, 2).Value = report.TotalHoursWorked;

            // Top Merchants
            if (report.TopMerchants.Any())
            {
                row += 2;
                ws.Cell(row, 1).Value = "Top Merchant per Attività";
                ws.Cell(row, 1).Style.Font.Bold = true;
                row++;

                ws.Cell(row, 1).Value = "Merchant";
                ws.Cell(row, 2).Value = "Dipendenti";
                ws.Cell(row, 3).Value = "Prenotazioni";
                ws.Cell(row, 4).Value = "Ore Lavorate";
                ws.Cell(row, 5).Value = "Tasso Presenza (%)";
                ws.Range(row, 1, row, 5).Style.Font.Bold = true;
                ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.LightGray;

                foreach (var m in report.TopMerchants)
                {
                    row++;
                    ws.Cell(row, 1).Value = m.BusinessName;
                    ws.Cell(row, 2).Value = m.EmployeeCount;
                    ws.Cell(row, 3).Value = m.BookingCount;
                    ws.Cell(row, 4).Value = m.TotalHoursWorked;
                    ws.Cell(row, 5).Value = m.AverageAttendanceRate;
                }
            }

            ws.Columns().AdjustToContents();

            // Foglio trend mensili
            if (report.MonthlyTrends.Any())
            {
                var wsTrends = workbook.Worksheets.Add("Trend Mensili");
                wsTrends.Cell(1, 1).Value = "Mese";
                wsTrends.Cell(1, 2).Value = "Nuovi Merchant";
                wsTrends.Cell(1, 3).Value = "Nuovi Dipendenti";
                wsTrends.Cell(1, 4).Value = "Prenotazioni";
                wsTrends.Cell(1, 5).Value = "Ore Lavorate";
                wsTrends.Range(1, 1, 1, 5).Style.Font.Bold = true;
                wsTrends.Range(1, 1, 1, 5).Style.Fill.BackgroundColor = XLColor.LightBlue;

                int trendRow = 2;
                foreach (var trend in report.MonthlyTrends)
                {
                    wsTrends.Cell(trendRow, 1).Value = trend.MonthName;
                    wsTrends.Cell(trendRow, 2).Value = trend.NewMerchants;
                    wsTrends.Cell(trendRow, 3).Value = trend.NewEmployees;
                    wsTrends.Cell(trendRow, 4).Value = trend.TotalBookings;
                    wsTrends.Cell(trendRow, 5).Value = trend.TotalHoursWorked;
                    trendRow++;
                }

                wsTrends.Columns().AdjustToContents();
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportToCsvAsync(ReportExportRequest request)
    {
        var sb = new StringBuilder();

        if (request.ReportType == "attendance" && request.EmployeeId.HasValue)
        {
            var report = await GetEmployeeAttendanceReportAsync(
                request.EmployeeId.Value,
                request.StartDate,
                request.EndDate,
                request.IncludeDetails);

            sb.AppendLine("Report Presenze Dipendente");
            sb.AppendLine($"Dipendente;{report.EmployeeName}");
            sb.AppendLine($"Email;{report.Email}");
            sb.AppendLine($"Periodo;{report.PeriodStart:dd/MM/yyyy};{report.PeriodEnd:dd/MM/yyyy}");
            sb.AppendLine();

            sb.AppendLine("RIEPILOGO");
            sb.AppendLine($"Turni Assegnati;{report.TotalShiftsAssigned}");
            sb.AppendLine($"Turni Completati;{report.ShiftsCompleted}");
            sb.AppendLine($"Turni Mancati;{report.ShiftsMissed}");
            sb.AppendLine($"Tasso Presenza (%);{report.AttendanceRate}");
            sb.AppendLine($"Ore Programmate;{report.TotalHoursScheduled}");
            sb.AppendLine($"Ore Lavorate;{report.TotalHoursWorked}");
            sb.AppendLine($"Ore Straordinario;{report.TotalOvertimeHours}");
            sb.AppendLine($"Anomalie Totali;{report.TotalAnomalies}");
            sb.AppendLine($"Ritardi;{report.LateArrivals}");
            sb.AppendLine($"Uscite Anticipate;{report.EarlyDepartures}");
            sb.AppendLine($"Assenze Non Autorizzate;{report.UnauthorizedAbsences}");
            sb.AppendLine($"Ferie Approvate;{report.LeaveRequestsApproved}");
            sb.AppendLine($"Giorni Ferie Presi;{report.LeaveDaysTaken}");
            sb.AppendLine($"Giorni Ferie Rimanenti;{report.LeaveDaysRemaining}");
            sb.AppendLine();

            if (request.IncludeDetails && report.DailyDetails.Any())
            {
                sb.AppendLine("DETTAGLIO GIORNALIERO");
                sb.AppendLine("Data;Stato;Ingresso Previsto;Uscita Prevista;Ingresso Effettivo;Uscita Effettiva;Ore Programmate;Ore Lavorate;Anomalia;Note");

                foreach (var day in report.DailyDetails)
                {
                    sb.AppendLine($"{day.Date:dd/MM/yyyy};{day.Status};" +
                        $"{day.ScheduledStart?.ToString(@"hh\:mm") ?? "-"};" +
                        $"{day.ScheduledEnd?.ToString(@"hh\:mm") ?? "-"};" +
                        $"{day.ActualCheckIn?.ToString("HH:mm") ?? "-"};" +
                        $"{day.ActualCheckOut?.ToString("HH:mm") ?? "-"};" +
                        $"{day.HoursScheduled};{day.HoursWorked};" +
                        $"{(day.HasAnomaly ? day.AnomalyType : "-")};" +
                        $"{day.Notes?.Replace(";", ",") ?? ""}");
                }
            }
        }
        else if (request.ReportType == "merchant-summary" && request.MerchantId.HasValue)
        {
            var report = await GetMerchantSummaryReportAsync(
                request.MerchantId.Value,
                request.StartDate,
                request.EndDate);

            sb.AppendLine($"Report Riepilogativo - {report.BusinessName}");
            sb.AppendLine($"Periodo;{report.PeriodStart:dd/MM/yyyy};{report.PeriodEnd:dd/MM/yyyy}");
            sb.AppendLine();

            sb.AppendLine("STATISTICHE GENERALI");
            sb.AppendLine($"Dipendenti Totali;{report.TotalEmployees}");
            sb.AppendLine($"Dipendenti Attivi;{report.ActiveEmployees}");
            sb.AppendLine($"Turni Totali;{report.TotalShifts}");
            sb.AppendLine($"Ore Programmate;{report.TotalHoursScheduled}");
            sb.AppendLine($"Ore Lavorate;{report.TotalHoursWorked}");
            sb.AppendLine($"Tasso Presenza Medio (%);{report.AverageAttendanceRate}");
            sb.AppendLine($"Anomalie Totali;{report.TotalAnomalies}");
            sb.AppendLine($"Anomalie Pendenti;{report.PendingAnomalies}");
            sb.AppendLine($"Richieste Ferie;{report.TotalLeaveRequests}");
            sb.AppendLine($"Prenotazioni;{report.TotalBookings}");
            sb.AppendLine();

            sb.AppendLine("RIEPILOGO DIPENDENTI");
            sb.AppendLine("Dipendente;Turni Assegnati;Turni Completati;Ore Lavorate;Ore Straordinario;Tasso Presenza (%);Anomalie;Giorni Ferie");

            foreach (var emp in report.EmployeeSummaries)
            {
                sb.AppendLine($"{emp.EmployeeName};{emp.ShiftsAssigned};{emp.ShiftsCompleted};" +
                    $"{emp.HoursWorked};{emp.OvertimeHours};{emp.AttendanceRate};" +
                    $"{emp.Anomalies};{emp.LeaveDaysTaken}");
            }

            if (report.DailyStats.Any())
            {
                sb.AppendLine();
                sb.AppendLine("STATISTICHE GIORNALIERE");
                sb.AppendLine("Data;Giorno;Presenti;Assenti;In Ferie;Ore Lavorate;Turni Completati;Anomalie;Prenotazioni");

                foreach (var day in report.DailyStats)
                {
                    sb.AppendLine($"{day.Date:dd/MM/yyyy};{day.DayOfWeek};" +
                        $"{day.EmployeesPresent};{day.EmployeesAbsent};{day.EmployeesOnLeave};" +
                        $"{day.TotalHoursWorked};{day.ShiftsCompleted};{day.Anomalies};{day.Bookings}");
                }
            }
        }
        else if (request.ReportType == "admin-global")
        {
            var report = await GetAdminGlobalReportAsync(request.StartDate, request.EndDate);

            sb.AppendLine("Report Globale Amministrazione");
            sb.AppendLine($"Periodo;{report.PeriodStart:dd/MM/yyyy};{report.PeriodEnd:dd/MM/yyyy}");
            sb.AppendLine($"Generato;{report.GeneratedAt:dd/MM/yyyy HH:mm}");
            sb.AppendLine();

            sb.AppendLine("STATISTICHE GLOBALI");
            sb.AppendLine($"Merchant Totali;{report.TotalMerchants}");
            sb.AppendLine($"Merchant Attivi;{report.ActiveMerchants}");
            sb.AppendLine($"Dipendenti Totali;{report.TotalEmployees}");
            sb.AppendLine($"Consumatori Totali;{report.TotalConsumers}");
            sb.AppendLine($"Prenotazioni Totali;{report.TotalBookings}");
            sb.AppendLine($"Turni Totali;{report.TotalShifts}");
            sb.AppendLine($"Ore Lavorate Totali;{report.TotalHoursWorked}");
            sb.AppendLine();

            if (report.TopMerchants.Any())
            {
                sb.AppendLine("TOP MERCHANT");
                sb.AppendLine("Merchant;Dipendenti;Prenotazioni;Ore Lavorate;Tasso Presenza (%)");

                foreach (var m in report.TopMerchants)
                {
                    sb.AppendLine($"{m.BusinessName};{m.EmployeeCount};{m.BookingCount};" +
                        $"{m.TotalHoursWorked};{m.AverageAttendanceRate}");
                }
            }

            if (report.MonthlyTrends.Any())
            {
                sb.AppendLine();
                sb.AppendLine("TREND MENSILI");
                sb.AppendLine("Mese;Nuovi Merchant;Nuovi Dipendenti;Prenotazioni;Ore Lavorate");

                foreach (var trend in report.MonthlyTrends)
                {
                    sb.AppendLine($"{trend.MonthName};{trend.NewMerchants};{trend.NewEmployees};" +
                        $"{trend.TotalBookings};{trend.TotalHoursWorked}");
                }
            }
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
