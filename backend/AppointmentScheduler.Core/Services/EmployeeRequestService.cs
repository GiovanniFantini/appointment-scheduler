using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

public class EmployeeRequestService : IEmployeeRequestService
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IUtcClock _clock;

    public EmployeeRequestService(IApplicationDbContext context, INotificationService notificationService, IUtcClock clock)
    {
        _context = context;
        _notificationService = notificationService;
        _clock = clock;
    }

    public async Task<List<EmployeeRequestDto>> GetMerchantRequestsAsync(int merchantId, RequestStatus? status = null)
    {
        var query = _context.EmployeeRequests
            .Include(r => r.Employee)
            .Where(r => r.MerchantId == merchantId);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(MapToDto).ToList();
    }

    public async Task<EmployeeRequestDto?> GetByIdAsync(int id, int merchantId)
    {
        var request = await _context.EmployeeRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id && r.MerchantId == merchantId);

        return request == null ? null : MapToDto(request);
    }

    public async Task<EmployeeRequestDto> CreateAsync(int employeeId, int merchantId, CreateEmployeeRequestRequest request)
    {
        var now = _clock.UtcNow;
        // Hourly leaves: keep StartTime/EndTime only if both provided and request type supports it.
        var (startTime, endTime) = NormalizeHourlyRange(request.Type, request.StartTime, request.EndTime);

        // Validate EventId belongs to the same merchant and the requesting employee is a participant.
        int? eventId = await ResolveEventLinkAsync(request.EventId, employeeId, merchantId);

        var employeeRequest = new EmployeeRequest
        {
            EmployeeId = employeeId,
            MerchantId = merchantId,
            Type = request.Type,
            Status = RequestStatus.Pending,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            StartTime = startTime,
            EndTime = endTime,
            EventId = eventId,
            Notes = request.Notes,
            CreatedAt = now,
        };

        _context.EmployeeRequests.Add(employeeRequest);
        await _context.SaveChangesAsync();

        await _context.Entry(employeeRequest).Reference(r => r.Employee).LoadAsync();

        return MapToDto(employeeRequest);
    }

    public async Task<EmployeeRequestDto?> ApproveAsync(int id, int merchantId, int reviewerUserId, ReviewEmployeeRequestRequest? request = null)
    {
        var now = _clock.UtcNow;
        var employeeRequest = await _context.EmployeeRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id && r.MerchantId == merchantId);

        if (employeeRequest == null)
            return null;

        // Una richiesta già decisa (approvata/rifiutata) non è più modificabile (§9.1).
        if (employeeRequest.Status != RequestStatus.Pending)
            throw new InvalidOperationException("Questa richiesta è già stata decisa e non è più modificabile.");

        employeeRequest.Status = RequestStatus.Approved;
        employeeRequest.ReviewedByUserId = reviewerUserId;
    employeeRequest.ReviewedAt = now;
        employeeRequest.ReviewNotes = request?.ReviewNotes;
    employeeRequest.UpdatedAt = now;

        // Merchant può linkare/scollegare il turno in fase di review
        if (request != null)
        {
            var resolved = await ResolveEventLinkAsync(request.EventId, employeeRequest.EmployeeId, merchantId);
            employeeRequest.EventId = resolved;
        }

        await _context.SaveChangesAsync();

        await NotifyEmployeeAsync(employeeRequest, NotificationType.RequestApproved, "approvata");

        return MapToDto(employeeRequest);
    }

    public async Task<EmployeeRequestDto?> RejectAsync(int id, int merchantId, int reviewerUserId, ReviewEmployeeRequestRequest? request = null)
    {
        var now = _clock.UtcNow;
        var employeeRequest = await _context.EmployeeRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id && r.MerchantId == merchantId);

        if (employeeRequest == null)
            return null;

        // Una richiesta già decisa (approvata/rifiutata) non è più modificabile (§9.1).
        if (employeeRequest.Status != RequestStatus.Pending)
            throw new InvalidOperationException("Questa richiesta è già stata decisa e non è più modificabile.");

        employeeRequest.Status = RequestStatus.Rejected;
        employeeRequest.ReviewedByUserId = reviewerUserId;
    employeeRequest.ReviewedAt = now;
        employeeRequest.ReviewNotes = request?.ReviewNotes;
    employeeRequest.UpdatedAt = now;

        await _context.SaveChangesAsync();

        await NotifyEmployeeAsync(employeeRequest, NotificationType.RequestRejected, "rifiutata");

        return MapToDto(employeeRequest);
    }

    /// <summary>
    /// Crea una notifica per il dipendente sull'esito della review.
    /// No-op se l'employee non è ancora associato a un User (pre-registrazione).
    /// </summary>
    private async Task NotifyEmployeeAsync(EmployeeRequest employeeRequest, NotificationType type, string outcomeLabel)
    {
        if (!employeeRequest.Employee.UserId.HasValue)
            return;

        var typeName = employeeRequest.Type.ToString();
        var dateLabel = employeeRequest.EndDate.HasValue && employeeRequest.EndDate.Value != employeeRequest.StartDate
            ? $"{employeeRequest.StartDate:dd/MM/yyyy} - {employeeRequest.EndDate.Value:dd/MM/yyyy}"
            : $"{employeeRequest.StartDate:dd/MM/yyyy}";

        var title = $"Richiesta {typeName} {outcomeLabel}";
        var message = $"La tua richiesta di {typeName} del {dateLabel} è stata {outcomeLabel}.";
        if (!string.IsNullOrWhiteSpace(employeeRequest.ReviewNotes))
            message += $" Note: {employeeRequest.ReviewNotes}";

        await _notificationService.CreateAsync(
            employeeRequest.Employee.UserId.Value,
            title,
            message,
            type,
            employeeRequest.Id);
    }

    public async Task<List<EmployeeRequestDto>> GetEmployeeRequestsAsync(int employeeId, int merchantId, RequestStatus? status = null)
    {
        var query = _context.EmployeeRequests
            .Include(r => r.Employee)
            .Where(r => r.EmployeeId == employeeId && r.MerchantId == merchantId);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(MapToDto).ToList();
    }

    private static EmployeeRequestDto MapToDto(EmployeeRequest r)
    {
        var fullName = $"{r.Employee.FirstName} {r.Employee.LastName}";
        var initials = $"{r.Employee.FirstName.FirstOrDefault()}{r.Employee.LastName.FirstOrDefault()}".ToUpper();

        return new EmployeeRequestDto
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            EmployeeFullName = fullName,
            EmployeeInitials = initials,
            MerchantId = r.MerchantId,
            Type = r.Type,
            TypeName = r.Type.ToString(),
            Status = r.Status,
            StatusName = r.Status.ToString(),
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            StartTime = r.StartTime,
            EndTime = r.EndTime,
            EventId = r.EventId,
            Notes = r.Notes,
            ReviewNotes = r.ReviewNotes,
            ReviewedAt = r.ReviewedAt,
            CreatedAt = r.CreatedAt,
        };
    }

    /// <summary>
    /// Normalizza la fascia oraria di un permesso. Convenzione: entrambi null = tutto il giorno.
    /// Distingue il caso legittimo "tutto il giorno" (entrambi vuoti) da un input incoerente
    /// (un solo orario fornito, oppure fine ≤ inizio): in quest'ultimo caso lancia
    /// InvalidOperationException invece di scartare gli orari in silenzio.
    /// </summary>
    private static (TimeOnly?, TimeOnly?) NormalizeHourlyRange(EmployeeRequestType type, TimeOnly? start, TimeOnly? end)
    {
        // Solo i Permessi supportano fasce orarie. Ferie/Malattia sono sempre full-day:
        // eventuali orari passati vengono ignorati (non è un errore dell'utente).
        if (type != EmployeeRequestType.Permessi)
            return (null, null);

        // Nessun orario fornito → permesso "tutto il giorno": caso legittimo.
        if (!start.HasValue && !end.HasValue)
            return (null, null);

        // Un solo orario fornito → input incoerente, l'utente ha sbagliato a compilare.
        if (!start.HasValue || !end.HasValue)
            throw new InvalidOperationException(
                "Per un permesso orario indica sia l'ora di inizio sia quella di fine, oppure seleziona 'tutto il giorno'.");

        // Entrambi forniti ma intervallo non valido.
        if (end.Value <= start.Value)
            throw new InvalidOperationException(
                "L'ora di fine del permesso deve essere successiva all'ora di inizio.");

        return (start, end);
    }

    /// <summary>
    /// Valida che l'EventId passato sia un turno del merchant a cui il dipendente partecipa.
    /// Ritorna null se non valido.
    /// </summary>
    private async Task<int?> ResolveEventLinkAsync(int? eventId, int employeeId, int merchantId)
    {
        if (!eventId.HasValue) return null;

        var ok = await _context.Events.AnyAsync(e =>
            e.Id == eventId.Value
            && e.MerchantId == merchantId
            && e.EventType == EventType.Turno
            && e.Participants.Any(p => p.EmployeeId == employeeId));

        return ok ? eventId : null;
    }
}
