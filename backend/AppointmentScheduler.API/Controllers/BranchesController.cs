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
[Authorize(Policy = "MerchantOnly")]
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

    [HttpPost]
    public async Task<ActionResult<MerchantBranchDto>> Create([FromBody] CreateBranchRequest request)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        try
        {
            var branch = await _branchService.CreateBranchAsync(merchantId, request);
            return CreatedAtAction(nameof(GetById), new { id = branch.Id }, branch);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MerchantBranchDto>> Update(int id, [FromBody] UpdateBranchRequest request)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        try
        {
            var branch = await _branchService.UpdateBranchAsync(id, merchantId, request);
            if (branch == null)
                return NotFound(new { message = "Filiale non trovata" });

            return Ok(branch);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        try
        {
            var ok = await _branchService.DeleteBranchAsync(id, merchantId);
            if (!ok)
                return NotFound(new { message = "Filiale non trovata" });

            return Ok(new { message = "Filiale eliminata" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/set-headquarters")]
    public async Task<IActionResult> SetHeadquarters(int id)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        try
        {
            var ok = await _branchService.SetHeadquartersAsync(id, merchantId);
            if (!ok)
                return NotFound(new { message = "Filiale non trovata" });

            return Ok(new { message = "Sede principale aggiornata" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Reparti ────────────────────────────────────────────────────────────

    [HttpPost("{branchId}/departments")]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment(int branchId, [FromBody] CreateDepartmentRequest request)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        try
        {
            var department = await _branchService.CreateDepartmentAsync(branchId, merchantId, request);
            return Ok(department);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("departments/{departmentId}")]
    public async Task<ActionResult<DepartmentDto>> UpdateDepartment(int departmentId, [FromBody] UpdateDepartmentRequest request)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        try
        {
            var department = await _branchService.UpdateDepartmentAsync(departmentId, merchantId, request);
            if (department == null)
                return NotFound(new { message = "Reparto non trovato" });

            return Ok(department);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("departments/{departmentId}")]
    public async Task<IActionResult> DeleteDepartment(int departmentId)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var ok = await _branchService.DeleteDepartmentAsync(departmentId, merchantId);
        if (!ok)
            return NotFound(new { message = "Reparto non trovato" });

        return Ok(new { message = "Reparto eliminato" });
    }
}
