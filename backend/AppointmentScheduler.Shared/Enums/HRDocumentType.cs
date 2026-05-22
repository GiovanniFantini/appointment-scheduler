namespace AppointmentScheduler.Shared.Enums;

public enum HRDocumentType
{
    Payslip = 1,        // Busta paga
    Contract = 2,       // Contratto
    Bonus = 3,          // Bonus
    Communication = 4,  // Comunicazione
    LevelChange = 5,    // Cambio livello
    Certification = 6,  // Attestato / certificazione
    DisciplinaryAction = 7, // Provvedimento disciplinare
    Invoice = 8,        // Fattura
    PayrollStatement = 9, // Cedolino
    Other = 99          // Altro
}
