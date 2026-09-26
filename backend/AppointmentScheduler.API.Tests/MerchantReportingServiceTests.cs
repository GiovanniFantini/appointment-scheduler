using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class MerchantReportingServiceTests
{
    private static readonly DateOnly From = new(2026, 9, 1);
    private static readonly DateOnly To = new(2026, 9, 30);

    [Fact]
    public async Task Report_ScopesTenantBranchAndDates_AndIncludesOverlappingRequests()
    {
        var shift = new Event { Id = 1, MerchantId = 7, BranchId = 3, StartDate = From,
            EndDate = From.AddDays(1), EventType = EventType.Turno,
            Participants = [new() { EmployeeId = 1 }, new() { EmployeeId = 2 }] };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantBranches, [new() { Id = 3, MerchantId = 7 }])
            .WithSet(x => x.Events, [shift,
                new() { MerchantId = 8, BranchId = 3, StartDate = From, EventType = EventType.Turno },
                new() { MerchantId = 7, BranchId = 4, StartDate = From, EventType = EventType.Turno },
                new() { MerchantId = 7, BranchId = 3, StartDate = From.AddDays(-1), EventType = EventType.Turno },
                new() { MerchantId = 7, BranchId = 3, StartDate = From, EventType = EventType.Ferie }])
            .WithSet(x => x.EmployeeMemberships, [new() { MerchantId = 7, EmployeeId = 1, HomeBranchId = 3 },
                new() { MerchantId = 7, EmployeeId = 2, HomeBranchId = 4 }])
            .WithSet(x => x.EmployeeRequests, [
                new() { MerchantId = 7, EmployeeId = 1, StartDate = From.AddDays(-3), EndDate = From, Status = RequestStatus.Approved },
                new() { MerchantId = 7, EmployeeId = 2, EventId = 1, Event = shift, StartDate = To, Status = RequestStatus.Pending },
                new() { MerchantId = 7, EmployeeId = 2, StartDate = From, Status = RequestStatus.Rejected },
                new() { MerchantId = 8, EmployeeId = 1, StartDate = From, Status = RequestStatus.Pending },
                new() { MerchantId = 7, EmployeeId = 1, StartDate = To.AddDays(1), Status = RequestStatus.Pending }])
            .Build();
        var clock = new Mock<ITimeClockService>();
        var rows = new List<TimeClockReportRowDto> { new() { WorkedMinutes = 450, BreakMinutes = 30 } };
        clock.Setup(x => x.GetReportAsync(7, 3, From, To)).ReturnsAsync(rows);

        var result = await new MerchantReportingService(context.Object, clock.Object).GetAsync(7, From, To, 3);

        result.ShiftCount.Should().Be(1);
        result.AssignedShiftCount.Should().Be(2);
        result.ApprovedRequests.Should().Be(1);
        result.PendingRequests.Should().Be(1);
        result.RejectedRequests.Should().Be(0);
        result.Days.Should().HaveCount(30);
        result.Days[0].ShiftCount.Should().Be(1);
        result.Days[1].ShiftCount.Should().Be(0);
        result.TimeEntries.Should().BeSameAs(rows);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(366)]
    public async Task Report_RejectsInvalidRange(int days)
    {
        var context = new ApplicationDbContextMockBuilder().Build();
        var service = new MerchantReportingService(context.Object, Mock.Of<ITimeClockService>());
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetAsync(7, From, From.AddDays(days), null));
    }

    [Fact]
    public async Task Report_RejectsForeignBranch()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantBranches, [new() { Id = 3, MerchantId = 8 }]).Build();
        var service = new MerchantReportingService(context.Object, Mock.Of<ITimeClockService>());
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetAsync(7, From, To, 3));
    }

    [Fact]
    public async Task Report_EmptyAllBranches_ReturnsZeroesAndInclusiveDates()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.Events).WithEmptySet(x => x.EmployeeRequests).Build();
        var clock = new Mock<ITimeClockService>();
        clock.Setup(x => x.GetReportAsync(7, null, From, From)).ReturnsAsync([]);
        var result = await new MerchantReportingService(context.Object, clock.Object).GetAsync(7, From, From, null);
        result.ShiftCount.Should().Be(0);
        result.PendingRequests.Should().Be(0);
        result.Days.Should().ContainSingle().Which.Date.Should().Be(From);
        result.TimeEntries.Should().BeEmpty();
    }
}
