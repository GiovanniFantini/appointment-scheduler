using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests.Controllers;

public class SkillsControllerTests
{
    private readonly Mock<ISkillService> _skillService = new();

    [Fact]
    public async Task GetAll_ReturnsBadRequest_WhenMerchantIdClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.GetAll();

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _skillService.Verify(service => service.GetAllByMerchantAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_ReturnsSkills_WhenMerchantIdClaimIsPresent()
    {
        var skills = new List<SkillDto> { new() { Id = 1, Name = "Cashier" } };
        _skillService.Setup(service => service.GetAllByMerchantAsync(12)).ReturnsAsync(skills);
        var controller = CreateController(12);

        var result = await controller.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(skills);
    }

    [Fact]
    public async Task GetById_ReturnsBadRequest_WhenMerchantIdClaimIsInvalid()
    {
        var controller = CreateController(rawMerchantId: "bad");

        var result = await controller.GetById(4);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _skillService.Verify(service => service.GetByIdAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenSkillDoesNotExist()
    {
        _skillService.Setup(service => service.GetByIdAsync(4, 12)).ReturnsAsync((SkillDto?)null);
        var controller = CreateController(12);

        var result = await controller.GetById(4);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_ReturnsSkill_WhenItExists()
    {
        var skill = new SkillDto { Id = 4, Name = "Cashier" };
        _skillService.Setup(service => service.GetByIdAsync(4, 12)).ReturnsAsync(skill);
        var controller = CreateController(12);

        var result = await controller.GetById(4);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(skill);
    }

    [Fact]
    public async Task GetEmployees_ReturnsBadRequest_WhenMerchantIdClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.GetEmployees(4);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _skillService.Verify(service => service.GetEmployeesBySkillAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetEmployees_ReturnsEmployees_WhenMerchantIdClaimIsPresent()
    {
        var employees = new List<EmployeeDto> { new() { Id = 8, FirstName = "Ada", LastName = "Lovelace" } };
        _skillService.Setup(service => service.GetEmployeesBySkillAsync(4, 12)).ReturnsAsync(employees);
        var controller = CreateController(12);

        var result = await controller.GetEmployees(4);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(employees);
    }

    [Fact]
    public async Task Suggested_ReturnsBadRequest_WhenMerchantIdClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.Suggested(4, new DateOnly(2026, 5, 22), new TimeOnly(9, 0), new TimeOnly(17, 0), 3);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _skillService.Verify(
            service => service.GetSuggestedEmployeesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly?>(), It.IsAny<TimeOnly?>(), It.IsAny<int?>()),
            Times.Never);
    }

    [Fact]
    public async Task Suggested_ReturnsSuggestedEmployees_WhenMerchantIdClaimIsPresent()
    {
        var suggested = new List<SuggestedEmployeeDto> { new() { EmployeeId = 8, FullName = "Ada Lovelace", IsAvailable = true } };
        var date = new DateOnly(2026, 5, 22);
        var start = new TimeOnly(9, 0);
        var end = new TimeOnly(17, 0);
        _skillService.Setup(service => service.GetSuggestedEmployeesAsync(12, 4, date, start, end, 3)).ReturnsAsync(suggested);
        var controller = CreateController(12);

        var result = await controller.Suggested(4, date, start, end, 3);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(suggested);
    }

    private SkillsController CreateController(int? merchantId = null, string? rawMerchantId = null)
    {
        var controller = new SkillsController(_skillService.Object);
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
