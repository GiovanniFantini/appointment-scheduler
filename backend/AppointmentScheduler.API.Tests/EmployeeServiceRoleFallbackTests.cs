using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class EmployeeServiceRoleFallbackTests
{
    private readonly Mock<IUtcClock> _clock = new();

    [Fact]
    public async Task CreateAsync_UsesInternalBaseRole_WhenRoleIdIsMissingForInternal()
    {
        var roles = new List<MerchantRole>
        {
            new() { Id = 10, MerchantId = 7, Name = "Responsabile App", IsDefault = true },
            new() { Id = 11, MerchantId = 7, Name = "Interno Base" },
            new() { Id = 12, MerchantId = 7, Name = "Esterno Base" }
        };
        var employee = new Employee
        {
            Id = 100,
            Email = "mario.rossi@example.com",
            FirstName = "Mario",
            LastName = "Rossi",
            Kind = EmployeeKind.Internal,
            IsActive = true
        };
        var membership = new EmployeeMembership
        {
            Id = 200,
            EmployeeId = employee.Id,
            Employee = employee,
            MerchantId = 7,
            RoleId = 10,
            HomeBranchId = 3,
            IsActive = false,
            BranchAccess = new List<EmployeeBranchAccess>()
        };
        employee.Memberships.Add(membership);
        var branches = new List<MerchantBranch>
        {
            new() { Id = 3, MerchantId = 7, Name = "HQ", IsHeadquarters = true, IsActive = true }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, new List<User>(), user => [user.Id])
            .WithSet(x => x.Employees, new List<Employee> { employee }, item => [item.Id])
            .WithSet(x => x.EmployeeMemberships, new List<EmployeeMembership> { membership }, item => [item.Id])
            .WithSet(x => x.MerchantRoles, roles, role => [role.Id])
            .WithSet(x => x.MerchantBranches, branches, branch => [branch.Id])
            .WithSet(x => x.Departments, new List<Department>(), department => [department.Id])
            .WithSet(x => x.Skills, new List<Skill>(), skill => [skill.Id])
            .WithSet(x => x.EmployeeSkills, new List<EmployeeSkill>(), employeeSkill => [employeeSkill.EmployeeId, employeeSkill.SkillId])
            .WithSet(x => x.EmployeeBranchAccess, new List<EmployeeBranchAccess>(), access => [access.Id])
            .Build();

        var service = CreateService(context.Object);

        var result = await service.CreateAsync(7, new CreateEmployeeRequest
        {
            FirstName = "Mario",
            LastName = "Rossi",
            Email = "mario.rossi@example.com",
            Kind = EmployeeKind.Internal,
            RoleId = 0,
            SkillIds = new List<int>(),
            AllowedBranchIds = new List<int>()
        });

        result.RoleId.Should().Be(11);
    }

    [Fact]
    public async Task CreateAsync_UsesExternalBaseRole_WhenRoleIdIsMissingForExternal()
    {
        var roles = new List<MerchantRole>
        {
            new() { Id = 10, MerchantId = 7, Name = "Responsabile App", IsDefault = true },
            new() { Id = 11, MerchantId = 7, Name = "Interno Base" },
            new() { Id = 12, MerchantId = 7, Name = "Esterno Base" }
        };
        var employee = new Employee
        {
            Id = 101,
            Email = "luca.bianchi@example.com",
            FirstName = "Luca",
            LastName = "Bianchi",
            Kind = EmployeeKind.External,
            IsActive = true
        };
        var membership = new EmployeeMembership
        {
            Id = 201,
            EmployeeId = employee.Id,
            Employee = employee,
            MerchantId = 7,
            RoleId = 10,
            HomeBranchId = 3,
            IsActive = false,
            BranchAccess = new List<EmployeeBranchAccess>()
        };
        employee.Memberships.Add(membership);
        var branches = new List<MerchantBranch>
        {
            new() { Id = 3, MerchantId = 7, Name = "HQ", IsHeadquarters = true, IsActive = true }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, new List<User>(), user => [user.Id])
            .WithSet(x => x.Employees, new List<Employee> { employee }, item => [item.Id])
            .WithSet(x => x.EmployeeMemberships, new List<EmployeeMembership> { membership }, item => [item.Id])
            .WithSet(x => x.MerchantRoles, roles, role => [role.Id])
            .WithSet(x => x.MerchantBranches, branches, branch => [branch.Id])
            .WithSet(x => x.Departments, new List<Department>(), department => [department.Id])
            .WithSet(x => x.Skills, new List<Skill>(), skill => [skill.Id])
            .WithSet(x => x.EmployeeSkills, new List<EmployeeSkill>(), employeeSkill => [employeeSkill.EmployeeId, employeeSkill.SkillId])
            .WithSet(x => x.EmployeeBranchAccess, new List<EmployeeBranchAccess>(), access => [access.Id])
            .Build();

        var service = CreateService(context.Object);

        var result = await service.CreateAsync(7, new CreateEmployeeRequest
        {
            FirstName = "Luca",
            LastName = "Bianchi",
            Email = "luca.bianchi@example.com",
            Kind = EmployeeKind.External,
            RoleId = 0,
            SkillIds = new List<int>(),
            AllowedBranchIds = new List<int>()
        });

        result.RoleId.Should().Be(12);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenBaseRoleForKindIsMissing()
    {
        var roles = new List<MerchantRole>
        {
            new() { Id = 10, MerchantId = 7, Name = "Responsabile App", IsDefault = true },
            new() { Id = 11, MerchantId = 7, Name = "Interno Base" }
        };
        var branches = new List<MerchantBranch>
        {
            new() { Id = 3, MerchantId = 7, Name = "HQ", IsHeadquarters = true, IsActive = true }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, new List<User>(), user => [user.Id])
            .WithSet(x => x.Employees, new List<Employee>(), employee => [employee.Id])
            .WithSet(x => x.EmployeeMemberships, new List<EmployeeMembership>(), membership => [membership.Id])
            .WithSet(x => x.MerchantRoles, roles, role => [role.Id])
            .WithSet(x => x.MerchantBranches, branches, branch => [branch.Id])
            .WithSet(x => x.Departments, new List<Department>(), department => [department.Id])
            .WithSet(x => x.Skills, new List<Skill>(), skill => [skill.Id])
            .WithSet(x => x.EmployeeSkills, new List<EmployeeSkill>(), employeeSkill => [employeeSkill.EmployeeId, employeeSkill.SkillId])
            .WithSet(x => x.EmployeeBranchAccess, new List<EmployeeBranchAccess>(), access => [access.Id])
            .Build();

        var service = CreateService(context.Object);

        var action = () => service.CreateAsync(7, new CreateEmployeeRequest
        {
            FirstName = "Luca",
            LastName = "Bianchi",
            Kind = EmployeeKind.External,
            RoleId = 0,
            SkillIds = new List<int>(),
            AllowedBranchIds = new List<int>()
        });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Esterno Base*");
    }

    private EmployeeService CreateService(IApplicationDbContext context)
    {
        _clock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 9, 0, 0, DateTimeKind.Utc));
        return new EmployeeService(context, _clock.Object);
    }
}
