using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione dei template di turni
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ShiftTemplatesController : ControllerBase
{
    private readonly IShiftTemplateService _shiftTemplateService;

    public ShiftTemplatesController(IShiftTemplateService shiftTemplateService)
    {
        _shiftTemplateService = shiftTemplateService;
    }

    /// <summary>
    /// Recupera tutti i template di turni del merchant corrente
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<ShiftTemplateDto>>> GetMyTemplates()
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var templates = await _shiftTemplateService.GetMerchantShiftTemplatesAsync(merchantId);
        return Ok(templates);
    }

    /// <summary>
    /// Recupera un template specifico
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<ShiftTemplateDto>> GetById(int id)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var template = await _shiftTemplateService.GetShiftTemplateByIdAsync(id);

        if (template == null)
            return NotFound(new { message = "Template non trovato" });

        if (template.MerchantId != merchantId)
            return Forbid();

        return Ok(template);
    }

    /// <summary>
    /// Crea un nuovo template
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<ShiftTemplateDto>> Create([FromBody] CreateShiftTemplateRequest request)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        try
        {
            var template = await _shiftTemplateService.CreateShiftTemplateAsync(merchantId, request);
            return CreatedAtAction(nameof(GetById), new { id = template.Id }, template);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nella creazione del template: {ex.Message}" });
        }
    }

    /// <summary>
    /// Aggiorna un template esistente
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<ShiftTemplateDto>> Update(int id, [FromBody] UpdateShiftTemplateRequest request)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        try
        {
            var template = await _shiftTemplateService.UpdateShiftTemplateAsync(id, merchantId, request);

            if (template == null)
                return NotFound(new { message = "Template non trovato o non autorizzato" });

            return Ok(template);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nell'aggiornamento del template: {ex.Message}" });
        }
    }

    /// <summary>
    /// Elimina un template
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var result = await _shiftTemplateService.DeleteShiftTemplateAsync(id, merchantId);

        if (!result)
            return NotFound(new { message = "Template non trovato" });

        return NoContent();
    }
}
