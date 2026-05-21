using System.Security.Claims;
using AppointmentScheduler.API.Authorization;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Magazzino lato Employee: tutta l'operatività vive qui.
/// L'accesso è modulato sul livello della feature Magazzino del ruolo del dipendente:
///  - ReadOnly → consultazione (overview, items, movements, low-stock, letture)
///  - Operator → rettifiche stock e ricevimento merci
///  - Manager  → anche anagrafiche articoli/fornitori e gestione ordini di acquisto
/// </summary>
[ApiController]
[Route("api/employee/inventory")]
[Authorize(Policy = "EmployeeOnly")]
public class EmployeeInventoryController : ControllerBase
{
    private readonly IEmployeeInventoryService _employeeInventoryService;
    private readonly ILogger<EmployeeInventoryController> _logger;

    public EmployeeInventoryController(
        IEmployeeInventoryService employeeInventoryService,
        ILogger<EmployeeInventoryController> logger)
    {
        _employeeInventoryService = employeeInventoryService;
        _logger = logger;
    }

    private bool TryGetEmployeeId(out int employeeId)
    {
        employeeId = 0;
        var claim = User.FindFirst("EmployeeId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out employeeId);
    }

    private bool TryGetMerchantId(out int merchantId)
    {
        merchantId = 0;
        var claim = User.FindFirst("MerchantId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out merchantId);
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out userId);
    }

    /// <summary>
    /// Estrae employeeId e merchantId dal token. Se mancanti restituisce BadRequest.
    /// </summary>
    private ActionResult? ResolveIdentity(out int employeeId, out int merchantId)
    {
        var hasEmployee = TryGetEmployeeId(out employeeId);
        var hasMerchant = TryGetMerchantId(out merchantId);
        if (!hasEmployee || !hasMerchant)
            return BadRequest(new { message = "Token non valido" });
        return null;
    }

    /// <summary>
    /// Verifica che il dipendente abbia almeno il livello richiesto sul Magazzino.
    /// Restituisce 403 Forbidden se il livello è insufficiente.
    /// </summary>
    private ActionResult? RequireLevel(FeatureAccessLevel minimumLevel)
    {
        if (!User.HasFeature(MerchantFeature.Magazzino))
            return Forbid();
        if (!User.RequireFeatureLevel(MerchantFeature.Magazzino, minimumLevel))
            return Forbid();
        return null;
    }

    // ── Consultazione (ReadOnly) ────────────────────────────────────────────

    [HttpGet("overview")]
    public async Task<ActionResult<EmployeeInventoryOverviewDto>> GetOverview([FromQuery] int? branchId = null)
    {
        if (ResolveIdentity(out int employeeId, out int merchantId) is { } bad) return bad;
        if (RequireLevel(FeatureAccessLevel.ReadOnly) is { } forbidden) return forbidden;

        try
        {
            var overview = await _employeeInventoryService.GetOverviewAsync(employeeId, merchantId, branchId);
            return Ok(overview);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("items")]
    public async Task<ActionResult<List<InventoryItemDto>>> GetItems([FromQuery] int? branchId = null, [FromQuery] string? search = null)
    {
        if (ResolveIdentity(out int employeeId, out int merchantId) is { } bad) return bad;
        if (RequireLevel(FeatureAccessLevel.ReadOnly) is { } forbidden) return forbidden;

        try
        {
            var items = await _employeeInventoryService.GetItemsAsync(employeeId, merchantId, branchId, search);
            return Ok(items);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("movements")]
    public async Task<ActionResult<List<InventoryMovementDto>>> GetMovements(
        [FromQuery] int? branchId = null,
        [FromQuery] int? itemId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null)
    {
        if (ResolveIdentity(out int employeeId, out int merchantId) is { } bad) return bad;
        if (RequireLevel(FeatureAccessLevel.ReadOnly) is { } forbidden) return forbidden;

        try
        {
            var movements = await _employeeInventoryService.GetMovementsAsync(employeeId, merchantId, branchId, itemId, from, to);
            return Ok(movements);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<List<LowStockReportRowDto>>> GetLowStock([FromQuery] int? branchId = null)
    {
        if (ResolveIdentity(out int employeeId, out int merchantId) is { } bad) return bad;
        if (RequireLevel(FeatureAccessLevel.ReadOnly) is { } forbidden) return forbidden;

        try
        {
            var rows = await _employeeInventoryService.GetLowStockAsync(employeeId, merchantId, branchId);
            return Ok(rows);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("suppliers")]
    public async Task<ActionResult<List<SupplierDto>>> GetSuppliers([FromQuery] bool includeInactive = true)
    {
        if (ResolveIdentity(out int employeeId, out int merchantId) is { } bad) return bad;
        if (RequireLevel(FeatureAccessLevel.ReadOnly) is { } forbidden) return forbidden;

        try
        {
            var suppliers = await _employeeInventoryService.GetSuppliersAsync(employeeId, merchantId, includeInactive);
            return Ok(suppliers);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("purchase-orders")]
    public async Task<ActionResult<List<PurchaseOrderDto>>> GetPurchaseOrders(
        [FromQuery] int? branchId = null,
        [FromQuery] PurchaseOrderStatus? status = null)
    {
        if (ResolveIdentity(out int employeeId, out int merchantId) is { } bad) return bad;
        if (RequireLevel(FeatureAccessLevel.ReadOnly) is { } forbidden) return forbidden;

        try
        {
            var orders = await _employeeInventoryService.GetPurchaseOrdersAsync(employeeId, merchantId, branchId, status);
            return Ok(orders);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("purchase-orders/{id}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetPurchaseOrder(int id)
    {
        if (ResolveIdentity(out int employeeId, out int merchantId) is { } bad) return bad;
        if (RequireLevel(FeatureAccessLevel.ReadOnly) is { } forbidden) return forbidden;

        try
        {
            var order = await _employeeInventoryService.GetPurchaseOrderByIdAsync(employeeId, merchantId, id);
            if (order == null)
                return NotFound(new { message = "Ordine acquisto non trovato" });

            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Operatività quotidiana (Operator) ───────────────────────────────────

    [HttpPost("adjustments")]
    public async Task<ActionResult<InventoryMovementDto>> CreateAdjustment([FromBody] CreateInventoryAdjustmentRequest request)
    {
        if (ResolveIdentity(out int employeeId, out int merchantId) is { } bad) return bad;
        if (!TryGetUserId(out int userId))
            return BadRequest(new { message = "Token non valido" });
        if (RequireLevel(FeatureAccessLevel.Operator) is { } forbidden) return forbidden;

        try
        {
            var movement = await _employeeInventoryService.CreateAdjustmentAsync(employeeId, merchantId, userId, request);
            return Ok(movement);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Il saldo è stato modificato da un'altra operazione. Riprova." });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Errore di persistenza durante la rettifica stock per employeeId {EmployeeId}, merchantId {MerchantId}, branchId {BranchId}, itemId {ItemId}", employeeId, merchantId, request.BranchId, request.ItemId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Errore durante il salvataggio della rettifica stock." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore inatteso durante la rettifica stock per employeeId {EmployeeId}, merchantId {MerchantId}, branchId {BranchId}, itemId {ItemId}", employeeId, merchantId, request.BranchId, request.ItemId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Errore inatteso durante la rettifica stock." });
        }
    }

    [HttpPost("purchase-orders/{id}/receive")]
    public async Task<ActionResult<PurchaseOrderDto>> ReceivePurchaseOrder(int id, [FromBody] CreateGoodsReceiptRequest request)
    {
        if (ResolveIdentity(out int employeeId, out int merchantId) is { } bad) return bad;
        if (!TryGetUserId(out int userId))
            return BadRequest(new { message = "Token non valido" });
        if (RequireLevel(FeatureAccessLevel.Operator) is { } forbidden) return forbidden;

        try
        {
            var order = await _employeeInventoryService.ReceiveOrderAsync(employeeId, merchantId, userId, id, request);
            if (order == null)
                return NotFound(new { message = "Ordine acquisto non trovato" });

            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Lo stock è stato modificato da un'altra operazione. Riprova." });
        }
    }

    // ── Anagrafiche e gestione ordini (Manager) ─────────────────────────────

    [HttpPost("items")]
    public async Task<ActionResult<InventoryItemDto>> CreateItem([FromBody] CreateInventoryItemRequest request)
    {
        if (ResolveIdentity(out int employeeId, out int merchantId) is { } bad) return bad;
        if (RequireLevel(FeatureAccessLevel.Manager) is { } forbidden) return forbidden;

        try
        {
            var item = await _employeeInventoryService.CreateItemAsync(employeeId, merchantId, request);
            return CreatedAtAction(nameof(GetItems), null, item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("items/{id}")]
    public async Task<ActionResult<InventoryItemDto>> UpdateItem(int id, [FromBody] UpdateInventoryItemRequest request)
    {
        if (ResolveIdentity(out int employeeId, out int merchantId) is { } bad) return bad;
        if (RequireLevel(FeatureAccessLevel.Manager) is { } forbidden) return forbidden;

        try
        {
            var item = await _employeeInventoryService.UpdateItemAsync(employeeId, merchantId, id, request);
            if (item == null)
                return NotFound(new { message = "Articolo non trovato" });

            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("suppliers")]
    public async Task<ActionResult<SupplierDto>> CreateSupplier([FromBody] CreateSupplierRequest request)
    {
        if (ResolveIdentity(out int employeeId, out int merchantId) is { } bad) return bad;
        if (RequireLevel(FeatureAccessLevel.Manager) is { } forbidden) return forbidden;

        try
        {
            var supplier = await _employeeInventoryService.CreateSupplierAsync(employeeId, merchantId, request);
            return CreatedAtAction(nameof(GetSuppliers), null, supplier);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("suppliers/{id}")]
    public async Task<ActionResult<SupplierDto>> UpdateSupplier(int id, [FromBody] UpdateSupplierRequest request)
    {
        if (ResolveIdentity(out int employeeId, out int merchantId) is { } bad) return bad;
        if (RequireLevel(FeatureAccessLevel.Manager) is { } forbidden) return forbidden;

        try
        {
            var supplier = await _employeeInventoryService.UpdateSupplierAsync(employeeId, merchantId, id, request);
            if (supplier == null)
                return NotFound(new { message = "Fornitore non trovato" });

            return Ok(supplier);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("purchase-orders")]
    public async Task<ActionResult<PurchaseOrderDto>> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request)
    {
        if (ResolveIdentity(out int employeeId, out int merchantId) is { } bad) return bad;
        if (!TryGetUserId(out int userId))
            return BadRequest(new { message = "Token non valido" });
        if (RequireLevel(FeatureAccessLevel.Manager) is { } forbidden) return forbidden;

        try
        {
            var order = await _employeeInventoryService.CreatePurchaseOrderAsync(employeeId, merchantId, userId, request);
            return CreatedAtAction(nameof(GetPurchaseOrder), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("purchase-orders/{id}/send")]
    public async Task<ActionResult<PurchaseOrderDto>> SendPurchaseOrder(int id)
    {
        if (ResolveIdentity(out int employeeId, out int merchantId) is { } bad) return bad;
        if (RequireLevel(FeatureAccessLevel.Manager) is { } forbidden) return forbidden;

        try
        {
            var order = await _employeeInventoryService.MarkPurchaseOrderSentAsync(employeeId, merchantId, id);
            if (order == null)
                return NotFound(new { message = "Ordine acquisto non trovato" });

            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("purchase-orders/{id}/cancel")]
    public async Task<ActionResult<PurchaseOrderDto>> CancelPurchaseOrder(int id)
    {
        if (ResolveIdentity(out int employeeId, out int merchantId) is { } bad) return bad;
        if (RequireLevel(FeatureAccessLevel.Manager) is { } forbidden) return forbidden;

        try
        {
            var order = await _employeeInventoryService.CancelPurchaseOrderAsync(employeeId, merchantId, id);
            if (order == null)
                return NotFound(new { message = "Ordine acquisto non trovato" });

            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
