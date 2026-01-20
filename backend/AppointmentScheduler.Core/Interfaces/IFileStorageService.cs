using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Interfaces;

/// <summary>
/// Servizio per gestire l'archiviazione file in Azure Blob Storage
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Genera SAS URL per upload file
    /// </summary>
    Task<string> GenerateUploadSasUrlAsync(string blobPath, int expirationMinutes = 5);

    /// <summary>
    /// Genera SAS URL per download file
    /// </summary>
    Task<string> GenerateDownloadSasUrlAsync(string blobPath, int expirationMinutes = 5);

    /// <summary>
    /// Verifica esistenza file
    /// </summary>
    Task<bool> BlobExistsAsync(string blobPath);

    /// <summary>
    /// Ottiene proprietà file (size, contentType, eTag)
    /// </summary>
    Task<BlobPropertiesDto> GetBlobPropertiesAsync(string blobPath);

    /// <summary>
    /// Elimina file (hard delete)
    /// </summary>
    Task DeleteBlobAsync(string blobPath);

    /// <summary>
    /// Costruisce blob path secondo convenzioni
    /// </summary>
    string BuildBlobPath(
        int tenantId,
        int employeeId,
        HRDocumentType documentType,
        int? year,
        int? month,
        int documentId,
        int versionNumber,
        string fileExtension);
}
