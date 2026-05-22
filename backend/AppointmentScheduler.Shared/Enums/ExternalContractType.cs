namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Tipo di rapporto di una risorsa esterna. Significativo solo quando
/// <see cref="EmployeeKind.External"/>. I dettagli non classificabili
/// (valore <see cref="Other"/>) vanno in Employee.ExternalNotes.
/// </summary>
public enum ExternalContractType
{
    /// <summary>Lavoratore a chiamata / intermittente.</summary>
    OnCall = 0,

    /// <summary>Somministrato da agenzia interinale o cooperativa.</summary>
    Agency = 1,

    /// <summary>Libero professionista / partita IVA.</summary>
    Freelance = 2,

    /// <summary>Qualsiasi altro rapporto non classificabile.</summary>
    Other = 3
}
