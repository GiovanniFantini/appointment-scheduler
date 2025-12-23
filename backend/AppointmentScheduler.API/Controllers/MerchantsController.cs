using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione dei merchant (Admin)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class MerchantsController : ControllerBase
{
    private readonly IMerchantService _merchantService;

    public MerchantsController(IMerchantService merchantService)
    {
        _merchantService = merchantService;
    }

    /// <summary>
    /// Recupera tutti i merchant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MerchantDto>>> GetAll()
    {
        var merchants = await _merchantService.GetAllMerchantsAsync();
        return Ok(merchants);
    }

    /// <summary>
    /// Recupera i merchant in attesa di approvazione
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<MerchantDto>>> GetPending()
    {
        var merchants = await _merchantService.GetPendingMerchantsAsync();
        return Ok(merchants);
    }

    /// <summary>
    /// Recupera un merchant specifico
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MerchantDto>> GetById(int id)
    {
        var merchant = await _merchantService.GetMerchantByIdAsync(id);

        if (merchant == null)
            return NotFound(new { message = "Merchant non trovato" });

        return Ok(merchant);
    }

    /// <summary>
    /// Approva un merchant
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _merchantService.ApproveMerchantAsync(id);

        if (!result)
            return NotFound(new { message = "Merchant non trovato" });

        return Ok(new { message = "Merchant approvato con successo" });
    }

    /// <summary>
    /// Rifiuta o disabilita un merchant
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        var result = await _merchantService.RejectMerchantAsync(id);

        if (!result)
            return NotFound(new { message = "Merchant non trovato" });

        return Ok(new { message = "Merchant rifiutato con successo" });
    }
}
