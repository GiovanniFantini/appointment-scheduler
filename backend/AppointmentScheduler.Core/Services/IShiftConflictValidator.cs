using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Rileva conflitti (non bloccanti) al momento di assegnare uno o più dipendenti
/// a un turno: sovrapposizione con ferie approvate o con altri turni.
/// </summary>
public interface IShiftConflictValidator
{
    /// <summary>
    /// Rileva i conflitti per una specifica assegnazione.
    /// </summary>
    /// <param name="merchantId">Merchant context.</param>
    /// <param name="employeeIds">Dipendenti da verificare.</param>
    /// <param name="date">Data del turno.</param>
    /// <param name="start">Ora di inizio del turno (null se all-day).</param>
    /// <param name="end">Ora di fine del turno (null se all-day).</param>
    /// <param name="excludeEventId">Se valorizzato, esclude questo eventId dal controllo sovrapposizione (utile in update).</param>
    Task<List<ShiftConflictDto>> DetectAssignmentConflictsAsync(
        int merchantId,
        IReadOnlyList<int> employeeIds,
        DateOnly date,
        TimeOnly? start,
        TimeOnly? end,
        int? excludeEventId = null);
}
