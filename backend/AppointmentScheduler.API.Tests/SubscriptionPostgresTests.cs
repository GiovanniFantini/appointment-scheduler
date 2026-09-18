using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.Helpers;
using AppointmentScheduler.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit.Abstractions;

namespace AppointmentScheduler.API.Tests;

public sealed class LocalPostgresFactAttribute : FactAttribute
{
    public LocalPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SCHEDULER_TEST_POSTGRES")))
            Skip = "Impostare SCHEDULER_TEST_POSTGRES su un PostgreSQL locale di test.";
    }
}

public sealed class SubscriptionPostgresTests(ITestOutputHelper output)
{
    [LocalPostgresFact]
    public async Task MigrationAndConcurrentQuotas_OnRealPostgres()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SCHEDULER_TEST_POSTGRES"));
        if (connection.Host is not ("localhost" or "127.0.0.1" or "::1"))
            throw new InvalidOperationException("Il test accetta solo un server locale.");
        // Il nome casuale impedisce di usare o cancellare un database preesistente.
        connection.Database = "scheduler_test_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection.ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        try
        {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20260728144359_AddClockingRequiredSince");
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO "Users" ("Id", "Email", "PasswordHash", "FirstName", "LastName", "AccountType", "IsActive", "CreatedAt")
                VALUES (100, 'migration@example.test', 'test', 'Test', 'Migration', 2, TRUE, NOW());
                INSERT INTO "Merchants" ("Id", "UserId", "CompanyName", "IsApproved", "IsActive", "CreatedAt")
                VALUES (100, 100, 'Preesistente', TRUE, TRUE, NOW());
                """);
            await migrator.MigrateAsync();
            var merchant = await db.Merchants.Include(m => m.SubscriptionPlan).SingleAsync();
            merchant.SubscriptionPlan!.Name.Should().Be("Completo");
            merchant.SubscriptionPlan.Features.Should().Equal(Enumerable.Range(1, 10));
            merchant.SubscriptionPlan.MaxEmployees.Should().BeNull();
            merchant.SubscriptionPlan.MaxBranches.Should().BeNull();
            merchant.SubscriptionPlan.MaxStorageBytes.Should().BeNull();
            merchant.TrialEndsAt.Should().BeNull();
            await migrator.MigrateAsync();
            (await db.SubscriptionPlans.CountAsync()).Should().Be(1);
            var script = migrator.GenerateScript("20260728144359_AddClockingRequiredSince", options: MigrationsSqlGenerationOptions.Idempotent);
            await db.Database.ExecuteSqlRawAsync(script);
            (await db.SubscriptionPlans.CountAsync()).Should().Be(1);
            var newMerchant = new Merchant { CompanyName = "Nuovo", User = new User { Email = "new@example.test" } };
            db.Merchants.Add(newMerchant);
            await db.SaveChangesAsync();
            newMerchant.SubscriptionPlanId.Should().BeNull();
            output.WriteLine("PASS migration completa, backfill Completo, riesecuzione e SQL idempotente, nuovi merchant non assegnati.");

            var branch = new MerchantBranch { MerchantId = 100, Name = "Sede" };
            var role = new MerchantRole { MerchantId = 100, Name = "Ruolo" };
            var employee = new Employee { Email = "initial@example.test", FirstName = "Test", LastName = "Initial" };
            db.EmployeeMemberships.Add(new EmployeeMembership { MerchantId = 100, Employee = employee, Role = role, HomeBranch = branch });
            await db.SaveChangesAsync();
            var plan = merchant.SubscriptionPlan;
            plan.MaxBranches = 2;
            plan.MaxEmployees = 2;
            plan.MaxStorageBytes = 100;
            await db.SaveChangesAsync();

            await Race(options, context => context.MerchantBranches.Add(new MerchantBranch { MerchantId = 100, Name = Guid.NewGuid().ToString() }));
            (await db.MerchantBranches.CountAsync(b => b.MerchantId == 100 && b.IsActive)).Should().Be(2);
            output.WriteLine("PASS lock PostgreSQL osservato e quota filiali: un solo inserimento accettato su due concorrenti.");

            await Race(options, context => context.EmployeeMemberships.Add(new EmployeeMembership { MerchantId = 100, RoleId = role.Id,
                HomeBranchId = branch.Id, Employee = new Employee { Email = Guid.NewGuid() + "@example.test", FirstName = "Test", LastName = "Concurrent" } }));
            (await db.EmployeeMemberships.CountAsync(m => m.MerchantId == 100 && m.IsActive && m.Employee.IsActive)).Should().Be(2);
            (await db.Employees.CountAsync()).Should().Be(2);
            output.WriteLine("PASS quota dipendenti concorrenti e rollback dell'anagrafica rifiutata.");

            await Race(options, context => context.HRDocuments.Add(Document(employee.Id, 60)));
            (await db.HRDocumentVersions.SumAsync(v => v.FileSizeBytes)).Should().Be(60);
            (await db.HRDocuments.CountAsync()).Should().Be(1);
            output.WriteLine("PASS quota documenti concorrenti, con documento e versione nuovi nello stesso salvataggio.");

            db.ChangeTracker.Clear();
            var document = await db.HRDocuments.SingleAsync();
            document.IsDeleted = true;
            await db.SaveChangesAsync();
            db.HRDocuments.Add(Document(employee.Id, 100));
            await db.SaveChangesAsync();
            document.IsDeleted = false;
            await Assert.ThrowsAsync<SubscriptionLimitException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            plan = await db.SubscriptionPlans.SingleAsync();
            plan.MaxBranches = 1;
            await db.SaveChangesAsync();
            (await db.MerchantBranches.FirstAsync(b => b.MerchantId == 100)).Name = "Modifica oltre quota";
            await db.SaveChangesAsync();
            db.MerchantBranches.Add(new MerchantBranch { MerchantId = 100, Name = "Oltre quota" });
            await Assert.ThrowsAsync<SubscriptionLimitException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            output.WriteLine("PASS ripristino documenti respinto oltre quota; downgrade conserva dati e consente modifiche.");

            await migrator.MigrateAsync("20260728144359_AddClockingRequiredSince");
            await migrator.MigrateAsync();
            (await db.Merchants.CountAsync()).Should().Be(2);
            (await db.Merchants.CountAsync(m => m.SubscriptionPlanId != null)).Should().Be(2);
            (await db.HRDocuments.IgnoreQueryFilters().CountAsync()).Should().Be(2);
            output.WriteLine("PASS rollback e riapplicazione della migration conservano i dati operativi.");
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static HRDocument Document(int employeeId, long bytes) => new()
    {
        TenantId = 100, EmployeeId = employeeId, CreatedByUserId = 100, Title = "Test quota",
        Versions = [new HRDocumentVersion { VersionNumber = 1, UploadedByUserId = 100,
            UploadStatus = UploadStatus.Completed, FileSizeBytes = bytes, BlobPath = Guid.NewGuid().ToString(), FileName = "test.pdf", ContentType = "application/pdf" }]
    };

    private static async Task Race(DbContextOptions<ApplicationDbContext> options, Action<ApplicationDbContext> prepare)
    {
        await using var blocker = new ApplicationDbContext(options);
        await using var transaction = await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(71423, 100)");
        async Task<bool> Write()
        {
            await using var worker = new ApplicationDbContext(options);
            prepare(worker);
            try { await worker.SaveChangesAsync(); return true; }
            catch (SubscriptionLimitException) { return false; }
        }
        var attempts = new[] { Write(), Write() };
        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            long waiting;
            do
            {
                waiting = await blocker.Database.SqlQueryRaw<long>("""
                    SELECT COUNT(*) AS "Value" FROM pg_locks
                    WHERE locktype = 'advisory' AND classid = 71423 AND objid = 100 AND NOT granted
                    """).SingleAsync();
                if (waiting == 2) break;
                await Task.Delay(50);
            } while (DateTime.UtcNow < deadline && attempts.All(t => !t.IsCompleted));
            waiting.Should().Be(2, "entrambe le scritture devono attendere il lock PostgreSQL");
        }
        finally
        {
            await transaction.CommitAsync();
            var results = await Task.WhenAll(attempts).WaitAsync(TimeSpan.FromSeconds(20));
            results.Count(success => success).Should().Be(1);
        }
    }
}
