namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Traccia un singolo accesso in download a una versione di documento HR.
/// È un audit trail: viene registrata una riga ogni volta che un dipendente
/// genera un URL di download. Non implica presa visione formale — per quella
/// vedi <see cref="HRDocumentAcknowledgement"/>.
/// </summary>
public class HRDocumentDownload
{
    public int Id { get; set; }

    // Versione scaricata
    public int HRDocumentVersionId { get; set; }

    // Dipendente che ha scaricato
    public int EmployeeId { get; set; }

    // Quando
    public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public HRDocumentVersion HRDocumentVersion { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
