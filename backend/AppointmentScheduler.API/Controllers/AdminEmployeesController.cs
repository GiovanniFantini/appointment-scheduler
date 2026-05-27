using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Vista sys-admin degli employee di tutti i merchant, inclusi quelli pre-caricati
/// senza User collegato. Solo lettura.
/// </summary>
[ApiController]
[Route("api/admin/employees")]
[Authorize(Policy = "AdminOnly")]
public class AdminEmployeesController : ControllerBase
{
    private readonly IAdminEmployeeService _service;

    public AdminEmployeesController(IAdminEmployeeService service)
    {
        _service = service;
    }

    /// <summary>Lista paginata con filtri opzionali (merchantId, isActive, kind, hasAccount, search).</summary>
    [HttpGet]
    public async Task<ActionResult<AdminEmployeeListResponse>> GetAll(
        [FromQuery] int? merchantId,
        [FromQuery] bool? isActive,
        [FromQuery] bool? hasAccount,
        [FromQuery] EmployeeKind? kind,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await _service.GetEmployeesAsync(new AdminEmployeeListQuery
        {
            MerchantId = merchantId,
            IsActive = isActive,
            HasAccount = hasAccount,
            Kind = kind,
            Search = search,
            Page = page,
            PageSize = pageSize
        });
        return Ok(result);
    }

    /// <summary>Dettaglio employee con tutte le membership (merchant, ruolo, filiale, stato).</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AdminEmployeeDetailDto>> GetById(int id)
    {
        var detail = await _service.GetByIdAsync(id);
        if (detail == null)
            return NotFound(new { message = "Employee non trovato" });

        return Ok(detail);
    }
}
