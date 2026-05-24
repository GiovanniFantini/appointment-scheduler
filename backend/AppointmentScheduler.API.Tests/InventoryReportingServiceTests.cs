using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class InventoryReportingServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_ComputesMainCounters()
    {
        var items = new List<InventoryItem>
        {
            new() { Id = 1, MerchantId = 7, Name = "A", Sku = "A", IsActive = true, ReorderPoint = 5 },
            new() { Id = 2, MerchantId = 7, Name = "B", Sku = "B", IsActive = true, ReorderPoint = 0 },
            new() { Id = 3, MerchantId = 7, Name = "C", Sku = "C", IsActive = false, ReorderPoint = 1 }
        };

        var suppliers = new List<Supplier>
        {
            new() { Id = 10, MerchantId = 7, Name = "S1", IsActive = true },
            new() { Id = 11, MerchantId = 7, Name = "S2", IsActive = false }
        };

        var balances = new List<InventoryStockBalance>
        {
            new() { Id = 1, MerchantId = 7, BranchId = 3, ItemId = 1, QuantityOnHand = 3, WeightedAverageCost = 2, InventoryValue = 6 },
            new() { Id = 2, MerchantId = 7, BranchId = 3, ItemId = 2, QuantityOnHand = 10, WeightedAverageCost = 1, InventoryValue = 10 }
        };

        var orders = new List<PurchaseOrder>
        {
            new() { Id = 1, MerchantId = 7, BranchId = 3, SupplierId = 10, Status = PurchaseOrderStatus.Draft },
            new() { Id = 2, MerchantId = 7, BranchId = 3, SupplierId = 10, Status = PurchaseOrderStatus.Sent },
            new() { Id = 3, MerchantId = 7, BranchId = 3, SupplierId = 10, Status = PurchaseOrderStatus.Closed }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.InventoryItems, items, x => [x.Id])
            .WithSet(x => x.Suppliers, suppliers, x => [x.Id])
            .WithSet(x => x.InventoryStockBalances, balances, x => [x.Id])
            .WithSet(x => x.PurchaseOrders, orders, x => [x.Id])
            .Build();

        var service = new InventoryReportingService(context.Object);

        var dashboard = await service.GetDashboardAsync(7, 3);

        dashboard.TotalItems.Should().Be(2);
        dashboard.ActiveSuppliers.Should().Be(1);
        dashboard.OpenPurchaseOrders.Should().Be(2);
        dashboard.TotalStockValue.Should().Be(16);
        dashboard.LowStockItems.Should().Be(1);
    }

    [Fact]
    public async Task GetLowStockAsync_ReturnsSuggestedReorderQuantity()
    {
        var branch = new MerchantBranch { Id = 3, MerchantId = 7, Name = "HQ", IsActive = true };
        var item = new InventoryItem { Id = 1, MerchantId = 7, Name = "Pasta", Sku = "PA", ReorderPoint = 10 };

        var balances = new List<InventoryStockBalance>
        {
            new() { Id = 1, MerchantId = 7, BranchId = 3, Branch = branch, ItemId = 1, Item = item, QuantityOnHand = 6, WeightedAverageCost = 2, InventoryValue = 12 }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.InventoryStockBalances, balances, x => [x.Id])
            .Build();

        var service = new InventoryReportingService(context.Object);

        var result = await service.GetLowStockAsync(7, 3);

        result.Should().ContainSingle();
        result[0].SuggestedReorderQuantity.Should().Be(4);
        result[0].ItemName.Should().Be("Pasta");
    }
}
