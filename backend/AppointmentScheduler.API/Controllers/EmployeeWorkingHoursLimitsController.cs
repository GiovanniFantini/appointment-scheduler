using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione dei limiti orari dei dipendenti
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmployeeWorkingHoursLimitsController : ControllerBase
{
    private readonly IEmployeeWorkingHoursLimitService _limitService;

    public EmployeeWorkingHoursLimitsController(IEmployeeWorkingHoursLimitService limitService)
    {
        _limitService = limitService;
    }

    /// <summary>
    /// Recupera tutti i limiti orari di un dipendente
    /// </summary>
    [HttpGet("employee/{employeeId}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<EmployeeWorkingHoursLimitDto>>> GetEmployeeLimits(int employeeId)
    {
        var limits = await _limitService.GetEmployeeLimitsAsync(employeeId);
        return Ok(limits);
    }

    /// <summary>
    /// Recupera il limite orario attivo per un dipendente
    /// </summary>
    [HttpGet("employee/{employeeId}/active")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<EmployeeWorkingHoursLimitDto>> GetActiveLimit(
        int employeeId,
        [FromQuery] DateTime? date = null)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var limit = await _limitService.GetActiveEmployeeLimitAsync(employeeId, targetDate);

        if (limit == null)
            return NotFound(new { message = "Nessun limite attivo trovato per questo dipendente" });

        return Ok(limit);
    }

    /// <summary>
    /// Recupera i limiti orari del dipendente loggato
    /// </summary>
    [HttpGet("my-limits")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<IEnumerable<EmployeeWorkingHoursLimitDto>>> GetMyLimits()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        var limits = await _limitService.GetEmployeeLimitsAsync(employeeId);
        return Ok(limits);
    }

    /// <summary>
    /// Recupera un limite specifico
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<EmployeeWorkingHoursLimitDto>> GetById(int id)
    {
        var limit = await _limitService.GetLimitByIdAsync(id);

        if (limit == null)
            return NotFound(new { message = "Limite non trovato" });

        return Ok(limit);
    }

    /// <summary>
    /// Crea un nuovo limite orario
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<EmployeeWorkingHoursLimitDto>> Create([FromBody] CreateEmployeeWorkingHoursLimitRequest request)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        try
        {
            var limit = await _limitService.CreateLimitAsync(merchantId, request);
            return CreatedAtAction(nameof(GetById), new { id = limit.Id }, limit);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nella creazione del limite: {ex.Message}" });
        }
    }

    /// <summary>
    /// Aggiorna un limite orario esistente
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<EmployeeWorkingHoursLimitDto>> Update(int id, [FromBody] UpdateEmployeeWorkingHoursLimitRequest request)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        try
        {
            var limit = await _limitService.UpdateLimitAsync(id, merchantId, request);

            if (limit == null)
                return NotFound(new { message = "Limite non trovato o non autorizzato" });

            return Ok(limit);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nell'aggiornamento del limite: {ex.Message}" });
        }
    }

    /// <summary>
    /// Elimina un limite orario
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var result = await _limitService.DeleteLimitAsync(id, merchantId);

        if (!result)
            return NotFound(new { message = "Limite non trovato" });

        return NoContent();
    }
}
