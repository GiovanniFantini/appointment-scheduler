namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Distingue un dipendente interno dell'azienda da una risorsa esterna (lavoratore
/// a chiamata, somministrato, libero professionista). L'esterno non accede all'app
/// e non timbra: serve al responsabile per completare la turnazione e allo storico.
/// </summary>
public enum EmployeeKind
{
    Internal = 0,
    External = 1
}
