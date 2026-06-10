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

    // ----- Validazione coerenza evento (titolo, date, orari) -----

    private (EventService service, SaveChangesTracker tracker) BuildService()
    {
        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsHeadquarters = true };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantBranches, new List<MerchantBranch> { branch }, item => [item.Id])
            .WithEmptySet(x => x.Events, item => [item.Id])
            .WithEmptySet(x => x.Departments, item => [item.Id])
            .WithEmptySet(x => x.EmployeeRequests, item => [item.Id])
            .WithEmptySet(x => x.EmployeeMemberships, item => [item.Id])
            .WithEmptySet(x => x.EmployeeSkills, item => [item.Id])
            .Build(out var tracker);

        _clock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 23, 8, 0, 0, DateTimeKind.Utc));
        _conflictValidator
            .Setup(s => s.DetectAssignmentConflictsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly?>(), It.IsAny<TimeOnly?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(new List<ShiftConflictDto>());

        var service = new EventService(context.Object, _conflictValidator.Object, _notificationService.Object, _clock.Object);
        return (service, tracker);
    }

    private static CreateEventRequest ValidTimedShift() => new()
    {
        BranchId = 3,
        Title = "Turno mattina",
        EventType = EventType.Turno,
        StartDate = new DateOnly(2026, 6, 1),
        IsAllDay = false,
        StartTime = new TimeOnly(9, 0),
        EndTime = new TimeOnly(17, 0),
    };

    [Fact]
    public async Task CreateAsync_Throws_WhenTitleIsBlank()
    {
        var (service, tracker) = BuildService();
        var request = ValidTimedShift();
        request.Title = "   ";

        var act = () => service.CreateAsync(7, 12, request);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Be("Il titolo è obbligatorio.");
        tracker.Count.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenTimedShiftHasNoTimes()
    {
        var (service, tracker) = BuildService();
        var request = ValidTimedShift();
        request.StartTime = null;
        request.EndTime = null;

        var act = () => service.CreateAsync(7, 12, request);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("Tutto il giorno");
        tracker.Count.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenEndTimeNotAfterStartTime()
    {
        var (service, tracker) = BuildService();
        var request = ValidTimedShift();
        request.StartTime = new TimeOnly(17, 0);
        request.EndTime = new TimeOnly(9, 0);

        var act = () => service.CreateAsync(7, 12, request);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Be("L'orario di fine deve essere successivo a quello di inizio.");
        tracker.Count.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenEndDateBeforeStartDate()
    {
        var (service, tracker) = BuildService();
        var request = ValidTimedShift();
        request.EndDate = new DateOnly(2026, 5, 31);

        var act = () => service.CreateAsync(7, 12, request);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Be("La data di fine non può essere precedente alla data di inizio.");
        tracker.Count.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_DoesNotThrowValidationError_ForValidTimedShift()
    {
        var (service, tracker) = BuildService();

        // La persistenza completa (ReloadEventNavigation) non è verificabile sul
        // DbContext mockato; qui ci basta confermare che un turno valido SUPERA i
        // controlli di coerenza — quindi NON lancia InvalidOperationException.
        var act = () => service.CreateAsync(7, 12, ValidTimedShift());

        await act.Should().NotThrowAsync<InvalidOperationException>();
        tracker.Count.Should().BeGreaterThan(0);
    }
}