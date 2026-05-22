using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests.Controllers;

public class EmployeeRequestsControllerTests
{
    private readonly Mock<IEmployeeRequestService> _requestService = new();

    [Fact]
    public async Task GetAll_ReturnsBadRequest_WhenMerchantClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.GetAll();

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Merchant ID non trovato nel token");
    }

    [Fact]
    public async Task GetAll_ReturnsRequests_WhenMerchantClaimExists()
    {
        var requests = new List<EmployeeRequestDto> { new() { Id = 1 } };
        _requestService.Setup(service => service.GetMerchantRequestsAsync(7, RequestStatus.Pending)).ReturnsAsync(requests);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetAll(RequestStatus.Pending);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(requests);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenRequestDoesNotExist()
    {
        _requestService.Setup(service => service.GetByIdAsync(3, 7)).ReturnsAsync((EmployeeRequestDto?)null);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetById(3);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Richiesta non trovata");
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenRequestExists()
    {
        var request = new EmployeeRequestDto { Id = 3 };
        _requestService.Setup(service => service.GetByIdAsync(3, 7)).ReturnsAsync(request);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetById(3);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(request);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenEmployeeClaimIsMissing()
    {
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Create(new CreateEmployeeRequestRequest());

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Employee ID non trovato nel token");
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenServiceThrows()
    {
        var request = new CreateEmployeeRequestRequest();
        _requestService.Setup(service => service.CreateAsync(11, 7, request)).ThrowsAsync(new Exception("boom"));
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"));

        var result = await controller.Create(request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Errore nella creazione della richiesta: boom");
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenCreationSucceeds()
    {
        var request = new CreateEmployeeRequestRequest();
        var created = new EmployeeRequestDto { Id = 8 };
        _requestService.Setup(service => service.CreateAsync(11, 7, request)).ReturnsAsync(created);
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"));

        var result = await controller.Create(request);

        var response = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        response.ActionName.Should().Be(nameof(EmployeeRequestsController.GetById));
        response.RouteValues!["id"].Should().Be(8);
        response.Value.Should().BeSameAs(created);
    }

    [Fact]
    public async Task Approve_ReturnsBadRequest_WhenUserClaimIsMissing()
    {
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Approve(5);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("User ID non trovato nel token");
    }

    [Fact]
    public async Task Approve_ReturnsNotFound_WhenServiceReturnsNull()
    {
        _requestService.Setup(service => service.ApproveAsync(5, 7, 12, null)).ReturnsAsync((EmployeeRequestDto?)null);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"));

        var result = await controller.Approve(5);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Richiesta non trovata o non autorizzata");
    }

    [Fact]
    public async Task Approve_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        var body = new ReviewEmployeeRequestRequest();
        _requestService.Setup(service => service.ApproveAsync(5, 7, 12, body)).ThrowsAsync(new InvalidOperationException("Stato non valido"));
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"));

        var result = await controller.Approve(5, body);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Stato non valido");
    }

    [Fact]
    public async Task Approve_ReturnsOk_WhenServiceSucceeds()
    {
        var body = new ReviewEmployeeRequestRequest();
        var approved = new EmployeeRequestDto { Id = 5 };
        _requestService.Setup(service => service.ApproveAsync(5, 7, 12, body)).ReturnsAsync(approved);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"));

        var result = await controller.Approve(5, body);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(approved);
    }

    [Fact]
    public async Task Reject_ReturnsNotFound_WhenServiceReturnsNull()
    {
        _requestService.Setup(service => service.RejectAsync(5, 7, 12, null)).ReturnsAsync((EmployeeRequestDto?)null);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"));

        var result = await controller.Reject(5);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Richiesta non trovata o non autorizzata");
    }

    [Fact]
    public async Task Reject_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        var body = new ReviewEmployeeRequestRequest();
        _requestService.Setup(service => service.RejectAsync(5, 7, 12, body)).ThrowsAsync(new InvalidOperationException("Stato non valido"));
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"));

        var result = await controller.Reject(5, body);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Stato non valido");
    }

    [Fact]
    public async Task Reject_ReturnsOk_WhenServiceSucceeds()
    {
        var body = new ReviewEmployeeRequestRequest();
        var rejected = new EmployeeRequestDto { Id = 5 };
        _requestService.Setup(service => service.RejectAsync(5, 7, 12, body)).ReturnsAsync(rejected);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"));

        var result = await controller.Reject(5, body);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(rejected);
    }

    [Fact]
    public async Task GetMyRequests_ReturnsBadRequest_WhenEmployeeClaimIsMissing()
    {
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetMyRequests();

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Employee ID non trovato nel token");
    }

    [Fact]
    public async Task GetMyRequests_ReturnsOk_WhenIdentityIsValid()
    {
        var requests = new List<EmployeeRequestDto> { new() { Id = 8 } };
        _requestService.Setup(service => service.GetEmployeeRequestsAsync(11, 7, RequestStatus.Approved)).ReturnsAsync(requests);
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"));

        var result = await controller.GetMyRequests(RequestStatus.Approved);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(requests);
    }

    private EmployeeRequestsController CreateController(params Claim[] claims)
    {
        return new EmployeeRequestsController(_requestService.Object).WithUser(claims);
    }
}