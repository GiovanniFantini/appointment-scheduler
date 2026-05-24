using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.API.Authorization;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione delle richieste dei dipendenti (ferie, cambio turno, permessi, malattia)
/// </summary>
[ApiController]
[Route("api/employee-requests")]
[Authorize]
public class EmployeeRequestsController : ControllerBase
{
    private readonly IEmployeeRequestService _requestService;

    public EmployeeRequestsController(IEmployeeRequestService requestService)
    {
        _requestService = requestService;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out userId);
    }

    private bool TryGetMerchantId(out int merchantId)
    {
        merchantId = 0;
        var claim = User.FindFirst("MerchantId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out merchantId);
    }

    private bool TryGetEmployeeId(out int employeeId)
    {
        employeeId = 0;
        var claim = User.FindFirst("EmployeeId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out employeeId);
    }

    /// <summary>
    /// Lista tutte le richieste del merchant (con filtro opzionale per status)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "ApprovedMerchantOnly")]
    public async Task<ActionResult<List<EmployeeRequestDto>>> GetAll(
        [FromQuery] RequestStatus? status = null)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var requests = await _requestService.GetMerchantRequestsAsync(merchantId, status);
        return Ok(requests);
    }

    /// <summary>
    /// Lista le richieste in attesa di approvazione per il dipendente abilitato.
    /// </summary>
    [HttpGet("approvals")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<List<EmployeeRequestDto>>> GetPendingApprovals()
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        if (!User.RequireFeatureLevel(MerchantFeature.Richieste, FeatureAccessLevel.Operator))
            return Forbid();

        var requests = await _requestService.GetMerchantRequestsAsync(merchantId, RequestStatus.Pending);
        return Ok(requests);
    }

    /// <summary>
    /// Dettaglio di una richiesta (lato employee approvatore)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<EmployeeRequestDto>> GetById(int id)
    {
        if (!User.RequireFeatureLevel(MerchantFeature.Richieste, FeatureAccessLevel.Operator))
            return Forbid();

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var request = await _requestService.GetByIdAsync(id, merchantId);
        if (request == null)
            return NotFound(new { message = "Richiesta non trovata" });

        return Ok(request);
    }

    /// <summary>
    /// Crea una nuova richiesta (Employee o Merchant per test)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<EmployeeRequestDto>> Create([FromBody] CreateEmployeeRequestRequest request)
    {
        if (!User.HasFeature(MerchantFeature.Richieste))
            return Forbid();

        if (!TryGetEmployeeId(out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato nel token" });

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        try
        {
            var result = await _requestService.CreateAsync(employeeId, merchantId, request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nella creazione della richiesta: {ex.Message}" });
        }
    }

    /// <summary>
    /// Approva una richiesta
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<EmployeeRequestDto>> Approve(int id, [FromBody] ReviewEmployeeRequestRequest? body = null)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        if (!TryGetUserId(out int userId))
            return BadRequest(new { message = "User ID non trovato nel token" });

        if (!User.RequireFeatureLevel(MerchantFeature.Richieste, FeatureAccessLevel.Operator))
            return Forbid();

        try
        {
            var result = await _requestService.ApproveAsync(id, merchantId, userId, body);
            if (result == null)
                return NotFound(new { message = "Richiesta non trovata o non autorizzata" });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Rifiuta una richiesta
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<EmployeeRequestDto>> Reject(int id, [FromBody] ReviewEmployeeRequestRequest? body = null)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        if (!TryGetUserId(out int userId))
            return BadRequest(new { message = "User ID non trovato nel token" });

        if (!User.RequireFeatureLevel(MerchantFeature.Richieste, FeatureAccessLevel.Operator))
            return Forbid();

        try
        {
            var result = await _requestService.RejectAsync(id, merchantId, userId, body);
            if (result == null)
                return NotFound(new { message = "Richiesta non trovata o non autorizzata" });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lista le richieste del dipendente corrente
    /// </summary>
    [HttpGet("my")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<List<EmployeeRequestDto>>> GetMyRequests(
        [FromQuery] RequestStatus? status = null)
    {
        if (!User.HasFeature(MerchantFeature.Richieste))
            return Forbid();

        if (!TryGetEmployeeId(out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato nel token" });

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var requests = await _requestService.GetEmployeeRequestsAsync(employeeId, merchantId, status);
        return Ok(requests);
    }

    /// <summary>
    /// Lista le richieste del merchant da mostrare nel calendario team.
    /// Disponibile per ruoli operativi del Calendario (Operator/Manager).
    /// </summary>
    [HttpGet("calendar")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<List<EmployeeRequestDto>>> GetCalendarRequests(
        [FromQuery] RequestStatus? status = null)
    {
        if (!User.RequireFeatureLevel(MerchantFeature.Calendario, FeatureAccessLevel.Operator))
            return Forbid();

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var requests = await _requestService.GetMerchantRequestsAsync(merchantId, status);
        return Ok(requests);
    }

    /// <summary>
    /// Elimina una richiesta del dipendente corrente, anche se già approvata.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!User.HasFeature(MerchantFeature.Richieste))
            return Forbid();

        if (!TryGetEmployeeId(out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato nel token" });

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var deleted = await _requestService.DeleteAsync(id, employeeId, merchantId);
        if (!deleted)
            return NotFound(new { message = "Richiesta non trovata o non autorizzata" });

        return Ok(new { message = "Richiesta eliminata con successo" });
    }
}
