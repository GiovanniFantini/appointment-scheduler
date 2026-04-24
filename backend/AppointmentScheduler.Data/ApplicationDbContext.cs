using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Core
    public DbSet<User> Users { get; set; }
    public DbSet<Merchant> Merchants { get; set; }
    public DbSet<Employee> Employees { get; set; }

    // Membership & Roles
    public DbSet<EmployeeMembership> EmployeeMemberships { get; set; }
    public DbSet<MerchantRole> MerchantRoles { get; set; }
    public DbSet<RoleFeature> RoleFeatures { get; set; }

    // Events
    public DbSet<Event> Events { get; set; }
    public DbSet<EventParticipant> EventParticipants { get; set; }

    // Notifications
    public DbSet<Notification> Notifications { get; set; }

    // HR Documents (placeholder payroll)
    public DbSet<HRDocument> HRDocuments { get; set; }
    public DbSet<HRDocumentVersion> HRDocumentVersions { get; set; }

    // Employee Requests
    public DbSet<EmployeeRequest> EmployeeRequests { get; set; }

    // Authentication
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.Merchant)
                .WithOne(m => m.User)
                .HasForeignKey<Merchant>(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Employee)
                .WithOne(emp => emp.User)
                .HasForeignKey<Employee>(emp => emp.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Merchant ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.VatNumber).HasMaxLength(20);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.BusinessEmail).HasMaxLength(256);
        });

        // ── Employee ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(e => e.Email);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        });

        // ── EmployeeMembership ────────────────────────────────────────────────
        modelBuilder.Entity<EmployeeMembership>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.Memberships)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.EmployeeMemberships)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.Memberships)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.EmployeeId, e.MerchantId }).IsUnique();
            entity.HasIndex(e => e.MerchantId);
        });

        // ── MerchantRole ──────────────────────────────────────────────────────
        modelBuilder.Entity<MerchantRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.Roles)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.MerchantId);
        });

        // ── RoleFeature ───────────────────────────────────────────────────────
        modelBuilder.Entity<RoleFeature>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.Features)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.RoleId, e.Feature }).IsUnique();
        });

        // ── Event ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.Recurrence).HasMaxLength(500);

            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.Events)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.MerchantId);
            entity.HasIndex(e => new { e.MerchantId, e.StartDate });
            entity.HasIndex(e => e.EventType);
        });

        // ── EventParticipant ──────────────────────────────────────────────────
        modelBuilder.Entity<EventParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Event)
                .WithMany(ev => ev.Participants)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.EventParticipations)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.EventId, e.EmployeeId }).IsUnique();
            entity.HasIndex(e => e.EmployeeId);
        });

        // ── Notification ──────────────────────────────────────────────────────
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
        });

        // ── HRDocument ────────────────────────────────────────────────────────
        modelBuilder.Entity<HRDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne(e => e.Tenant)
                .WithMany(m => m.HRDocuments)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.HRDocuments)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.UpdatedBy)
                .WithMany()
                .HasForeignKey(e => e.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.EmployeeId });
            entity.HasIndex(e => new { e.TenantId, e.DocumentType, e.Year, e.Month });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsDeleted);
        });

        // ── HRDocumentVersion ─────────────────────────────────────────────────
        modelBuilder.Entity<HRDocumentVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BlobPath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FileHash).HasMaxLength(64);
            entity.Property(e => e.ChangeNotes).HasMaxLength(1000);

            entity.HasOne(e => e.HRDocument)
                .WithMany(d => d.Versions)
                .HasForeignKey(e => e.HRDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.UploadedBy)
                .WithMany()
                .HasForeignKey(e => e.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.HRDocumentId, e.VersionNumber }).IsUnique();
            entity.HasIndex(e => e.UploadStatus);
        });

        // ── EmployeeRequest ───────────────────────────────────────────────────
        modelBuilder.Entity<EmployeeRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.ReviewNotes).HasMaxLength(1000);

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.Requests)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.EmployeeRequests)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ReviewedBy)
                .WithMany()
                .HasForeignKey(e => e.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Event)
                .WithMany()
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.MerchantId);
            entity.HasIndex(e => new { e.MerchantId, e.Status });
            entity.HasIndex(e => new { e.EmployeeId, e.MerchantId });
            entity.HasIndex(e => e.EventId);
        });

        // ── PasswordResetToken ────────────────────────────────────────────────
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(128);
            entity.HasIndex(e => e.Token).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);
        });
    }
}
