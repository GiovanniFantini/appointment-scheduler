namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Join M:N tra Employee e Skill. Un dipendente può possedere più mansioni.
/// </summary>
public class EmployeeSkill
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int SkillId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
