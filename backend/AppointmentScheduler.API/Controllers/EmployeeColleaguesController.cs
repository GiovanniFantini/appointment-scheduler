using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione dei colleghi (per l'app employee)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "EmployeeOnly")]
public class EmployeeColleaguesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EmployeeColleaguesController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Recupera tutti i colleghi dell'employee corrente (stesso merchant)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ColleagueDto>>> GetColleagues()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato nel token. Effettua nuovamente il login." });

        // Recupera l'employee corrente per ottenere il MerchantId
        var currentEmployee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (currentEmployee == null)
            return NotFound(new { message = "Dipendente non trovato" });

        // Recupera tutti i colleghi dello stesso merchant (escluso l'employee corrente)
        var colleagues = await _context.Employees
            .Where(e => e.MerchantId == currentEmployee.MerchantId && e.IsActive)
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .Select(e => new ColleagueDto
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                FullName = e.FirstName + " " + e.LastName,
                Email = e.Email,
                PhoneNumber = e.PhoneNumber,
                BadgeCode = e.BadgeCode,
                Role = e.Role,
                IsActive = e.IsActive
            })
            .ToListAsync();

        return Ok(colleagues);
    }

    /// <summary>
    /// Recupera i dettagli di un collega specifico
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ColleagueDto>> GetColleagueById(int id)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato nel token" });

        // Recupera l'employee corrente per verificare il merchant
        var currentEmployee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (currentEmployee == null)
            return NotFound(new { message = "Dipendente non trovato" });

        // Recupera il collega richiesto
        var colleague = await _context.Employees
            .Where(e => e.Id == id && e.MerchantId == currentEmployee.MerchantId && e.IsActive)
            .Select(e => new ColleagueDto
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                FullName = e.FirstName + " " + e.LastName,
                Email = e.Email,
                PhoneNumber = e.PhoneNumber,
                BadgeCode = e.BadgeCode,
                Role = e.Role,
                IsActive = e.IsActive
            })
            .FirstOrDefaultAsync();

        if (colleague == null)
            return NotFound(new { message = "Collega non trovato o non appartenente allo stesso merchant" });

        return Ok(colleague);
    }
}
