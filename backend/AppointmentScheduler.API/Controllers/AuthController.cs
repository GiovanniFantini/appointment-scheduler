using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPasswordResetService _passwordResetService;

    public AuthController(IAuthService authService, IPasswordResetService passwordResetService)
    {
        _authService = authService;
        _passwordResetService = passwordResetService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);

        if (response == null)
            return Unauthorized(new { message = "Email o password non validi" });

        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);

            if (response == null)
                return BadRequest(new { message = "Email già registrata" });

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Errore durante la registrazione. Riprova più tardi." });
        }
    }

    /// <summary>
    /// Richiede il reset della password. Risponde sempre 200 per sicurezza (anti-enumeration).
    /// Valido per tutti i tipi di utente: consumer, merchant, admin ed employee.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _passwordResetService.RequestPasswordResetAsync(request.Email);
        return Ok(new { message = "Se l'email risulta registrata, riceverai le istruzioni per il recupero della password." });
    }

    /// <summary>
    /// Imposta la nuova password utilizzando il token ricevuto via email.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var success = await _passwordResetService.ResetPasswordAsync(request.Token, request.NewPassword);

        if (!success)
            return BadRequest(new { message = "Il link non e' piu' valido. Richiedi un nuovo recupero password." });

        return Ok(new { message = "Password aggiornata con successo. Puoi ora accedere con la nuova password." });
    }
}
