using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests;

public class HRDocumentServiceTests
{
    private readonly Mock<IFileStorageService> _fileStorage = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<IUtcClock> _clock = new();

    [Fact]
    public async Task GetDocumentByIdAsync_ReturnsNull_WhenDocumentMissing()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.HRDocuments, x => [x.Id])
            .Build();

        var service = new HRDocumentService(context.Object, _fileStorage.Object, _notifications.Object, _clock.Object);

        var result = await service.GetDocumentByIdAsync(1, 7);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEmployeeDocumentByIdAsync_ReturnsNull_WhenDocumentMissing()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.HRDocuments, x => [x.Id])
            .Build();

        var service = new HRDocumentService(context.Object, _fileStorage.Object, _notifications.Object, _clock.Object);

        var result = await service.GetEmployeeDocumentByIdAsync(1, 7, 11);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FinalizeDocumentUploadAsync_ReturnsFalse_WhenPayloadIsInvalid()
    {
        var context = new ApplicationDbContextMockBuilder().Build();
        var service = new HRDocumentService(context.Object, _fileStorage.Object, _notifications.Object, _clock.Object);

        var result = await service.FinalizeDocumentUploadAsync(1, 7, new HRDocumentFinalizeDto
        {
            FileName = "",
            FileSizeBytes = 0,
            ContentType = ""
        });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateDocumentAsync_ReturnsFalse_WhenDocumentMissing()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.HRDocuments, x => [x.Id])
            .Build();

        var service = new HRDocumentService(context.Object, _fileStorage.Object, _notifications.Object, _clock.Object);

        var updated = await service.UpdateDocumentAsync(1, 7, new HRDocumentUpdateDto { Title = "Nuovo" });

        updated.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteDocumentAsync_ReturnsFalse_WhenDocumentMissing()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.HRDocuments, x => [x.Id])
            .Build();

        var service = new HRDocumentService(context.Object, _fileStorage.Object, _notifications.Object, _clock.Object);

        var deleted = await service.DeleteDocumentAsync(1, 7);

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task AcknowledgeVersionAsync_ThrowsUnauthorized_WhenDocumentMissing()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.HRDocuments, x => [x.Id])
            .Build();

        var service = new HRDocumentService(context.Object, _fileStorage.Object, _notifications.Object, _clock.Object);

        var act = () => service.AcknowledgeVersionAsync(1, 7, 11, 2);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Document not found or access denied");
    }

    [Fact]
    public async Task GetDocumentAccessLogAsync_ReturnsEmpty_WhenDocumentMissing()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.HRDocuments, x => [x.Id])
            .Build();

        var service = new HRDocumentService(context.Object, _fileStorage.Object, _notifications.Object, _clock.Object);

        var rows = await service.GetDocumentAccessLogAsync(1, 7);

        rows.Should().BeEmpty();
    }
}
