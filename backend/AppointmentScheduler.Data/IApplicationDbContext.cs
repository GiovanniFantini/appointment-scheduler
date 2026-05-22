using AppointmentScheduler.Shared.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Merchant> Merchants { get; }
    DbSet<Employee> Employees { get; }
    DbSet<MerchantBranch> MerchantBranches { get; }
    DbSet<Department> Departments { get; }
    DbSet<EmployeeBranchAccess> EmployeeBranchAccess { get; }
    DbSet<EmployeeMembership> EmployeeMemberships { get; }
    DbSet<MerchantRole> MerchantRoles { get; }
    DbSet<RoleFeature> RoleFeatures { get; }
    DbSet<Event> Events { get; }
    DbSet<EventParticipant> EventParticipants { get; }
    DbSet<EventRequiredSkill> EventRequiredSkills { get; }
    DbSet<Skill> Skills { get; }
    DbSet<EmployeeSkill> EmployeeSkills { get; }
    DbSet<EmployeeRequest> EmployeeRequests { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<InventoryStockBalance> InventoryStockBalances { get; }
    DbSet<InventoryMovement> InventoryMovements { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<PurchaseOrderLine> PurchaseOrderLines { get; }
    DbSet<GoodsReceipt> GoodsReceipts { get; }
    DbSet<GoodsReceiptLine> GoodsReceiptLines { get; }
    DbSet<HRDocument> HRDocuments { get; }
    DbSet<HRDocumentVersion> HRDocumentVersions { get; }
    DbSet<HRDocumentDownload> HRDocumentDownloads { get; }
    DbSet<HRDocumentAcknowledgement> HRDocumentAcknowledgements { get; }
    DbSet<TimeEntry> TimeEntries { get; }
    DbSet<BranchTimeClockSettings> BranchTimeClockSettings { get; }
    DbSet<TimeClockAnomaly> TimeClockAnomalies { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }

    DatabaseFacade Database { get; }
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}