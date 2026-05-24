using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests.Controllers;

public class MerchantRolesControllerTests
{
    private readonly Mock<IMerchantRoleService> _merchantRoleService = new();

    [Fact]
    public async Task GetAll_ReturnsBadRequest_WhenMerchantIdClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.GetAll();

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _merchantRoleService.Verify(service => service.GetRolesAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_ReturnsRoles_ForCurrentMerchant()
    {
        var roles = new List<MerchantRoleDto> { new() { Id = 5, MerchantId = 12, Name = "Manager" } };
        _merchantRoleService.Setup(service => service.GetRolesAsync(12)).ReturnsAsync(roles);
        var controller = CreateController(merchantId: 12);

        var result = await controller.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(roles);
    }

    [Fact]
    public async Task GetById_ReturnsBadRequest_WhenMerchantIdClaimIsInvalid()
    {
        var controller = CreateController(rawMerchantId: "oops");

        var result = await controller.GetById(10);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _merchantRoleService.Verify(service => service.GetByIdAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenServiceReturnsNull()
    {
        _merchantRoleService.Setup(service => service.GetByIdAsync(10, 12)).ReturnsAsync((MerchantRoleDto?)null);
        var controller = CreateController(merchantId: 12);

        var result = await controller.GetById(10);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenMerchantIdClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.Create(new CreateMerchantRoleRequest());

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _merchantRoleService.Verify(service => service.CreateAsync(It.IsAny<int>(), It.IsAny<CreateMerchantRoleRequest>()), Times.Never);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenServiceSucceeds()
    {
        var request = new CreateMerchantRoleRequest { Name = "Lead" };
        var created = new MerchantRoleDto { Id = 4, MerchantId = 12, Name = "Lead" };
        _merchantRoleService.Setup(service => service.CreateAsync(12, request)).ReturnsAsync(created);
        var controller = CreateController(merchantId: 12);

        var result = await controller.Create(request);

        var createdAt = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAt.ActionName.Should().Be(nameof(MerchantRolesController.GetById));
        createdAt.RouteValues!["id"].Should().Be(4);
        createdAt.Value.Should().BeSameAs(created);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenServiceThrows()
    {
        var request = new CreateMerchantRoleRequest { Name = "Lead" };
        _merchantRoleService
            .Setup(service => service.CreateAsync(12, request))
            .ThrowsAsync(new InvalidOperationException("duplicate"));
        var controller = CreateController(merchantId: 12);

        var result = await controller.Create(request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Contain("duplicate");
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenMerchantIdClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.Update(10, new UpdateMerchantRoleRequest());

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _merchantRoleService.Verify(service => service.UpdateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UpdateMerchantRoleRequest>()), Times.Never);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenServiceReturnsNull()
    {
        _merchantRoleService.Setup(service => service.UpdateAsync(10, 12, It.IsAny<UpdateMerchantRoleRequest>())).ReturnsAsync((MerchantRoleDto?)null);
        var controller = CreateController(merchantId: 12);

        var result = await controller.Update(10, new UpdateMerchantRoleRequest { Name = "Lead" });

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_WhenMerchantIdClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.Delete(10);

        result.Should().BeOfType<BadRequestObjectResult>();
        _merchantRoleService.Verify(service => service.DeleteAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ReturnsConflict_WhenServiceThrowsInvalidOperation()
    {
        _merchantRoleService.Setup(service => service.DeleteAsync(10, 12)).ThrowsAsync(new InvalidOperationException("default role"));
        var controller = CreateController(merchantId: 12);

        var result = await controller.Delete(10);

        var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.GetAnonymousString("message").Should().Be("default role");
    }

    [Fact]
    public async Task Delete_ReturnsOk_WhenServiceDeletesRole()
    {
        _merchantRoleService.Setup(service => service.DeleteAsync(10, 12)).ReturnsAsync(true);
        var controller = CreateController(merchantId: 12);

        var result = await controller.Delete(10);

        result.Should().BeOfType<OkObjectResult>();
        result.GetAnonymousString("message").Should().Be("Ruolo eliminato con successo");
    }

    private MerchantRolesController CreateController(int? merchantId = null, string? rawMerchantId = null)
    {
        var controller = new MerchantRolesController(_merchantRoleService.Object);
        if (merchantId.HasValue)
        {
            return controller.WithUser(new Claim("MerchantId", merchantId.Value.ToString()));
        }

        if (rawMerchantId is not null)
        {
            return controller.WithUser(new Claim("MerchantId", rawMerchantId));
        }

        return controller.WithUser();
    }
}