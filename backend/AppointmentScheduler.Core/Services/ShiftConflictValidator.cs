using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Implementazione del validator. Non solleva eccezioni: ritorna sempre la lista di warning,
/// lasciando al caller la decisione di bloccare o procedere. Per il progetto la policy è "procedi + segnala".
/// </summary>
public class ShiftConflictValidator : IShiftConflictValidator
{
    private readonly ApplicationDbContext _context;

    public ShiftConflictValidator(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<ShiftConflictDto>> DetectAssignmentConflictsAsync(
        int merchantId,
        IReadOnlyList<int> employeeIds,
        DateOnly date,
        TimeOnly? start,
        TimeOnly? end,
        int? excludeEventId = null)
    {
        var result = new List<ShiftConflictDto>();

        if (employeeIds == null || employeeIds.Count == 0)
            return result;

        var ids = employeeIds.Distinct().ToList();

        // Leave conflicts: approved requests (Ferie / Malattia / Permessi) that include this date
        var leaveTypes = new[] { EmployeeRequestType.Ferie, EmployeeRequestType.Malattia, EmployeeRequestType.Permessi };

        var leaves = await _context.EmployeeRequests
            .Include(r => r.Employee)
            .Where(r => r.MerchantId == merchantId
                        && r.Status == RequestStatus.Approved
                        && leaveTypes.Contains(r.Type)
                        && ids.Contains(r.EmployeeId)
                        && r.StartDate <= date
                        && (r.EndDate == null || r.EndDate >= date))
            .ToListAsync();

        foreach (var leave in leaves)
        {
            // Hourly leaves only conflict if they overlap the shift time window
            if (leave.StartTime.HasValue && leave.EndTime.HasValue && start.HasValue && end.HasValue)
            {
                if (!IntervalsOverlap(start.Value, end.Value, leave.StartTime.Value, leave.EndTime.Value))
                    continue;
            }

            result.Add(new ShiftConflictDto
            {
                EmployeeId = leave.EmployeeId,
                EmployeeFullName = FullName(leave.Employee),
                Date = date,
                Kind = ShiftConflictKind.LeaveOverlap,
                RequestId = leave.Id,
                RequestType = leave.Type,
                ConflictStart = leave.StartTime,
                ConflictEnd = leave.EndTime,
                Message = BuildLeaveMessage(leave.Type, leave.StartTime, leave.EndTime)
            });
        }

        // Shift overlap: other events (Turno) with the same date where the employee is participant
        var otherShifts = await _context.Events
            .Include(e => e.Participants)
                .ThenInclude(p => p.Employee)
            .Where(e => e.MerchantId == merchantId
                        && e.EventType == EventType.Turno
                        && e.StartDate == date
                        && (excludeEventId == null || e.Id != excludeEventId.Value)
                        && e.Participants.Any(p => ids.Contains(p.EmployeeId)))
            .ToListAsync();

        foreach (var other in otherShifts)
        {
            var otherStart = other.StartTime;
            var otherEnd = other.EndTime;

            foreach (var participant in other.Participants.Where(p => ids.Contains(p.EmployeeId)))
            {
                var pStart = participant.StartTimeOverride ?? otherStart;
                var pEnd = participant.EndTimeOverride ?? otherEnd;

                // If either side is all-day we treat it as a conflict regardless of times.
                bool overlaps;
                if (!start.HasValue || !end.HasValue || !pStart.HasValue || !pEnd.HasValue)
                    overlaps = true;
                else
                    overlaps = IntervalsOverlap(start.Value, end.Value, pStart.Value, pEnd.Value);

                if (!overlaps)
                    continue;

                result.Add(new ShiftConflictDto
                {
                    EmployeeId = participant.EmployeeId,
                    EmployeeFullName = FullName(participant.Employee),
                    Date = date,
                    Kind = ShiftConflictKind.ShiftOverlap,
                    ConflictingEventId = other.Id,
                    ConflictingEventTitle = other.Title,
                    ConflictStart = pStart,
                    ConflictEnd = pEnd,
                    Message = $"Sovrapposizione con turno \"{other.Title}\""
                        + (pStart.HasValue && pEnd.HasValue ? $" ({pStart:HH\\:mm}-{pEnd:HH\\:mm})" : string.Empty)
                });
            }
        }

        return result;
    }

    private static bool IntervalsOverlap(TimeOnly aStart, TimeOnly aEnd, TimeOnly bStart, TimeOnly bEnd)
    {
        return aStart < bEnd && bStart < aEnd;
    }

    private static string FullName(Shared.Models.Employee? emp)
    {
        if (emp == null) return string.Empty;
        return $"{emp.FirstName} {emp.LastName}".Trim();
    }

    private static string BuildLeaveMessage(EmployeeRequestType type, TimeOnly? start, TimeOnly? end)
    {
        if (start.HasValue && end.HasValue)
            return $"Conflitto con {type} approvato ({start:HH\\:mm}-{end:HH\\:mm})";
        return $"Conflitto con {type} approvato (tutto il giorno)";
    }
}
