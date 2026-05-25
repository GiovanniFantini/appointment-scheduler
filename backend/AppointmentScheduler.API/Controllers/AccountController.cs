using System.Security.Claims;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Self-service account: profilo + cambio password.
/// Accessibile a tutti gli account autenticati (Admin/Merchant/Employee) — usa lo stesso JWT.
/// </summary>
[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out userId);
    }

    [HttpGet("me")]
    public async Task<ActionResult<AccountProfileDto>> Me()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var profile = await _accountService.GetProfileAsync(userId);
        return profile == null ? NotFound() : Ok(profile);
    }

    [HttpPut("me")]
    public async Task<ActionResult<AccountProfileDto>> Update([FromBody] UpdateAccountProfileRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest(new { message = "Nome e cognome obbligatori" });
        var updated = await _accountService.UpdateProfileAsync(userId, request);
        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var (ok, error) = await _accountService.ChangePasswordAsync(userId, request);
        return ok ? Ok(new { message = "Password aggiornata" }) : BadRequest(new { message = error });
    }
}
