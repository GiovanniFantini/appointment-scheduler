using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests.Controllers;

public class AdminToolsControllerTests
{
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<ILogger<AdminToolsController>> _logger = new();

    [Fact]
    public void GetEmailStatus_MapsServiceStatusIntoDto()
    {
        _emailService
            .Setup(service => service.GetStatus())
            .Returns(new EmailServiceStatus(true, "sender@example.com", "Scheduler", "acs.contoso.test"));
        var controller = CreateController();

        var result = controller.GetEmailStatus().Result;

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<EmailServiceStatusDto>().Subject;
        payload.IsConfigured.Should().BeTrue();
        payload.SenderAddress.Should().Be("sender@example.com");
        payload.SenderDisplayName.Should().Be("Scheduler");
        payload.EndpointHost.Should().Be("acs.contoso.test");
    }

    [Fact]
    public async Task SendTestEmail_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError("ToAddress", "Required");

        var result = await controller.SendTestEmail(new TestEmailRequest());

        result.Should().BeOfType<BadRequestObjectResult>();
        _emailService.Verify(service => service.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendTestEmail_ReturnsServiceUnavailable_WhenEmailProviderIsNotConfigured()
    {
        _emailService
            .Setup(service => service.GetStatus())
            .Returns(new EmailServiceStatus(false, string.Empty, string.Empty, null));
        var controller = CreateController();

        var result = await controller.SendTestEmail(new TestEmailRequest { ToAddress = "user@example.com" });

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        result.GetAnonymousBool("success").Should().BeFalse();
        result.GetAnonymousBool("isConfigured").Should().BeFalse();
        _emailService.Verify(service => service.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendTestEmail_UsesRecipientAsDisplayName_WhenDisplayNameIsMissing()
    {
        _emailService
            .Setup(service => service.GetStatus())
            .Returns(new EmailServiceStatus(true, "sender@example.com", "Scheduler", "acs.contoso.test"));
        var controller = CreateController();

        var result = await controller.SendTestEmail(new TestEmailRequest { ToAddress = "user@example.com" });

        result.Should().BeOfType<OkObjectResult>();
        _emailService.Verify(
            service => service.SendAsync(
                "user@example.com",
                "user@example.com",
                "Email di test - Appointment Scheduler Admin",
                It.Is<string>(html => html.Contains("user@example.com"))),
            Times.Once);
    }

    [Fact]
    public async Task SendTestEmail_ReturnsBadGateway_WhenProviderThrowsRequestFailedException()
    {
        _emailService
            .Setup(service => service.GetStatus())
            .Returns(new EmailServiceStatus(true, "sender@example.com", "Scheduler", "acs.contoso.test"));
        _emailService
            .Setup(service => service.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new RequestFailedException(429, "Quota exceeded", "QuotaExceeded", null));
        var controller = CreateController();

        var result = await controller.SendTestEmail(new TestEmailRequest { ToAddress = "user@example.com", ToDisplayName = "User" });

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        result.GetAnonymousBool("success").Should().BeFalse();
        result.GetAnonymousInt("status").Should().Be(429);
        result.GetAnonymousString("errorCode").Should().Be("QuotaExceeded");
    }

    [Fact]
    public async Task SendTestEmail_ReturnsInternalServerError_WhenUnexpectedExceptionOccurs()
    {
        _emailService
            .Setup(service => service.GetStatus())
            .Returns(new EmailServiceStatus(true, "sender@example.com", "Scheduler", "acs.contoso.test"));
        _emailService
            .Setup(service => service.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var controller = CreateController();

        var result = await controller.SendTestEmail(new TestEmailRequest { ToAddress = "user@example.com", ToDisplayName = "User" });

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.GetAnonymousBool("success").Should().BeFalse();
        result.GetAnonymousString("message").Should().Be("Errore interno durante l'invio dell'email di test.");
    }

    private AdminToolsController CreateController()
    {
        return new AdminToolsController(_emailService.Object, _logger.Object);
    }
}