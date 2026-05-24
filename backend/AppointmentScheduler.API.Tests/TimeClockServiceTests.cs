using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class TimeClockServiceTests
{
    private readonly Mock<IUtcClock> _utcClock = new();
    private readonly Mock<IWallClock> _wallClock = new();

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

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

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

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

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

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

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

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

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

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

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

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

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

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

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

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EventParticipants, participants, x => [x.Id])
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithSet(x => x.TimeClockAnomalies, anomalies, x => [x.Id])
            .Build(out var tracker);

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

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

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

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

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

        var result = await service.ApproveAnomalyAsync(7, 7, 101, new ReviewAnomalyRequest { ReviewNotes = "Giustificata" });

        result.Status.Should().Be(TimeClockAnomalyStatus.Approved);
        anomalies[0].ReviewedByUserId.Should().Be(101);
        anomalies[0].ReviewedAt.Should().Be(now);
        anomalies[0].UpdatedAt.Should().Be(now);
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsDefaultSettings_WhenNoOverrideExists()
    {
        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.MerchantBranches, new List<MerchantBranch> { branch }, x => [x.Id])
            .WithEmptySet(x => x.BranchTimeClockSettings, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

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

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

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
    public async Task CreateManualEntryAsync_Throws_WhenParticipantIsMissing()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.EventParticipants, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

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

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

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

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

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

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

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

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

        var result = await service.GetMyAnomaliesAsync(11, 7, TimeClockAnomalyStatus.Open);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task GetWellbeingStatsAsync_ReturnsZeros_WhenNoEntriesExist()
    {
        _utcClock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc));

        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.TimeEntries, x => [x.Id])
            .WithEmptySet(x => x.TimeClockAnomalies, x => [x.Id])
            .Build();

        var service = new TimeClockService(context.Object, _utcClock.Object, _wallClock.Object);

        var stats = await service.GetWellbeingStatsAsync(11, 7);

        stats.WorkedMinutesThisWeek.Should().Be(0);
        stats.WorkedMinutesThisMonth.Should().Be(0);
        stats.OpenAnomalies.Should().Be(0);
        stats.HasWellbeingAlert.Should().BeFalse();
    }
}
