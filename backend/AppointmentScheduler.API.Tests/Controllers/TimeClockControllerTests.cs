using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests.Controllers;

public class TimeClockControllerTests
{
    private readonly Mock<ITimeClockService> _timeClockService = new();

    [Fact]
    public async Task GetSettings_ReturnsBadRequest_WhenMerchantClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.GetSettings(3);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Token non valido");
    }

    [Fact]
    public async Task GetSettings_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        _timeClockService.Setup(service => service.GetSettingsAsync(3, 7)).ThrowsAsync(new InvalidOperationException("Filiale non valida"));
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetSettings(3);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Filiale non valida");
    }

    [Fact]
    public async Task GetSettings_ReturnsOk_WhenServiceSucceeds()
    {
        var settings = new BranchTimeClockSettingsDto { BranchId = 3 };
        _timeClockService.Setup(service => service.GetSettingsAsync(3, 7)).ReturnsAsync(settings);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetSettings(3);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(settings);
    }

    [Fact]
    public async Task GetEntries_ReturnsOk_WhenIdentityIsValid()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 1, 31);
        var entries = new List<TimeEntryDto> { new() { Id = 9 } };
        _timeClockService.Setup(service => service.GetEntriesAsync(7, 3, from, to, 11)).ReturnsAsync(entries);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetEntries(3, from, to, 11);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(entries);
    }

    [Fact]
    public async Task GetAnomalies_ReturnsOk_WhenIdentityIsValid()
    {
        var anomalies = new List<TimeClockAnomalyDto> { new() { Id = 6 } };
        _timeClockService.Setup(service => service.GetAnomaliesAsync(7, 3, TimeClockAnomalyStatus.Open)).ReturnsAsync(anomalies);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetAnomalies(3, TimeClockAnomalyStatus.Open);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(anomalies);
    }

    [Fact]
    public async Task GetReport_ReturnsOk_WhenIdentityIsValid()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 1, 31);
        var rows = new List<TimeClockReportRowDto> { new() { EmployeeId = 11 } };
        _timeClockService.Setup(service => service.GetReportAsync(7, 3, from, to)).ReturnsAsync(rows);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetReport(3, from, to);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(rows);
    }

    private TimeClockController CreateController(params Claim[] claims)
    {
        return new TimeClockController(_timeClockService.Object).WithUser(claims);
    }
}
