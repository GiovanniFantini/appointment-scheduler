using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

public class EmployeeRequest
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public int MerchantId { get; set; }

    public EmployeeRequestType Type { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public string? Notes { get; set; }
    public string? ReviewNotes { get; set; }

    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public Merchant Merchant { get; set; } = null!;
    public User? ReviewedBy { get; set; }
}
