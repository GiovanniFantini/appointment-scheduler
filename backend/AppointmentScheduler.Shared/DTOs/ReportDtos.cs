namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// Richiesta per generare report presenze/assenze
/// </summary>
public class AttendanceReportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? EmployeeId { get; set; }
    public int? MerchantId { get; set; }
    public bool IncludeDetails { get; set; } = true;
}

/// <summary>
/// Report aggregato presenze dipendente
/// </summary>
public class EmployeeAttendanceReportDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // Statistiche turni
    public int TotalShiftsAssigned { get; set; }
    public int ShiftsCompleted { get; set; }
    public int ShiftsMissed { get; set; }
    public decimal AttendanceRate { get; set; } // Percentuale

    // Ore lavorate
    public decimal TotalHoursScheduled { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public decimal TotalOvertimeHours { get; set; }

    // Anomalie
    public int TotalAnomalies { get; set; }
    public int LateArrivals { get; set; }
    public int EarlyDepartures { get; set; }
    public int UnauthorizedAbsences { get; set; }

    // Ferie e permessi
    public int LeaveRequestsApproved { get; set; }
    public decimal LeaveDaysTaken { get; set; }
    public decimal LeaveDaysRemaining { get; set; }

    // Dettagli giornalieri (opzionale)
    public List<DailyAttendanceDto> DailyDetails { get; set; } = new();
}

/// <summary>
/// Dettaglio presenze giornaliere
/// </summary>
public class DailyAttendanceDto
{
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty; // Present, Absent, Leave, Holiday
    public TimeSpan? ScheduledStart { get; set; }
    public TimeSpan? ScheduledEnd { get; set; }
    public DateTime? ActualCheckIn { get; set; }
    public DateTime? ActualCheckOut { get; set; }
    public decimal HoursScheduled { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal OvertimeHours { get; set; }
    public bool HasAnomaly { get; set; }
    public string? AnomalyType { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Report riepilogativo merchant
/// </summary>
public class MerchantSummaryReportDto
{
    public int MerchantId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // Statistiche generali
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int TotalShifts { get; set; }
    public decimal TotalHoursScheduled { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public decimal AverageAttendanceRate { get; set; }

    // Anomalie totali
    public int TotalAnomalies { get; set; }
    public int PendingAnomalies { get; set; }
    public int ResolvedAnomalies { get; set; }

    // Ferie e permessi
    public int TotalLeaveRequests { get; set; }
    public int PendingLeaveRequests { get; set; }
    public int ApprovedLeaveRequests { get; set; }
    public int RejectedLeaveRequests { get; set; }

    // Prenotazioni
    public int TotalBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }

    // Report per dipendente
    public List<EmployeeAttendanceSummaryDto> EmployeeSummaries { get; set; } = new();

    // Dati per grafici
    public List<DailyStatsDto> DailyStats { get; set; } = new();
    public List<WeeklyStatsDto> WeeklyStats { get; set; } = new();
}

/// <summary>
/// Riepilogo presenza singolo dipendente
/// </summary>
public class EmployeeAttendanceSummaryDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int ShiftsAssigned { get; set; }
    public int ShiftsCompleted { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal AttendanceRate { get; set; }
    public int Anomalies { get; set; }
    public decimal LeaveDaysTaken { get; set; }
}

/// <summary>
/// Statistiche giornaliere per grafici
/// </summary>
public class DailyStatsDto
{
    public DateTime Date { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public int EmployeesPresent { get; set; }
    public int EmployeesAbsent { get; set; }
    public int EmployeesOnLeave { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public int ShiftsCompleted { get; set; }
    public int Anomalies { get; set; }
    public int Bookings { get; set; }
}

/// <summary>
/// Statistiche settimanali per grafici
/// </summary>
public class WeeklyStatsDto
{
    public int WeekNumber { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public decimal TotalHoursScheduled { get; set; }
    public int ShiftsCompleted { get; set; }
    public int TotalAnomalies { get; set; }
    public decimal AverageAttendanceRate { get; set; }
}

/// <summary>
/// Report admin globale
/// </summary>
public class AdminGlobalReportDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime GeneratedAt { get; set; }

    // Statistiche globali
    public int TotalMerchants { get; set; }
    public int ActiveMerchants { get; set; }
    public int TotalEmployees { get; set; }
    public int TotalConsumers { get; set; }

    // Attivita
    public int TotalBookings { get; set; }
    public int TotalShifts { get; set; }
    public decimal TotalHoursWorked { get; set; }

    // Top merchant per attivita
    public List<MerchantActivityDto> TopMerchants { get; set; } = new();

    // Trend mensili
    public List<MonthlyTrendDto> MonthlyTrends { get; set; } = new();
}

/// <summary>
/// Attivita singolo merchant per classifica
/// </summary>
public class MerchantActivityDto
{
    public int MerchantId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public int BookingCount { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public decimal AverageAttendanceRate { get; set; }
}

/// <summary>
/// Trend mensile
/// </summary>
public class MonthlyTrendDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int NewMerchants { get; set; }
    public int NewEmployees { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalHoursWorked { get; set; }
}

/// <summary>
/// Opzioni export report
/// </summary>
public class ReportExportRequest
{
    public string ReportType { get; set; } = string.Empty; // attendance, merchant-summary, admin-global
    public string Format { get; set; } = "pdf"; // pdf, excel, csv
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? EmployeeId { get; set; }
    public int? MerchantId { get; set; }
    public bool IncludeCharts { get; set; } = true;
    public bool IncludeDetails { get; set; } = true;
}

/// <summary>
/// Dashboard statistiche real-time
/// </summary>
public class DashboardStatsDto
{
    public DateTime GeneratedAt { get; set; }

    // Oggi
    public int TodayShifts { get; set; }
    public int TodayEmployeesPresent { get; set; }
    public int TodayEmployeesAbsent { get; set; }
    public int TodayPendingAnomalies { get; set; }
    public int TodayBookings { get; set; }

    // Questa settimana
    public decimal WeekHoursWorked { get; set; }
    public decimal WeekHoursScheduled { get; set; }
    public int WeekAnomalies { get; set; }
    public int WeekLeaveRequests { get; set; }

    // Questo mese
    public decimal MonthHoursWorked { get; set; }
    public decimal MonthAttendanceRate { get; set; }
    public int MonthTotalShifts { get; set; }

    // Confronto con periodo precedente
    public decimal HoursWorkedTrend { get; set; } // Percentuale di variazione
    public decimal AttendanceRateTrend { get; set; }
    public decimal BookingsTrend { get; set; }

    // Prossimi eventi
    public List<UpcomingEventDto> UpcomingEvents { get; set; } = new();
}

/// <summary>
/// Evento prossimo (turno, ferie, scadenze)
/// </summary>
public class UpcomingEventDto
{
    public string EventType { get; set; } = string.Empty; // shift, leave, document-expiry
    public DateTime Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
}
