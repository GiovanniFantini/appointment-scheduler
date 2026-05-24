namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Livello di accesso operativo di un ruolo a una feature.
/// Usato dalle feature a livelli (es. Calendario, Documenti, Timbratura,
/// Magazzino): distingue chi consulta,
/// chi opera e chi gestisce. Per le altre feature il valore resta null e non
/// viene letto. Una feature a livelli abilitata senza livello vale ReadOnly.
/// </summary>
public enum FeatureAccessLevel
{
    /// <summary>Sola consultazione.</summary>
    ReadOnly = 1,

    /// <summary>Operatività quotidiana (rettifiche stock, ricevimento merci).</summary>
    Operator = 2,

    /// <summary>Operatività + gestione anagrafiche (articoli, fornitori, ordini di acquisto).</summary>
    Manager = 3
}
