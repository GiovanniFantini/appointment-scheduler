using System.Security.Claims;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.API.Controllers;

[ApiController]
[Route("api/merchant/inventory/purchase-orders")]
[Authorize(Policy = "MerchantOnly")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;

    public PurchaseOrdersController(IPurchaseOrderService purchaseOrderService)
    {
        _purchaseOrderService = purchaseOrderService;
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

    private bool HasMagazzinoFeature()
        => User.FindAll("Feature").Any(c => c.Value == "Magazzino");

    [HttpGet]
    public async Task<ActionResult<List<PurchaseOrderDto>>> GetAll([FromQuery] int? branchId = null, [FromQuery] PurchaseOrderStatus? status = null)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

        var orders = await _purchaseOrderService.GetOrdersAsync(merchantId, branchId, status);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(int id)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

        var order = await _purchaseOrderService.GetOrderByIdAsync(id, merchantId);
        if (order == null)
            return NotFound(new { message = "Ordine acquisto non trovato" });

        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDto>> Create([FromBody] CreatePurchaseOrderRequest request)
    {
        if (!TryGetMerchantId(out int merchantId) || !TryGetUserId(out int userId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

        try
        {
            var order = await _purchaseOrderService.CreateOrderAsync(merchantId, userId, request);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/send")]
    public async Task<ActionResult<PurchaseOrderDto>> Send(int id)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

        try
        {
            var order = await _purchaseOrderService.MarkAsSentAsync(id, merchantId);
            if (order == null)
                return NotFound(new { message = "Ordine acquisto non trovato" });

            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<PurchaseOrderDto>> Cancel(int id)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

        try
        {
            var order = await _purchaseOrderService.CancelOrderAsync(id, merchantId);
            if (order == null)
                return NotFound(new { message = "Ordine acquisto non trovato" });

            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/receive")]
    public async Task<ActionResult<PurchaseOrderDto>> Receive(int id, [FromBody] CreateGoodsReceiptRequest request)
    {
        if (!TryGetMerchantId(out int merchantId) || !TryGetUserId(out int userId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

        try
        {
            var order = await _purchaseOrderService.ReceiveOrderAsync(id, merchantId, userId, request);
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
}