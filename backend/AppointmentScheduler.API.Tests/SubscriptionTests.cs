using System.Security.Claims;
using AppointmentScheduler.API.Authorization;
using AppointmentScheduler.API.Controllers;
using AppointmentScheduler.API.Middleware;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.Helpers;
using AppointmentScheduler.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AppointmentScheduler.API.Tests.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Encodings.Web;

namespace AppointmentScheduler.API.Tests;

public sealed class SubscriptionTests
{
    public sealed class SubscriptionTestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            => Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(Principal(), Scheme.Name)));
    }
    private static readonly DateTime Now = new(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);
    private static IUtcClock Clock()
    {
        var clock = new Mock<IUtcClock>();
        clock.SetupGet(c => c.UtcNow).Returns(Now);
        return clock.Object;
    }

    private static ClaimsPrincipal Principal(string role = "Employee", int employeeId = 1)
        => new(new ClaimsIdentity([
            new(ClaimTypes.Role, role), new(ClaimTypes.NameIdentifier, "123"),
            new("MerchantId", "1"), new("EmployeeId", employeeId.ToString()),
            new("Feature", "Magazzino"), new("FeatureLevel", "Magazzino:Manager")
        ], "test"));

    private static async Task<ApplicationDbContext> Database()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var plan = new SubscriptionPlan { Id = 1, Name = "Calendario", Features = [1], MaxEmployees = 1, MaxBranches = 1, MaxStorageBytes = 100 };
        var merchant = new Merchant { Id = 1, UserId = 123, CompanyName = "Azienda", IsApproved = true, SubscriptionPlan = plan };
        var employee = new Employee { Id = 1, UserId = 123, FirstName = "Ada", LastName = "Rossi", Email = "ada@example.test" };
        var branch = new MerchantBranch { Id = 1, Merchant = merchant, Name = "Sede" };
        var role = new MerchantRole { Id = 1, Merchant = merchant, Name = "Responsabile", Features = [
            new RoleFeature { Feature = MerchantFeature.Calendario, IsEnabled = true, AccessLevel = FeatureAccessLevel.Operator },
            new RoleFeature { Feature = MerchantFeature.Magazzino, IsEnabled = true, AccessLevel = FeatureAccessLevel.Manager }] };
        db.Merchants.Add(merchant);
        db.MerchantBranches.Add(branch);
        db.EmployeeMemberships.Add(new EmployeeMembership { Id = 1, Merchant = merchant, Employee = employee, Role = role, HomeBranch = branch });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return db;
    }

    [Fact]
    public async Task Access_IsIntersectionOfPlanAndRole_AndRefreshesAfterPlanChange()
    {
        await using var db = await Database();
        var resolver = new SubscriptionAccess(db, Clock());
        var state = await resolver.ResolveAsync(1, Principal(), default);
        state.ActiveFeatures.Should().Equal("Calendario");
        state.FeatureLevels["Calendario"].Should().Be("Operator");
        var plan = await db.SubscriptionPlans.SingleAsync();
        plan.Features = [10];
        await db.SaveChangesAsync();
        var refreshed = await resolver.ResolveAsync(1, Principal(), default);
        refreshed.ActiveFeatures.Should().Equal("Magazzino");
        (await resolver.ResolveAsync(1, Principal("Merchant"), default)).FeatureLevels["Magazzino"].Should().Be("Manager");
    }

    [Fact]
    public async Task Employee_CannotUseAnotherCompanyWithoutMembership()
    {
        await using var db = await Database();
        db.Merchants.Add(new Merchant { Id = 2, CompanyName = "Altra", IsApproved = true, SubscriptionPlanId = 1 });
        await db.SaveChangesAsync();
        var state = await new SubscriptionAccess(db, Clock()).ResolveAsync(2, Principal(), default);
        state.Status.Should().Be("Inactive");
        state.ActiveFeatures.Should().BeEmpty();
    }

    [Fact]
    public async Task ExpiredTrial_BlocksAtExactDeadline_AndKeepsData()
    {
        await using var db = await Database();
        (await db.Merchants.SingleAsync()).TrialEndsAt = Now;
        await db.SaveChangesAsync();
        var state = await new SubscriptionAccess(db, Clock()).ResolveAsync(1, Principal(), default);
        state.Status.Should().Be("Expired");
        state.ActiveFeatures.Should().BeEmpty();
        (await db.EmployeeMemberships.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task NoAssignment_DoesNotGrantCompletePlan()
    {
        await using var db = await Database();
        (await db.Merchants.SingleAsync()).SubscriptionPlanId = null;
        await db.SaveChangesAsync();
        var state = await new SubscriptionAccess(db, Clock()).ResolveAsync(1, Principal("Merchant"), default);
        state.Status.Should().Be("Unassigned");
        state.ActiveFeatures.Should().BeEmpty();
    }

    [Fact]
    public async Task Middleware_RejectsDirectApiWithOldToken_AndAllowsPurchasedFeature()
    {
        await using var db = await Database();
        var called = false;
        var middleware = new SubscriptionMiddleware(_ => { called = true; return Task.CompletedTask; });
        var http = new DefaultHttpContext { User = Principal("Merchant") };
        http.Response.Body = new MemoryStream();
        http.Request.Path = "/api/employee/inventory/items";
        http.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new RequiresPlanFeatureAttribute(MerchantFeature.Magazzino)), "test"));
        await middleware.InvokeAsync(http, new SubscriptionAccess(db, Clock()));
        http.Response.StatusCode.Should().Be(403);
        called.Should().BeFalse();
        http.User.HasFeature(MerchantFeature.Magazzino).Should().BeFalse();
        http.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new RequiresPlanFeatureAttribute(MerchantFeature.Calendario)), "test"));
        await middleware.InvokeAsync(http, new SubscriptionAccess(db, Clock()));
        called.Should().BeTrue();
    }

    [Fact]
    public async Task Trial_AssignExtendAndConvert_PreservesExistingDeadlineOnExtension()
    {
        await using var db = await Database();
        var controller = new SubscriptionPlansController(db, Clock());
        (await controller.Assign(1, new AssignSubscriptionRequest { PlanId = 1, TrialDays = 14 }, default)).Should().BeOfType<NoContentResult>();
        (await db.Merchants.SingleAsync()).TrialEndsAt.Should().Be(Now.AddDays(14));
        await controller.Extend(1, new ExtendTrialRequest { Days = 7 }, default);
        (await db.Merchants.SingleAsync()).TrialEndsAt.Should().Be(Now.AddDays(21));
        (await db.Merchants.SingleAsync()).TrialEndsAt = Now.AddDays(-3);
        await db.SaveChangesAsync();
        await controller.Extend(1, new ExtendTrialRequest { Days = 7 }, default);
        (await db.Merchants.SingleAsync()).TrialEndsAt.Should().Be(Now.AddDays(7));
        await controller.Assign(1, new AssignSubscriptionRequest { PlanId = 1 }, default);
        (await db.Merchants.SingleAsync()).TrialEndsAt.Should().BeNull();
    }

    [Fact]
    public async Task EmployeeQuota_BlocksCreationButAllowsEditsAndDeactivationAboveLimit()
    {
        await using var db = await Database();
        var employee = new Employee { Id = 2, FirstName = "B", LastName = "R", Email = "b@example.test" };
        db.EmployeeMemberships.Add(new EmployeeMembership { Employee = employee, MerchantId = 1, RoleId = 1, HomeBranchId = 1 });
        await Assert.ThrowsAsync<SubscriptionLimitException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        (await db.SubscriptionPlans.SingleAsync()).MaxEmployees = 0;
        await db.SaveChangesAsync();
        var membership = await db.EmployeeMemberships.SingleAsync();
        membership.JoinedAt = Now;
        await db.SaveChangesAsync();
        membership.IsActive = false;
        await db.SaveChangesAsync();
        membership.IsActive = true;
        await Assert.ThrowsAsync<SubscriptionLimitException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task GlobalEmployeeReactivation_IsAlsoLimited()
    {
        await using var db = await Database();
        var employee = await db.Employees.SingleAsync();
        employee.IsActive = false;
        await db.SaveChangesAsync();
        (await db.SubscriptionPlans.SingleAsync()).MaxEmployees = 0;
        await db.SaveChangesAsync();
        employee.IsActive = true;
        await Assert.ThrowsAsync<SubscriptionLimitException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task BranchQuota_AppliesToReactivation_AndUnlimitedPlanAllowsCreation()
    {
        await using var db = await Database();
        db.MerchantBranches.Add(new MerchantBranch { Id = 2, MerchantId = 1, Name = "Seconda", IsActive = false });
        await db.SaveChangesAsync();
        var branch = await db.MerchantBranches.SingleAsync(b => b.Id == 2);
        branch.IsActive = true;
        await Assert.ThrowsAsync<SubscriptionLimitException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        (await db.SubscriptionPlans.SingleAsync()).MaxBranches = null;
        await db.SaveChangesAsync();
        (await db.MerchantBranches.SingleAsync(b => b.Id == 2)).IsActive = true;
        await db.SaveChangesAsync();
        (await db.MerchantBranches.CountAsync(b => b.IsActive)).Should().Be(2);
    }

    [Fact]
    public async Task StorageQuota_CountsAllVersions_AndFreesLogicalSpaceOnDeletion()
    {
        await using var db = await Database();
        var doc = new HRDocument { Id = 1, TenantId = 1, EmployeeId = 1, Title = "Documento" };
        db.HRDocuments.Add(doc);
        await db.SaveChangesAsync();
        db.HRDocumentVersions.Add(new HRDocumentVersion { HRDocumentId = 1, VersionNumber = 1, FileSizeBytes = 80, UploadStatus = UploadStatus.Completed });
        await db.SaveChangesAsync();
        db.HRDocumentVersions.Add(new HRDocumentVersion { HRDocumentId = 1, VersionNumber = 2, FileSizeBytes = 30, UploadStatus = UploadStatus.Completed });
        await Assert.ThrowsAsync<SubscriptionLimitException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        (await db.HRDocuments.SingleAsync()).IsDeleted = true;
        await db.SaveChangesAsync();
        var next = new HRDocument { Id = 2, TenantId = 1, EmployeeId = 1, Title = "Nuovo" };
        db.HRDocuments.Add(next);
        await db.SaveChangesAsync();
        db.HRDocumentVersions.Add(new HRDocumentVersion { HRDocumentId = 2, VersionNumber = 1, FileSizeBytes = 100, UploadStatus = UploadStatus.Completed });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task StorageQuota_BlocksNewDocumentAndVersionInOneSave_AndRestore()
    {
        await using var db = await Database();
        var document = new HRDocument { TenantId = 1, EmployeeId = 1, Title = "Documento", Versions = [
            new HRDocumentVersion { VersionNumber = 1, FileSizeBytes = 101, UploadStatus = UploadStatus.Completed }] };
        db.HRDocuments.Add(document);
        await Assert.ThrowsAsync<SubscriptionLimitException>(() => db.SaveChangesAsync());
        document.IsDeleted = true;
        await db.SaveChangesAsync();
        document.IsDeleted = false;
        await Assert.ThrowsAsync<SubscriptionLimitException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task BranchQuota_DoesNotDoubleCountAConcurrentDeactivation()
    {
        await using var db = await Database();
        var options = (DbContextOptions<ApplicationDbContext>)db.GetService<IDbContextOptions>();
        var branch = await db.MerchantBranches.SingleAsync();
        await using (var other = new ApplicationDbContext(options))
        {
            (await other.MerchantBranches.SingleAsync()).IsActive = false;
            await other.SaveChangesAsync();
        }
        branch.IsActive = false;
        db.MerchantBranches.AddRange(new MerchantBranch { MerchantId = 1, Name = "A" }, new MerchantBranch { MerchantId = 1, Name = "B" });
        await Assert.ThrowsAsync<SubscriptionLimitException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task StorageQuota_UsesCommittedSizeWhenAnotherContextChangesVersion()
    {
        await using var db = await Database();
        db.HRDocuments.Add(new HRDocument { TenantId = 1, EmployeeId = 1, Title = "Documento", Versions = [
            new HRDocumentVersion { VersionNumber = 1, FileSizeBytes = 100, UploadStatus = UploadStatus.Completed }] });
        await db.SaveChangesAsync();
        var version = await db.HRDocumentVersions.SingleAsync();
        var options = (DbContextOptions<ApplicationDbContext>)db.GetService<IDbContextOptions>();
        await using (var other = new ApplicationDbContext(options))
        {
            (await other.HRDocumentVersions.SingleAsync()).FileSizeBytes = 10;
            await other.SaveChangesAsync();
        }
        version.FileSizeBytes = 90;
        db.HRDocumentVersions.Add(new HRDocumentVersion { HRDocumentId = version.HRDocumentId, VersionNumber = 2,
            FileSizeBytes = 20, UploadStatus = UploadStatus.Completed });
        await Assert.ThrowsAsync<SubscriptionLimitException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task SynchronousSave_AlsoEnforcesQuotas()
    {
        await using var db = await Database();
        db.MerchantBranches.Add(new MerchantBranch { MerchantId = 1, Name = "Seconda" });
        Assert.Throws<SubscriptionLimitException>(() => db.SaveChanges());
    }

    [Fact]
    public async Task CalendarWithoutSkills_RejectsCreationAndPreservesHistoricalSkillsOnUpdate()
    {
        var service = new Mock<AppointmentScheduler.Core.Services.IEventService>();
        var claims = new[] { new Claim("MerchantId", "1"), new Claim(ClaimTypes.NameIdentifier, "123"),
            new Claim("Feature", "Calendario"), new Claim("FeatureLevel", "Calendario:Manager") };
        var controller = new EventsController(service.Object).WithUser(claims);
        var result = await controller.Create(new CreateEventRequest { RequiredSkills = [new() { SkillId = 9 }] });
        result.Result.Should().BeOfType<ForbidResult>();
        service.Verify(s => s.CreateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CreateEventRequest>()), Times.Never);
        service.Setup(s => s.GetByIdAsync(1, 1)).ReturnsAsync(new EventDto { Id = 1,
            RequiredSkills = [new() { SkillId = 2, Quantity = 3 }], Participants = [new() { EmployeeId = 1, SkillId = 2 }] });
        var update = new UpdateEventRequest { OwnerEmployeeIds = [1, 2], RequiredSkills = [new() { SkillId = 9 }],
            ParticipantSkills = [new() { EmployeeId = 2, SkillId = 9 }] };
        service.Setup(s => s.UpdateAsync(1, 1, update)).ReturnsAsync(new EventDto { Id = 1 });
        (await controller.Update(1, update)).Result.Should().BeOfType<OkObjectResult>();
        update.RequiredSkills.Should().ContainSingle().Which.SkillId.Should().Be(2);
        update.ParticipantSkills.Should().ContainSingle().Which.EmployeeId.Should().Be(1);
    }

    [Fact]
    public async Task ResourcesWithoutSkills_CannotModifySkillAssignments()
    {
        var service = new Mock<AppointmentScheduler.Core.Services.IEmployeeService>();
        var controller = new EmployeeResourcesController(service.Object).WithUser(new Claim("MerchantId", "1"), new Claim("Feature", "Risorse"));
        (await controller.Create(new CreateEmployeeRequest { SkillIds = [9] })).Result.Should().BeOfType<ForbidResult>();
        service.Setup(s => s.GetByIdAsync(1, 1)).ReturnsAsync(new EmployeeDto { Id = 1, Skills = [new() { SkillId = 2 }] });
        var update = new UpdateEmployeeRequest { SkillIds = [9] };
        service.Setup(s => s.UpdateAsync(1, 1, update)).ReturnsAsync(new EmployeeDto { Id = 1 });
        (await controller.Update(1, update)).Result.Should().BeOfType<OkObjectResult>();
        update.SkillIds.Should().Equal(2);
    }

    [Fact]
    public async Task HttpPipeline_EnforcesPackageAndTrial_WithoutRenewingToken()
    {
        await using var db = await Database();
        var options = (DbContextOptions<ApplicationDbContext>)db.GetService<IDbContextOptions>();
        using var factory = new ApiWebApplicationFactory();
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.AddScoped(_ => new ApplicationDbContext(options));
            services.RemoveAll<IUtcClock>();
            services.AddSingleton(Clock());
            services.AddAuthentication("SubscriptionTest").AddScheme<AuthenticationSchemeOptions, SubscriptionTestAuthHandler>("SubscriptionTest", _ => { });
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultAuthenticateScheme = "SubscriptionTest";
                o.DefaultChallengeScheme = "SubscriptionTest";
                o.DefaultScheme = "SubscriptionTest";
            });
        }));
        using var client = app.CreateClient();
        (await client.GetAsync("/api/subscription")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/employee/inventory/items")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/admin/subscription-plans")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/events/employee?from=2026-09-14&to=2026-09-20")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/employee-requests/calendar")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await db.Merchants.SingleAsync()).TrialEndsAt = Now;
        await db.SaveChangesAsync();
        (await client.GetAsync("/api/events/employee?from=2026-09-14&to=2026-09-20")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/subscription/branches")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var expired = await client.GetAsync("/api/subscription");
        expired.StatusCode.Should().Be(HttpStatusCode.OK);
        (await expired.Content.ReadAsStringAsync()).Should().Contain("Expired");
        (await db.Merchants.SingleAsync()).TrialEndsAt = null;
        await db.SaveChangesAsync();
        (await client.GetAsync("/api/events/employee?from=2026-09-14&to=2026-09-20")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
