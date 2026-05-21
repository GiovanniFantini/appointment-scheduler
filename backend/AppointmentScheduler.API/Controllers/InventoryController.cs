using System.Security.Claims;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.API.Controllers;

[ApiController]
[Route("api/merchant/inventory")]
[Authorize(Policy = "MerchantOnly")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
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

    [HttpGet("items")]
    public async Task<ActionResult<List<InventoryItemDto>>> GetItems(
        [FromQuery] int? branchId = null,
        [FromQuery] string? search = null,
        [FromQuery] bool includeInactive = true)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

        var items = await _inventoryService.GetItemsAsync(merchantId, branchId, search, includeInactive);
        return Ok(items);
    }

    [HttpGet("items/{id}")]
    public async Task<ActionResult<InventoryItemDto>> GetItem(int id, [FromQuery] int? branchId = null)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

        var item = await _inventoryService.GetItemByIdAsync(id, merchantId, branchId);
        if (item == null)
            return NotFound(new { message = "Articolo non trovato" });

        return Ok(item);
    }

    [HttpPost("items")]
    public async Task<ActionResult<InventoryItemDto>> CreateItem([FromBody] CreateInventoryItemRequest request)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

        try
        {
            var item = await _inventoryService.CreateItemAsync(merchantId, request);
            return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("items/{id}")]
    public async Task<ActionResult<InventoryItemDto>> UpdateItem(int id, [FromBody] UpdateInventoryItemRequest request)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

        try
        {
            var item = await _inventoryService.UpdateItemAsync(id, merchantId, request);
            if (item == null)
                return NotFound(new { message = "Articolo non trovato" });

            return Ok(item);
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
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

        var movements = await _inventoryService.GetMovementsAsync(merchantId, branchId, itemId, from, to);
        return Ok(movements);
    }

    [HttpPost("adjustments")]
    public async Task<ActionResult<InventoryMovementDto>> CreateAdjustment([FromBody] CreateInventoryAdjustmentRequest request)
    {
        if (!TryGetMerchantId(out int merchantId) || !TryGetUserId(out int userId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

        try
        {
            var movement = await _inventoryService.CreateAdjustmentAsync(merchantId, userId, request);
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
    }
}