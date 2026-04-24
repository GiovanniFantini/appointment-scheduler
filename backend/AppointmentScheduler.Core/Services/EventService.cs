using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio per la gestione degli eventi aziendali
/// </summary>
public class EventService : IEventService
{
    private readonly ApplicationDbContext _context;
    private readonly IShiftConflictValidator _conflictValidator;

    public EventService(ApplicationDbContext context, IShiftConflictValidator conflictValidator)
    {
        _context = context;
        _conflictValidator = conflictValidator;
    }

    /// <summary>
    /// Recupera tutti gli eventi di un merchant con filtri opzionali
    /// </summary>
    public async Task<List<EventDto>> GetMerchantEventsAsync(int merchantId, DateOnly? from, DateOnly? to, EventType? type)
    {
        var query = _context.Events
            .Include(e => e.Participants)
                .ThenInclude(p => p.Employee)
            .Where(e => e.MerchantId == merchantId)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(e => e.StartDate >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.StartDate <= to.Value || (e.EndDate.HasValue && e.EndDate.Value <= to.Value));

        if (type.HasValue)
            query = query.Where(e => e.EventType == type.Value);

        var events = await query
            .OrderBy(e => e.StartDate)
            .ThenBy(e => e.StartTime)
            .ToListAsync();

        return events.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Recupera gli eventi di un dipendente in un merchant specifico
    /// </summary>
    public async Task<List<EventDto>> GetEmployeeEventsAsync(int employeeId, int merchantId, DateOnly? from, DateOnly? to)
    {
        var query = _context.Events
            .Include(e => e.Participants)
                .ThenInclude(p => p.Employee)
            .Where(e => e.MerchantId == merchantId &&
                        (e.EventType == EventType.ChiusuraAziendale ||
                         e.Participants.Any(p => p.EmployeeId == employeeId)))
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(e => e.StartDate >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.StartDate <= to.Value || (e.EndDate.HasValue && e.EndDate.Value <= to.Value));

        var events = await query
            .OrderBy(e => e.StartDate)
            .ThenBy(e => e.StartTime)
            .ToListAsync();

        return events.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Recupera un evento per ID, verificando l'appartenenza al merchant
    /// </summary>
    public async Task<EventDto?> GetByIdAsync(int id, int merchantId)
    {
        var evt = await _context.Events
            .Include(e => e.Participants)
                .ThenInclude(p => p.Employee)
            .FirstOrDefaultAsync(e => e.Id == id && e.MerchantId == merchantId);

        return evt == null ? null : MapToDto(evt);
    }

    /// <summary>
    /// Crea un nuovo evento con i partecipanti specificati
    /// </summary>
    public async Task<EventDto> CreateAsync(int merchantId, int createdByUserId, CreateEventRequest request)
    {
        var evt = new Event
        {
            MerchantId = merchantId,
            Title = request.Title,
            EventType = request.EventType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsAllDay = request.IsAllDay,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsOnCall = request.IsOnCall,
            Recurrence = request.Recurrence,
            NotificationEnabled = request.NotificationEnabled,
            Notes = request.Notes,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        var overrideMap = BuildOverrideMap(request.ParticipantOverrides);

        // Add owner participants
        foreach (var empId in request.OwnerEmployeeIds.Distinct())
        {
            evt.Participants.Add(BuildParticipant(empId, true, overrideMap));
        }

        // Add co-owner participants (not already added as owner)
        var ownerIds = new HashSet<int>(request.OwnerEmployeeIds);
        foreach (var empId in request.CoOwnerEmployeeIds.Distinct())
        {
            if (!ownerIds.Contains(empId))
            {
                evt.Participants.Add(BuildParticipant(empId, false, overrideMap));
            }
        }

        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(evt).Collection(e => e.Participants).LoadAsync();
        foreach (var p in evt.Participants)
            await _context.Entry(p).Reference(x => x.Employee).LoadAsync();

        var dto = MapToDto(evt);
        if (evt.EventType == EventType.Turno)
            dto.Warnings = await DetectConflictsForEventAsync(evt);
        return dto;
    }

    /// <summary>
    /// Aggiorna un evento esistente
    /// </summary>
    public async Task<EventDto?> UpdateAsync(int id, int merchantId, UpdateEventRequest request)
    {
        var evt = await _context.Events
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == id && e.MerchantId == merchantId);

        if (evt == null)
            return null;

        evt.Title = request.Title;
        evt.EventType = request.EventType;
        evt.StartDate = request.StartDate;
        evt.EndDate = request.EndDate;
        evt.IsAllDay = request.IsAllDay;
        evt.StartTime = request.StartTime;
        evt.EndTime = request.EndTime;
        evt.IsOnCall = request.IsOnCall;
        evt.Recurrence = request.Recurrence;
        evt.NotificationEnabled = request.NotificationEnabled;
        evt.Notes = request.Notes;
        evt.UpdatedAt = DateTime.UtcNow;

        var overrideMap = BuildOverrideMap(request.ParticipantOverrides);

        // Replace participants
        _context.EventParticipants.RemoveRange(evt.Participants);
        evt.Participants.Clear();

        foreach (var empId in request.OwnerEmployeeIds.Distinct())
        {
            var p = BuildParticipant(empId, true, overrideMap);
            p.EventId = evt.Id;
            evt.Participants.Add(p);
        }

        var ownerIds = new HashSet<int>(request.OwnerEmployeeIds);
        foreach (var empId in request.CoOwnerEmployeeIds.Distinct())
        {
            if (!ownerIds.Contains(empId))
            {
                var p = BuildParticipant(empId, false, overrideMap);
                p.EventId = evt.Id;
                evt.Participants.Add(p);
            }
        }

        await _context.SaveChangesAsync();

        // Reload with Employee navigation
        await _context.Entry(evt).Collection(e => e.Participants).LoadAsync();
        foreach (var p in evt.Participants)
            await _context.Entry(p).Reference(x => x.Employee).LoadAsync();

        var dto = MapToDto(evt);
        if (evt.EventType == EventType.Turno)
            dto.Warnings = await DetectConflictsForEventAsync(evt);
        return dto;
    }

    /// <summary>
    /// Elimina un evento
    /// </summary>
    public async Task<bool> DeleteAsync(int id, int merchantId)
    {
        var evt = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id && e.MerchantId == merchantId);

        if (evt == null)
            return false;

        _context.Events.Remove(evt);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Clona un evento in ogni giorno dell'intervallo specificato.
    /// L'offset temporale tra l'evento originale e fromDate viene applicato a ogni giorno nel range.
    /// </summary>
    public async Task<List<EventDto>> CloneAsync(int id, int merchantId, CloneEventRequest request)
    {
        var original = await _context.Events
            .Include(e => e.Participants)
                .ThenInclude(p => p.Employee)
            .FirstOrDefaultAsync(e => e.Id == id && e.MerchantId == merchantId);

        if (original == null)
            return new List<EventDto>();

        var cloned = new List<Event>();
        var current = request.FromDate;

        while (current <= request.ToDate)
        {
            var clone = new Event
            {
                MerchantId = original.MerchantId,
                Title = original.Title,
                EventType = original.EventType,
                StartDate = current,
                EndDate = original.EndDate.HasValue
                    ? current.AddDays((original.EndDate.Value.DayNumber - original.StartDate.DayNumber))
                    : null,
                IsAllDay = original.IsAllDay,
                StartTime = original.StartTime,
                EndTime = original.EndTime,
                IsOnCall = original.IsOnCall,
                Recurrence = null, // Clones are single occurrences
                NotificationEnabled = original.NotificationEnabled,
                Notes = original.Notes,
                CreatedByUserId = original.CreatedByUserId,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var participant in original.Participants)
            {
                clone.Participants.Add(new EventParticipant
                {
                    EmployeeId = participant.EmployeeId,
                    IsOwner = participant.IsOwner,
                    StartTimeOverride = participant.StartTimeOverride,
                    EndTimeOverride = participant.EndTimeOverride,
                    ParticipantNotes = participant.ParticipantNotes
                });
            }

            cloned.Add(clone);
            current = current.AddDays(1);
        }

        _context.Events.AddRange(cloned);
        await _context.SaveChangesAsync();

        // Reload participants with Employee for all clones
        foreach (var clone in cloned)
        {
            await _context.Entry(clone).Collection(e => e.Participants).LoadAsync();
            foreach (var p in clone.Participants)
                await _context.Entry(p).Reference(x => x.Employee).LoadAsync();
        }

        var results = new List<EventDto>();
        foreach (var clone in cloned)
        {
            var dto = MapToDto(clone);
            if (clone.EventType == EventType.Turno)
                dto.Warnings = await DetectConflictsForEventAsync(clone);
            results.Add(dto);
        }
        return results;
    }

    /// <inheritdoc />
    public async Task<List<EventDto>> CloneWeekAsync(int merchantId, int createdByUserId, CloneWeekRequest request)
    {
        if (request.NumberOfWeeks < 1)
            return new List<EventDto>();

        var sourceStart = request.SourceWeekStart;
        var sourceEnd = sourceStart.AddDays(6);

        var sourceQuery = _context.Events
            .Include(e => e.Participants)
                .ThenInclude(p => p.Employee)
            .Where(e => e.MerchantId == merchantId
                        && e.StartDate >= sourceStart
                        && e.StartDate <= sourceEnd);

        if (request.EmployeeFilter != null && request.EmployeeFilter.Count > 0)
        {
            var filter = request.EmployeeFilter;
            sourceQuery = sourceQuery.Where(e => e.Participants.Any(p => filter.Contains(p.EmployeeId)));
        }

        var sources = await sourceQuery.ToListAsync();
        if (sources.Count == 0)
            return new List<EventDto>();

        var cloned = new List<Event>();

        for (int w = 0; w < request.NumberOfWeeks; w++)
        {
            foreach (var src in sources)
            {
                var dayOffset = src.StartDate.DayNumber - sourceStart.DayNumber;
                var targetDate = request.TargetWeekStart.AddDays(7 * w + dayOffset);

                var clone = new Event
                {
                    MerchantId = src.MerchantId,
                    Title = src.Title,
                    EventType = src.EventType,
                    StartDate = targetDate,
                    EndDate = src.EndDate.HasValue
                        ? targetDate.AddDays(src.EndDate.Value.DayNumber - src.StartDate.DayNumber)
                        : null,
                    IsAllDay = src.IsAllDay,
                    StartTime = src.StartTime,
                    EndTime = src.EndTime,
                    IsOnCall = src.IsOnCall,
                    Recurrence = null,
                    NotificationEnabled = src.NotificationEnabled,
                    Notes = src.Notes,
                    CreatedByUserId = createdByUserId,
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var p in src.Participants)
                {
                    clone.Participants.Add(new EventParticipant
                    {
                        EmployeeId = p.EmployeeId,
                        IsOwner = p.IsOwner,
                        StartTimeOverride = p.StartTimeOverride,
                        EndTimeOverride = p.EndTimeOverride,
                        ParticipantNotes = p.ParticipantNotes
                    });
                }

                cloned.Add(clone);
            }
        }

        _context.Events.AddRange(cloned);
        await _context.SaveChangesAsync();

        foreach (var clone in cloned)
        {
            await _context.Entry(clone).Collection(e => e.Participants).LoadAsync();
            foreach (var p in clone.Participants)
                await _context.Entry(p).Reference(x => x.Employee).LoadAsync();
        }

        var results = new List<EventDto>();
        foreach (var clone in cloned)
        {
            var dto = MapToDto(clone);
            if (clone.EventType == EventType.Turno)
                dto.Warnings = await DetectConflictsForEventAsync(clone);
            results.Add(dto);
        }
        return results;
    }

    /// <inheritdoc />
    public async Task<List<EffectiveShiftDto>> GetEffectiveScheduleAsync(int employeeId, int merchantId, DateOnly from, DateOnly to)
    {
        if (from > to)
            return new List<EffectiveShiftDto>();

        // 1. Load shifts where this employee is a participant, in range.
        var shifts = await _context.Events
            .Include(e => e.Participants)
            .Where(e => e.MerchantId == merchantId
                        && e.EventType == EventType.Turno
                        && e.StartDate >= from
                        && e.StartDate <= to
                        && e.Participants.Any(p => p.EmployeeId == employeeId))
            .OrderBy(e => e.StartDate)
            .ThenBy(e => e.StartTime)
            .ToListAsync();

        // 2. Load approved leaves for this employee overlapping the range.
        var leaveTypes = new[] { EmployeeRequestType.Ferie, EmployeeRequestType.Malattia, EmployeeRequestType.Permessi };
        var leaves = await _context.EmployeeRequests
            .Where(r => r.MerchantId == merchantId
                        && r.EmployeeId == employeeId
                        && r.Status == RequestStatus.Approved
                        && leaveTypes.Contains(r.Type)
                        && r.StartDate <= to
                        && (r.EndDate ?? r.StartDate) >= from)
            .ToListAsync();

        var result = new List<EffectiveShiftDto>();

        foreach (var shift in shifts)
        {
            var participant = shift.Participants.First(p => p.EmployeeId == employeeId);
            var canonicalStart = participant.StartTimeOverride ?? shift.StartTime;
            var canonicalEnd = participant.EndTimeOverride ?? shift.EndTime;

            var applicableLeaves = leaves.Where(l =>
                    (l.EventId == null || l.EventId == shift.Id)
                    && l.StartDate <= shift.StartDate
                    && (l.EndDate ?? l.StartDate) >= shift.StartDate)
                .ToList();

            List<PresenceSegment> segments;
            if (shift.IsAllDay || !canonicalStart.HasValue || !canonicalEnd.HasValue)
            {
                // All-day shift: any full-day leave absorbs the whole day; hourly leaves don't apply.
                var absorbs = applicableLeaves.Any(l => !l.StartTime.HasValue || !l.EndTime.HasValue);
                segments = absorbs
                    ? new List<PresenceSegment>()
                    : new List<PresenceSegment> { new PresenceSegment { From = null, To = null } };
            }
            else
            {
                segments = SubtractLeaves(canonicalStart.Value, canonicalEnd.Value, applicableLeaves);
            }

            result.Add(new EffectiveShiftDto
            {
                EventId = shift.Id,
                EmployeeId = employeeId,
                Title = shift.Title,
                Date = shift.StartDate,
                IsAllDay = shift.IsAllDay,
                CanonicalStart = canonicalStart,
                CanonicalEnd = canonicalEnd,
                IsOnCall = shift.IsOnCall,
                Segments = segments,
                IsFullyAbsent = segments.Count == 0,
                AppliedLeaves = applicableLeaves.Select(l => new AppliedLeaveDto
                {
                    RequestId = l.Id,
                    Type = l.Type,
                    IsFullDay = !l.StartTime.HasValue || !l.EndTime.HasValue,
                    StartTime = l.StartTime,
                    EndTime = l.EndTime,
                    Notes = l.Notes
                }).ToList()
            });
        }

        return result;
    }

    /// <summary>
    /// Sottrae gli intervalli dei permessi dalla fascia [start,end] e restituisce i segmenti di presenza residui.
    /// </summary>
    private static List<PresenceSegment> SubtractLeaves(TimeOnly start, TimeOnly end, IReadOnlyList<Shared.Models.EmployeeRequest> leaves)
    {
        // A full-day leave absorbs the whole shift.
        if (leaves.Any(l => !l.StartTime.HasValue || !l.EndTime.HasValue))
            return new List<PresenceSegment>();

        // Start with a single segment [start,end], subtract each hourly leave in order.
        var segments = new List<(TimeOnly From, TimeOnly To)> { (start, end) };

        foreach (var leave in leaves.OrderBy(l => l.StartTime!.Value))
        {
            var lStart = leave.StartTime!.Value;
            var lEnd = leave.EndTime!.Value;
            var next = new List<(TimeOnly From, TimeOnly To)>();
            foreach (var seg in segments)
            {
                // No overlap → keep as-is.
                if (lEnd <= seg.From || lStart >= seg.To)
                {
                    next.Add(seg);
                    continue;
                }
                // Leave covers whole segment → drop.
                if (lStart <= seg.From && lEnd >= seg.To)
                {
                    continue;
                }
                // Leave cuts the start.
                if (lStart <= seg.From && lEnd < seg.To)
                {
                    next.Add((lEnd, seg.To));
                    continue;
                }
                // Leave cuts the end.
                if (lStart > seg.From && lEnd >= seg.To)
                {
                    next.Add((seg.From, lStart));
                    continue;
                }
                // Leave in the middle → splits segment.
                next.Add((seg.From, lStart));
                next.Add((lEnd, seg.To));
            }
            segments = next;
        }

        return segments.Select(s => new PresenceSegment { From = s.From, To = s.To }).ToList();
    }

    /// <summary>
    /// Costruisce una mappa EmployeeId → override, scartando duplicati (primo prevale).
    /// </summary>
    private static Dictionary<int, ParticipantOverride> BuildOverrideMap(IEnumerable<ParticipantOverride>? overrides)
    {
        var map = new Dictionary<int, ParticipantOverride>();
        if (overrides == null) return map;
        foreach (var o in overrides)
        {
            if (!map.ContainsKey(o.EmployeeId))
                map[o.EmployeeId] = o;
        }
        return map;
    }

    private static EventParticipant BuildParticipant(int employeeId, bool isOwner, IReadOnlyDictionary<int, ParticipantOverride> overrideMap)
    {
        var p = new EventParticipant
        {
            EmployeeId = employeeId,
            IsOwner = isOwner
        };
        if (overrideMap.TryGetValue(employeeId, out var ov))
        {
            p.StartTimeOverride = ov.StartTimeOverride;
            p.EndTimeOverride = ov.EndTimeOverride;
            p.ParticipantNotes = ov.ParticipantNotes;
        }
        return p;
    }

    /// <summary>
    /// Rileva i conflitti (non bloccanti) per tutti i partecipanti del turno appena creato/aggiornato/clonato.
    /// </summary>
    private async Task<List<ShiftConflictDto>> DetectConflictsForEventAsync(Event evt)
    {
        var all = new List<ShiftConflictDto>();
        if (evt.EventType != EventType.Turno) return all;

        // Group participants by effective (override-aware) time window so the validator
        // checks the right window for each one.
        var groups = evt.Participants
            .GroupBy(p => (
                Start: p.StartTimeOverride ?? evt.StartTime,
                End: p.EndTimeOverride ?? evt.EndTime));

        foreach (var group in groups)
        {
            var ids = group.Select(p => p.EmployeeId).ToList();
            var conflicts = await _conflictValidator.DetectAssignmentConflictsAsync(
                evt.MerchantId, ids, evt.StartDate, group.Key.Start, group.Key.End, excludeEventId: evt.Id);
            all.AddRange(conflicts);
        }

        return all;
    }

    private static EventDto MapToDto(Event evt)
    {
        return new EventDto
        {
            Id = evt.Id,
            MerchantId = evt.MerchantId,
            Title = evt.Title,
            EventType = evt.EventType,
            StartDate = evt.StartDate,
            EndDate = evt.EndDate,
            IsAllDay = evt.IsAllDay,
            StartTime = evt.StartTime,
            EndTime = evt.EndTime,
            IsOnCall = evt.IsOnCall,
            Recurrence = evt.Recurrence,
            NotificationEnabled = evt.NotificationEnabled,
            Notes = evt.Notes,
            CreatedAt = evt.CreatedAt,
            Participants = evt.Participants.Select(p => new EventParticipantDto
            {
                EmployeeId = p.EmployeeId,
                FullName = p.Employee != null
                    ? $"{p.Employee.FirstName} {p.Employee.LastName}"
                    : string.Empty,
                IsOwner = p.IsOwner,
                StartTimeOverride = p.StartTimeOverride,
                EndTimeOverride = p.EndTimeOverride,
                ParticipantNotes = p.ParticipantNotes
            }).ToList()
        };
    }
}
