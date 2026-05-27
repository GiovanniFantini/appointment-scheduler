using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class AdminUserServiceTests
{
    private readonly Mock<IUtcClock> _clock = new();

    private static List<User> BuildUsers() => new()
    {
        new()
        {
            Id = 1, Email = "admin@example.com", FirstName = "Admin", LastName = "Root",
            AccountType = AccountType.Admin, IsActive = true,
            CreatedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = 2, Email = "owner@contoso.com", FirstName = "Owner", LastName = "Contoso",
            AccountType = AccountType.Merchant, IsActive = true,
            CreatedAt = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc),
            Merchant = new Merchant { Id = 100, CompanyName = "Contoso", IsApproved = true, IsActive = true }
        },
        new()
        {
            Id = 3, Email = "luca@example.com", FirstName = "Luca", LastName = "Verdi",
            AccountType = AccountType.Employee, IsActive = false,
            CreatedAt = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            Employee = new Employee { Id = 500, Email = "luca@example.com", Kind = EmployeeKind.Internal, IsActive = false }
        }
    };

    [Fact]
    public async Task GetUsersAsync_NoFilters_ReturnsAllOrderedByCreatedDesc()
    {
        var users = BuildUsers();
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, x => [x.Id])
            .Build();

        var service = new AdminUserService(context.Object, _clock.Object);

        var result = await service.GetUsersAsync(new AdminUserListQuery());

        result.Total.Should().Be(3);
        result.Items.Should().HaveCount(3);
        result.Items.Select(i => i.Id).Should().ContainInOrder(3, 2, 1);
        result.Items[1].MerchantName.Should().Be("Contoso");
        result.Items[1].MerchantId.Should().Be(100);
        result.Items[0].EmployeeId.Should().Be(500);
    }

    [Fact]
    public async Task GetUsersAsync_FiltersByAccountTypeAndActive()
    {
        var users = BuildUsers();
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, x => [x.Id])
            .Build();

        var service = new AdminUserService(context.Object, _clock.Object);

        var result = await service.GetUsersAsync(new AdminUserListQuery
        {
            AccountType = AccountType.Employee,
            IsActive = false
        });

        result.Total.Should().Be(1);
        result.Items.Single().Id.Should().Be(3);
    }

    [Fact]
    public async Task GetUsersAsync_SearchMatchesEmailOrName_CaseInsensitive()
    {
        var users = BuildUsers();
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, x => [x.Id])
            .Build();

        var service = new AdminUserService(context.Object, _clock.Object);

        var byEmail = await service.GetUsersAsync(new AdminUserListQuery { Search = "CONTOSO" });
        byEmail.Items.Should().ContainSingle().Which.Id.Should().Be(2);

        var byName = await service.GetUsersAsync(new AdminUserListQuery { Search = "verdi" });
        byName.Items.Should().ContainSingle().Which.Id.Should().Be(3);
    }

    [Fact]
    public async Task GetUsersAsync_PaginatesResults()
    {
        var users = BuildUsers();
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, x => [x.Id])
            .Build();

        var service = new AdminUserService(context.Object, _clock.Object);

        var page1 = await service.GetUsersAsync(new AdminUserListQuery { Page = 1, PageSize = 2 });
        page1.Items.Should().HaveCount(2);
        page1.Total.Should().Be(3);
        page1.Items.Select(i => i.Id).Should().ContainInOrder(3, 2);

        var page2 = await service.GetUsersAsync(new AdminUserListQuery { Page = 2, PageSize = 2 });
        page2.Items.Should().HaveCount(1);
        page2.Items[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDetail_WithMerchantLink()
    {
        var users = BuildUsers();
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, x => [x.Id])
            .Build();

        var service = new AdminUserService(context.Object, _clock.Object);

        var detail = await service.GetByIdAsync(2);

        detail.Should().NotBeNull();
        detail!.AccountType.Should().Be(AccountType.Merchant);
        detail.Merchant.Should().NotBeNull();
        detail.Merchant!.CompanyName.Should().Be("Contoso");
        detail.Employee.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenMissing()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.Users, x => [x.Id])
            .Build();

        var service = new AdminUserService(context.Object, _clock.Object);

        var result = await service.GetByIdAsync(42);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeactivateAsync_PreventsSelfDeactivate()
    {
        var users = BuildUsers();
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, x => [x.Id])
            .Build(out var tracker);

        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc));
        var service = new AdminUserService(context.Object, _clock.Object);

        var result = await service.DeactivateAsync(id: 1, currentAdminUserId: 1);

        result.Should().Be(DeactivateResult.CannotDeactivateSelf);
        users.Single(u => u.Id == 1).IsActive.Should().BeTrue();
        tracker.Count.Should().Be(0);
    }

    [Fact]
    public async Task DeactivateAsync_SetsInactive_AndStampsUpdatedAt()
    {
        var users = BuildUsers();
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, x => [x.Id])
            .Build(out var tracker);

        var now = new DateTime(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc);
        _clock.Setup(c => c.UtcNow).Returns(now);
        var service = new AdminUserService(context.Object, _clock.Object);

        var result = await service.DeactivateAsync(id: 2, currentAdminUserId: 1);

        result.Should().Be(DeactivateResult.Success);
        var target = users.Single(u => u.Id == 2);
        target.IsActive.Should().BeFalse();
        target.UpdatedAt.Should().Be(now);
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task DeactivateAsync_ReturnsNotFound_WhenMissing()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.Users, x => [x.Id])
            .Build();

        var service = new AdminUserService(context.Object, _clock.Object);

        var result = await service.DeactivateAsync(id: 99, currentAdminUserId: 1);
        result.Should().Be(DeactivateResult.NotFound);
    }

    [Fact]
    public async Task ActivateAsync_SetsActive_AndStampsUpdatedAt()
    {
        var users = BuildUsers();
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, x => [x.Id])
            .Build(out var tracker);

        var now = new DateTime(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc);
        _clock.Setup(c => c.UtcNow).Returns(now);
        var service = new AdminUserService(context.Object, _clock.Object);

        var ok = await service.ActivateAsync(3);

        ok.Should().BeTrue();
        var target = users.Single(u => u.Id == 3);
        target.IsActive.Should().BeTrue();
        target.UpdatedAt.Should().Be(now);
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task ActivateAsync_NoOp_WhenAlreadyActive()
    {
        var users = BuildUsers();
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, x => [x.Id])
            .Build(out var tracker);

        var service = new AdminUserService(context.Object, _clock.Object);

        var ok = await service.ActivateAsync(2);

        ok.Should().BeTrue();
        users.Single(u => u.Id == 2).IsActive.Should().BeTrue();
        tracker.Count.Should().Be(0);
    }
}
