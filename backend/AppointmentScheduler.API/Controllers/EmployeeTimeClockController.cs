using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Timbratura lato dipendente: clock-in/out, pause, stato corrente, storico.
/// </summary>
[ApiController]
[Route("api/time-clock")]
[Authorize(Policy = "EmployeeOnly")]
public class EmployeeTimeClockController : ControllerBase
{
    private readonly ITimeClockService _timeClockService;

    public EmployeeTimeClockController(ITimeClockService timeClockService)
    {
        _timeClockService = timeClockService;
    }

    private bool TryGetEmployeeId(out int employeeId)
    {
        employeeId = 0;
        var claim = User.FindFirst("EmployeeId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out employeeId);
    }

    private bool TryGetMerchantId(out int merchantId)
    {
        merchantId = 0;
        var claim = User.FindFirst("MerchantId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out merchantId);
    }

    private bool HasTimbraturaFeature()
        => User.FindAll("Feature").Any(c => c.Value == "Timbratura");

    /// <summary>Stato corrente di timbratura del dipendente.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<CurrentClockStatusDto>> GetStatus()
    {
        if (!TryGetEmployeeId(out int employeeId) || !TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasTimbraturaFeature())
            return Forbid();

        var status = await _timeClockService.GetCurrentStatusAsync(employeeId, merchantId);
        return Ok(status);
    }

    /// <summary>Timbra l'entrata.</summary>
    [HttpPost("clock-in")]
    public Task<ActionResult<ClockActionResultDto>> ClockIn([FromBody] ClockActionRequest request)
        => Execute(request, (s, e, m, r) => s.ClockInAsync(e, m, r));

    /// <summary>Timbra l'uscita.</summary>
    [HttpPost("clock-out")]
    public Task<ActionResult<ClockActionResultDto>> ClockOut([FromBody] ClockActionRequest request)
        => Execute(request, (s, e, m, r) => s.ClockOutAsync(e, m, r));

    /// <summary>Avvia una pausa.</summary>
    [HttpPost("break/start")]
    public Task<ActionResult<ClockActionResultDto>> StartBreak([FromBody] ClockActionRequest request)
        => Execute(request, (s, e, m, r) => s.StartBreakAsync(e, m, r));

    /// <summary>Termina la pausa in corso.</summary>
    [HttpPost("break/end")]
    public Task<ActionResult<ClockActionResultDto>> EndBreak([FromBody] ClockActionRequest request)
        => Execute(request, (s, e, m, r) => s.EndBreakAsync(e, m, r));

    /// <summary>Storico timbrature del dipendente in un intervallo di date.</summary>
    [HttpGet("my-entries")]
    public async Task<ActionResult<List<TimeEntryDto>>> GetMyEntries(
        [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null)
    {
        if (!TryGetEmployeeId(out int employeeId) || !TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasTimbraturaFeature())
            return Forbid();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from ?? today.AddDays(-30);
        var toDate = to ?? today;

        var entries = await _timeClockService.GetMyEntriesAsync(employeeId, merchantId, fromDate, toDate);
        return Ok(entries);
    }

    /// <summary>Anomalie del dipendente, opzionalmente filtrate per stato.</summary>
    [HttpGet("my-anomalies")]
    public async Task<ActionResult<List<TimeClockAnomalyDto>>> GetMyAnomalies(
        [FromQuery] TimeClockAnomalyStatus? status = null)
    {
        if (!TryGetEmployeeId(out int employeeId) || !TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasTimbraturaFeature())
            return Forbid();

        var anomalies = await _timeClockService.GetMyAnomaliesAsync(employeeId, merchantId, status);
        return Ok(anomalies);
    }

    /// <summary>Statistiche di benessere del dipendente (ore, straordinari, alert).</summary>
    [HttpGet("wellbeing")]
    public async Task<ActionResult<WellbeingStatsDto>> GetWellbeing()
    {
        if (!TryGetEmployeeId(out int employeeId) || !TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasTimbraturaFeature())
            return Forbid();

        var stats = await _timeClockService.GetWellbeingStatsAsync(employeeId, merchantId);
        return Ok(stats);
    }

    /// <summary>Il dipendente giustifica una propria anomalia.</summary>
    [HttpPost("anomalies/{id}/justify")]
    public async Task<ActionResult<TimeClockAnomalyDto>> JustifyAnomaly(
        int id, [FromBody] JustifyAnomalyRequest request)
    {
        if (!TryGetEmployeeId(out int employeeId) || !TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasTimbraturaFeature())
            return Forbid();

        try
        {
            var result = await _timeClockService.JustifyAnomalyAsync(id, employeeId, merchantId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<ActionResult<ClockActionResultDto>> Execute(
        ClockActionRequest request,
        Func<ITimeClockService, int, int, ClockActionRequest, Task<ClockActionResultDto>> action)
    {
        if (!TryGetEmployeeId(out int employeeId) || !TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasTimbraturaFeature())
            return Forbid();

        try
        {
            var result = await action(_timeClockService, employeeId, merchantId, request ?? new ClockActionRequest());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
