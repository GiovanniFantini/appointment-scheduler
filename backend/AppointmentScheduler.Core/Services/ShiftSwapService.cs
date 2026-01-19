using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio per la gestione delle richieste di scambio turni
/// </summary>
public class ShiftSwapService : IShiftSwapService
{
    private readonly ApplicationDbContext _context;

    public ShiftSwapService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ShiftSwapRequestDto>> GetMerchantSwapRequestsAsync(int merchantId)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference - EF Core handles null navigation properties
        var requests = await _context.ShiftSwapRequests
            .Include(sr => sr.Shift)
                .ThenInclude(s => s.Employee)
            .Include(sr => sr.Shift)
                .ThenInclude(s => s.ShiftTemplate)
            .Include(sr => sr.RequestingEmployee)
            .Include(sr => sr.TargetEmployee)
            .Include(sr => sr.OfferedShift)
                .ThenInclude(s => s.ShiftTemplate)
            .Where(sr => sr.Shift.MerchantId == merchantId)
            .OrderByDescending(sr => sr.CreatedAt)
            .ToListAsync();
#pragma warning restore CS8602

        return requests.Select(MapToDto);
    }

    public async Task<IEnumerable<ShiftSwapRequestDto>> GetEmployeeSwapRequestsAsync(int employeeId)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference - EF Core handles null navigation properties
        var requests = await _context.ShiftSwapRequests
            .Include(sr => sr.Shift)
                .ThenInclude(s => s.Employee)
            .Include(sr => sr.Shift)
                .ThenInclude(s => s.ShiftTemplate)
            .Include(sr => sr.RequestingEmployee)
            .Include(sr => sr.TargetEmployee)
            .Include(sr => sr.OfferedShift)
                .ThenInclude(s => s.ShiftTemplate)
            .Where(sr => sr.RequestingEmployeeId == employeeId)
            .OrderByDescending(sr => sr.CreatedAt)
            .ToListAsync();
#pragma warning restore CS8602

        return requests.Select(MapToDto);
    }

    public async Task<IEnumerable<ShiftSwapRequestDto>> GetEmployeeReceivedSwapRequestsAsync(int employeeId)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference - EF Core handles null navigation properties
        var requests = await _context.ShiftSwapRequests
            .Include(sr => sr.Shift)
                .ThenInclude(s => s.Employee)
            .Include(sr => sr.Shift)
                .ThenInclude(s => s.ShiftTemplate)
            .Include(sr => sr.RequestingEmployee)
            .Include(sr => sr.TargetEmployee)
            .Include(sr => sr.OfferedShift)
                .ThenInclude(s => s.ShiftTemplate)
            .Where(sr => sr.TargetEmployeeId == employeeId || (sr.TargetEmployeeId == null && sr.Shift.MerchantId == employeeId))
            .OrderByDescending(sr => sr.CreatedAt)
            .ToListAsync();
#pragma warning restore CS8602

        return requests.Select(MapToDto);
    }

    public async Task<ShiftSwapRequestDto?> GetSwapRequestByIdAsync(int id)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference - EF Core handles null navigation properties
        var request = await _context.ShiftSwapRequests
            .Include(sr => sr.Shift)
                .ThenInclude(s => s.Employee)
            .Include(sr => sr.Shift)
                .ThenInclude(s => s.ShiftTemplate)
            .Include(sr => sr.RequestingEmployee)
            .Include(sr => sr.TargetEmployee)
            .Include(sr => sr.OfferedShift)
                .ThenInclude(s => s.ShiftTemplate)
            .FirstOrDefaultAsync(sr => sr.Id == id);
#pragma warning restore CS8602

        return request == null ? null : MapToDto(request);
    }

    public async Task<ShiftSwapRequestDto> CreateSwapRequestAsync(int requestingEmployeeId, CreateShiftSwapRequest request)
    {
        var shift = await _context.Shifts
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Id == request.ShiftId);

        if (shift == null)
            throw new InvalidOperationException("Turno non trovato");

        if (shift.EmployeeId != requestingEmployeeId)
            throw new InvalidOperationException("Puoi richiedere lo scambio solo dei tuoi turni");

        var swapRequest = new ShiftSwapRequest
        {
            ShiftId = request.ShiftId,
            RequestingEmployeeId = requestingEmployeeId,
            TargetEmployeeId = request.TargetEmployeeId,
            OfferedShiftId = request.OfferedShiftId,
            Message = request.Message,
            Status = ShiftSwapStatus.Pending,
            RequiresMerchantApproval = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ShiftSwapRequests.Add(swapRequest);
        await _context.SaveChangesAsync();

        await _context.Entry(swapRequest).Reference(sr => sr.Shift).LoadAsync();
        await _context.Entry(swapRequest.Shift).Reference(s => s.Employee).LoadAsync();
        await _context.Entry(swapRequest.Shift).Reference(s => s.ShiftTemplate).LoadAsync();
        await _context.Entry(swapRequest).Reference(sr => sr.RequestingEmployee).LoadAsync();
        await _context.Entry(swapRequest).Reference(sr => sr.TargetEmployee).LoadAsync();
        await _context.Entry(swapRequest).Reference(sr => sr.OfferedShift).LoadAsync();
        if (swapRequest.OfferedShift != null)
            await _context.Entry(swapRequest.OfferedShift).Reference(s => s.ShiftTemplate).LoadAsync();

        return MapToDto(swapRequest);
    }

    public async Task<ShiftSwapRequestDto?> RespondToSwapRequestAsync(int swapRequestId, int respondingUserId, RespondToShiftSwapRequest request)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference - EF Core handles null navigation properties
        var swapRequest = await _context.ShiftSwapRequests
            .Include(sr => sr.Shift)
                .ThenInclude(s => s.Employee)
            .Include(sr => sr.Shift)
                .ThenInclude(s => s.ShiftTemplate)
            .Include(sr => sr.RequestingEmployee)
            .Include(sr => sr.TargetEmployee)
            .Include(sr => sr.OfferedShift)
                .ThenInclude(s => s.ShiftTemplate)
            .FirstOrDefaultAsync(sr => sr.Id == swapRequestId);
#pragma warning restore CS8602

        if (swapRequest == null)
            return null;

        swapRequest.Status = request.Status;
        swapRequest.ResponseMessage = request.ResponseMessage;
        swapRequest.UpdatedAt = DateTime.UtcNow;

        // Se approvato, scambia i turni
        if (request.Status == ShiftSwapStatus.Approved)
        {
            if (swapRequest.TargetEmployeeId.HasValue && swapRequest.OfferedShiftId.HasValue)
            {
                var shift = await _context.Shifts.FindAsync(swapRequest.ShiftId);
                var offeredShift = await _context.Shifts.FindAsync(swapRequest.OfferedShiftId);

                if (shift != null && offeredShift != null)
                {
                    var tempEmployeeId = shift.EmployeeId;
                    shift.EmployeeId = offeredShift.EmployeeId;
                    offeredShift.EmployeeId = tempEmployeeId;

                    shift.UpdatedAt = DateTime.UtcNow;
                    offeredShift.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        await _context.SaveChangesAsync();

        return MapToDto(swapRequest);
    }

    public async Task<bool> CancelSwapRequestAsync(int swapRequestId, int employeeId)
    {
        var swapRequest = await _context.ShiftSwapRequests
            .FirstOrDefaultAsync(sr => sr.Id == swapRequestId && sr.RequestingEmployeeId == employeeId);

        if (swapRequest == null)
            return false;

        swapRequest.Status = ShiftSwapStatus.Cancelled;
        swapRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    private static ShiftSwapRequestDto MapToDto(ShiftSwapRequest swapRequest)
    {
        return new ShiftSwapRequestDto
        {
            Id = swapRequest.Id,
            ShiftId = swapRequest.ShiftId,
            Shift = MapShiftToDto(swapRequest.Shift),
            RequestingEmployeeId = swapRequest.RequestingEmployeeId,
            RequestingEmployeeName = $"{swapRequest.RequestingEmployee.FirstName} {swapRequest.RequestingEmployee.LastName}",
            TargetEmployeeId = swapRequest.TargetEmployeeId,
            TargetEmployeeName = swapRequest.TargetEmployee != null
                ? $"{swapRequest.TargetEmployee.FirstName} {swapRequest.TargetEmployee.LastName}"
                : null,
            OfferedShiftId = swapRequest.OfferedShiftId,
            OfferedShift = swapRequest.OfferedShift != null ? MapShiftToDto(swapRequest.OfferedShift) : null,
            Status = swapRequest.Status,
            StatusName = swapRequest.Status.ToString(),
            Message = swapRequest.Message,
            ResponseMessage = swapRequest.ResponseMessage,
            RequiresMerchantApproval = swapRequest.RequiresMerchantApproval,
            MerchantResponseAt = swapRequest.MerchantResponseAt,
            ApprovedByMerchantId = swapRequest.ApprovedByMerchantId,
            CreatedAt = swapRequest.CreatedAt,
            UpdatedAt = swapRequest.UpdatedAt
        };
    }

    private static ShiftDto MapShiftToDto(Shift shift)
    {
        var totalHours = CalculateTotalHours(shift.StartTime, shift.EndTime, shift.BreakDurationMinutes);

        return new ShiftDto
        {
            Id = shift.Id,
            MerchantId = shift.MerchantId,
            ShiftTemplateId = shift.ShiftTemplateId,
            ShiftTemplateName = shift.ShiftTemplate?.Name,
            EmployeeId = shift.EmployeeId,
            EmployeeName = shift.Employee != null ? $"{shift.Employee.FirstName} {shift.Employee.LastName}" : null,
            Date = shift.Date,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            BreakDurationMinutes = shift.BreakDurationMinutes,
            TotalHours = totalHours,
            ShiftType = shift.ShiftType,
            ShiftTypeName = shift.ShiftType.ToString(),
            Color = shift.Color,
            Notes = shift.Notes,
            IsConfirmed = shift.IsConfirmed,
            IsCheckedIn = shift.IsCheckedIn,
            CheckInTime = shift.CheckInTime,
            IsCheckedOut = shift.IsCheckedOut,
            CheckOutTime = shift.CheckOutTime,
            IsActive = shift.IsActive,
            CreatedAt = shift.CreatedAt,
            UpdatedAt = shift.UpdatedAt
        };
    }

    private static decimal CalculateTotalHours(TimeSpan startTime, TimeSpan endTime, int breakMinutes)
    {
        var duration = endTime - startTime;
        if (duration.TotalMinutes < 0)
            duration = duration.Add(TimeSpan.FromDays(1));

        var workMinutes = duration.TotalMinutes - breakMinutes;
        return (decimal)(workMinutes / 60.0);
    }
}
