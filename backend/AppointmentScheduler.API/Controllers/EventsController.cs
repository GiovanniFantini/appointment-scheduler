using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione degli eventi aziendali
/// </summary>
[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
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
    /// Recupera tutti gli eventi del merchant.
    /// I merchant leggono il MerchantId dal JWT; gli Admin possono specificarlo come query param.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<List<EventDto>>> GetMerchantEvents(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] EventType? type,
        [FromQuery] int? merchantId = null)
    {
        int resolvedMerchantId;

        if (User.IsInRole("Admin") && merchantId.HasValue)
        {
            resolvedMerchantId = merchantId.Value;
        }
        else
        {
            if (!TryGetMerchantId(out resolvedMerchantId))
                return BadRequest(new { message = "Merchant ID non trovato nel token" });
        }

        var events = await _eventService.GetMerchantEventsAsync(resolvedMerchantId, from, to, type);
        return Ok(events);
    }

    /// <summary>
    /// Recupera gli eventi del dipendente corrente nel merchant selezionato
    /// </summary>
    [HttpGet("employee")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<List<EventDto>>> GetEmployeeEvents(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        if (!TryGetEmployeeId(out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato nel token" });

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var events = await _eventService.GetEmployeeEventsAsync(employeeId, merchantId, from, to);
        return Ok(events);
    }

    /// <summary>
    /// Recupera un evento per ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<EventDto>> GetById(int id)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var evt = await _eventService.GetByIdAsync(id, merchantId);

        if (evt == null)
            return NotFound(new { message = "Evento non trovato" });

        return Ok(evt);
    }

    /// <summary>
    /// Crea un nuovo evento
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<EventDto>> Create([FromBody] CreateEventRequest request)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        if (!TryGetUserId(out int userId))
            return BadRequest(new { message = "User ID non trovato nel token" });

        try
        {
            var evt = await _eventService.CreateAsync(merchantId, userId, request);
            return CreatedAtAction(nameof(GetById), new { id = evt.Id }, evt);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nella creazione dell'evento: {ex.Message}" });
        }
    }

    /// <summary>
    /// Aggiorna un evento esistente
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<EventDto>> Update(int id, [FromBody] UpdateEventRequest request)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var evt = await _eventService.UpdateAsync(id, merchantId, request);

        if (evt == null)
            return NotFound(new { message = "Evento non trovato o non autorizzato" });

        return Ok(evt);
    }

    /// <summary>
    /// Elimina un evento
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var result = await _eventService.DeleteAsync(id, merchantId);

        if (!result)
            return NotFound(new { message = "Evento non trovato o non autorizzato" });

        return Ok(new { message = "Evento eliminato con successo" });
    }

    /// <summary>
    /// Clona un evento in un intervallo di date (un clone per ogni giorno)
    /// </summary>
    [HttpPost("{id}/clone")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<List<EventDto>>> Clone(int id, [FromBody] CloneEventRequest request)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        if (request.FromDate > request.ToDate)
            return BadRequest(new { message = "La data di inizio deve essere precedente o uguale alla data di fine" });

        try
        {
            var cloned = await _eventService.CloneAsync(id, merchantId, request);

            if (cloned.Count == 0)
                return NotFound(new { message = "Evento originale non trovato o non autorizzato" });

            return Ok(cloned);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nella clonazione dell'evento: {ex.Message}" });
        }
    }
}
