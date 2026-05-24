using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.API.Authorization;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Gestione operativa risorse lato employee app.
/// </summary>
[ApiController]
[Route("api/employee/resources")]
[Authorize(Policy = "EmployeeOnly")]
public class EmployeeResourcesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeeResourcesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    private bool TryGetMerchantId(out int merchantId)
    {
        merchantId = 0;
        var claim = User.FindFirst("MerchantId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out merchantId);
    }

    private ActionResult? RequireFeature()
    {
        if (!User.HasFeature(MerchantFeature.Risorse))
            return Forbid();
        return null;
    }

    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetAll([FromQuery] EmployeeKind? kind = null)
    {
        if (RequireFeature() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var employees = await _employeeService.GetMerchantEmployeesAsync(merchantId, kind);
        return Ok(employees);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        if (RequireFeature() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var employee = await _employeeService.GetByIdAsync(id, merchantId);
        if (employee == null)
            return NotFound(new { message = "Dipendente non trovato o non autorizzato" });

        return Ok(employee);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create([FromBody] CreateEmployeeRequest request)
    {
        if (RequireFeature() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        try
        {
            var employee = await _employeeService.CreateAsync(merchantId, request);
            return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nella creazione del dipendente: {ex.Message}" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EmployeeDto>> Update(int id, [FromBody] UpdateEmployeeRequest request)
    {
        if (RequireFeature() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        try
        {
            var employee = await _employeeService.UpdateAsync(id, merchantId, request);
            if (employee == null)
                return NotFound(new { message = "Dipendente non trovato o non autorizzato" });

            return Ok(employee);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(int id)
    {
        if (RequireFeature() is { } forbidden)
            return forbidden;

        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var result = await _employeeService.RemoveFromMerchantAsync(id, merchantId);
        if (!result)
            return NotFound(new { message = "Dipendente non trovato o non autorizzato" });

        return Ok(new { message = "Dipendente rimosso dal merchant con successo" });
    }
}
