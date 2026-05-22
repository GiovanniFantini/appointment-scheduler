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

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenMerchantClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.Create(new CreateBranchRequest { Name = "HQ" });

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Merchant ID non trovato nel token");
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        var request = new CreateBranchRequest { Name = "HQ" };
        _branchService.Setup(service => service.CreateBranchAsync(5, request)).ThrowsAsync(new InvalidOperationException("Vincolo violato"));
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.Create(request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Vincolo violato");
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenServiceSucceeds()
    {
        var request = new CreateBranchRequest { Name = "HQ" };
        var branch = new MerchantBranchDto { Id = 4, Name = "HQ" };
        _branchService.Setup(service => service.CreateBranchAsync(5, request)).ReturnsAsync(branch);
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.Create(request);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(BranchesController.GetById));
        created.RouteValues!["id"].Should().Be(4);
        created.Value.Should().BeSameAs(branch);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        var request = new UpdateBranchRequest { Name = "HQ updated" };
        _branchService.Setup(service => service.UpdateBranchAsync(3, 5, request)).ThrowsAsync(new InvalidOperationException("Vincolo violato"));
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.Update(3, request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Vincolo violato");
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenBranchDoesNotExist()
    {
        var request = new UpdateBranchRequest { Name = "HQ updated" };
        _branchService.Setup(service => service.UpdateBranchAsync(3, 5, request)).ReturnsAsync((MerchantBranchDto?)null);
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.Update(3, request);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Filiale non trovata");
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenBranchIsUpdated()
    {
        var request = new UpdateBranchRequest { Name = "HQ updated" };
        var branch = new MerchantBranchDto { Id = 3, Name = "HQ updated" };
        _branchService.Setup(service => service.UpdateBranchAsync(3, 5, request)).ReturnsAsync(branch);
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.Update(3, request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(branch);
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        _branchService.Setup(service => service.DeleteBranchAsync(3, 5)).ThrowsAsync(new InvalidOperationException("Vincolo violato"));
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.Delete(3);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Vincolo violato");
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenBranchDoesNotExist()
    {
        _branchService.Setup(service => service.DeleteBranchAsync(3, 5)).ReturnsAsync(false);
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.Delete(3);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Filiale non trovata");
    }

    [Fact]
    public async Task Delete_ReturnsOk_WhenBranchIsDeleted()
    {
        _branchService.Setup(service => service.DeleteBranchAsync(3, 5)).ReturnsAsync(true);
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.Delete(3);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.GetAnonymousString("message").Should().Be("Filiale eliminata");
    }

    [Fact]
    public async Task SetHeadquarters_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        _branchService.Setup(service => service.SetHeadquartersAsync(3, 5)).ThrowsAsync(new InvalidOperationException("Vincolo violato"));
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.SetHeadquarters(3);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Vincolo violato");
    }

    [Fact]
    public async Task SetHeadquarters_ReturnsNotFound_WhenBranchDoesNotExist()
    {
        _branchService.Setup(service => service.SetHeadquartersAsync(3, 5)).ReturnsAsync(false);
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.SetHeadquarters(3);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Filiale non trovata");
    }

    [Fact]
    public async Task SetHeadquarters_ReturnsOk_WhenBranchIsPromoted()
    {
        _branchService.Setup(service => service.SetHeadquartersAsync(3, 5)).ReturnsAsync(true);
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.SetHeadquarters(3);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.GetAnonymousString("message").Should().Be("Sede principale aggiornata");
    }

    [Fact]
    public async Task CreateDepartment_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        var request = new CreateDepartmentRequest { Name = "Sales" };
        _branchService.Setup(service => service.CreateDepartmentAsync(2, 5, request)).ThrowsAsync(new InvalidOperationException("Vincolo violato"));
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.CreateDepartment(2, request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Vincolo violato");
    }

    [Fact]
    public async Task CreateDepartment_ReturnsOk_WhenDepartmentIsCreated()
    {
        var request = new CreateDepartmentRequest { Name = "Sales" };
        var department = new DepartmentDto { Id = 7, Name = "Sales", BranchId = 2 };
        _branchService.Setup(service => service.CreateDepartmentAsync(2, 5, request)).ReturnsAsync(department);
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.CreateDepartment(2, request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(department);
    }

    [Fact]
    public async Task UpdateDepartment_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        var request = new UpdateDepartmentRequest { Name = "Sales updated" };
        _branchService.Setup(service => service.UpdateDepartmentAsync(7, 5, request)).ThrowsAsync(new InvalidOperationException("Vincolo violato"));
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.UpdateDepartment(7, request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Vincolo violato");
    }

    [Fact]
    public async Task UpdateDepartment_ReturnsNotFound_WhenDepartmentDoesNotExist()
    {
        var request = new UpdateDepartmentRequest { Name = "Sales updated" };
        _branchService.Setup(service => service.UpdateDepartmentAsync(7, 5, request)).ReturnsAsync((DepartmentDto?)null);
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.UpdateDepartment(7, request);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Reparto non trovato");
    }

    [Fact]
    public async Task UpdateDepartment_ReturnsOk_WhenDepartmentIsUpdated()
    {
        var request = new UpdateDepartmentRequest { Name = "Sales updated" };
        var department = new DepartmentDto { Id = 7, Name = "Sales updated", BranchId = 2 };
        _branchService.Setup(service => service.UpdateDepartmentAsync(7, 5, request)).ReturnsAsync(department);
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.UpdateDepartment(7, request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(department);
    }

    [Fact]
    public async Task DeleteDepartment_ReturnsNotFound_WhenDepartmentDoesNotExist()
    {
        _branchService.Setup(service => service.DeleteDepartmentAsync(7, 5)).ReturnsAsync(false);
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.DeleteDepartment(7);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Reparto non trovato");
    }

    [Fact]
    public async Task DeleteDepartment_ReturnsOk_WhenDepartmentIsDeleted()
    {
        _branchService.Setup(service => service.DeleteDepartmentAsync(7, 5)).ReturnsAsync(true);
        var controller = CreateController(new Claim("MerchantId", "5"));

        var result = await controller.DeleteDepartment(7);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.GetAnonymousString("message").Should().Be("Reparto eliminato");
    }

    private BranchesController CreateController(params Claim[] claims)
    {
        return new BranchesController(_branchService.Object).WithUser(claims);
    }
}