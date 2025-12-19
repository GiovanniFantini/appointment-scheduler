using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio per la gestione delle prenotazioni
/// </summary>
public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _context;

    public BookingService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Recupera tutte le prenotazioni di un utente
    /// </summary>
    public async Task<IEnumerable<BookingDto>> GetUserBookingsAsync(int userId)
    {
        var bookings = await _context.Bookings
            .Include(b => b.Service)
                .ThenInclude(s => s.Merchant)
            .Include(b => b.User)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        return bookings.Select(MapToDto);
    }

    /// <summary>
    /// Recupera tutte le prenotazioni di un merchant
    /// </summary>
    public async Task<IEnumerable<BookingDto>> GetMerchantBookingsAsync(int merchantId)
    {
        var bookings = await _context.Bookings
            .Include(b => b.Service)
                .ThenInclude(s => s.Merchant)
            .Include(b => b.User)
            .Where(b => b.Service.MerchantId == merchantId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        return bookings.Select(MapToDto);
    }

    /// <summary>
    /// Recupera una prenotazione per ID
    /// </summary>
    public async Task<BookingDto?> GetBookingByIdAsync(int bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Service)
                .ThenInclude(s => s.Merchant)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        return booking == null ? null : MapToDto(booking);
    }

    /// <summary>
    /// Crea una nuova prenotazione
    /// </summary>
    public async Task<BookingDto> CreateBookingAsync(int userId, CreateBookingRequest request)
    {
        var service = await _context.Services
            .Include(s => s.Merchant)
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId);

        if (service == null)
            throw new InvalidOperationException("Servizio non trovato");

        if (!service.IsActive || !service.Merchant.IsApproved)
            throw new InvalidOperationException("Servizio non disponibile per la prenotazione");

        var booking = new Booking
        {
            UserId = userId,
            ServiceId = request.ServiceId,
            BookingDate = request.BookingDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            NumberOfPeople = request.NumberOfPeople,
            Notes = request.Notes,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        await _context.Entry(booking)
            .Reference(b => b.Service)
            .Query()
            .Include(s => s.Merchant)
            .LoadAsync();

        await _context.Entry(booking)
            .Reference(b => b.User)
            .LoadAsync();

        return MapToDto(booking);
    }

    /// <summary>
    /// Conferma una prenotazione (solo merchant)
    /// </summary>
    public async Task<bool> ConfirmBookingAsync(int bookingId, int merchantId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.Service.MerchantId == merchantId);

        if (booking == null)
            return false;

        if (booking.Status != BookingStatus.Pending)
            return false;

        booking.Status = BookingStatus.Confirmed;
        booking.ConfirmedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Cancella una prenotazione
    /// </summary>
    public async Task<bool> CancelBookingAsync(int bookingId, int userId)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

        if (booking == null)
            return false;

        if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
            return false;

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Completa una prenotazione (solo merchant)
    /// </summary>
    public async Task<bool> CompleteBookingAsync(int bookingId, int merchantId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.Service.MerchantId == merchantId);

        if (booking == null)
            return false;

        if (booking.Status != BookingStatus.Confirmed)
            return false;

        booking.Status = BookingStatus.Completed;
        await _context.SaveChangesAsync();
        return true;
    }

    private BookingDto MapToDto(Booking booking)
    {
        return new BookingDto
        {
            Id = booking.Id,
            UserId = booking.UserId,
            UserName = $"{booking.User.FirstName} {booking.User.LastName}",
            UserEmail = booking.User.Email,
            UserPhone = booking.User.PhoneNumber,
            ServiceId = booking.ServiceId,
            ServiceName = booking.Service.Name,
            MerchantName = booking.Service.Merchant.BusinessName,
            BookingDate = booking.BookingDate,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Status = booking.Status,
            StatusText = GetStatusText(booking.Status),
            NumberOfPeople = booking.NumberOfPeople,
            Notes = booking.Notes,
            CreatedAt = booking.CreatedAt,
            ConfirmedAt = booking.ConfirmedAt,
            CancelledAt = booking.CancelledAt
        };
    }

    private string GetStatusText(BookingStatus status)
    {
        return status switch
        {
            BookingStatus.Pending => "In attesa",
            BookingStatus.Confirmed => "Confermata",
            BookingStatus.Cancelled => "Cancellata",
            BookingStatus.Completed => "Completata",
            BookingStatus.NoShow => "Cliente assente",
            _ => "Sconosciuto"
        };
    }
}
