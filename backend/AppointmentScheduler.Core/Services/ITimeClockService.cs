using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Gestione della timbratura (clock-in/out, pause) e della relativa
/// configurazione per filiale.
/// </summary>
public interface ITimeClockService
{
    // ── Employee ───────────────────────────────────────────────────────────

    /// <summary>Stato corrente di timbratura del dipendente nel merchant.</summary>
    Task<CurrentClockStatusDto> GetCurrentStatusAsync(int employeeId, int merchantId);

    /// <summary>Registra l'entrata sul turno corrente/indicato.</summary>
    Task<ClockActionResultDto> ClockInAsync(int employeeId, int merchantId, ClockActionRequest request);

    /// <summary>Registra l'uscita sul turno corrente/indicato.</summary>
    Task<ClockActionResultDto> ClockOutAsync(int employeeId, int merchantId, ClockActionRequest request);

    /// <summary>Avvia una pausa.</summary>
    Task<ClockActionResultDto> StartBreakAsync(int employeeId, int merchantId, ClockActionRequest request);

    /// <summary>Termina la pausa in corso.</summary>
    Task<ClockActionResultDto> EndBreakAsync(int employeeId, int merchantId, ClockActionRequest request);

    /// <summary>Timbrature del dipendente in un intervallo di date.</summary>
    Task<List<TimeEntryDto>> GetMyEntriesAsync(int employeeId, int merchantId, DateOnly from, DateOnly to);

    /// <summary>Anomalie del dipendente, opzionalmente filtrate per stato.</summary>
    Task<List<TimeClockAnomalyDto>> GetMyAnomaliesAsync(int employeeId, int merchantId, TimeClockAnomalyStatus? status);

    /// <summary>Il dipendente giustifica una propria anomalia.</summary>
    Task<TimeClockAnomalyDto> JustifyAnomalyAsync(int anomalyId, int employeeId, int merchantId, JustifyAnomalyRequest request);

    /// <summary>Statistiche di benessere del dipendente (ore, straordinari, alert).</summary>
    Task<WellbeingStatsDto> GetWellbeingStatsAsync(int employeeId, int merchantId);

    // ── Merchant ───────────────────────────────────────────────────────────

    /// <summary>
    /// Configurazione timbratura di una filiale. Se non esiste viene restituita
    /// una configurazione con i valori di default (non persistita).
    /// </summary>
    Task<BranchTimeClockSettingsDto> GetSettingsAsync(int branchId, int merchantId);

    /// <summary>Aggiorna (o crea) la configurazione timbratura di una filiale.</summary>
    Task<BranchTimeClockSettingsDto> UpdateSettingsAsync(int branchId, int merchantId, UpdateTimeClockSettingsRequest request);

    /// <summary>Timbrature del merchant filtrate per filiale/intervallo/dipendente.</summary>
    Task<List<TimeEntryDto>> GetEntriesAsync(int merchantId, int? branchId, DateOnly from, DateOnly to, int? employeeId);

    /// <summary>Inserisce manualmente una timbratura (correzione del merchant).</summary>
    Task<TimeEntryDto> CreateManualEntryAsync(int merchantId, int userId, CreateManualEntryRequest request);

    /// <summary>Anomalie del merchant filtrate per filiale e stato.</summary>
    Task<List<TimeClockAnomalyDto>> GetAnomaliesAsync(int merchantId, int? branchId, TimeClockAnomalyStatus? status);

    /// <summary>Il merchant approva un'anomalia giustificata.</summary>
    Task<TimeClockAnomalyDto> ApproveAnomalyAsync(int anomalyId, int merchantId, int userId, ReviewAnomalyRequest request);

    /// <summary>Il merchant respinge un'anomalia giustificata.</summary>
    Task<TimeClockAnomalyDto> RejectAnomalyAsync(int anomalyId, int merchantId, int userId, ReviewAnomalyRequest request);

    /// <summary>
    /// Rileva le mancate timbrature sui turni passati (no clock-in / no
    /// clock-out). Restituisce il numero di anomalie create.
    /// </summary>
    Task<int> RunMissingPunchDetectionAsync(int merchantId, int? branchId);

    /// <summary>Report ore lavorate per dipendente/giornata.</summary>
    Task<List<TimeClockReportRowDto>> GetReportAsync(int merchantId, int? branchId, DateOnly from, DateOnly to);
}
