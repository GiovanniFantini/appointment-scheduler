using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests.Controllers;

public class EmployeeTimeClockControllerTests
{
    private readonly Mock<ITimeClockService> _timeClockService = new();

    [Fact]
    public async Task GetStatus_ReturnsBadRequest_WhenIdentityClaimsAreMissing()
    {
        var controller = CreateController();

        var result = await controller.GetStatus();

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Token non valido");
    }

    [Fact]
    public async Task GetStatus_ReturnsForbid_WhenFeatureIsMissing()
    {
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"));

        var result = await controller.GetStatus();

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetStatus_ReturnsOk_WhenFeatureExists()
    {
        var status = new CurrentClockStatusDto();
        _timeClockService.Setup(service => service.GetCurrentStatusAsync(11, 7)).ReturnsAsync(status);
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"), new Claim("Feature", "Timbratura"));

        var result = await controller.GetStatus();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(status);
    }

    [Fact]
    public async Task GetTodayShifts_ReturnsBadRequest_WhenIdentityClaimsAreMissing()
    {
        var controller = CreateController();

        var result = await controller.GetTodayShifts();

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Token non valido");
    }

    [Fact]
    public async Task GetTodayShifts_ReturnsForbid_WhenFeatureIsMissing()
    {
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"));

        var result = await controller.GetTodayShifts();

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetTodayShifts_ReturnsOk_WhenFeatureExists()
    {
        var shifts = new TodayShiftsDto { TimeClockEnabled = true };
        _timeClockService.Setup(service => service.GetTodayShiftsAsync(11, 7)).ReturnsAsync(shifts);
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"), new Claim("Feature", "Timbratura"));

        var result = await controller.GetTodayShifts();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(shifts);
    }

    [Theory]
    [InlineData("ClockIn")]
    [InlineData("ClockOut")]
    [InlineData("StartBreak")]
    [InlineData("EndBreak")]
    public async Task ClockActions_ReturnBadRequest_WhenServiceThrowsInvalidOperationException(string actionName)
    {
        var request = new ClockActionRequest();
        SetupClockActionThrow(actionName, request, new InvalidOperationException("Operazione non valida"));
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"), new Claim("Feature", "Timbratura"));

        var result = await ExecuteClockAction(controller, actionName, request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Operazione non valida");
    }

    [Theory]
    [InlineData("ClockIn")]
    [InlineData("ClockOut")]
    [InlineData("StartBreak")]
    [InlineData("EndBreak")]
    public async Task ClockActions_ReturnOk_WhenServiceSucceeds(string actionName)
    {
        var request = new ClockActionRequest();
        var response = new ClockActionResultDto();
        SetupClockActionReturn(actionName, request, response);
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"), new Claim("Feature", "Timbratura"));

        var result = await ExecuteClockAction(controller, actionName, request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task GetMyEntries_ReturnsForbid_WhenFeatureIsMissing()
    {
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"));

        var result = await controller.GetMyEntries();

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetMyEntries_ReturnsOk_WhenFeatureExists()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 1, 31);
        var entries = new List<TimeEntryDto> { new() { Id = 9 } };
        _timeClockService.Setup(service => service.GetMyEntriesAsync(11, 7, from, to)).ReturnsAsync(entries);
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"), new Claim("Feature", "Timbratura"));

        var result = await controller.GetMyEntries(from, to);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(entries);
    }

    [Fact]
    public async Task GetMyAnomalies_ReturnsOk_WhenFeatureExists()
    {
        var anomalies = new List<TimeClockAnomalyDto> { new() { Id = 4 } };
        _timeClockService.Setup(service => service.GetMyAnomaliesAsync(11, 7, TimeClockAnomalyStatus.Open)).ReturnsAsync(anomalies);
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"), new Claim("Feature", "Timbratura"));

        var result = await controller.GetMyAnomalies(TimeClockAnomalyStatus.Open);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(anomalies);
    }

    [Fact]
    public async Task GetWellbeing_ReturnsOk_WhenFeatureExists()
    {
        var stats = new WellbeingStatsDto();
        _timeClockService.Setup(service => service.GetWellbeingStatsAsync(11, 7)).ReturnsAsync(stats);
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"), new Claim("Feature", "Timbratura"));

        var result = await controller.GetWellbeing();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(stats);
    }

    [Fact]
    public async Task JustifyAnomaly_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        var request = new JustifyAnomalyRequest();
        _timeClockService.Setup(service => service.JustifyAnomalyAsync(5, 11, 7, request)).ThrowsAsync(new InvalidOperationException("Giustificazione non valida"));
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"), new Claim("Feature", "Timbratura"));

        var result = await controller.JustifyAnomaly(5, request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Giustificazione non valida");
    }

    [Fact]
    public async Task JustifyAnomaly_ReturnsOk_WhenServiceSucceeds()
    {
        var request = new JustifyAnomalyRequest();
        var anomaly = new TimeClockAnomalyDto { Id = 5 };
        _timeClockService.Setup(service => service.JustifyAnomalyAsync(5, 11, 7, request)).ReturnsAsync(anomaly);
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"), new Claim("Feature", "Timbratura"));

        var result = await controller.JustifyAnomaly(5, request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(anomaly);
    }

    private EmployeeTimeClockController CreateController(params Claim[] claims)
    {
        return new EmployeeTimeClockController(_timeClockService.Object).WithUser(claims);
    }

    private void SetupClockActionThrow(string actionName, ClockActionRequest request, Exception exception)
    {
        switch (actionName)
        {
            case "ClockIn":
                _timeClockService.Setup(service => service.ClockInAsync(11, 7, request)).ThrowsAsync(exception);
                break;
            case "ClockOut":
                _timeClockService.Setup(service => service.ClockOutAsync(11, 7, request)).ThrowsAsync(exception);
                break;
            case "StartBreak":
                _timeClockService.Setup(service => service.StartBreakAsync(11, 7, request)).ThrowsAsync(exception);
                break;
            default:
                _timeClockService.Setup(service => service.EndBreakAsync(11, 7, request)).ThrowsAsync(exception);
                break;
        }
    }

    private void SetupClockActionReturn(string actionName, ClockActionRequest request, ClockActionResultDto response)
    {
        switch (actionName)
        {
            case "ClockIn":
                _timeClockService.Setup(service => service.ClockInAsync(11, 7, request)).ReturnsAsync(response);
                break;
            case "ClockOut":
                _timeClockService.Setup(service => service.ClockOutAsync(11, 7, request)).ReturnsAsync(response);
                break;
            case "StartBreak":
                _timeClockService.Setup(service => service.StartBreakAsync(11, 7, request)).ReturnsAsync(response);
                break;
            default:
                _timeClockService.Setup(service => service.EndBreakAsync(11, 7, request)).ReturnsAsync(response);
                break;
        }
    }

    private static Task<ActionResult<ClockActionResultDto>> ExecuteClockAction(EmployeeTimeClockController controller, string actionName, ClockActionRequest request)
        => actionName switch
        {
            "ClockIn" => controller.ClockIn(request),
            "ClockOut" => controller.ClockOut(request),
            "StartBreak" => controller.StartBreak(request),
            _ => controller.EndBreak(request)
        };
}