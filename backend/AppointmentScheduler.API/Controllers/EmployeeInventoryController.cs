using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.API.Controllers;

[ApiController]
[Route("api/employee/inventory")]
[Authorize(Policy = "EmployeeOnly")]
public class EmployeeInventoryController : ControllerBase
{
    private readonly IEmployeeInventoryService _employeeInventoryService;

    public EmployeeInventoryController(IEmployeeInventoryService employeeInventoryService)
    {
        _employeeInventoryService = employeeInventoryService;
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

    private bool HasMagazzinoFeature()
        => User.FindAll("Feature").Any(c => c.Value == "Magazzino");

    [HttpGet("overview")]
    public async Task<ActionResult<EmployeeInventoryOverviewDto>> GetOverview([FromQuery] int? branchId = null)
    {
        if (!TryGetEmployeeId(out int employeeId) || !TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

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
        if (!TryGetEmployeeId(out int employeeId) || !TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

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
        if (!TryGetEmployeeId(out int employeeId) || !TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

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
        if (!TryGetEmployeeId(out int employeeId) || !TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

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
}