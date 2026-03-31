using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPasswordResetService _passwordResetService;

    public AuthController(IAuthService authService, IPasswordResetService passwordResetService)
    {
        _authService = authService;
        _passwordResetService = passwordResetService;
    }

    /// <summary>Login per merchant (AccountType=Merchant).</summary>
    [HttpPost("merchant/login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> MerchantLogin([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginMerchantAsync(request);
        if (response == null)
            return Unauthorized(new { message = "Email o password non validi" });
        return Ok(response);
    }

    /// <summary>Registrazione merchant: crea account + profilo azienda (richiede approvazione admin).</summary>
    [HttpPost("merchant/register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> MerchantRegister([FromBody] RegisterMerchantRequest request)
    {
        var response = await _authService.RegisterMerchantAsync(request);
        if (response == null)
            return BadRequest(new { message = "Email già registrata" });
        return Ok(response);
    }

    /// <summary>Login per admin (AccountType=Admin).</summary>
    [HttpPost("admin/login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> AdminLogin([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAdminAsync(request);
        if (response == null)
            return Unauthorized(new { message = "Email o password non validi" });
        return Ok(response);
    }

    /// <summary>Login per employee (AccountType=Employee). Ritorna lista aziende disponibili.</summary>
    [HttpPost("employee/login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> EmployeeLogin([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginEmployeeAsync(request);
        if (response == null)
            return Unauthorized(new { message = "Email o password non validi" });
        return Ok(response);
    }

    /// <summary>Registrazione employee autonoma.</summary>
    [HttpPost("employee/register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> EmployeeRegister([FromBody] EmployeeRegisterRequest request)
    {
        var response = await _authService.RegisterEmployeeAsync(request);
        if (response == null)
            return BadRequest(new { message = "Email già registrata" });
        return Ok(response);
    }

    /// <summary>
    /// Seleziona l'azienda attiva per l'employee.
    /// Ritorna un nuovo JWT con MerchantId e Features nel claim.
    /// </summary>
    [HttpPost("employee/select-company/{merchantId:int}")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<AuthResponse>> SelectCompany(int merchantId)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var response = await _authService.SelectCompanyAsync(userId, merchantId);
        if (response == null)
            return Forbid();

        return Ok(response);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _passwordResetService.RequestPasswordResetAsync(request.Email);
        return Ok(new { message = "Se l'email risulta registrata, riceverai le istruzioni per il recupero della password." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var success = await _passwordResetService.ResetPasswordAsync(request.Token, request.NewPassword);
        if (!success)
            return BadRequest(new { message = "Il link non è più valido. Richiedi un nuovo recupero password." });
        return Ok(new { message = "Password aggiornata con successo." });
    }
}
