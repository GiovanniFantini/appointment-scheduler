using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class TimeClockServiceTests
{
    private readonly Mock<IUtcClock> _utcClock = new();
    private readonly Mock<IWallClock> _wallClock = new();
    private readonly Mock<INotificationService> _notifications = new();

    [Fact]
    public async Task GetMyEntriesAsync_ReturnsMappedEntriesInDescendingOrder()
    {
        var employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi" };
        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var evt = new Event
        {
            Id = 100,
            MerchantId = 7,
            BranchId = 3,
            Branch = branch,
            EventType = EventType.Turno,
            Title = "Turno mattina",
            StartDate = new DateOnly(2026, 5, 24)
        };

        var entries = new List<TimeEntry>
        {
            new()
            {
                Id = 1,
                MerchantId = 7,
                BranchId = 3,
                Branch = branch,
                EmployeeId = 11,
                Employee = employee,
                EventId = 100,
                Event = evt,
                EventParticipantId = 501,
                Type = TimeEntryType.ClockIn,
                WorkDate = new DateOnly(2026, 5, 24),
                ActualTimestampUtc = new DateTime(2026, 5, 24, 7, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2026, 5, 24, 7, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = 2,
                MerchantId = 7,
                BranchId = 3,
                Branch = branch,
                EmployeeId = 11,
                Employee = employee,
                EventId = 100,
                Event = evt,
                EventParticipantId = 501,
                Type = TimeEntryType.ClockOut,
                WorkDate = new DateOnly(2026, 5, 24),
                ActualTimestampUtc = new DateTime(2026, 5, 24, 15, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2026, 5, 24, 15, 0, 0, DateTimeKind.Utc)
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.TimeEntries, entries, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetMyEntriesAsync(11, 7, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().ContainInOrder(2, 1);
        result[0].BranchName.Should().Be("HQ");
        result[0].EventTitle.Should().Be("Turno mattina");
    }

    [Fact]
    public async Task GetEntriesAsync_AppliesBranchAndEmployeeFilters()
    {
        var entries = new List<TimeEntry>
        {
            new() { Id = 1, MerchantId = 7, BranchId = 3, EmployeeId = 11, EventId = 100, EventParticipantId = 1, WorkDate = new DateOnly(2026, 5, 24), ActualTimestampUtc = new DateTime(2026, 5, 24, 8, 0, 0, DateTimeKind.Utc), Branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ" }, Employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi" }, Event = new Event { Id = 100, MerchantId = 7, BranchId = 3, Title = "A" } },
            new() { Id = 2, MerchantId = 7, BranchId = 4, EmployeeId = 11, EventId = 101, EventParticipantId = 2, WorkDate = new DateOnly(2026, 5, 24), ActualTimestampUtc = new DateTime(2026, 5, 24, 9, 0, 0, DateTimeKind.Utc), Branch = new MerchantBranch { Id = 4, MerchantId = 7, Name = "Store" }, Employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi" }, Event = new Event { Id = 101, MerchantId = 7, BranchId = 4, Title = "B" } },
            new() { Id = 3, MerchantId = 7, BranchId = 3, EmployeeId = 12, EventId = 102, EventParticipantId = 3, WorkDate = new DateOnly(2026, 5, 24), ActualTimestampUtc = new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc), Branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ" }, Employee = new Employee { Id = 12, FirstName = "Luca", LastName = "Verdi" }, Event = new Event { Id = 102, MerchantId = 7, BranchId = 3, Title = "C" } }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.TimeEntries, entries, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetEntriesAsync(7, branchId: 3, from: new DateOnly(2026, 5, 1), to: new DateOnly(2026, 5, 31), employeeId: 11);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task GetCurrentStatusAsync_ReturnsNoShiftMessage_WhenNoShiftIsAvailable()
    {
        var now = new DateTime(2026, 5, 24, 9, 0, 0, DateTimeKind.Utc);
        _utcClock.SetupGet(x => x.UtcNow).Returns(now);
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 11, 0, 0, DateTimeKind.Unspecified));

        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.EventParticipants, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetCurrentStatusAsync(11, 7);

        result.StatusMessage.Should().Be("Nessun turno in programma.");
        result.TimeClockEnabled.Should().BeFalse();
        result.TodayEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task ClockInAsync_Throws_WhenNoShiftIsAvailable()
    {
        var now = new DateTime(2026, 5, 24, 9, 0, 0, DateTimeKind.Utc);
        _utcClock.SetupGet(x => x.UtcNow).Returns(now);
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 11, 0, 0, DateTimeKind.Unspecified));

        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.EventParticipants, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var act = () => service.ClockInAsync(11, 7, new ClockActionRequest());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Nessun turno disponibile per la timbratura. È possibile timbrare solo su un turno pianificato.");
    }

    [Fact]
    public async Task ClockOutAsync_Throws_WhenSequenceIsInvalid()
    {
        var nowUtc = new DateTime(2026, 5, 24, 9, 0, 0, DateTimeKind.Utc);
        _utcClock.SetupGet(x => x.UtcNow).Returns(nowUtc);
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 11, 0, 0, DateTimeKind.Unspecified));

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var shift = new Event
        {
            Id = 100,
            MerchantId = 7,
            BranchId = 3,
            Branch = branch,
            EventType = EventType.Turno,
            Title = "Turno",
            StartDate = new DateOnly(2026, 5, 24),
            StartTime = new TimeOnly(9, 0),
            EndDate = new DateOnly(2026, 5, 24),
            EndTime = new TimeOnly(17, 0)
        };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = shift, EmployeeId = 11 }
        };

        var settings = new List<BranchTimeClockSettings>
        {
            new() { Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var act = () => service.ClockOutAsync(11, 7, new ClockActionRequest());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Devi prima timbrare l'entrata.");
    }

    [Fact]
    public async Task GetSettingsAsync_Throws_WhenBranchIsMissing()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.MerchantBranches, x => [x.Id])
            .WithEmptySet(x => x.BranchTimeClockSettings, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var act = () => service.GetSettingsAsync(3, 7);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Filiale non trovata.");
    }

    [Fact]
    public async Task UpdateSettingsAsync_Throws_WhenAnyValueIsNegative()
    {
        var branches = new List<MerchantBranch>
        {
            new() { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantBranches, branches, x => [x.Id])
            .WithEmptySet(x => x.BranchTimeClockSettings, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var act = () => service.UpdateSettingsAsync(3, 7, new UpdateTimeClockSettingsRequest
        {
            IsEnabled = true,
            GraceInMinutes = -1,
            GraceOutMinutes = 5,
            EarlyClockInToleranceMinutes = 5,
            LateClockOutToleranceMinutes = 5,
            GeofenceRadiusMeters = 150,
            MaxBreakMinutes = 30,
            RoundingMinutes = 0
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("I valori di configurazione non possono essere negativi.");
    }

    [Fact]
    public async Task RunMissingPunchDetection_CreatesOnlyForInternalEmployees()
    {
        var now = new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc);
        _utcClock.SetupGet(x => x.UtcNow).Returns(now);

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var pastShift = new Event
        {
            Id = 100,
            MerchantId = 7,
            BranchId = 3,
            Branch = branch,
            EventType = EventType.Turno,
            StartDate = new DateOnly(2026, 5, 23)
        };

        var internalEmployee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi", Kind = EmployeeKind.Internal };
        var externalEmployee = new Employee { Id = 12, FirstName = "Luca", LastName = "Verdi", Kind = EmployeeKind.External };

        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = pastShift, EmployeeId = 11, Employee = internalEmployee },
            new() { Id = 502, EventId = 100, Event = pastShift, EmployeeId = 12, Employee = externalEmployee }
        };

        var anomalies = new List<TimeClockAnomaly>();
        var settings = new List<BranchTimeClockSettings>
        {
            new() { Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true, ClockingRequired = true, ClockingRequiredSince = new DateOnly(2026, 5, 1) }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .Build(out var tracker);

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var created = await service.RunMissingPunchDetectionAsync(7, null);

        created.Should().Be(1);
        anomalies.Should().ContainSingle(a => a.EmployeeId == 11 && a.Type == TimeClockAnomalyType.MissingClockIn);
        anomalies.Should().NotContain(a => a.EmployeeId == 12);
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task JustifyAnomalyAsync_Throws_WhenAnomalyAlreadyReviewed()
    {
        var anomalies = new List<TimeClockAnomaly>
        {
            new()
            {
                Id = 1,
                MerchantId = 7,
                EmployeeId = 11,
                Status = TimeClockAnomalyStatus.Approved,
                WorkDate = new DateOnly(2026, 5, 23),
                Employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi" }
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var act = () => service.JustifyAnomalyAsync(1, 11, 7, new JustifyAnomalyRequest { Reason = TimeClockAnomalyReason.PersonalEmergency });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Questa anomalia è già stata giustificata o revisionata.");
    }

    [Fact]
    public async Task ApproveAnomalyAsync_UpdatesReviewFields_WhenAnomalyIsOpen()
    {
        var now = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);
        _utcClock.SetupGet(x => x.UtcNow).Returns(now);

        var anomalies = new List<TimeClockAnomaly>
        {
            new()
            {
                Id = 7,
                MerchantId = 7,
                EmployeeId = 11,
                Status = TimeClockAnomalyStatus.Open,
                Type = TimeClockAnomalyType.LateClockIn,
                WorkDate = new DateOnly(2026, 5, 24),
                Employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi" },
                Event = new Event { Id = 100, MerchantId = 7, BranchId = 3, Title = "Turno" }
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .Build(out var tracker);

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.ApproveAnomalyAsync(7, 7, 101, new ReviewAnomalyRequest { ReviewNotes = "Giustificata" });

        result.Status.Should().Be(TimeClockAnomalyStatus.Approved);
        anomalies[0].ReviewedByUserId.Should().Be(101);
        anomalies[0].ReviewedAt.Should().Be(now);
        anomalies[0].UpdatedAt.Should().Be(now);
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task JustifyAnomalyAsync_LeavesAnomalyPending_AndNotifiesReviewers()
    {
        var now = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);
        _utcClock.SetupGet(x => x.UtcNow).Returns(now);

        var anomalies = new List<TimeClockAnomaly>
        {
            new()
            {
                Id = 7,
                MerchantId = 7,
                EmployeeId = 11,
                Status = TimeClockAnomalyStatus.Open,
                Type = TimeClockAnomalyType.MissingClockIn,
                WorkDate = new DateOnly(2026, 5, 23),
                Employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi", UserId = 55 }
            }
        };

        var managerRole = new MerchantRole
        {
            Id = 1,
            MerchantId = 7,
            Features =
            [
                new RoleFeature { Id = 1, RoleId = 1, Feature = MerchantFeature.Timbratura, IsEnabled = true, AccessLevel = FeatureAccessLevel.Manager }
            ]
        };
        var operatorRole = new MerchantRole
        {
            Id = 2,
            MerchantId = 7,
            Features =
            [
                new RoleFeature { Id = 2, RoleId = 2, Feature = MerchantFeature.Timbratura, IsEnabled = true, AccessLevel = FeatureAccessLevel.Operator }
            ]
        };

        var memberships = new List<EmployeeMembership>
        {
            new() { Id = 1, MerchantId = 7, EmployeeId = 20, RoleId = 1, Role = managerRole, IsActive = true, Employee = new Employee { Id = 20, UserId = 90 } },
            new() { Id = 2, MerchantId = 7, EmployeeId = 21, RoleId = 2, Role = operatorRole, IsActive = true, Employee = new Employee { Id = 21, UserId = 91 } }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .WithSet(x => x.EmployeeMemberships, memberships, x => [x.Id])
            .WithSet(x => x.Merchants, [new Merchant { Id = 7, UserId = 1 }], x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.JustifyAnomalyAsync(
            7, 11, 7, new JustifyAnomalyRequest { Reason = TimeClockAnomalyReason.Forgotten, Notes = "Ho dimenticato" });

        result.Status.Should().Be(TimeClockAnomalyStatus.Justified);
        _notifications.Verify(x => x.CreateAsync(90, It.IsAny<string>(), It.IsAny<string>(), NotificationType.RequestSubmitted, 7), Times.Once);
        _notifications.Verify(x => x.CreateAsync(1, It.IsAny<string>(), It.IsAny<string>(), NotificationType.RequestSubmitted, 7), Times.Once);
        _notifications.Verify(x => x.CreateAsync(91, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task RejectAnomalyAsync_RegistersApprovedLeave_ForMissingClockIn()
    {
        var now = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);
        _utcClock.SetupGet(x => x.UtcNow).Returns(now);

        var anomalies = new List<TimeClockAnomaly>
        {
            new()
            {
                Id = 7,
                MerchantId = 7,
                EmployeeId = 11,
                EventId = 100,
                Status = TimeClockAnomalyStatus.Justified,
                Type = TimeClockAnomalyType.MissingClockIn,
                WorkDate = new DateOnly(2026, 5, 23),
                Employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi", UserId = 55 }
            }
        };
        var requests = new List<EmployeeRequest>();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .WithSet(x => x.EmployeeRequests, requests, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.RejectAnomalyAsync(7, 7, 101, new ReviewAnomalyRequest { ReviewNotes = "Assenza non giustificata" });

        result.Status.Should().Be(TimeClockAnomalyStatus.Rejected);
        requests.Should().ContainSingle();
        requests[0].Type.Should().Be(EmployeeRequestType.Ferie);
        requests[0].Status.Should().Be(RequestStatus.Approved);
        requests[0].StartDate.Should().Be(new DateOnly(2026, 5, 23));
        requests[0].EndDate.Should().Be(new DateOnly(2026, 5, 23));
        requests[0].EventId.Should().Be(100);
        requests[0].ReviewedByUserId.Should().Be(101);
        _notifications.Verify(x => x.CreateAsync(55, It.IsAny<string>(), It.IsAny<string>(), NotificationType.RequestRejected, 7), Times.Once);
    }

    [Fact]
    public async Task RejectAnomalyAsync_DoesNotRegisterLeave_ForMissingClockOut()
    {
        var now = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);
        _utcClock.SetupGet(x => x.UtcNow).Returns(now);

        var anomalies = new List<TimeClockAnomaly>
        {
            new()
            {
                Id = 7,
                MerchantId = 7,
                EmployeeId = 11,
                Status = TimeClockAnomalyStatus.Justified,
                Type = TimeClockAnomalyType.MissingClockOut,
                WorkDate = new DateOnly(2026, 5, 23),
                Employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi", UserId = 55 }
            }
        };
        var requests = new List<EmployeeRequest>();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .WithSet(x => x.EmployeeRequests, requests, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        await service.RejectAnomalyAsync(7, 7, 101, new ReviewAnomalyRequest());

        requests.Should().BeEmpty();
    }

    [Fact]
    public async Task RejectAnomalyAsync_DoesNotDuplicateLeave_WhenDayAlreadyCoveredByRequest()
    {
        var now = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);
        _utcClock.SetupGet(x => x.UtcNow).Returns(now);

        var anomalies = new List<TimeClockAnomaly>
        {
            new()
            {
                Id = 7,
                MerchantId = 7,
                EmployeeId = 11,
                Status = TimeClockAnomalyStatus.Justified,
                Type = TimeClockAnomalyType.MissingClockIn,
                WorkDate = new DateOnly(2026, 5, 23),
                Employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi", UserId = 55 }
            }
        };
        var requests = new List<EmployeeRequest>
        {
            new()
            {
                Id = 1,
                MerchantId = 7,
                EmployeeId = 11,
                Type = EmployeeRequestType.Malattia,
                Status = RequestStatus.Approved,
                StartDate = new DateOnly(2026, 5, 22),
                EndDate = new DateOnly(2026, 5, 25)
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .WithSet(x => x.EmployeeRequests, requests, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        await service.RejectAnomalyAsync(7, 7, 101, new ReviewAnomalyRequest());

        requests.Should().ContainSingle();
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsDefaultSettings_WhenNoOverrideExists()
    {
        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantBranches, new List<MerchantBranch> { branch }, x => [x.Id])
            .WithEmptySet(x => x.BranchTimeClockSettings, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetSettingsAsync(3, 7);

        result.BranchId.Should().Be(3);
        result.IsEnabled.Should().BeFalse();
        result.GeofencingEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateSettingsAsync_CreatesNewSettingsAndUpdatesBranchCoordinates()
    {
        var now = new DateTime(2026, 5, 24, 13, 0, 0, DateTimeKind.Utc);
        _utcClock.SetupGet(x => x.UtcNow).Returns(now);

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var settings = new List<BranchTimeClockSettings>();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantBranches, new List<MerchantBranch> { branch }, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .Build(out var tracker);

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.UpdateSettingsAsync(3, 7, new UpdateTimeClockSettingsRequest
        {
            IsEnabled = true,
            ClockingRequired = true,
            GraceInMinutes = 5,
            GraceOutMinutes = 5,
            EarlyClockInToleranceMinutes = 10,
            LateClockOutToleranceMinutes = 10,
            GeofencingEnabled = true,
            GeofenceRadiusMeters = 150,
            BreakTrackingEnabled = true,
            MaxBreakMinutes = 45,
            RoundingMinutes = 5,
            RequirePhoto = false,
            BranchLatitude = 45.123,
            BranchLongitude = 9.456
        });

        result.IsEnabled.Should().BeTrue();
        settings.Should().ContainSingle();
        branch.Latitude.Should().Be(45.123);
        branch.Longitude.Should().Be(9.456);
        branch.UpdatedAt.Should().Be(now);
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task UpdateSettingsAsync_SetsClockingRequiredSince_OnlyWhenObligationStarts()
    {
        // L'obbligo decorre dal giorno in cui viene acceso; i salvataggi successivi
        // di altre impostazioni non devono spostarne la decorrenza in avanti,
        // altrimenti i turni già coperti tornerebbero fuori perimetro.
        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var settings = new List<BranchTimeClockSettings>();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantBranches, new List<MerchantBranch> { branch }, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 13, 0, 0, DateTimeKind.Utc));
        var enabled = await service.UpdateSettingsAsync(3, 7, RequiredSettingsRequest());
        enabled.ClockingRequiredSince.Should().Be(new DateOnly(2026, 5, 24));

        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 30, 13, 0, 0, DateTimeKind.Utc));
        var resaved = await service.UpdateSettingsAsync(3, 7, RequiredSettingsRequest());
        resaved.ClockingRequiredSince.Should().Be(new DateOnly(2026, 5, 24));
    }

    [Fact]
    public async Task UpdateSettingsAsync_ClearsClockingRequiredSince_WhenObligationIsRemoved()
    {
        // Tolto l'obbligo la decorrenza si azzera: se verrà riattivato ripartirà
        // dalla nuova data, senza segnalare i turni del periodo di sospensione.
        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var settings = new List<BranchTimeClockSettings>
        {
            new()
            {
                Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true,
                ClockingRequired = true, ClockingRequiredSince = new DateOnly(2026, 5, 1)
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantBranches, new List<MerchantBranch> { branch }, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 13, 0, 0, DateTimeKind.Utc));

        var request = RequiredSettingsRequest();
        request.ClockingRequired = false;
        var result = await service.UpdateSettingsAsync(3, 7, request);

        result.ClockingRequiredSince.Should().BeNull();
        settings[0].ClockingRequiredSince.Should().BeNull();
    }

    private static UpdateTimeClockSettingsRequest RequiredSettingsRequest() => new()
    {
        IsEnabled = true,
        ClockingRequired = true,
        GraceInMinutes = 5,
        GraceOutMinutes = 5,
        EarlyClockInToleranceMinutes = 10,
        LateClockOutToleranceMinutes = 10,
        GeofencingEnabled = false,
        GeofenceRadiusMeters = 150,
        BreakTrackingEnabled = true,
        MaxBreakMinutes = 45,
        RoundingMinutes = 0,
        RequirePhoto = false
    };

    [Fact]
    public async Task CreateManualEntryAsync_Throws_WhenParticipantIsMissing()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.EventParticipants, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var act = () => service.CreateManualEntryAsync(7, 101, new CreateManualEntryRequest
        {
            EventParticipantId = 777,
            Type = TimeEntryType.ClockIn,
            ActualTimestampUtc = new DateTime(2026, 5, 24, 8, 0, 0, DateTimeKind.Utc)
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Partecipazione al turno non trovata.");
    }

    [Fact]
    public async Task CreateManualEntryAsync_Throws_WhenParticipantEventIsNotTurno()
    {
        var eventParticipant = new EventParticipant
        {
            Id = 501,
            EmployeeId = 11,
            EventId = 100,
            Event = new Event
            {
                Id = 100,
                MerchantId = 7,
                BranchId = 3,
                EventType = EventType.Ferie,
                StartDate = new DateOnly(2026, 5, 24)
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, new List<EventParticipant> { eventParticipant }, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var act = () => service.CreateManualEntryAsync(7, 101, new CreateManualEntryRequest
        {
            EventParticipantId = 501,
            Type = TimeEntryType.ClockIn,
            ActualTimestampUtc = new DateTime(2026, 5, 24, 8, 0, 0, DateTimeKind.Utc)
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("La timbratura si applica solo ai turni.");
    }

    [Fact]
    public async Task GetReportAsync_ComputesWorkedBreakAndOpenAnomalyFlag()
    {
        var now = new DateTime(2026, 5, 24, 18, 0, 0, DateTimeKind.Utc);
        _utcClock.SetupGet(x => x.UtcNow).Returns(now);

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi" };
        var evt = new Event
        {
            Id = 100,
            MerchantId = 7,
            BranchId = 3,
            Branch = branch,
            EventType = EventType.Turno,
            Title = "Turno mattina",
            StartDate = new DateOnly(2026, 5, 24),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        };
        var participant = new EventParticipant
        {
            Id = 501,
            EventId = 100,
            Event = evt,
            EmployeeId = 11,
            Employee = employee,
            StartTimeOverride = new TimeOnly(9, 0),
            EndTimeOverride = new TimeOnly(17, 0)
        };

        var entries = new List<TimeEntry>
        {
            new() { Id = 1, MerchantId = 7, BranchId = 3, Branch = branch, EmployeeId = 11, Employee = employee, EventId = 100, Event = evt, EventParticipantId = 501, EventParticipant = participant, Type = TimeEntryType.ClockIn, WorkDate = new DateOnly(2026, 5, 24), ActualTimestampUtc = new DateTime(2026, 5, 24, 9, 0, 0, DateTimeKind.Utc), CreatedAt = new DateTime(2026, 5, 24, 9, 0, 0, DateTimeKind.Utc) },
            new() { Id = 2, MerchantId = 7, BranchId = 3, Branch = branch, EmployeeId = 11, Employee = employee, EventId = 100, Event = evt, EventParticipantId = 501, EventParticipant = participant, Type = TimeEntryType.BreakStart, WorkDate = new DateOnly(2026, 5, 24), ActualTimestampUtc = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc), CreatedAt = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc) },
            new() { Id = 3, MerchantId = 7, BranchId = 3, Branch = branch, EmployeeId = 11, Employee = employee, EventId = 100, Event = evt, EventParticipantId = 501, EventParticipant = participant, Type = TimeEntryType.BreakEnd, WorkDate = new DateOnly(2026, 5, 24), ActualTimestampUtc = new DateTime(2026, 5, 24, 12, 30, 0, DateTimeKind.Utc), CreatedAt = new DateTime(2026, 5, 24, 12, 30, 0, DateTimeKind.Utc) },
            new() { Id = 4, MerchantId = 7, BranchId = 3, Branch = branch, EmployeeId = 11, Employee = employee, EventId = 100, Event = evt, EventParticipantId = 501, EventParticipant = participant, Type = TimeEntryType.ClockOut, WorkDate = new DateOnly(2026, 5, 24), ActualTimestampUtc = new DateTime(2026, 5, 24, 17, 0, 0, DateTimeKind.Utc), CreatedAt = new DateTime(2026, 5, 24, 17, 0, 0, DateTimeKind.Utc) }
        };

        var anomalies = new List<TimeClockAnomaly>
        {
            new()
            {
                Id = 10,
                MerchantId = 7,
                EmployeeId = 11,
                EventParticipantId = 501,
                Status = TimeClockAnomalyStatus.Open,
                Type = TimeClockAnomalyType.LateClockIn,
                WorkDate = new DateOnly(2026, 5, 24)
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.TimeEntries, entries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var report = await service.GetReportAsync(7, 3, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

        report.Should().ContainSingle();
        report[0].WorkedMinutes.Should().Be(450);
        report[0].BreakMinutes.Should().Be(30);
        report[0].ScheduledMinutes.Should().Be(480);
        report[0].OvertimeMinutes.Should().Be(0);
        report[0].HasOpenAnomaly.Should().BeTrue();
    }

    [Fact]
    public async Task GetAnomaliesAsync_AppliesBranchAndStatusFilters()
    {
        var anomalies = new List<TimeClockAnomaly>
        {
            new() { Id = 1, MerchantId = 7, EmployeeId = 11, Status = TimeClockAnomalyStatus.Open, Type = TimeClockAnomalyType.LateClockIn, Severity = 2, WorkDate = new DateOnly(2026, 5, 24), CreatedAt = new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc), Employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi" }, Event = new Event { Id = 100, MerchantId = 7, BranchId = 3, Title = "A" } },
            new() { Id = 2, MerchantId = 7, EmployeeId = 12, Status = TimeClockAnomalyStatus.Justified, Type = TimeClockAnomalyType.EarlyClockOut, Severity = 1, WorkDate = new DateOnly(2026, 5, 24), CreatedAt = new DateTime(2026, 5, 24, 9, 0, 0, DateTimeKind.Utc), Employee = new Employee { Id = 12, FirstName = "Luca", LastName = "Verdi" }, Event = new Event { Id = 101, MerchantId = 7, BranchId = 4, Title = "B" } }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetAnomaliesAsync(7, branchId: 3, status: TimeClockAnomalyStatus.Open);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task GetMyAnomaliesAsync_FiltersByStatus()
    {
        var anomalies = new List<TimeClockAnomaly>
        {
            new() { Id = 1, MerchantId = 7, EmployeeId = 11, Status = TimeClockAnomalyStatus.Open, Type = TimeClockAnomalyType.LateClockIn, Severity = 2, WorkDate = new DateOnly(2026, 5, 24), CreatedAt = new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc), Employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi" }, Event = new Event { Id = 100, MerchantId = 7, BranchId = 3, Title = "A" } },
            new() { Id = 2, MerchantId = 7, EmployeeId = 11, Status = TimeClockAnomalyStatus.Rejected, Type = TimeClockAnomalyType.EarlyClockOut, Severity = 1, WorkDate = new DateOnly(2026, 5, 24), CreatedAt = new DateTime(2026, 5, 24, 9, 0, 0, DateTimeKind.Utc), Employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi" }, Event = new Event { Id = 101, MerchantId = 7, BranchId = 3, Title = "B" } }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetMyAnomaliesAsync(11, 7, TimeClockAnomalyStatus.Open);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(1);
    }

    // ----- Deviazione timbratura (fuso orario) -----

    [Fact]
    public async Task ClockInAsync_ComputesDeviationInWallClock_NotProcessTimeZone()
    {
        // Scenario reale del bug: server in UTC, azienda in UTC+2 (ora legale).
        // Turno alle 14:25, entrata reale alle 14:38 ora di parete → la deviazione
        // corretta è +13 minuti (in ritardo), NON -107 (che usciva confrontando
        // l'ora UTC del processo con l'orario di parete del turno).
        var nowUtc = new DateTime(2026, 6, 10, 12, 38, 0, DateTimeKind.Utc);   // 14:38 in UTC+2
        var nowWall = new DateTime(2026, 6, 10, 14, 38, 0, DateTimeKind.Unspecified);
        _utcClock.SetupGet(x => x.UtcNow).Returns(nowUtc);
        _wallClock.SetupGet(x => x.Now).Returns(nowWall);

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var shift = new Event
        {
            Id = 100, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            Title = "Pomeriggio", StartDate = new DateOnly(2026, 6, 10), EndDate = new DateOnly(2026, 6, 10),
            StartTime = new TimeOnly(14, 25), EndTime = new TimeOnly(17, 0),
        };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = shift, EmployeeId = 11, Employee = new Employee { Id = 11, FirstName = "Mario", LastName = "Rossi" } }
        };
        var settings = new List<BranchTimeClockSettings>
        {
            new() { Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true, GraceInMinutes = 5, EarlyClockInToleranceMinutes = 15 }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithSet(x => x.TimeEntries, new List<TimeEntry>(), x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, new List<TimeClockAnomaly>(), x => [x.Id])
            .WithSet(x => x.MerchantBranches, new List<MerchantBranch> { branch }, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.ClockInAsync(11, 7, new ClockActionRequest());

        result.Entry.DeviationMinutes.Should().Be(13);
        // +13 > GraceInMinutes(5) → anomalia di ritardo, NON di anticipo.
        result.Anomaly.Should().NotBeNull();
        result.Anomaly!.Type.Should().Be(TimeClockAnomalyType.LateClockIn);
    }

    // ----- Turni multipli nello stesso giorno -----

    private static (MerchantBranch branch, Event morning, Event afternoon, List<EventParticipant> participants, List<BranchTimeClockSettings> settings)
        TwoShiftsSameDay()
    {
        var today = new DateOnly(2026, 5, 24);
        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };

        var morning = new Event
        {
            Id = 100, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            Title = "Mattina", StartDate = today, EndDate = today,
            StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(13, 0),
        };
        var afternoon = new Event
        {
            Id = 101, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            Title = "Pomeriggio", StartDate = today, EndDate = today,
            StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(18, 0),
        };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = morning, EmployeeId = 11 },
            new() { Id = 502, EventId = 101, Event = afternoon, EmployeeId = 11 },
        };
        var settings = new List<BranchTimeClockSettings>
        {
            new() { Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true, EarlyClockInToleranceMinutes = 15 },
        };
        return (branch, morning, afternoon, participants, settings);
    }

    [Fact]
    public async Task GetTodayShiftsAsync_ReturnsAllShifts_AndActivatesTheOneInItsWindow()
    {
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 13, 0, 0, DateTimeKind.Utc));
        // Ora di parete = 15:00 → siamo nella finestra del turno pomeridiano.
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 15, 0, 0, DateTimeKind.Unspecified));

        var (_, _, _, participants, settings) = TwoShiftsSameDay();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetTodayShiftsAsync(11, 7);

        result.TimeClockEnabled.Should().BeTrue();
        result.Shifts.Should().HaveCount(2);
        result.Shifts.Select(s => s.Shift.Title).Should().ContainInOrder("Mattina", "Pomeriggio");
        result.ActiveEventParticipantId.Should().Be(502); // pomeriggio
        result.Shifts.Single(s => s.Shift.EventParticipantId == 502).IsActive.Should().BeTrue();
        result.Shifts.Single(s => s.Shift.EventParticipantId == 501).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetTodayShiftsAsync_PrioritisesOpenShift_OverTimeWindow()
    {
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 13, 0, 0, DateTimeKind.Utc));
        // Ora di parete = 15:00: per la finestra sarebbe attivo il pomeriggio, ma
        // la mattina è ancora aperta (entrata senza uscita) → ha la priorità.
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 15, 0, 0, DateTimeKind.Unspecified));

        var (_, _, _, participants, settings) = TwoShiftsSameDay();

        var entries = new List<TimeEntry>
        {
            new() { Id = 1, MerchantId = 7, BranchId = 3, EmployeeId = 11, EventId = 100, EventParticipantId = 501, Type = TimeEntryType.ClockIn, WorkDate = new DateOnly(2026, 5, 24), ActualTimestampUtc = new DateTime(2026, 5, 24, 7, 0, 0, DateTimeKind.Utc) },
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithSet(x => x.TimeEntries, entries, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetTodayShiftsAsync(11, 7);

        result.ActiveEventParticipantId.Should().Be(501); // mattina, ancora aperta
        var morning = result.Shifts.Single(s => s.Shift.EventParticipantId == 501);
        morning.IsClockedIn.Should().BeTrue();
        morning.IsActive.Should().BeTrue();
        morning.SuggestedAction.Should().Be("Timbra l'uscita");
    }

    [Fact]
    public async Task GetTodayShiftsAsync_MarksCompletedShift_AndDeactivatesIt()
    {
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 13, 0, 0, DateTimeKind.Utc));
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 15, 0, 0, DateTimeKind.Unspecified));

        var (_, _, _, participants, settings) = TwoShiftsSameDay();

        // Mattina conclusa (in+out); pomeriggio non ancora iniziato.
        var entries = new List<TimeEntry>
        {
            new() { Id = 1, MerchantId = 7, BranchId = 3, EmployeeId = 11, EventId = 100, EventParticipantId = 501, Type = TimeEntryType.ClockIn, WorkDate = new DateOnly(2026, 5, 24), ActualTimestampUtc = new DateTime(2026, 5, 24, 7, 0, 0, DateTimeKind.Utc) },
            new() { Id = 2, MerchantId = 7, BranchId = 3, EmployeeId = 11, EventId = 100, EventParticipantId = 501, Type = TimeEntryType.ClockOut, WorkDate = new DateOnly(2026, 5, 24), ActualTimestampUtc = new DateTime(2026, 5, 24, 11, 0, 0, DateTimeKind.Utc) },
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithSet(x => x.TimeEntries, entries, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetTodayShiftsAsync(11, 7);

        var morning = result.Shifts.Single(s => s.Shift.EventParticipantId == 501);
        morning.IsCompleted.Should().BeTrue();
        morning.IsActive.Should().BeFalse();
        // Con la mattina conclusa, l'attivo è il pomeriggio (nella sua finestra).
        result.ActiveEventParticipantId.Should().Be(502);
    }

    [Fact]
    public async Task GetTodayShiftsAsync_ReportsDisabled_WhenBranchClockingOff()
    {
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 13, 0, 0, DateTimeKind.Utc));
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 15, 0, 0, DateTimeKind.Unspecified));

        var (_, _, _, participants, _) = TwoShiftsSameDay();
        // Nessuna riga BranchTimeClockSettings → default IsEnabled = false.
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithEmptySet(x => x.BranchTimeClockSettings, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetTodayShiftsAsync(11, 7);

        result.Shifts.Should().HaveCount(2);
        result.TimeClockEnabled.Should().BeFalse();
        // Con la timbratura spenta nessun turno è azionabile.
        result.Shifts.Should().OnlyContain(s => s.IsActive == false);
    }

    [Fact]
    public async Task GetTodayShiftsAsync_ShiftOutsideWindow_IsNotActive_AndWaiting()
    {
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 6, 0, 0, DateTimeKind.Utc));
        // Ora di parete = 08:00: il pomeriggio (14:00) è fuori finestra; la mattina
        // (09:00, tolleranza 15') inizia alle 08:45 → alle 08:00 ancora in attesa.
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 8, 0, 0, DateTimeKind.Unspecified));

        var (_, _, _, participants, settings) = TwoShiftsSameDay();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetTodayShiftsAsync(11, 7);

        var afternoon = result.Shifts.Single(s => s.Shift.EventParticipantId == 502);
        afternoon.IsActive.Should().BeFalse();
        afternoon.StatusMessage.Should().Be("In attesa dell'orario di inizio.");
        // La mattina è quella più vicina ma fuori finestra → non ancora attiva.
        result.Shifts.Single(s => s.Shift.EventParticipantId == 501).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetTodayShiftsAsync_IncludesYesterdayShift_StillOpenAcrossMidnight()
    {
        // "Oggi" = 25/05 01:00. Un turno notturno iniziato il 24/05 alle 22:00,
        // con entrata ma senza uscita, deve comparire ed essere quello attivo.
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc));
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 25, 1, 0, 0, DateTimeKind.Unspecified));

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var night = new Event
        {
            Id = 100, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            Title = "Notturno", StartDate = new DateOnly(2026, 5, 24), EndDate = new DateOnly(2026, 5, 25),
            StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(6, 0),
        };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = night, EmployeeId = 11, Employee = new Employee { Id = 11, Kind = EmployeeKind.Internal } }
        };
        var settings = new List<BranchTimeClockSettings>
        {
            new() { Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true, EarlyClockInToleranceMinutes = 15 }
        };
        var entries = new List<TimeEntry>
        {
            new() { Id = 1, MerchantId = 7, BranchId = 3, EmployeeId = 11, EventId = 100, EventParticipantId = 501, Type = TimeEntryType.ClockIn, WorkDate = new DateOnly(2026, 5, 24), ActualTimestampUtc = new DateTime(2026, 5, 24, 20, 0, 0, DateTimeKind.Utc) },
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithSet(x => x.TimeEntries, entries, x => [x.Id])
            .WithEmptySet(x => x.TimeClockAnomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetTodayShiftsAsync(11, 7);

        result.Shifts.Should().ContainSingle();
        result.ActiveEventParticipantId.Should().Be(501);
        result.Shifts[0].IsClockedIn.Should().BeTrue();
    }

    [Fact]
    public async Task GetTodayShiftsAsync_OpenShiftMessage_UsesWallClockTime_NotProcessZone()
    {
        // Entrata reale 09:03 ora di parete (UTC+2 → 07:03 UTC). Il messaggio deve
        // dire "In turno da 09:03", non l'ora UTC del processo.
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 8, 0, 0, DateTimeKind.Utc));
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Unspecified));

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var shift = new Event
        {
            Id = 100, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            Title = "Mattina", StartDate = new DateOnly(2026, 5, 24), EndDate = new DateOnly(2026, 5, 24),
            StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(13, 0),
        };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = shift, EmployeeId = 11 }
        };
        var settings = new List<BranchTimeClockSettings>
        {
            new() { Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true }
        };
        var entries = new List<TimeEntry>
        {
            new() { Id = 1, MerchantId = 7, BranchId = 3, EmployeeId = 11, EventId = 100, EventParticipantId = 501, Type = TimeEntryType.ClockIn, WorkDate = new DateOnly(2026, 5, 24), ActualTimestampUtc = new DateTime(2026, 5, 24, 7, 3, 0, DateTimeKind.Utc) },
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithSet(x => x.TimeEntries, entries, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetTodayShiftsAsync(11, 7);

        result.Shifts[0].StatusMessage.Should().Be("In turno da 09:03.");
    }

    [Fact]
    public async Task GetWellbeingStatsAsync_ReturnsZeros_WhenNoEntriesExist()
    {
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc));

        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithEmptySet(x => x.TimeClockAnomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var stats = await service.GetWellbeingStatsAsync(11, 7);

        stats.WorkedMinutesThisWeek.Should().Be(0);
        stats.WorkedMinutesThisMonth.Should().Be(0);
        stats.OpenAnomalies.Should().Be(0);
        stats.HasWellbeingAlert.Should().BeFalse();
    }

    // ── Turni scaduti non timbrati → anomalia (rilevamento lazy) ─────────────

    /// <summary>
    /// Scenario base: un turno di IERI (rispetto a "oggi") di un dipendente interno,
    /// con orari valorizzati e finestra di timbratura ormai chiusa.
    /// </summary>
    private static (List<EventParticipant> participants, List<BranchTimeClockSettings> settings)
        YesterdayShift(TimeOnly? start, TimeOnly? end, int lateClockOutTolerance = 0, bool clockingRequired = true)
    {
        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var yesterday = new Event
        {
            Id = 100, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            Title = "Turno Test", StartDate = new DateOnly(2026, 5, 23), EndDate = new DateOnly(2026, 5, 23),
            StartTime = start, EndTime = end,
        };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = yesterday, EmployeeId = 11, Employee = new Employee { Id = 11, Kind = EmployeeKind.Internal } }
        };
        var settings = new List<BranchTimeClockSettings>
        {
            new()
            {
                Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true,
                ClockingRequired = clockingRequired,
                ClockingRequiredSince = clockingRequired ? new DateOnly(2026, 5, 1) : null,
                LateClockOutToleranceMinutes = lateClockOutTolerance
            }
        };
        return (participants, settings);
    }

    [Fact]
    public async Task GetTodayShiftsAsync_DropsExpiredYesterdayShift_AndCreatesMissingClockInAnomaly()
    {
        // "Oggi" = 24/05 10:00. Turno di ieri 09:00–17:00 mai timbrato: finestra chiusa.
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc));
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Unspecified));

        var (participants, settings) = YesterdayShift(new TimeOnly(9, 0), new TimeOnly(17, 0));
        var anomalies = new List<TimeClockAnomaly>();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetTodayShiftsAsync(11, 7);

        // Sparisce dalla lista turni di oggi…
        result.Shifts.Should().BeEmpty();
        // …e diventa un'anomalia di mancata entrata.
        anomalies.Should().ContainSingle(a =>
            a.EventParticipantId == 501
            && a.Type == TimeClockAnomalyType.MissingClockIn
            && a.Status == TimeClockAnomalyStatus.Open);
    }

    [Fact]
    public async Task GetTodayShiftsAsync_KeepsYesterdayShift_WhenWindowStillOpenWithinTolerance()
    {
        // "Oggi" = 24/05 ma ora di parete 17:20: il turno di ieri finiva alle 17:00
        // con tolleranza uscita 30' → finestra ancora aperta, resta timbrabile.
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 0, 30, 0, DateTimeKind.Utc));
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 23, 17, 20, 0, DateTimeKind.Unspecified));

        var (participants, settings) = YesterdayShift(new TimeOnly(9, 0), new TimeOnly(17, 0), lateClockOutTolerance: 30);
        var anomalies = new List<TimeClockAnomaly>();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetTodayShiftsAsync(11, 7);

        result.Shifts.Should().ContainSingle(s => s.Shift.EventParticipantId == 501);
    }

    [Fact]
    public async Task GetTodayShiftsAsync_KeepsYesterdayShift_WhenNoEndTime_EvenIfNeverPunched()
    {
        // Caso limite: turno di ieri senza orario di fine → finestra non determinabile
        // → non viene scartato (resta timbrabile finché non si interviene).
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc));
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Unspecified));

        var (participants, settings) = YesterdayShift(new TimeOnly(9, 0), end: null);
        var anomalies = new List<TimeClockAnomaly>();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetTodayShiftsAsync(11, 7);

        result.Shifts.Should().ContainSingle(s => s.Shift.EventParticipantId == 501);
    }

    [Fact]
    public async Task GetTodayShiftsAsync_TodayShiftPastWindow_NeverPunched_IsMarkedExpired()
    {
        // Turno di OGGI 09:00–13:00, ora di parete 15:00, mai timbrato: resta in
        // lista (turno odierno) ma con stato "scaduto", non più "in attesa".
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 13, 0, 0, DateTimeKind.Utc));
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 15, 0, 0, DateTimeKind.Unspecified));

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var shift = new Event
        {
            Id = 100, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            Title = "Mattina", StartDate = new DateOnly(2026, 5, 24), EndDate = new DateOnly(2026, 5, 24),
            StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(13, 0),
        };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = shift, EmployeeId = 11, Employee = new Employee { Id = 11, Kind = EmployeeKind.Internal } }
        };
        var settings = new List<BranchTimeClockSettings>
        {
            new() { Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true, ClockingRequired = true, ClockingRequiredSince = new DateOnly(2026, 5, 1) }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithEmptySet(x => x.TimeClockAnomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetTodayShiftsAsync(11, 7);

        var s = result.Shifts.Single(x => x.Shift.EventParticipantId == 501);
        s.IsExpired.Should().BeTrue();
        s.IsActive.Should().BeFalse();
        s.StatusMessage.Should().Be("Finestra di timbratura chiusa.");
    }

    [Fact]
    public async Task RunMissingPunchDetectionForEmployeeAsync_OnlyCoversTheGivenEmployee()
    {
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc));

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var pastShift = new Event
        {
            Id = 100, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            StartDate = new DateOnly(2026, 5, 23)
        };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = pastShift, EmployeeId = 11, Employee = new Employee { Id = 11, Kind = EmployeeKind.Internal } },
            new() { Id = 502, EventId = 100, Event = pastShift, EmployeeId = 12, Employee = new Employee { Id = 12, Kind = EmployeeKind.Internal } }
        };
        var anomalies = new List<TimeClockAnomaly>();
        var settings = new List<BranchTimeClockSettings>
        {
            new() { Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true, ClockingRequired = true, ClockingRequiredSince = new DateOnly(2026, 5, 1) }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var created = await service.RunMissingPunchDetectionForEmployeeAsync(11, 7);

        created.Should().Be(1);
        anomalies.Should().ContainSingle(a => a.EventParticipantId == 501);
        anomalies.Should().NotContain(a => a.EventParticipantId == 502);
    }

    [Fact]
    public async Task RunMissingPunchDetectionForEmployeeAsync_IsIdempotent_OnRepeatedRuns()
    {
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc));

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var pastShift = new Event
        {
            Id = 100, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            StartDate = new DateOnly(2026, 5, 23)
        };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = pastShift, EmployeeId = 11, Employee = new Employee { Id = 11, Kind = EmployeeKind.Internal } }
        };
        var anomalies = new List<TimeClockAnomaly>();
        var settings = new List<BranchTimeClockSettings>
        {
            new() { Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true, ClockingRequired = true, ClockingRequiredSince = new DateOnly(2026, 5, 1) }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var first = await service.RunMissingPunchDetectionForEmployeeAsync(11, 7);
        var second = await service.RunMissingPunchDetectionForEmployeeAsync(11, 7);

        first.Should().Be(1);
        second.Should().Be(0);
        anomalies.Should().ContainSingle(a => a.Type == TimeClockAnomalyType.MissingClockIn);
    }

    [Fact]
    public async Task RunMissingPunchDetectionForEmployeeAsync_SkipsConcurrentlyCreatedAnomaly()
    {
        // Simula la corsa: l'anomalia per quel turno è già presente sul DB (creata da
        // un'altra richiesta). La detction non deve duplicarla.
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc));

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var pastShift = new Event
        {
            Id = 100, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            StartDate = new DateOnly(2026, 5, 23)
        };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = pastShift, EmployeeId = 11, Employee = new Employee { Id = 11, Kind = EmployeeKind.Internal } }
        };
        var anomalies = new List<TimeClockAnomaly>
        {
            new() { Id = 99, MerchantId = 7, EmployeeId = 11, EventId = 100, EventParticipantId = 501, Type = TimeClockAnomalyType.MissingClockIn, Status = TimeClockAnomalyStatus.Open, WorkDate = new DateOnly(2026, 5, 23) }
        };
        var settings = new List<BranchTimeClockSettings>
        {
            new() { Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true, ClockingRequired = true, ClockingRequiredSince = new DateOnly(2026, 5, 1) }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var created = await service.RunMissingPunchDetectionForEmployeeAsync(11, 7);

        created.Should().Be(0);
        anomalies.Should().ContainSingle();
    }

    [Fact]
    public async Task ClockOutAsync_OvernightShiftWithoutEndDate_ComputesDeviationAcrossMidnight()
    {
        // Turno notturno 22:00→06:00 SENZA EndDate (convenzione: EndDate opzionale).
        // L'uscita reale alle 06:02 del giorno dopo è in ritardo di soli 2 minuti:
        // la deviazione non deve essere ~+1440 (= "il giorno prima alle 06:00"),
        // quindi nessuna anomalia di uscita oltre orario con straordinario fittizio.
        var nowUtc = new DateTime(2026, 5, 25, 6, 2, 0, DateTimeKind.Utc);
        var nowWall = new DateTime(2026, 5, 25, 6, 2, 0, DateTimeKind.Unspecified);
        _utcClock.SetupGet(x => x.UtcNow).Returns(nowUtc);
        _wallClock.SetupGet(x => x.Now).Returns(nowWall);

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var night = new Event
        {
            Id = 100, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            Title = "Notturno", StartDate = new DateOnly(2026, 5, 24), EndDate = null,
            StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(6, 0),
        };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = night, EmployeeId = 11, Employee = new Employee { Id = 11, Kind = EmployeeKind.Internal } }
        };
        var settings = new List<BranchTimeClockSettings>
        {
            new() { Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true, GraceOutMinutes = 5, LateClockOutToleranceMinutes = 15 }
        };
        var entries = new List<TimeEntry>
        {
            new() { Id = 1, MerchantId = 7, BranchId = 3, EmployeeId = 11, EventId = 100, EventParticipantId = 501, Type = TimeEntryType.ClockIn, WorkDate = new DateOnly(2026, 5, 24), ActualTimestampUtc = new DateTime(2026, 5, 24, 22, 0, 0, DateTimeKind.Utc) }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithSet(x => x.TimeEntries, entries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, new List<TimeClockAnomaly>(), x => [x.Id])
            .WithSet(x => x.MerchantBranches, new List<MerchantBranch> { branch }, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.ClockOutAsync(11, 7, new ClockActionRequest { EventParticipantId = 501 });

        result.Entry.DeviationMinutes.Should().Be(2);
        result.Anomaly.Should().BeNull();
    }

    [Fact]
    public async Task ClockInAsync_OvernightShiftWithoutEndDate_IsTimbrableInWindow()
    {
        // Turno notturno 22:00→06:00 SENZA EndDate. Entrata alle 22:00 in punto:
        // la finestra deve essere aperta (la fine 06:00 è del giorno dopo, non prima
        // dell'inizio). Lo stato corrente deve risultare "in turno".
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 22, 0, 0, DateTimeKind.Utc));
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 22, 0, 0, DateTimeKind.Unspecified));

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var night = new Event
        {
            Id = 100, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            Title = "Notturno", StartDate = new DateOnly(2026, 5, 24), EndDate = null,
            StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(6, 0),
        };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = night, EmployeeId = 11, Employee = new Employee { Id = 11, Kind = EmployeeKind.Internal } }
        };
        var settings = new List<BranchTimeClockSettings>
        {
            new() { Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true, EarlyClockInToleranceMinutes = 15, GraceInMinutes = 5 }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithSet(x => x.TimeEntries, new List<TimeEntry>(), x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, new List<TimeClockAnomaly>(), x => [x.Id])
            .WithSet(x => x.MerchantBranches, new List<MerchantBranch> { branch }, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.ClockInAsync(11, 7, new ClockActionRequest { EventParticipantId = 501 });

        result.Entry.DeviationMinutes.Should().Be(0);
        result.Anomaly.Should().BeNull();
        result.Status.IsClockedIn.Should().BeTrue();
    }

    [Fact]
    public async Task ClockOutAsync_DaytimeShiftWithoutEndDate_KeepsEndOnStartDate()
    {
        // Turno diurno 09:00→17:00 SENZA EndDate (caso comune). La fine NON deve
        // slittare al giorno dopo: uscita alle 17:00 → deviazione 0, nessuna anomalia.
        // Copre il ramo "EndDate null ma end > start" di ResolveEndWall.
        var nowUtc = new DateTime(2026, 5, 24, 17, 0, 0, DateTimeKind.Utc);
        var nowWall = new DateTime(2026, 5, 24, 17, 0, 0, DateTimeKind.Unspecified);
        _utcClock.SetupGet(x => x.UtcNow).Returns(nowUtc);
        _wallClock.SetupGet(x => x.Now).Returns(nowWall);

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var shift = new Event
        {
            Id = 100, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            Title = "Giornata", StartDate = new DateOnly(2026, 5, 24), EndDate = null,
            StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0),
        };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = shift, EmployeeId = 11, Employee = new Employee { Id = 11, Kind = EmployeeKind.Internal } }
        };
        var settings = new List<BranchTimeClockSettings>
        {
            new() { Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true, GraceOutMinutes = 5, LateClockOutToleranceMinutes = 15 }
        };
        var entries = new List<TimeEntry>
        {
            new() { Id = 1, MerchantId = 7, BranchId = 3, EmployeeId = 11, EventId = 100, EventParticipantId = 501, Type = TimeEntryType.ClockIn, WorkDate = new DateOnly(2026, 5, 24), ActualTimestampUtc = new DateTime(2026, 5, 24, 9, 0, 0, DateTimeKind.Utc) }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithSet(x => x.TimeEntries, entries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, new List<TimeClockAnomaly>(), x => [x.Id])
            .WithSet(x => x.MerchantBranches, new List<MerchantBranch> { branch }, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.ClockOutAsync(11, 7, new ClockActionRequest { EventParticipantId = 501 });

        result.Entry.DeviationMinutes.Should().Be(0);
        result.Anomaly.Should().BeNull();
    }

    [Fact]
    public async Task RunMissingPunchDetectionForEmployeeAsync_CreatesMissingClockOut_WhenClockInWithoutClockOut()
    {
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc));

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var pastShift = new Event
        {
            Id = 100, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            StartDate = new DateOnly(2026, 5, 23)
        };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = pastShift, EmployeeId = 11, Employee = new Employee { Id = 11, Kind = EmployeeKind.Internal } }
        };
        var entries = new List<TimeEntry>
        {
            new() { Id = 1, MerchantId = 7, BranchId = 3, EmployeeId = 11, EventId = 100, EventParticipantId = 501, Type = TimeEntryType.ClockIn, WorkDate = new DateOnly(2026, 5, 23), ActualTimestampUtc = new DateTime(2026, 5, 23, 7, 0, 0, DateTimeKind.Utc) }
        };
        var anomalies = new List<TimeClockAnomaly>();
        var settings = new List<BranchTimeClockSettings>
        {
            new() { Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true, ClockingRequired = true, ClockingRequiredSince = new DateOnly(2026, 5, 1) }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.TimeEntries, entries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var created = await service.RunMissingPunchDetectionForEmployeeAsync(11, 7);

        created.Should().Be(1);
        anomalies.Should().ContainSingle(a => a.Type == TimeClockAnomalyType.MissingClockOut);
    }

    // ── Timbratura facoltativa: niente anomalia di mancata timbratura ────────

    [Fact]
    public async Task RunMissingPunchDetectionForEmployeeAsync_SkipsShift_WhenBranchClockingOptional()
    {
        // Filiale con timbratura FACOLTATIVA (ClockingRequired = false): un turno
        // passato mai timbrato NON deve generare l'anomalia di mancata entrata.
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc));
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Unspecified));

        var (participants, settings) = YesterdayShift(new TimeOnly(9, 0), new TimeOnly(17, 0), clockingRequired: false);
        var anomalies = new List<TimeClockAnomaly>();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var created = await service.RunMissingPunchDetectionForEmployeeAsync(11, 7);

        created.Should().Be(0);
        anomalies.Should().BeEmpty();
    }

    // ── Decorrenza dell'obbligo: nessuna anomalia retroattiva ───────────────

    [Fact]
    public async Task RunMissingPunchDetectionForEmployeeAsync_IgnoresShiftsBeforeClockingRequiredSince()
    {
        // Obbligo attivato il 23/05: il turno del 22/05 non deve generare nulla,
        // quello del 23/05 sì. Senza decorrenza, abilitare la timbratura
        // riempirebbe la lista di anomalie da giustificare su tutto lo storico.
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc));

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var employee = new Employee { Id = 11, Kind = EmployeeKind.Internal };
        var beforeShift = new Event
        {
            Id = 100, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            StartDate = new DateOnly(2026, 5, 22)
        };
        var afterShift = new Event
        {
            Id = 101, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            StartDate = new DateOnly(2026, 5, 23)
        };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = beforeShift, EmployeeId = 11, Employee = employee },
            new() { Id = 502, EventId = 101, Event = afterShift, EmployeeId = 11, Employee = employee }
        };
        var settings = new List<BranchTimeClockSettings>
        {
            new()
            {
                Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true,
                ClockingRequired = true, ClockingRequiredSince = new DateOnly(2026, 5, 23)
            }
        };
        var anomalies = new List<TimeClockAnomaly>();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var created = await service.RunMissingPunchDetectionForEmployeeAsync(11, 7);

        created.Should().Be(1);
        anomalies.Should().ContainSingle(a => a.EventParticipantId == 502);
    }

    [Fact]
    public async Task RunMissingPunchDetectionForEmployeeAsync_SkipsShift_WhenClockingRequiredWithoutStartDate()
    {
        // Obbligo senza decorrenza (configurazione incoerente): non essendoci una
        // data da cui far valere la regola, non si segnala nulla.
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc));
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Unspecified));

        var (participants, settings) = YesterdayShift(new TimeOnly(9, 0), new TimeOnly(17, 0));
        settings[0].ClockingRequiredSince = null;
        var anomalies = new List<TimeClockAnomaly>();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var created = await service.RunMissingPunchDetectionForEmployeeAsync(11, 7);

        created.Should().Be(0);
        anomalies.Should().BeEmpty();
    }

    [Fact]
    public async Task RunMissingPunchDetectionForEmployeeAsync_SkipsShift_WhenClockingDisabledForBranch()
    {
        // Timbratura disattivata ma obbligo rimasto acceso in configurazione:
        // finché la filiale non timbra non ci sono mancate timbrature.
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc));
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Unspecified));

        var (participants, settings) = YesterdayShift(new TimeOnly(9, 0), new TimeOnly(17, 0));
        settings[0].IsEnabled = false;
        var anomalies = new List<TimeClockAnomaly>();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var created = await service.RunMissingPunchDetectionForEmployeeAsync(11, 7);

        created.Should().Be(0);
        anomalies.Should().BeEmpty();
    }

    [Fact]
    public async Task RunMissingPunchDetectionAsync_OnlyFlagsRequiredBranches_InMultiBranchMerchant()
    {
        // Stesso merchant, due filiali: A (id 3) obbligatoria, B (id 4) facoltativa.
        // Due turni passati mai timbrati, uno per filiale: solo quello sulla filiale
        // obbligatoria deve diventare un'anomalia di mancata entrata.
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc));

        var branchA = new MerchantBranch { Id = 3, MerchantId = 7, Name = "A", IsActive = true };
        var branchB = new MerchantBranch { Id = 4, MerchantId = 7, Name = "B", IsActive = true };
        var shiftA = new Event { Id = 100, MerchantId = 7, BranchId = 3, Branch = branchA, EventType = EventType.Turno, StartDate = new DateOnly(2026, 5, 23) };
        var shiftB = new Event { Id = 101, MerchantId = 7, BranchId = 4, Branch = branchB, EventType = EventType.Turno, StartDate = new DateOnly(2026, 5, 23) };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = shiftA, EmployeeId = 11, Employee = new Employee { Id = 11, Kind = EmployeeKind.Internal } },
            new() { Id = 502, EventId = 101, Event = shiftB, EmployeeId = 12, Employee = new Employee { Id = 12, Kind = EmployeeKind.Internal } }
        };
        var settings = new List<BranchTimeClockSettings>
        {
            new() { Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true, ClockingRequired = true, ClockingRequiredSince = new DateOnly(2026, 5, 1) },
            new() { Id = 11, BranchId = 4, MerchantId = 7, IsEnabled = true, ClockingRequired = false }
        };
        var anomalies = new List<TimeClockAnomaly>();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var created = await service.RunMissingPunchDetectionAsync(7, null);

        created.Should().Be(1);
        anomalies.Should().ContainSingle(a => a.EventParticipantId == 501 && a.Type == TimeClockAnomalyType.MissingClockIn);
        anomalies.Should().NotContain(a => a.EventParticipantId == 502);
    }

    [Fact]
    public async Task GetTodayShiftsAsync_OptionalBranchPastWindow_NotMarkedExpired()
    {
        // Turno di OGGI 09:00–13:00, ora di parete 15:00, mai timbrato, su filiale
        // FACOLTATIVA: non è una mancata timbratura, quindi non va marcato "scaduto"
        // ma indicato come timbratura facoltativa, senza pulsanti.
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 13, 0, 0, DateTimeKind.Utc));
        _wallClock.SetupGet(x => x.Now).Returns(new DateTime(2026, 5, 24, 15, 0, 0, DateTimeKind.Unspecified));

        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var shift = new Event
        {
            Id = 100, MerchantId = 7, BranchId = 3, Branch = branch, EventType = EventType.Turno,
            Title = "Mattina", StartDate = new DateOnly(2026, 5, 24), EndDate = new DateOnly(2026, 5, 24),
            StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(13, 0),
        };
        var participants = new List<EventParticipant>
        {
            new() { Id = 501, EventId = 100, Event = shift, EmployeeId = 11, Employee = new Employee { Id = 11, Kind = EmployeeKind.Internal } }
        };
        var settings = new List<BranchTimeClockSettings>
        {
            new() { Id = 10, BranchId = 3, MerchantId = 7, IsEnabled = true, ClockingRequired = false }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithSet(x => x.BranchTimeClockSettings, settings, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithEmptySet(x => x.TimeClockAnomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object, _notifications.Object);

        var result = await service.GetTodayShiftsAsync(11, 7);

        var s = result.Shifts.Single(x => x.Shift.EventParticipantId == 501);
        s.IsExpired.Should().BeFalse();
        s.IsActive.Should().BeFalse();
        s.StatusMessage.Should().Be("Timbratura facoltativa.");
    }
}
