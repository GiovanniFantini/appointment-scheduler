using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Rappresenta un documento HR/Payroll con metadati e versioning
/// </summary>
public class HRDocument
{
    public int Id { get; set; }

    // Multi-tenant
    public int TenantId { get; set; }  // FK a Merchant.Id

    // Dipendente
    public int EmployeeId { get; set; }

    // Classificazione
    public HRDocumentType DocumentType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Periodo di riferimento
    public int? Year { get; set; }
    public int? Month { get; set; }  // 1-12

    // Versioning
    public int CurrentVersion { get; set; } = 1;

    // Stato
    public HRDocumentStatus Status { get; set; } = HRDocumentStatus.Draft;
    public bool IsDeleted { get; set; } = false;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }

    // Navigation properties
    public Merchant Tenant { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public User? UpdatedBy { get; set; }
    public ICollection<HRDocumentVersion> Versions { get; set; } = new List<HRDocumentVersion>();
}
