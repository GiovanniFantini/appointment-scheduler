namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Fabbisogno di mansione per un Event (turno). Esempio: turno mattina richiede 1 Cassiere + 1 Repartista.
/// </summary>
public class EventRequiredSkill
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int SkillId { get; set; }

    public int Quantity { get; set; } = 1;

    // Navigation properties
    public Event Event { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
