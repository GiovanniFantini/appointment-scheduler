using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class NotificationServiceTests
{
    private readonly Mock<IUtcClock> _clock = new();

    [Fact]
    public async Task GetSummaryAsync_ReturnsUnreadCountAndFiveMostRecentNotifications()
    {
        var notifications = Enumerable.Range(1, 7)
            .Select(index => new Notification
            {
                Id = index,
                UserId = 12,
                Title = $"Notification {index}",
                Message = $"Message {index}",
                IsRead = index % 2 == 0,
                CreatedAt = new DateTime(2026, 5, 22, 10, 0, 0, DateTimeKind.Utc).AddMinutes(index)
            })
            .Concat(new[]
            {
                new Notification { Id = 100, UserId = 99, Title = "Other", Message = "Other", CreatedAt = new DateTime(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc) }
            })
            .ToList();
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Notifications, notifications, notification => [notification.Id])
            .Build();
        var service = new NotificationService(context.Object, _clock.Object);

        var result = await service.GetSummaryAsync(12);

        result.UnreadCount.Should().Be(4);
        result.Recent.Should().HaveCount(5);
        result.Recent.Select(x => x.Id).Should().ContainInOrder(7, 6, 5, 4, 3);
    }

    [Fact]
    public async Task MarkReadAsync_DoesNothing_WhenNotificationDoesNotBelongToUser()
    {
        var notifications = new List<Notification>
        {
            new() { Id = 1, UserId = 77, Title = "Hello", Message = "World", IsRead = false }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Notifications, notifications, notification => [notification.Id])
            .Build(out var saveChangesTracker);
        var service = new NotificationService(context.Object, _clock.Object);

        await service.MarkReadAsync(1, 12);

        notifications[0].IsRead.Should().BeFalse();
        saveChangesTracker.Count.Should().Be(0);
    }

    [Fact]
    public async Task MarkAllReadAsync_UpdatesOnlyUnreadNotificationsOfTheUser()
    {
        var notifications = new List<Notification>
        {
            new() { Id = 1, UserId = 12, Title = "A", Message = "A", IsRead = false },
            new() { Id = 2, UserId = 12, Title = "B", Message = "B", IsRead = true },
            new() { Id = 3, UserId = 77, Title = "C", Message = "C", IsRead = false }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Notifications, notifications, notification => [notification.Id])
            .Build(out var saveChangesTracker);
        var service = new NotificationService(context.Object, _clock.Object);

        await service.MarkAllReadAsync(12);

        notifications.Single(x => x.Id == 1).IsRead.Should().BeTrue();
        notifications.Single(x => x.Id == 2).IsRead.Should().BeTrue();
        notifications.Single(x => x.Id == 3).IsRead.Should().BeFalse();
        saveChangesTracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_AddsNotificationWithClockTimestamp()
    {
        var now = new DateTime(2026, 5, 22, 10, 30, 0, DateTimeKind.Utc);
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        var notifications = new List<Notification>();
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Notifications, notifications, notification => [notification.Id])
            .Build(out var saveChangesTracker);
        var service = new NotificationService(context.Object, _clock.Object);

        await service.CreateAsync(12, "Title", "Message", NotificationType.DocumentPublished, 33);

        notifications.Should().ContainSingle();
        var notification = notifications[0];
        notification.UserId.Should().Be(12);
        notification.Title.Should().Be("Title");
        notification.Message.Should().Be("Message");
        notification.Type.Should().Be(NotificationType.DocumentPublished);
        notification.RelatedEntityId.Should().Be(33);
        notification.CreatedAt.Should().Be(now);
        saveChangesTracker.Count.Should().Be(1);
    }
}