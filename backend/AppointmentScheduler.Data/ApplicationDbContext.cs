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
    public DbSet<ShiftTemplate> ShiftTemplates { get; set; }
    public DbSet<Shift> Shifts { get; set; }
    public DbSet<ShiftSwapRequest> ShiftSwapRequests { get; set; }
    public DbSet<EmployeeWorkingHoursLimit> EmployeeWorkingHoursLimits { get; set; }

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

            entity.HasMany(e => e.Employees)
                .WithOne(emp => emp.User)
                .HasForeignKey(emp => emp.UserId)
                .OnDelete(DeleteBehavior.SetNull);
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

        // ShiftTemplate configuration
        modelBuilder.Entity<ShiftTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Color).HasMaxLength(7);

            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.ShiftTemplates)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.MerchantId);
        });

        // Shift configuration
        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Color).HasMaxLength(7);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.Shifts)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ShiftTemplate)
                .WithMany(st => st.Shifts)
                .HasForeignKey(e => e.ShiftTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.Shifts)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.MerchantId);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => new { e.EmployeeId, e.Date });
        });

        // ShiftSwapRequest configuration
        modelBuilder.Entity<ShiftSwapRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.ResponseMessage).HasMaxLength(1000);

            entity.HasOne(e => e.Shift)
                .WithMany(s => s.SwapRequests)
                .HasForeignKey(e => e.ShiftId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RequestingEmployee)
                .WithMany()
                .HasForeignKey(e => e.RequestingEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TargetEmployee)
                .WithMany()
                .HasForeignKey(e => e.TargetEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.OfferedShift)
                .WithMany()
                .HasForeignKey(e => e.OfferedShiftId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ShiftId);
            entity.HasIndex(e => e.RequestingEmployeeId);
            entity.HasIndex(e => e.Status);
        });

        // EmployeeWorkingHoursLimit configuration
        modelBuilder.Entity<EmployeeWorkingHoursLimit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MaxHoursPerDay).HasColumnType("decimal(5,2)");
            entity.Property(e => e.MaxHoursPerWeek).HasColumnType("decimal(5,2)");
            entity.Property(e => e.MaxHoursPerMonth).HasColumnType("decimal(6,2)");
            entity.Property(e => e.MinHoursPerWeek).HasColumnType("decimal(5,2)");
            entity.Property(e => e.MinHoursPerMonth).HasColumnType("decimal(6,2)");
            entity.Property(e => e.MaxOvertimeHoursPerWeek).HasColumnType("decimal(5,2)");
            entity.Property(e => e.MaxOvertimeHoursPerMonth).HasColumnType("decimal(6,2)");

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.WorkingHoursLimits)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.EmployeeWorkingHoursLimits)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => new { e.EmployeeId, e.ValidFrom, e.ValidTo });
        });
    }
}
