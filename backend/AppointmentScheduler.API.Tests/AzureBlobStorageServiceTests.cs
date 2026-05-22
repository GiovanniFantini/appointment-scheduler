namespace AppointmentScheduler.API.Tests;

public class AzureBlobStorageServiceTests
{
    private readonly AzureBlobStorageService _service = CreateService();

    [Fact]
    public void BuildBlobPath_UsesEmployeeFirstLayout_WithYearAndMonth()
    {
        var path = _service.BuildBlobPath(
            tenantId: 7,
            employeeId: 12,
            documentType: HRDocumentType.Invoice,
            year: 2026,
            month: 3,
            documentId: 55,
            versionNumber: 2,
            fileExtension: ".pdf");

        path.Should().Be("merchant-7/employee-12/2026/03/Fatture/doc-55_v2.pdf");
    }

    [Fact]
    public void BuildBlobPath_UsesNoMonthSegment_WhenYearWithoutMonth()
    {
        var path = _service.BuildBlobPath(
            tenantId: 7,
            employeeId: 12,
            documentType: HRDocumentType.PayrollStatement,
            year: 2026,
            month: null,
            documentId: 99,
            versionNumber: 1,
            fileExtension: "PDF");

        path.Should().Be("merchant-7/employee-12/2026/senza-mese-di-riferimento/Cedolini/doc-99_v1.pdf");
    }

    [Fact]
    public void BuildBlobPath_UsesNoYearSegment_WhenYearIsNull()
    {
        var path = _service.BuildBlobPath(
            tenantId: 7,
            employeeId: 12,
            documentType: HRDocumentType.Other,
            year: null,
            month: null,
            documentId: 100,
            versionNumber: 4,
            fileExtension: "docx");

        path.Should().Be("merchant-7/employee-12/senza-anno-di-riferimento/Altro/doc-100_v4.docx");
    }

    [Theory]
    [InlineData(HRDocumentType.Payslip, "BustePaga")]
    [InlineData(HRDocumentType.Contract, "Contratti")]
    [InlineData(HRDocumentType.Bonus, "Bonus")]
    [InlineData(HRDocumentType.Communication, "Comunicazioni")]
    [InlineData(HRDocumentType.LevelChange, "CambiLivello")]
    [InlineData(HRDocumentType.Certification, "Certificazioni")]
    [InlineData(HRDocumentType.DisciplinaryAction, "ProvvedimentiDisciplinari")]
    [InlineData(HRDocumentType.Invoice, "Fatture")]
    [InlineData(HRDocumentType.PayrollStatement, "Cedolini")]
    [InlineData(HRDocumentType.Other, "Altro")]
    public void BuildBlobPath_MapsEachDocumentTypeToExpectedItalianFolder(HRDocumentType documentType, string expectedFolder)
    {
        var path = _service.BuildBlobPath(
            tenantId: 9,
            employeeId: 21,
            documentType: documentType,
            year: 2025,
            month: 11,
            documentId: 300,
            versionNumber: 1,
            fileExtension: "pdf");

        path.Should().Contain($"/{expectedFolder}/");
    }

    [Fact]
    public void BuildBlobPath_Throws_WhenMonthSpecifiedWithoutYear()
    {
        var act = () => _service.BuildBlobPath(
            tenantId: 7,
            employeeId: 12,
            documentType: HRDocumentType.Invoice,
            year: null,
            month: 1,
            documentId: 1,
            versionNumber: 1,
            fileExtension: "pdf");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void BuildBlobPath_Throws_WhenMonthIsOutsideRange(int month)
    {
        var act = () => _service.BuildBlobPath(
            tenantId: 7,
            employeeId: 12,
            documentType: HRDocumentType.Invoice,
            year: 2026,
            month: month,
            documentId: 1,
            versionNumber: 1,
            fileExtension: "pdf");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0, 1, 1, 1)]
    [InlineData(1, 0, 1, 1)]
    [InlineData(1, 1, 0, 1)]
    [InlineData(1, 1, 1, 0)]
    public void BuildBlobPath_Throws_WhenIdentifiersAreNotPositive(
        int tenantId,
        int employeeId,
        int documentId,
        int versionNumber)
    {
        var act = () => _service.BuildBlobPath(
            tenantId: tenantId,
            employeeId: employeeId,
            documentType: HRDocumentType.Invoice,
            year: 2026,
            month: 1,
            documentId: documentId,
            versionNumber: versionNumber,
            fileExtension: "pdf");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("../pdf")]
    [InlineData("p/df")]
    [InlineData("p\\df")]
    public void BuildBlobPath_Throws_WhenExtensionIsInvalid(string extension)
    {
        var act = () => _service.BuildBlobPath(
            tenantId: 7,
            employeeId: 12,
            documentType: HRDocumentType.Invoice,
            year: 2026,
            month: 1,
            documentId: 1,
            versionNumber: 1,
            fileExtension: extension);

        act.Should().Throw<ArgumentException>();
    }

    private static AzureBlobStorageService CreateService()
    {
        return new AzureBlobStorageService(new AzureBlobStorageOptions
        {
            ConnectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;",
            ContainerName = "erp-documents",
            SasTokenExpirationMinutes = 5
        });
    }
}
