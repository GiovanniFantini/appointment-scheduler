using AppointmentScheduler.API.Authorization;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.API.Controllers;

[ApiController]
[Route("api/merchant-reports")]
[Authorize(Policy = "ApprovedMerchantOnly")]
[RequiresPlanFeature(MerchantFeature.Report)]
public class MerchantReportsController(IMerchantReportingService reports) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MerchantReportDto>> Get(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int? branchId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("MerchantId")?.Value, out var merchantId) || merchantId <= 0)
            return BadRequest(new { message = "Merchant ID non trovato nel token" });
        if (!from.HasValue || !to.HasValue)
            return BadRequest(new { message = "Specifica la data iniziale e finale." });

        try
        {
            return Ok(await reports.GetAsync(merchantId, from.Value, to.Value, branchId, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
