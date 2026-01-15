using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessHoursController : ControllerBase
{
    private readonly IBusinessHoursService _businessHoursService;

    public BusinessHoursController(IBusinessHoursService businessHoursService)
    {
        _businessHoursService = businessHoursService;
    }

    /// <summary>
    /// Get business hours for a merchant (public - for customers to see opening hours)
    /// </summary>
    [HttpGet("merchant/{merchantId}")]
    public async Task<ActionResult<IEnumerable<BusinessHoursDto>>> GetMerchantBusinessHours(int merchantId)
    {
        var businessHours = await _businessHoursService.GetMerchantBusinessHoursAsync(merchantId);
        return Ok(businessHours);
    }

    /// <summary>
    /// Get business hours for the authenticated merchant
    /// </summary>
    [HttpGet("my-business-hours")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<BusinessHoursDto>>> GetMyBusinessHours()
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        var businessHours = await _businessHoursService.GetMerchantBusinessHoursAsync(merchantId.Value);
        return Ok(businessHours);
    }

    /// <summary>
    /// Get a single business hours entry by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<BusinessHoursDto>> GetBusinessHours(int id)
    {
        var businessHours = await _businessHoursService.GetBusinessHoursByIdAsync(id);
        if (businessHours == null)
            return NotFound();

        return Ok(businessHours);
    }

    /// <summary>
    /// Create business hours for a specific day
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<BusinessHoursDto>> CreateBusinessHours(CreateBusinessHoursDto dto)
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        try
        {
            var businessHours = await _businessHoursService.CreateBusinessHoursAsync(merchantId.Value, dto);
            return CreatedAtAction(nameof(GetBusinessHours), new { id = businessHours.Id }, businessHours);
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
    /// Setup complete week of business hours (replaces existing)
    /// </summary>
    [HttpPost("setup-week")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<BusinessHoursDto>>> SetupWeek(CreateBusinessHoursDto[] weekDays)
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        try
        {
            var businessHours = await _businessHoursService.SetupDefaultWeekAsync(merchantId.Value, weekDays);
            return Ok(businessHours);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update business hours
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<BusinessHoursDto>> UpdateBusinessHours(int id, UpdateBusinessHoursDto dto)
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        try
        {
            var businessHours = await _businessHoursService.UpdateBusinessHoursAsync(id, merchantId.Value, dto);
            return Ok(businessHours);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Delete business hours
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> DeleteBusinessHours(int id)
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        try
        {
            await _businessHoursService.DeleteBusinessHoursAsync(id, merchantId.Value);
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
