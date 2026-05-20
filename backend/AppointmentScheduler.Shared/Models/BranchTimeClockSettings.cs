namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Configurazione della timbratura per una singola filiale (relazione 1:1 con
/// <see cref="MerchantBranch"/>). Filiali diverse possono avere geofence e
/// politiche di tolleranza differenti. La riga viene creata on-demand con i
/// valori di default quando il merchant apre la configurazione.
/// </summary>
public class BranchTimeClockSettings
{
    public int Id { get; set; }

    /// <summary>Filiale a cui la configurazione appartiene (FK 1:1).</summary>
    public int BranchId { get; set; }

    /// <summary>Denormalizzato per scoping multi-tenant.</summary>
    public int MerchantId { get; set; }

    /// <summary>Se false la timbratura è disattivata per la filiale.</summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Se true i dipendenti della filiale devono timbrare; se false la
    /// timbratura è disponibile ma facoltativa.
    /// </summary>
    public bool ClockingRequired { get; set; } = false;

    // ── Tolleranze (in minuti) ────────────────────────────────────────────

    /// <summary>Ritardo tollerato in entrata prima di generare un'anomalia.</summary>
    public int GraceInMinutes { get; set; } = 5;

    /// <summary>Anticipo/ritardo tollerato in uscita prima di un'anomalia.</summary>
    public int GraceOutMinutes { get; set; } = 5;

    /// <summary>Quanto in anticipo è ammesso timbrare l'entrata.</summary>
    public int EarlyClockInToleranceMinutes { get; set; } = 15;

    /// <summary>Quanto oltre la fine turno è ammesso timbrare l'uscita.</summary>
    public int LateClockOutToleranceMinutes { get; set; } = 15;

    // ── Geofencing (Fase 2) ───────────────────────────────────────────────

    /// <summary>Se true la posizione viene confrontata col raggio della filiale.</summary>
    public bool GeofencingEnabled { get; set; } = false;

    /// <summary>Raggio del geofence attorno alla filiale, in metri.</summary>
    public int GeofenceRadiusMeters { get; set; } = 150;

    // ── Pause ─────────────────────────────────────────────────────────────

    /// <summary>Se true il dipendente può registrare le pause.</summary>
    public bool BreakTrackingEnabled { get; set; } = true;

    /// <summary>Durata massima di una pausa prima di generare un'anomalia.</summary>
    public int MaxBreakMinutes { get; set; } = 60;

    // ── Varie ─────────────────────────────────────────────────────────────

    /// <summary>Arrotondamento delle ore lavorate, in minuti (0 = nessuno).</summary>
    public int RoundingMinutes { get; set; } = 0;

    /// <summary>Predisposto per Fase 5: richiede una foto alla timbratura.</summary>
    public bool RequirePhoto { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public MerchantBranch Branch { get; set; } = null!;
    public Merchant Merchant { get; set; } = null!;
}
