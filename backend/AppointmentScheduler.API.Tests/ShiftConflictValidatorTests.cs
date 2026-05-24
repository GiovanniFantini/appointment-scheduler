using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class ShiftConflictValidatorTests
{
    [Fact]
    public async Task DetectAssignmentConflictsAsync_ReturnsEmpty_WhenNoEmployees()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.EmployeeRequests, x => [x.Id])
            .WithEmptySet(x => x.Events, x => [x.Id])
            .WithEmptySet(x => x.EmployeeMemberships, x => [x.Id])
            .Build();

        var service = new ShiftConflictValidator(context.Object);

        var result = await service.DetectAssignmentConflictsAsync(7, [], new DateOnly(2026, 6, 1), new TimeOnly(9, 0), new TimeOnly(17, 0));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectAssignmentConflictsAsync_DetectsApprovedLeaveOverlap()
    {
        var employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi" };
        var requests = new List<EmployeeRequest>
        {
            new()
            {
                Id = 10,
                MerchantId = 7,
                EmployeeId = 11,
                Employee = employee,
                Type = EmployeeRequestType.Ferie,
                Status = RequestStatus.Approved,
                StartDate = new DateOnly(2026, 6, 1),
                EndDate = new DateOnly(2026, 6, 1)
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EmployeeRequests, requests, x => [x.Id])
            .WithEmptySet(x => x.Events, x => [x.Id])
            .WithEmptySet(x => x.EmployeeMemberships, x => [x.Id])
            .Build();

        var service = new ShiftConflictValidator(context.Object);

        var result = await service.DetectAssignmentConflictsAsync(7, [11], new DateOnly(2026, 6, 1), new TimeOnly(9, 0), new TimeOnly(17, 0));

        result.Should().ContainSingle(c => c.Kind == ShiftConflictKind.LeaveOverlap && c.EmployeeId == 11 && c.RequestId == 10);
    }

    [Fact]
    public async Task DetectAssignmentConflictsAsync_DetectsBranchMismatch_WhenEmployeeHasNoAccess()
    {
        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ" };
        var memberships = new List<EmployeeMembership>
        {
            new()
            {
                Id = 50,
                EmployeeId = 11,
                MerchantId = 7,
                IsActive = true,
                HomeBranchId = 4,
                Employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi" },
                BranchAccess = new List<EmployeeBranchAccess>()
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.EmployeeRequests, x => [x.Id])
            .WithEmptySet(x => x.Events, x => [x.Id])
            .WithSet(x => x.MerchantBranches, new List<MerchantBranch> { branch }, x => [x.Id])
            .WithSet(x => x.EmployeeMemberships, memberships, x => [x.Id])
            .Build();

        var service = new ShiftConflictValidator(context.Object);

        var result = await service.DetectAssignmentConflictsAsync(7, [11], new DateOnly(2026, 6, 1), new TimeOnly(9, 0), new TimeOnly(17, 0), branchId: 3);

        result.Should().ContainSingle(c => c.Kind == ShiftConflictKind.BranchMismatch && c.BranchId == 3);
    }
}
