using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

public class AzureBlobStorageService : IFileStorageService
{
    private const string SegmentoSenzaAnno = "senza-anno-di-riferimento";
    private const string SegmentoSenzaMese = "senza-mese-di-riferimento";

    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _defaultContainerName;
    private readonly int _sasExpirationMinutes;

    public AzureBlobStorageService(AzureBlobStorageOptions options)
    {
        var connectionString = options.ConnectionString
            ?? throw new InvalidOperationException("Connection string di Azure Blob Storage non configurata");

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
            throw new FileNotFoundException($"Blob non trovato: {blobPath}");
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
            throw new FileNotFoundException($"Blob non trovato: {blobPath}");
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
        if (tenantId <= 0) throw new ArgumentOutOfRangeException(nameof(tenantId), "tenantId deve essere maggiore di zero");
        if (employeeId <= 0) throw new ArgumentOutOfRangeException(nameof(employeeId), "employeeId deve essere maggiore di zero");
        if (documentId <= 0) throw new ArgumentOutOfRangeException(nameof(documentId), "documentId deve essere maggiore di zero");
        if (versionNumber <= 0) throw new ArgumentOutOfRangeException(nameof(versionNumber), "versionNumber deve essere maggiore di zero");
        if (month.HasValue && !year.HasValue)
            throw new ArgumentException("month non puo' essere valorizzato quando year e' null", nameof(month));

        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "month deve essere compreso tra 1 e 12");

        if (!Enum.IsDefined(typeof(HRDocumentType), documentType))
            throw new ArgumentOutOfRangeException(nameof(documentType), "Tipo documento non supportato");

        var cleanExtension = SanitizeExtension(fileExtension);
        var typeFolder = MapDocumentTypeFolder(documentType);

        var parts = new List<string>
        {
            $"merchant-{tenantId}",
            $"employee-{employeeId}"
        };

        if (year.HasValue)
        {
            parts.Add(year.Value.ToString());
            parts.Add(month.HasValue ? month.Value.ToString("D2") : SegmentoSenzaMese);
        }
        else
        {
            parts.Add(SegmentoSenzaAnno);
        }

        parts.Add(typeFolder);
        var fileName = $"doc-{documentId}_v{versionNumber}.{cleanExtension}";
        parts.Add(fileName);

        return string.Join("/", parts);
    }

    private static string SanitizeExtension(string fileExtension)
    {
        if (string.IsNullOrWhiteSpace(fileExtension))
            throw new ArgumentException("fileExtension non puo' essere vuota", nameof(fileExtension));

        var cleanExtension = fileExtension.Trim().TrimStart('.').ToLowerInvariant();
        if (cleanExtension.Length == 0)
            throw new ArgumentException("fileExtension non puo' essere vuota", nameof(fileExtension));

        if (cleanExtension.Contains('/') || cleanExtension.Contains('\\') || cleanExtension.Contains(".."))
            throw new ArgumentException("fileExtension contiene caratteri non validi", nameof(fileExtension));

        return cleanExtension;
    }

    private static string MapDocumentTypeFolder(HRDocumentType documentType) => documentType switch
    {
        HRDocumentType.Payslip => "BustePaga",
        HRDocumentType.Contract => "Contratti",
        HRDocumentType.Bonus => "Bonus",
        HRDocumentType.Communication => "Comunicazioni",
        HRDocumentType.LevelChange => "CambiLivello",
        HRDocumentType.Certification => "Certificazioni",
        HRDocumentType.DisciplinaryAction => "ProvvedimentiDisciplinari",
        HRDocumentType.Invoice => "Fatture",
        HRDocumentType.PayrollStatement => "Cedolini",
        HRDocumentType.Other => "Altro",
        _ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType, "Tipo documento non supportato")
    };

    private (string ContainerName, string BlobPath) ResolveContainerAndBlobPath(string blobPath)
    {
        if (string.IsNullOrWhiteSpace(blobPath))
            throw new ArgumentException("blobPath non puo' essere vuoto", nameof(blobPath));

        var separatorIndex = blobPath.IndexOf('/');
        if (separatorIndex <= 0)
            return (_defaultContainerName, blobPath);

        var firstSegment = blobPath[..separatorIndex];
        if (!firstSegment.StartsWith("merchant-", StringComparison.OrdinalIgnoreCase))
            return (_defaultContainerName, blobPath);

        var normalizedBlobPath = blobPath[(separatorIndex + 1)..];
        if (string.IsNullOrWhiteSpace(normalizedBlobPath))
            throw new ArgumentException("blobPath non contiene il nome del blob", nameof(blobPath));

        return (firstSegment.ToLowerInvariant(), normalizedBlobPath);
    }
}
