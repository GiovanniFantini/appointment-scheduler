using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.API.Controllers;

[ApiController]
[Route("api/merchant/inventory/suppliers")]
[Authorize(Policy = "MerchantOnly")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    private bool TryGetMerchantId(out int merchantId)
    {
        merchantId = 0;
        var claim = User.FindFirst("MerchantId")?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out merchantId);
    }

    private bool HasMagazzinoFeature()
        => User.FindAll("Feature").Any(c => c.Value == "Magazzino");

    [HttpGet]
    public async Task<ActionResult<List<SupplierDto>>> GetAll([FromQuery] bool includeInactive = true)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

        var suppliers = await _supplierService.GetSuppliersAsync(merchantId, includeInactive);
        return Ok(suppliers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SupplierDto>> GetById(int id)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

        var supplier = await _supplierService.GetSupplierByIdAsync(id, merchantId);
        if (supplier == null)
            return NotFound(new { message = "Fornitore non trovato" });

        return Ok(supplier);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create([FromBody] CreateSupplierRequest request)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

        try
        {
            var supplier = await _supplierService.CreateSupplierAsync(merchantId, request);
            return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplier);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SupplierDto>> Update(int id, [FromBody] UpdateSupplierRequest request)
    {
        if (!TryGetMerchantId(out int merchantId))
            return BadRequest(new { message = "Token non valido" });
        if (!HasMagazzinoFeature())
            return Forbid();

        try
        {
            var supplier = await _supplierService.UpdateSupplierAsync(id, merchantId, request);
            if (supplier == null)
                return NotFound(new { message = "Fornitore non trovato" });

            return Ok(supplier);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}