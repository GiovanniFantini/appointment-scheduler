using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class MerchantRoleServiceTests
{
    private readonly Mock<IUtcClock> _clock = new();

    [Fact]
    public async Task GetRolesAsync_ReturnsDefaultRoleFirst_AndCountsOnlyActiveMembers()
    {
        var roles = new List<MerchantRole>
        {
            new()
            {
                Id = 2,
                MerchantId = 7,
                Name = "Operator",
                IsDefault = false,
                Features = new List<RoleFeature> { new() { Feature = MerchantFeature.Calendario, IsEnabled = true, AccessLevel = FeatureAccessLevel.Operator } },
                Memberships = new List<EmployeeMembership>
                {
                    new() { Id = 10, IsActive = true },
                    new() { Id = 11, IsActive = false }
                }
            },
            new()
            {
                Id = 1,
                MerchantId = 7,
                Name = "Responsabile",
                IsDefault = true,
                Features = new List<RoleFeature> { new() { Feature = MerchantFeature.Calendario, IsEnabled = true, AccessLevel = FeatureAccessLevel.Manager } },
                Memberships = new List<EmployeeMembership>
                {
                    new() { Id = 12, IsActive = true }
                }
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantRoles, roles, x => [x.Id])
            .Build();

        var service = new MerchantRoleService(context.Object, _clock.Object);

        var result = await service.GetRolesAsync(7);

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().ContainInOrder(1, 2);
        result[0].MemberCount.Should().Be(1);
        result[1].MemberCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenRoleDoesNotExist()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.MerchantRoles, x => [x.Id])
            .Build();

        var service = new MerchantRoleService(context.Object, _clock.Object);

        var result = await service.DeleteAsync(99, 7);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenRoleIsDefault()
    {
        var roles = new List<MerchantRole>
        {
            new() { Id = 1, MerchantId = 7, Name = "Default", IsDefault = true, Memberships = new List<EmployeeMembership>() }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantRoles, roles, x => [x.Id])
            .Build();

        var service = new MerchantRoleService(context.Object, _clock.Object);

        var act = () => service.DeleteAsync(1, 7);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Non è possibile eliminare il ruolo predefinito del merchant.");
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenRoleHasActiveMembers()
    {
        var roles = new List<MerchantRole>
        {
            new()
            {
                Id = 1,
                MerchantId = 7,
                Name = "Manager",
                IsDefault = false,
                Memberships = new List<EmployeeMembership> { new() { Id = 11, IsActive = true } }
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantRoles, roles, x => [x.Id])
            .Build();

        var service = new MerchantRoleService(context.Object, _clock.Object);

        var act = () => service.DeleteAsync(1, 7);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Non è possibile eliminare un ruolo con dipendenti assegnati.");
    }

    [Fact]
    public async Task DeleteAsync_RemovesRole_WhenNoActiveMembersAndNotDefault()
    {
        var roles = new List<MerchantRole>
        {
            new()
            {
                Id = 1,
                MerchantId = 7,
                Name = "Manager",
                IsDefault = false,
                Memberships = new List<EmployeeMembership> { new() { Id = 11, IsActive = false } }
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantRoles, roles, x => [x.Id])
            .Build(out var tracker);

        var service = new MerchantRoleService(context.Object, _clock.Object);

        var result = await service.DeleteAsync(1, 7);

        result.Should().BeTrue();
        roles.Should().BeEmpty();
        tracker.Count.Should().Be(1);
    }
}
