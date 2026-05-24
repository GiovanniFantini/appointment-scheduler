using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class SupplierServiceTests
{
    private readonly Mock<IUtcClock> _clock = new();

    [Fact]
    public async Task GetSuppliersAsync_FiltersInactive_WhenRequested()
    {
        var suppliers = new List<Supplier>
        {
            new() { Id = 1, MerchantId = 7, Name = "A", IsActive = true },
            new() { Id = 2, MerchantId = 7, Name = "B", IsActive = false },
            new() { Id = 3, MerchantId = 9, Name = "C", IsActive = true },
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Suppliers, suppliers, x => [x.Id])
            .Build();

        var service = new SupplierService(context.Object, _clock.Object);

        var result = await service.GetSuppliersAsync(7, includeInactive: false);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("A");
    }

    [Fact]
    public async Task CreateSupplierAsync_Throws_WhenNameAlreadyExists()
    {
        var suppliers = new List<Supplier>
        {
            new() { Id = 1, MerchantId = 7, Name = "Acme", IsActive = true }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Suppliers, suppliers, x => [x.Id])
            .Build();

        var service = new SupplierService(context.Object, _clock.Object);

        var act = () => service.CreateSupplierAsync(7, new CreateSupplierRequest { Name = "Acme" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Esiste già un fornitore con nome 'Acme'.");
    }

    [Fact]
    public async Task CreateSupplierAsync_NormalizesOptionalFields_AndSaves()
    {
        var now = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);
        _clock.SetupGet(x => x.UtcNow).Returns(now);

        var suppliers = new List<Supplier>();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Suppliers, suppliers, x => [x.Id])
            .Build(out var tracker);

        var service = new SupplierService(context.Object, _clock.Object);

        var created = await service.CreateSupplierAsync(7, new CreateSupplierRequest
        {
            Name = "  Acme  ",
            ContactName = "   ",
            Email = " test@acme.it ",
            Phone = " ",
            Notes = "  note  "
        });

        created.Name.Should().Be("Acme");
        created.ContactName.Should().BeNull();
        created.Email.Should().Be("test@acme.it");
        created.Phone.Should().BeNull();
        created.Notes.Should().Be("note");
        created.CreatedAt.Should().Be(now);
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task UpdateSupplierAsync_ReturnsNull_WhenSupplierIsMissing()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.Suppliers, x => [x.Id])
            .Build();

        var service = new SupplierService(context.Object, _clock.Object);

        var result = await service.UpdateSupplierAsync(1, 7, new UpdateSupplierRequest { Name = "Acme" });

        result.Should().BeNull();
    }
}
