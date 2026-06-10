using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class EmployeeRequestServiceTests
{
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<IUtcClock> _clock = new();
    private readonly Mock<IShiftConflictValidator> _conflictValidator = new();

    public EmployeeRequestServiceTests()
    {
        // Default: nessun turno sovrapposto. I test sul caso conflitto sovrascrivono questo setup.
        _conflictValidator
            .Setup(v => v.DetectAssignmentConflictsAsync(
                It.IsAny<int>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<DateOnly>(),
                It.IsAny<TimeOnly?>(), It.IsAny<TimeOnly?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(new List<ShiftConflictDto>());
    }

    [Fact]
    public async Task ApproveAsync_ReturnsNull_WhenRequestDoesNotExist()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.EmployeeRequests, x => [x.Id])
            .Build();
        var service = new EmployeeRequestService(context.Object, _notifications.Object, _clock.Object, _conflictValidator.Object);

        var result = await service.ApproveAsync(999, 7, 101);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ApproveAsync_Throws_WhenRequestAlreadyDecided()
    {
        var employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi", UserId = 55 };
        var requests = new List<EmployeeRequest>
        {
            new()
            {
                Id = 3,
                MerchantId = 7,
                EmployeeId = 11,
                Employee = employee,
                Type = EmployeeRequestType.Ferie,
                Status = RequestStatus.Approved,
                StartDate = new DateOnly(2026, 6, 2),
                CreatedAt = new DateTime(2026, 5, 20, 8, 0, 0, DateTimeKind.Utc)
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EmployeeRequests, requests, x => [x.Id])
            .Build();
        var service = new EmployeeRequestService(context.Object, _notifications.Object, _clock.Object, _conflictValidator.Object);

        var act = () => service.ApproveAsync(3, 7, 101);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Questa richiesta è già stata decisa e non è più modificabile.");
    }

    [Fact]
    public async Task ApproveAsync_UpdatesFields_AndSendsNotification_WhenEmployeeHasUser()
    {
        var now = new DateTime(2026, 5, 24, 9, 0, 0, DateTimeKind.Utc);
        _clock.SetupGet(x => x.UtcNow).Returns(now);

        var employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi", UserId = 55 };
        var requests = new List<EmployeeRequest>
        {
            new()
            {
                Id = 3,
                MerchantId = 7,
                EmployeeId = 11,
                Employee = employee,
                Type = EmployeeRequestType.Permessi,
                Status = RequestStatus.Pending,
                StartDate = new DateOnly(2026, 6, 2),
                EndDate = new DateOnly(2026, 6, 2),
                CreatedAt = new DateTime(2026, 5, 20, 8, 0, 0, DateTimeKind.Utc)
            }
        };

        var review = new ReviewEmployeeRequestRequest
        {
            ReviewNotes = "OK",
            EventId = 777
        };

        var events = new List<Event>();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EmployeeRequests, requests, x => [x.Id])
            .WithSet(x => x.Events, events, x => [x.Id])
            .Build(out var tracker);

        var service = new EmployeeRequestService(context.Object, _notifications.Object, _clock.Object, _conflictValidator.Object);

        var result = await service.ApproveAsync(3, 7, 101, review);

        result.Should().NotBeNull();
        requests[0].Status.Should().Be(RequestStatus.Approved);
        requests[0].ReviewedByUserId.Should().Be(101);
        requests[0].ReviewedAt.Should().Be(now);
        requests[0].UpdatedAt.Should().Be(now);
        requests[0].ReviewNotes.Should().Be("OK");
        requests[0].EventId.Should().BeNull();
        tracker.Count.Should().Be(1);

        _notifications.Verify(x => x.CreateAsync(
            55,
            It.Is<string>(s => s.Contains("approvata")),
            It.Is<string>(s => s.Contains("Note: OK")),
            NotificationType.RequestApproved,
            3),
            Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_Throws_WhenAbsenceOverlapsExistingShift_AndNotForced()
    {
        var now = new DateTime(2026, 5, 24, 9, 0, 0, DateTimeKind.Utc);
        _clock.SetupGet(x => x.UtcNow).Returns(now);

        var employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi", UserId = 55 };
        var requests = new List<EmployeeRequest>
        {
            new()
            {
                Id = 3,
                MerchantId = 7,
                EmployeeId = 11,
                Employee = employee,
                Type = EmployeeRequestType.Ferie,
                Status = RequestStatus.Pending,
                StartDate = new DateOnly(2026, 6, 12),
                EndDate = new DateOnly(2026, 6, 12),
                CreatedAt = now.AddDays(-2)
            }
        };

        // Il validator segnala un turno sovrapposto per la data richiesta.
        _conflictValidator
            .Setup(v => v.DetectAssignmentConflictsAsync(
                7, It.IsAny<IReadOnlyList<int>>(), new DateOnly(2026, 6, 12),
                It.IsAny<TimeOnly?>(), It.IsAny<TimeOnly?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(new List<ShiftConflictDto>
            {
                new() { EmployeeId = 11, Kind = ShiftConflictKind.ShiftOverlap, Message = "Sovrapposizione con turno \"Mattina\"" }
            });

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EmployeeRequests, requests, x => [x.Id])
            .Build(out var tracker);

        var service = new EmployeeRequestService(context.Object, _notifications.Object, _clock.Object, _conflictValidator.Object);

        var act = () => service.ApproveAsync(3, 7, 101);

        var ex = await act.Should().ThrowAsync<EventConflictException>();
        ex.Which.Conflicts.Should().ContainSingle(c => c.Kind == ShiftConflictKind.ShiftOverlap);
        requests[0].Status.Should().Be(RequestStatus.Pending);
        tracker.Count.Should().Be(0);
    }

    [Fact]
    public async Task ApproveAsync_Proceeds_WhenAbsenceOverlapsExistingShift_AndForced()
    {
        var now = new DateTime(2026, 5, 24, 9, 0, 0, DateTimeKind.Utc);
        _clock.SetupGet(x => x.UtcNow).Returns(now);

        var employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi", UserId = 55 };
        var requests = new List<EmployeeRequest>
        {
            new()
            {
                Id = 3,
                MerchantId = 7,
                EmployeeId = 11,
                Employee = employee,
                Type = EmployeeRequestType.Ferie,
                Status = RequestStatus.Pending,
                StartDate = new DateOnly(2026, 6, 12),
                EndDate = new DateOnly(2026, 6, 12),
                CreatedAt = now.AddDays(-2)
            }
        };

        _conflictValidator
            .Setup(v => v.DetectAssignmentConflictsAsync(
                7, It.IsAny<IReadOnlyList<int>>(), new DateOnly(2026, 6, 12),
                It.IsAny<TimeOnly?>(), It.IsAny<TimeOnly?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(new List<ShiftConflictDto>
            {
                new() { EmployeeId = 11, Kind = ShiftConflictKind.ShiftOverlap, Message = "Sovrapposizione con turno \"Mattina\"" }
            });

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EmployeeRequests, requests, x => [x.Id])
            .Build(out var tracker);

        var service = new EmployeeRequestService(context.Object, _notifications.Object, _clock.Object, _conflictValidator.Object);

        var result = await service.ApproveAsync(3, 7, 101, new ReviewEmployeeRequestRequest { Force = true });

        result.Should().NotBeNull();
        requests[0].Status.Should().Be(RequestStatus.Approved);
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task RejectAsync_DoesNotNotify_WhenEmployeeHasNoUser()
    {
        var now = new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc);
        _clock.SetupGet(x => x.UtcNow).Returns(now);

        var employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi", UserId = null };
        var requests = new List<EmployeeRequest>
        {
            new()
            {
                Id = 9,
                MerchantId = 7,
                EmployeeId = 11,
                Employee = employee,
                Type = EmployeeRequestType.Ferie,
                Status = RequestStatus.Pending,
                StartDate = new DateOnly(2026, 7, 1),
                CreatedAt = now.AddDays(-2)
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EmployeeRequests, requests, x => [x.Id])
            .Build(out var tracker);

        var service = new EmployeeRequestService(context.Object, _notifications.Object, _clock.Object, _conflictValidator.Object);

        var result = await service.RejectAsync(9, 7, 101, new ReviewEmployeeRequestRequest { ReviewNotes = "No" });

        result.Should().NotBeNull();
        requests[0].Status.Should().Be(RequestStatus.Rejected);
        tracker.Count.Should().Be(1);
        _notifications.Verify(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRequest_WhenOwnedByEmployeeAndMerchant()
    {
        var requests = new List<EmployeeRequest>
        {
            new()
            {
                Id = 9,
                MerchantId = 7,
                EmployeeId = 11,
                Employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi" },
                Type = EmployeeRequestType.Ferie,
                Status = RequestStatus.Pending,
                StartDate = new DateOnly(2026, 7, 1)
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EmployeeRequests, requests, x => [x.Id])
            .Build(out var tracker);

        var service = new EmployeeRequestService(context.Object, _notifications.Object, _clock.Object, _conflictValidator.Object);

        var deleted = await service.DeleteAsync(9, 11, 7);

        deleted.Should().BeTrue();
        requests.Should().BeEmpty();
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenPermessiHasOnlyOneBoundaryTime()
    {
        var request = new CreateEmployeeRequestRequest
        {
            Type = EmployeeRequestType.Permessi,
            StartDate = new DateOnly(2026, 7, 10),
            EndDate = new DateOnly(2026, 7, 10),
            StartTime = new TimeOnly(9, 0),
            EndTime = null,
            Notes = "permesso"
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.EmployeeRequests, x => [x.Id])
            .Build();

        var service = new EmployeeRequestService(context.Object, _notifications.Object, _clock.Object, _conflictValidator.Object);

        var act = () => service.CreateAsync(11, 7, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Per un permesso orario indica sia l'ora di inizio sia quella di fine, oppure seleziona 'tutto il giorno'.");
    }
}
