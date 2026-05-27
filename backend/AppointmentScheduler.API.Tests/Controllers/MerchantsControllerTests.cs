using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests.Controllers;

public class MerchantsControllerTests
{
    private readonly Mock<IMerchantService> _merchantService = new();
    private readonly Mock<IMerchantRoleService> _merchantRoleService = new();

    [Fact]
    public async Task GetAll_ReturnsAllMerchants()
    {
        var merchants = new List<MerchantDto> { new() { Id = 1, CompanyName = "Contoso" } };
        _merchantService.Setup(service => service.GetAllAsync()).ReturnsAsync(merchants);
        var controller = CreateController();

        var result = await controller.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(merchants);
    }

    [Fact]
    public async Task GetPending_ReturnsPendingMerchants()
    {
        var merchants = new List<MerchantDto> { new() { Id = 1, CompanyName = "Contoso", IsApproved = false } };
        _merchantService.Setup(service => service.GetPendingAsync()).ReturnsAsync(merchants);
        var controller = CreateController();

        var result = await controller.GetPending();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(merchants);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMerchantDoesNotExist()
    {
        _merchantService.Setup(service => service.GetByIdAsync(5)).ReturnsAsync((MerchantDto?)null);
        var controller = CreateController();

        var result = await controller.GetById(5);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_ReturnsMerchant_WhenItExists()
    {
        var merchant = new MerchantDto { Id = 5, CompanyName = "Contoso" };
        _merchantService.Setup(service => service.GetByIdAsync(5)).ReturnsAsync(merchant);
        var controller = CreateController();

        var result = await controller.GetById(5);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(merchant);
    }

    [Fact]
    public async Task Approve_ReturnsNotFound_WhenMerchantDoesNotExist()
    {
        _merchantService.Setup(service => service.ApproveAsync(7)).ReturnsAsync(false);
        var controller = CreateController();

        var result = await controller.Approve(7);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Approve_ReturnsOk_WhenMerchantIsApproved()
    {
        _merchantService.Setup(service => service.ApproveAsync(7)).ReturnsAsync(true);
        var controller = CreateController();

        var result = await controller.Approve(7);

        result.Should().BeOfType<OkObjectResult>();
        result.GetAnonymousString("message").Should().Be("Merchant approvato con successo");
    }

    [Fact]
    public async Task Reject_ReturnsNotFound_WhenMerchantDoesNotExist()
    {
        _merchantService.Setup(service => service.RejectAsync(7)).ReturnsAsync(false);
        var controller = CreateController();

        var result = await controller.Reject(7);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Reject_ReturnsOk_WhenMerchantIsRejected()
    {
        _merchantService.Setup(service => service.RejectAsync(7)).ReturnsAsync(true);
        var controller = CreateController();

        var result = await controller.Reject(7);

        result.Should().BeOfType<OkObjectResult>();
        result.GetAnonymousString("message").Should().Be("Merchant rifiutato con successo");
    }

    [Fact]
    public async Task Update_ReturnsForbid_WhenMerchantTriesToUpdateAnotherMerchant()
    {
        var controller = CreateController(
            new Claim(ClaimTypes.Role, "Merchant"),
            new Claim("MerchantId", "10"));

        var result = await controller.Update(7, new UpdateMerchantRequest { CompanyName = "Updated" });

        result.Result.Should().BeOfType<ForbidResult>();
        _merchantService.Verify(service => service.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateMerchantRequest>()), Times.Never);
    }

    [Fact]
    public async Task Update_AllowsAdminWithoutMerchantClaim()
    {
        var request = new UpdateMerchantRequest { CompanyName = "Updated" };
        var merchant = new MerchantDto { Id = 7, CompanyName = "Updated" };
        _merchantService.Setup(service => service.UpdateAsync(7, request)).ReturnsAsync(merchant);
        var controller = CreateController(new Claim(ClaimTypes.Role, "Admin"));

        var result = await controller.Update(7, request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(merchant);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenMerchantOwnsRecordButServiceReturnsNull()
    {
        var request = new UpdateMerchantRequest { CompanyName = "Updated" };
        _merchantService.Setup(service => service.UpdateAsync(7, request)).ReturnsAsync((MerchantDto?)null);
        var controller = CreateController(
            new Claim(ClaimTypes.Role, "Merchant"),
            new Claim("MerchantId", "7"));

        var result = await controller.Update(7, request);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    private MerchantsController CreateController(params Claim[] claims)
    {
        return new MerchantsController(_merchantService.Object, _merchantRoleService.Object).WithUser(claims);
    }
}