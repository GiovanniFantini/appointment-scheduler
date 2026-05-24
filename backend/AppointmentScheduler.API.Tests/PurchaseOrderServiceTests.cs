using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class PurchaseOrderServiceTests
{
    private readonly Mock<IUtcClock> _clock = new();

    [Fact]
    public async Task CreateOrderAsync_Throws_WhenNoLinesProvided()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.MerchantBranches, x => [x.Id])
            .Build();

        var service = new PurchaseOrderService(context.Object, _clock.Object);

        var act = () => service.CreateOrderAsync(7, 101, new CreatePurchaseOrderRequest
        {
            BranchId = 3,
            SupplierId = 1,
            Lines = new List<CreatePurchaseOrderLineRequest>()
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("L'ordine acquisto deve contenere almeno una riga.");
    }

    [Fact]
    public async Task MarkAsSentAsync_ReturnsNull_WhenOrderNotFound()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.PurchaseOrders, x => [x.Id])
            .Build();

        var service = new PurchaseOrderService(context.Object, _clock.Object);

        var result = await service.MarkAsSentAsync(55, 7);

        result.Should().BeNull();
    }

    [Fact]
    public async Task MarkAsSentAsync_Throws_WhenOrderIsNotDraft()
    {
        var orders = new List<PurchaseOrder>
        {
            new() { Id = 55, MerchantId = 7, Status = PurchaseOrderStatus.Sent }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.PurchaseOrders, orders, x => [x.Id])
            .Build();

        var service = new PurchaseOrderService(context.Object, _clock.Object);

        var act = () => service.MarkAsSentAsync(55, 7);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Solo un ordine Draft può essere inviato.");
    }

    [Fact]
    public async Task CancelOrderAsync_Throws_WhenOrderIsClosed()
    {
        var orders = new List<PurchaseOrder>
        {
            new() { Id = 55, MerchantId = 7, Status = PurchaseOrderStatus.Closed, Lines = new List<PurchaseOrderLine>() }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.PurchaseOrders, orders, x => [x.Id])
            .Build();

        var service = new PurchaseOrderService(context.Object, _clock.Object);

        var act = () => service.CancelOrderAsync(55, 7);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Un ordine chiuso non può essere annullato.");
    }

    [Fact]
    public async Task CancelOrderAsync_Throws_WhenOrderHasReceivedLines()
    {
        var orders = new List<PurchaseOrder>
        {
            new()
            {
                Id = 55,
                MerchantId = 7,
                Status = PurchaseOrderStatus.Sent,
                Lines = new List<PurchaseOrderLine>
                {
                    new() { Id = 1, QuantityOrdered = 10, QuantityReceived = 1 }
                }
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.PurchaseOrders, orders, x => [x.Id])
            .Build();

        var service = new PurchaseOrderService(context.Object, _clock.Object);

        var act = () => service.CancelOrderAsync(55, 7);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Un ordine già ricevuto anche solo parzialmente non può essere annullato.");
    }
}
