using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione documenti HR/Payroll (per Merchant/HR)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "MerchantOnly")]
public class HRDocumentsController : ControllerBase
{
    private readonly IHRDocumentService _hrDocumentService;

    public HRDocumentsController(IHRDocumentService hrDocumentService)
    {
        _hrDocumentService = hrDocumentService;
    }

    /// <summary>
    /// Lista documenti HR per il tenant corrente (con filtri opzionali)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HRDocumentDto>>> GetDocuments(
        [FromQuery] int? employeeId = null,
        [FromQuery] HRDocumentType? documentType = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] HRDocumentStatus? status = null)
    {
        var tenantId = GetTenantId();
        if (tenantId == null)
            return BadRequest(new { message = "Tenant ID non trovato" });

        var documents = await _hrDocumentService.GetDocumentsAsync(
            tenantId.Value,
            employeeId,
            documentType,
            year,
            month,
            status);

        return Ok(documents);
    }

    /// <summary>
    /// Dettaglio documento con tutte le versioni
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<HRDocumentDetailDto>> GetDocumentById(int id)
    {
        var tenantId = GetTenantId();
        if (tenantId == null)
            return BadRequest(new { message = "Tenant ID non trovato" });

        var document = await _hrDocumentService.GetDocumentByIdAsync(id, tenantId.Value);

        if (document == null)
            return NotFound(new { message = "Documento non trovato" });

        return Ok(document);
    }

    /// <summary>
    /// Crea nuovo documento HR e restituisce upload URL
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<HRDocumentUploadResponseDto>> CreateDocument(
        [FromBody] HRDocumentCreateDto dto)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();

        if (tenantId == null || userId == null)
            return BadRequest(new { message = "Tenant ID o User ID non trovato" });

        try
        {
            var result = await _hrDocumentService.CreateDocumentAsync(
                tenantId.Value,
                userId.Value,
                dto);

            return CreatedAtAction(nameof(GetDocumentById), new { id = result.DocumentId }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Errore durante la creazione del documento", error = ex.Message });
        }
    }

    /// <summary>
    /// Finalizza upload documento (dopo che il file è stato caricato)
    /// </summary>
    [HttpPut("{id}/finalize")]
    public async Task<ActionResult> FinalizeUpload(int id, [FromBody] HRDocumentFinalizeDto dto)
    {
        var tenantId = GetTenantId();
        if (tenantId == null)
            return BadRequest(new { message = "Tenant ID non trovato" });

        try
        {
            var success = await _hrDocumentService.FinalizeDocumentUploadAsync(id, tenantId.Value, dto);

            if (!success)
                return NotFound(new { message = "Documento non trovato o upload non riuscito" });

            return Ok(new { success = true, documentId = id, status = "Published" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Errore durante la finalizzazione", error = ex.Message });
        }
    }

    /// <summary>
    /// Aggiunge nuova versione al documento
    /// </summary>
    [HttpPost("{id}/versions")]
    public async Task<ActionResult<HRDocumentVersionUploadResponseDto>> AddVersion(
        int id,
        [FromBody] AddVersionRequest? request = null)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();

        if (tenantId == null || userId == null)
            return BadRequest(new { message = "Tenant ID o User ID non trovato" });

        try
        {
            var result = await _hrDocumentService.AddDocumentVersionAsync(
                id,
                tenantId.Value,
                userId.Value,
                request?.ChangeNotes);

            return CreatedAtAction(nameof(GetDocumentById), new { id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Errore durante l'aggiunta della versione", error = ex.Message });
        }
    }

    /// <summary>
    /// Aggiorna metadati documento
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateDocument(int id, [FromBody] HRDocumentUpdateDto dto)
    {
        var tenantId = GetTenantId();
        if (tenantId == null)
            return BadRequest(new { message = "Tenant ID non trovato" });

        try
        {
            var success = await _hrDocumentService.UpdateDocumentAsync(id, tenantId.Value, dto);

            if (!success)
                return NotFound(new { message = "Documento non trovato" });

            return Ok(new { success = true, message = "Documento aggiornato" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Errore durante l'aggiornamento", error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina documento (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteDocument(int id)
    {
        var tenantId = GetTenantId();
        if (tenantId == null)
            return BadRequest(new { message = "Tenant ID non trovato" });

        try
        {
            var success = await _hrDocumentService.DeleteDocumentAsync(id, tenantId.Value);

            if (!success)
                return NotFound(new { message = "Documento non trovato" });

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Errore durante l'eliminazione", error = ex.Message });
        }
    }

    /// <summary>
    /// Genera URL download per documento (versione corrente o specifica)
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<ActionResult<HRDocumentDownloadDto>> GenerateDownloadUrl(
        int id,
        [FromQuery] int? versionNumber = null)
    {
        var tenantId = GetTenantId();
        if (tenantId == null)
            return BadRequest(new { message = "Tenant ID non trovato" });

        try
        {
            var result = await _hrDocumentService.GenerateDownloadUrlAsync(
                id,
                tenantId.Value,
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

    private int? GetTenantId()
    {
        var claim = User.FindFirst("MerchantId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}

/// <summary>
/// Request per aggiungere versione
/// </summary>
public class AddVersionRequest
{
    public string? ChangeNotes { get; set; }
}
