using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Timbratura lato merchant: configurazione per filiale, monitoraggio presenze,
/// correzioni manuali.
/// </summary>
[ApiController]
[Route("api/merchant/time-clock")]
[Authorize(Policy = "ApprovedMerchantOnly")]
public class TimeClockController : ControllerBase
{
    private readonly ITimeClockService _timeClockService;

    public TimeClockController(ITimeClockService timeClockService)
    {
        _timeClockService = timeClockService;
    }

    private bool TryGetMerchantId(out int merchantId)
    {
        merchantId = 0;
        var claim = User.FindFirst("MerchantId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out merchantId);
    }

    /// <summary>Configurazione timbratura di una filiale.</summary>
    [HttpGet("settings")]
    public async Task<ActionResult<BranchTimeClockSettingsDto>> GetSettings([FromQuery] int branchId)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });

        try
        {
            var settings = await _timeClockService.GetSettingsAsync(branchId, merchantId);
            return Ok(settings);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Timbrature del merchant filtrate per filiale, intervallo e dipendente.</summary>
    [HttpGet("entries")]
    public async Task<ActionResult<List<TimeEntryDto>>> GetEntries(
        [FromQuery] int? branchId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] int? employeeId = null)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from ?? today.AddDays(-30);
        var toDate = to ?? today;

        var entries = await _timeClockService.GetEntriesAsync(merchantId, branchId, fromDate, toDate, employeeId);
        return Ok(entries);
    }

    /// <summary>Anomalie del merchant filtrate per filiale e stato.</summary>
    [HttpGet("anomalies")]
    public async Task<ActionResult<List<TimeClockAnomalyDto>>> GetAnomalies(
        [FromQuery] int? branchId = null,
        [FromQuery] TimeClockAnomalyStatus? status = null)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });

        var anomalies = await _timeClockService.GetAnomaliesAsync(merchantId, branchId, status);
        return Ok(anomalies);
    }

    /// <summary>Report ore lavorate per dipendente e giornata.</summary>
    [HttpGet("report")]
    public async Task<ActionResult<List<TimeClockReportRowDto>>> GetReport(
        [FromQuery] int? branchId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from ?? today.AddDays(-30);
        var toDate = to ?? today;

        var rows = await _timeClockService.GetReportAsync(merchantId, branchId, fromDate, toDate);
        return Ok(rows);
    }

}
