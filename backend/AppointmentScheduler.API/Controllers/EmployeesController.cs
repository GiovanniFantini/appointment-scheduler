using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione dei dipendenti del merchant
/// </summary>
[ApiController]
[Route("api/employees")]
[Authorize(Policy = "ApprovedMerchantOnly")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    private bool TryGetMerchantId(out int merchantId)
    {
        merchantId = 0;
        var claim = User.FindFirst("MerchantId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out merchantId);
    }

    /// <summary>
    /// Recupera i dipendenti del merchant corrente.
    /// </summary>
    /// <param name="kind">
    /// Filtro opzionale: omesso = tutti, Internal = solo dipendenti interni,
    /// External = solo risorse esterne.
    /// </param>
    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetAll([FromQuery] EmployeeKind? kind = null)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var employees = await _employeeService.GetMerchantEmployeesAsync(merchantId, kind);
        return Ok(employees);
    }

    /// <summary>
    /// Recupera un dipendente specifico del merchant corrente
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var employee = await _employeeService.GetByIdAsync(id, merchantId);

        if (employee == null)
            return NotFound(new { message = "Dipendente non trovato o non autorizzato" });

        return Ok(employee);
    }

}
