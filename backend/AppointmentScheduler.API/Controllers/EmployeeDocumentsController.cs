using AppointmentScheduler.API.Authorization;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Endpoint documentali self-service per il dipendente corrente.
/// </summary>
[ApiController]
[Route("api/employee/documents")]
[Authorize(Policy = "EmployeeOnly")]
public class EmployeeDocumentsController : ControllerBase
{
    private readonly IHRDocumentService _hrDocumentService;
    private readonly IEmployeeService _employeeService;

    public EmployeeDocumentsController(
        IHRDocumentService hrDocumentService,
        IEmployeeService employeeService)
    {
        _hrDocumentService = hrDocumentService;
        _employeeService = employeeService;
    }

    [HttpGet("upload-targets")]
    public async Task<ActionResult<IEnumerable<DocumentUploadTargetDto>>> GetUploadTargets()
    {
        if (RequireLevel(FeatureAccessLevel.Operator) is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });

        var employees = await _employeeService.GetMerchantEmployeesAsync(merchantId);
        var targets = employees
            .Where(e => e.IsActive)
            .Select(e => new DocumentUploadTargetDto
            {
                EmployeeId = e.Id,
                FullName = e.FullName,
                Kind = e.Kind,
                HasUserAccount = e.HasUserAccount
            })
            .OrderBy(t => t.FullName)
            .ToList();

        return Ok(targets);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HRDocumentDto>>> GetMyDocuments(
        [FromQuery] HRDocumentType? documentType = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        if (RequireLevel(FeatureAccessLevel.ReadOnly) is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId) || !TryGetEmployeeId(out int employeeId))
            return BadRequest(new { message = "Token non valido" });

        var documents = await _hrDocumentService.GetEmployeeDocumentsAsync(
            merchantId,
            employeeId,
            documentType,
            year,
            month);

        return Ok(documents);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<HRDocumentDetailDto>> GetMyDocumentById(int id)
    {
        if (RequireLevel(FeatureAccessLevel.ReadOnly) is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId) || !TryGetEmployeeId(out int employeeId))
            return BadRequest(new { message = "Token non valido" });

        var document = await _hrDocumentService.GetEmployeeDocumentByIdAsync(id, merchantId, employeeId);
        if (document == null)
            return NotFound(new { message = "Documento non trovato" });

        return Ok(document);
    }

    [HttpGet("{id}/download")]
    public async Task<ActionResult<HRDocumentDownloadDto>> DownloadMyDocument(int id, [FromQuery] int? versionNumber = null)
    {
        if (RequireLevel(FeatureAccessLevel.ReadOnly) is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId) || !TryGetEmployeeId(out int employeeId))
            return BadRequest(new { message = "Token non valido" });

        try
        {
            var result = await _hrDocumentService.GenerateEmployeeDownloadUrlAsync(id, merchantId, employeeId, versionNumber);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<HRDocumentUploadResponseDto>> CreateDocumentForEmployee([FromBody] HRDocumentCreateDto dto)
    {
        if (RequireLevel(FeatureAccessLevel.Operator) is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId) || !TryGetUserId(out int userId))
            return BadRequest(new { message = "Token non valido" });

        try
        {
            var result = await _hrDocumentService.CreateDocumentAsync(merchantId, userId, dto);
            return CreatedAtAction(nameof(GetMyDocumentById), new { id = result.DocumentId }, result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Errore durante la creazione del documento", error = ex.Message });
        }
    }

    [HttpPost("{id}/versions")]
    public async Task<ActionResult<HRDocumentVersionUploadResponseDto>> AddVersion(
        int id,
        [FromBody] AddVersionRequest? request = null)
    {
        if (RequireLevel(FeatureAccessLevel.Operator) is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId) || !TryGetUserId(out int userId))
            return BadRequest(new { message = "Token non valido" });

        try
        {
            var result = await _hrDocumentService.AddDocumentVersionAsync(
                id,
                merchantId,
                userId,
                request?.ChangeNotes);

            return CreatedAtAction(nameof(GetMyDocumentById), new { id }, result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Errore durante l'aggiunta della versione", error = ex.Message });
        }
    }

    [HttpPut("{id}/finalize")]
    public async Task<ActionResult> FinalizeUpload(int id, [FromBody] HRDocumentFinalizeDto dto)
    {
        if (RequireLevel(FeatureAccessLevel.Operator) is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });

        try
        {
            var success = await _hrDocumentService.FinalizeDocumentUploadAsync(id, merchantId, dto);
            if (!success)
                return BadRequest(new { message = "Finalize non riuscita: file non valido o upload incompleto" });

            return Ok(new { success = true, documentId = id, status = "Published" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Errore durante la finalizzazione", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteDocument(int id)
    {
        if (RequireLevel(FeatureAccessLevel.Manager) is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });

        try
        {
            var success = await _hrDocumentService.DeleteDocumentAsync(id, merchantId);
            if (!success)
                return NotFound(new { message = "Documento non trovato" });

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Errore durante la cancellazione", error = ex.Message });
        }
    }

    private bool TryGetMerchantId(out int merchantId)
    {
        merchantId = 0;
        var claim = User.FindFirst("MerchantId")?.Value;
        return !string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out merchantId);
    }

    private bool TryGetEmployeeId(out int employeeId)
    {
        employeeId = 0;
        var claim = User.FindFirst("EmployeeId")?.Value;
        return !string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out employeeId);
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out userId);
    }

    private ActionResult? RequireLevel(FeatureAccessLevel minimumLevel)
    {
        if (!User.HasFeature(MerchantFeature.Documenti))
            return Forbid();
        if (!User.RequireFeatureLevel(MerchantFeature.Documenti, minimumLevel))
            return Forbid();
        return null;
    }
}

/// <summary>
/// Request per aggiungere versione
/// </summary>
public class AddVersionRequest
{
    public string? ChangeNotes { get; set; }
}
