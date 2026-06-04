using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

[ApiController]
[Route("api/auth")]
// Rate limit per IP su tutti gli endpoint di autenticazione pubblici. Il
// rate limit per-email (es. brute force su un singolo account) è applicato
// dentro AuthService perché richiede l'email già deserializzata dal binder.
[EnableRateLimiting("auth-ip")]
public class AuthController : ControllerBase
{
    // Risposta uniforme per i due register quando l'email è già usata o quando
    // la registrazione è andata effettivamente a buon fine ma non vogliamo
    // emettere subito un token (futuro: email verification). Anti-enumeration.
    private const string GenericRegisterAck =
        "Se i dati sono validi, riceverai a breve un'email di conferma.";

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
        try
        {
            var response = await _authService.RegisterMerchantAsync(request);
            // Email già registrata: rispondiamo come per il caso di successo
            // ma senza emettere un token. Anti-enumeration: un client non può
            // distinguere "email libera" da "email già in uso" leggendo la
            // risposta. AuthService manda all'utente legittimo una notifica
            // "qualcuno ha tentato di registrarsi con la tua email".
            if (response == null)
                return Ok(new { message = GenericRegisterAck });
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            // Validazione di dominio (es. password troppo corta): 400 con il messaggio.
            return BadRequest(new { message = ex.Message });
        }
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
        try
        {
            var response = await _authService.RegisterEmployeeAsync(request);
            if (response == null)
                return Ok(new { message = GenericRegisterAck });
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            // Validazione di dominio (es. password troppo corta): 400 con il messaggio.
            return BadRequest(new { message = ex.Message });
        }
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
        var result = await _passwordResetService.ResetPasswordAsync(request.Token, request.NewPassword);

        return result switch
        {
            ResetPasswordResult.Success =>
                Ok(new { message = "Password aggiornata con successo." }),

            // Esponiamo un codice macchina (reason) per permettere al frontend di
            // mostrare un messaggio specifico, mantenendo un testo leggibile lato server.
            ResetPasswordResult.InvalidInput =>
                BadRequest(new { reason = "invalid_input", message = "Dati non validi. Controlla la password e riprova." }),

            ResetPasswordResult.TokenExpired =>
                BadRequest(new { reason = "expired", message = "Il link è scaduto. Richiedi un nuovo recupero password." }),

            ResetPasswordResult.TokenAlreadyUsed =>
                BadRequest(new { reason = "used", message = "Il link è già stato utilizzato. Richiedi un nuovo recupero password." }),

            // TokenNotFound e qualsiasi altro caso: non riveliamo se il token esiste.
            _ =>
                BadRequest(new { reason = "not_found", message = "Il link non è valido. Richiedi un nuovo recupero password." })
        };
    }
}
