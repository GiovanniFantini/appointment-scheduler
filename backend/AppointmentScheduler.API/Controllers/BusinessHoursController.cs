using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using System.Security.Claims;

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

    #region Business Hours

    /// <summary>
    /// Get business hours for a specific service
    /// </summary>
    [HttpGet("service/{serviceId}")]
    public async Task<ActionResult<IEnumerable<BusinessHoursDto>>> GetServiceBusinessHours(int serviceId)
    {
        var businessHours = await _businessHoursService.GetServiceBusinessHoursAsync(serviceId);
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
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
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
    /// Setup complete weekly business hours for a service (replaces existing)
    /// </summary>
    [HttpPost("service/{serviceId}/setup-weekly")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<BusinessHoursDto>>> SetupWeeklyHours(
        int serviceId,
        List<CreateBusinessHoursDto> weeklyHours)
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        try
        {
            var businessHours = await _businessHoursService.SetupWeeklyHoursAsync(merchantId.Value, serviceId, weeklyHours);
            return Ok(businessHours);
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
            if (businessHours == null)
                return NotFound();

            return Ok(businessHours);
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
    /// Delete business hours
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult> DeleteBusinessHours(int id)
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        try
        {
            var result = await _businessHoursService.DeleteBusinessHoursAsync(id, merchantId.Value);
            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    #endregion

    #region Business Hours Exceptions

    /// <summary>
    /// Get exceptions for a specific service (optionally filtered by date range)
    /// </summary>
    [HttpGet("service/{serviceId}/exceptions")]
    public async Task<ActionResult<IEnumerable<BusinessHoursExceptionDto>>> GetServiceExceptions(
        int serviceId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var exceptions = await _businessHoursService.GetServiceExceptionsAsync(serviceId, fromDate, toDate);
        return Ok(exceptions);
    }

    /// <summary>
    /// Get a single exception by ID
    /// </summary>
    [HttpGet("exceptions/{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<BusinessHoursExceptionDto>> GetException(int id)
    {
        var exception = await _businessHoursService.GetExceptionByIdAsync(id);
        if (exception == null)
            return NotFound();

        return Ok(exception);
    }

    /// <summary>
    /// Create a business hours exception (holiday, special event, etc.)
    /// </summary>
    [HttpPost("exceptions")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<BusinessHoursExceptionDto>> CreateException(CreateBusinessHoursExceptionDto dto)
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        try
        {
            var exception = await _businessHoursService.CreateExceptionAsync(merchantId.Value, dto);
            return CreatedAtAction(nameof(GetException), new { id = exception.Id }, exception);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
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
    /// Update a business hours exception
    /// </summary>
    [HttpPut("exceptions/{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<BusinessHoursExceptionDto>> UpdateException(int id, UpdateBusinessHoursExceptionDto dto)
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        try
        {
            var exception = await _businessHoursService.UpdateExceptionAsync(id, merchantId.Value, dto);
            if (exception == null)
                return NotFound();

            return Ok(exception);
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
    /// Delete a business hours exception
    /// </summary>
    [HttpDelete("exceptions/{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult> DeleteException(int id)
    {
        var merchantId = GetMerchantId();
        if (merchantId == null)
            return Unauthorized("Merchant ID not found");

        try
        {
            var result = await _businessHoursService.DeleteExceptionAsync(id, merchantId.Value);
            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    #endregion

    #region Available Slots

    /// <summary>
    /// Get available slots based on business hours for a service on a specific date (public)
    /// This is the new method that uses business hours instead of manual availability entries
    /// </summary>
    [HttpGet("service/{serviceId}/available-slots")]
    public async Task<ActionResult<IEnumerable<AvailableSlotDto>>> GetAvailableSlotsFromBusinessHours(
        int serviceId,
        [FromQuery] DateTime date)
    {
        var slots = await _businessHoursService.GetAvailableSlotsFromBusinessHoursAsync(serviceId, date);
        return Ok(slots);
    }

    #endregion

    #region Helper Methods

    private int? GetMerchantId()
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
        return string.IsNullOrEmpty(merchantIdClaim) ? null : int.Parse(merchantIdClaim);
    }

    #endregion
}
