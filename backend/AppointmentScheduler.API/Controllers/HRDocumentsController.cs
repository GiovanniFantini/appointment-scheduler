using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.API.Authorization;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller merchant-only di sola consultazione/configurazione per i documenti HR/Payroll.
/// Le operazioni di upload, versioning, finalize e delete vivono nell'API employee.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ApprovedMerchantOnly")]
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
        if (RequireLevel(FeatureAccessLevel.ReadOnly) is { } forbidden)
            return forbidden;

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
        if (RequireLevel(FeatureAccessLevel.ReadOnly) is { } forbidden)
            return forbidden;

        var tenantId = GetTenantId();
        if (tenantId == null)
            return BadRequest(new { message = "Tenant ID non trovato" });

        var document = await _hrDocumentService.GetDocumentByIdAsync(id, tenantId.Value);

        if (document == null)
            return NotFound(new { message = "Documento non trovato" });

        return Ok(document);
    }

    /// <summary>
    /// Genera URL download per documento (versione corrente o specifica)
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<ActionResult<HRDocumentDownloadDto>> GenerateDownloadUrl(
        int id,
        [FromQuery] int? versionNumber = null)
    {
        if (RequireLevel(FeatureAccessLevel.ReadOnly) is { } forbidden)
            return forbidden;

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
        catch (UnauthorizedAccessException)
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
    /// Log accessi del documento: chi ha scaricato e chi ha confermato la presa
    /// visione, per versione.
    /// </summary>
    [HttpGet("{id}/access-log")]
    public async Task<ActionResult<IEnumerable<HRDocumentAccessRowDto>>> GetAccessLog(int id)
    {
        if (RequireLevel(FeatureAccessLevel.ReadOnly) is { } forbidden)
            return forbidden;

        var tenantId = GetTenantId();
        if (tenantId == null)
            return BadRequest(new { message = "Tenant ID non trovato" });

        var rows = await _hrDocumentService.GetDocumentAccessLogAsync(id, tenantId.Value);
        return Ok(rows);
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

    private ActionResult? RequireLevel(FeatureAccessLevel minimumLevel)
    {
        if (!User.HasFeature(MerchantFeature.Documenti))
            return Forbid();
        if (!User.RequireFeatureLevel(MerchantFeature.Documenti, minimumLevel))
            return Forbid();
        return null;
    }
}
