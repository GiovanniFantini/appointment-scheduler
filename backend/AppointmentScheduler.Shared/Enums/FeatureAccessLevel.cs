namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Livello di accesso operativo di un ruolo a una feature.
/// Attualmente usato solo dalla feature Magazzino: distingue chi consulta,
/// chi movimenta la merce e chi gestisce le anagrafiche.
/// Per le altre feature il valore resta null e non viene letto.
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
