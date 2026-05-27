using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class AdminEmployeeServiceTests
{
    private static (List<Employee> employees, List<Merchant> merchants) BuildFixture()
    {
        var contoso = new Merchant { Id = 100, CompanyName = "Contoso", IsApproved = true, IsActive = true };
        var fabrikam = new Merchant { Id = 200, CompanyName = "Fabrikam", IsApproved = true, IsActive = true };

        var hq = new MerchantBranch { Id = 1001, MerchantId = 100, Name = "HQ" };
        var fbq = new MerchantBranch { Id = 2001, MerchantId = 200, Name = "HQ" };

        var roleC = new MerchantRole { Id = 11, MerchantId = 100, Name = "Responsabile App", IsDefault = true };
        var roleF = new MerchantRole { Id = 21, MerchantId = 200, Name = "Responsabile App", IsDefault = true };

        var employees = new List<Employee>
        {
            // Employee con User collegato e una sola membership (Contoso)
            new()
            {
                Id = 1, UserId = 500, Email = "luca@contoso.com", FirstName = "Luca", LastName = "Verdi",
                Kind = EmployeeKind.Internal, IsActive = true,
                CreatedAt = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc),
                Memberships = new List<EmployeeMembership>
                {
                    new() { Id = 1, EmployeeId = 1, MerchantId = 100, RoleId = 11, HomeBranchId = 1001, IsActive = true, JoinedAt = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc), Merchant = contoso, Role = roleC, HomeBranch = hq }
                }
            },
            // Employee pre-caricato (no UserId), External, in due merchant
            new()
            {
                Id = 2, UserId = null, Email = "ext@example.com", FirstName = "Mario", LastName = "Esterno",
                Kind = EmployeeKind.External, IsActive = true,
                CreatedAt = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc),
                Memberships = new List<EmployeeMembership>
                {
                    new() { Id = 2, EmployeeId = 2, MerchantId = 100, RoleId = 11, HomeBranchId = 1001, IsActive = true, JoinedAt = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc), Merchant = contoso, Role = roleC, HomeBranch = hq },
                    new() { Id = 3, EmployeeId = 2, MerchantId = 200, RoleId = 21, HomeBranchId = 2001, IsActive = true, JoinedAt = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc), Merchant = fabrikam, Role = roleF, HomeBranch = fbq }
                }
            },
            // Employee disattivo, nessuna membership
            new()
            {
                Id = 3, UserId = null, Email = "ghost@example.com", FirstName = "Ghost", LastName = "User",
                Kind = EmployeeKind.Internal, IsActive = false,
                CreatedAt = new DateTime(2026, 5, 14, 0, 0, 0, DateTimeKind.Utc),
                Memberships = new List<EmployeeMembership>()
            }
        };

        return (employees, new List<Merchant> { contoso, fabrikam });
    }

    private static AdminEmployeeService BuildService(List<Employee> employees)
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Employees, employees, x => [x.Id])
            .Build();
        return new AdminEmployeeService(context.Object);
    }

    [Fact]
    public async Task GetEmployeesAsync_ReturnsAll_OrderedByCreatedDesc()
    {
        var (employees, _) = BuildFixture();
        var service = BuildService(employees);

        var result = await service.GetEmployeesAsync(new AdminEmployeeListQuery());

        result.Total.Should().Be(3);
        result.Items.Select(i => i.Id).Should().ContainInOrder(3, 2, 1);
    }

    [Fact]
    public async Task GetEmployeesAsync_FiltersByMerchantId()
    {
        var (employees, _) = BuildFixture();
        var service = BuildService(employees);

        var contosoOnly = await service.GetEmployeesAsync(new AdminEmployeeListQuery { MerchantId = 100 });
        contosoOnly.Total.Should().Be(2);
        contosoOnly.Items.Select(i => i.Id).Should().BeEquivalentTo(new[] { 1, 2 });

        var fabrikamOnly = await service.GetEmployeesAsync(new AdminEmployeeListQuery { MerchantId = 200 });
        fabrikamOnly.Items.Should().ContainSingle().Which.Id.Should().Be(2);
    }

    [Fact]
    public async Task GetEmployeesAsync_FiltersByHasAccount()
    {
        var (employees, _) = BuildFixture();
        var service = BuildService(employees);

        var withAccount = await service.GetEmployeesAsync(new AdminEmployeeListQuery { HasAccount = true });
        withAccount.Items.Should().ContainSingle().Which.UserId.Should().Be(500);

        var withoutAccount = await service.GetEmployeesAsync(new AdminEmployeeListQuery { HasAccount = false });
        withoutAccount.Total.Should().Be(2);
        withoutAccount.Items.Should().OnlyContain(i => i.UserId == null);
    }

    [Fact]
    public async Task GetEmployeesAsync_FiltersByKindAndIsActive()
    {
        var (employees, _) = BuildFixture();
        var service = BuildService(employees);

        var externals = await service.GetEmployeesAsync(new AdminEmployeeListQuery { Kind = EmployeeKind.External });
        externals.Items.Should().ContainSingle().Which.Id.Should().Be(2);

        var inactives = await service.GetEmployeesAsync(new AdminEmployeeListQuery { IsActive = false });
        inactives.Items.Should().ContainSingle().Which.Id.Should().Be(3);
    }

    [Fact]
    public async Task GetEmployeesAsync_Search_CaseInsensitive()
    {
        var (employees, _) = BuildFixture();
        var service = BuildService(employees);

        var result = await service.GetEmployeesAsync(new AdminEmployeeListQuery { Search = "MARIO" });
        result.Items.Should().ContainSingle().Which.Id.Should().Be(2);
    }

    [Fact]
    public async Task GetEmployeesAsync_PrimaryMerchant_OnlyWhenSingleMembership()
    {
        var (employees, _) = BuildFixture();
        var service = BuildService(employees);

        var result = await service.GetEmployeesAsync(new AdminEmployeeListQuery());
        var single = result.Items.Single(i => i.Id == 1);
        single.MerchantCount.Should().Be(1);
        single.PrimaryMerchantId.Should().Be(100);
        single.PrimaryMerchantName.Should().Be("Contoso");

        var multi = result.Items.Single(i => i.Id == 2);
        multi.MerchantCount.Should().Be(2);
        multi.PrimaryMerchantId.Should().BeNull();
        multi.PrimaryMerchantName.Should().BeNull();

        var none = result.Items.Single(i => i.Id == 3);
        none.MerchantCount.Should().Be(0);
        none.PrimaryMerchantId.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDetailWithMemberships()
    {
        var (employees, _) = BuildFixture();
        var service = BuildService(employees);

        var detail = await service.GetByIdAsync(2);

        detail.Should().NotBeNull();
        detail!.HasAccount.Should().BeFalse();
        detail.Memberships.Should().HaveCount(2);
        detail.Memberships.Select(m => m.MerchantName).Should().BeEquivalentTo(new[] { "Contoso", "Fabrikam" });
        detail.Memberships.Should().OnlyContain(m => !string.IsNullOrEmpty(m.HomeBranchName));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenMissing()
    {
        var (employees, _) = BuildFixture();
        var service = BuildService(employees);

        var result = await service.GetByIdAsync(99);
        result.Should().BeNull();
    }
}
