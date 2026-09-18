using System.Text.Json;
using AppointmentScheduler.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AppointmentScheduler.Data;

public partial class ApplicationDbContext
{
    // I testi liberi e i segreti non entrano nel registro, nemmeno aggiungendo nuove proprietà al modello.
    private static readonly HashSet<string> SafeTextFields = new(StringComparer.Ordinal)
    {
        "StartTime", "EndTime", "WorkDate", "StartDate", "EndDate"
    };

    public override int SaveChanges() => SaveChanges(true);
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
        => SaveChangesAsync(acceptAllChangesOnSuccess).GetAwaiter().GetResult();
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(true, cancellationToken);

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries<ActivityEvent>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Il registro attività è di sola aggiunta.");
        var changes = ChangeTracker.Entries().Where(e => e.Entity is not ActivityEvent
            && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => new PendingActivity(e, e.State, e.OriginalValues.Clone(),
                e.Properties.Where(p => e.State != EntityState.Modified || p.IsModified).Select(p => p.Metadata.Name).ToArray(),
                e.Properties.Where(p => e.State != EntityState.Modified || p.IsModified)
                    .ToDictionary(p => p.Metadata.Name, p => new FieldChange(
                        e.State == EntityState.Added ? null : SafeValue(p, p.OriginalValue),
                        e.State == EntityState.Deleted ? null : SafeValue(p, p.CurrentValue)))))
            .ToArray();
        if (changes.Length == 0) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        await using var transaction = Database.IsRelational() && Database.CurrentTransaction == null
            ? await Database.BeginTransactionAsync(cancellationToken) : null;
        var outer = transaction == null && Database.IsRelational() ? Database.CurrentTransaction : null;
        var savepoint = "audit_" + Guid.NewGuid().ToString("N");
        if (outer != null) await outer.CreateSavepointAsync(savepoint, cancellationToken);
        var domainSaved = false;
        try
        {
            var count = await SaveWithQuotaAsync(true, cancellationToken);
            domainSaved = true;
            var events = changes.Select(change =>
            {
                var entry = change.Entry;
                if (change.State == EntityState.Added)
                    foreach (var property in entry.Properties)
                        change.Fields[property.Metadata.Name] = new FieldChange(null, SafeValue(property, property.CurrentValue));
                var item = Activity.Create("change", Activity.Action, "committed");
                item.EntityType = entry.Metadata.ClrType.Name;
                item.EntityId = string.Join(",", entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).Select(p => p.CurrentValue));
                // Il tenant della riga prevale sul contesto, anche per le operazioni amministrative.
                item.MerchantId = entry.Properties.FirstOrDefault(p => p.Metadata.Name is "MerchantId" or "TenantId")?.CurrentValue as int?
                    ?? (entry.Entity is Merchant merchant ? merchant.Id : Activity.MerchantId);
                item.Details = JsonSerializer.Serialize(new { state = change.State.ToString(), fields = change.Fields });
                return item;
            }).ToArray();
            ActivityEvents.AddRange(events);
            await base.SaveChangesAsync(true, cancellationToken);
            if (transaction != null) await transaction.CommitAsync(cancellationToken);
            if (outer != null) await outer.ReleaseSavepointAsync(savepoint, cancellationToken);
            foreach (var item in events) Entry(item).State = EntityState.Detached;
            if (!acceptAllChangesOnSuccess)
            {
                foreach (var change in changes)
                {
                    change.Entry.State = change.State;
                    change.Entry.OriginalValues.SetValues(change.Original);
                    if (change.State == EntityState.Modified)
                        foreach (var property in change.Entry.Properties)
                            property.IsModified = change.Modified.Contains(property.Metadata.Name);
                }
            }
            return count;
        }
        catch
        {
            if (outer != null) await outer.RollbackToSavepointAsync(savepoint, CancellationToken.None);
            // Dopo un rollback le chiavi generate non rappresentano più righe persistite.
            if (domainSaved) ChangeTracker.Clear();
            throw;
        }
    }

    private static object? SafeValue(PropertyEntry property, object? value)
    {
        if (value == null) return null;
        var name = property.Metadata.Name;
        if (name.Contains("Password", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Token", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Secret", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Latitude", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Longitude", StringComparison.OrdinalIgnoreCase)) return "[redacted]";
        var type = Nullable.GetUnderlyingType(property.Metadata.ClrType) ?? property.Metadata.ClrType;
        return type.IsEnum || value is bool or int or long or short or decimal or double or float or DateTime or DateOnly or TimeOnly or Guid
            || SafeTextFields.Contains(name) ? value : "[redacted]";
    }

    private sealed record FieldChange(object? Before, object? After);
    private sealed record PendingActivity(EntityEntry Entry, EntityState State, PropertyValues Original, string[] Modified,
        Dictionary<string, FieldChange> Fields);
}
