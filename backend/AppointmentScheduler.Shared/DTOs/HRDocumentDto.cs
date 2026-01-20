using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO per lista documenti HR
/// </summary>
public class HRDocumentDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public HRDocumentType DocumentType { get; set; }
    public string DocumentTypeText { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int CurrentVersion { get; set; }
    public HRDocumentStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO per dettaglio documento con versioni
/// </summary>
public class HRDocumentDetailDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public HRDocumentType DocumentType { get; set; }
    public string DocumentTypeText { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int CurrentVersion { get; set; }
    public HRDocumentStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedByEmail { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedByEmail { get; set; }
    public List<HRDocumentVersionDto> Versions { get; set; } = new();
}

/// <summary>
/// DTO per versione documento
/// </summary>
public class HRDocumentVersionDto
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? ChangeNotes { get; set; }
    public UploadStatus UploadStatus { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedByEmail { get; set; } = string.Empty;
}

/// <summary>
/// DTO per creare nuovo documento
/// </summary>
public class HRDocumentCreateDto
{
    public int EmployeeId { get; set; }
    public HRDocumentType DocumentType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
}

/// <summary>
/// DTO per aggiornare metadati documento
/// </summary>
public class HRDocumentUpdateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public HRDocumentStatus? Status { get; set; }
}

/// <summary>
/// DTO risposta dopo creazione documento (con upload URL)
/// </summary>
public class HRDocumentUploadResponseDto
{
    public int DocumentId { get; set; }
    public string UploadUrl { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// DTO per finalizzare upload documento
/// </summary>
public class HRDocumentFinalizeDto
{
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? FileHash { get; set; }
}

/// <summary>
/// DTO risposta per aggiunta versione
/// </summary>
public class HRDocumentVersionUploadResponseDto
{
    public int VersionId { get; set; }
    public int VersionNumber { get; set; }
    public string UploadUrl { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// DTO per download documento
/// </summary>
public class HRDocumentDownloadDto
{
    public string DownloadUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// DTO per proprietà blob
/// </summary>
public class BlobPropertiesDto
{
    public long ContentLength { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? ETag { get; set; }
    public DateTime LastModified { get; set; }
}
