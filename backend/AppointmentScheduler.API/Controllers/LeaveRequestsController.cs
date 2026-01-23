using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione delle richieste di permesso/ferie
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;

    public LeaveRequestsController(ILeaveRequestService leaveRequestService)
    {
        _leaveRequestService = leaveRequestService;
    }

    /// <summary>
    /// Recupera tutte le richieste di permesso del merchant
    /// </summary>
    [HttpGet("merchant")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetMerchantRequests()
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var requests = await _leaveRequestService.GetMerchantLeaveRequestsAsync(merchantId);
        return Ok(requests);
    }

    /// <summary>
    /// Recupera le richieste di permesso del dipendente loggato
    /// </summary>
    [HttpGet("my-requests")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetMyRequests()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        var requests = await _leaveRequestService.GetEmployeeLeaveRequestsAsync(employeeId);
        return Ok(requests);
    }

    /// <summary>
    /// Recupera una richiesta specifica
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<LeaveRequestDto>> GetById(int id)
    {
        var request = await _leaveRequestService.GetLeaveRequestByIdAsync(id);

        if (request == null)
            return NotFound(new { message = "Richiesta non trovata" });

        return Ok(request);
    }

    /// <summary>
    /// Crea una nuova richiesta di permesso/ferie
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<LeaveRequestDto>> Create([FromBody] CreateLeaveRequest request)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        try
        {
            var leaveRequest = await _leaveRequestService.CreateLeaveRequestAsync(employeeId, request);
            return CreatedAtAction(nameof(GetById), new { id = leaveRequest.Id }, leaveRequest);
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
    /// Risponde a una richiesta di permesso (approvazione/rifiuto)
    /// </summary>
    [HttpPost("{id}/respond")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<LeaveRequestDto>> Respond(int id, [FromBody] RespondToLeaveRequest request)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        try
        {
            var leaveRequest = await _leaveRequestService.RespondToLeaveRequestAsync(id, merchantId, request);

            if (leaveRequest == null)
                return NotFound(new { message = "Richiesta non trovata" });

            return Ok(leaveRequest);
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
    /// Cancella una richiesta di permesso
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<IActionResult> Cancel(int id)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        try
        {
            var result = await _leaveRequestService.CancelLeaveRequestAsync(id, employeeId);

            if (!result)
                return NotFound(new { message = "Richiesta non trovata" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Recupera i saldi ferie del dipendente loggato
    /// </summary>
    [HttpGet("my-balances")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<IEnumerable<EmployeeLeaveBalanceDto>>> GetMyBalances([FromQuery] int? year)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        var targetYear = year ?? DateTime.UtcNow.Year;
        var balances = await _leaveRequestService.GetEmployeeLeaveBalancesAsync(employeeId, targetYear);
        return Ok(balances);
    }

    /// <summary>
    /// Recupera i saldi ferie di tutti i dipendenti del merchant
    /// </summary>
    [HttpGet("balances")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<EmployeeLeaveBalanceDto>>> GetMerchantBalances([FromQuery] int? year)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var targetYear = year ?? DateTime.UtcNow.Year;
        var balances = await _leaveRequestService.GetMerchantEmployeeLeaveBalancesAsync(merchantId, targetYear);
        return Ok(balances);
    }

    /// <summary>
    /// Crea o aggiorna un saldo ferie
    /// </summary>
    [HttpPost("balances")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<EmployeeLeaveBalanceDto>> UpsertBalance([FromBody] UpsertEmployeeLeaveBalanceRequest request)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        try
        {
            var balance = await _leaveRequestService.UpsertEmployeeLeaveBalanceAsync(merchantId, request);
            return Ok(balance);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nella gestione del saldo: {ex.Message}" });
        }
    }

    /// <summary>
    /// Elimina un saldo ferie
    /// </summary>
    [HttpDelete("balances/{balanceId}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> DeleteBalance(int balanceId)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        try
        {
            var result = await _leaveRequestService.DeleteEmployeeLeaveBalanceAsync(balanceId, merchantId);

            if (!result)
                return NotFound(new { message = "Saldo non trovato" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
