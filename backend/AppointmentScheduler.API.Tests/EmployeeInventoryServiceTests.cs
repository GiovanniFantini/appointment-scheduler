using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class EmployeeInventoryServiceTests
{
    private readonly Mock<IInventoryService> _inventoryService = new();
    private readonly Mock<ISupplierService> _supplierService = new();
    private readonly Mock<IPurchaseOrderService> _purchaseOrderService = new();

    [Fact]
    public async Task GetSuppliersAsync_Throws_WhenMembershipIsMissing()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.EmployeeMemberships, x => [x.Id])
            .Build();

        var service = new EmployeeInventoryService(context.Object, _inventoryService.Object, _supplierService.Object, _purchaseOrderService.Object);

        var act = () => service.GetSuppliersAsync(11, 7);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Membership dipendente non trovato.");
        _supplierService.Verify(x => x.GetSuppliersAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task GetSuppliersAsync_DelegatesToSupplierService_WhenMembershipExists()
    {
        var memberships = new List<EmployeeMembership>
        {
            new() { Id = 1, EmployeeId = 11, MerchantId = 7, IsActive = true, HomeBranchId = 3, BranchAccess = new List<EmployeeBranchAccess>() }
        };

        var expected = new List<SupplierDto> { new() { Id = 1, Name = "Acme" } };
        _supplierService.Setup(x => x.GetSuppliersAsync(7, true)).ReturnsAsync(expected);

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EmployeeMemberships, memberships, x => [x.Id])
            .Build();

        var service = new EmployeeInventoryService(context.Object, _inventoryService.Object, _supplierService.Object, _purchaseOrderService.Object);

        var result = await service.GetSuppliersAsync(11, 7);

        result.Should().BeSameAs(expected);
        _supplierService.Verify(x => x.GetSuppliersAsync(7, true), Times.Once);
    }

    [Fact]
    public async Task CreateAdjustmentAsync_Throws_WhenBranchIsNotAccessible()
    {
        var memberships = new List<EmployeeMembership>
        {
            new()
            {
                Id = 1,
                EmployeeId = 11,
                MerchantId = 7,
                IsActive = true,
                HomeBranchId = 3,
                BranchAccess = new List<EmployeeBranchAccess>
                {
                    new() { Id = 10, MembershipId = 1, BranchId = 4 }
                }
            }
        };

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EmployeeMemberships, memberships, x => [x.Id])
            .Build();

        var service = new EmployeeInventoryService(context.Object, _inventoryService.Object, _supplierService.Object, _purchaseOrderService.Object);

        var act = () => service.CreateAdjustmentAsync(11, 7, 101, new CreateInventoryAdjustmentRequest
        {
            BranchId = 9,
            ItemId = 1,
            QuantityDelta = 5,
            Reason = "count fix"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Filiale non accessibile per il dipendente.");
        _inventoryService.Verify(x => x.CreateAdjustmentAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CreateInventoryAdjustmentRequest>()), Times.Never);
    }

    [Fact]
    public async Task ReceiveOrderAsync_ReturnsNull_WhenOrderDoesNotExist()
    {
        var memberships = new List<EmployeeMembership>
        {
            new() { Id = 1, EmployeeId = 11, MerchantId = 7, IsActive = true, HomeBranchId = 3, BranchAccess = new List<EmployeeBranchAccess>() }
        };

        _purchaseOrderService.Setup(x => x.GetOrderByIdAsync(55, 7)).ReturnsAsync((PurchaseOrderDto?)null);

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EmployeeMemberships, memberships, x => [x.Id])
            .Build();

        var service = new EmployeeInventoryService(context.Object, _inventoryService.Object, _supplierService.Object, _purchaseOrderService.Object);

        var result = await service.ReceiveOrderAsync(11, 7, 101, 55, new CreateGoodsReceiptRequest());

        result.Should().BeNull();
        _purchaseOrderService.Verify(x => x.ReceiveOrderAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CreateGoodsReceiptRequest>()), Times.Never);
    }

    [Fact]
    public async Task MarkPurchaseOrderSentAsync_Throws_WhenOrderBranchIsNotAccessible()
    {
        var memberships = new List<EmployeeMembership>
        {
            new() { Id = 1, EmployeeId = 11, MerchantId = 7, IsActive = true, HomeBranchId = 3, BranchAccess = new List<EmployeeBranchAccess>() }
        };

        _purchaseOrderService.Setup(x => x.GetOrderByIdAsync(55, 7)).ReturnsAsync(new PurchaseOrderDto
        {
            Id = 55,
            MerchantId = 7,
            BranchId = 9,
            BranchName = "Remote",
            SupplierId = 1,
            SupplierName = "Acme",
            OrderNumber = "PO-1",
            Status = PurchaseOrderStatus.Draft,
            OrderedAt = new DateTime(2026, 5, 24, 8, 0, 0, DateTimeKind.Utc)
        });

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.EmployeeMemberships, memberships, x => [x.Id])
            .Build();

        var service = new EmployeeInventoryService(context.Object, _inventoryService.Object, _supplierService.Object, _purchaseOrderService.Object);

        var act = () => service.MarkPurchaseOrderSentAsync(11, 7, 55);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Filiale non accessibile per il dipendente.");
        _purchaseOrderService.Verify(x => x.MarkAsSentAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }
}
