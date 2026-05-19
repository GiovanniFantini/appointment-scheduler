namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Mansione/qualifica configurabile per merchant (es. "Cassiere", "Repartista").
/// Distinta da MerchantRole: questo è cosa il dipendente sa fare, MerchantRole è cosa
/// può vedere/fare nell'app (RBAC).
/// </summary>
public class Skill
{
    public int Id { get; set; }
    public int MerchantId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Colore hex (es. "#3b82f6") per badge/chip in UI.</summary>
    public string Color { get; set; } = "#3b82f6";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Merchant Merchant { get; set; } = null!;
    public ICollection<EmployeeSkill> EmployeeSkills { get; set; } = new List<EmployeeSkill>();
    public ICollection<EventRequiredSkill> EventRequiredSkills { get; set; } = new List<EventRequiredSkill>();
    public ICollection<EventParticipant> Participations { get; set; } = new List<EventParticipant>();
}
