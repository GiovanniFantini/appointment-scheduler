using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.API.Authorization;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Timbratura operativa lato employee app (configurazione e monitoraggio team).
/// </summary>
[ApiController]
[Route("api/employee/time-clock-management")]
[Authorize(Policy = "EmployeeOnly")]
public class EmployeeTimeClockManagementController : ControllerBase
{
    private readonly ITimeClockService _timeClockService;

    public EmployeeTimeClockManagementController(ITimeClockService timeClockService)
    {
        _timeClockService = timeClockService;
    }

    private bool TryGetMerchantId(out int merchantId)
    {
        merchantId = 0;
        var claim = User.FindFirst("MerchantId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out merchantId);
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out userId);
    }

    private ActionResult? RequireReadOnly()
    {
        if (!User.RequireFeatureLevel(MerchantFeature.Timbratura, FeatureAccessLevel.ReadOnly))
            return Forbid();
        return null;
    }

    private ActionResult? RequireOperator()
    {
        if (!User.RequireFeatureLevel(MerchantFeature.Timbratura, FeatureAccessLevel.Operator))
            return Forbid();
        return null;
    }

    private ActionResult? RequireManager()
    {
        if (!User.RequireFeatureLevel(MerchantFeature.Timbratura, FeatureAccessLevel.Manager))
            return Forbid();
        return null;
    }

    [HttpGet("settings")]
    public async Task<ActionResult<BranchTimeClockSettingsDto>> GetSettings([FromQuery] int branchId)
    {
        if (RequireReadOnly() is { } forbidden)
            return forbidden;

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

    [HttpPut("settings/{branchId}")]
    public async Task<ActionResult<BranchTimeClockSettingsDto>> UpdateSettings(
        int branchId, [FromBody] UpdateTimeClockSettingsRequest request)
    {
        if (RequireManager() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });

        try
        {
            var settings = await _timeClockService.UpdateSettingsAsync(branchId, merchantId, request);
            return Ok(settings);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("entries")]
    public async Task<ActionResult<List<TimeEntryDto>>> GetEntries(
        [FromQuery] int? branchId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] int? employeeId = null)
    {
        if (RequireReadOnly() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from ?? today.AddDays(-30);
        var toDate = to ?? today;

        var entries = await _timeClockService.GetEntriesAsync(merchantId, branchId, fromDate, toDate, employeeId);
        return Ok(entries);
    }

    [HttpPost("entries")]
    public async Task<ActionResult<TimeEntryDto>> CreateManualEntry([FromBody] CreateManualEntryRequest request)
    {
        if (RequireOperator() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId) || !TryGetUserId(out int userId))
            return BadRequest(new { message = "Token non valido" });

        try
        {
            var entry = await _timeClockService.CreateManualEntryAsync(merchantId, userId, request);
            return Ok(entry);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("anomalies")]
    public async Task<ActionResult<List<TimeClockAnomalyDto>>> GetAnomalies(
        [FromQuery] int? branchId = null,
        [FromQuery] TimeClockAnomalyStatus? status = null)
    {
        if (RequireReadOnly() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });

        var anomalies = await _timeClockService.GetAnomaliesAsync(merchantId, branchId, status);
        return Ok(anomalies);
    }

    [HttpPost("anomalies/{id}/approve")]
    public Task<ActionResult<TimeClockAnomalyDto>> ApproveAnomaly(int id, [FromBody] ReviewAnomalyRequest? body = null)
        => ReviewAnomaly(id, body, approve: true);

    [HttpPost("anomalies/{id}/reject")]
    public Task<ActionResult<TimeClockAnomalyDto>> RejectAnomaly(int id, [FromBody] ReviewAnomalyRequest? body = null)
        => ReviewAnomaly(id, body, approve: false);

    [HttpGet("report")]
    public async Task<ActionResult<List<TimeClockReportRowDto>>> GetReport(
        [FromQuery] int? branchId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null)
    {
        if (RequireReadOnly() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from ?? today.AddDays(-30);
        var toDate = to ?? today;

        var rows = await _timeClockService.GetReportAsync(merchantId, branchId, fromDate, toDate);
        return Ok(rows);
    }

    [HttpPost("run-detection")]
    public async Task<ActionResult<object>> RunDetection([FromQuery] int? branchId = null)
    {
        if (RequireManager() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });

        var created = await _timeClockService.RunMissingPunchDetectionAsync(merchantId, branchId);
        return Ok(new { created });
    }

    private async Task<ActionResult<TimeClockAnomalyDto>> ReviewAnomaly(
        int id, ReviewAnomalyRequest? body, bool approve)
    {
        if (RequireManager() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId) || !TryGetUserId(out int userId))
            return BadRequest(new { message = "Token non valido" });

        try
        {
            var request = body ?? new ReviewAnomalyRequest();
            var result = approve
                ? await _timeClockService.ApproveAnomalyAsync(id, merchantId, userId, request)
                : await _timeClockService.RejectAnomalyAsync(id, merchantId, userId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
