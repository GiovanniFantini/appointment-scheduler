using System.Security.Claims;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.API.Authorization;

public sealed class SubscriptionAccess(ApplicationDbContext db, IUtcClock clock)
{
    public async Task<SubscriptionAccessDto> ResolveAsync(int merchantId, ClaimsPrincipal user, CancellationToken ct)
    {
        var merchant = await db.Merchants.AsNoTracking().Include(m => m.SubscriptionPlan)
            .SingleOrDefaultAsync(m => m.Id == merchantId, ct);
        var plan = merchant?.SubscriptionPlan;
        var status = merchant is not { IsActive: true, IsApproved: true } ? "Inactive"
            : plan == null ? "Unassigned"
            : merchant.TrialEndsAt <= clock.UtcNow ? "Expired"
            : merchant.TrialEndsAt.HasValue ? "Trial" : "Active";
        var features = status is "Active" or "Trial"
            ? plan!.Features.Where(f => Enum.IsDefined(typeof(MerchantFeature), f))
                .Select(f => ((MerchantFeature)f).ToString()).ToArray()
            : [];
        var levels = new Dictionary<string, string>();
        if (user.IsInRole("Employee"))
        {
            int.TryParse(user.FindFirstValue("EmployeeId"), out var employeeId);
            int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var userId);
            var membership = await db.EmployeeMemberships.AsNoTracking().Include(m => m.Role).ThenInclude(r => r.Features)
                .SingleOrDefaultAsync(m => m.MerchantId == merchantId && m.EmployeeId == employeeId
                    && m.IsActive && m.Employee.IsActive && m.Employee.UserId == userId, ct);
            if (membership == null) status = "Inactive";
            var roleFeatures = membership?.Role.Features.Where(f => f.IsEnabled).ToArray() ?? [];
            features = features.Intersect(roleFeatures.Select(f => f.Feature.ToString())).ToArray();
            levels = roleFeatures.Where(f => features.Contains(f.Feature.ToString()))
                .ToDictionary(f => f.Feature.ToString(), f => (f.AccessLevel ?? FeatureAccessLevel.ReadOnly).ToString());
        }
        else
        {
            levels = features.ToDictionary(f => f, _ => FeatureAccessLevel.Manager.ToString());
        }
        return new(merchantId, plan?.Id, plan?.Name, status, merchant?.TrialEndsAt, features, levels,
            plan?.MaxEmployees, plan?.MaxBranches, plan?.MaxStorageBytes);
    }
}
