using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>
/// Controller per la gestione delle prenotazioni
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Recupera le prenotazioni dell'utente corrente
    /// </summary>
    [HttpGet("my-bookings")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetMyBookings()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var bookings = await _bookingService.GetUserBookingsAsync(userId);
        return Ok(bookings);
    }

    /// <summary>
    /// Recupera le prenotazioni del merchant corrente
    /// </summary>
    [HttpGet("merchant-bookings")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetMerchantBookings()
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var bookings = await _bookingService.GetMerchantBookingsAsync(merchantId);
        return Ok(bookings);
    }

    /// <summary>
    /// Recupera una prenotazione specifica
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BookingDto>> GetBooking(int id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        if (booking == null)
            return NotFound(new { message = "Prenotazione non trovata" });

        return Ok(booking);
    }

    /// <summary>
    /// Crea una nuova prenotazione
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BookingDto>> CreateBooking([FromBody] CreateBookingRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        try
        {
            var booking = await _bookingService.CreateBookingAsync(userId, request);
            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Conferma una prenotazione (solo merchant)
    /// </summary>
    [HttpPost("{id}/confirm")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> ConfirmBooking(int id)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var result = await _bookingService.ConfirmBookingAsync(id, merchantId);

        if (!result)
            return NotFound(new { message = "Prenotazione non trovata o non puo' essere confermata" });

        return Ok(new { message = "Prenotazione confermata con successo" });
    }

    /// <summary>
    /// Cancella una prenotazione
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        var result = await _bookingService.CancelBookingAsync(id, userId);

        if (!result)
            return NotFound(new { message = "Prenotazione non trovata o non puo' essere cancellata" });

        return Ok(new { message = "Prenotazione cancellata con successo" });
    }

    /// <summary>
    /// Completa una prenotazione (solo merchant)
    /// </summary>
    [HttpPost("{id}/complete")]
    [Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> CompleteBooking(int id)
    {
        var merchantIdClaim = User.FindFirst("MerchantId")?.Value;

        if (string.IsNullOrEmpty(merchantIdClaim) || !int.TryParse(merchantIdClaim, out int merchantId))
            return BadRequest(new { message = "Merchant ID non trovato" });

        var result = await _bookingService.CompleteBookingAsync(id, merchantId);

        if (!result)
            return NotFound(new { message = "Prenotazione non trovata o non puo' essere completata" });

        return Ok(new { message = "Prenotazione completata con successo" });
    }
}
