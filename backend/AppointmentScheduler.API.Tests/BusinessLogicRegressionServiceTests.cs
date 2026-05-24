using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class BusinessLogicRegressionServiceTests
{
    private readonly Mock<IUtcClock> _clock = new();

    [Fact]
    public async Task EmployeeService_RemoveFromMerchantAsync_ReturnsFalse_WhenMembershipMissing()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.EmployeeMemberships, x => [x.Id])
            .Build();

        var service = new EmployeeService(context.Object, _clock.Object);

        var removed = await service.RemoveFromMerchantAsync(11, 7);

        removed.Should().BeFalse();
    }

    [Fact]
    public async Task EmployeeService_RemoveFromMerchantAsync_DisablesMembership_WhenFound()
    {
        var memberships = new List<EmployeeMembership>
        {
            new() { Id = 1, EmployeeId = 11, MerchantId = 7, IsActive = true }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EmployeeMemberships, memberships, x => [x.Id])
            .Build(out var tracker);

        var service = new EmployeeService(context.Object, _clock.Object);

        var removed = await service.RemoveFromMerchantAsync(11, 7);

        removed.Should().BeTrue();
        memberships[0].IsActive.Should().BeFalse();
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task EmployeeService_CreateAsync_Throws_WhenInternalEmployeeHasNoEmail()
    {
        var context = new ApplicationDbContextMockBuilder().Build();
        var service = new EmployeeService(context.Object, _clock.Object);

        var act = () => service.CreateAsync(7, new CreateEmployeeRequest
        {
            FirstName = "Mario",
            LastName = "Rossi",
            Kind = EmployeeKind.Internal,
            Email = "   "
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("L'email è obbligatoria per i dipendenti interni.");
    }

    [Fact]
    public async Task EventService_UpdateAsync_ReturnsNull_WhenEventNotFound()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.Events, x => [x.Id])
            .Build();

        var conflictValidator = new Mock<IShiftConflictValidator>();
        var notifications = new Mock<INotificationService>();
        var service = new EventService(context.Object, conflictValidator.Object, notifications.Object, _clock.Object);

        var result = await service.UpdateAsync(999, 7, new UpdateEventRequest
        {
            BranchId = 3,
            EventType = EventType.Turno,
            Title = "Update",
            StartDate = new DateOnly(2026, 6, 1),
            IsAllDay = true
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task EventService_DeleteAsync_ReturnsFalse_WhenEventNotFound()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.Events, x => [x.Id])
            .Build();

        var conflictValidator = new Mock<IShiftConflictValidator>();
        var notifications = new Mock<INotificationService>();
        var service = new EventService(context.Object, conflictValidator.Object, notifications.Object, _clock.Object);

        var deleted = await service.DeleteAsync(999, 7);

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task EventService_CloneAsync_ReturnsEmpty_WhenEventNotFound()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.Events, x => [x.Id])
            .Build();

        var conflictValidator = new Mock<IShiftConflictValidator>();
        var notifications = new Mock<INotificationService>();
        var service = new EventService(context.Object, conflictValidator.Object, notifications.Object, _clock.Object);

        var result = await service.CloneAsync(999, 7, new CloneEventRequest
        {
            FromDate = new DateOnly(2026, 6, 1),
            ToDate = new DateOnly(2026, 6, 2)
        });

        result.Should().BeEmpty();
    }
}
