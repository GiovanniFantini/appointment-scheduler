using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione delle richieste di scambio turni
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ShiftSwapRequestsController : ControllerBase
{
    private readonly IShiftSwapService _shiftSwapService;

    public ShiftSwapRequestsController(IShiftSwapService shiftSwapService)
    {
        _shiftSwapService = shiftSwapService;
    }

    /// <summary>
    /// Recupera tutte le richieste di scambio del merchant
    /// </summary>
    [HttpGet("merchant")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<ShiftSwapRequestDto>>> GetMerchantRequests()
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var requests = await _shiftSwapService.GetMerchantSwapRequestsAsync(merchantId);
        return Ok(requests);
    }

    /// <summary>
    /// Recupera le richieste di scambio inviate dal dipendente loggato
    /// </summary>
    [HttpGet("my-requests")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<IEnumerable<ShiftSwapRequestDto>>> GetMyRequests()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        var requests = await _shiftSwapService.GetEmployeeSwapRequestsAsync(employeeId);
        return Ok(requests);
    }

    /// <summary>
    /// Recupera le richieste di scambio ricevute dal dipendente loggato
    /// </summary>
    [HttpGet("received")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<IEnumerable<ShiftSwapRequestDto>>> GetReceivedRequests()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        var requests = await _shiftSwapService.GetEmployeeReceivedSwapRequestsAsync(employeeId);
        return Ok(requests);
    }

    /// <summary>
    /// Recupera una richiesta specifica
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ShiftSwapRequestDto>> GetById(int id)
    {
        var request = await _shiftSwapService.GetSwapRequestByIdAsync(id);

        if (request == null)
            return NotFound(new { message = "Richiesta non trovata" });

        return Ok(request);
    }

    /// <summary>
    /// Crea una nuova richiesta di scambio turno
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<ShiftSwapRequestDto>> Create([FromBody] CreateShiftSwapRequest request)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        try
        {
            var swapRequest = await _shiftSwapService.CreateSwapRequestAsync(employeeId, request);
            return CreatedAtAction(nameof(GetById), new { id = swapRequest.Id }, swapRequest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nella creazione della richiesta: {ex.Message}" });
        }
    }

    /// <summary>
    /// Risponde a una richiesta di scambio
    /// </summary>
    [HttpPost("{id}/respond")]
    [Authorize]
    public async Task<ActionResult<ShiftSwapRequestDto>> Respond(int id, [FromBody] RespondToShiftSwapRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest(new { message = "User ID non trovato" });

        try
        {
            var swapRequest = await _shiftSwapService.RespondToSwapRequestAsync(id, userId, request);

            if (swapRequest == null)
                return NotFound(new { message = "Richiesta non trovata" });

            return Ok(swapRequest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nella risposta alla richiesta: {ex.Message}" });
        }
    }

    /// <summary>
    /// Cancella una richiesta di scambio
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<IActionResult> Cancel(int id)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        var result = await _shiftSwapService.CancelSwapRequestAsync(id, employeeId);

        if (!result)
            return NotFound(new { message = "Richiesta non trovata" });

        return NoContent();
    }
}
