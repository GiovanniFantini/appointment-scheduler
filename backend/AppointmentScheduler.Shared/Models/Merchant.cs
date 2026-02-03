namespace AppointmentScheduler.Shared.Models;

public class Merchant
{
    public int Id { get; set; }
    public int UserId { get; set; } // FK a User
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? VatNumber { get; set; } // Partita IVA
    public bool IsApproved { get; set; } = false; // Approvato dall'admin
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<BusinessHours> BusinessHours { get; set; } = new List<BusinessHours>();
    public ICollection<ClosurePeriod> ClosurePeriods { get; set; } = new List<ClosurePeriod>();
    public ICollection<ShiftTemplate> ShiftTemplates { get; set; } = new List<ShiftTemplate>();
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
    public ICollection<EmployeeWorkingHoursLimit> EmployeeWorkingHoursLimits { get; set; } = new List<EmployeeWorkingHoursLimit>();
    public ICollection<OvertimeRecord> OvertimeRecords { get; set; } = new List<OvertimeRecord>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<BoardMessage> BoardMessages { get; set; } = new List<BoardMessage>();
}
