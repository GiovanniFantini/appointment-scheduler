using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Exceptions;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione dei turni
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ShiftsController : ControllerBase
{
    private readonly IShiftService _shiftService;

    public ShiftsController(IShiftService shiftService)
    {
        _shiftService = shiftService;
    }

    /// <summary>
    /// Recupera tutti i turni del merchant in un range di date
    /// Gli Admin possono specificare merchantId opzionale, altrimenti usa il proprio
    /// </summary>
    [HttpGet("merchant")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<ShiftDto>>> GetMerchantShifts(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? merchantId = null)
    {
        // Se merchantId non è specificato, prova a prenderlo dal claim (per merchant normali)
        if (!merchantId.HasValue)
        {
            var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

            if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int parsedMerchantId))
            {
                // Se non ha merchantId nel claim e non è Admin, errore
                if (!User.IsInRole("Admin"))
                    return BadRequest(new { message = "Merchant ID non trovato. Sei loggato come merchant?" });

                // Admin senza merchantId specificato -> errore
                return BadRequest(new { message = "Admin deve specificare merchantId come query parameter" });
            }

            merchantId = parsedMerchantId;
        }
        // Se merchantId è specificato ma l'utente non è Admin, verifica che corrisponda al proprio
        else if (!User.IsInRole("Admin"))
        {
            var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

            if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int ownMerchantId) || ownMerchantId != merchantId.Value)
                return Forbid(); // Non può accedere ai turni di altri merchant
        }

        var shifts = await _shiftService.GetMerchantShiftsAsync(merchantId.Value, startDate, endDate);
        return Ok(shifts);
    }

    /// <summary>
    /// Recupera tutti i turni di un dipendente in un range di date
    /// </summary>
    [HttpGet("employee/{employeeId}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<ShiftDto>>> GetEmployeeShifts(
        int employeeId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var shifts = await _shiftService.GetEmployeeShiftsAsync(employeeId, startDate, endDate);
        return Ok(shifts);
    }

    /// <summary>
    /// Recupera i turni del dipendente loggato (per employee app)
    /// </summary>
    [HttpGet("my-shifts")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<IEnumerable<ShiftDto>>> GetMyShifts(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        var shifts = await _shiftService.GetEmployeeShiftsAsync(employeeId, startDate, endDate);
        return Ok(shifts);
    }

    /// <summary>
    /// Recupera un turno specifico
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ShiftDto>> GetById(int id)
    {
        var shift = await _shiftService.GetShiftByIdAsync(id);

        if (shift == null)
            return NotFound(new { message = "Turno non trovato" });

        return Ok(shift);
    }

    /// <summary>
    /// Crea un nuovo turno
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<ShiftDto>> Create([FromBody] CreateShiftRequest request)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        try
        {
            var shift = await _shiftService.CreateShiftAsync(merchantId, request);
            return CreatedAtAction(nameof(GetById), new { id = shift.Id }, shift);
        }
        catch (LeaveConflictException ex)
        {
            return Conflict(new LeaveConflictResponse
            {
                Message = ex.Message,
                Conflicts = ex.Conflicts
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nella creazione del turno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Crea turni multipli da un template
    /// </summary>
    [HttpPost("from-template")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<ShiftDto>>> CreateFromTemplate([FromBody] CreateShiftsFromTemplateRequest request)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        try
        {
            var shifts = await _shiftService.CreateShiftsFromTemplateAsync(merchantId, request);
            return Ok(shifts);
        }
        catch (LeaveConflictException ex)
        {
            return Conflict(new LeaveConflictResponse
            {
                Message = ex.Message,
                Conflicts = ex.Conflicts
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nella creazione dei turni: {ex.Message}" });
        }
    }

    /// <summary>
    /// Aggiorna un turno esistente
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<ShiftDto>> Update(int id, [FromBody] UpdateShiftRequest request)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        try
        {
            var shift = await _shiftService.UpdateShiftAsync(id, merchantId, request);

            if (shift == null)
                return NotFound(new { message = "Turno non trovato o non autorizzato" });

            return Ok(shift);
        }
        catch (LeaveConflictException ex)
        {
            return Conflict(new LeaveConflictResponse
            {
                Message = ex.Message,
                Conflicts = ex.Conflicts
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nell'aggiornamento del turno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Assegna un turno a un dipendente
    /// </summary>
    [HttpPost("{id}/assign")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<ShiftDto>> Assign(int id, [FromBody] AssignShiftRequest request)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        try
        {
            var shift = await _shiftService.AssignShiftAsync(id, merchantId, request);

            if (shift == null)
                return NotFound(new { message = "Turno non trovato" });

            return Ok(shift);
        }
        catch (LeaveConflictException ex)
        {
            return Conflict(new LeaveConflictResponse
            {
                Message = ex.Message,
                Conflicts = ex.Conflicts
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nell'assegnazione del turno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Elimina un turno
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var result = await _shiftService.DeleteShiftAsync(id, merchantId);

        if (!result)
            return NotFound(new { message = "Turno non trovato" });

        return NoContent();
    }

    /// <summary>
    /// Recupera tutti i turni del team per un merchant specifico (vista employee)
    /// Permette ai dipendenti di vedere i turni di tutti i colleghi
    /// </summary>
    [HttpGet("team")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<IEnumerable<ShiftDto>>> GetTeamShifts(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int empId))
            return BadRequest(new { message = "Employee ID non trovato" });

        try
        {
            var shifts = await _shiftService.GetTeamShiftsAsync(empId, startDate, endDate);
            return Ok(shifts);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Recupera statistiche turni per un dipendente
    /// </summary>
    [HttpGet("employee/{employeeId}/stats")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<EmployeeShiftStatsDto>> GetEmployeeStats(int employeeId)
    {
        try
        {
            var stats = await _shiftService.GetEmployeeShiftStatsAsync(employeeId);
            return Ok(stats);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Recupera statistiche turni per il dipendente loggato
    /// </summary>
    [HttpGet("my-stats")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<EmployeeShiftStatsDto>> GetMyStats()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        try
        {
            var stats = await _shiftService.GetEmployeeShiftStatsAsync(employeeId);
            return Ok(stats);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
