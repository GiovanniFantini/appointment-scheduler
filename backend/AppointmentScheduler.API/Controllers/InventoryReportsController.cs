using AppointmentScheduler.API.Authorization;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Magazzino lato Merchant: solo i report. L'operatività vive sull'app Employee
/// (<see cref="EmployeeInventoryController"/>).
/// </summary>
[ApiController]
[Route("api/merchant/inventory/reports")]
[Authorize(Policy = "MerchantOnly")]
public class InventoryReportsController : ControllerBase
{
    private readonly IInventoryReportingService _inventoryReportingService;

    public InventoryReportsController(IInventoryReportingService inventoryReportingService)
    {
        _inventoryReportingService = inventoryReportingService;
    }

    private bool TryGetMerchantId(out int merchantId)
    {
        merchantId = 0;
        var claim = User.FindFirst("MerchantId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out merchantId);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<InventoryDashboardDto>> GetDashboard([FromQuery] int? branchId = null)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!User.HasFeature(MerchantFeature.Magazzino))
            return Forbid();

        var dashboard = await _inventoryReportingService.GetDashboardAsync(merchantId, branchId);
        return Ok(dashboard);
    }

    [HttpGet("valuation")]
    public async Task<ActionResult<List<InventoryValuationReportRowDto>>> GetValuation([FromQuery] int? branchId = null)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!User.HasFeature(MerchantFeature.Magazzino))
            return Forbid();

        var rows = await _inventoryReportingService.GetValuationAsync(merchantId, branchId);
        return Ok(rows);
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<List<LowStockReportRowDto>>> GetLowStock([FromQuery] int? branchId = null)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!User.HasFeature(MerchantFeature.Magazzino))
            return Forbid();

        var rows = await _inventoryReportingService.GetLowStockAsync(merchantId, branchId);
        return Ok(rows);
    }
}