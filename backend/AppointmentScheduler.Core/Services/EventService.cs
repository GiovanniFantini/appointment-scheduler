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

    public EventService(ApplicationDbContext context)
    {
        _context = context;
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
            .Where(e => e.MerchantId == merchantId && e.Participants.Any(p => p.EmployeeId == employeeId))
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

        // Add owner participants
        foreach (var empId in request.OwnerEmployeeIds.Distinct())
        {
            evt.Participants.Add(new EventParticipant
            {
                EmployeeId = empId,
                IsOwner = true
            });
        }

        // Add co-owner participants (not already added as owner)
        var ownerIds = new HashSet<int>(request.OwnerEmployeeIds);
        foreach (var empId in request.CoOwnerEmployeeIds.Distinct())
        {
            if (!ownerIds.Contains(empId))
            {
                evt.Participants.Add(new EventParticipant
                {
                    EmployeeId = empId,
                    IsOwner = false
                });
            }
        }

        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(evt).Collection(e => e.Participants).LoadAsync();
        foreach (var p in evt.Participants)
            await _context.Entry(p).Reference(x => x.Employee).LoadAsync();

        return MapToDto(evt);
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

        // Replace participants
        _context.EventParticipants.RemoveRange(evt.Participants);
        evt.Participants.Clear();

        foreach (var empId in request.OwnerEmployeeIds.Distinct())
        {
            evt.Participants.Add(new EventParticipant
            {
                EventId = evt.Id,
                EmployeeId = empId,
                IsOwner = true
            });
        }

        var ownerIds = new HashSet<int>(request.OwnerEmployeeIds);
        foreach (var empId in request.CoOwnerEmployeeIds.Distinct())
        {
            if (!ownerIds.Contains(empId))
            {
                evt.Participants.Add(new EventParticipant
                {
                    EventId = evt.Id,
                    EmployeeId = empId,
                    IsOwner = false
                });
            }
        }

        await _context.SaveChangesAsync();

        // Reload with Employee navigation
        await _context.Entry(evt).Collection(e => e.Participants).LoadAsync();
        foreach (var p in evt.Participants)
            await _context.Entry(p).Reference(x => x.Employee).LoadAsync();

        return MapToDto(evt);
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
                    IsOwner = participant.IsOwner
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

        return cloned.Select(MapToDto).ToList();
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
                IsOwner = p.IsOwner
            }).ToList()
        };
    }
}
