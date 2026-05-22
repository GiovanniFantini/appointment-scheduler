using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests.Controllers;

public class EmployeeProfileControllerTests
{
    private readonly Mock<IEmployeeService> _employeeService = new();

    [Fact]
    public async Task GetMySkills_ReturnsBadRequest_WhenEmployeeClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.GetMySkills();

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _employeeService.Verify(service => service.GetByIdAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetMySkills_ReturnsBadRequest_WhenMerchantClaimIsMissing()
    {
        var controller = CreateController(employeeId: 5);

        var result = await controller.GetMySkills();

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _employeeService.Verify(service => service.GetByIdAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetMySkills_ReturnsEmptyList_WhenEmployeeIsNotFound()
    {
        _employeeService.Setup(service => service.GetByIdAsync(5, 12)).ReturnsAsync((EmployeeDto?)null);
        var controller = CreateController(employeeId: 5, merchantId: 12);

        var result = await controller.GetMySkills();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<List<EmployeeSkillDto>>().Which.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMySkills_ReturnsEmployeeSkills_WhenEmployeeExists()
    {
        var skills = new List<EmployeeSkillDto>
        {
            new() { SkillId = 1, SkillName = "Cashier", SkillColor = "#ffffff" }
        };
        _employeeService.Setup(service => service.GetByIdAsync(5, 12)).ReturnsAsync(new EmployeeDto
        {
            Id = 5,
            FirstName = "Ada",
            LastName = "Lovelace",
            Skills = skills
        });
        var controller = CreateController(employeeId: 5, merchantId: 12);

        var result = await controller.GetMySkills();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(skills);
    }

    private EmployeeProfileController CreateController(int? employeeId = null, int? merchantId = null)
    {
        var claims = new List<Claim>();
        if (employeeId.HasValue)
        {
            claims.Add(new Claim("EmployeeId", employeeId.Value.ToString()));
        }

        if (merchantId.HasValue)
        {
            claims.Add(new Claim("MerchantId", merchantId.Value.ToString()));
        }

        return new EmployeeProfileController(_employeeService.Object).WithUser(claims.ToArray());
    }
}