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
        var employeeRequest = new EmployeeRequest
        {
            EmployeeId = employeeId,
            MerchantId = merchantId,
            Type = request.Type,
            Status = RequestStatus.Pending,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
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
            Notes = r.Notes,
            ReviewNotes = r.ReviewNotes,
            ReviewedAt = r.ReviewedAt,
            CreatedAt = r.CreatedAt,
        };
    }
}
