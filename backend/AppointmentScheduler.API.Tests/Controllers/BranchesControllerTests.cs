using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests.Controllers;

public class BranchesControllerTests
{
    private readonly Mock<IBranchService> _branchService = new();

    [Fact]
    public async Task GetAll_ReturnsBadRequest_WhenMerchantClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.GetAll();

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Merchant ID non trovato nel token");
    }

    [Fact]
    public async Task GetAll_ReturnsBranches_WhenMerchantClaimIsPresent()
    {
        var branches = new List<MerchantBranchDto> { new() { Id = 1, Name = "HQ" } };
        _branchService.Setup(service => service.GetBranchesAsync(5, false)).ReturnsAsync(branches);
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.GetAll(false);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(branches);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenBranchDoesNotExist()
    {
        _branchService.Setup(service => service.GetBranchByIdAsync(3, 5)).ReturnsAsync((MerchantBranchDto?)null);
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.GetById(3);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Filiale non trovata");
    }

    [Fact]
    public async Task GetById_ReturnsBranch_WhenItExists()
    {
        var branch = new MerchantBranchDto { Id = 3, Name = "HQ" };
        _branchService.Setup(service => service.GetBranchByIdAsync(3, 5)).ReturnsAsync(branch);
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.GetById(3);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(branch);
    }

    private BranchesController CreateController(params Claim[] claims)
    {
        return new BranchesController(_branchService.Object).WithUser(claims);
    }
}
