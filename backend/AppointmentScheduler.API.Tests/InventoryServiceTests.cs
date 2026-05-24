using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class InventoryServiceTests
{
    private readonly Mock<IUtcClock> _clock = new();

    [Fact]
    public async Task CreateItemAsync_Throws_WhenSkuAlreadyExists()
    {
        var items = new List<InventoryItem>
        {
            new() { Id = 1, MerchantId = 7, Sku = "SKU-1", Name = "Existing", IsActive = true }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.InventoryItems, items, x => [x.Id])
            .Build();

        var service = new InventoryService(context.Object, _clock.Object);

        var act = () => service.CreateItemAsync(7, new CreateInventoryItemRequest { Sku = "SKU-1", Name = "New" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Esiste già un articolo con SKU 'SKU-1'.");
    }

    [Fact]
    public async Task CreateItemAsync_TrimsValuesAndSaves()
    {
        var now = new DateTime(2026, 5, 24, 12, 30, 0, DateTimeKind.Utc);
        _clock.SetupGet(x => x.UtcNow).Returns(now);

        var items = new List<InventoryItem>();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.InventoryItems, items, x => [x.Id])
            .Build(out var tracker);

        var service = new InventoryService(context.Object, _clock.Object);

        var created = await service.CreateItemAsync(7, new CreateInventoryItemRequest
        {
            Sku = "  SKU-2  ",
            Name = "  Acqua  ",
            UnitOfMeasure = "  lt ",
            ReorderPoint = 3
        });

        created.Sku.Should().Be("SKU-2");
        created.Name.Should().Be("Acqua");
        created.UnitOfMeasure.Should().Be("lt");
        created.CreatedAt.Should().Be(now);
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task UpdateItemAsync_ReturnsNull_WhenItemDoesNotExist()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.InventoryItems, x => [x.Id])
            .Build();

        var service = new InventoryService(context.Object, _clock.Object);

        var result = await service.UpdateItemAsync(99, 7, new UpdateInventoryItemRequest { Sku = "A", Name = "B" });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateItemAsync_Throws_WhenSkuConflicts()
    {
        var items = new List<InventoryItem>
        {
            new() { Id = 1, MerchantId = 7, Sku = "SKU-1", Name = "Item1", IsActive = true },
            new() { Id = 2, MerchantId = 7, Sku = "SKU-2", Name = "Item2", IsActive = true }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.InventoryItems, items, x => [x.Id])
            .Build();

        var service = new InventoryService(context.Object, _clock.Object);

        var act = () => service.UpdateItemAsync(2, 7, new UpdateInventoryItemRequest
        {
            Sku = "SKU-1",
            Name = "Item2",
            IsActive = true
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Esiste già un articolo con SKU 'SKU-1'.");
    }
}
