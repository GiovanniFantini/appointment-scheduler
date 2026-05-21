namespace AppointmentScheduler.Shared.Enums;

public enum EmployeeRequestType
{
    Ferie = 1,
    // Il valore 2 (CambioTurno) è stato rimosso: nessuna UI poteva creare quel
    // tipo di richiesta. I valori 3/4 restano espliciti per non spostarsi.
    Permessi = 3,
    Malattia = 4
}
