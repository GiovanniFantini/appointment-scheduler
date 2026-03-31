using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione dei dipendenti del merchant
/// </summary>
[ApiController]
[Route("api/employees")]
[Authorize(Policy = "MerchantOnly")]
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
    /// Recupera tutti i dipendenti del merchant corrente
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetAll()
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var employees = await _employeeService.GetMerchantEmployeesAsync(merchantId);
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

    /// <summary>
    /// Aggiunge un nuovo dipendente al merchant corrente
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create([FromBody] CreateEmployeeRequest request)
    {
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

    /// <summary>
    /// Aggiorna i dati di un dipendente del merchant corrente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<EmployeeDto>> Update(int id, [FromBody] UpdateEmployeeRequest request)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var employee = await _employeeService.UpdateAsync(id, merchantId, request);

        if (employee == null)
            return NotFound(new { message = "Dipendente non trovato o non autorizzato" });

        return Ok(employee);
    }

    /// <summary>
    /// Rimuove un dipendente dal merchant (disattiva la membership)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(int id)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato nel token" });

        var result = await _employeeService.RemoveFromMerchantAsync(id, merchantId);

        if (!result)
            return NotFound(new { message = "Dipendente non trovato o non autorizzato" });

        return Ok(new { message = "Dipendente rimosso dal merchant con successo" });
    }
}
