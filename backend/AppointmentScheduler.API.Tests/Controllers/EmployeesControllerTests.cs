using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests.Controllers;

public class EmployeesControllerTests
{
    private readonly Mock<IEmployeeService> _employeeService = new();

    [Fact]
    public async Task GetAll_ReturnsBadRequest_WhenMerchantClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.GetAll();

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Merchant ID non trovato nel token");
    }

    [Fact]
    public async Task GetAll_ReturnsEmployees_WhenMerchantClaimExists()
    {
        var employees = new List<EmployeeDto> { new() { Id = 1, FirstName = "Jane", LastName = "Doe" } };
        _employeeService.Setup(service => service.GetMerchantEmployeesAsync(7, EmployeeKind.Internal)).ReturnsAsync(employees);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetAll(EmployeeKind.Internal);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(employees);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenEmployeeDoesNotExist()
    {
        _employeeService.Setup(service => service.GetByIdAsync(3, 7)).ReturnsAsync((EmployeeDto?)null);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetById(3);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Dipendente non trovato o non autorizzato");
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenEmployeeExists()
    {
        var employee = new EmployeeDto { Id = 3, FirstName = "Jane", LastName = "Doe" };
        _employeeService.Setup(service => service.GetByIdAsync(3, 7)).ReturnsAsync(employee);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetById(3);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(employee);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenMerchantClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.Create(new CreateEmployeeRequest());

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Merchant ID non trovato nel token");
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenServiceThrows()
    {
        var request = new CreateEmployeeRequest();
        _employeeService.Setup(service => service.CreateAsync(7, request)).ThrowsAsync(new Exception("boom"));
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Create(request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Errore nella creazione del dipendente: boom");
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenServiceSucceeds()
    {
        var request = new CreateEmployeeRequest();
        var employee = new EmployeeDto { Id = 4, FirstName = "Jane", LastName = "Doe" };
        _employeeService.Setup(service => service.CreateAsync(7, request)).ReturnsAsync(employee);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Create(request);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(EmployeesController.GetById));
        created.RouteValues!["id"].Should().Be(4);
        created.Value.Should().BeSameAs(employee);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        var request = new UpdateEmployeeRequest();
        _employeeService.Setup(service => service.UpdateAsync(3, 7, request)).ThrowsAsync(new InvalidOperationException("Filiale non valida"));
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Update(3, request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Filiale non valida");
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenEmployeeDoesNotExist()
    {
        var request = new UpdateEmployeeRequest();
        _employeeService.Setup(service => service.UpdateAsync(3, 7, request)).ReturnsAsync((EmployeeDto?)null);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Update(3, request);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Dipendente non trovato o non autorizzato");
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenServiceSucceeds()
    {
        var request = new UpdateEmployeeRequest();
        var employee = new EmployeeDto { Id = 3, FirstName = "Jane", LastName = "Doe" };
        _employeeService.Setup(service => service.UpdateAsync(3, 7, request)).ReturnsAsync(employee);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Update(3, request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(employee);
    }

    [Fact]
    public async Task Remove_ReturnsNotFound_WhenEmployeeDoesNotExist()
    {
        _employeeService.Setup(service => service.RemoveFromMerchantAsync(3, 7)).ReturnsAsync(false);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Remove(3);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Dipendente non trovato o non autorizzato");
    }

    [Fact]
    public async Task Remove_ReturnsOk_WhenEmployeeIsRemoved()
    {
        _employeeService.Setup(service => service.RemoveFromMerchantAsync(3, 7)).ReturnsAsync(true);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Remove(3);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.GetAnonymousString("message").Should().Be("Dipendente rimosso dal merchant con successo");
    }

    private EmployeesController CreateController(params Claim[] claims)
    {
        return new EmployeesController(_employeeService.Object).WithUser(claims);
    }
}