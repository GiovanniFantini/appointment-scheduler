using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.API.Authorization;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Gestione operativa mansioni lato employee app.
/// </summary>
[ApiController]
[Route("api/employee/skills")]
[Authorize(Policy = "EmployeeOnly")]
public class EmployeeSkillsController : ControllerBase
{
    private readonly ISkillService _skillService;

    public EmployeeSkillsController(ISkillService skillService)
    {
        _skillService = skillService;
    }

    private bool TryGetMerchantId(out int merchantId)
    {
        merchantId = 0;
        var claim = User.FindFirst("MerchantId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out merchantId);
    }

    private ActionResult? RequireFeature()
    {
        if (!User.HasFeature(MerchantFeature.Mansioni))
            return Forbid();
        return null;
    }

    [HttpGet]
    public async Task<ActionResult<List<SkillDto>>> GetAll()
    {
        if (RequireFeature() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var skills = await _skillService.GetAllByMerchantAsync(merchantId);
        return Ok(skills);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SkillDto>> GetById(int id)
    {
        if (RequireFeature() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var skill = await _skillService.GetByIdAsync(id, merchantId);
        if (skill == null)
            return NotFound(new { message = "Mansione non trovata" });

        return Ok(skill);
    }

    [HttpGet("{id}/employees")]
    public async Task<ActionResult<List<EmployeeDto>>> GetEmployees(int id)
    {
        if (RequireFeature() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var employees = await _skillService.GetEmployeesBySkillAsync(id, merchantId);
        return Ok(employees);
    }

    [HttpPost]
    public async Task<ActionResult<SkillDto>> Create([FromBody] CreateSkillRequest request)
    {
        if (RequireFeature() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        try
        {
            var skill = await _skillService.CreateAsync(merchantId, request);
            return CreatedAtAction(nameof(GetById), new { id = skill.Id }, skill);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SkillDto>> Update(int id, [FromBody] UpdateSkillRequest request)
    {
        if (RequireFeature() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        try
        {
            var skill = await _skillService.UpdateAsync(id, merchantId, request);
            if (skill == null)
                return NotFound(new { message = "Mansione non trovata" });

            return Ok(skill);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (RequireFeature() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var ok = await _skillService.DeleteAsync(id, merchantId);
        if (!ok)
            return NotFound(new { message = "Mansione non trovata" });

        return Ok(new { message = "Mansione eliminata" });
    }

    [HttpGet("{id}/suggested-employees")]
    public async Task<ActionResult<List<SuggestedEmployeeDto>>> Suggested(
        int id,
        [FromQuery] DateOnly date,
        [FromQuery] TimeOnly? startTime,
        [FromQuery] TimeOnly? endTime,
        [FromQuery] int? excludeEventId)
    {
        if (RequireFeature() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var suggested = await _skillService.GetSuggestedEmployeesAsync(
            merchantId, id, date, startTime, endTime, excludeEventId);
        return Ok(suggested);
    }
}
