namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Reparto / area operativa interna a una filiale (es. "Produzione",
/// "Amministrazione", "Magazzino"). È un tag organizzativo: raggruppa turni e
/// persone, abilita filtri e swimlane nel calendario.
///
/// È sempre opzionale: un turno o un dipendente senza reparto è uno stato
/// valido (Jolly — persona che salta tra reparti diversi).
/// </summary>
public class Department
{
    public int Id { get; set; }
    public int BranchId { get; set; }

    /// <summary>FK al merchant, denormalizzato per query/validazioni cross-tenant.</summary>
    public int MerchantId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Colore hex (es. "#3b82f6") per badge/chip in UI.</summary>
    public string Color { get; set; } = "#3b82f6";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public MerchantBranch Branch { get; set; } = null!;
}
