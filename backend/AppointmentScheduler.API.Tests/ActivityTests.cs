using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AppointmentScheduler.API.Controllers;
using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace AppointmentScheduler.API.Tests;

public sealed class ActivityTests
{
    [Fact]
    public async Task Changes_RecordActorKeysAndDiff_WithoutSecrets_AndAreAppendOnly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new ApplicationDbContext(options, new ActivityContext { UserId = 12, MerchantId = 3, Action = "Users.Update" });
        var user = new User { Email = "private@example.test", PasswordHash = "secret-hash" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var created = await db.ActivityEvents.SingleAsync();
        created.UserId.Should().Be(12);
        created.EntityId.Should().Be(user.Id.ToString());
        created.Details.Should().NotContain("private@example.test").And.NotContain("secret-hash");
        user.IsActive = !user.IsActive;
        await db.SaveChangesAsync();
        var updated = await db.ActivityEvents.OrderBy(e => e.Id).LastAsync();
        updated.Details.Should().Contain("IsActive").And.Contain("Modified");
        created.Action = "tampered";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveFalse_PreservesPendingStates_ForCallerAcceptance()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new ApplicationDbContext(options);
        var plan = new SubscriptionPlan { Name = "Test" };
        db.SubscriptionPlans.Add(plan);
        await db.SaveChangesAsync(false);
        db.Entry(plan).State.Should().Be(EntityState.Added);
        db.ChangeTracker.AcceptAllChanges();
        (await db.ActivityEvents.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Collection_ValidatesAndDeduplicates_AndQueriesEnforceTenantAndActor()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new ApplicationDbContext(options);
        db.Merchants.Add(new Merchant { Id = 1, IsApproved = true, CompanyName = "Test", User = new User { Id = 1, Email = "test@example.test" },
            SubscriptionPlan = new SubscriptionPlan { Name = "Test", Features = Enumerable.Range(1, 10).ToArray() } });
        await db.SaveChangesAsync();
        db.EmployeeMemberships.Add(new EmployeeMembership { MerchantId = 1, IsActive = true,
            Employee = new Employee { Id = 1, UserId = 1, Email = "test@example.test" },
            Role = new MerchantRole { MerchantId = 1, Name = "Employee" },
            HomeBranch = new MerchantBranch { MerchantId = 1, Name = "Home" } });
        await db.SaveChangesAsync();
        db.ActivityEvents.AddRange(new ActivityEvent { UserId = 1, MerchantId = 1, Action = "own", Category = "request" },
            new ActivityEvent { UserId = 2, MerchantId = 1, Action = "colleague", Category = "request" },
            new ActivityEvent { UserId = 1, MerchantId = 2, Action = "other-tenant", Category = "request" });
        await db.SaveChangesAsync();
        using var factory = new ApiWebApplicationFactory();
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.AddScoped(sp => new ApplicationDbContext(options, sp.GetRequiredService<ActivityContext>()));
            services.AddAuthentication("Matrix").AddScheme<AuthenticationSchemeOptions, ApiEndpointRegressionTests.MatrixAuth>("Matrix", _ => { });
            services.PostConfigure<AuthenticationOptions>(o => { o.DefaultAuthenticateScheme = "Matrix"; o.DefaultChallengeScheme = "Matrix"; });
        }));
        using var client = app.CreateClient();
        (await client.GetAsync("/api/activity")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var batch = new ActivityBatch { App = "employee", SessionId = Guid.NewGuid(), Events = [new ClientActivity
            { EventId = Guid.NewGuid(), OperationId = Guid.NewGuid(), Action = "ui.click", Target = "event.save", Page = "/calendar" }] };
        (await client.PostAsJsonAsync("/api/activity/collect", batch)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsJsonAsync("/api/activity/collect", batch)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await db.ActivityEvents.CountAsync(e => e.EventId == batch.Events[0].EventId)).Should().Be(1);
        batch.Events[0].Action = "fake.committed";
        (await client.PostAsJsonAsync("/api/activity/collect", batch)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        client.DefaultRequestHeaders.Add("X-Test-Role", "Merchant");
        var merchant = await client.GetFromJsonAsync<JsonElement>("/api/activity");
        merchant.GetProperty("items").EnumerateArray().Should().OnlyContain(e => e.GetProperty("merchantId").GetInt32() == 1);
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Employee");
        var employee = await client.GetFromJsonAsync<JsonElement>("/api/activity?merchantId=2");
        employee.GetProperty("items").GetArrayLength().Should().Be(0);
        var own = await client.GetFromJsonAsync<JsonElement>("/api/activity");
        own.GetProperty("items").EnumerateArray().Should().OnlyContain(e => e.GetProperty("userId").GetInt32() == 1 && e.GetProperty("category").GetString() != "change");
    }

    [LocalPostgresFact]
    public async Task AuditFailure_RollsBackDomainWrite_AndOuterTransaction()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SCHEDULER_TEST_POSTGRES"));
        if (connection.Host is not ("localhost" or "127.0.0.1" or "::1")) throw new InvalidOperationException("Usare un server locale di test.");
        connection.Database = "scheduler_activity_test_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection.ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        try
        {
            await db.Database.MigrateAsync();
            var initial = await db.SubscriptionPlans.CountAsync();
            db.SubscriptionPlans.Add(new SubscriptionPlan { Name = "Persisted" });
            await db.SaveChangesAsync();
            var recorded = await db.ActivityEvents.SingleAsync();
            recorded.EntityId.Should().NotBe("0");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"ActivityEvents\" ADD CONSTRAINT reject_audit CHECK (\"Action\" <> 'reject')");
            db.Activity.Action = "reject";
            db.SubscriptionPlans.Add(new SubscriptionPlan { Name = "Rollback" });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            (await db.SubscriptionPlans.CountAsync()).Should().Be(initial + 1);
            await using (var outer = await db.Database.BeginTransactionAsync())
            {
                db.SubscriptionPlans.Add(new SubscriptionPlan { Name = "Outer rollback" });
                await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
                await outer.CommitAsync();
            }
            (await db.SubscriptionPlans.CountAsync()).Should().Be(initial + 1);
            db.Activity.Action = "allowed";
            await using (var outer = await db.Database.BeginTransactionAsync())
            {
                db.SubscriptionPlans.Add(new SubscriptionPlan { Name = "Caller rollback" });
                await db.SaveChangesAsync();
                await outer.RollbackAsync();
            }
            (await db.SubscriptionPlans.CountAsync()).Should().Be(initial + 1);
            (await db.ActivityEvents.CountAsync()).Should().Be(1);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
