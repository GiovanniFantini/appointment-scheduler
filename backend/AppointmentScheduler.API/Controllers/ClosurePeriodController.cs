using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClosurePeriodController : ControllerBase
{
    private readonly IClosurePeriodService _closurePeriodService;

    public ClosurePeriodController(IClosurePeriodService closurePeriodService)
    {
        _closurePeriodService = closurePeriodService;
    }

    /// <summary>
    /// Get closure periods for a merchant (public - for customers to see when business is closed)
    /// </summary>
    [HttpGet("merchant/{merchantId}")]
    public async Task<ActionResult<IEnumerable<ClosurePeriodDto>>> GetMerchantClosurePeriods(int merchantId)
    {
        var closurePeriods = await _closurePeriodService.GetMerchantClosurePeriodsAsync(merchantId);
        return Ok(closurePeriods);
    }

    /// <summary>
    /// Get closure periods for the authenticated merchant
    /// </summary>
    [HttpGet("my-closures")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<ClosurePeriodDto>>> GetMyClosurePeriods()
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        var closurePeriods = await _closurePeriodService.GetMerchantClosurePeriodsAsync(merchantId.Value);
        return Ok(closurePeriods);
    }

    /// <summary>
    /// Get a single closure period by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<ClosurePeriodDto>> GetClosurePeriod(int id)
    {
        var closurePeriod = await _closurePeriodService.GetClosurePeriodByIdAsync(id);
        if (closurePeriod == null)
            return NotFound();

        return Ok(closurePeriod);
    }

    /// <summary>
    /// Check if merchant is closed on a specific date
    /// </summary>
    [HttpGet("merchant/{merchantId}/is-closed")]
    public async Task<ActionResult<bool>> IsClosedOnDate(int merchantId, [FromQuery] DateTime date)
    {
        var isClosed = await _closurePeriodService.IsClosedOnDateAsync(merchantId, date);
        return Ok(isClosed);
    }

    /// <summary>
    /// Create a closure period
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<ClosurePeriodDto>> CreateClosurePeriod(CreateClosurePeriodDto dto)
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        try
        {
            var closurePeriod = await _closurePeriodService.CreateClosurePeriodAsync(merchantId.Value, dto);
            return CreatedAtAction(nameof(GetClosurePeriod), new { id = closurePeriod.Id }, closurePeriod);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update a closure period
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<ClosurePeriodDto>> UpdateClosurePeriod(int id, UpdateClosurePeriodDto dto)
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        try
        {
            var closurePeriod = await _closurePeriodService.UpdateClosurePeriodAsync(id, merchantId.Value, dto);
            return Ok(closurePeriod);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Delete a closure period
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> DeleteClosurePeriod(int id)
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        try
        {
            await _closurePeriodService.DeleteClosurePeriodAsync(id, merchantId.Value);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    private int? GetMerchantId()
    {
        var merchantIdClaim = User.FindFirst("MerchantId");
        if (merchantIdClaim != null && int.TryParse(merchantIdClaim.Value, out var merchantId))
            return merchantId;

        return null;
    }
}
