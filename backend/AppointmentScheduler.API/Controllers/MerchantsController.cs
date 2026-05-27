using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione dei merchant
/// </summary>
[ApiController]
[Route("api/merchants")]
[Authorize]
public class MerchantsController : ControllerBase
{
    private readonly IMerchantService _merchantService;
    private readonly IMerchantRoleService _merchantRoleService;

    public MerchantsController(IMerchantService merchantService, IMerchantRoleService merchantRoleService)
    {
        _merchantService = merchantService;
        _merchantRoleService = merchantRoleService;
    }

    /// <summary>
    /// Recupera tutti i merchant (solo Admin)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<List<MerchantDto>>> GetAll()
    {
        var merchants = await _merchantService.GetAllAsync();
        return Ok(merchants);
    }

    /// <summary>
    /// Recupera i merchant in attesa di approvazione (solo Admin)
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<List<MerchantDto>>> GetPending()
    {
        var merchants = await _merchantService.GetPendingAsync();
        return Ok(merchants);
    }

    /// <summary>
    /// Recupera un merchant per ID.
    /// Admin può accedere a qualsiasi merchant; un Merchant può accedere solo al proprio.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<MerchantDto>> GetById(int id)
    {
        var merchant = await _merchantService.GetByIdAsync(id);

        if (merchant == null)
            return NotFound(new { message = "Merchant non trovato" });

        return Ok(merchant);
    }

    /// <summary>
    /// Approva un merchant (solo Admin)
    /// </summary>
    [HttpPatch("{id}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _merchantService.ApproveAsync(id);

        if (!result)
            return NotFound(new { message = "Merchant non trovato" });

        return Ok(new { message = "Merchant approvato con successo" });
    }

    /// <summary>
    /// Rifiuta o disabilita un merchant (solo Admin)
    /// </summary>
    [HttpPatch("{id}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Reject(int id)
    {
        var result = await _merchantService.RejectAsync(id);

        if (!result)
            return NotFound(new { message = "Merchant non trovato" });

        return Ok(new { message = "Merchant rifiutato con successo" });
    }

    /// <summary>
    /// Aggiorna i dati di un merchant.
    /// Un merchant può aggiornare solo il proprio profilo; Admin può aggiornare qualsiasi merchant.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<MerchantDto>> Update(int id, [FromBody] UpdateMerchantRequest request)
    {
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin)
        {
            // Verify the merchant is updating their own record
            var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
            if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int claimMerchantId) || claimMerchantId != id)
                return Forbid();
        }

        var merchant = await _merchantService.UpdateAsync(id, request);

        if (merchant == null)
            return NotFound(new { message = "Merchant non trovato" });

        return Ok(merchant);
    }

    /// <summary>
    /// Recupera i ruoli di un merchant (solo Admin).
    /// </summary>
    [HttpGet("{id}/roles")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<List<MerchantRoleDto>>> GetRoles(int id)
    {
        var merchant = await _merchantService.GetByIdAsync(id);
        if (merchant == null)
            return NotFound(new { message = "Merchant non trovato" });

        var roles = await _merchantRoleService.GetRolesAsync(id);
        return Ok(roles);
    }

    /// <summary>
    /// Recupera lo stato di abilitazione delle feature del merchant (sul ruolo predefinito).
    /// Ritorna sempre tutte le 10 MerchantFeature; quelle non presenti nel ruolo default
    /// sono restituite con IsEnabled=false e AccessLevel=null.
    /// </summary>
    [HttpGet("{id}/features")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<List<RoleFeatureDto>>> GetFeatures(int id)
    {
        var merchant = await _merchantService.GetByIdAsync(id);
        if (merchant == null)
            return NotFound(new { message = "Merchant non trovato" });

        var defaultRole = await _merchantRoleService.GetDefaultRoleAsync(id);
        if (defaultRole == null)
            return NotFound(new { message = "Ruolo predefinito del merchant non trovato" });

        var byFeature = defaultRole.Features.ToDictionary(f => f.Feature);
        var allFeatures = Enum.GetValues<MerchantFeature>()
            .Select(f => byFeature.TryGetValue(f, out var existing)
                ? existing
                : new RoleFeatureDto { Feature = f, IsEnabled = false, AccessLevel = null })
            .ToList();

        return Ok(allFeatures);
    }

    /// <summary>
    /// Aggiorna lo stato delle feature del merchant operando sul ruolo predefinito (solo Admin).
    /// </summary>
    [HttpPut("{id}/features")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<MerchantRoleDto>> UpdateFeatures(int id, [FromBody] List<MerchantFeatureRequest> features)
    {
        var merchant = await _merchantService.GetByIdAsync(id);
        if (merchant == null)
            return NotFound(new { message = "Merchant non trovato" });

        var updated = await _merchantRoleService.UpdateDefaultRoleFeaturesAsync(id, features);
        if (updated == null)
            return NotFound(new { message = "Ruolo predefinito del merchant non trovato" });

        return Ok(updated);
    }
}
