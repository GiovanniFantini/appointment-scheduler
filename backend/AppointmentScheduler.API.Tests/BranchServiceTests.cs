using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class BranchServiceTests
{
    private readonly Mock<IUtcClock> _clock = new();

    [Fact]
    public async Task CreateBranchAsync_MakesFirstBranchHeadquarters()
    {
        var now = new DateTime(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc);
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        var branches = new List<MerchantBranch>();
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantBranches, branches, branch => [branch.Id])
            .Build(out var tracker);
        var service = new BranchService(context.Object, _clock.Object);

        var result = await service.CreateBranchAsync(7, new CreateBranchRequest { Name = "  HQ  ", Code = " MI01 " });

        result.Name.Should().Be("HQ");
        result.IsHeadquarters.Should().BeTrue();
        result.Code.Should().Be("MI01");
        branches.Should().ContainSingle();
        branches[0].CreatedAt.Should().Be(now);
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task UpdateBranchAsync_Throws_WhenTryingToDeactivateHeadquarters()
    {
        var branches = new List<MerchantBranch>
        {
            new() { Id = 5, MerchantId = 7, Name = "HQ", IsHeadquarters = true, IsActive = true, Departments = new List<Department>() }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantBranches, branches, branch => [branch.Id])
            .Build();
        var service = new BranchService(context.Object, _clock.Object);

        var act = () => service.UpdateBranchAsync(5, 7, new UpdateBranchRequest { Name = "HQ", IsActive = false });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Non puoi disattivare la sede principale. Promuovi prima un'altra filiale.");
    }

    [Fact]
    public async Task DeleteBranchAsync_Throws_WhenBranchHasEvents()
    {
        var branches = new List<MerchantBranch>
        {
            new() { Id = 5, MerchantId = 7, Name = "Store", IsHeadquarters = false },
            new() { Id = 6, MerchantId = 7, Name = "HQ", IsHeadquarters = true }
        };
        var events = new List<Event>
        {
            new() { Id = 1, MerchantId = 7, BranchId = 5, Title = "Shift", EventType = EventType.Turno, StartDate = new DateOnly(2026, 5, 22), CreatedByUserId = 1 }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantBranches, branches, branch => [branch.Id])
            .WithSet(x => x.Events, events)
            .Build();
        var service = new BranchService(context.Object, _clock.Object);

        var act = () => service.DeleteBranchAsync(5, 7);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("La filiale ha turni associati: disattivala invece di eliminarla.");
    }

    [Fact]
    public async Task SetHeadquartersAsync_DemotesCurrentHeadquarters_AndPromotesSelectedBranch()
    {
        var now = new DateTime(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc);
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        var branches = new List<MerchantBranch>
        {
            new() { Id = 5, MerchantId = 7, Name = "HQ", IsHeadquarters = true, IsActive = true },
            new() { Id = 6, MerchantId = 7, Name = "Store", IsHeadquarters = false, IsActive = true }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantBranches, branches, branch => [branch.Id])
            .Build(out var tracker);
        var service = new BranchService(context.Object, _clock.Object);

        var result = await service.SetHeadquartersAsync(6, 7);

        result.Should().BeTrue();
        branches.Single(x => x.Id == 5).IsHeadquarters.Should().BeFalse();
        branches.Single(x => x.Id == 6).IsHeadquarters.Should().BeTrue();
        branches.Single(x => x.Id == 6).UpdatedAt.Should().Be(now);
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task CreateDepartmentAsync_Throws_WhenNameAlreadyExistsInBranch()
    {
        var branches = new List<MerchantBranch>
        {
            new() { Id = 5, MerchantId = 7, Name = "HQ" }
        };
        var departments = new List<Department>
        {
            new() { Id = 1, BranchId = 5, MerchantId = 7, Name = "Sales" }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantBranches, branches, branch => [branch.Id])
            .WithSet(x => x.Departments, departments, department => [department.Id])
            .Build();
        var service = new BranchService(context.Object, _clock.Object);

        var act = () => service.CreateDepartmentAsync(5, 7, new CreateDepartmentRequest { Name = "Sales" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Esiste già un reparto con nome 'Sales' in questa filiale.");
    }

    [Fact]
    public async Task DeleteDepartmentAsync_RemovesDepartment_WhenItExists()
    {
        var departments = new List<Department>
        {
            new() { Id = 4, BranchId = 5, MerchantId = 7, Name = "Sales" }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Departments, departments, department => [department.Id])
            .Build(out var tracker);
        var service = new BranchService(context.Object, _clock.Object);

        var result = await service.DeleteDepartmentAsync(4, 7);

        result.Should().BeTrue();
        departments.Should().BeEmpty();
        tracker.Count.Should().Be(1);
    }
}