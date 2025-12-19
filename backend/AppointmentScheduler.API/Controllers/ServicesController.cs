using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione dei servizi
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IServiceManagementService _serviceManagementService;

    public ServicesController(IServiceManagementService serviceManagementService)
    {
        _serviceManagementService = serviceManagementService;
    }

    /// <summary>
    /// Recupera tutti i servizi attivi (pubblico)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ServiceDto>>> GetActive([FromQuery] int? serviceType = null)
    {
        var services = await _serviceManagementService.GetActiveServicesAsync(serviceType);
        return Ok(services);
    }

    /// <summary>
    /// Recupera un servizio specifico
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceDto>> GetById(int id)
    {
        var service = await _serviceManagementService.GetServiceByIdAsync(id);

        if (service == null)
            return NotFound(new { message = "Servizio non trovato" });

        return Ok(service);
    }

    /// <summary>
    /// Recupera tutti i servizi del merchant corrente
    /// </summary>
    [HttpGet("my-services")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<ServiceDto>>> GetMyServices()
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato. Assicurati di essere registrato come merchant." });

        var services = await _serviceManagementService.GetMerchantServicesAsync(merchantId);
        return Ok(services);
    }

    /// <summary>
    /// Crea un nuovo servizio
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<ServiceDto>> Create([FromBody] CreateServiceRequest request)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        try
        {
            var service = await _serviceManagementService.CreateServiceAsync(merchantId, request);
            return CreatedAtAction(nameof(GetById), new { id = service.Id }, service);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nella creazione del servizio: {ex.Message}" });
        }
    }

    /// <summary>
    /// Aggiorna un servizio esistente
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<ServiceDto>> Update(int id, [FromBody] UpdateServiceRequest request)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var service = await _serviceManagementService.UpdateServiceAsync(id, merchantId, request);

        if (service == null)
            return NotFound(new { message = "Servizio non trovato o non autorizzato" });

        return Ok(service);
    }

    /// <summary>
    /// Elimina un servizio
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var result = await _serviceManagementService.DeleteServiceAsync(id, merchantId);

        if (!result)
            return NotFound(new { message = "Servizio non trovato o non autorizzato" });

        return Ok(new { message = "Servizio eliminato con successo" });
    }
}
