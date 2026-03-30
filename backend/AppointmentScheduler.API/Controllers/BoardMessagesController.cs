using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione della bacheca aziendale (comunicazioni merchant-employee)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BoardMessagesController : ControllerBase
{
    private readonly IBoardMessageService _boardMessageService;
    private readonly ApplicationDbContext _context;

    public BoardMessagesController(IBoardMessageService boardMessageService, ApplicationDbContext context)
    {
        _boardMessageService = boardMessageService;
        _context = context;
    }

    /// <summary>
    /// Recupera tutti i messaggi della bacheca del merchant (vista merchant)
    /// </summary>
    [HttpGet("merchant")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<BoardMessageDto>>> GetMerchantMessages()
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var messages = await _boardMessageService.GetMerchantMessagesAsync(merchantId);
        return Ok(messages);
    }

    /// <summary>
    /// Recupera i messaggi attivi della bacheca per il dipendente corrente
    /// </summary>
    [HttpGet("employee")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<IEnumerable<BoardMessageDto>>> GetEmployeeMessages([FromQuery] int? merchantId = null)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return BadRequest(new { message = "User ID non trovato" });

        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        // Se merchantId non specificato, recupera il primo merchant dell'employee
        if (!merchantId.HasValue)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId && e.IsActive);

            if (employee == null)
                return NotFound(new { message = "Nessun merchant trovato" });

            merchantId = employee.MerchantId;
        }

        var messages = await _boardMessageService.GetActiveMessagesForEmployeeAsync(merchantId.Value, employeeId);
        return Ok(messages);
    }

    /// <summary>
    /// Recupera un messaggio specifico
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<BoardMessageDto>> GetById(int id)
    {
        int? employeeId = null;
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
        if (!string.IsNullOrEmpty(employeeIdClaim) && int.TryParse(employeeIdClaim, out int parsedEmployeeId))
            employeeId = parsedEmployeeId;

        var message = await _boardMessageService.GetMessageByIdAsync(id, employeeId);

        if (message == null)
            return NotFound(new { message = "Messaggio non trovato" });

        return Ok(message);
    }

    /// <summary>
    /// Crea un nuovo messaggio nella bacheca (solo merchant)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<BoardMessageDto>> Create([FromBody] CreateBoardMessageRequest request)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return BadRequest(new { message = "User ID non trovato" });

        var message = await _boardMessageService.CreateMessageAsync(merchantId, userId, request);
        return CreatedAtAction(nameof(GetById), new { id = message.Id }, message);
    }

    /// <summary>
    /// Aggiorna un messaggio esistente (solo merchant)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<BoardMessageDto>> Update(int id, [FromBody] UpdateBoardMessageRequest request)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var message = await _boardMessageService.UpdateMessageAsync(id, merchantId, request);

        if (message == null)
            return NotFound(new { message = "Messaggio non trovato" });

        return Ok(message);
    }

    /// <summary>
    /// Elimina un messaggio (soft delete, solo merchant)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var result = await _boardMessageService.DeleteMessageAsync(id, merchantId);

        if (!result)
            return NotFound(new { message = "Messaggio non trovato" });

        return NoContent();
    }

    /// <summary>
    /// Segna un messaggio come letto dal dipendente corrente
    /// </summary>
    [HttpPost("{id}/read")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            return BadRequest(new { message = "Employee ID non trovato" });

        var result = await _boardMessageService.MarkAsReadAsync(id, employeeId);

        if (!result)
            return NotFound(new { message = "Messaggio non trovato" });

        return Ok(new { message = "Messaggio segnato come letto" });
    }
}
