using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

public class EmployeeRequestService : IEmployeeRequestService
{
    private readonly ApplicationDbContext _context;

    public EmployeeRequestService(ApplicationDbContext context)
    {
        _context = context;
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
            CreatedAt = DateTime.UtcNow,
        };

        _context.EmployeeRequests.Add(employeeRequest);
        await _context.SaveChangesAsync();

        await _context.Entry(employeeRequest).Reference(r => r.Employee).LoadAsync();

        return MapToDto(employeeRequest);
    }

    public async Task<EmployeeRequestDto?> ApproveAsync(int id, int merchantId, int reviewerUserId, ReviewEmployeeRequestRequest? request = null)
    {
        var employeeRequest = await _context.EmployeeRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id && r.MerchantId == merchantId);

        if (employeeRequest == null)
            return null;

        employeeRequest.Status = RequestStatus.Approved;
        employeeRequest.ReviewedByUserId = reviewerUserId;
        employeeRequest.ReviewedAt = DateTime.UtcNow;
        employeeRequest.ReviewNotes = request?.ReviewNotes;
        employeeRequest.UpdatedAt = DateTime.UtcNow;

        // Merchant può linkare/scollegare il turno in fase di review
        if (request != null)
        {
            var resolved = await ResolveEventLinkAsync(request.EventId, employeeRequest.EmployeeId, merchantId);
            employeeRequest.EventId = resolved;
        }

        await _context.SaveChangesAsync();

        return MapToDto(employeeRequest);
    }

    public async Task<EmployeeRequestDto?> RejectAsync(int id, int merchantId, int reviewerUserId, ReviewEmployeeRequestRequest? request = null)
    {
        var employeeRequest = await _context.EmployeeRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id && r.MerchantId == merchantId);

        if (employeeRequest == null)
            return null;

        employeeRequest.Status = RequestStatus.Rejected;
        employeeRequest.ReviewedByUserId = reviewerUserId;
        employeeRequest.ReviewedAt = DateTime.UtcNow;
        employeeRequest.ReviewNotes = request?.ReviewNotes;
        employeeRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(employeeRequest);
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
    /// Ritorna la coppia (StartTime, EndTime) se entrambi valorizzati e il tipo supporta permessi orari;
    /// altrimenti (null, null).
    /// </summary>
    private static (TimeOnly?, TimeOnly?) NormalizeHourlyRange(EmployeeRequestType type, TimeOnly? start, TimeOnly? end)
    {
        // Only Permessi supports hourly ranges. Ferie/Malattia are always full-day.
        if (type != EmployeeRequestType.Permessi)
            return (null, null);

        if (!start.HasValue || !end.HasValue)
            return (null, null);

        if (end.Value <= start.Value)
            return (null, null);

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
