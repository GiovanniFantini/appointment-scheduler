using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.API.Authorization;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Gestione operativa filiali e reparti lato employee app.
/// </summary>
[ApiController]
[Route("api/employee/branches")]
[Authorize(Policy = "EmployeeOnly")]
public class EmployeeBranchesController : ControllerBase
{
    private readonly IBranchService _branchService;

    public EmployeeBranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    private bool TryGetMerchantId(out int merchantId)
    {
        merchantId = 0;
        var claim = User.FindFirst("MerchantId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out merchantId);
    }

    private ActionResult? RequireFeature()
    {
        if (!User.HasFeature(MerchantFeature.Filiali))
            return Forbid();
        return null;
    }

    [HttpGet]
    public async Task<ActionResult<List<MerchantBranchDto>>> GetAll([FromQuery] bool includeInactive = true)
    {
        if (RequireFeature() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var branches = await _branchService.GetBranchesAsync(merchantId, includeInactive);
        return Ok(branches);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MerchantBranchDto>> GetById(int id)
    {
        if (RequireFeature() is { } forbidden)
            return forbidden;

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
        if (RequireFeature() is { } forbidden)
            return forbidden;

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
        if (RequireFeature() is { } forbidden)
            return forbidden;

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
        if (RequireFeature() is { } forbidden)
            return forbidden;

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
        if (RequireFeature() is { } forbidden)
            return forbidden;

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

    [HttpPost("{branchId}/departments")]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment(int branchId, [FromBody] CreateDepartmentRequest request)
    {
        if (RequireFeature() is { } forbidden)
            return forbidden;

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
        if (RequireFeature() is { } forbidden)
            return forbidden;

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
        if (RequireFeature() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var ok = await _branchService.DeleteDepartmentAsync(departmentId, merchantId);
        if (!ok)
            return NotFound(new { message = "Reparto non trovato" });

        return Ok(new { message = "Reparto eliminato" });
    }
}
