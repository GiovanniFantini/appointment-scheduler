namespace AppointmentScheduler.Shared.DTOs;

public class SkillDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3b82f6";
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSkillRequest
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3b82f6";
    public bool IsActive { get; set; } = true;
}

public class UpdateSkillRequest
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3b82f6";
    public bool IsActive { get; set; } = true;
}

public class EmployeeSkillDto
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string SkillColor { get; set; } = "#3b82f6";
}

public class EventRequiredSkillDto
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string SkillColor { get; set; } = "#3b82f6";
    public int Quantity { get; set; }

    /// <summary>Quanti partecipanti del turno hanno questa mansione (per indicatore copertura).</summary>
    public int CoveredQuantity { get; set; }
}

public class EventRequiredSkillInput
{
    public int SkillId { get; set; }
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// Mansione con cui un partecipante prende parte al turno. Opzionale: solo per merchant
/// che usano le mansioni. La chiave Employee → SkillId è inviata nel payload create/update.
/// </summary>
public class ParticipantSkillAssignment
{
    public int EmployeeId { get; set; }
    public int? SkillId { get; set; }
}

/// <summary>
/// Dipendente suggerito per coprire un fabbisogno di mansione su un turno.
/// </summary>
public class SuggestedEmployeeDto
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    /// <summary>Motivo dell'indisponibilità ("In ferie", "Già su altro turno"). Null se disponibile.</summary>
    public string? UnavailableReason { get; set; }
}
