using AppointmentScheduler.API.Authorization;
using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Shared.Enums;
using Microsoft.AspNetCore.Authorization;

namespace AppointmentScheduler.API.Tests.Controllers;

public class MerchantReportsControllerTests
{
    [Fact]
    public void Endpoint_RequiresApprovedMerchantAndReportFeature()
    {
        var type = typeof(MerchantReportsController);
        type.GetCustomAttribute<AuthorizeAttribute>()!.Policy.Should().Be("ApprovedMerchantOnly");
        type.GetCustomAttribute<RequiresPlanFeatureAttribute>()!.Feature.Should().Be(MerchantFeature.Report);
    }

    [Fact]
    public async Task Get_UsesMerchantClaimAndPassesFilters()
    {
        var service = new Mock<IMerchantReportingService>();
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 30);
        var report = new MerchantReportDto();
        service.Setup(s => s.GetAsync(7, from, to, 3, default)).ReturnsAsync(report);
        var controller = new MerchantReportsController(service.Object).WithUser(new Claim("MerchantId", "7"));
        var result = await controller.Get(from, to, 3, default);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(report);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("0")]
    public async Task Get_RejectsMissingOrInvalidMerchant(string? merchant)
    {
        var service = new Mock<IMerchantReportingService>(MockBehavior.Strict);
        var controller = new MerchantReportsController(service.Object)
            .WithUser(merchant == null ? [] : [new Claim("MerchantId", merchant)]);
        var result = await controller.Get(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), null, default);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Get_RejectsMissingDates()
    {
        var controller = new MerchantReportsController(Mock.Of<IMerchantReportingService>())
            .WithUser(new Claim("MerchantId", "7"));
        var result = await controller.Get(null, null, null, default);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
