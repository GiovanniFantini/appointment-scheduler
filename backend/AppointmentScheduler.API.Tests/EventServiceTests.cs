using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class EventServiceTests
{
    private readonly Mock<IShiftConflictValidator> _conflictValidator = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IUtcClock> _clock = new();

    [Fact]
    public async Task CreateAsync_ThrowsEventConflictException_WhenShiftOverlapsBusinessClosure()
    {
        var branch = new MerchantBranch
        {
            Id = 3,
            MerchantId = 7,
            Name = "HQ",
            IsHeadquarters = true,
        };
        var closures = new List<Event>
        {
            new()
            {
                Id = 21,
                MerchantId = 7,
                BranchId = 3,
                Branch = branch,
                EventType = EventType.ChiusuraAziendale,
                Title = "Chiusura straordinaria",
                StartDate = new DateOnly(2026, 6, 1),
                EndDate = new DateOnly(2026, 6, 1),
                IsAllDay = true,
            }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantBranches, new List<MerchantBranch> { branch }, item => [item.Id])
            .WithSet(x => x.Events, closures, item => [item.Id])
            .WithEmptySet(x => x.Departments, item => [item.Id])
            .WithEmptySet(x => x.EmployeeRequests, item => [item.Id])
            .WithEmptySet(x => x.EmployeeMemberships, item => [item.Id])
            .WithEmptySet(x => x.EmployeeSkills, item => [item.Id])
            .Build(out var tracker);

        _clock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 23, 8, 0, 0, DateTimeKind.Utc));
        _conflictValidator
            .Setup(service => service.DetectAssignmentConflictsAsync(7, It.IsAny<IReadOnlyList<int>>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly?>(), It.IsAny<TimeOnly?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(new List<ShiftConflictDto>());

        var service = new EventService(context.Object, _conflictValidator.Object, _notificationService.Object, _clock.Object);

        var act = () => service.CreateAsync(7, 12, new CreateEventRequest
        {
            BranchId = 3,
            Title = "Turno mattina",
            EventType = EventType.Turno,
            StartDate = new DateOnly(2026, 6, 1),
            IsAllDay = true,
        });

        var exception = await act.Should().ThrowAsync<EventConflictException>();

        exception.Which.Message.Should().Be("Il turno si sovrappone a chiusure o assenze già registrate.");
        exception.Which.Conflicts.Should().ContainSingle(conflict =>
            conflict.Kind == ShiftConflictKind.EventOverlap &&
            conflict.ConflictingEventId == 21);
        tracker.Count.Should().Be(0);
    }
}