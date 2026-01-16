using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeAuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public EmployeeAuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] EmployeeRegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterEmployeeAsync(request);

            if (response == null)
                return BadRequest(new { message = "Errore durante la registrazione" });

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

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);

        if (response == null)
            return Unauthorized(new { message = "Email o password non validi" });

        // Verifica che sia effettivamente un employee
        if (!response.IsEmployee)
            return Unauthorized(new { message = "Accesso riservato ai dipendenti" });

        return Ok(response);
    }
}
