using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests.Controllers;

public class NotificationsControllerTests
{
    private readonly Mock<INotificationService> _notificationService = new();

    [Fact]
    public async Task GetAll_ReturnsBadRequest_WhenUserIdClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.GetAll();

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _notificationService.Verify(service => service.GetAllAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_ReturnsNotifications_ForCurrentUser()
    {
        var notifications = new List<NotificationDto>
        {
            new() { Id = 1, Title = "Shift", Message = "Assigned", IsRead = false, CreatedAt = DateTime.UtcNow }
        };
        _notificationService.Setup(service => service.GetAllAsync(42)).ReturnsAsync(notifications);
        var controller = CreateController(userId: 42);

        var result = await controller.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(notifications);
    }

    [Fact]
    public async Task GetSummary_ReturnsBadRequest_WhenUserIdClaimIsInvalid()
    {
        var controller = CreateController(rawUserId: "not-an-int");

        var result = await controller.GetSummary();

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _notificationService.Verify(service => service.GetSummaryAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetSummary_ReturnsSummary_ForCurrentUser()
    {
        var summary = new NotificationSummaryDto { UnreadCount = 3 };
        _notificationService.Setup(service => service.GetSummaryAsync(21)).ReturnsAsync(summary);
        var controller = CreateController(userId: 21);

        var result = await controller.GetSummary();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(summary);
    }

    [Fact]
    public async Task MarkRead_ReturnsBadRequest_WhenUserIdClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.MarkRead(9);

        result.Should().BeOfType<BadRequestObjectResult>();
        _notificationService.Verify(service => service.MarkReadAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task MarkRead_CallsServiceAndReturnsOk_WhenUserIdIsPresent()
    {
        var controller = CreateController(userId: 21);

        var result = await controller.MarkRead(9);

        result.Should().BeOfType<OkObjectResult>();
        result.GetAnonymousString("message").Should().Be("Notifica segnata come letta");
        _notificationService.Verify(service => service.MarkReadAsync(9, 21), Times.Once);
    }

    [Fact]
    public async Task MarkAllRead_ReturnsBadRequest_WhenUserIdClaimIsInvalid()
    {
        var controller = CreateController(rawUserId: "abc");

        var result = await controller.MarkAllRead();

        result.Should().BeOfType<BadRequestObjectResult>();
        _notificationService.Verify(service => service.MarkAllReadAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task MarkAllRead_CallsServiceAndReturnsOk_WhenUserIdIsPresent()
    {
        var controller = CreateController(userId: 77);

        var result = await controller.MarkAllRead();

        result.Should().BeOfType<OkObjectResult>();
        result.GetAnonymousString("message").Should().Be("Tutte le notifiche segnate come lette");
        _notificationService.Verify(service => service.MarkAllReadAsync(77), Times.Once);
    }

    private NotificationsController CreateController(int? userId = null, string? rawUserId = null)
    {
        var controller = new NotificationsController(_notificationService.Object);
        if (userId.HasValue)
        {
            return controller.WithUser(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        if (rawUserId is not null)
        {
            return controller.WithUser(new Claim(ClaimTypes.NameIdentifier, rawUserId));
        }

        return controller.WithUser();
    }
}