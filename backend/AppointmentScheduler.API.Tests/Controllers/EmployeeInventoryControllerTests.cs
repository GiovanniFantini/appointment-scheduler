using AppointmentScheduler.API.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.API.Tests.Controllers;

public class EmployeeInventoryControllerTests
{
    private readonly Mock<IEmployeeInventoryService> _inventoryService = new();
    private readonly Mock<ILogger<EmployeeInventoryController>> _logger = new();

    [Fact]
    public async Task GetOverview_ReturnsBadRequest_WhenIdentityIsMissing()
    {
        var controller = CreateController();

        var result = await controller.GetOverview();

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Token non valido");
    }

    [Fact]
    public async Task GetOverview_ReturnsForbid_WhenFeatureIsMissing()
    {
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"));

        var result = await controller.GetOverview();

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetOverview_ReturnsOk_WhenRequestIsValid()
    {
        var overview = new EmployeeInventoryOverviewDto();
        _inventoryService.Setup(service => service.GetOverviewAsync(11, 7, 2)).ReturnsAsync(overview);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.ReadOnly));

        var result = await controller.GetOverview(2);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(overview);
    }

    [Fact]
    public async Task GetItems_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        _inventoryService.Setup(service => service.GetItemsAsync(11, 7, 2, "sku")).ThrowsAsync(new InvalidOperationException("Filiale non accessibile"));
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.ReadOnly));

        var result = await controller.GetItems(2, "sku");

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Filiale non accessibile");
    }

    [Fact]
    public async Task GetItems_ReturnsOk_WhenRequestIsValid()
    {
        var items = new List<InventoryItemDto> { new() { Id = 1 } };
        _inventoryService.Setup(service => service.GetItemsAsync(11, 7, 2, "sku")).ReturnsAsync(items);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.ReadOnly));

        var result = await controller.GetItems(2, "sku");

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(items);
    }

    [Fact]
    public async Task GetMovements_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        var from = new DateOnly(2026, 2, 1);
        var to = new DateOnly(2026, 2, 2);
        _inventoryService.Setup(service => service.GetMovementsAsync(11, 7, 2, 3, from, to)).ThrowsAsync(new InvalidOperationException("Filiale non accessibile"));
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.ReadOnly));

        var result = await controller.GetMovements(2, 3, from, to);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Filiale non accessibile");
    }

    [Fact]
    public async Task GetMovements_ReturnsOk_WhenRequestIsValid()
    {
        var from = new DateOnly(2026, 2, 1);
        var to = new DateOnly(2026, 2, 2);
        var movements = new List<InventoryMovementDto> { new() { Id = 1 } };
        _inventoryService.Setup(service => service.GetMovementsAsync(11, 7, 2, 3, from, to)).ReturnsAsync(movements);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.ReadOnly));

        var result = await controller.GetMovements(2, 3, from, to);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(movements);
    }

    [Fact]
    public async Task GetLowStock_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        _inventoryService.Setup(service => service.GetLowStockAsync(11, 7, 2)).ThrowsAsync(new InvalidOperationException("Filiale non accessibile"));
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.ReadOnly));

        var result = await controller.GetLowStock(2);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Filiale non accessibile");
    }

    [Fact]
    public async Task GetLowStock_ReturnsOk_WhenRequestIsValid()
    {
        var rows = new List<LowStockReportRowDto> { new() { ItemId = 1 } };
        _inventoryService.Setup(service => service.GetLowStockAsync(11, 7, 2)).ReturnsAsync(rows);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.ReadOnly));

        var result = await controller.GetLowStock(2);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(rows);
    }

    [Fact]
    public async Task GetSuppliers_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        _inventoryService.Setup(service => service.GetSuppliersAsync(11, 7, false)).ThrowsAsync(new InvalidOperationException("Membership non trovata"));
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.ReadOnly));

        var result = await controller.GetSuppliers(false);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Membership non trovata");
    }

    [Fact]
    public async Task GetSuppliers_ReturnsOk_WhenRequestIsValid()
    {
        var suppliers = new List<SupplierDto> { new() { Id = 1, Name = "Vendor" } };
        _inventoryService.Setup(service => service.GetSuppliersAsync(11, 7, false)).ReturnsAsync(suppliers);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.ReadOnly));

        var result = await controller.GetSuppliers(false);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(suppliers);
    }

    [Fact]
    public async Task GetPurchaseOrders_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        _inventoryService.Setup(service => service.GetPurchaseOrdersAsync(11, 7, 2, PurchaseOrderStatus.Draft)).ThrowsAsync(new InvalidOperationException("Filiale non accessibile"));
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.ReadOnly));

        var result = await controller.GetPurchaseOrders(2, PurchaseOrderStatus.Draft);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Filiale non accessibile");
    }

    [Fact]
    public async Task GetPurchaseOrders_ReturnsOk_WhenRequestIsValid()
    {
        var orders = new List<PurchaseOrderDto> { new() { Id = 1, BranchId = 2 } };
        _inventoryService.Setup(service => service.GetPurchaseOrdersAsync(11, 7, 2, PurchaseOrderStatus.Draft)).ReturnsAsync(orders);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.ReadOnly));

        var result = await controller.GetPurchaseOrders(2, PurchaseOrderStatus.Draft);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(orders);
    }

    [Fact]
    public async Task GetPurchaseOrder_ReturnsNotFound_WhenOrderDoesNotExist()
    {
        _inventoryService.Setup(service => service.GetPurchaseOrderByIdAsync(11, 7, 5)).ReturnsAsync((PurchaseOrderDto?)null);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.ReadOnly));

        var result = await controller.GetPurchaseOrder(5);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Ordine acquisto non trovato");
    }

    [Fact]
    public async Task GetPurchaseOrder_ReturnsOk_WhenOrderExists()
    {
        var order = new PurchaseOrderDto { Id = 5, BranchId = 2 };
        _inventoryService.Setup(service => service.GetPurchaseOrderByIdAsync(11, 7, 5)).ReturnsAsync(order);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.ReadOnly));

        var result = await controller.GetPurchaseOrder(5);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(order);
    }

    [Fact]
    public async Task CreateAdjustment_ReturnsBadRequest_WhenUserClaimIsMissing()
    {
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"), new Claim("Feature", "Magazzino"), new Claim("FeatureLevel", "Magazzino:Operator"));

        var result = await controller.CreateAdjustment(new CreateInventoryAdjustmentRequest());

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Token non valido");
    }

    [Fact]
    public async Task CreateAdjustment_ReturnsConflict_WhenConcurrencyExceptionOccurs()
    {
        var request = new CreateInventoryAdjustmentRequest { BranchId = 2, ItemId = 3 };
        _inventoryService.Setup(service => service.CreateAdjustmentAsync(11, 7, 12, request)).ThrowsAsync(new DbUpdateConcurrencyException());
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Operator, includeUser: true));

        var result = await controller.CreateAdjustment(request);

        var conflict = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.GetAnonymousString("message").Should().Be("Il saldo è stato modificato da un'altra operazione. Riprova.");
    }

    [Fact]
    public async Task CreateAdjustment_ReturnsServerError_WhenDbUpdateExceptionOccurs()
    {
        var request = new CreateInventoryAdjustmentRequest { BranchId = 2, ItemId = 3 };
        _inventoryService.Setup(service => service.CreateAdjustmentAsync(11, 7, 12, request)).ThrowsAsync(new DbUpdateException("boom", new Exception()));
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Operator, includeUser: true));

        var result = await controller.CreateAdjustment(request);

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        objectResult.GetAnonymousString("message").Should().Be("Errore durante il salvataggio della rettifica stock.");
    }

    [Fact]
    public async Task CreateAdjustment_ReturnsServerError_WhenUnexpectedExceptionOccurs()
    {
        var request = new CreateInventoryAdjustmentRequest { BranchId = 2, ItemId = 3 };
        _inventoryService.Setup(service => service.CreateAdjustmentAsync(11, 7, 12, request)).ThrowsAsync(new Exception("boom"));
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Operator, includeUser: true));

        var result = await controller.CreateAdjustment(request);

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        objectResult.GetAnonymousString("message").Should().Be("Errore inatteso durante la rettifica stock.");
    }

    [Fact]
    public async Task CreateAdjustment_ReturnsOk_WhenServiceSucceeds()
    {
        var request = new CreateInventoryAdjustmentRequest { BranchId = 2, ItemId = 3 };
        var movement = new InventoryMovementDto { Id = 6 };
        _inventoryService.Setup(service => service.CreateAdjustmentAsync(11, 7, 12, request)).ReturnsAsync(movement);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Operator, includeUser: true));

        var result = await controller.CreateAdjustment(request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(movement);
    }

    [Fact]
    public async Task ReceivePurchaseOrder_ReturnsNotFound_WhenOrderDoesNotExist()
    {
        var request = new CreateGoodsReceiptRequest();
        _inventoryService.Setup(service => service.ReceiveOrderAsync(11, 7, 12, 5, request)).ReturnsAsync((PurchaseOrderDto?)null);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Operator, includeUser: true));

        var result = await controller.ReceivePurchaseOrder(5, request);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Ordine acquisto non trovato");
    }

    [Fact]
    public async Task ReceivePurchaseOrder_ReturnsConflict_WhenConcurrencyExceptionOccurs()
    {
        var request = new CreateGoodsReceiptRequest();
        _inventoryService.Setup(service => service.ReceiveOrderAsync(11, 7, 12, 5, request)).ThrowsAsync(new DbUpdateConcurrencyException());
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Operator, includeUser: true));

        var result = await controller.ReceivePurchaseOrder(5, request);

        var conflict = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.GetAnonymousString("message").Should().Be("Lo stock è stato modificato da un'altra operazione. Riprova.");
    }

    [Fact]
    public async Task ReceivePurchaseOrder_ReturnsServerError_WhenDbUpdateExceptionOccurs()
    {
        var request = new CreateGoodsReceiptRequest();
        _inventoryService.Setup(service => service.ReceiveOrderAsync(11, 7, 12, 5, request)).ThrowsAsync(new DbUpdateException("boom", new Exception()));
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Operator, includeUser: true));

        var result = await controller.ReceivePurchaseOrder(5, request);

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        objectResult.GetAnonymousString("message").Should().Be("Errore durante il salvataggio del ricevimento merci.");
    }

    [Fact]
    public async Task ReceivePurchaseOrder_ReturnsOk_WhenServiceSucceeds()
    {
        var request = new CreateGoodsReceiptRequest();
        var order = new PurchaseOrderDto { Id = 5, BranchId = 2 };
        _inventoryService.Setup(service => service.ReceiveOrderAsync(11, 7, 12, 5, request)).ReturnsAsync(order);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Operator, includeUser: true));

        var result = await controller.ReceivePurchaseOrder(5, request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(order);
    }

    [Fact]
    public async Task CreateItem_ReturnsForbid_WhenManagerLevelIsMissing()
    {
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Operator));

        var result = await controller.CreateItem(new CreateInventoryItemRequest());

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CreateItem_ReturnsCreatedAtAction_WhenServiceSucceeds()
    {
        var request = new CreateInventoryItemRequest();
        var item = new InventoryItemDto { Id = 7 };
        _inventoryService.Setup(service => service.CreateItemAsync(11, 7, request)).ReturnsAsync(item);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Manager));

        var result = await controller.CreateItem(request);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(EmployeeInventoryController.GetItems));
        created.Value.Should().BeSameAs(item);
    }

    [Fact]
    public async Task UpdateItem_ReturnsNotFound_WhenItemDoesNotExist()
    {
        var request = new UpdateInventoryItemRequest();
        _inventoryService.Setup(service => service.UpdateItemAsync(11, 7, 5, request)).ReturnsAsync((InventoryItemDto?)null);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Manager));

        var result = await controller.UpdateItem(5, request);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Articolo non trovato");
    }

    [Fact]
    public async Task UpdateItem_ReturnsOk_WhenServiceSucceeds()
    {
        var request = new UpdateInventoryItemRequest();
        var item = new InventoryItemDto { Id = 5 };
        _inventoryService.Setup(service => service.UpdateItemAsync(11, 7, 5, request)).ReturnsAsync(item);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Manager));

        var result = await controller.UpdateItem(5, request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(item);
    }

    [Fact]
    public async Task CreateSupplier_ReturnsCreatedAtAction_WhenServiceSucceeds()
    {
        var request = new CreateSupplierRequest();
        var supplier = new SupplierDto { Id = 8, Name = "Vendor" };
        _inventoryService.Setup(service => service.CreateSupplierAsync(11, 7, request)).ReturnsAsync(supplier);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Manager));

        var result = await controller.CreateSupplier(request);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(EmployeeInventoryController.GetSuppliers));
        created.Value.Should().BeSameAs(supplier);
    }

    [Fact]
    public async Task UpdateSupplier_ReturnsNotFound_WhenSupplierDoesNotExist()
    {
        var request = new UpdateSupplierRequest();
        _inventoryService.Setup(service => service.UpdateSupplierAsync(11, 7, 5, request)).ReturnsAsync((SupplierDto?)null);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Manager));

        var result = await controller.UpdateSupplier(5, request);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Fornitore non trovato");
    }

    [Fact]
    public async Task UpdateSupplier_ReturnsOk_WhenServiceSucceeds()
    {
        var request = new UpdateSupplierRequest();
        var supplier = new SupplierDto { Id = 5, Name = "Vendor" };
        _inventoryService.Setup(service => service.UpdateSupplierAsync(11, 7, 5, request)).ReturnsAsync(supplier);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Manager));

        var result = await controller.UpdateSupplier(5, request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(supplier);
    }

    [Fact]
    public async Task CreatePurchaseOrder_ReturnsServerError_WhenDbUpdateExceptionOccurs()
    {
        var request = new CreatePurchaseOrderRequest { BranchId = 2, SupplierId = 3 };
        _inventoryService.Setup(service => service.CreatePurchaseOrderAsync(11, 7, 12, request)).ThrowsAsync(new DbUpdateException("boom", new Exception()));
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Manager, includeUser: true));

        var result = await controller.CreatePurchaseOrder(request);

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        objectResult.GetAnonymousString("message").Should().Be("Errore durante il salvataggio dell'ordine acquisto.");
    }

    [Fact]
    public async Task CreatePurchaseOrder_ReturnsCreatedAtAction_WhenServiceSucceeds()
    {
        var request = new CreatePurchaseOrderRequest { BranchId = 2, SupplierId = 3 };
        var order = new PurchaseOrderDto { Id = 5, BranchId = 2 };
        _inventoryService.Setup(service => service.CreatePurchaseOrderAsync(11, 7, 12, request)).ReturnsAsync(order);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Manager, includeUser: true));

        var result = await controller.CreatePurchaseOrder(request);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(EmployeeInventoryController.GetPurchaseOrder));
        created.RouteValues!["id"].Should().Be(5);
        created.Value.Should().BeSameAs(order);
    }

    [Fact]
    public async Task SendPurchaseOrder_ReturnsNotFound_WhenOrderDoesNotExist()
    {
        _inventoryService.Setup(service => service.MarkPurchaseOrderSentAsync(11, 7, 5)).ReturnsAsync((PurchaseOrderDto?)null);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Manager));

        var result = await controller.SendPurchaseOrder(5);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Ordine acquisto non trovato");
    }

    [Fact]
    public async Task SendPurchaseOrder_ReturnsServerError_WhenDbUpdateExceptionOccurs()
    {
        _inventoryService.Setup(service => service.MarkPurchaseOrderSentAsync(11, 7, 5)).ThrowsAsync(new DbUpdateException("boom", new Exception()));
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Manager));

        var result = await controller.SendPurchaseOrder(5);

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        objectResult.GetAnonymousString("message").Should().Be("Errore durante l'invio dell'ordine acquisto.");
    }

    [Fact]
    public async Task SendPurchaseOrder_ReturnsOk_WhenServiceSucceeds()
    {
        var order = new PurchaseOrderDto { Id = 5, BranchId = 2 };
        _inventoryService.Setup(service => service.MarkPurchaseOrderSentAsync(11, 7, 5)).ReturnsAsync(order);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Manager));

        var result = await controller.SendPurchaseOrder(5);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(order);
    }

    [Fact]
    public async Task CancelPurchaseOrder_ReturnsNotFound_WhenOrderDoesNotExist()
    {
        _inventoryService.Setup(service => service.CancelPurchaseOrderAsync(11, 7, 5)).ReturnsAsync((PurchaseOrderDto?)null);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Manager));

        var result = await controller.CancelPurchaseOrder(5);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Ordine acquisto non trovato");
    }

    [Fact]
    public async Task CancelPurchaseOrder_ReturnsServerError_WhenDbUpdateExceptionOccurs()
    {
        _inventoryService.Setup(service => service.CancelPurchaseOrderAsync(11, 7, 5)).ThrowsAsync(new DbUpdateException("boom", new Exception()));
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Manager));

        var result = await controller.CancelPurchaseOrder(5);

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        objectResult.GetAnonymousString("message").Should().Be("Errore durante l'annullamento dell'ordine acquisto.");
    }

    [Fact]
    public async Task CancelPurchaseOrder_ReturnsOk_WhenServiceSucceeds()
    {
        var order = new PurchaseOrderDto { Id = 5, BranchId = 2 };
        _inventoryService.Setup(service => service.CancelPurchaseOrderAsync(11, 7, 5)).ReturnsAsync(order);
        var controller = CreateController(InventoryClaims(FeatureAccessLevel.Manager));

        var result = await controller.CancelPurchaseOrder(5);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(order);
    }

    private EmployeeInventoryController CreateController(params Claim[] claims)
    {
        return new EmployeeInventoryController(_inventoryService.Object, _logger.Object).WithUser(claims);
    }

    private static Claim[] InventoryClaims(FeatureAccessLevel level, bool includeUser = false)
    {
        var claims = new List<Claim>
        {
            new("EmployeeId", "11"),
            new("MerchantId", "7"),
            new("Feature", "Magazzino"),
            new("FeatureLevel", $"Magazzino:{level}")
        };

        if (includeUser)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, "12"));

        return claims.ToArray();
    }
}