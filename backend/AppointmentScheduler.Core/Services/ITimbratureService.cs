using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Interfaccia per il sistema di timbratura intelligente
/// Implementa i principi: Fiducia > controllo, Semplicità, Prevenzione > punizione
/// </summary>
public interface ITimbratureService
{
    /// <summary>
    /// Check-in su un turno con validazione intelligente
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <param name="request">Dati del check-in</param>
    /// <returns>Response con messaggi contestuali ed empatici</returns>
    Task<TimbratureResponse> CheckInAsync(int employeeId, CheckInRequest request);

    /// <summary>
    /// Check-out da un turno con rilevamento straordinari
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <param name="request">Dati del check-out</param>
    /// <returns>Response con messaggi contestuali ed empatici</returns>
    Task<TimbratureResponse> CheckOutAsync(int employeeId, CheckOutRequest request);

    /// <summary>
    /// Inizia una pausa
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <param name="request">Dati della pausa</param>
    /// <returns>Pausa creata</returns>
    Task<ShiftBreakDto> StartBreakAsync(int employeeId, StartBreakRequest request);

    /// <summary>
    /// Termina una pausa in corso
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <param name="request">Dati fine pausa</param>
    /// <returns>Pausa aggiornata</returns>
    Task<ShiftBreakDto> EndBreakAsync(int employeeId, EndBreakRequest request);

    /// <summary>
    /// Recupera lo stato corrente del dipendente (in turno, in pausa, etc.)
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <returns>Stato corrente con suggerimenti</returns>
    Task<CurrentStatusResponse> GetCurrentStatusAsync(int employeeId);

    /// <summary>
    /// Recupera il turno odierno del dipendente (se esiste)
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <returns>Turno di oggi o null</returns>
    Task<ShiftDto?> GetTodayShiftAsync(int employeeId);

    /// <summary>
    /// Risolve un'anomalia con opzioni rapide empatiche
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <param name="request">Motivo e note</param>
    /// <returns>Anomalia risolta</returns>
    Task<ShiftAnomalyDto> ResolveAnomalyAsync(int employeeId, ResolveAnomalyRequest request);

    /// <summary>
    /// Classifica straordinario (Paid, BankedHours, Recovery, Voluntary)
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <param name="request">Tipo e note</param>
    /// <returns>Straordinario classificato</returns>
    Task<OvertimeRecordDto> ClassifyOvertimeAsync(int employeeId, ClassifyOvertimeRequest request);

    /// <summary>
    /// Corregge un turno (auto-approvato se entro 24h)
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <param name="request">Campo e nuovo valore</param>
    /// <returns>Correzione applicata</returns>
    Task<ShiftCorrectionDto> CorrectShiftAsync(int employeeId, CorrectShiftRequest request);

    /// <summary>
    /// Auto-valida turni secondo regole 95% (±15min, pattern coerente)
    /// </summary>
    /// <param name="merchantId">ID del merchant</param>
    /// <param name="date">Data da validare (null = oggi)</param>
    /// <returns>Numero di turni auto-validati</returns>
    Task<int> AutoValidateShiftsAsync(int merchantId, DateTime? date = null);

    /// <summary>
    /// Approvazione batch merchant (1-click)
    /// </summary>
    /// <param name="merchantId">ID del merchant</param>
    /// <param name="shiftIds">IDs dei turni da approvare</param>
    /// <returns>Numero di turni approvati</returns>
    Task<int> BatchApproveShiftsAsync(int merchantId, List<int> shiftIds);

    /// <summary>
    /// Calcola statistiche benessere (alert se >50h/settimana)
    /// </summary>
    /// <param name="employeeId">ID del dipendente</param>
    /// <returns>Statistiche con alert benessere</returns>
    Task<WellbeingStatsResponse> GetWellbeingStatsAsync(int employeeId);
}

/// <summary>
/// Response per lo stato corrente del dipendente
/// </summary>
public class CurrentStatusResponse
{
    public bool IsCheckedIn { get; set; }
    public bool IsOnBreak { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? BreakStartTime { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public string? SuggestedAction { get; set; }
    public ShiftDto? CurrentShift { get; set; }
    public ShiftBreakDto? CurrentBreak { get; set; }
    public decimal TotalWorkedHoursToday { get; set; }
    public decimal TotalWorkedHoursThisWeek { get; set; }
}

/// <summary>
/// Response per statistiche benessere
/// </summary>
public class WellbeingStatsResponse
{
    public decimal HoursThisWeek { get; set; }
    public decimal HoursThisMonth { get; set; }
    public decimal OvertimeThisWeek { get; set; }
    public decimal OvertimeThisMonth { get; set; }
    public bool HasWellbeingAlert { get; set; }
    public string? WellbeingMessage { get; set; }
    public int ConsecutiveLongDays { get; set; }
    public DateTime? LastDayOff { get; set; }
}
