using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Gestione globale degli utenti della piattaforma. Riservato al sys-admin.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IAdminUserService adminUserService,
        IPasswordResetService passwordResetService,
        ILogger<UsersController> logger)
    {
        _adminUserService = adminUserService;
        _passwordResetService = passwordResetService;
        _logger = logger;
    }

    /// <summary>Lista paginata utenti con filtri opzionali.</summary>
    [HttpGet]
    public async Task<ActionResult<AdminUserListResponse>> GetAll(
        [FromQuery] AccountType? accountType,
        [FromQuery] bool? isActive,
        [FromQuery] int? merchantId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await _adminUserService.GetUsersAsync(new AdminUserListQuery
        {
            AccountType = accountType,
            IsActive = isActive,
            MerchantId = merchantId,
            Search = search,
            Page = page,
            PageSize = pageSize
        });
        return Ok(result);
    }

    /// <summary>Dettaglio utente.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AdminUserDetailDto>> GetById(int id)
    {
        var user = await _adminUserService.GetByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "Utente non trovato" });

        return Ok(user);
    }

    /// <summary>Riattiva l'account di un utente (IsActive=true).</summary>
    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var ok = await _adminUserService.ActivateAsync(id);
        if (!ok)
            return NotFound(new { message = "Utente non trovato" });

        return Ok(new { message = "Account riattivato" });
    }

    /// <summary>Disattiva l'account di un utente (IsActive=false). Non puoi disattivare te stesso.</summary>
    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        if (!TryGetCurrentUserId(out var currentId))
            return Unauthorized();

        var result = await _adminUserService.DeactivateAsync(id, currentId);
        return result switch
        {
            DeactivateResult.NotFound => NotFound(new { message = "Utente non trovato" }),
            DeactivateResult.CannotDeactivateSelf => Conflict(new { message = "Non puoi disattivare il tuo stesso account" }),
            _ => Ok(new { message = "Account disattivato" })
        };
    }

    /// <summary>
    /// Invia un'email di reset password all'utente specificato.
    /// Riusa lo stesso flusso del forgot-password self-service: il sys-admin
    /// non vede il token, l'utente riceve l'email con il link di reset.
    /// </summary>
    [HttpPost("{id}/send-password-reset")]
    public async Task<IActionResult> SendPasswordReset(int id)
    {
        var user = await _adminUserService.GetByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "Utente non trovato" });

        await _passwordResetService.RequestPasswordResetAsync(user.Email);
        _logger.LogInformation("Sys-admin ha richiesto reset password per UserId {UserId}", id);

        return Ok(new { message = "Email di reset password inviata" });
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out userId);
    }
}
