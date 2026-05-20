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
    public async Task<List<EventDto>> GetMerchantEventsAsync(int merchantId, DateOnly? from, DateOnly? to, EventType? type,
        int? branchId = null, int? departmentId = null)
    {
        var query = _context.Events
            .Include(e => e.Participants)
                .ThenInclude(p => p.Employee)
            .Include(e => e.Participants)
                .ThenInclude(p => p.Skill)
            .Include(e => e.Participants)
                .ThenInclude(p => p.Department)
            .Include(e => e.RequiredSkills)
                .ThenInclude(rs => rs.Skill)
            .Include(e => e.Branch)
            .Include(e => e.Department)
            .Where(e => e.MerchantId == merchantId)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(e => e.StartDate >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.StartDate <= to.Value || (e.EndDate.HasValue && e.EndDate.Value <= to.Value));

        if (type.HasValue)
            query = query.Where(e => e.EventType == type.Value);

        // Filtro filiale: gli eventi AppliesToAllBranches compaiono in qualsiasi filiale.
        if (branchId.HasValue)
            query = query.Where(e => e.BranchId == branchId.Value || e.AppliesToAllBranches);

        // Filtro reparto: gli eventi che valgono per tutte le filiali (es. chiusure
        // aziendali) restano sempre visibili anche quando si filtra per reparto.
        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId.Value || e.AppliesToAllBranches);

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
            .Include(e => e.Participants)
                .ThenInclude(p => p.Skill)
            .Include(e => e.Participants)
                .ThenInclude(p => p.Department)
            .Include(e => e.RequiredSkills)
                .ThenInclude(rs => rs.Skill)
            .Include(e => e.Branch)
            .Include(e => e.Department)
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
            .Include(e => e.Participants)
                .ThenInclude(p => p.Skill)
            .Include(e => e.Participants)
                .ThenInclude(p => p.Department)
            .Include(e => e.RequiredSkills)
                .ThenInclude(rs => rs.Skill)
            .Include(e => e.Branch)
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id && e.MerchantId == merchantId);

        return evt == null ? null : MapToDto(evt);
    }

    /// <summary>
    /// Crea un nuovo evento con i partecipanti specificati
    /// </summary>
    public async Task<EventDto> CreateAsync(int merchantId, int createdByUserId, CreateEventRequest request)
    {
        var branchId = await ResolveBranchIdAsync(merchantId, request.BranchId);
        await ValidateDepartmentAsync(branchId, request.DepartmentId);
        await ValidateParticipantDepartmentsAsync(branchId, request.ParticipantOverrides);

        var evt = new Event
        {
            MerchantId = merchantId,
            BranchId = branchId,
            DepartmentId = request.DepartmentId,
            AppliesToAllBranches = request.AppliesToAllBranches,
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
        var skillMap = BuildParticipantSkillMap(request.ParticipantSkills);

        // Add owner participants
        foreach (var empId in request.OwnerEmployeeIds.Distinct())
        {
            evt.Participants.Add(BuildParticipant(empId, true, overrideMap, skillMap));
        }

        // Add co-owner participants (not already added as owner)
        var ownerIds = new HashSet<int>(request.OwnerEmployeeIds);
        foreach (var empId in request.CoOwnerEmployeeIds.Distinct())
        {
            if (!ownerIds.Contains(empId))
            {
                evt.Participants.Add(BuildParticipant(empId, false, overrideMap, skillMap));
            }
        }

        // Fabbisogno mansioni
        foreach (var rs in request.RequiredSkills.Where(r => r.Quantity > 0).DistinctBy(r => r.SkillId))
        {
            evt.RequiredSkills.Add(new EventRequiredSkill
            {
                SkillId = rs.SkillId,
                Quantity = rs.Quantity
            });
        }

        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        await ReloadEventNavigationAsync(evt);

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
            .Include(e => e.RequiredSkills)
            .FirstOrDefaultAsync(e => e.Id == id && e.MerchantId == merchantId);

        if (evt == null)
            return null;

        var branchId = await ResolveBranchIdAsync(merchantId, request.BranchId);
        await ValidateDepartmentAsync(branchId, request.DepartmentId);
        await ValidateParticipantDepartmentsAsync(branchId, request.ParticipantOverrides);

        evt.BranchId = branchId;
        evt.DepartmentId = request.DepartmentId;
        evt.AppliesToAllBranches = request.AppliesToAllBranches;
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
        var skillMap = BuildParticipantSkillMap(request.ParticipantSkills);

        // Replace participants
        _context.EventParticipants.RemoveRange(evt.Participants);
        evt.Participants.Clear();

        foreach (var empId in request.OwnerEmployeeIds.Distinct())
        {
            var p = BuildParticipant(empId, true, overrideMap, skillMap);
            p.EventId = evt.Id;
            evt.Participants.Add(p);
        }

        var ownerIds = new HashSet<int>(request.OwnerEmployeeIds);
        foreach (var empId in request.CoOwnerEmployeeIds.Distinct())
        {
            if (!ownerIds.Contains(empId))
            {
                var p = BuildParticipant(empId, false, overrideMap, skillMap);
                p.EventId = evt.Id;
                evt.Participants.Add(p);
            }
        }

        // Replace RequiredSkills
        _context.EventRequiredSkills.RemoveRange(evt.RequiredSkills);
        evt.RequiredSkills.Clear();
        foreach (var rs in request.RequiredSkills.Where(r => r.Quantity > 0).DistinctBy(r => r.SkillId))
        {
            evt.RequiredSkills.Add(new EventRequiredSkill
            {
                EventId = evt.Id,
                SkillId = rs.SkillId,
                Quantity = rs.Quantity
            });
        }

        await _context.SaveChangesAsync();

        await ReloadEventNavigationAsync(evt);

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
            .Include(e => e.Participants)
                .ThenInclude(p => p.Skill)
            .Include(e => e.Participants)
                .ThenInclude(p => p.Department)
            .Include(e => e.RequiredSkills)
                .ThenInclude(rs => rs.Skill)
            .Include(e => e.Branch)
            .Include(e => e.Department)
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
                BranchId = original.BranchId,
                DepartmentId = original.DepartmentId,
                AppliesToAllBranches = original.AppliesToAllBranches,
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
                    ParticipantNotes = participant.ParticipantNotes,
                    SkillId = participant.SkillId,
                    DepartmentId = participant.DepartmentId
                });
            }
            foreach (var rs in original.RequiredSkills)
            {
                clone.RequiredSkills.Add(new EventRequiredSkill
                {
                    SkillId = rs.SkillId,
                    Quantity = rs.Quantity
                });
            }

            cloned.Add(clone);
            current = current.AddDays(1);
        }

        _context.Events.AddRange(cloned);
        await _context.SaveChangesAsync();

        foreach (var clone in cloned)
            await ReloadEventNavigationAsync(clone);

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
            .Include(e => e.Participants)
                .ThenInclude(p => p.Skill)
            .Include(e => e.Participants)
                .ThenInclude(p => p.Department)
            .Include(e => e.RequiredSkills)
                .ThenInclude(rs => rs.Skill)
            .Include(e => e.Branch)
            .Include(e => e.Department)
            .Where(e => e.MerchantId == merchantId
                        && e.StartDate >= sourceStart
                        && e.StartDate <= sourceEnd);

        if (request.EmployeeFilter != null && request.EmployeeFilter.Count > 0)
        {
            var filter = request.EmployeeFilter;
            sourceQuery = sourceQuery.Where(e => e.Participants.Any(p => filter.Contains(p.EmployeeId)));
        }

        // Filtra per filiale sorgente se specificato.
        if (request.SourceBranchId.HasValue)
        {
            var srcBranch = request.SourceBranchId.Value;
            sourceQuery = sourceQuery.Where(e => e.BranchId == srcBranch);
        }

        var sources = await sourceQuery.ToListAsync();
        if (sources.Count == 0)
            return new List<EventDto>();

        // Se è richiesta una filiale di destinazione, verifica che appartenga al merchant.
        int? targetBranchId = null;
        if (request.TargetBranchId.HasValue)
        {
            targetBranchId = await ResolveBranchIdAsync(merchantId, request.TargetBranchId.Value);
        }

        var cloned = new List<Event>();

        for (int w = 0; w < request.NumberOfWeeks; w++)
        {
            foreach (var src in sources)
            {
                var dayOffset = src.StartDate.DayNumber - sourceStart.DayNumber;
                var targetDate = request.TargetWeekStart.AddDays(7 * w + dayOffset);

                // Filiale del clone: target esplicita o quella dell'originale.
                // Se la filiale cambia, il reparto (figlio della filiale) non è più valido → null.
                var cloneBranchId = targetBranchId ?? src.BranchId;
                var cloneDepartmentId = cloneBranchId == src.BranchId ? src.DepartmentId : null;

                var clone = new Event
                {
                    MerchantId = src.MerchantId,
                    BranchId = cloneBranchId,
                    DepartmentId = cloneDepartmentId,
                    AppliesToAllBranches = src.AppliesToAllBranches,
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
                        ParticipantNotes = p.ParticipantNotes,
                        SkillId = p.SkillId,
                        // Se la filiale del clone cambia, il reparto del partecipante non è più valido.
                        DepartmentId = cloneBranchId == src.BranchId ? p.DepartmentId : null
                    });
                }
                foreach (var rs in src.RequiredSkills)
                {
                    clone.RequiredSkills.Add(new EventRequiredSkill
                    {
                        SkillId = rs.SkillId,
                        Quantity = rs.Quantity
                    });
                }

                cloned.Add(clone);
            }
        }

        _context.Events.AddRange(cloned);
        await _context.SaveChangesAsync();

        foreach (var clone in cloned)
            await ReloadEventNavigationAsync(clone);

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

    private static EventParticipant BuildParticipant(
        int employeeId,
        bool isOwner,
        IReadOnlyDictionary<int, ParticipantOverride> overrideMap,
        IReadOnlyDictionary<int, int?> skillMap)
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
            p.DepartmentId = ov.DepartmentId;
        }
        if (skillMap.TryGetValue(employeeId, out var skillId))
        {
            p.SkillId = skillId;
        }
        return p;
    }

    private static Dictionary<int, int?> BuildParticipantSkillMap(IEnumerable<ParticipantSkillAssignment>? assignments)
    {
        var map = new Dictionary<int, int?>();
        if (assignments == null) return map;
        foreach (var a in assignments)
        {
            map[a.EmployeeId] = a.SkillId;
        }
        return map;
    }

    private async Task ReloadEventNavigationAsync(Event evt)
    {
        await _context.Entry(evt).Reference(e => e.Branch).LoadAsync();
        if (evt.DepartmentId.HasValue)
            await _context.Entry(evt).Reference(e => e.Department).LoadAsync();
        await _context.Entry(evt).Collection(e => e.Participants).LoadAsync();
        foreach (var p in evt.Participants)
        {
            await _context.Entry(p).Reference(x => x.Employee).LoadAsync();
            if (p.SkillId.HasValue)
                await _context.Entry(p).Reference(x => x.Skill).LoadAsync();
            if (p.DepartmentId.HasValue)
                await _context.Entry(p).Reference(x => x.Department).LoadAsync();
        }
        await _context.Entry(evt).Collection(e => e.RequiredSkills).LoadAsync();
        foreach (var rs in evt.RequiredSkills)
            await _context.Entry(rs).Reference(x => x.Skill).LoadAsync();
    }

    /// <summary>
    /// Risolve la filiale di un evento: se branchId è 0/non valido, ricade sulla HQ del
    /// merchant (caso mono-sede). Verifica sempre che la filiale appartenga al merchant.
    /// </summary>
    private async Task<int> ResolveBranchIdAsync(int merchantId, int branchId)
    {
        if (branchId > 0)
        {
            var ok = await _context.MerchantBranches
                .AnyAsync(b => b.Id == branchId && b.MerchantId == merchantId);
            if (ok)
                return branchId;
            throw new InvalidOperationException("Filiale non valida per questo merchant.");
        }

        // Fallback: HQ (o, se manca, la prima filiale del merchant).
        var hq = await _context.MerchantBranches
            .Where(b => b.MerchantId == merchantId)
            .OrderByDescending(b => b.IsHeadquarters)
            .ThenBy(b => b.Id)
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync();

        if (hq == null)
            throw new InvalidOperationException("Il merchant non ha filiali configurate.");

        return hq.Value;
    }

    /// <summary>
    /// Verifica che il reparto (se valorizzato) appartenga alla filiale dell'evento.
    /// </summary>
    private async Task ValidateDepartmentAsync(int branchId, int? departmentId)
    {
        if (!departmentId.HasValue)
            return;

        var ok = await _context.Departments
            .AnyAsync(d => d.Id == departmentId.Value && d.BranchId == branchId);
        if (!ok)
            throw new InvalidOperationException("Il reparto selezionato non appartiene alla filiale dell'evento.");
    }

    /// <summary>
    /// Verifica che tutti i reparti assegnati ai partecipanti (caso Jolly) appartengano
    /// alla filiale dell'evento. Previene assegnazioni cross-filiale di reparti.
    /// </summary>
    private async Task ValidateParticipantDepartmentsAsync(int branchId, IEnumerable<ParticipantOverride>? overrides)
    {
        var ids = overrides?
            .Where(o => o.DepartmentId.HasValue)
            .Select(o => o.DepartmentId!.Value)
            .Distinct()
            .ToList();
        if (ids == null || ids.Count == 0)
            return;

        var validCount = await _context.Departments
            .CountAsync(d => ids.Contains(d.Id) && d.BranchId == branchId);
        if (validCount != ids.Count)
            throw new InvalidOperationException("Un reparto assegnato a un partecipante non appartiene alla filiale dell'evento.");
    }

    private static CoverageStatus CalculateCoverage(Event evt, out Dictionary<int, int> coveredBySkill)
    {
        var covered = new Dictionary<int, int>();
        coveredBySkill = covered;

        if (evt.RequiredSkills.Count == 0)
            return CoverageStatus.None;

        foreach (var p in evt.Participants.Where(x => x.SkillId.HasValue))
        {
            var sid = p.SkillId!.Value;
            covered[sid] = covered.GetValueOrDefault(sid) + 1;
        }

        if (evt.Participants.Count == 0)
            return CoverageStatus.Empty;

        var allCovered = evt.RequiredSkills.All(rs => covered.GetValueOrDefault(rs.SkillId) >= rs.Quantity);
        if (allCovered)
            return CoverageStatus.Covered;

        var anyCovered = evt.RequiredSkills.Any(rs => covered.GetValueOrDefault(rs.SkillId) > 0);
        return anyCovered ? CoverageStatus.Partial : CoverageStatus.Empty;
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
                evt.MerchantId, ids, evt.StartDate, group.Key.Start, group.Key.End,
                excludeEventId: evt.Id, branchId: evt.BranchId);
            all.AddRange(conflicts);
        }

        all.AddRange(await DetectSkillMismatchesAsync(evt));

        return all;
    }

    /// <summary>
    /// Verifica che ogni partecipante assegnato a una mansione la possieda effettivamente in EmployeeSkills.
    /// </summary>
    private async Task<List<ShiftConflictDto>> DetectSkillMismatchesAsync(Event evt)
    {
        var result = new List<ShiftConflictDto>();
        var assigned = evt.Participants.Where(p => p.SkillId.HasValue).ToList();
        if (assigned.Count == 0) return result;

        var employeeIds = assigned.Select(p => p.EmployeeId).Distinct().ToList();
        // Carichiamo le righe (employeeId, skillId) e raggruppiamo in memoria: più safe
        // rispetto a GroupBy lato EF, e il payload è limitato dalla cardinalità dei partecipanti.
        var rows = await _context.EmployeeSkills
            .Where(es => employeeIds.Contains(es.EmployeeId))
            .Select(es => new { es.EmployeeId, es.SkillId })
            .ToListAsync();
        var ownedByEmployee = rows
            .GroupBy(r => r.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.SkillId).ToHashSet());

        foreach (var p in assigned)
        {
            var sid = p.SkillId!.Value;
            var owned = ownedByEmployee.GetValueOrDefault(p.EmployeeId);
            if (owned != null && owned.Contains(sid)) continue;

            var skillName = p.Skill?.Name
                ?? evt.RequiredSkills.FirstOrDefault(rs => rs.SkillId == sid)?.Skill?.Name
                ?? "mansione";

            var fullName = p.Employee != null
                ? $"{p.Employee.FirstName} {p.Employee.LastName}".Trim()
                : string.Empty;

            result.Add(new ShiftConflictDto
            {
                EmployeeId = p.EmployeeId,
                EmployeeFullName = fullName,
                Date = evt.StartDate,
                Kind = ShiftConflictKind.SkillMismatch,
                SkillId = sid,
                SkillName = skillName,
                Message = $"{(string.IsNullOrEmpty(fullName) ? "Il dipendente" : fullName)} è assegnato come {skillName} ma non possiede la mansione."
            });
        }

        return result;
    }

    private static EventDto MapToDto(Event evt)
    {
        var coverage = CalculateCoverage(evt, out var coveredBySkillOut);
        var coveredBySkill = coveredBySkillOut;

        return new EventDto
        {
            Id = evt.Id,
            MerchantId = evt.MerchantId,
            BranchId = evt.BranchId,
            BranchName = evt.Branch?.Name ?? string.Empty,
            DepartmentId = evt.DepartmentId,
            DepartmentName = evt.Department?.Name,
            DepartmentColor = evt.Department?.Color,
            AppliesToAllBranches = evt.AppliesToAllBranches,
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
            CoverageStatus = coverage,
            Participants = evt.Participants.Select(p => new EventParticipantDto
            {
                EmployeeId = p.EmployeeId,
                FullName = p.Employee != null
                    ? $"{p.Employee.FirstName} {p.Employee.LastName}"
                    : string.Empty,
                IsOwner = p.IsOwner,
                StartTimeOverride = p.StartTimeOverride,
                EndTimeOverride = p.EndTimeOverride,
                ParticipantNotes = p.ParticipantNotes,
                SkillId = p.SkillId,
                SkillName = p.Skill?.Name,
                SkillColor = p.Skill?.Color,
                DepartmentId = p.DepartmentId,
                DepartmentName = p.Department?.Name,
                DepartmentColor = p.Department?.Color
            }).ToList(),
            RequiredSkills = evt.RequiredSkills.Select(rs => new EventRequiredSkillDto
            {
                SkillId = rs.SkillId,
                SkillName = rs.Skill?.Name ?? string.Empty,
                SkillColor = rs.Skill?.Color ?? "#3b82f6",
                Quantity = rs.Quantity,
                CoveredQuantity = coveredBySkill.GetValueOrDefault(rs.SkillId)
            }).ToList()
        };
    }
}
