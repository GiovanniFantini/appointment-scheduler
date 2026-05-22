using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests.Controllers;

public class HRDocumentsControllerTests
{
    private readonly Mock<IHRDocumentService> _hrDocumentService = new();

    [Fact]
    public async Task GetDocuments_ReturnsForbid_WhenDocumentFeatureIsMissing()
    {
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetDocuments();

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetDocuments_ReturnsBadRequest_WhenTenantClaimIsMissing()
    {
        var controller = CreateController(new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.GetDocuments();

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Tenant ID non trovato");
    }

    [Fact]
    public async Task GetDocuments_ReturnsOk_WhenIdentityAndFeatureAreValid()
    {
        var documents = new List<HRDocumentDto> { new() { Id = 1 } };
        _hrDocumentService.Setup(service => service.GetDocumentsAsync(7, 11, HRDocumentType.Contract, 2026, 2, HRDocumentStatus.Published)).ReturnsAsync(documents);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.GetDocuments(11, HRDocumentType.Contract, 2026, 2, HRDocumentStatus.Published);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(documents);
    }

    [Fact]
    public async Task GetDocumentById_ReturnsNotFound_WhenDocumentDoesNotExist()
    {
        _hrDocumentService.Setup(service => service.GetDocumentByIdAsync(5, 7)).ReturnsAsync((HRDocumentDetailDto?)null);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.GetDocumentById(5);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Documento non trovato");
    }

    [Fact]
    public async Task GetDocumentById_ReturnsOk_WhenDocumentExists()
    {
        var document = new HRDocumentDetailDto { Id = 5 };
        _hrDocumentService.Setup(service => service.GetDocumentByIdAsync(5, 7)).ReturnsAsync(document);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.GetDocumentById(5);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(document);
    }

    [Fact]
    public async Task GenerateDownloadUrl_ReturnsForbid_WhenServiceThrowsUnauthorizedAccessException()
    {
        _hrDocumentService.Setup(service => service.GenerateDownloadUrlAsync(5, 7, 2)).ThrowsAsync(new UnauthorizedAccessException());
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.GenerateDownloadUrl(5, 2);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GenerateDownloadUrl_ReturnsNotFound_WhenFileDoesNotExist()
    {
        _hrDocumentService.Setup(service => service.GenerateDownloadUrlAsync(5, 7, 2)).ThrowsAsync(new FileNotFoundException("Versione non trovata"));
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.GenerateDownloadUrl(5, 2);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Versione non trovata");
    }

    [Fact]
    public async Task GenerateDownloadUrl_ReturnsServerError_WhenUnexpectedExceptionOccurs()
    {
        _hrDocumentService.Setup(service => service.GenerateDownloadUrlAsync(5, 7, 2)).ThrowsAsync(new Exception("boom"));
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.GenerateDownloadUrl(5, 2);

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        objectResult.GetAnonymousString("message").Should().Be("Errore durante la generazione URL");
    }

    [Fact]
    public async Task GenerateDownloadUrl_ReturnsOk_WhenServiceSucceeds()
    {
        var download = new HRDocumentDownloadDto { FileName = "doc.pdf" };
        _hrDocumentService.Setup(service => service.GenerateDownloadUrlAsync(5, 7, 2)).ReturnsAsync(download);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.GenerateDownloadUrl(5, 2);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(download);
    }

    [Fact]
    public async Task GetAccessLog_ReturnsOk_WhenIdentityAndFeatureAreValid()
    {
        var rows = new List<HRDocumentAccessRowDto> { new() { EmployeeId = 11 } };
        _hrDocumentService.Setup(service => service.GetDocumentAccessLogAsync(5, 7)).ReturnsAsync(rows);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.GetAccessLog(5);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(rows);
    }

    private HRDocumentsController CreateController(params Claim[] claims)
    {
        return new HRDocumentsController(_hrDocumentService.Object).WithUser(claims);
    }
}