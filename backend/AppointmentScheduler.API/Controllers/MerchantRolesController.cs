using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione dei ruoli del merchant
/// </summary>
[ApiController]
[Route("api/merchant-roles")]
[Authorize(Policy = "MerchantOnly")]
public class MerchantRolesController : ControllerBase
{
    private readonly IMerchantRoleService _merchantRoleService;

    public MerchantRolesController(IMerchantRoleService merchantRoleService)
    {
        _merchantRoleService = merchantRoleService;
    }

    private bool TryGetMerchantId(out int merchantId)
    {
        merchantId = 0;
        var claim = User.FindFirst("MerchantId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out merchantId);
    }

    /// <summary>
    /// Recupera tutti i ruoli del merchant corrente
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<MerchantRoleDto>>> GetAll()
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var roles = await _merchantRoleService.GetRolesAsync(merchantId);
        return Ok(roles);
    }

    /// <summary>
    /// Recupera un ruolo specifico del merchant corrente
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MerchantRoleDto>> GetById(int id)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var role = await _merchantRoleService.GetByIdAsync(id, merchantId);

        if (role == null)
            return NotFound(new { message = "Ruolo non trovato" });

        return Ok(role);
    }

    /// <summary>
    /// Crea un nuovo ruolo per il merchant corrente
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MerchantRoleDto>> Create([FromBody] CreateMerchantRoleRequest request)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        try
        {
            var role = await _merchantRoleService.CreateAsync(merchantId, request);
            return CreatedAtAction(nameof(GetById), new { id = role.Id }, role);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nella creazione del ruolo: {ex.Message}" });
        }
    }

    /// <summary>
    /// Aggiorna un ruolo esistente del merchant corrente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<MerchantRoleDto>> Update(int id, [FromBody] UpdateMerchantRoleRequest request)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var role = await _merchantRoleService.UpdateAsync(id, merchantId, request);

        if (role == null)
            return NotFound(new { message = "Ruolo non trovato o non autorizzato" });

        return Ok(role);
    }

    /// <summary>
    /// Elimina un ruolo del merchant corrente.
    /// Non è possibile eliminare ruoli di default o con dipendenti assegnati.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        try
        {
            var result = await _merchantRoleService.DeleteAsync(id, merchantId);

            if (!result)
                return NotFound(new { message = "Ruolo non trovato o non autorizzato" });

            return Ok(new { message = "Ruolo eliminato con successo" });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Assegna un ruolo a un dipendente del merchant corrente
    /// </summary>
    [HttpPost("assign")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var result = await _merchantRoleService.AssignRoleAsync(merchantId, request);

        if (!result)
            return BadRequest(new { message = "Impossibile assegnare il ruolo. Verificare che il ruolo appartenga al merchant." });

        return Ok(new { message = "Ruolo assegnato con successo" });
    }
}
