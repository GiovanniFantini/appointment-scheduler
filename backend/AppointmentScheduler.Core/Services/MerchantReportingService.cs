using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Core.Services;

public class MerchantReportingService(IApplicationDbContext context, ITimeClockService timeClock) : IMerchantReportingService
{
    public async Task<MerchantReportDto> GetAsync(int merchantId, DateOnly from, DateOnly to, int? branchId, CancellationToken cancellationToken = default)
    {
        if (from > to || to.DayNumber - from.DayNumber > 365)
            throw new ArgumentException("Seleziona un intervallo valido di massimo 366 giorni.");

        if (branchId.HasValue && !await context.MerchantBranches.AsNoTracking()
            .AnyAsync(b => b.Id == branchId && b.MerchantId == merchantId, cancellationToken))
            throw new ArgumentException("Filiale non disponibile.");

        // I turni sono attribuiti al giorno di inizio, anche quando terminano dopo mezzanotte.
        var shifts = await context.Events.AsNoTracking()
            .Where(e => e.MerchantId == merchantId && e.EventType == EventType.Turno
                && e.StartDate >= from && e.StartDate <= to
                && (!branchId.HasValue || e.BranchId == branchId))
            .Select(e => new { e.StartDate, Assignments = e.Participants.Count })
            .ToListAsync(cancellationToken);

        var requests = context.EmployeeRequests.AsNoTracking()
            .Where(r => r.MerchantId == merchantId && r.StartDate <= to && (r.EndDate ?? r.StartDate) >= from);
        // Le richieste senza turno appartengono alla filiale primaria attuale del dipendente.
        if (branchId.HasValue)
            requests = requests.Where(r => r.EventId != null
                ? r.Event != null && r.Event.MerchantId == merchantId && r.Event.BranchId == branchId
                : context.EmployeeMemberships.Any(m => m.MerchantId == merchantId
                    && m.EmployeeId == r.EmployeeId && m.HomeBranchId == branchId));

        var statuses = await requests.Select(r => r.Status).ToListAsync(cancellationToken);
        var byDay = shifts.GroupBy(s => s.StartDate).ToDictionary(g => g.Key);
        var days = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1).Select(offset =>
        {
            var date = from.AddDays(offset);
            byDay.TryGetValue(date, out var items);
            return new MerchantReportDayDto
            {
                Date = date,
                ShiftCount = items?.Count() ?? 0,
                AssignedShiftCount = items?.Sum(s => s.Assignments) ?? 0
            };
        }).ToList();

        return new MerchantReportDto
        {
            From = from,
            To = to,
            ShiftCount = shifts.Count,
            AssignedShiftCount = shifts.Sum(s => s.Assignments),
            PendingRequests = statuses.Count(s => s == RequestStatus.Pending),
            ApprovedRequests = statuses.Count(s => s == RequestStatus.Approved),
            RejectedRequests = statuses.Count(s => s == RequestStatus.Rejected),
            Days = days,
            TimeEntries = await timeClock.GetReportAsync(merchantId, branchId, from, to)
        };
    }
}
