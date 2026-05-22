using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _defaultContainerName;
    private readonly int _sasExpirationMinutes;

    public AzureBlobStorageService(AzureBlobStorageOptions options)
    {
        var connectionString = options.ConnectionString
            ?? throw new InvalidOperationException("Azure Blob Storage connection string not configured");

        _blobServiceClient = new BlobServiceClient(connectionString);
        _defaultContainerName = options.ContainerName;
        _sasExpirationMinutes = options.SasTokenExpirationMinutes;
    }

    public async Task<string> GenerateUploadSasUrlAsync(string blobPath, int expirationMinutes = 5)
    {
        var (containerName, normalizedBlobPath) = ResolveContainerAndBlobPath(blobPath);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(normalizedBlobPath);

        // Assicurati che il container esista
        await containerClient.CreateIfNotExistsAsync();

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = normalizedBlobPath,
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
        var (containerName, normalizedBlobPath) = ResolveContainerAndBlobPath(blobPath);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(normalizedBlobPath);

        // Verifica che il blob esista
        if (!await blobClient.ExistsAsync())
        {
            throw new FileNotFoundException($"Blob not found: {blobPath}");
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = normalizedBlobPath,
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
        var (containerName, normalizedBlobPath) = ResolveContainerAndBlobPath(blobPath);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(normalizedBlobPath);

        return await blobClient.ExistsAsync();
    }

    public async Task<BlobPropertiesDto> GetBlobPropertiesAsync(string blobPath)
    {
        var (containerName, normalizedBlobPath) = ResolveContainerAndBlobPath(blobPath);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(normalizedBlobPath);

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
        var (containerName, normalizedBlobPath) = ResolveContainerAndBlobPath(blobPath);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(normalizedBlobPath);

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
            $"merchant-{tenantId}"
        };

        if (year.HasValue)
        {
            parts.Add(year.Value.ToString());
            parts.Add(month.HasValue ? month.Value.ToString("D2") : "senza-mese-di-riferimento");
        }
        else
        {
            parts.Add("senza-anno-di-riferimento");
        }

        parts.Add($"employee-{employeeId}");
        parts.Add(documentType.ToString().ToLowerInvariant());

        // Clean extension (remove dot if present)
        var cleanExtension = fileExtension.TrimStart('.');
        var fileName = $"doc-{documentId}_v{versionNumber}.{cleanExtension}";
        parts.Add(fileName);

        return string.Join("/", parts);
    }

    private (string ContainerName, string BlobPath) ResolveContainerAndBlobPath(string blobPath)
    {
        if (string.IsNullOrWhiteSpace(blobPath))
            throw new ArgumentException("blobPath cannot be empty", nameof(blobPath));

        var separatorIndex = blobPath.IndexOf('/');
        if (separatorIndex <= 0)
            return (_defaultContainerName, blobPath);

        var firstSegment = blobPath[..separatorIndex];
        if (!firstSegment.StartsWith("merchant-", StringComparison.OrdinalIgnoreCase))
            return (_defaultContainerName, blobPath);

        var normalizedBlobPath = blobPath[(separatorIndex + 1)..];
        if (string.IsNullOrWhiteSpace(normalizedBlobPath))
            throw new ArgumentException("blobPath is missing blob name", nameof(blobPath));

        return (firstSegment.ToLowerInvariant(), normalizedBlobPath);
    }
}
