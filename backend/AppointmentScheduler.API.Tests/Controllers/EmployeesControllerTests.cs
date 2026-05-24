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

    private EmployeesController CreateController(params Claim[] claims)
    {
        return new EmployeesController(_employeeService.Object).WithUser(claims);
    }
}
