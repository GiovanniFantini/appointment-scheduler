using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Endpoint dedicati all'employee corrente (self-service).
/// Separato da EmployeesController (MerchantOnly) perché qui la policy è EmployeeOnly.
/// </summary>
[ApiController]
[Route("api/employee-profile")]
[Authorize(Policy = "EmployeeOnly")]
public class EmployeeProfileController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeeProfileController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
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

    /// <summary>
    /// Ritorna le mansioni dell'employee corrente sul merchant selezionato.
    /// </summary>
    [HttpGet("me/skills")]
    public async Task<ActionResult<List<EmployeeSkillDto>>> GetMySkills()
    {
        if (!TryGetEmployeeId(out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato nel token" });
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var employee = await _employeeService.GetByIdAsync(employeeId, merchantId);
        if (employee == null)
            return Ok(new List<EmployeeSkillDto>());

        return Ok(employee.Skills);
    }

    /// <summary>
    /// Ritorna la lista dei colleghi dell'employee corrente (per selezione in calendar modals).
    /// </summary>
    [HttpGet("colleagues")]
    public async Task<ActionResult<List<EmployeeDto>>> GetColleagues()
    {
        if (!TryGetEmployeeId(out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato nel token" });
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        // Carica tutti i dipendenti del merchant per consentire la selezione (Manager può creare turni per altri)
        var employees = await _employeeService.GetMerchantEmployeesAsync(merchantId);
        return Ok(employees ?? new List<EmployeeDto>());
    }
}
