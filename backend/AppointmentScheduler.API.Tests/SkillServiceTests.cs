using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class SkillServiceTests
{
    private readonly Mock<IUtcClock> _clock = new();

    [Fact]
    public async Task GetAllByMerchantAsync_ReturnsOrderedSkills_WithEmployeeCounts()
    {
        var skills = new List<Skill>
        {
            new() { Id = 2, MerchantId = 7, Name = "Zeta", EmployeeSkills = new List<EmployeeSkill> { new() } },
            new() { Id = 1, MerchantId = 7, Name = "Alpha", EmployeeSkills = new List<EmployeeSkill> { new(), new() } },
            new() { Id = 3, MerchantId = 9, Name = "Other", EmployeeSkills = new List<EmployeeSkill> { new(), new(), new() } }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Skills, skills, skill => [skill.Id])
            .Build();
        var service = new SkillService(context.Object, _clock.Object);

        var result = await service.GetAllByMerchantAsync(7);

        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().ContainInOrder("Alpha", "Zeta");
        result[0].EmployeeCount.Should().Be(2);
        result[1].EmployeeCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenSkillNameAlreadyExists()
    {
        var skills = new List<Skill>
        {
            new() { Id = 1, MerchantId = 7, Name = "Cashier" }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Skills, skills, skill => [skill.Id])
            .Build();
        var service = new SkillService(context.Object, _clock.Object);

        var act = () => service.CreateAsync(7, new CreateSkillRequest { Name = "Cashier" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Esiste già una mansione con nome 'Cashier'.");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedSkill_WithCurrentEmployeeCount()
    {
        var now = new DateTime(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc);
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        var skills = new List<Skill>
        {
            new() { Id = 1, MerchantId = 7, Name = "Cashier", Color = "#111111", IsActive = true, CreatedAt = now.AddDays(-1) }
        };
        var employeeSkills = new List<EmployeeSkill>
        {
            new() { Id = 10, SkillId = 1 },
            new() { Id = 11, SkillId = 1 },
            new() { Id = 12, SkillId = 2 }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Skills, skills, skill => [skill.Id])
            .WithSet(x => x.EmployeeSkills, employeeSkills, employeeSkill => [employeeSkill.Id])
            .Build(out var tracker);
        var service = new SkillService(context.Object, _clock.Object);

        var result = await service.UpdateAsync(1, 7, new UpdateSkillRequest { Name = "Front Desk", Color = "#222222", IsActive = false });

        result.Should().NotBeNull();
        result!.Name.Should().Be("Front Desk");
        result.Color.Should().Be("#222222");
        result.IsActive.Should().BeFalse();
        result.EmployeeCount.Should().Be(2);
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesSkill_WhenReferencesExist()
    {
        var skills = new List<Skill>
        {
            new() { Id = 1, MerchantId = 7, Name = "Cashier", IsActive = true }
        };
        var employeeSkills = new List<EmployeeSkill> { new() { Id = 10, SkillId = 1 } };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Skills, skills, skill => [skill.Id])
            .WithSet(x => x.EmployeeSkills, employeeSkills, employeeSkill => [employeeSkill.Id])
            .Build(out var tracker);
        var service = new SkillService(context.Object, _clock.Object);

        var result = await service.DeleteAsync(1, 7);

        result.Should().BeTrue();
        skills[0].IsActive.Should().BeFalse();
        skills.Should().HaveCount(1);
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_HardDeletesSkill_WhenNoReferencesExist()
    {
        var skills = new List<Skill>
        {
            new() { Id = 1, MerchantId = 7, Name = "Cashier", IsActive = true }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Skills, skills, skill => [skill.Id])
            .WithEmptySet(x => x.EmployeeSkills, employeeSkill => [employeeSkill.Id])
            .WithEmptySet(x => x.EventRequiredSkills, requiredSkill => [requiredSkill.Id])
            .WithEmptySet(x => x.EventParticipants, participant => [participant.Id])
            .Build(out var tracker);
        var service = new SkillService(context.Object, _clock.Object);

        var result = await service.DeleteAsync(1, 7);

        result.Should().BeTrue();
        skills.Should().BeEmpty();
        tracker.Count.Should().Be(1);
    }
}