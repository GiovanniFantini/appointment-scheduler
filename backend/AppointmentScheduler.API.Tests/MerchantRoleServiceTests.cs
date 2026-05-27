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
    public async Task GetDefaultRoleAsync_ReturnsOnlyDefaultRole()
    {
        var roles = new List<MerchantRole>
        {
            new() { Id = 1, MerchantId = 7, Name = "Operator", IsDefault = false, Features = new List<RoleFeature>(), Memberships = new List<EmployeeMembership>() },
            new() { Id = 2, MerchantId = 7, Name = "Default", IsDefault = true,  Features = new List<RoleFeature> { new() { Feature = MerchantFeature.Timbratura, IsEnabled = true, AccessLevel = FeatureAccessLevel.Manager } }, Memberships = new List<EmployeeMembership>() },
            new() { Id = 3, MerchantId = 8, Name = "Other-Tenant-Default", IsDefault = true, Features = new List<RoleFeature>(), Memberships = new List<EmployeeMembership>() }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantRoles, roles, x => [x.Id])
            .Build();

        var service = new MerchantRoleService(context.Object, _clock.Object);

        var result = await service.GetDefaultRoleAsync(7);

        result.Should().NotBeNull();
        result!.Id.Should().Be(2);
        result.IsDefault.Should().BeTrue();
        result.Features.Should().ContainSingle(f => f.Feature == MerchantFeature.Timbratura);
    }

    [Fact]
    public async Task UpdateDefaultRoleFeaturesAsync_ReplacesFeatures_OnDefaultRoleOnly()
    {
        var existingFeatures = new List<RoleFeature>
        {
            new() { Id = 100, RoleId = 2, Feature = MerchantFeature.Calendario, IsEnabled = true, AccessLevel = FeatureAccessLevel.Manager }
        };
        var defaultRole = new MerchantRole
        {
            Id = 2,
            MerchantId = 7,
            Name = "Default",
            IsDefault = true,
            Features = existingFeatures,
            Memberships = new List<EmployeeMembership>()
        };
        var nonDefaultRole = new MerchantRole
        {
            Id = 1,
            MerchantId = 7,
            Name = "Operator",
            IsDefault = false,
            Features = new List<RoleFeature>
            {
                new() { Id = 200, RoleId = 1, Feature = MerchantFeature.Calendario, IsEnabled = true, AccessLevel = FeatureAccessLevel.Operator }
            },
            Memberships = new List<EmployeeMembership>()
        };
        var roles = new List<MerchantRole> { defaultRole, nonDefaultRole };
        var allRoleFeatures = new List<RoleFeature>();
        allRoleFeatures.AddRange(existingFeatures);
        allRoleFeatures.AddRange(nonDefaultRole.Features);

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantRoles, roles, x => [x.Id])
            .WithSet(x => x.RoleFeatures, allRoleFeatures, x => [x.Id])
            .Build(out var tracker);

        var service = new MerchantRoleService(context.Object, _clock.Object);

        var newFeatures = new List<MerchantFeatureRequest>
        {
            new() { Feature = MerchantFeature.Timbratura, IsEnabled = true, AccessLevel = FeatureAccessLevel.Operator },
            new() { Feature = MerchantFeature.Magazzino, IsEnabled = false }
        };

        var result = await service.UpdateDefaultRoleFeaturesAsync(7, newFeatures);

        result.Should().NotBeNull();
        result!.Id.Should().Be(2);
        result.Features.Should().HaveCount(2);
        result.Features.Should().ContainSingle(f => f.Feature == MerchantFeature.Timbratura && f.IsEnabled && f.AccessLevel == FeatureAccessLevel.Operator);
        result.Features.Should().ContainSingle(f => f.Feature == MerchantFeature.Magazzino && !f.IsEnabled);

        // Non-default role unchanged
        nonDefaultRole.Features.Should().ContainSingle(f => f.Feature == MerchantFeature.Calendario);
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task UpdateDefaultRoleFeaturesAsync_ReturnsNull_WhenDefaultRoleMissing()
    {
        var roles = new List<MerchantRole>
        {
            new() { Id = 1, MerchantId = 7, Name = "Operator", IsDefault = false, Features = new List<RoleFeature>(), Memberships = new List<EmployeeMembership>() }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantRoles, roles, x => [x.Id])
            .WithEmptySet(x => x.RoleFeatures, x => [x.Id])
            .Build();

        var service = new MerchantRoleService(context.Object, _clock.Object);

        var result = await service.UpdateDefaultRoleFeaturesAsync(7, new List<MerchantFeatureRequest>());

        result.Should().BeNull();
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
