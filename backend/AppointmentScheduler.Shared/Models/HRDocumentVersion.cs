using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Rappresenta una versione specifica di un documento HR nel Blob Storage
/// </summary>
public class HRDocumentVersion
{
    public int Id { get; set; }

    // Documento parent
    public int HRDocumentId { get; set; }

    // Versione
    public int VersionNumber { get; set; }

    // File nel Blob Storage
    public string BlobPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? FileHash { get; set; }  // SHA256 per integrity check

    // Metadati versione
    public string? ChangeNotes { get; set; }

    // Stato upload
    public UploadStatus UploadStatus { get; set; } = UploadStatus.Uploading;

    // Audit
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int UploadedByUserId { get; set; }

    // Navigation properties
    public HRDocument HRDocument { get; set; } = null!;
    public User UploadedBy { get; set; } = null!;
}
