using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly int _sasExpirationMinutes;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AzureBlobStorage")
            ?? throw new InvalidOperationException("Azure Blob Storage connection string not configured");

        _blobServiceClient = new BlobServiceClient(connectionString);
        _containerName = configuration["AzureBlobStorage:ContainerName"] ?? "erp-documents";
        _sasExpirationMinutes = int.Parse(configuration["AzureBlobStorage:SasTokenExpirationMinutes"] ?? "5");
    }

    public async Task<string> GenerateUploadSasUrlAsync(string blobPath, int expirationMinutes = 5)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        // Assicurati che il container esista
        await containerClient.CreateIfNotExistsAsync();

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobPath,
            Resource = "b", // blob
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // clock skew tolerance
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes > 0 ? expirationMinutes : _sasExpirationMinutes)
        };

        // Permessi write e create per upload
        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        var sasToken = blobClient.GenerateSasUri(sasBuilder);
        return sasToken.ToString();
    }

    public async Task<string> GenerateDownloadSasUrlAsync(string blobPath, int expirationMinutes = 5)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        // Verifica che il blob esista
        if (!await blobClient.ExistsAsync())
        {
            throw new FileNotFoundException($"Blob not found: {blobPath}");
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobPath,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // clock skew tolerance
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes > 0 ? expirationMinutes : _sasExpirationMinutes)
        };

        // Permesso read-only per download
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasToken = blobClient.GenerateSasUri(sasBuilder);
        return sasToken.ToString();
    }

    public async Task<bool> BlobExistsAsync(string blobPath)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        return await blobClient.ExistsAsync();
    }

    public async Task<BlobPropertiesDto> GetBlobPropertiesAsync(string blobPath)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync())
        {
            throw new FileNotFoundException($"Blob not found: {blobPath}");
        }

        var properties = await blobClient.GetPropertiesAsync();

        return new BlobPropertiesDto
        {
            ContentLength = properties.Value.ContentLength,
            ContentType = properties.Value.ContentType,
            ETag = properties.Value.ETag.ToString(),
            LastModified = properties.Value.LastModified.UtcDateTime
        };
    }

    public async Task DeleteBlobAsync(string blobPath)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        await blobClient.DeleteIfExistsAsync();
    }

    public string BuildBlobPath(
        int tenantId,
        int employeeId,
        HRDocumentType documentType,
        int? year,
        int? month,
        int documentId,
        int versionNumber,
        string fileExtension)
    {
        var parts = new List<string>
        {
            $"tenant-{tenantId}",
            $"employee-{employeeId}",
            documentType.ToString().ToLowerInvariant()
        };

        if (year.HasValue)
            parts.Add(year.Value.ToString());

        if (month.HasValue)
            parts.Add(month.Value.ToString("D2")); // Zero-padded (01, 02, etc.)

        // Clean extension (remove dot if present)
        var cleanExtension = fileExtension.TrimStart('.');
        var fileName = $"doc-{documentId}_v{versionNumber}.{cleanExtension}";
        parts.Add(fileName);

        return string.Join("/", parts);
    }
}
