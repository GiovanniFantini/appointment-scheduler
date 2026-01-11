using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione dei dipendenti
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    /// <summary>
    /// Recupera tutti i dipendenti del merchant corrente
    /// </summary>
    [HttpGet("my-employees")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetMyEmployees()
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato. Assicurati di essere registrato come merchant." });

        var employees = await _employeeService.GetMerchantEmployeesAsync(merchantId);
        return Ok(employees);
    }

    /// <summary>
    /// Recupera un dipendente specifico
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var employee = await _employeeService.GetEmployeeByIdAsync(id);

        if (employee == null)
            return NotFound(new { message = "Dipendente non trovato" });

        // Verifica che il dipendente appartenga al merchant corrente
        if (employee.MerchantId != merchantId)
            return Forbid();

        return Ok(employee);
    }

    /// <summary>
    /// Crea un nuovo dipendente
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<EmployeeDto>> Create([FromBody] CreateEmployeeRequest request)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        try
        {
            var employee = await _employeeService.CreateEmployeeAsync(merchantId, request);
            return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Errore nella creazione del dipendente: {ex.Message}" });
        }
    }

    /// <summary>
    /// Aggiorna un dipendente esistente
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<EmployeeDto>> Update(int id, [FromBody] UpdateEmployeeRequest request)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var employee = await _employeeService.UpdateEmployeeAsync(id, merchantId, request);

        if (employee == null)
            return NotFound(new { message = "Dipendente non trovato o non autorizzato" });

        return Ok(employee);
    }

    /// <summary>
    /// Elimina un dipendente
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var result = await _employeeService.DeleteEmployeeAsync(id, merchantId);

        if (!result)
            return NotFound(new { message = "Dipendente non trovato o non autorizzato" });

        return Ok(new { message = "Dipendente eliminato con successo" });
    }
}
