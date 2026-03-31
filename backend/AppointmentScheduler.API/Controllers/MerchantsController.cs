using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

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

    public MerchantsController(IMerchantService merchantService)
    {
        _merchantService = merchantService;
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
}
