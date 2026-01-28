using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Interfaces;

/// <summary>
/// Servizio per la generazione di report e statistiche
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Genera report presenze per un singolo dipendente
    /// </summary>
    Task<EmployeeAttendanceReportDto> GetEmployeeAttendanceReportAsync(
        int employeeId,
        DateTime startDate,
        DateTime endDate,
        bool includeDetails = true);

    /// <summary>
    /// Genera report riepilogativo per un merchant
    /// </summary>
    Task<MerchantSummaryReportDto> GetMerchantSummaryReportAsync(
        int merchantId,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Genera report globale per admin
    /// </summary>
    Task<AdminGlobalReportDto> GetAdminGlobalReportAsync(
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Ottiene statistiche dashboard in tempo reale
    /// </summary>
    Task<DashboardStatsDto> GetDashboardStatsAsync(int? merchantId = null);

    /// <summary>
    /// Ottiene statistiche dashboard per dipendente
    /// </summary>
    Task<DashboardStatsDto> GetEmployeeDashboardStatsAsync(int employeeId);

    /// <summary>
    /// Esporta report in formato PDF
    /// </summary>
    Task<byte[]> ExportToPdfAsync(ReportExportRequest request);

    /// <summary>
    /// Esporta report in formato Excel
    /// </summary>
    Task<byte[]> ExportToExcelAsync(ReportExportRequest request);

    /// <summary>
    /// Esporta report in formato CSV
    /// </summary>
    Task<byte[]> ExportToCsvAsync(ReportExportRequest request);
}
