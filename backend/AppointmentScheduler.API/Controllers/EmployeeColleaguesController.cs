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
    /// Recupera tutti i merchant per cui l'employee corrente lavora
    /// </summary>
    [HttpGet("my-merchants")]
    public async Task<ActionResult<IEnumerable<EmployeeMerchantDto>>> GetMyMerchants()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return BadRequest(new { message = "User ID non trovato nel token. Effettua nuovamente il login." });

        // Recupera tutti gli Employee record per questo user
        var employeeRecords = await _context.Employees
            .Include(e => e.Merchant)
            .Where(e => e.UserId == userId && e.IsActive)
            .OrderBy(e => e.Merchant.BusinessName)
            .Select(e => new EmployeeMerchantDto
            {
                EmployeeId = e.Id,
                MerchantId = e.MerchantId,
                BusinessName = e.Merchant.BusinessName,
                Role = e.Role,
                BadgeCode = e.BadgeCode
            })
            .ToListAsync();

        return Ok(employeeRecords);
    }

    /// <summary>
    /// Recupera tutti i colleghi dell'employee corrente per un merchant specifico
    /// </summary>
    [HttpGet("merchant/{merchantId}")]
    public async Task<ActionResult<IEnumerable<ColleagueDto>>> GetColleaguesByMerchant(int merchantId)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return BadRequest(new { message = "User ID non trovato nel token" });

        // Verifica che l'employee lavori per questo merchant
        var currentEmployeeRecord = await _context.Employees
            .FirstOrDefaultAsync(e => e.UserId == userId && e.MerchantId == merchantId && e.IsActive);

        if (currentEmployeeRecord == null)
            return Forbid(); // Non lavora per questo merchant

        // Recupera tutti i colleghi dello stesso merchant
        var colleagues = await _context.Employees
            .Where(e => e.MerchantId == merchantId && e.IsActive)
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
    /// Recupera tutti i colleghi dell'employee corrente (tutti i merchant)
    /// DEPRECATO: Usa GetColleaguesByMerchant specificando il merchant
    /// </summary>
    [HttpGet]
    [Obsolete("Use GET /merchant/{merchantId} instead")]
    public async Task<ActionResult<IEnumerable<ColleagueDto>>> GetColleagues()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return BadRequest(new { message = "User ID non trovato nel token. Effettua nuovamente il login." });

        // Recupera il primo merchant per cui lavora (backward compatibility)
        var firstEmployeeRecord = await _context.Employees
            .FirstOrDefaultAsync(e => e.UserId == userId && e.IsActive);

        if (firstEmployeeRecord == null)
            return NotFound(new { message = "Nessun merchant trovato per questo employee" });

        // Recupera tutti i colleghi dello stesso merchant
        var colleagues = await _context.Employees
            .Where(e => e.MerchantId == firstEmployeeRecord.MerchantId && e.IsActive)
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
