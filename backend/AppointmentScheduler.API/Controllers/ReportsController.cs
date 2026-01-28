using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la generazione di report e statistiche
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Ottiene le statistiche dashboard per merchant
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats([FromQuery] int? merchantId = null)
    {
        // Se merchantId non specificato, usa quello dell'utente corrente
        if (!merchantId.HasValue)
        {
            var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
            if (!string.IsNullOrEmpty(merchantIdClaim) && int.TryParse(merchantIdClaim, out int parsedMerchantId))
            {
                merchantId = parsedMerchantId;
            }
        }

        // Admin può specificare qualsiasi merchantId
        if (merchantId.HasValue && !User.IsInRole("Admin"))
        {
            var ownMerchantIdClaim = User.FindFirst("MerchantId")?.Value;
            if (string.IsNullOrEmpty(ownMerchantIdClaim) ||
                !int.TryParse(ownMerchantIdClaim, out int ownMerchantId) ||
                ownMerchantId != merchantId.Value)
            {
                return Forbid();
            }
        }

        var stats = await _reportService.GetDashboardStatsAsync(merchantId);
        return Ok(stats);
    }

    /// <summary>
    /// Ottiene le statistiche dashboard per dipendente
    /// </summary>
    [HttpGet("dashboard/employee")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<DashboardStatsDto>> GetEmployeeDashboardStats()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
        {
            return BadRequest(new { message = "Employee ID non trovato" });
        }

        try
        {
            var stats = await _reportService.GetEmployeeDashboardStatsAsync(employeeId);
            return Ok(stats);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Ottiene le statistiche dashboard globali per admin
    /// </summary>
    [HttpGet("dashboard/admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<DashboardStatsDto>> GetAdminDashboardStats()
    {
        var stats = await _reportService.GetDashboardStatsAsync(null);
        return Ok(stats);
    }

    /// <summary>
    /// Genera report presenze per un dipendente
    /// </summary>
    [HttpGet("attendance/employee/{employeeId}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<EmployeeAttendanceReportDto>> GetEmployeeAttendanceReport(
        int employeeId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] bool includeDetails = true)
    {
        try
        {
            var report = await _reportService.GetEmployeeAttendanceReportAsync(
                employeeId, startDate, endDate, includeDetails);
            return Ok(report);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Genera report presenze per il dipendente corrente
    /// </summary>
    [HttpGet("attendance/my")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<EmployeeAttendanceReportDto>> GetMyAttendanceReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] bool includeDetails = true)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
        {
            return BadRequest(new { message = "Employee ID non trovato" });
        }

        try
        {
            var report = await _reportService.GetEmployeeAttendanceReportAsync(
                employeeId, startDate, endDate, includeDetails);
            return Ok(report);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Genera report riepilogativo merchant
    /// </summary>
    [HttpGet("merchant-summary")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<MerchantSummaryReportDto>> GetMerchantSummaryReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? merchantId = null)
    {
        if (!merchantId.HasValue)
        {
            var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
            if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int parsedMerchantId))
            {
                return BadRequest(new { message = "Merchant ID non trovato" });
            }
            merchantId = parsedMerchantId;
        }
        else if (!User.IsInRole("Admin"))
        {
            var ownMerchantIdClaim = User.FindFirst("MerchantId")?.Value;
            if (string.IsNullOrEmpty(ownMerchantIdClaim) ||
                !int.TryParse(ownMerchantIdClaim, out int ownMerchantId) ||
                ownMerchantId != merchantId.Value)
            {
                return Forbid();
            }
        }

        try
        {
            var report = await _reportService.GetMerchantSummaryReportAsync(
                merchantId.Value, startDate, endDate);
            return Ok(report);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Genera report globale admin
    /// </summary>
    [HttpGet("admin-global")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<AdminGlobalReportDto>> GetAdminGlobalReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var report = await _reportService.GetAdminGlobalReportAsync(startDate, endDate);
        return Ok(report);
    }

    /// <summary>
    /// Esporta report in PDF
    /// </summary>
    [HttpPost("export/pdf")]
    [Authorize]
    public async Task<IActionResult> ExportToPdf([FromBody] ReportExportRequest request)
    {
        // Validazione accesso
        if (!await ValidateExportAccessAsync(request))
        {
            return Forbid();
        }

        try
        {
            var pdfBytes = await _reportService.ExportToPdfAsync(request);
            var fileName = $"report_{request.ReportType}_{request.StartDate:yyyyMMdd}_{request.EndDate:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore generazione PDF: {ex.Message}" });
        }
    }

    /// <summary>
    /// Esporta report in Excel
    /// </summary>
    [HttpPost("export/excel")]
    [Authorize]
    public async Task<IActionResult> ExportToExcel([FromBody] ReportExportRequest request)
    {
        if (!await ValidateExportAccessAsync(request))
        {
            return Forbid();
        }

        try
        {
            var excelBytes = await _reportService.ExportToExcelAsync(request);
            var fileName = $"report_{request.ReportType}_{request.StartDate:yyyyMMdd}_{request.EndDate:yyyyMMdd}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore generazione Excel: {ex.Message}" });
        }
    }

    /// <summary>
    /// Esporta report in CSV
    /// </summary>
    [HttpPost("export/csv")]
    [Authorize]
    public async Task<IActionResult> ExportToCsv([FromBody] ReportExportRequest request)
    {
        if (!await ValidateExportAccessAsync(request))
        {
            return Forbid();
        }

        try
        {
            var csvBytes = await _reportService.ExportToCsvAsync(request);
            var fileName = $"report_{request.ReportType}_{request.StartDate:yyyyMMdd}_{request.EndDate:yyyyMMdd}.csv";
            return File(csvBytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore generazione CSV: {ex.Message}" });
        }
    }

    private Task<bool> ValidateExportAccessAsync(ReportExportRequest request)
    {
        // Admin ha accesso a tutto
        if (User.IsInRole("Admin"))
        {
            return Task.FromResult(true);
        }

        // Merchant può accedere solo ai propri report
        if (User.IsInRole("Merchant"))
        {
            if (request.ReportType == "admin-global")
            {
                return Task.FromResult(false);
            }

            if (request.MerchantId.HasValue)
            {
                var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(merchantIdClaim) ||
                    !int.TryParse(merchantIdClaim, out int ownMerchantId) ||
                    ownMerchantId != request.MerchantId.Value)
                {
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(true);
        }

        // Employee può accedere solo al proprio report presenze
        if (User.IsInRole("Employee"))
        {
            if (request.ReportType != "attendance")
            {
                return Task.FromResult(false);
            }

            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (string.IsNullOrEmpty(employeeIdClaim) ||
                !int.TryParse(employeeIdClaim, out int ownEmployeeId) ||
                (request.EmployeeId.HasValue && ownEmployeeId != request.EmployeeId.Value))
            {
                return Task.FromResult(false);
            }

            // Imposta l'employeeId se non specificato
            request.EmployeeId = ownEmployeeId;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
