namespace AppointmentScheduler.Shared.Models;

public class Employee
{
    public int Id { get; set; }

    /// <summary>
    /// FK a User — nullable: l'employee può essere pre-caricato dal merchant prima che si registri.
    /// L'associazione avviene tramite email (weak association).
    /// </summary>
    public int? UserId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public ICollection<EmployeeMembership> Memberships { get; set; } = new List<EmployeeMembership>();
    public ICollection<EventParticipant> EventParticipations { get; set; } = new List<EventParticipant>();
    public ICollection<HRDocument> HRDocuments { get; set; } = new List<HRDocument>();
    public ICollection<EmployeeRequest> Requests { get; set; } = new List<EmployeeRequest>();
}
