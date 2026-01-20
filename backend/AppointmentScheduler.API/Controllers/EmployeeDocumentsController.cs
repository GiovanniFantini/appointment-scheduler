using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la consultazione documenti da parte dei dipendenti
/// </summary>
[ApiController]
[Route("api/employee/documents")]
[Authorize(Policy = "EmployeeOnly")]
public class EmployeeDocumentsController : ControllerBase
{
    private readonly IHRDocumentService _hrDocumentService;
    private readonly ApplicationDbContext _context;

    public EmployeeDocumentsController(
        IHRDocumentService hrDocumentService,
        ApplicationDbContext context)
    {
        _hrDocumentService = hrDocumentService;
        _context = context;
    }

    /// <summary>
    /// Lista documenti del dipendente autenticato (solo Published)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HRDocumentDto>>> GetMyDocuments(
        [FromQuery] HRDocumentType? documentType = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var employeeId = await GetEmployeeIdFromUserAsync();
        if (employeeId == null)
            return BadRequest(new { message = "Employee ID non trovato. Assicurati di essere un dipendente registrato." });

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId.Value);

        if (employee == null)
            return NotFound(new { message = "Dipendente non trovato" });

        var documents = await _hrDocumentService.GetDocumentsAsync(
            employee.MerchantId,
            employeeId.Value,
            documentType,
            year,
            month,
            HRDocumentStatus.Published); // Solo documenti pubblicati

        return Ok(documents);
    }

    /// <summary>
    /// Dettaglio documento (solo se appartiene al dipendente)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<HRDocumentDetailDto>> GetDocumentById(int id)
    {
        var employeeId = await GetEmployeeIdFromUserAsync();
        if (employeeId == null)
            return BadRequest(new { message = "Employee ID non trovato" });

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId.Value);

        if (employee == null)
            return NotFound(new { message = "Dipendente non trovato" });

        var document = await _hrDocumentService.GetDocumentByIdAsync(id, employee.MerchantId);

        if (document == null || document.EmployeeId != employeeId.Value)
            return NotFound(new { message = "Documento non trovato" });

        if (document.Status != HRDocumentStatus.Published)
            return Forbid();

        return Ok(document);
    }

    /// <summary>
    /// Download documento (versione corrente o specifica)
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<ActionResult<HRDocumentDownloadDto>> DownloadDocument(
        int id,
        [FromQuery] int? versionNumber = null)
    {
        var employeeId = await GetEmployeeIdFromUserAsync();
        if (employeeId == null)
            return BadRequest(new { message = "Employee ID non trovato" });

        try
        {
            var result = await _hrDocumentService.GenerateEmployeeDownloadUrlAsync(
                id,
                employeeId.Value,
                versionNumber);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Errore durante la generazione URL", error = ex.Message });
        }
    }

    /// <summary>
    /// Ottiene l'employeeId dall'utente autenticato
    /// </summary>
    private async Task<int?> GetEmployeeIdFromUserAsync()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.UserId == userId);

        return employee?.Id;
    }
}
