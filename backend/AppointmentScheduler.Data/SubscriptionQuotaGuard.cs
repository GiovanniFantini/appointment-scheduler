using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Helpers;
using AppointmentScheduler.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Data;

public partial class ApplicationDbContext
{
    private async Task<int> SaveWithQuotaAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken)
    {
        ChangeTracker.DetectChanges();
        var memberships = ChangeTracker.Entries<EmployeeMembership>().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        var branches = ChangeTracker.Entries<MerchantBranch>().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        var versions = ChangeTracker.Entries<HRDocumentVersion>().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        var documentChanges = ChangeTracker.Entries<HRDocument>().Where(e => e.State is EntityState.Added or EntityState.Deleted
            || e.State == EntityState.Modified && e.Property(d => d.IsDeleted).IsModified).ToArray();
        var employees = ChangeTracker.Entries<Employee>().Where(e => e.State == EntityState.Modified && e.Property(x => x.IsActive).IsModified).ToArray();
        var ids = memberships.Select(e => e.Entity.MerchantId).Concat(branches.Select(e => e.Entity.MerchantId)).ToHashSet();
        var employeeIds = employees.Select(e => e.Entity.Id).ToArray();
        if (employeeIds.Length > 0)
            ids.UnionWith(await EmployeeMemberships.Where(m => employeeIds.Contains(m.EmployeeId)).Select(m => m.MerchantId).ToListAsync(cancellationToken));
        var documentIds = versions.Select(e => e.Property(v => v.HRDocumentId).CurrentValue)
            .Concat(documentChanges.Select(e => e.Property(d => d.Id).CurrentValue)).Distinct().ToArray();
        var documents = documentIds.Length > 0
            ? await HRDocuments.IgnoreQueryFilters().AsNoTracking().Where(d => documentIds.Contains(d.Id)).Select(d => new { d.Id, d.TenantId, d.IsDeleted }).ToListAsync(cancellationToken)
            : [];
        ids.UnionWith(documents.Select(d => d.TenantId));
        ids.UnionWith(documentChanges.Select(d => d.Entity.TenantId));
        if (ids.Count == 0) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        // Il lock dura fino al commit: due creazioni concorrenti non possono consumare lo stesso posto residuo.
        await using var transaction = Database.IsRelational() && Database.CurrentTransaction == null
            ? await Database.BeginTransactionAsync(cancellationToken) : null;
        foreach (var merchantId in ids.Where(id => id > 0).Order())
        {
            if (Database.IsNpgsql())
                await Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(71423, {merchantId})", cancellationToken);
            var plan = await Merchants.AsNoTracking().Where(m => m.Id == merchantId).Select(m => m.SubscriptionPlan).SingleOrDefaultAsync(cancellationToken);
            // La registrazione crea la sede iniziale prima che l'admin assegni un pacchetto.
            if (plan == null) continue;

            if (plan.MaxEmployees.HasValue)
            {
                var rows = await EmployeeMemberships.AsNoTracking().Where(m => m.MerchantId == merchantId)
                    .Select(m => new { m.Id, m.EmployeeId, m.IsActive, EmployeeActive = m.Employee.IsActive }).ToListAsync(cancellationToken);
                var before = rows.Count(m => m.IsActive && m.EmployeeActive);
                var after = 0;
                foreach (var row in rows)
                {
                    var change = memberships.FirstOrDefault(e => e.Entity.Id == row.Id);
                    var active = change == null ? row.IsActive : change.State != EntityState.Deleted
                        && (change.Property(m => m.IsActive).IsModified ? change.Entity.IsActive : row.IsActive);
                    var employeeActive = employees.FirstOrDefault(e => e.Entity.Id == row.EmployeeId)?.Entity.IsActive ?? row.EmployeeActive;
                    if (active && employeeActive) after++;
                }
                foreach (var added in memberships.Where(e => e.State == EntityState.Added && e.Entity.MerchantId == merchantId && e.Entity.IsActive))
                {
                    var employee = ChangeTracker.Entries<Employee>().FirstOrDefault(e => e.Entity.Id == added.Entity.EmployeeId)?.Entity;
                    var active = employee?.IsActive ?? await Employees.Where(e => e.Id == added.Entity.EmployeeId).Select(e => e.IsActive).SingleAsync(cancellationToken);
                    if (active) after++;
                }
                CheckIncrease(before, after, plan.MaxEmployees.Value, "dipendenti attivi");
            }
            if (plan.MaxBranches.HasValue)
            {
                var rows = await MerchantBranches.AsNoTracking().Where(b => b.MerchantId == merchantId)
                    .Select(b => new { b.Id, b.IsActive }).ToListAsync(cancellationToken);
                var before = rows.Count(b => b.IsActive);
                // Le OriginalValues possono precedere un altro commit: conta lo stato letto sotto lock.
                var delta = branches.Where(e => e.Entity.MerchantId == merchantId).Sum(e =>
                {
                    var persisted = rows.FirstOrDefault(b => b.Id == e.Entity.Id)?.IsActive ?? false;
                    var active = e.State != EntityState.Deleted && (e.State == EntityState.Added || e.Property(b => b.IsActive).IsModified
                        ? e.Entity.IsActive : persisted);
                    return (active ? 1 : 0) - (persisted ? 1 : 0);
                });
                CheckIncrease(before, before + delta, plan.MaxBranches.Value, "filiali attive");
            }
            if (plan.MaxStorageBytes.HasValue)
            {
                var before = await HRDocumentVersions.Where(v => v.HRDocument.TenantId == merchantId && !v.HRDocument.IsDeleted && v.UploadStatus == UploadStatus.Completed)
                    .SumAsync(v => (long?)v.FileSizeBytes, cancellationToken) ?? 0;
                // Il ripristino deve includere anche le versioni nascoste dai filtri di eliminazione logica.
                var parents = await HRDocuments.IgnoreQueryFilters().AsNoTracking().Where(d => documentIds.Contains(d.Id) && d.TenantId == merchantId)
                    .Select(d => new { d.Id, d.IsDeleted }).ToDictionaryAsync(d => d.Id, d => !d.IsDeleted, cancellationToken);
                var rows = await HRDocumentVersions.IgnoreQueryFilters().AsNoTracking().Where(v => documentIds.Contains(v.HRDocumentId) && v.HRDocument.TenantId == merchantId)
                    .Select(v => new { v.Id, v.HRDocumentId, v.UploadStatus, v.FileSizeBytes }).ToListAsync(cancellationToken);
                var oldSize = rows.Where(v => parents.GetValueOrDefault(v.HRDocumentId) && v.UploadStatus == UploadStatus.Completed).Sum(v => v.FileSizeBytes);
                foreach (var entry in documentChanges.Where(d => d.Entity.TenantId == merchantId))
                    parents[entry.Property(d => d.Id).CurrentValue] = entry.State != EntityState.Deleted && !entry.Entity.IsDeleted;
                var sizes = rows.ToDictionary(v => v.Id, v => (DocumentId: v.HRDocumentId, Status: v.UploadStatus, Size: v.FileSizeBytes));
                foreach (var entry in versions.Where(e => parents.ContainsKey(e.Property(v => v.HRDocumentId).CurrentValue)))
                {
                    var id = entry.Property(v => v.Id).CurrentValue;
                    if (entry.State == EntityState.Deleted) { sizes.Remove(id); continue; }
                    var persisted = sizes.GetValueOrDefault(id);
                    sizes[id] = (entry.Property(v => v.HRDocumentId).CurrentValue,
                        entry.State == EntityState.Added || entry.Property(v => v.UploadStatus).IsModified ? entry.Entity.UploadStatus : persisted.Status,
                        entry.State == EntityState.Added || entry.Property(v => v.FileSizeBytes).IsModified ? entry.Entity.FileSizeBytes : persisted.Size);
                }
                var newSize = sizes.Values.Where(v => parents.GetValueOrDefault(v.DocumentId) && v.Status == UploadStatus.Completed).Sum(v => v.Size);
                CheckIncrease(before, checked(before - oldSize + newSize), plan.MaxStorageBytes.Value, "spazio documenti");
            }
        }
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        if (transaction != null) await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static void CheckIncrease(long before, long after, long limit, string category)
    {
        if (after > before && after > limit)
            throw new SubscriptionLimitException($"Limite del pacchetto raggiunto: {category}. Riduci l’utilizzo o contatta l’amministratore.");
    }
}
