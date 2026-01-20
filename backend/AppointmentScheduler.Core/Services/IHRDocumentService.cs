using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

public interface IHRDocumentService
{
    /// <summary>
    /// Lista documenti per tenant (filtri opzionali)
    /// </summary>
    Task<List<HRDocumentDto>> GetDocumentsAsync(
        int tenantId,
        int? employeeId = null,
        HRDocumentType? documentType = null,
        int? year = null,
        int? month = null,
        HRDocumentStatus? status = null);

    /// <summary>
    /// Dettaglio documento con versioni
    /// </summary>
    Task<HRDocumentDetailDto?> GetDocumentByIdAsync(int documentId, int tenantId);

    /// <summary>
    /// Crea documento e prepara upload
    /// </summary>
    Task<HRDocumentUploadResponseDto> CreateDocumentAsync(
        int tenantId,
        int userId,
        HRDocumentCreateDto dto);

    /// <summary>
    /// Finalizza upload documento
    /// </summary>
    Task<bool> FinalizeDocumentUploadAsync(
        int documentId,
        int tenantId,
        HRDocumentFinalizeDto dto);

    /// <summary>
    /// Aggiunge nuova versione
    /// </summary>
    Task<HRDocumentVersionUploadResponseDto> AddDocumentVersionAsync(
        int documentId,
        int tenantId,
        int userId,
        string? changeNotes = null);

    /// <summary>
    /// Aggiorna metadati documento
    /// </summary>
    Task<bool> UpdateDocumentAsync(
        int documentId,
        int tenantId,
        HRDocumentUpdateDto dto);

    /// <summary>
    /// Soft delete documento
    /// </summary>
    Task<bool> DeleteDocumentAsync(int documentId, int tenantId);

    /// <summary>
    /// Genera URL download per dipendente
    /// </summary>
    Task<HRDocumentDownloadDto> GenerateEmployeeDownloadUrlAsync(
        int documentId,
        int employeeId,
        int? versionNumber = null);

    /// <summary>
    /// Genera URL download per HR/Merchant
    /// </summary>
    Task<HRDocumentDownloadDto> GenerateDownloadUrlAsync(
        int documentId,
        int tenantId,
        int? versionNumber = null);
}
