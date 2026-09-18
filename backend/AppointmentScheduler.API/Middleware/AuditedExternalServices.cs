using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Middleware;

public sealed class AuditedEmailService(AzureEmailService inner, ActivityRecorder recorder) : IEmailService
{
    public EmailServiceStatus GetStatus() => inner.GetStatus();
    public Task SendAsync(string toAddress, string toDisplayName, string subject, string htmlBody)
        => recorder.RunAsync("email.send", () => inner.SendAsync(toAddress, toDisplayName, subject, htmlBody));
}

public sealed class AuditedFileStorageService(AzureBlobStorageService inner, ActivityRecorder recorder) : IFileStorageService
{
    public Task<string> GenerateUploadSasUrlAsync(string blobPath, int expirationMinutes = 5)
        => recorder.RunAsync("document.upload-url", () => inner.GenerateUploadSasUrlAsync(blobPath, expirationMinutes));
    public Task<string> GenerateDownloadSasUrlAsync(string blobPath, int expirationMinutes = 5)
        => recorder.RunAsync("document.download-url", () => inner.GenerateDownloadSasUrlAsync(blobPath, expirationMinutes));
    public Task<bool> BlobExistsAsync(string blobPath)
        => recorder.RunAsync("document.exists", () => inner.BlobExistsAsync(blobPath));
    public Task<BlobPropertiesDto> GetBlobPropertiesAsync(string blobPath)
        => recorder.RunAsync("document.properties", () => inner.GetBlobPropertiesAsync(blobPath));
    public Task DeleteBlobAsync(string blobPath)
        => recorder.RunAsync("document.delete-blob", () => inner.DeleteBlobAsync(blobPath));
    public string BuildBlobPath(int tenantId, int employeeId, HRDocumentType documentType, int? year, int? month,
        int documentId, int versionNumber, string fileExtension)
        => inner.BuildBlobPath(tenantId, employeeId, documentType, year, month, documentId, versionNumber, fileExtension);
}
