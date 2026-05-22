using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests.Controllers;

public class EmployeeDocumentsControllerTests
{
    private readonly Mock<IHRDocumentService> _hrDocumentService = new();
    private readonly Mock<IEmployeeService> _employeeService = new();

    [Fact]
    public async Task GetUploadTargets_ReturnsForbid_WhenOperatorLevelIsMissing()
    {
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.GetUploadTargets();

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetUploadTargets_ReturnsOk_WithOnlyActiveEmployees()
    {
        var employees = new List<EmployeeDto>
        {
            new() { Id = 1, FirstName = "Active", LastName = "One", IsActive = true, Kind = EmployeeKind.Internal, HasUserAccount = true },
            new() { Id = 2, FirstName = "Inactive", LastName = "Two", IsActive = false, Kind = EmployeeKind.External, HasUserAccount = false }
        };
        _employeeService.Setup(service => service.GetMerchantEmployeesAsync(7, null)).ReturnsAsync(employees);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:Operator"));

        var result = await controller.GetUploadTargets();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var targets = ok.Value.Should().BeAssignableTo<IEnumerable<DocumentUploadTargetDto>>().Subject.ToList();
        targets.Should().HaveCount(1);
        targets[0].EmployeeId.Should().Be(1);
    }

    [Fact]
    public async Task GetMyDocuments_ReturnsBadRequest_WhenIdentityIsMissing()
    {
        var controller = CreateController(new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.GetMyDocuments();

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Token non valido");
    }

    [Fact]
    public async Task GetMyDocuments_ReturnsOk_WhenIdentityAndFeatureAreValid()
    {
        var documents = new List<HRDocumentDto> { new() { Id = 1 } };
        _hrDocumentService.Setup(service => service.GetEmployeeDocumentsAsync(7, 11, HRDocumentType.Contract, 2026, 2)).ReturnsAsync(documents);
        var controller = CreateController(
            new Claim("MerchantId", "7"),
            new Claim("EmployeeId", "11"),
            new Claim("Feature", "Documenti"),
            new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.GetMyDocuments(HRDocumentType.Contract, 2026, 2);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(documents);
    }

    [Fact]
    public async Task GetMyDocumentById_ReturnsNotFound_WhenDocumentDoesNotExist()
    {
        _hrDocumentService.Setup(service => service.GetEmployeeDocumentByIdAsync(5, 7, 11)).ReturnsAsync((HRDocumentDetailDto?)null);
        var controller = CreateController(
            new Claim("MerchantId", "7"),
            new Claim("EmployeeId", "11"),
            new Claim("Feature", "Documenti"),
            new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.GetMyDocumentById(5);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Documento non trovato");
    }

    [Fact]
    public async Task GetMyDocumentById_ReturnsOk_WhenDocumentExists()
    {
        var document = new HRDocumentDetailDto { Id = 5 };
        _hrDocumentService.Setup(service => service.GetEmployeeDocumentByIdAsync(5, 7, 11)).ReturnsAsync(document);
        var controller = CreateController(
            new Claim("MerchantId", "7"),
            new Claim("EmployeeId", "11"),
            new Claim("Feature", "Documenti"),
            new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.GetMyDocumentById(5);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(document);
    }

    [Fact]
    public async Task DownloadMyDocument_ReturnsForbid_WhenServiceThrowsUnauthorizedAccessException()
    {
        _hrDocumentService.Setup(service => service.GenerateEmployeeDownloadUrlAsync(5, 7, 11, 2)).ThrowsAsync(new UnauthorizedAccessException());
        var controller = CreateController(
            new Claim("MerchantId", "7"),
            new Claim("EmployeeId", "11"),
            new Claim("Feature", "Documenti"),
            new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.DownloadMyDocument(5, 2);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DownloadMyDocument_ReturnsNotFound_WhenFileDoesNotExist()
    {
        _hrDocumentService.Setup(service => service.GenerateEmployeeDownloadUrlAsync(5, 7, 11, 2)).ThrowsAsync(new FileNotFoundException("Versione non trovata"));
        var controller = CreateController(
            new Claim("MerchantId", "7"),
            new Claim("EmployeeId", "11"),
            new Claim("Feature", "Documenti"),
            new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.DownloadMyDocument(5, 2);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Versione non trovata");
    }

    [Fact]
    public async Task DownloadMyDocument_ReturnsOk_WhenServiceSucceeds()
    {
        var download = new HRDocumentDownloadDto { FileName = "doc.pdf" };
        _hrDocumentService.Setup(service => service.GenerateEmployeeDownloadUrlAsync(5, 7, 11, 2)).ReturnsAsync(download);
        var controller = CreateController(
            new Claim("MerchantId", "7"),
            new Claim("EmployeeId", "11"),
            new Claim("Feature", "Documenti"),
            new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.DownloadMyDocument(5, 2);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(download);
    }

    [Fact]
    public async Task AcknowledgeVersion_ReturnsOk_WithAlreadyConfirmedFalse_WhenServiceCreatesAcknowledgement()
    {
        _hrDocumentService.Setup(service => service.AcknowledgeVersionAsync(5, 7, 11, 2)).ReturnsAsync(true);
        var controller = CreateController(
            new Claim("MerchantId", "7"),
            new Claim("EmployeeId", "11"),
            new Claim("Feature", "Documenti"),
            new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.AcknowledgeVersion(5, 2);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.GetAnonymousBool("acknowledged").Should().BeTrue();
        ok.GetAnonymousBool("alreadyConfirmed").Should().BeFalse();
    }

    [Fact]
    public async Task AcknowledgeVersion_ReturnsOk_WithAlreadyConfirmedTrue_WhenServiceReturnsFalse()
    {
        _hrDocumentService.Setup(service => service.AcknowledgeVersionAsync(5, 7, 11, 2)).ReturnsAsync(false);
        var controller = CreateController(
            new Claim("MerchantId", "7"),
            new Claim("EmployeeId", "11"),
            new Claim("Feature", "Documenti"),
            new Claim("FeatureLevel", "Documenti:ReadOnly"));

        var result = await controller.AcknowledgeVersion(5, 2);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.GetAnonymousBool("acknowledged").Should().BeTrue();
        ok.GetAnonymousBool("alreadyConfirmed").Should().BeTrue();
    }

    [Fact]
    public async Task GetAccessLog_ReturnsOk_WhenOperatorLevelIsPresent()
    {
        var rows = new List<HRDocumentAccessRowDto> { new() { EmployeeId = 11 } };
        _hrDocumentService.Setup(service => service.GetDocumentAccessLogAsync(5, 7)).ReturnsAsync(rows);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:Operator"));

        var result = await controller.GetAccessLog(5);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(rows);
    }

    [Fact]
    public async Task CreateDocumentForEmployee_ReturnsCreatedAtAction_WhenServiceSucceeds()
    {
        var request = new HRDocumentCreateDto();
        var response = new HRDocumentUploadResponseDto { DocumentId = 5 };
        _hrDocumentService.Setup(service => service.CreateDocumentAsync(7, 12, request)).ReturnsAsync(response);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:Operator"));

        var result = await controller.CreateDocumentForEmployee(request);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(EmployeeDocumentsController.GetMyDocumentById));
        created.RouteValues!["id"].Should().Be(5);
        created.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task CreateDocumentForEmployee_ReturnsForbid_WhenServiceThrowsUnauthorizedAccessException()
    {
        var request = new HRDocumentCreateDto();
        _hrDocumentService.Setup(service => service.CreateDocumentAsync(7, 12, request)).ThrowsAsync(new UnauthorizedAccessException());
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:Operator"));

        var result = await controller.CreateDocumentForEmployee(request);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CreateDocumentForEmployee_ReturnsServerError_WhenUnexpectedExceptionOccurs()
    {
        var request = new HRDocumentCreateDto();
        _hrDocumentService.Setup(service => service.CreateDocumentAsync(7, 12, request)).ThrowsAsync(new Exception("boom"));
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:Operator"));

        var result = await controller.CreateDocumentForEmployee(request);

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        objectResult.GetAnonymousString("message").Should().Be("Errore durante la creazione del documento");
    }

    [Fact]
    public async Task AddVersion_ReturnsCreatedAtAction_WhenServiceSucceeds()
    {
        var response = new HRDocumentVersionUploadResponseDto { VersionId = 3, VersionNumber = 2 };
        _hrDocumentService.Setup(service => service.AddDocumentVersionAsync(5, 7, 12, "note")).ReturnsAsync(response);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:Operator"));

        var result = await controller.AddVersion(5, new AddVersionRequest { ChangeNotes = "note" });

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(EmployeeDocumentsController.GetMyDocumentById));
        created.RouteValues!["id"].Should().Be(5);
        created.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task AddVersion_ReturnsForbid_WhenServiceThrowsUnauthorizedAccessException()
    {
        _hrDocumentService.Setup(service => service.AddDocumentVersionAsync(5, 7, 12, "note")).ThrowsAsync(new UnauthorizedAccessException());
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:Operator"));

        var result = await controller.AddVersion(5, new AddVersionRequest { ChangeNotes = "note" });

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task FinalizeUpload_ReturnsBadRequest_WhenServiceReturnsFalse()
    {
        var request = new HRDocumentFinalizeDto();
        _hrDocumentService.Setup(service => service.FinalizeDocumentUploadAsync(5, 7, request)).ReturnsAsync(false);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:Operator"));

        var result = await controller.FinalizeUpload(5, request);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Finalize non riuscita: file non valido o upload incompleto");
    }

    [Fact]
    public async Task FinalizeUpload_ReturnsOk_WhenServiceReturnsTrue()
    {
        var request = new HRDocumentFinalizeDto();
        _hrDocumentService.Setup(service => service.FinalizeDocumentUploadAsync(5, 7, request)).ReturnsAsync(true);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:Operator"));

        var result = await controller.FinalizeUpload(5, request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.GetAnonymousBool("success").Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDocument_ReturnsForbid_WhenManagerLevelIsMissing()
    {
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:Operator"));

        var result = await controller.DeleteDocument(5);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteDocument_ReturnsNotFound_WhenDocumentDoesNotExist()
    {
        _hrDocumentService.Setup(service => service.DeleteDocumentAsync(5, 7)).ReturnsAsync(false);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:Manager"));

        var result = await controller.DeleteDocument(5);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Documento non trovato");
    }

    [Fact]
    public async Task DeleteDocument_ReturnsNoContent_WhenDocumentIsDeleted()
    {
        _hrDocumentService.Setup(service => service.DeleteDocumentAsync(5, 7)).ReturnsAsync(true);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim("Feature", "Documenti"), new Claim("FeatureLevel", "Documenti:Manager"));

        var result = await controller.DeleteDocument(5);

        result.Should().BeOfType<NoContentResult>();
    }

    private EmployeeDocumentsController CreateController(params Claim[] claims)
    {
        return new EmployeeDocumentsController(_hrDocumentService.Object, _employeeService.Object).WithUser(claims);
    }
}