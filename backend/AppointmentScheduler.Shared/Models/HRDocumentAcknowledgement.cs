namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Conferma esplicita di presa visione di una versione di documento HR da parte
/// di un dipendente. È il dato "forte" (legalmente rilevante): a differenza del
/// download (vedi <see cref="HRDocumentDownload"/>) richiede un'azione esplicita.
/// Una sola conferma per coppia (versione, dipendente).
/// </summary>
public class HRDocumentAcknowledgement
{
    public int Id { get; set; }

    // Versione confermata
    public int HRDocumentVersionId { get; set; }

    // Dipendente che ha confermato
    public int EmployeeId { get; set; }

    // Quando
    public DateTime AcknowledgedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public HRDocumentVersion HRDocumentVersion { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
