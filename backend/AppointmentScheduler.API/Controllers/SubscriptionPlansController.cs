using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AppointmentScheduler.API.Controllers;

[ApiController, Route("api/admin/subscription-plans"), Authorize(Policy = "AdminOnly")]
public sealed class SubscriptionPlansController(ApplicationDbContext db, IUtcClock clock) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await db.SubscriptionPlans.AsNoTracking()
        .OrderBy(p => p.Name).Select(p => new { p.Id, p.Name, p.Description, p.Features, p.MaxEmployees,
            p.MaxBranches, p.MaxStorageBytes, assignedMerchants = db.Merchants.Count(m => m.SubscriptionPlanId == p.Id) }).ToListAsync(ct));

    [HttpPost]
    public Task<IActionResult> Create(SubscriptionPlanRequest request, CancellationToken ct) => Save(null, request, ct);

    [HttpPut("{id:int}")]
    public Task<IActionResult> Update(int id, SubscriptionPlanRequest request, CancellationToken ct) => Save(id, request, ct);

    private async Task<IActionResult> Save(int? id, SubscriptionPlanRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Features == null
            || request.Features.Any(f => !Enum.IsDefined(typeof(MerchantFeature), f)))
            return BadRequest(new { message = "Nome o funzioni non validi." });
        var name = request.Name.Trim();
        if (await db.SubscriptionPlans.AnyAsync(p => p.Name == name && p.Id != id, ct))
            return Conflict(new { message = "Esiste già un pacchetto con questo nome." });
        var plan = id.HasValue ? await db.SubscriptionPlans.FindAsync([id.Value], ct) : new SubscriptionPlan();
        if (plan == null) return NotFound();
        plan.Name = name;
        plan.Description = request.Description?.Trim();
        plan.Features = request.Features.Distinct().Order().ToArray();
        plan.MaxEmployees = request.MaxEmployees;
        plan.MaxBranches = request.MaxBranches;
        plan.MaxStorageBytes = request.MaxStorageBytes;
        plan.UpdatedAt = clock.UtcNow;
        if (!id.HasValue) db.SubscriptionPlans.Add(plan);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Conflict(new { message = "Esiste già un pacchetto con questo nome." });
        }
        return Ok(new { plan.Id });
    }

    [HttpGet("merchants/{merchantId:int}")]
    public async Task<IActionResult> GetAssignment(int merchantId, CancellationToken ct)
    {
        var merchant = await db.Merchants.AsNoTracking().Include(m => m.SubscriptionPlan).SingleOrDefaultAsync(m => m.Id == merchantId, ct);
        if (merchant == null) return NotFound();
        var plan = merchant.SubscriptionPlan;
        return Ok(new { planId = plan?.Id, planName = plan?.Name, merchant.TrialEndsAt,
            status = plan == null ? "Unassigned" : merchant.TrialEndsAt <= clock.UtcNow ? "Expired" : merchant.TrialEndsAt.HasValue ? "Trial" : "Active",
            employees = await db.EmployeeMemberships.CountAsync(m => m.MerchantId == merchantId && m.IsActive && m.Employee.IsActive, ct),
            branches = await db.MerchantBranches.CountAsync(b => b.MerchantId == merchantId && b.IsActive, ct),
            storageBytes = await db.HRDocumentVersions.Where(v => v.HRDocument.TenantId == merchantId && !v.HRDocument.IsDeleted && v.UploadStatus == UploadStatus.Completed).SumAsync(v => (long?)v.FileSizeBytes, ct) ?? 0 });
    }

    [HttpPut("merchants/{merchantId:int}")]
    public async Task<IActionResult> Assign(int merchantId, AssignSubscriptionRequest request, CancellationToken ct)
    {
        var merchant = await db.Merchants.FindAsync([merchantId], ct);
        if (merchant == null || !await db.SubscriptionPlans.AnyAsync(p => p.Id == request.PlanId, ct)) return NotFound();
        merchant.SubscriptionPlanId = request.PlanId;
        merchant.TrialEndsAt = request.TrialDays.HasValue ? clock.UtcNow.AddDays(request.TrialDays.Value) : null;
        merchant.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("merchants/{merchantId:int}/extend-trial")]
    public async Task<IActionResult> Extend(int merchantId, ExtendTrialRequest request, CancellationToken ct)
    {
        var merchant = await db.Merchants.FindAsync([merchantId], ct);
        if (merchant == null) return NotFound();
        if (!merchant.SubscriptionPlanId.HasValue || !merchant.TrialEndsAt.HasValue)
            return BadRequest(new { message = "Il merchant non ha una prova da prorogare." });
        merchant.TrialEndsAt = (merchant.TrialEndsAt > clock.UtcNow ? merchant.TrialEndsAt.Value : clock.UtcNow).AddDays(request.Days);
        merchant.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
