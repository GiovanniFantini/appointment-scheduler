using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Implementazione del sistema di timbratura intelligente
/// Principi: Fiducia, Semplicità, Prevenzione, Empatia
/// </summary>
public class TimbratureService : ITimbratureService
{
    private readonly ApplicationDbContext _context;
    private const int AUTO_APPROVE_TOLERANCE_MINUTES = 15;
    private const int SELF_CORRECTION_HOURS = 24;
    private const int WELLBEING_ALERT_HOURS = 50;

    public TimbratureService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TimbratureResponse> CheckInAsync(int employeeId, CheckInRequest request)
    {
        var shift = await _context.Shifts
            .Include(s => s.Employee)
            .Include(s => s.Merchant)
            .FirstOrDefaultAsync(s => s.Id == request.ShiftId && s.EmployeeId == employeeId);

        if (shift == null)
        {
            return new TimbratureResponse
            {
                Success = false,
                Message = "Turno non trovato o non assegnato a te."
            };
        }

        if (shift.IsCheckedIn)
        {
            return new TimbratureResponse
            {
                Success = false,
                Message = "Hai già effettuato il check-in per questo turno."
            };
        }

        var now = DateTime.UtcNow;
        var shiftDateTime = shift.Date.Add(shift.StartTime);
        var minutesDifference = (int)(now - shiftDateTime).TotalMinutes;

        // Check-in
        shift.IsCheckedIn = true;
        shift.CheckInTime = now;
        shift.CheckInLocation = request.Location;
        shift.UpdatedAt = now;

        // Rilevamento anomalie con messaggi empatici
        ShiftAnomaly? anomaly = null;
        string empatheticMessage = "";

        if (minutesDifference < -AUTO_APPROVE_TOLERANCE_MINUTES)
        {
            // Check-in troppo in anticipo
            empatheticMessage = "Noto che sei arrivato più presto del solito. Grazie per la puntualità! 😊";
            anomaly = CreateAnomaly(shift.Id, AnomalyType.EarlyCheckIn, 1, empatheticMessage);
        }
        else if (minutesDifference > AUTO_APPROVE_TOLERANCE_MINUTES)
        {
            // Check-in in ritardo
            var minutesLate = minutesDifference;
            empatheticMessage = $"Noto che oggi sei arrivato {minutesLate} minuti più tardi del solito. C'è stato qualche imprevisto?";
            anomaly = CreateAnomaly(shift.Id, AnomalyType.LateCheckIn, minutesLate > 30 ? 3 : 2, empatheticMessage);
        }

        if (anomaly != null)
        {
            _context.ShiftAnomalies.Add(anomaly);
        }

        await _context.SaveChangesAsync();

        // Calcola durata turno e suggerisci pausa
        var shiftHours = (shift.EndTime - shift.StartTime).TotalHours;
        var breakSuggestion = shiftHours >= 6 ? CalculateBreakSuggestion(shift) : null;

        return new TimbratureResponse
        {
            Success = true,
            Message = $"✓ Entrata {now.ToLocalTime():HH:mm}",
            Context = $"Giornata {shiftHours:F1}h" + (shift.BreakDurationMinutes > 0 ? $", pausa prevista {shift.BreakDurationMinutes}min" : ""),
            NextSteps = breakSuggestion != null ? $"Pausa suggerita {breakSuggestion}" : "Ricorda l'uscita stasera",
            EmpatheticMessage = empatheticMessage != "" ? empatheticMessage : null,
            HasAnomaly = anomaly != null,
            AnomalyType = anomaly?.Type,
            AnomalyId = anomaly?.Id,
            QuickResolutionOptions = anomaly != null ? new List<string> { "Traffico", "Permesso", "Recupero", "Altro" } : null,
            SuggestedBreakTime = breakSuggestion,
            TotalShiftHours = (decimal)shiftHours
        };
    }

    public async Task<TimbratureResponse> CheckOutAsync(int employeeId, CheckOutRequest request)
    {
        var shift = await _context.Shifts
            .Include(s => s.Employee)
            .Include(s => s.Merchant)
            .Include(s => s.Breaks)
            .FirstOrDefaultAsync(s => s.Id == request.ShiftId && s.EmployeeId == employeeId);

        if (shift == null)
        {
            return new TimbratureResponse { Success = false, Message = "Turno non trovato." };
        }

        if (!shift.IsCheckedIn)
        {
            return new TimbratureResponse { Success = false, Message = "Devi prima effettuare il check-in." };
        }

        if (shift.IsCheckedOut)
        {
            return new TimbratureResponse { Success = false, Message = "Hai già effettuato il check-out." };
        }

        var now = DateTime.UtcNow;
        shift.IsCheckedOut = true;
        shift.CheckOutTime = now;
        shift.CheckOutLocation = request.Location;
        shift.UpdatedAt = now;

        // Calcola ore lavorate effettive
        var actualWorkedMinutes = (int)(now - shift.CheckInTime!.Value).TotalMinutes;
        var totalBreakMinutes = shift.Breaks.Where(b => b.BreakEndTime != null)
            .Sum(b => b.DurationMinutes);
        var netWorkedMinutes = actualWorkedMinutes - totalBreakMinutes;

        // Calcola ore previste
        var expectedMinutes = (int)(shift.EndTime - shift.StartTime).TotalMinutes - shift.BreakDurationMinutes;
        var overtimeMinutes = netWorkedMinutes - expectedMinutes;

        // Rilevamento straordinari
        OvertimeRecord? overtime = null;
        string overtimeMessage = "";

        if (overtimeMinutes > AUTO_APPROVE_TOLERANCE_MINUTES)
        {
            overtime = new OvertimeRecord
            {
                ShiftId = shift.Id,
                EmployeeId = employeeId,
                MerchantId = shift.MerchantId,
                DurationMinutes = overtimeMinutes,
                Type = OvertimeType.Pending,
                IsAutoDetected = true,
                Date = shift.Date
            };
            _context.OvertimeRecords.Add(overtime);

            overtimeMessage = $"Rilevato straordinario di {overtimeMinutes} minuti. Come preferisci gestirlo?";
        }

        // Rilevamento anomalie
        ShiftAnomaly? anomaly = null;
        if (Math.Abs(overtimeMinutes) > AUTO_APPROVE_TOLERANCE_MINUTES * 2)
        {
            var type = overtimeMinutes > 0 ? AnomalyType.LateCheckOut : AnomalyType.EarlyCheckOut;
            var message = overtimeMinutes > 0
                ? "Stai facendo molte ore, va tutto bene? Ricorda di prenderti cura di te! 💚"
                : "Noto che oggi hai finito prima. Tutto ok?";

            anomaly = CreateAnomaly(shift.Id, type, 2, message);
            _context.ShiftAnomalies.Add(anomaly);
        }

        // Auto-validazione
        if (Math.Abs(overtimeMinutes) <= AUTO_APPROVE_TOLERANCE_MINUTES)
        {
            shift.ValidationStatus = ValidationStatus.AutoApproved;
            shift.ValidatedAt = now;
            shift.ValidatedBy = "System";
        }
        else
        {
            shift.ValidationStatus = ValidationStatus.RequiresReview;
        }

        await _context.SaveChangesAsync();

        var workedHours = netWorkedMinutes / 60.0m;
        return new TimbratureResponse
        {
            Success = true,
            Message = $"✓ Uscita {now.ToLocalTime():HH:mm}",
            Context = $"Ore lavorate: {workedHours:F1}h (previste: {expectedMinutes / 60.0:F1}h)",
            NextSteps = overtimeMinutes > 0 ? overtimeMessage : "Buon riposo! 🌙",
            EmpatheticMessage = anomaly?.EmpatheticMessage,
            HasOvertime = overtime != null,
            OvertimeMinutes = overtimeMinutes > 0 ? overtimeMinutes : null,
            HasAnomaly = anomaly != null,
            AnomalyType = anomaly?.Type
        };
    }

    public async Task<ShiftBreakDto> StartBreakAsync(int employeeId, StartBreakRequest request)
    {
        var shift = await _context.Shifts
            .Include(s => s.Breaks)
            .FirstOrDefaultAsync(s => s.Id == request.ShiftId && s.EmployeeId == employeeId);

        if (shift == null || !shift.IsCheckedIn || shift.IsCheckedOut)
        {
            throw new InvalidOperationException("Turno non valido o non attivo.");
        }

        var shiftBreak = new ShiftBreak
        {
            ShiftId = shift.Id,
            BreakStartTime = DateTime.UtcNow,
            BreakType = request.BreakType,
            Notes = request.Notes,
            IsAutoSuggested = false
        };

        _context.ShiftBreaks.Add(shiftBreak);
        await _context.SaveChangesAsync();

        return MapToBreakDto(shiftBreak);
    }

    public async Task<ShiftBreakDto> EndBreakAsync(int employeeId, EndBreakRequest request)
    {
        var shiftBreak = await _context.ShiftBreaks
            .Include(sb => sb.Shift)
            .FirstOrDefaultAsync(sb => sb.Id == request.BreakId && sb.Shift.EmployeeId == employeeId);

        if (shiftBreak == null)
        {
            throw new InvalidOperationException("Pausa non trovata.");
        }

        if (shiftBreak.BreakEndTime != null)
        {
            throw new InvalidOperationException("Pausa già terminata.");
        }

        var now = DateTime.UtcNow;
        shiftBreak.BreakEndTime = now;
        shiftBreak.DurationMinutes = (int)(now - shiftBreak.BreakStartTime).TotalMinutes;
        shiftBreak.IsShortBreak = shiftBreak.DurationMinutes < 15;

        await _context.SaveChangesAsync();

        return MapToBreakDto(shiftBreak);
    }

    public async Task<CurrentStatusResponse> GetCurrentStatusAsync(int employeeId)
    {
        var today = DateTime.UtcNow.Date;
        var shift = await _context.Shifts
            .Include(s => s.Breaks)
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.Date == today);

        if (shift == null)
        {
            return new CurrentStatusResponse
            {
                StatusMessage = "Nessun turno oggi",
                SuggestedAction = "Goditi il riposo! 🌟"
            };
        }

        var currentBreak = shift.Breaks.FirstOrDefault(b => b.BreakEndTime == null);
        var workedMinutes = shift.CheckInTime != null && !shift.IsCheckedOut
            ? (int)(DateTime.UtcNow - shift.CheckInTime.Value).TotalMinutes
            : 0;

        var breakMinutes = shift.Breaks.Where(b => b.BreakEndTime != null).Sum(b => b.DurationMinutes);
        if (currentBreak != null)
        {
            breakMinutes += (int)(DateTime.UtcNow - currentBreak.BreakStartTime).TotalMinutes;
        }

        var netWorkedMinutes = workedMinutes - breakMinutes;
        var weekStats = await GetWeeklyHoursAsync(employeeId);

        return new CurrentStatusResponse
        {
            IsCheckedIn = shift.IsCheckedIn && !shift.IsCheckedOut,
            IsOnBreak = currentBreak != null,
            CheckInTime = shift.CheckInTime,
            BreakStartTime = currentBreak?.BreakStartTime,
            StatusMessage = GetStatusMessage(shift, currentBreak),
            SuggestedAction = GetSuggestedAction(shift, currentBreak, netWorkedMinutes),
            CurrentShift = MapToShiftDto(shift),
            CurrentBreak = currentBreak != null ? MapToBreakDto(currentBreak) : null,
            TotalWorkedHoursToday = netWorkedMinutes / 60.0m,
            TotalWorkedHoursThisWeek = weekStats
        };
    }

    public async Task<ShiftDto?> GetTodayShiftAsync(int employeeId)
    {
        var today = DateTime.UtcNow.Date;
        var shift = await _context.Shifts
            .Include(s => s.Merchant)
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.Date == today);

        return shift != null ? MapToShiftDto(shift) : null;
    }

    public async Task<ShiftAnomalyDto> ResolveAnomalyAsync(int employeeId, ResolveAnomalyRequest request)
    {
        var anomaly = await _context.ShiftAnomalies
            .Include(a => a.Shift)
            .FirstOrDefaultAsync(a => a.Id == request.AnomalyId && a.Shift.EmployeeId == employeeId);

        if (anomaly == null)
        {
            throw new InvalidOperationException("Anomalia non trovata.");
        }

        anomaly.EmployeeReason = request.Reason;
        anomaly.EmployeeNotes = request.Notes;
        anomaly.IsResolved = true;
        anomaly.ResolvedAt = DateTime.UtcNow;
        anomaly.ResolutionMethod = "1-click";

        // Auto-approva se motivo valido
        var autoApproveReasons = new[] { AnomalyReason.Traffic, AnomalyReason.TechnicalIssue, AnomalyReason.SmartWorking };
        if (autoApproveReasons.Contains(request.Reason))
        {
            anomaly.RequiresMerchantReview = false;
        }

        await _context.SaveChangesAsync();

        return MapToAnomalyDto(anomaly);
    }

    public async Task<OvertimeRecordDto> ClassifyOvertimeAsync(int employeeId, ClassifyOvertimeRequest request)
    {
        var overtime = await _context.OvertimeRecords
            .FirstOrDefaultAsync(o => o.Id == request.OvertimeId && o.EmployeeId == employeeId);

        if (overtime == null)
        {
            throw new InvalidOperationException("Straordinario non trovato.");
        }

        overtime.Type = request.Type;
        overtime.EmployeeNotes = request.Notes;
        overtime.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToOvertimeDto(overtime);
    }

    public async Task<ShiftCorrectionDto> CorrectShiftAsync(int employeeId, CorrectShiftRequest request)
    {
        var shift = await _context.Shifts
            .FirstOrDefaultAsync(s => s.Id == request.ShiftId && s.EmployeeId == employeeId);

        if (shift == null)
        {
            throw new InvalidOperationException("Turno non trovato.");
        }

        var hoursSinceShift = (DateTime.UtcNow - shift.Date).TotalHours;
        var isWithin24Hours = hoursSinceShift <= SELF_CORRECTION_HOURS;

        var correction = new ShiftCorrection
        {
            ShiftId = shift.Id,
            EmployeeId = employeeId,
            CorrectedField = request.CorrectedField,
            OriginalValue = GetFieldValue(shift, request.CorrectedField),
            NewValue = request.NewValue,
            Reason = request.Reason,
            IsWithin24Hours = isWithin24Hours,
            RequiresMerchantApproval = !isWithin24Hours,
            IsApproved = isWithin24Hours // Auto-approvato se entro 24h
        };

        if (isWithin24Hours)
        {
            // Applica correzione immediatamente
            ApplyCorrection(shift, request.CorrectedField, request.NewValue);
            shift.ValidationStatus = ValidationStatus.SelfCorrected;
        }

        _context.ShiftCorrections.Add(correction);
        await _context.SaveChangesAsync();

        return MapToCorrectionDto(correction);
    }

    public async Task<int> AutoValidateShiftsAsync(int merchantId, DateTime? date = null)
    {
        var targetDate = date ?? DateTime.UtcNow.Date.AddDays(-1); // Default: ieri
        var shifts = await _context.Shifts
            .Include(s => s.Breaks)
            .Where(s => s.MerchantId == merchantId
                && s.Date == targetDate
                && s.IsCheckedIn
                && s.IsCheckedOut
                && s.ValidationStatus == ValidationStatus.Pending)
            .ToListAsync();

        int validatedCount = 0;

        foreach (var shift in shifts)
        {
            if (IsEligibleForAutoValidation(shift))
            {
                shift.ValidationStatus = ValidationStatus.AutoApproved;
                shift.ValidatedAt = DateTime.UtcNow;
                shift.ValidatedBy = "System";
                validatedCount++;
            }
        }

        await _context.SaveChangesAsync();
        return validatedCount;
    }

    public async Task<int> BatchApproveShiftsAsync(int merchantId, List<int> shiftIds)
    {
        var shifts = await _context.Shifts
            .Where(s => s.MerchantId == merchantId && shiftIds.Contains(s.Id))
            .ToListAsync();

        foreach (var shift in shifts)
        {
            shift.ValidationStatus = ValidationStatus.ManuallyApproved;
            shift.ValidatedAt = DateTime.UtcNow;
            shift.ValidatedBy = $"Merchant-{merchantId}";
        }

        await _context.SaveChangesAsync();
        return shifts.Count;
    }

    public async Task<WellbeingStatsResponse> GetWellbeingStatsAsync(int employeeId)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var weekShifts = await _context.Shifts
            .Include(s => s.Breaks)
            .Where(s => s.EmployeeId == employeeId && s.Date >= weekStart && s.IsCheckedIn && s.IsCheckedOut)
            .ToListAsync();

        var monthShifts = await _context.Shifts
            .Include(s => s.Breaks)
            .Where(s => s.EmployeeId == employeeId && s.Date >= monthStart && s.IsCheckedIn && s.IsCheckedOut)
            .ToListAsync();

        var weekHours = CalculateTotalHours(weekShifts);
        var monthHours = CalculateTotalHours(monthShifts);

        var weekOvertime = await _context.OvertimeRecords
            .Where(o => o.EmployeeId == employeeId && o.Date >= weekStart)
            .SumAsync(o => o.DurationMinutes) / 60.0m;

        var monthOvertime = await _context.OvertimeRecords
            .Where(o => o.EmployeeId == employeeId && o.Date >= monthStart)
            .SumAsync(o => o.DurationMinutes) / 60.0m;

        var hasWellbeingAlert = weekHours >= WELLBEING_ALERT_HOURS;
        string? wellbeingMessage = null;

        if (hasWellbeingAlert)
        {
            wellbeingMessage = $"Stai facendo molte ore questa settimana ({weekHours:F1}h). Ricorda di prenderti cura di te! 💚";
        }

        return new WellbeingStatsResponse
        {
            HoursThisWeek = weekHours,
            HoursThisMonth = monthHours,
            OvertimeThisWeek = weekOvertime,
            OvertimeThisMonth = monthOvertime,
            HasWellbeingAlert = hasWellbeingAlert,
            WellbeingMessage = wellbeingMessage
        };
    }

    // Helper methods

    private ShiftAnomaly CreateAnomaly(int shiftId, AnomalyType type, int severity, string message)
    {
        return new ShiftAnomaly
        {
            ShiftId = shiftId,
            Type = type,
            Severity = severity,
            EmpatheticMessage = message,
            RequiresMerchantReview = severity >= 3
        };
    }

    private string CalculateBreakSuggestion(Shift shift)
    {
        // Suggerisci pausa a metà turno
        var midpoint = shift.StartTime.Add(TimeSpan.FromHours((shift.EndTime - shift.StartTime).TotalHours / 2));
        return midpoint.ToString(@"hh\:mm");
    }

    private bool IsEligibleForAutoValidation(Shift shift)
    {
        if (shift.CheckInTime == null || shift.CheckOutTime == null)
            return false;

        var expectedStart = shift.Date.Add(shift.StartTime);
        var expectedEnd = shift.Date.Add(shift.EndTime);

        var checkInDiff = Math.Abs((shift.CheckInTime.Value - expectedStart).TotalMinutes);
        var checkOutDiff = Math.Abs((shift.CheckOutTime.Value - expectedEnd).TotalMinutes);

        // Auto-approva se ±15min e nessuna anomalia grave
        return checkInDiff <= AUTO_APPROVE_TOLERANCE_MINUTES
            && checkOutDiff <= AUTO_APPROVE_TOLERANCE_MINUTES;
    }

    private decimal CalculateTotalHours(List<Shift> shifts)
    {
        decimal totalMinutes = 0;

        foreach (var shift in shifts)
        {
            if (shift.CheckInTime != null && shift.CheckOutTime != null)
            {
                var workedMinutes = (shift.CheckOutTime.Value - shift.CheckInTime.Value).TotalMinutes;
                var breakMinutes = shift.Breaks.Where(b => b.BreakEndTime != null).Sum(b => b.DurationMinutes);
                totalMinutes += (decimal)(workedMinutes - breakMinutes);
            }
        }

        return totalMinutes / 60;
    }

    private async Task<decimal> GetWeeklyHoursAsync(int employeeId)
    {
        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
        var shifts = await _context.Shifts
            .Include(s => s.Breaks)
            .Where(s => s.EmployeeId == employeeId && s.Date >= weekStart && s.IsCheckedIn)
            .ToListAsync();

        return CalculateTotalHours(shifts.Where(s => s.IsCheckedOut).ToList());
    }

    private string GetStatusMessage(Shift shift, ShiftBreak? currentBreak)
    {
        if (!shift.IsCheckedIn)
            return "Non hai ancora fatto check-in";
        if (shift.IsCheckedOut)
            return "Hai completato il turno";
        if (currentBreak != null)
            return "Sei in pausa";
        return "Sei al lavoro";
    }

    private string GetSuggestedAction(Shift shift, ShiftBreak? currentBreak, int netWorkedMinutes)
    {
        if (!shift.IsCheckedIn)
            return "Ricorda di fare check-in quando arrivi";
        if (currentBreak != null)
            return "Goditi la pausa! ☕";
        if (netWorkedMinutes > 240 && !shift.Breaks.Any()) // 4h senza pause
            return "Forse è ora di una pausa?";
        return "Continua così! 💪";
    }

    private string GetFieldValue(Shift shift, string field)
    {
        return field switch
        {
            "CheckInTime" => shift.CheckInTime?.ToString() ?? "",
            "CheckOutTime" => shift.CheckOutTime?.ToString() ?? "",
            _ => ""
        };
    }

    private void ApplyCorrection(Shift shift, string field, string value)
    {
        switch (field)
        {
            case "CheckInTime":
                shift.CheckInTime = DateTime.Parse(value);
                break;
            case "CheckOutTime":
                shift.CheckOutTime = DateTime.Parse(value);
                break;
        }
    }

    // Mapping helpers
    private ShiftDto MapToShiftDto(Shift shift) => new ShiftDto
    {
        Id = shift.Id,
        MerchantId = shift.MerchantId,
        EmployeeId = shift.EmployeeId,
        Date = shift.Date,
        StartTime = shift.StartTime,
        EndTime = shift.EndTime,
        ShiftType = shift.ShiftType,
        IsCheckedIn = shift.IsCheckedIn,
        CheckInTime = shift.CheckInTime,
        IsCheckedOut = shift.IsCheckedOut,
        CheckOutTime = shift.CheckOutTime
    };

    private ShiftBreakDto MapToBreakDto(ShiftBreak b) => new ShiftBreakDto
    {
        Id = b.Id,
        ShiftId = b.ShiftId,
        BreakStartTime = b.BreakStartTime,
        BreakEndTime = b.BreakEndTime,
        DurationMinutes = b.DurationMinutes,
        BreakType = b.BreakType,
        IsAutoSuggested = b.IsAutoSuggested,
        IsShortBreak = b.IsShortBreak
    };

    private ShiftAnomalyDto MapToAnomalyDto(ShiftAnomaly a) => new ShiftAnomalyDto
    {
        Id = a.Id,
        ShiftId = a.ShiftId,
        Type = a.Type,
        Severity = a.Severity,
        EmpatheticMessage = a.EmpatheticMessage,
        EmployeeReason = a.EmployeeReason,
        IsResolved = a.IsResolved,
        DetectedAt = a.DetectedAt
    };

    private OvertimeRecordDto MapToOvertimeDto(OvertimeRecord o) => new OvertimeRecordDto
    {
        Id = o.Id,
        ShiftId = o.ShiftId,
        EmployeeId = o.EmployeeId,
        DurationMinutes = o.DurationMinutes,
        Type = o.Type,
        IsApproved = o.IsApproved,
        Date = o.Date
    };

    private ShiftCorrectionDto MapToCorrectionDto(ShiftCorrection c) => new ShiftCorrectionDto
    {
        Id = c.Id,
        ShiftId = c.ShiftId,
        EmployeeId = c.EmployeeId,
        CorrectedField = c.CorrectedField,
        NewValue = c.NewValue,
        IsWithin24Hours = c.IsWithin24Hours,
        RequiresMerchantApproval = c.RequiresMerchantApproval,
        IsApproved = c.IsApproved
    };
}
