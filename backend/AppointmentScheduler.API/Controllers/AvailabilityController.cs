using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using System.Security.Claims;

namespace AppointmentScheduler.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;

    public AvailabilityController(IAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    /// <summary>
    /// Get all availabilities for the authenticated merchant's services
    /// </summary>
    [HttpGet("my-availabilities")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<AvailabilityDto>>> GetMyAvailabilities()
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        var availabilities = await _availabilityService.GetMerchantAvailabilitiesAsync(merchantId.Value);
        return Ok(availabilities);
    }

    /// <summary>
    /// Get availabilities for a specific service (public)
    /// </summary>
    [HttpGet("service/{serviceId}")]
    public async Task<ActionResult<IEnumerable<AvailabilityDto>>> GetServiceAvailabilities(int serviceId)
    {
        var availabilities = await _availabilityService.GetServiceAvailabilitiesAsync(serviceId);
        return Ok(availabilities);
    }

    /// <summary>
    /// Get available slots for a service on a specific date (public)
    /// </summary>
    [HttpGet("available-slots")]
    public async Task<ActionResult<IEnumerable<AvailableSlotDto>>> GetAvailableSlots(
        [FromQuery] int serviceId,
        [FromQuery] DateTime date)
    {
        var slots = await _availabilityService.GetAvailableSlotsAsync(serviceId, date);
        return Ok(slots);
    }

    /// <summary>
    /// Get a single availability by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<AvailabilityDto>> GetAvailability(int id)
    {
        var availability = await _availabilityService.GetAvailabilityByIdAsync(id);
        if (availability == null)
            return NotFound();

        return Ok(availability);
    }

    /// <summary>
    /// Create a new availability
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<AvailabilityDto>> CreateAvailability(CreateAvailabilityRequest request)
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        try
        {
            var availability = await _availabilityService.CreateAvailabilityAsync(merchantId.Value, request);
            return CreatedAtAction(nameof(GetAvailability), new { id = availability.Id }, availability);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing availability
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<AvailabilityDto>> UpdateAvailability(int id, UpdateAvailabilityRequest request)
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        var availability = await _availabilityService.UpdateAvailabilityAsync(id, merchantId.Value, request);
        if (availability == null)
            return NotFound();

        return Ok(availability);
    }

    /// <summary>
    /// Delete an availability
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> DeleteAvailability(int id)
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        var success = await _availabilityService.DeleteAvailabilityAsync(id, merchantId.Value);
        if (!success)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Add slots to an availability (for TimeSlot mode)
    /// </summary>
    [HttpPost("{id}/slots")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<AvailabilityDto>> AddSlots(int id, List<CreateAvailabilitySlotRequest> slots)
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        try
        {
            var availability = await _availabilityService.AddSlotsToAvailabilityAsync(id, merchantId.Value, slots);
            if (availability == null)
                return NotFound();

            return Ok(availability);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
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
