using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class SkillServiceTests
{
    private readonly Mock<IUtcClock> _clock = new();

    [Fact]
    public async Task GetEmployeesBySkillAsync_ReturnsEmpty_WhenSkillDoesNotBelongToMerchant()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Skills, new List<Skill> { new() { Id = 1, MerchantId = 9, Name = "Cashier" } }, x => [x.Id])
            .WithEmptySet(x => x.EmployeeMemberships, x => [x.Id])
            .Build();

        var service = new SkillService(context.Object, _clock.Object);

        var result = await service.GetEmployeesBySkillAsync(1, 7);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEmployeesBySkillAsync_MasksTechnicalEmail_ForExternalEmployee()
    {
        var skill = new Skill { Id = 5, MerchantId = 7, Name = "Magazzino", Color = "#111111" };
        var employee = new Employee
        {
            Id = 11,
            FirstName = "Luca",
            LastName = "Verdi",
            Email = "esterno-abc@noemail.local",
            Kind = EmployeeKind.External,
            IsActive = true,
            Skills = new List<EmployeeSkill>
            {
                new() { Id = 77, EmployeeId = 11, SkillId = 5, Skill = skill }
            }
        };
        var role = new MerchantRole { Id = 3, MerchantId = 7, Name = "Operator" };
        var memberships = new List<EmployeeMembership>
        {
            new()
            {
                Id = 1,
                MerchantId = 7,
                EmployeeId = 11,
                Employee = employee,
                IsActive = true,
                RoleId = 3,
                Role = role,
                HomeBranchId = 3
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Skills, new List<Skill> { skill }, x => [x.Id])
            .WithSet(x => x.EmployeeMemberships, memberships, x => [x.Id])
            .Build();

        var service = new SkillService(context.Object, _clock.Object);

        var result = await service.GetEmployeesBySkillAsync(5, 7);

        result.Should().ContainSingle();
        result[0].Email.Should().BeEmpty();
        result[0].HasTechnicalEmail.Should().BeTrue();
        result[0].RoleName.Should().Be("Operator");
        result[0].Skills.Should().ContainSingle(x => x.SkillId == 5 && x.SkillName == "Magazzino");
    }

    [Fact]
    public async Task GetSuggestedEmployeesAsync_FlagsLeavesAndOverlappingShifts()
    {
        var skill = new Skill { Id = 5, MerchantId = 7, Name = "Cassa", Color = "#111111" };

        var empAvailable = new Employee
        {
            Id = 11,
            FirstName = "Anna",
            LastName = "Bianchi",
            IsActive = true,
            Skills = new List<EmployeeSkill> { new() { EmployeeId = 11, SkillId = 5 } }
        };
        var empOnLeave = new Employee
        {
            Id = 12,
            FirstName = "Marco",
            LastName = "Rossi",
            IsActive = true,
            Skills = new List<EmployeeSkill> { new() { EmployeeId = 12, SkillId = 5 } }
        };
        var empBusy = new Employee
        {
            Id = 13,
            FirstName = "Luca",
            LastName = "Verdi",
            IsActive = true,
            Skills = new List<EmployeeSkill> { new() { EmployeeId = 13, SkillId = 5 } }
        };

        var memberships = new List<EmployeeMembership>
        {
            new() { Id = 1, MerchantId = 7, EmployeeId = 11, Employee = empAvailable, IsActive = true, RoleId = 1, Role = new MerchantRole { Id = 1, MerchantId = 7, Name = "Op" }, HomeBranchId = 3 },
            new() { Id = 2, MerchantId = 7, EmployeeId = 12, Employee = empOnLeave, IsActive = true, RoleId = 1, Role = new MerchantRole { Id = 1, MerchantId = 7, Name = "Op" }, HomeBranchId = 3 },
            new() { Id = 3, MerchantId = 7, EmployeeId = 13, Employee = empBusy, IsActive = true, RoleId = 1, Role = new MerchantRole { Id = 1, MerchantId = 7, Name = "Op" }, HomeBranchId = 3 }
        };

        var requests = new List<EmployeeRequest>
        {
            new()
            {
                Id = 31,
                EmployeeId = 12,
                MerchantId = 7,
                Type = EmployeeRequestType.Ferie,
                Status = RequestStatus.Approved,
                StartDate = new DateOnly(2026, 6, 10),
                EndDate = new DateOnly(2026, 6, 10)
            }
        };

        var shifts = new List<Event>
        {
            new()
            {
                Id = 90,
                MerchantId = 7,
                BranchId = 3,
                EventType = EventType.Turno,
                Title = "Turno esistente",
                StartDate = new DateOnly(2026, 6, 10),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(14, 0),
                Participants = new List<EventParticipant>
                {
                    new() { Id = 500, EmployeeId = 13 }
                }
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EmployeeMemberships, memberships, x => [x.Id])
            .WithSet(x => x.EmployeeRequests, requests, x => [x.Id])
            .WithSet(x => x.Events, shifts, x => [x.Id])
            .Build();

        var service = new SkillService(context.Object, _clock.Object);

        var result = await service.GetSuggestedEmployeesAsync(
            merchantId: 7,
            skillId: 5,
            date: new DateOnly(2026, 6, 10),
            startTime: new TimeOnly(11, 0),
            endTime: new TimeOnly(12, 0));

        result.Should().HaveCount(3);
        result[0].EmployeeId.Should().Be(11);
        result[0].IsAvailable.Should().BeTrue();
        result.Should().Contain(x => x.EmployeeId == 12 && !x.IsAvailable && x.UnavailableReason == "In ferie");
        result.Should().Contain(x => x.EmployeeId == 13 && !x.IsAvailable && x.UnavailableReason!.Contains("Turno esistente"));
    }

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
    public async Task GetByIdAsync_ReturnsSkillDto_WhenFound()
    {
        var now = new DateTime(2026, 5, 24, 8, 0, 0, DateTimeKind.Utc);
        var skills = new List<Skill>
        {
            new()
            {
                Id = 1,
                MerchantId = 7,
                Name = "Cashier",
                Color = "#111111",
                IsActive = true,
                CreatedAt = now,
                EmployeeSkills = new List<EmployeeSkill> { new() }
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Skills, skills, x => [x.Id])
            .Build();

        var service = new SkillService(context.Object, _clock.Object);

        var result = await service.GetByIdAsync(1, 7);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Cashier");
        result.EmployeeCount.Should().Be(1);
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