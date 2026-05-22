using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class MerchantServiceTests
{
    private readonly Mock<IUtcClock> _clock = new();

    [Fact]
    public async Task GetAllAsync_ReturnsMappedMerchants_WithInternalEmployeeCounts()
    {
        var merchants = new List<Merchant>
        {
            new()
            {
                Id = 1,
                UserId = 10,
                CompanyName = "Contoso",
                CreatedAt = new DateTime(2026, 5, 21, 10, 0, 0, DateTimeKind.Utc),
                User = new User { Id = 10, Email = "owner1@example.com", FirstName = "Ada", LastName = "One" },
                Branches = new List<MerchantBranch> { new() { Id = 101, MerchantId = 1, Name = "HQ" } }
            },
            new()
            {
                Id = 2,
                UserId = 20,
                CompanyName = "Fabrikam",
                CreatedAt = new DateTime(2026, 5, 22, 10, 0, 0, DateTimeKind.Utc),
                User = new User { Id = 20, Email = "owner2@example.com", FirstName = "Ada", LastName = "Two" },
                Branches = new List<MerchantBranch> { new() { Id = 201, MerchantId = 2, Name = "HQ" }, new() { Id = 202, MerchantId = 2, Name = "Store" } }
            }
        };
        var memberships = new List<EmployeeMembership>
        {
            new() { MerchantId = 1, EmployeeId = 1001, IsActive = true, Employee = new Employee { Kind = EmployeeKind.Internal } },
            new() { MerchantId = 1, EmployeeId = 1002, IsActive = true, Employee = new Employee { Kind = EmployeeKind.External } },
            new() { MerchantId = 2, EmployeeId = 2001, IsActive = true, Employee = new Employee { Kind = EmployeeKind.Internal } },
            new() { MerchantId = 2, EmployeeId = 2002, IsActive = true, Employee = new Employee { Kind = EmployeeKind.Internal } }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Merchants, merchants, merchant => [merchant.Id])
            .WithSet(x => x.EmployeeMemberships, memberships)
            .Build();
        var service = new MerchantService(context.Object, _clock.Object);

        var result = await service.GetAllAsync();

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().ContainInOrder(2, 1);
        result[0].CompanyName.Should().Be("Fabrikam");
        result[0].EmployeeCount.Should().Be(2);
        result[0].BranchCount.Should().Be(2);
        result[1].EmployeeCount.Should().Be(1);
        result[1].Owner!.Email.Should().Be("owner1@example.com");
    }

    [Fact]
    public async Task ApproveAsync_SetsApprovalFields_AndDoesNotOverwriteApprovedAt()
    {
        var now = new DateTime(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc);
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        var merchants = new List<Merchant>
        {
            new() { Id = 5, IsApproved = false, IsActive = false, ApprovedAt = new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc) }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Merchants, merchants, merchant => [merchant.Id])
            .Build(out var tracker);
        var service = new MerchantService(context.Object, _clock.Object);

        var result = await service.ApproveAsync(5);

        result.Should().BeTrue();
        merchants[0].IsApproved.Should().BeTrue();
        merchants[0].IsActive.Should().BeTrue();
        merchants[0].ApprovedAt.Should().Be(new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc));
        merchants[0].UpdatedAt.Should().Be(now);
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task RejectAsync_DisablesMerchant_AndClearsApprovedAtOnlyForNeverApprovedMerchant()
    {
        var now = new DateTime(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc);
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        var merchants = new List<Merchant>
        {
            new() { Id = 5, IsApproved = false, IsActive = true, ApprovedAt = new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc) },
            new() { Id = 6, IsApproved = true, IsActive = true, ApprovedAt = new DateTime(2026, 5, 21, 9, 0, 0, DateTimeKind.Utc) }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Merchants, merchants, merchant => [merchant.Id])
            .Build(out var tracker);
        var service = new MerchantService(context.Object, _clock.Object);

        var first = await service.RejectAsync(5);
        var second = await service.RejectAsync(6);

        first.Should().BeTrue();
        second.Should().BeTrue();
        merchants[0].IsActive.Should().BeFalse();
        merchants[0].ApprovedAt.Should().BeNull();
        merchants[1].IsActive.Should().BeFalse();
        merchants[1].ApprovedAt.Should().Be(new DateTime(2026, 5, 21, 9, 0, 0, DateTimeKind.Utc));
        tracker.Count.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsMappedMerchant_WhenMerchantExists()
    {
        var now = new DateTime(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc);
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        var merchants = new List<Merchant>
        {
            new()
            {
                Id = 5,
                UserId = 10,
                CompanyName = "Old Name",
                User = new User { Id = 10, Email = "owner@example.com", FirstName = "Ada", LastName = "Owner" },
                Branches = new List<MerchantBranch> { new() { Id = 100, MerchantId = 5, Name = "HQ" } }
            }
        };
        var memberships = new List<EmployeeMembership>
        {
            new() { MerchantId = 5, EmployeeId = 1, IsActive = true, Employee = new Employee { Kind = EmployeeKind.Internal } },
            new() { MerchantId = 5, EmployeeId = 2, IsActive = true, Employee = new Employee { Kind = EmployeeKind.External } }
        };
        var request = new UpdateMerchantRequest { CompanyName = "New Name", BusinessEmail = "biz@example.com", City = "Milan" };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Merchants, merchants, merchant => [merchant.Id])
            .WithSet(x => x.EmployeeMemberships, memberships)
            .Build(out var tracker);
        var service = new MerchantService(context.Object, _clock.Object);

        var result = await service.UpdateAsync(5, request);

        result.Should().NotBeNull();
        result!.CompanyName.Should().Be("New Name");
        result.BusinessEmail.Should().Be("biz@example.com");
        result.City.Should().Be("Milan");
        result.EmployeeCount.Should().Be(1);
        merchants[0].UpdatedAt.Should().Be(now);
        tracker.Count.Should().Be(1);
    }
}