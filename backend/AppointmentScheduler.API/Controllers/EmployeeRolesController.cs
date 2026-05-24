using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.API.Authorization;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Ruoli assegnabili alle risorse da parte dell'operativita' employee.
/// La configurazione dei ruoli resta in merchant app.
/// </summary>
[ApiController]
[Route("api/employee/roles")]
[Authorize(Policy = "EmployeeOnly")]
public class EmployeeRolesController : ControllerBase
{
    private readonly IMerchantRoleService _merchantRoleService;

    public EmployeeRolesController(IMerchantRoleService merchantRoleService)
    {
        _merchantRoleService = merchantRoleService;
    }

    private bool TryGetMerchantId(out int merchantId)
    {
        merchantId = 0;
        var claim = User.FindFirst("MerchantId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out merchantId);
    }

    [HttpGet]
    public async Task<ActionResult<List<MerchantRoleDto>>> GetAll()
    {
        if (!User.HasFeature(MerchantFeature.Risorse))
            return Forbid();

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var roles = await _merchantRoleService.GetRolesAsync(merchantId);
        return Ok(roles);
    }
}
