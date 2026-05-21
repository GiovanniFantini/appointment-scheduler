using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Gestione della timbratura. Le violazioni della sequenza (doppio clock-in,
/// clock-out senza clock-in, ...) lanciano InvalidOperationException — il
/// controller la traduce in 400.
/// </summary>
public class TimeClockService : ITimeClockService
{
    private readonly ApplicationDbContext _context;

    public TimeClockService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Stato corrente ─────────────────────────────────────────────────────

    public async Task<CurrentClockStatusDto> GetCurrentStatusAsync(int employeeId, int merchantId)
    {
        var nowUtc = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(nowUtc);

        var shift = await ResolveCurrentShiftAsync(employeeId, merchantId, today);

        var status = new CurrentClockStatusDto();

        if (shift == null)
        {
            status.StatusMessage = "Nessun turno in programma.";
            return status;
        }

        var settings = await GetOrDefaultSettingsAsync(shift.Event.BranchId, merchantId);
        status.TimeClockEnabled = settings.IsEnabled;
        status.RequiresGeolocation = settings.GeofencingEnabled;

        status.CurrentShift = MapShift(shift);

        var entries = await _context.TimeEntries
            .Include(e => e.Branch)
            .Include(e => e.Event)
            .Include(e => e.Employee)
            .Where(e => e.EventParticipantId == shift.Participant.Id)
            .OrderBy(e => e.ActualTimestampUtc)
            .ToListAsync();

        status.TodayEntries = entries.Select(MapEntry).ToList();

        var clockIn = entries.LastOrDefault(e => e.Type == TimeEntryType.ClockIn);
        var clockOut = entries.LastOrDefault(e => e.Type == TimeEntryType.ClockOut);
        var openBreak = FindOpenBreak(entries);

        status.IsClockedIn = clockIn != null && clockOut == null;
        status.IsOnBreak = openBreak != null;
        status.ClockInAtUtc = clockIn?.ActualTimestampUtc;
        status.BreakStartAtUtc = openBreak?.ActualTimestampUtc;
        status.WorkedMinutesToday = CalculateWorkedMinutes(entries, nowUtc);

        var (start, end) = ResolveShiftTimes(shift);
        var startWall = ToWallClock(shift.Event.StartDate, start);

        if (clockOut != null)
        {
            status.StatusMessage = "Turno completato.";
        }
        else if (status.IsOnBreak)
        {
            status.StatusMessage = "In pausa.";
            status.SuggestedAction = "Termina la pausa";
            status.ShowClockPrompt = true;
        }
        else if (status.IsClockedIn)
        {
            // L'orario di entrata mostrato è quello effettivamente timbrato.
            var since = clockIn!.ActualTimestampUtc.ToLocalTime().ToString("HH:mm");
            status.StatusMessage = $"In turno da {since}.";
            status.SuggestedAction = "Timbra l'uscita";
            status.ShowClockPrompt = true;
        }
        else
        {
            status.StatusMessage = "Turno da iniziare.";
            status.SuggestedAction = "Timbra l'entrata";
            // Mostra il prompt se siamo nell'intorno dell'inizio turno:
            // da EarlyClockInTolerance prima fino alla fine del turno.
            var endWall = ToWallClock(shift.Event.EndDate ?? shift.Event.StartDate, end);
            var windowStart = startWall?.AddMinutes(-settings.EarlyClockInToleranceMinutes);
            var nowWall = NowWallClock;
            status.ShowClockPrompt = windowStart != null && endWall != null
                && nowWall >= windowStart && nowWall <= endWall;
        }

        return status;
    }

    // ── Azioni di timbratura ───────────────────────────────────────────────

    public Task<ClockActionResultDto> ClockInAsync(int employeeId, int merchantId, ClockActionRequest request)
        => RegisterAsync(employeeId, merchantId, request, TimeEntryType.ClockIn);

    public Task<ClockActionResultDto> ClockOutAsync(int employeeId, int merchantId, ClockActionRequest request)
        => RegisterAsync(employeeId, merchantId, request, TimeEntryType.ClockOut);

    public Task<ClockActionResultDto> StartBreakAsync(int employeeId, int merchantId, ClockActionRequest request)
        => RegisterAsync(employeeId, merchantId, request, TimeEntryType.BreakStart);

    public Task<ClockActionResultDto> EndBreakAsync(int employeeId, int merchantId, ClockActionRequest request)
        => RegisterAsync(employeeId, merchantId, request, TimeEntryType.BreakEnd);

    private async Task<ClockActionResultDto> RegisterAsync(
        int employeeId, int merchantId, ClockActionRequest request, TimeEntryType type)
    {
        var nowUtc = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(nowUtc);

        var shift = request.EventParticipantId.HasValue
            ? await ResolveShiftByParticipantAsync(request.EventParticipantId.Value, employeeId, merchantId)
            : await ResolveCurrentShiftAsync(employeeId, merchantId, today);

        if (shift == null)
            throw new InvalidOperationException(
                "Nessun turno disponibile per la timbratura. È possibile timbrare solo su un turno pianificato.");

        var settings = await GetOrDefaultSettingsAsync(shift.Event.BranchId, merchantId);
        if (!settings.IsEnabled)
            throw new InvalidOperationException("La timbratura non è attiva per questa filiale.");

        if ((type == TimeEntryType.BreakStart || type == TimeEntryType.BreakEnd) && !settings.BreakTrackingEnabled)
            throw new InvalidOperationException("La registrazione delle pause non è attiva per questa filiale.");

        var entries = await _context.TimeEntries
            .Where(e => e.EventParticipantId == shift.Participant.Id)
            .OrderBy(e => e.ActualTimestampUtc)
            .ToListAsync();

        ValidateSequence(entries, type);

        var (start, end) = ResolveShiftTimes(shift);
        TimeOnly? expectedTime = type switch
        {
            TimeEntryType.ClockIn => start,
            TimeEntryType.ClockOut => end,
            _ => null
        };

        int? deviation = null;
        if (expectedTime.HasValue)
        {
            // Deviazione = differenza wall-clock tra adesso e l'orario atteso.
            var expectedWall = ToWallClock(
                type == TimeEntryType.ClockOut ? shift.Event.EndDate ?? shift.Event.StartDate : shift.Event.StartDate,
                expectedTime);
            if (expectedWall.HasValue)
                deviation = (int)Math.Round((NowWallClock - expectedWall.Value).TotalMinutes);
        }

        var entry = new TimeEntry
        {
            MerchantId = merchantId,
            BranchId = shift.Event.BranchId,
            EmployeeId = employeeId,
            EventId = shift.Event.Id,
            EventParticipantId = shift.Participant.Id,
            Type = type,
            Source = TimeEntrySource.Web,
            Status = TimeEntryStatus.Ok,
            WorkDate = shift.Event.StartDate,
            ActualTimestampUtc = nowUtc,
            ExpectedTime = expectedTime,
            DeviationMinutes = deviation,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            GpsAccuracyMeters = request.GpsAccuracyMeters,
            Notes = request.Notes,
            CreatedAt = nowUtc
        };

        // Geofence: valutato ma mai bloccante.
        await ApplyGeofenceAsync(entry, settings, request);

        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();

        // Rileva eventuali anomalie sulla timbratura appena registrata.
        var anomaly = await DetectAnomaliesForEntryAsync(entry, settings, entries);
        if (anomaly != null)
        {
            entry.Status = TimeEntryStatus.Anomaly;
            await _context.SaveChangesAsync();
        }

        var status = await GetCurrentStatusAsync(employeeId, merchantId);

        var result = new ClockActionResultDto
        {
            Success = true,
            Entry = MapEntry(await ReloadEntryAsync(entry.Id)),
            Status = status,
            Anomaly = anomaly == null ? null : MapAnomaly(anomaly),
            Message = anomaly != null
                ? AnomalyMessage(anomaly)
                : type switch
                {
                    TimeEntryType.ClockIn => "Entrata registrata.",
                    TimeEntryType.ClockOut => "Uscita registrata.",
                    TimeEntryType.BreakStart => "Pausa avviata.",
                    TimeEntryType.BreakEnd => "Pausa terminata.",
                    _ => "Timbratura registrata."
                }
        };

        if (type == TimeEntryType.ClockOut)
        {
            entries.Add(entry);
            result.WorkedShiftMinutes = CalculateWorkedMinutes(entries, nowUtc);
        }

        return result;
    }

    // ── Storico ────────────────────────────────────────────────────────────

    public async Task<List<TimeEntryDto>> GetMyEntriesAsync(int employeeId, int merchantId, DateOnly from, DateOnly to)
    {
        var entries = await _context.TimeEntries
            .Include(e => e.Branch)
            .Include(e => e.Event)
            .Include(e => e.Employee)
            .Where(e => e.EmployeeId == employeeId && e.MerchantId == merchantId
                        && e.WorkDate >= from && e.WorkDate <= to)
            .OrderByDescending(e => e.ActualTimestampUtc)
            .ToListAsync();

        return entries.Select(MapEntry).ToList();
    }

    public async Task<List<TimeEntryDto>> GetEntriesAsync(
        int merchantId, int? branchId, DateOnly from, DateOnly to, int? employeeId)
    {
        var query = _context.TimeEntries
            .Include(e => e.Branch)
            .Include(e => e.Event)
            .Include(e => e.Employee)
            .Where(e => e.MerchantId == merchantId && e.WorkDate >= from && e.WorkDate <= to);

        if (branchId.HasValue)
            query = query.Where(e => e.BranchId == branchId.Value);
        if (employeeId.HasValue)
            query = query.Where(e => e.EmployeeId == employeeId.Value);

        var entries = await query
            .OrderByDescending(e => e.ActualTimestampUtc)
            .ToListAsync();

        return entries.Select(MapEntry).ToList();
    }

    public Task<List<TimeClockReportRowDto>> GetReportAsync(
        int merchantId, int? branchId, DateOnly from, DateOnly to)
        => GetReportInternalAsync(merchantId, branchId, from, to, employeeId: null);

    private async Task<List<TimeClockReportRowDto>> GetReportInternalAsync(
        int merchantId, int? branchId, DateOnly from, DateOnly to, int? employeeId)
    {
        var query = _context.TimeEntries
            .Include(e => e.Branch)
            .Include(e => e.Employee)
            .Include(e => e.Event)
            .Include(e => e.EventParticipant)
            .Where(e => e.MerchantId == merchantId && e.WorkDate >= from && e.WorkDate <= to);

        if (branchId.HasValue)
            query = query.Where(e => e.BranchId == branchId.Value);
        if (employeeId.HasValue)
            query = query.Where(e => e.EmployeeId == employeeId.Value);

        var entries = await query.ToListAsync();
        if (entries.Count == 0) return new List<TimeClockReportRowDto>();

        // Anomalie aperte/in revisione per turno, per il flag HasOpenAnomaly.
        var participantIds = entries.Select(e => e.EventParticipantId).Distinct().ToList();
        var anomalyParticipants = await _context.TimeClockAnomalies
            .Where(a => a.MerchantId == merchantId
                        && a.EventParticipantId != null
                        && participantIds.Contains(a.EventParticipantId!.Value)
                        && (a.Status == TimeClockAnomalyStatus.Open
                            || a.Status == TimeClockAnomalyStatus.Justified))
            .Select(a => a.EventParticipantId!.Value)
            .ToListAsync();
        var anomalySet = anomalyParticipants.ToHashSet();

        var rows = new List<TimeClockReportRowDto>();

        // Una riga per turno (EventParticipant).
        foreach (var group in entries.GroupBy(e => e.EventParticipantId))
        {
            var groupEntries = group.OrderBy(e => e.ActualTimestampUtc).ToList();
            var first = groupEntries[0];
            var clockIn = groupEntries.FirstOrDefault(e => e.Type == TimeEntryType.ClockIn);
            var clockOut = groupEntries.LastOrDefault(e => e.Type == TimeEntryType.ClockOut);

            var worked = CalculateWorkedMinutes(groupEntries, DateTime.UtcNow);
            var breakMinutes = SumBreakMinutes(groupEntries);

            double? scheduled = null;
            var participant = first.EventParticipant;
            var ev = first.Event;
            if (participant != null && ev != null)
            {
                var start = participant.StartTimeOverride ?? ev.StartTime;
                var end = participant.EndTimeOverride ?? ev.EndTime;
                if (start.HasValue && end.HasValue)
                {
                    var span = end.Value.ToTimeSpan() - start.Value.ToTimeSpan();
                    if (span < TimeSpan.Zero) span += TimeSpan.FromDays(1); // turno notturno
                    scheduled = span.TotalMinutes;
                }
            }

            var overtime = scheduled.HasValue ? Math.Max(0, worked - scheduled.Value) : 0;

            rows.Add(new TimeClockReportRowDto
            {
                EmployeeId = first.EmployeeId,
                EmployeeName = first.Employee == null
                    ? string.Empty : $"{first.Employee.FirstName} {first.Employee.LastName}",
                BranchId = first.BranchId,
                BranchName = first.Branch?.Name ?? string.Empty,
                WorkDate = first.WorkDate,
                EventTitle = first.Event?.Title ?? string.Empty,
                ClockInUtc = clockIn?.ActualTimestampUtc,
                ClockOutUtc = clockOut?.ActualTimestampUtc,
                WorkedMinutes = worked,
                BreakMinutes = breakMinutes,
                ScheduledMinutes = scheduled,
                OvertimeMinutes = Math.Round(overtime, 1),
                HasOpenAnomaly = anomalySet.Contains(group.Key)
            });
        }

        return rows
            .OrderByDescending(r => r.WorkDate)
            .ThenBy(r => r.EmployeeName)
            .ToList();
    }

    private static double SumBreakMinutes(List<TimeEntry> entries)
    {
        double total = 0;
        TimeEntry? start = null;
        foreach (var e in entries.OrderBy(x => x.ActualTimestampUtc))
        {
            if (e.Type == TimeEntryType.BreakStart) start = e;
            else if (e.Type == TimeEntryType.BreakEnd && start != null)
            {
                total += (e.ActualTimestampUtc - start.ActualTimestampUtc).TotalMinutes;
                start = null;
            }
        }
        return Math.Round(total, 1);
    }

    // ── Configurazione ─────────────────────────────────────────────────────

    public async Task<BranchTimeClockSettingsDto> GetSettingsAsync(int branchId, int merchantId)
    {
        var branch = await _context.MerchantBranches
            .FirstOrDefaultAsync(b => b.Id == branchId && b.MerchantId == merchantId)
            ?? throw new InvalidOperationException("Filiale non trovata.");

        var settings = await GetOrDefaultSettingsAsync(branchId, merchantId);
        return MapSettings(settings, branch);
    }

    public async Task<BranchTimeClockSettingsDto> UpdateSettingsAsync(
        int branchId, int merchantId, UpdateTimeClockSettingsRequest request)
    {
        var branch = await _context.MerchantBranches
            .FirstOrDefaultAsync(b => b.Id == branchId && b.MerchantId == merchantId)
            ?? throw new InvalidOperationException("Filiale non trovata.");

        if (request.GraceInMinutes < 0 || request.GraceOutMinutes < 0
            || request.EarlyClockInToleranceMinutes < 0 || request.LateClockOutToleranceMinutes < 0
            || request.GeofenceRadiusMeters < 0 || request.MaxBreakMinutes < 0 || request.RoundingMinutes < 0)
            throw new InvalidOperationException("I valori di configurazione non possono essere negativi.");

        var settings = await _context.BranchTimeClockSettings
            .FirstOrDefaultAsync(s => s.BranchId == branchId);

        if (settings == null)
        {
            settings = new BranchTimeClockSettings { BranchId = branchId, MerchantId = merchantId };
            _context.BranchTimeClockSettings.Add(settings);
        }

        settings.IsEnabled = request.IsEnabled;
        settings.ClockingRequired = request.ClockingRequired;
        settings.GraceInMinutes = request.GraceInMinutes;
        settings.GraceOutMinutes = request.GraceOutMinutes;
        settings.EarlyClockInToleranceMinutes = request.EarlyClockInToleranceMinutes;
        settings.LateClockOutToleranceMinutes = request.LateClockOutToleranceMinutes;
        settings.GeofencingEnabled = request.GeofencingEnabled;
        settings.GeofenceRadiusMeters = request.GeofenceRadiusMeters;
        settings.BreakTrackingEnabled = request.BreakTrackingEnabled;
        settings.MaxBreakMinutes = request.MaxBreakMinutes;
        settings.RoundingMinutes = request.RoundingMinutes;
        settings.RequirePhoto = request.RequirePhoto;
        settings.UpdatedAt = DateTime.UtcNow;

        // Le coordinate del geofence vivono sulla filiale: aggiornale se fornite.
        if (request.BranchLatitude.HasValue && request.BranchLongitude.HasValue)
        {
            branch.Latitude = request.BranchLatitude;
            branch.Longitude = request.BranchLongitude;
            branch.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return MapSettings(settings, branch);
    }

    public async Task<TimeEntryDto> CreateManualEntryAsync(int merchantId, int userId, CreateManualEntryRequest request)
    {
        var participant = await _context.EventParticipants
            .Include(p => p.Event)
            .FirstOrDefaultAsync(p => p.Id == request.EventParticipantId
                                      && p.Event.MerchantId == merchantId)
            ?? throw new InvalidOperationException("Partecipazione al turno non trovata.");

        if (participant.Event.EventType != EventType.Turno)
            throw new InvalidOperationException("La timbratura si applica solo ai turni.");

        var nowUtc = DateTime.UtcNow;
        var entry = new TimeEntry
        {
            MerchantId = merchantId,
            BranchId = participant.Event.BranchId,
            EmployeeId = participant.EmployeeId,
            EventId = participant.EventId,
            EventParticipantId = participant.Id,
            Type = request.Type,
            Source = TimeEntrySource.Manual,
            Status = TimeEntryStatus.Corrected,
            WorkDate = participant.Event.StartDate,
            ActualTimestampUtc = request.ActualTimestampUtc,
            Notes = request.Notes,
            IsManualCorrection = true,
            CorrectedByUserId = userId,
            CorrectedAt = nowUtc,
            CreatedAt = nowUtc
        };

        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();

        return MapEntry(await ReloadEntryAsync(entry.Id));
    }

    // ── Anomalie ───────────────────────────────────────────────────────────

    public async Task<List<TimeClockAnomalyDto>> GetMyAnomaliesAsync(
        int employeeId, int merchantId, TimeClockAnomalyStatus? status)
    {
        var query = _context.TimeClockAnomalies
            .Include(a => a.Employee)
            .Include(a => a.Event)
            .Where(a => a.EmployeeId == employeeId && a.MerchantId == merchantId);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var anomalies = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return anomalies.Select(MapAnomaly).ToList();
    }

    public async Task<TimeClockAnomalyDto> JustifyAnomalyAsync(
        int anomalyId, int employeeId, int merchantId, JustifyAnomalyRequest request)
    {
        var anomaly = await _context.TimeClockAnomalies
            .Include(a => a.Employee)
            .Include(a => a.Event)
            .FirstOrDefaultAsync(a => a.Id == anomalyId
                                      && a.EmployeeId == employeeId
                                      && a.MerchantId == merchantId)
            ?? throw new InvalidOperationException("Anomalia non trovata.");

        if (anomaly.Status != TimeClockAnomalyStatus.Open)
            throw new InvalidOperationException("Questa anomalia è già stata giustificata o revisionata.");

        anomaly.EmployeeReason = request.Reason;
        anomaly.EmployeeNotes = request.Notes;
        anomaly.JustifiedAt = DateTime.UtcNow;
        anomaly.Status = TimeClockAnomalyStatus.Justified;
        anomaly.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapAnomaly(anomaly);
    }

    public async Task<WellbeingStatsDto> GetWellbeingStatsAsync(int employeeId, int merchantId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // Inizio settimana = lunedì.
        int dow = ((int)today.DayOfWeek + 6) % 7;
        var weekStart = today.AddDays(-dow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var rangeStart = weekStart < monthStart ? weekStart : monthStart;

        var report = await GetReportInternalAsync(merchantId, null, rangeStart, today,
            employeeId: employeeId);

        double weekWorked = 0, weekOt = 0, monthWorked = 0, monthOt = 0;
        var workedDates = new HashSet<DateOnly>();

        foreach (var r in report)
        {
            if (r.WorkDate >= monthStart) { monthWorked += r.WorkedMinutes; monthOt += r.OvertimeMinutes; }
            if (r.WorkDate >= weekStart) { weekWorked += r.WorkedMinutes; weekOt += r.OvertimeMinutes; }
            if (r.WorkedMinutes > 0) workedDates.Add(r.WorkDate);
        }

        // Giorni lavorati consecutivi fino a oggi.
        int consecutive = 0;
        var cursor = today;
        while (workedDates.Contains(cursor))
        {
            consecutive++;
            cursor = cursor.AddDays(-1);
        }

        var openAnomalies = await _context.TimeClockAnomalies
            .CountAsync(a => a.EmployeeId == employeeId && a.MerchantId == merchantId
                             && a.Status == TimeClockAnomalyStatus.Open);

        var stats = new WellbeingStatsDto
        {
            WorkedMinutesThisWeek = Math.Round(weekWorked, 1),
            WorkedMinutesThisMonth = Math.Round(monthWorked, 1),
            OvertimeMinutesThisWeek = Math.Round(weekOt, 1),
            OvertimeMinutesThisMonth = Math.Round(monthOt, 1),
            OpenAnomalies = openAnomalies,
            ConsecutiveWorkedDays = consecutive
        };

        // Soglie di benessere: >6 giorni consecutivi o >48h settimanali.
        if (consecutive >= 6)
        {
            stats.HasWellbeingAlert = true;
            stats.WellbeingMessage = $"Hai lavorato {consecutive} giorni consecutivi: valuta una giornata di riposo.";
        }
        else if (weekWorked > 48 * 60)
        {
            stats.HasWellbeingAlert = true;
            stats.WellbeingMessage = "Hai superato le 48 ore di lavoro questa settimana.";
        }

        return stats;
    }

    public async Task<List<TimeClockAnomalyDto>> GetAnomaliesAsync(
        int merchantId, int? branchId, TimeClockAnomalyStatus? status)
    {
        var query = _context.TimeClockAnomalies
            .Include(a => a.Employee)
            .Include(a => a.Event)
            .Where(a => a.MerchantId == merchantId);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        // Il filtro filiale passa per il turno collegato.
        if (branchId.HasValue)
            query = query.Where(a => a.Event != null && a.Event.BranchId == branchId.Value);

        var anomalies = await query
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        return anomalies.Select(MapAnomaly).ToList();
    }

    public Task<TimeClockAnomalyDto> ApproveAnomalyAsync(
        int anomalyId, int merchantId, int userId, ReviewAnomalyRequest request)
        => ReviewAnomalyAsync(anomalyId, merchantId, userId, request, TimeClockAnomalyStatus.Approved);

    public Task<TimeClockAnomalyDto> RejectAnomalyAsync(
        int anomalyId, int merchantId, int userId, ReviewAnomalyRequest request)
        => ReviewAnomalyAsync(anomalyId, merchantId, userId, request, TimeClockAnomalyStatus.Rejected);

    private async Task<TimeClockAnomalyDto> ReviewAnomalyAsync(
        int anomalyId, int merchantId, int userId, ReviewAnomalyRequest request, TimeClockAnomalyStatus newStatus)
    {
        var anomaly = await _context.TimeClockAnomalies
            .Include(a => a.Employee)
            .Include(a => a.Event)
            .FirstOrDefaultAsync(a => a.Id == anomalyId && a.MerchantId == merchantId)
            ?? throw new InvalidOperationException("Anomalia non trovata.");

        if (anomaly.Status == TimeClockAnomalyStatus.Approved || anomaly.Status == TimeClockAnomalyStatus.Rejected)
            throw new InvalidOperationException("Questa anomalia è già stata revisionata.");

        anomaly.Status = newStatus;
        anomaly.ReviewedByUserId = userId;
        anomaly.ReviewNotes = request.ReviewNotes;
        anomaly.ReviewedAt = DateTime.UtcNow;
        anomaly.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapAnomaly(anomaly);
    }

    public async Task<int> RunMissingPunchDetectionAsync(int merchantId, int? branchId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Turni conclusi (data passata) di tipo Turno del merchant.
        var query = _context.EventParticipants
            .Include(p => p.Event)
            .Where(p => p.Event.MerchantId == merchantId
                        && p.Event.EventType == EventType.Turno
                        && p.Event.StartDate < today);

        if (branchId.HasValue)
            query = query.Where(p => p.Event.BranchId == branchId.Value);

        var participants = await query.ToListAsync();
        if (participants.Count == 0) return 0;

        var participantIds = participants.Select(p => p.Id).ToList();

        // Timbrature esistenti per quei turni.
        var entries = await _context.TimeEntries
            .Where(e => participantIds.Contains(e.EventParticipantId))
            .ToListAsync();
        var entriesByParticipant = entries
            .GroupBy(e => e.EventParticipantId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Anomalie missing-punch già esistenti, per non duplicarle.
        var existing = await _context.TimeClockAnomalies
            .Where(a => a.MerchantId == merchantId
                        && (a.Type == TimeClockAnomalyType.MissingClockIn
                            || a.Type == TimeClockAnomalyType.MissingClockOut))
            .Select(a => new { a.EventParticipantId, a.Type })
            .ToListAsync();
        var existingSet = existing
            .Select(x => (x.EventParticipantId, x.Type))
            .ToHashSet();

        int created = 0;
        var nowUtc = DateTime.UtcNow;

        foreach (var p in participants)
        {
            var pEntries = entriesByParticipant.GetValueOrDefault(p.Id) ?? new List<TimeEntry>();
            var hasClockIn = pEntries.Any(e => e.Type == TimeEntryType.ClockIn);
            var hasClockOut = pEntries.Any(e => e.Type == TimeEntryType.ClockOut);

            // Niente timbrature affatto: una sola anomalia MissingClockIn.
            var clockInKey = ((int?)p.Id, TimeClockAnomalyType.MissingClockIn);
            var clockOutKey = ((int?)p.Id, TimeClockAnomalyType.MissingClockOut);

            if (!hasClockIn && !existingSet.Contains(clockInKey))
            {
                existingSet.Add(clockInKey);
                _context.TimeClockAnomalies.Add(new TimeClockAnomaly
                {
                    MerchantId = merchantId,
                    EmployeeId = p.EmployeeId,
                    EventId = p.EventId,
                    EventParticipantId = p.Id,
                    Type = TimeClockAnomalyType.MissingClockIn,
                    Status = TimeClockAnomalyStatus.Open,
                    Severity = 2,
                    WorkDate = p.Event.StartDate,
                    CreatedAt = nowUtc
                });
                created++;
            }
            // Entrata ma nessuna uscita.
            else if (hasClockIn && !hasClockOut && !existingSet.Contains(clockOutKey))
            {
                existingSet.Add(clockOutKey);
                _context.TimeClockAnomalies.Add(new TimeClockAnomaly
                {
                    MerchantId = merchantId,
                    EmployeeId = p.EmployeeId,
                    EventId = p.EventId,
                    EventParticipantId = p.Id,
                    Type = TimeClockAnomalyType.MissingClockOut,
                    Status = TimeClockAnomalyStatus.Open,
                    Severity = 2,
                    WorkDate = p.Event.StartDate,
                    CreatedAt = nowUtc
                });
                created++;
            }
        }

        if (created > 0)
            await _context.SaveChangesAsync();

        return created;
    }

    /// <summary>
    /// Rileva un'anomalia sulla timbratura appena registrata in base ai grace
    /// period configurati. Restituisce l'anomalia creata, o null se regolare.
    /// </summary>
    private async Task<TimeClockAnomaly?> DetectAnomaliesForEntryAsync(
        TimeEntry entry, BranchTimeClockSettings settings, List<TimeEntry> priorEntries)
    {
        TimeClockAnomalyType? type = null;
        int severity = 1;
        int? overtime = null;

        switch (entry.Type)
        {
            case TimeEntryType.ClockIn when entry.DeviationMinutes is int dIn:
                if (dIn > settings.GraceInMinutes) { type = TimeClockAnomalyType.LateClockIn; severity = 2; }
                else if (-dIn > settings.EarlyClockInToleranceMinutes) type = TimeClockAnomalyType.EarlyClockIn;
                break;

            case TimeEntryType.ClockOut when entry.DeviationMinutes is int dOut:
                if (-dOut > settings.GraceOutMinutes) { type = TimeClockAnomalyType.EarlyClockOut; severity = 2; }
                else if (dOut > settings.LateClockOutToleranceMinutes)
                {
                    type = TimeClockAnomalyType.LateClockOut;
                    overtime = dOut;
                }
                break;

            case TimeEntryType.BreakEnd:
                // Durata della pausa appena chiusa.
                var lastBreakStart = priorEntries
                    .Where(e => e.Type == TimeEntryType.BreakStart)
                    .OrderByDescending(e => e.ActualTimestampUtc)
                    .FirstOrDefault();
                if (lastBreakStart != null)
                {
                    var breakMin = (entry.ActualTimestampUtc - lastBreakStart.ActualTimestampUtc).TotalMinutes;
                    if (breakMin > settings.MaxBreakMinutes) type = TimeClockAnomalyType.ExtendedBreak;
                }
                break;
        }

        // Fuori area geofence: anomalia indipendente dal tipo.
        if (type == null && entry.GeofenceOk == false)
            type = TimeClockAnomalyType.LocationMismatch;

        if (type == null) return null;

        var anomaly = new TimeClockAnomaly
        {
            MerchantId = entry.MerchantId,
            EmployeeId = entry.EmployeeId,
            EventId = entry.EventId,
            EventParticipantId = entry.EventParticipantId,
            TimeEntryId = entry.Id,
            Type = type.Value,
            Status = TimeClockAnomalyStatus.Open,
            Severity = severity,
            WorkDate = entry.WorkDate,
            DeviationMinutes = entry.DeviationMinutes,
            OvertimeMinutes = overtime,
            CreatedAt = DateTime.UtcNow
        };

        _context.TimeClockAnomalies.Add(anomaly);
        await _context.SaveChangesAsync();
        return anomaly;
    }

    private static string AnomalyMessage(TimeClockAnomaly a) => a.Type switch
    {
        TimeClockAnomalyType.LateClockIn => "Timbratura registrata — risulti in ritardo. Puoi giustificare l'anomalia.",
        TimeClockAnomalyType.EarlyClockIn => "Timbratura registrata in anticipo rispetto al turno.",
        TimeClockAnomalyType.EarlyClockOut => "Uscita registrata in anticipo. Puoi giustificare l'anomalia.",
        TimeClockAnomalyType.LateClockOut => "Uscita registrata oltre l'orario di turno.",
        TimeClockAnomalyType.ExtendedBreak => "Pausa più lunga del previsto. Puoi giustificare l'anomalia.",
        TimeClockAnomalyType.LocationMismatch => "Timbratura registrata fuori dall'area della filiale.",
        _ => "Timbratura registrata con un'anomalia."
    };

    // ── Helper di dominio ──────────────────────────────────────────────────

    /// <summary>Turno+partecipazione risolti insieme per non perdere il context.</summary>
    private sealed record ShiftContext(Event Event, EventParticipant Participant);

    /// <summary>
    /// Risolve il turno "corrente" del dipendente: il turno di tipo Turno con
    /// data odierna (o quello aperto a cavallo di mezzanotte). Se ce ne sono più
    /// d'uno sceglie quello la cui finestra è più vicina all'ora corrente.
    /// </summary>
    private async Task<ShiftContext?> ResolveCurrentShiftAsync(int employeeId, int merchantId, DateOnly today)
    {
        var yesterday = today.AddDays(-1);

        var participants = await _context.EventParticipants
            .Include(p => p.Event)
                .ThenInclude(ev => ev.Branch)
            .Where(p => p.EmployeeId == employeeId
                        && p.Event.MerchantId == merchantId
                        && p.Event.EventType == EventType.Turno
                        && (p.Event.StartDate == today || p.Event.StartDate == yesterday))
            .ToListAsync();

        if (participants.Count == 0)
            return null;

        // Preferisci un turno la cui finestra contiene "adesso", altrimenti il
        // più vicino nel tempo.
        ShiftContext? best = null;
        double bestDistance = double.MaxValue;

        foreach (var p in participants)
        {
            // Scarta i turni di ieri se già chiusi con un clock-out.
            if (p.Event.StartDate == yesterday)
            {
                var hasClockOut = await _context.TimeEntries
                    .AnyAsync(e => e.EventParticipantId == p.Id && e.Type == TimeEntryType.ClockOut);
                if (hasClockOut) continue;
            }

            var ctx = new ShiftContext(p.Event, p);
            var (start, end) = ResolveShiftTimes(ctx);
            var startWall = ToWallClock(p.Event.StartDate, start);
            var endWall = ToWallClock(p.Event.EndDate ?? p.Event.StartDate, end);
            var nowWall = NowWallClock;

            double distance;
            if (startWall.HasValue && endWall.HasValue && nowWall >= startWall && nowWall <= endWall)
                distance = 0; // turno in corso
            else if (startWall.HasValue)
                distance = Math.Abs((startWall.Value - nowWall).TotalMinutes);
            else
                distance = double.MaxValue / 2; // turno all-day senza orario

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = ctx;
            }
        }

        return best;
    }

    private async Task<ShiftContext?> ResolveShiftByParticipantAsync(int participantId, int employeeId, int merchantId)
    {
        var participant = await _context.EventParticipants
            .Include(p => p.Event)
                .ThenInclude(ev => ev.Branch)
            .FirstOrDefaultAsync(p => p.Id == participantId
                                      && p.EmployeeId == employeeId
                                      && p.Event.MerchantId == merchantId);

        if (participant == null || participant.Event.EventType != EventType.Turno)
            return null;

        return new ShiftContext(participant.Event, participant);
    }

    /// <summary>Orari effettivi del turno: override del partecipante o quelli dell'evento.</summary>
    private static (TimeOnly? Start, TimeOnly? End) ResolveShiftTimes(ShiftContext shift)
    {
        var start = shift.Participant.StartTimeOverride ?? shift.Event.StartTime;
        var end = shift.Participant.EndTimeOverride ?? shift.Event.EndTime;
        return (start, end);
    }

    /// <summary>
    /// Valida la sequenza delle timbrature: no doppio clock-in, no clock-out
    /// senza clock-in, no break senza essere in turno, no break annidati.
    /// </summary>
    private static void ValidateSequence(List<TimeEntry> entries, TimeEntryType next)
    {
        var hasClockIn = entries.Any(e => e.Type == TimeEntryType.ClockIn);
        var hasClockOut = entries.Any(e => e.Type == TimeEntryType.ClockOut);
        var openBreak = FindOpenBreak(entries) != null;

        switch (next)
        {
            case TimeEntryType.ClockIn:
                if (hasClockIn)
                    throw new InvalidOperationException("Entrata già registrata per questo turno.");
                break;

            case TimeEntryType.ClockOut:
                if (!hasClockIn)
                    throw new InvalidOperationException("Devi prima timbrare l'entrata.");
                if (hasClockOut)
                    throw new InvalidOperationException("Uscita già registrata per questo turno.");
                if (openBreak)
                    throw new InvalidOperationException("Termina la pausa prima di timbrare l'uscita.");
                break;

            case TimeEntryType.BreakStart:
                if (!hasClockIn)
                    throw new InvalidOperationException("Devi prima timbrare l'entrata.");
                if (hasClockOut)
                    throw new InvalidOperationException("Il turno è già concluso.");
                if (openBreak)
                    throw new InvalidOperationException("Hai già una pausa in corso.");
                break;

            case TimeEntryType.BreakEnd:
                if (!openBreak)
                    throw new InvalidOperationException("Nessuna pausa in corso.");
                break;
        }
    }

    /// <summary>L'ultimo BreakStart non ancora seguito da un BreakEnd, se esiste.</summary>
    private static TimeEntry? FindOpenBreak(List<TimeEntry> entries)
    {
        TimeEntry? open = null;
        foreach (var e in entries.OrderBy(x => x.ActualTimestampUtc))
        {
            if (e.Type == TimeEntryType.BreakStart) open = e;
            else if (e.Type == TimeEntryType.BreakEnd) open = null;
        }
        return open;
    }

    /// <summary>
    /// Minuti lavorati = (ClockOut o adesso) − ClockIn − somma delle pause chiuse.
    /// Restituisce 0 se non c'è un clock-in.
    /// </summary>
    private static double CalculateWorkedMinutes(List<TimeEntry> entries, DateTime nowUtc)
    {
        var ordered = entries.OrderBy(e => e.ActualTimestampUtc).ToList();
        var clockIn = ordered.FirstOrDefault(e => e.Type == TimeEntryType.ClockIn);
        if (clockIn == null) return 0;

        var clockOut = ordered.LastOrDefault(e => e.Type == TimeEntryType.ClockOut);
        var endUtc = clockOut?.ActualTimestampUtc ?? nowUtc;

        var gross = (endUtc - clockIn.ActualTimestampUtc).TotalMinutes;

        double breakMinutes = 0;
        TimeEntry? breakStart = null;
        foreach (var e in ordered)
        {
            if (e.Type == TimeEntryType.BreakStart) breakStart = e;
            else if (e.Type == TimeEntryType.BreakEnd && breakStart != null)
            {
                breakMinutes += (e.ActualTimestampUtc - breakStart.ActualTimestampUtc).TotalMinutes;
                breakStart = null;
            }
        }

        return Math.Max(0, Math.Round(gross - breakMinutes, 1));
    }

    /// <summary>
    /// Valuta il geofence senza mai bloccare la timbratura. GeofenceOk resta null
    /// se il geofence è disattivato, se la filiale non ha coordinate o se il
    /// device non ha fornito la posizione.
    /// </summary>
    private async Task ApplyGeofenceAsync(TimeEntry entry, BranchTimeClockSettings settings, ClockActionRequest request)
    {
        if (!settings.GeofencingEnabled) return;
        if (request.Latitude == null || request.Longitude == null) return;

        var branch = await _context.MerchantBranches
            .FirstOrDefaultAsync(b => b.Id == entry.BranchId);
        if (branch?.Latitude == null || branch.Longitude == null) return;

        var distance = DistanceMeters(
            request.Latitude.Value, request.Longitude.Value,
            branch.Latitude.Value, branch.Longitude.Value);

        entry.DistanceFromBranchMeters = Math.Round(distance, 1);
        // Tolleranza: include l'accuratezza GPS dichiarata per non penalizzare
        // device imprecisi (evita falsi LocationMismatch).
        var allowed = settings.GeofenceRadiusMeters + (request.GpsAccuracyMeters ?? 0);
        entry.GeofenceOk = distance <= allowed;
    }

    /// <summary>Distanza in metri tra due coordinate (formula di Haversine).</summary>
    private static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6371000; // raggio terrestre medio in metri
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                 + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                   * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private async Task<TimeEntry> ReloadEntryAsync(int id)
    {
        return await _context.TimeEntries
            .Include(e => e.Branch)
            .Include(e => e.Event)
            .Include(e => e.Employee)
            .FirstAsync(e => e.Id == id);
    }

    private async Task<BranchTimeClockSettings> GetOrDefaultSettingsAsync(int branchId, int merchantId)
    {
        return await _context.BranchTimeClockSettings.FirstOrDefaultAsync(s => s.BranchId == branchId)
            ?? new BranchTimeClockSettings { BranchId = branchId, MerchantId = merchantId };
    }

    /// <summary>
    /// Combina data e ora di un turno in un istante "wall-clock" (orario del
    /// merchant, senza fuso). Coerente col resto del codebase, che tratta
    /// DateOnly/TimeOnly come orari locali del merchant senza conversioni UTC.
    /// Va confrontato solo con altri valori wall-clock (vedi <see cref="NowWallClock"/>).
    /// Null se l'ora non è valorizzata.
    /// </summary>
    private static DateTime? ToWallClock(DateOnly date, TimeOnly? time)
        => time.HasValue ? date.ToDateTime(time.Value, DateTimeKind.Unspecified) : null;

    /// <summary>
    /// "Adesso" come orario wall-clock del server. In produzione il server è
    /// configurato sul fuso del servizio; coerente con come gli altri service
    /// del progetto trattano gli orari dei turni.
    /// </summary>
    private static DateTime NowWallClock => DateTime.Now;

    // ── Mapping ────────────────────────────────────────────────────────────

    private static TimeEntryDto MapEntry(TimeEntry e) => new()
    {
        Id = e.Id,
        EmployeeId = e.EmployeeId,
        EmployeeName = e.Employee == null ? string.Empty : $"{e.Employee.FirstName} {e.Employee.LastName}",
        BranchId = e.BranchId,
        BranchName = e.Branch?.Name ?? string.Empty,
        EventId = e.EventId,
        EventParticipantId = e.EventParticipantId,
        EventTitle = e.Event?.Title ?? string.Empty,
        Type = e.Type,
        Source = e.Source,
        Status = e.Status,
        WorkDate = e.WorkDate,
        ActualTimestampUtc = e.ActualTimestampUtc,
        ExpectedTime = e.ExpectedTime,
        DeviationMinutes = e.DeviationMinutes,
        Latitude = e.Latitude,
        Longitude = e.Longitude,
        GpsAccuracyMeters = e.GpsAccuracyMeters,
        DistanceFromBranchMeters = e.DistanceFromBranchMeters,
        GeofenceOk = e.GeofenceOk,
        Notes = e.Notes,
        IsManualCorrection = e.IsManualCorrection,
        CreatedAt = e.CreatedAt
    };

    private static TimeClockShiftDto MapShift(ShiftContext shift)
    {
        var (start, end) = ResolveShiftTimes(shift);
        return new TimeClockShiftDto
        {
            EventId = shift.Event.Id,
            EventParticipantId = shift.Participant.Id,
            Title = shift.Event.Title,
            BranchId = shift.Event.BranchId,
            BranchName = shift.Event.Branch?.Name ?? string.Empty,
            Date = shift.Event.StartDate,
            StartTime = start,
            EndTime = end
        };
    }

    private static TimeClockAnomalyDto MapAnomaly(TimeClockAnomaly a) => new()
    {
        Id = a.Id,
        EmployeeId = a.EmployeeId,
        EmployeeName = a.Employee == null ? string.Empty : $"{a.Employee.FirstName} {a.Employee.LastName}",
        EventId = a.EventId,
        EventParticipantId = a.EventParticipantId,
        EventTitle = a.Event?.Title,
        TimeEntryId = a.TimeEntryId,
        Type = a.Type,
        Status = a.Status,
        Severity = a.Severity,
        WorkDate = a.WorkDate,
        DeviationMinutes = a.DeviationMinutes,
        OvertimeMinutes = a.OvertimeMinutes,
        EmployeeReason = a.EmployeeReason,
        EmployeeNotes = a.EmployeeNotes,
        JustifiedAt = a.JustifiedAt,
        ReviewNotes = a.ReviewNotes,
        ReviewedAt = a.ReviewedAt,
        CreatedAt = a.CreatedAt
    };

    private static BranchTimeClockSettingsDto MapSettings(BranchTimeClockSettings s, MerchantBranch branch) => new()
    {
        BranchId = s.BranchId,
        BranchName = branch.Name,
        IsEnabled = s.IsEnabled,
        ClockingRequired = s.ClockingRequired,
        GraceInMinutes = s.GraceInMinutes,
        GraceOutMinutes = s.GraceOutMinutes,
        EarlyClockInToleranceMinutes = s.EarlyClockInToleranceMinutes,
        LateClockOutToleranceMinutes = s.LateClockOutToleranceMinutes,
        GeofencingEnabled = s.GeofencingEnabled,
        GeofenceRadiusMeters = s.GeofenceRadiusMeters,
        BreakTrackingEnabled = s.BreakTrackingEnabled,
        MaxBreakMinutes = s.MaxBreakMinutes,
        RoundingMinutes = s.RoundingMinutes,
        RequirePhoto = s.RequirePhoto,
        BranchLatitude = branch.Latitude,
        BranchLongitude = branch.Longitude
    };
}
