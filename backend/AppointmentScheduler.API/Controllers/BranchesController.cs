using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione di filiali (MerchantBranch) e reparti (Department).
/// Lato UI l'accesso è gated dalla feature MerchantFeature.Filiali.
/// </summary>
[ApiController]
[Route("api/branches")]
[Authorize(Policy = "ApprovedMerchantOnly")]
public class BranchesController : ControllerBase
{
    private readonly IBranchService _branchService;

    public BranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    private bool TryGetMerchantId(out int merchantId)
    {
        merchantId = 0;
        var claim = User.FindFirst("MerchantId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out merchantId);
    }

    // ── Filiali ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<List<MerchantBranchDto>>> GetAll([FromQuery] bool includeInactive = true)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var branches = await _branchService.GetBranchesAsync(merchantId, includeInactive);
        return Ok(branches);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MerchantBranchDto>> GetById(int id)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var branch = await _branchService.GetBranchByIdAsync(id, merchantId);
        if (branch == null)
            return NotFound(new { message = "Filiale non trovata" });

        return Ok(branch);
    }

    // ── Reparti ────────────────────────────────────────────────────────────
}
