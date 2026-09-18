using AppointmentScheduler.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.API.Controllers;

[ApiController, Route("api/subscription"), Authorize]
public sealed class SubscriptionController(AppointmentScheduler.Core.Services.IBranchService branches) : ControllerBase
{
    [HttpGet]
    public ActionResult<SubscriptionAccessDto> Get()
        => HttpContext.Items[typeof(SubscriptionAccessDto)] is SubscriptionAccessDto state
            ? Ok(state) : BadRequest(new { message = "Seleziona un’azienda." });

    [HttpGet("branches")]
    public async Task<IActionResult> BranchLookup()
    {
        if (HttpContext.Items[typeof(SubscriptionAccessDto)] is not SubscriptionAccessDto state
            || state.Status is not ("Active" or "Trial")) return Forbid();
        // Sedi e reparti sono riferimenti condivisi dai moduli operativi, anche senza gestione Filiali.
        return Ok(await branches.GetBranchesAsync(state.MerchantId, false));
    }
}
