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

        // Phase 2: Validate availability
        await ValidateBookingAvailabilityAsync(service, request);

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

    /// <summary>
    /// Phase 2: Validates that a booking request matches service availability
    /// </summary>
    private async Task ValidateBookingAvailabilityAsync(Service service, CreateBookingRequest request)
    {
        var date = request.BookingDate.Date;
        var dayOfWeek = (int)date.DayOfWeek;

        // Find availability for the date (prefer specific date over recurring)
        var availability = await _context.Availabilities
            .Include(a => a.Slots)
            .Where(a => a.ServiceId == service.Id && a.IsActive)
            .Where(a =>
                (a.IsRecurring == false && a.SpecificDate == date) ||
                (a.IsRecurring == true && a.DayOfWeek == dayOfWeek))
            .OrderBy(a => a.IsRecurring) // Specific dates first
            .FirstOrDefaultAsync();

        if (availability == null)
            throw new InvalidOperationException($"Il servizio non è disponibile per la data {date:dd/MM/yyyy}");

        // Validate based on booking mode
        switch (service.BookingMode)
        {
            case BookingMode.TimeSlot:
                await ValidateTimeSlotBookingAsync(service, availability, request);
                break;

            case BookingMode.TimeRange:
                ValidateTimeRangeBooking(availability, request);
                break;

            case BookingMode.DayOnly:
                // For day-only bookings, just check the date has availability
                // No specific time validation needed
                break;

            default:
                throw new InvalidOperationException("Modalità di prenotazione non supportata");
        }
    }

    /// <summary>
    /// Validates TimeSlot booking mode
    /// </summary>
    private async Task ValidateTimeSlotBookingAsync(Service service, Availability availability, CreateBookingRequest request)
    {
        if (!availability.Slots.Any())
            throw new InvalidOperationException("Nessuno slot disponibile per questa data");

        // Find the specific slot
        var requestedSlotTime = request.StartTime.TimeOfDay;
        var slot = availability.Slots.FirstOrDefault(s => s.SlotTime == requestedSlotTime);

        if (slot == null)
            throw new InvalidOperationException($"Lo slot orario {requestedSlotTime:hh\\:mm} non è disponibile");

        // Check capacity for this slot
        var totalCapacity = slot.MaxCapacity ?? availability.MaxCapacity ?? service.MaxCapacityPerSlot ?? int.MaxValue;

        var bookedCount = await _context.Bookings
            .Where(b => b.ServiceId == service.Id
                        && b.StartTime == request.StartTime
                        && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .SumAsync(b => b.NumberOfPeople);

        var availableCapacity = totalCapacity - bookedCount;

        if (availableCapacity < request.NumberOfPeople)
            throw new InvalidOperationException(
                $"Capacità insufficiente. Posti disponibili: {availableCapacity}, richiesti: {request.NumberOfPeople}");
    }

    /// <summary>
    /// Validates TimeRange booking mode
    /// </summary>
    private void ValidateTimeRangeBooking(Availability availability, CreateBookingRequest request)
    {
        var requestStartTime = request.StartTime.TimeOfDay;
        var requestEndTime = request.EndTime.TimeOfDay;

        // Check if booking times are within availability window
        if (requestStartTime < availability.StartTime)
            throw new InvalidOperationException(
                $"L'orario di inizio deve essere dopo le {availability.StartTime:hh\\:mm}");

        if (requestEndTime > availability.EndTime)
            throw new InvalidOperationException(
                $"L'orario di fine deve essere prima delle {availability.EndTime:hh\\:mm}");

        if (requestStartTime >= requestEndTime)
            throw new InvalidOperationException("L'orario di fine deve essere dopo l'orario di inizio");
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
