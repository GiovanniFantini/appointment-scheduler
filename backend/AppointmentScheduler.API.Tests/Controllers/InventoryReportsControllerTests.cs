using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests.Controllers;

public class InventoryReportsControllerTests
{
    private readonly Mock<IInventoryReportingService> _reportingService = new();

    [Fact]
    public async Task GetDashboard_ReturnsBadRequest_WhenMerchantClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.GetDashboard();

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Token non valido");
    }

    [Fact]
    public async Task GetDashboard_ReturnsForbid_WhenFeatureIsMissing()
    {
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetDashboard();

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetDashboard_ReturnsOk_WhenFeatureExists()
    {
        var dashboard = new InventoryDashboardDto();
        _reportingService.Setup(service => service.GetDashboardAsync(7, 2)).ReturnsAsync(dashboard);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Magazzino"));

        var result = await controller.GetDashboard(2);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dashboard);
    }

    [Fact]
    public async Task GetValuation_ReturnsForbid_WhenFeatureIsMissing()
    {
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetValuation();

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetValuation_ReturnsOk_WhenFeatureExists()
    {
        var rows = new List<InventoryValuationReportRowDto> { new() { ItemId = 1 } };
        _reportingService.Setup(service => service.GetValuationAsync(7, 2)).ReturnsAsync(rows);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Magazzino"));

        var result = await controller.GetValuation(2);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(rows);
    }

    [Fact]
    public async Task GetLowStock_ReturnsForbid_WhenFeatureIsMissing()
    {
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetLowStock();

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetLowStock_ReturnsOk_WhenFeatureExists()
    {
        var rows = new List<LowStockReportRowDto> { new() { ItemId = 1 } };
        _reportingService.Setup(service => service.GetLowStockAsync(7, 2)).ReturnsAsync(rows);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Magazzino"));

        var result = await controller.GetLowStock(2);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(rows);
    }

    private InventoryReportsController CreateController(params Claim[] claims)
    {
        return new InventoryReportsController(_reportingService.Object).WithUser(claims);
    }
}