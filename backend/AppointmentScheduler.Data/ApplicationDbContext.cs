using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Merchant> Merchants { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Availability> Availabilities { get; set; }
    public DbSet<AvailabilitySlot> AvailabilitySlots { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<BusinessHours> BusinessHours { get; set; }
    public DbSet<ClosurePeriod> ClosurePeriods { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
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
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Merchant configuration
        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BusinessName).IsRequired().HasMaxLength(200);
        });

        // Service configuration
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.Services)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Booking configuration
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Service)
                .WithMany(s => s.Bookings)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.Bookings)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Availability configuration
        modelBuilder.Entity<Availability>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Service)
                .WithMany(s => s.Availabilities)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ServiceId);
            entity.HasIndex(e => e.DayOfWeek);
            entity.HasIndex(e => e.SpecificDate);
        });

        // AvailabilitySlot configuration
        modelBuilder.Entity<AvailabilitySlot>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Availability)
                .WithMany(a => a.Slots)
                .HasForeignKey(e => e.AvailabilityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.AvailabilityId);
        });

        // Employee configuration
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(e => e.Email);
            entity.Property(e => e.BadgeCode).HasMaxLength(50);

            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.Employees)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.MerchantId);
        });

        // BusinessHours configuration
        modelBuilder.Entity<BusinessHours>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.BusinessHours)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.MerchantId, e.DayOfWeek });
            entity.Property(e => e.DayOfWeek).IsRequired();
        });

        // ClosurePeriod configuration
        modelBuilder.Entity<ClosurePeriod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.ClosurePeriods)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.MerchantId);
            entity.HasIndex(e => new { e.StartDate, e.EndDate });
        });
    }
}
