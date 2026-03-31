namespace AppointmentScheduler.Shared.Models;

public class Merchant
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? VatNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
    public string? BusinessEmail { get; set; }
    public bool IsApproved { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<EmployeeMembership> EmployeeMemberships { get; set; } = new List<EmployeeMembership>();
    public ICollection<MerchantRole> Roles { get; set; } = new List<MerchantRole>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<HRDocument> HRDocuments { get; set; } = new List<HRDocument>();
}
