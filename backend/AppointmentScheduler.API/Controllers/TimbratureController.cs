using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per il sistema di timbratura intelligente
/// Implementa: Fiducia, Semplicità, Empatia, Auto-validazione 95%
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TimbratureController : ControllerBase
{
    private readonly ITimbratureService _timbratureService;

    public TimbratureController(ITimbratureService timbratureService)
    {
        _timbratureService = timbratureService;
    }

    /// <summary>
    /// Check-in su un turno - Pulsante auto-rilevato ENTRA
    /// </summary>
    [HttpPost("check-in")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<TimbratureResponse>> CheckIn([FromBody] CheckInRequest request)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        try
        {
            var response = await _timbratureService.CheckInAsync(employeeId, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Check-out da un turno - Pulsante auto-rilevato ESCI
    /// </summary>
    [HttpPost("check-out")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<TimbratureResponse>> CheckOut([FromBody] CheckOutRequest request)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        try
        {
            var response = await _timbratureService.CheckOutAsync(employeeId, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Inizia una pausa (con auto-suggerimento)
    /// </summary>
    [HttpPost("break/start")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<ShiftBreakDto>> StartBreak([FromBody] StartBreakRequest request)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        try
        {
            var breakDto = await _timbratureService.StartBreakAsync(employeeId, request);
            return Ok(breakDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Termina una pausa in corso
    /// </summary>
    [HttpPost("break/end")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<ShiftBreakDto>> EndBreak([FromBody] EndBreakRequest request)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        try
        {
            var breakDto = await _timbratureService.EndBreakAsync(employeeId, request);
            return Ok(breakDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Recupera lo stato corrente del dipendente (timer visivo + suggerimenti)
    /// </summary>
    [HttpGet("status")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<CurrentStatusResponse>> GetStatus()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        var status = await _timbratureService.GetCurrentStatusAsync(employeeId);
        return Ok(status);
    }

    /// <summary>
    /// Recupera il turno odierno
    /// </summary>
    [HttpGet("today")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<ShiftDto>> GetTodayShift()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        var shift = await _timbratureService.GetTodayShiftAsync(employeeId);

        if (shift == null)
            return NotFound(new { message = "Nessun turno oggi" });

        return Ok(shift);
    }

    /// <summary>
    /// Risolve un'anomalia con opzioni empatiche (Traffico, Permesso, Recupero, Altro)
    /// </summary>
    [HttpPost("anomaly/resolve")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<ShiftAnomalyDto>> ResolveAnomaly([FromBody] ResolveAnomalyRequest request)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        try
        {
            var anomaly = await _timbratureService.ResolveAnomalyAsync(employeeId, request);
            return Ok(anomaly);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Classifica straordinario (Paid, BankedHours, Recovery, Voluntary)
    /// </summary>
    [HttpPost("overtime/classify")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<OvertimeRecordDto>> ClassifyOvertime([FromBody] ClassifyOvertimeRequest request)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        try
        {
            var overtime = await _timbratureService.ClassifyOvertimeAsync(employeeId, request);
            return Ok(overtime);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Correggi un turno - Auto-approvato se entro 24h, altrimenti richiede merchant
    /// </summary>
    [HttpPost("correct")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<ShiftCorrectionDto>> CorrectShift([FromBody] CorrectShiftRequest request)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        try
        {
            var correction = await _timbratureService.CorrectShiftAsync(employeeId, request);
            return Ok(correction);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Statistiche benessere con alert se >50h/settimana
    /// </summary>
    [HttpGet("wellbeing")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<WellbeingStatsResponse>> GetWellbeingStats()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        var stats = await _timbratureService.GetWellbeingStatsAsync(employeeId);
        return Ok(stats);
    }

    // ========== MERCHANT ENDPOINTS ==========

    /// <summary>
    /// Auto-valida turni (95% automatico, ±15min tolerance)
    /// </summary>
    [HttpPost("auto-validate")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<int>> AutoValidateShifts([FromQuery] DateTime? date = null)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var count = await _timbratureService.AutoValidateShiftsAsync(merchantId, date);
        return Ok(new { validatedCount = count, message = $"{count} turni validati automaticamente" });
    }

    /// <summary>
    /// Approvazione batch (1-click) per merchant
    /// </summary>
    [HttpPost("batch-approve")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<int>> BatchApprove([FromBody] List<int> shiftIds)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var count = await _timbratureService.BatchApproveShiftsAsync(merchantId, shiftIds);
        return Ok(new { approvedCount = count, message = $"{count} turni approvati" });
    }
}
