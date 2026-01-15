using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Service for managing service availabilities and calculating available slots
/// </summary>
public class AvailabilityService : IAvailabilityService
{
    private readonly ApplicationDbContext _context;

    public AvailabilityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AvailabilityDto>> GetMerchantAvailabilitiesAsync(int merchantId)
    {
        var availabilities = await _context.Availabilities
            .Include(a => a.Service)
            .Include(a => a.Slots)
            .Where(a => a.Service!.MerchantId == merchantId)
            .OrderBy(a => a.IsRecurring ? a.DayOfWeek : 0)
            .ThenBy(a => a.SpecificDate)
            .ThenBy(a => a.StartTime)
            .ToListAsync();

        return availabilities.Select(MapToDto);
    }

    public async Task<IEnumerable<AvailabilityDto>> GetServiceAvailabilitiesAsync(int serviceId)
    {
        var availabilities = await _context.Availabilities
            .Include(a => a.Slots)
            .Where(a => a.ServiceId == serviceId && a.IsActive)
            .OrderBy(a => a.IsRecurring ? a.DayOfWeek : 0)
            .ThenBy(a => a.SpecificDate)
            .ThenBy(a => a.StartTime)
            .ToListAsync();

        return availabilities.Select(MapToDto);
    }

    public async Task<AvailabilityDto?> GetAvailabilityByIdAsync(int id)
    {
        var availability = await _context.Availabilities
            .Include(a => a.Slots)
            .FirstOrDefaultAsync(a => a.Id == id);

        return availability == null ? null : MapToDto(availability);
    }

    public async Task<AvailabilityDto> CreateAvailabilityAsync(int merchantId, CreateAvailabilityRequest request)
    {
        // Verify service belongs to merchant
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.MerchantId == merchantId);

        if (service == null)
            throw new UnauthorizedAccessException("Service not found or doesn't belong to merchant");

        // Validate request
        ValidateAvailabilityRequest(request);

        var availability = new Availability
        {
            ServiceId = request.ServiceId,
            DayOfWeek = request.DayOfWeek,
            SpecificDate = request.SpecificDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsRecurring = request.IsRecurring,
            MaxCapacity = request.MaxCapacity,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Availabilities.Add(availability);
        await _context.SaveChangesAsync();

        // Add slots if provided and service is TimeSlot mode
        if (request.Slots?.Any() == true && service.BookingMode == BookingMode.TimeSlot)
        {
            var slots = request.Slots.Select(s => new AvailabilitySlot
            {
                AvailabilityId = availability.Id,
                SlotTime = s.SlotTime,
                MaxCapacity = s.MaxCapacity
            }).ToList();

            _context.AvailabilitySlots.AddRange(slots);
            await _context.SaveChangesAsync();

            availability.Slots = slots;
        }

        return MapToDto(availability);
    }

    public async Task<AvailabilityDto?> UpdateAvailabilityAsync(int id, int merchantId, UpdateAvailabilityRequest request)
    {
        var availability = await _context.Availabilities
            .Include(a => a.Service)
            .Include(a => a.Slots)
            .FirstOrDefaultAsync(a => a.Id == id && a.Service!.MerchantId == merchantId);

        if (availability == null)
            return null;

        availability.StartTime = request.StartTime;
        availability.EndTime = request.EndTime;
        availability.MaxCapacity = request.MaxCapacity;
        availability.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return MapToDto(availability);
    }

    public async Task<bool> DeleteAvailabilityAsync(int id, int merchantId)
    {
        var availability = await _context.Availabilities
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id && a.Service!.MerchantId == merchantId);

        if (availability == null)
            return false;

        _context.Availabilities.Remove(availability);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<AvailabilityDto?> AddSlotsToAvailabilityAsync(int availabilityId, int merchantId, List<CreateAvailabilitySlotRequest> slotsRequest)
    {
        var availability = await _context.Availabilities
            .Include(a => a.Service)
            .Include(a => a.Slots)
            .FirstOrDefaultAsync(a => a.Id == availabilityId && a.Service!.MerchantId == merchantId);

        if (availability == null)
            return null;

        if (availability.Service!.BookingMode != BookingMode.TimeSlot)
            throw new InvalidOperationException("Slots can only be added to services with TimeSlot booking mode");

        var newSlots = slotsRequest.Select(s => new AvailabilitySlot
        {
            AvailabilityId = availabilityId,
            SlotTime = s.SlotTime,
            MaxCapacity = s.MaxCapacity
        }).ToList();

        _context.AvailabilitySlots.AddRange(newSlots);
        await _context.SaveChangesAsync();

        availability.Slots = await _context.AvailabilitySlots
            .Where(s => s.AvailabilityId == availabilityId)
            .ToListAsync();

        return MapToDto(availability);
    }

    public async Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(int serviceId, DateTime date)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == serviceId);

        if (service == null)
            return Enumerable.Empty<AvailableSlotDto>();

        // PRIORITY 1: Check if business hours are configured for this service
        // If yes, use business hours system (newer, more flexible)
        var hasBusinessHours = await _context.BusinessHours
            .AnyAsync(bh => bh.ServiceId == serviceId);

        if (hasBusinessHours)
        {
            // Use business hours system - delegate to BusinessHoursService
            // Check for exceptions first, then standard business hours
            return await GetAvailableSlotsFromBusinessHoursAsync(service, date);
        }

        // PRIORITY 2: Fallback to old availability system for backwards compatibility
        var dayOfWeek = (int)date.DayOfWeek;
        var dateOnly = date.Date;

        // Prefer specific date over recurring
        var availability = await _context.Availabilities
            .Include(a => a.Slots)
            .Where(a => a.ServiceId == serviceId && a.IsActive)
            .Where(a =>
                (a.IsRecurring == false && a.SpecificDate == dateOnly) ||
                (a.IsRecurring == true && a.DayOfWeek == dayOfWeek))
            .OrderBy(a => a.IsRecurring) // Specific dates first
            .FirstOrDefaultAsync();

        if (availability == null)
            return Enumerable.Empty<AvailableSlotDto>();

        // For TimeSlot mode, return configured slots with availability
        if (service.BookingMode == BookingMode.TimeSlot)
        {
            if (!availability.Slots.Any())
                return Enumerable.Empty<AvailableSlotDto>();

            var availableSlots = new List<AvailableSlotDto>();

            foreach (var slot in availability.Slots.OrderBy(s => s.SlotTime))
            {
                // Calculate how many bookings exist for this slot
                var slotDateTime = date.Date.Add(slot.SlotTime);
                var bookedCount = await _context.Bookings
                    .Where(b => b.ServiceId == serviceId
                                && b.StartTime == slotDateTime
                                && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
                    .SumAsync(b => b.NumberOfPeople);

                var totalCapacity = slot.MaxCapacity ?? availability.MaxCapacity ?? service.MaxCapacityPerSlot ?? int.MaxValue;
                var availableCapacity = totalCapacity - bookedCount;

                availableSlots.Add(new AvailableSlotDto
                {
                    Date = date,
                    SlotTime = slot.SlotTime,
                    AvailableCapacity = Math.Max(0, availableCapacity),
                    TotalCapacity = totalCapacity
                });
            }

            return availableSlots;
        }

        // For TimeRange and DayOnly modes, just return the availability window
        return new List<AvailableSlotDto>
        {
            new AvailableSlotDto
            {
                Date = date,
                SlotTime = availability.StartTime,
                AvailableCapacity = availability.MaxCapacity ?? int.MaxValue,
                TotalCapacity = availability.MaxCapacity ?? int.MaxValue
            }
        };
    }

    /// <summary>
    /// Get available slots from business hours system
    /// </summary>
    private async Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsFromBusinessHoursAsync(Service service, DateTime date)
    {
        // First, check for exceptions on this specific date
        var exception = await _context.BusinessHoursExceptions
            .FirstOrDefaultAsync(ex => ex.ServiceId == service.Id && ex.Date.Date == date.Date);

        if (exception != null)
        {
            if (exception.IsClosed)
                return Enumerable.Empty<AvailableSlotDto>();

            return await GenerateSlotsFromBusinessHours(service, date, exception.OpeningTime1, exception.ClosingTime1,
                exception.OpeningTime2, exception.ClosingTime2, exception.MaxCapacity);
        }

        // No exception, check business hours for this day of week
        // DayOfWeek: Monday = 0, Sunday = 6
        var dayOfWeek = (int)date.DayOfWeek;
        // Convert .NET DayOfWeek (Sunday = 0) to our format (Monday = 0)
        dayOfWeek = dayOfWeek == 0 ? 6 : dayOfWeek - 1;

        var businessHours = await _context.BusinessHours
            .FirstOrDefaultAsync(bh => bh.ServiceId == service.Id && bh.DayOfWeek == dayOfWeek);

        if (businessHours == null || businessHours.IsClosed)
            return Enumerable.Empty<AvailableSlotDto>();

        return await GenerateSlotsFromBusinessHours(service, date, businessHours.OpeningTime1, businessHours.ClosingTime1,
            businessHours.OpeningTime2, businessHours.ClosingTime2, businessHours.MaxCapacity);
    }

    private async Task<IEnumerable<AvailableSlotDto>> GenerateSlotsFromBusinessHours(
        Service service, DateTime date,
        TimeSpan? openingTime1, TimeSpan? closingTime1,
        TimeSpan? openingTime2, TimeSpan? closingTime2,
        int? maxCapacity)
    {
        var slots = new List<AvailableSlotDto>();

        if (service.BookingMode != BookingMode.TimeSlot)
        {
            // For non-TimeSlot modes, return availability windows instead of specific slots
            if (openingTime1.HasValue && closingTime1.HasValue)
            {
                slots.Add(new AvailableSlotDto
                {
                    Date = date,
                    SlotTime = openingTime1.Value,
                    TotalCapacity = maxCapacity ?? service.MaxCapacityPerSlot ?? int.MaxValue,
                    AvailableCapacity = 0 // Will be calculated
                });
            }

            if (openingTime2.HasValue && closingTime2.HasValue)
            {
                slots.Add(new AvailableSlotDto
                {
                    Date = date,
                    SlotTime = openingTime2.Value,
                    TotalCapacity = maxCapacity ?? service.MaxCapacityPerSlot ?? int.MaxValue,
                    AvailableCapacity = 0
                });
            }

            // Calculate available capacity for each window
            foreach (var slot in slots)
            {
                var bookedCount = await GetBookedCountForSlot(service.Id, date, slot.SlotTime);
                slot.AvailableCapacity = Math.Max(0, slot.TotalCapacity - bookedCount);
            }

            return slots;
        }

        // For TimeSlot mode, generate individual slots
        var slotDuration = service.SlotDurationMinutes ?? service.DurationMinutes;

        // Generate slots for first shift
        if (openingTime1.HasValue && closingTime1.HasValue)
        {
            var shiftSlots = GenerateTimeSlots(openingTime1.Value, closingTime1.Value, slotDuration);
            slots.AddRange(shiftSlots.Select(time => new AvailableSlotDto
            {
                Date = date,
                SlotTime = time,
                TotalCapacity = maxCapacity ?? service.MaxCapacityPerSlot ?? int.MaxValue,
                AvailableCapacity = 0
            }));
        }

        // Generate slots for second shift
        if (openingTime2.HasValue && closingTime2.HasValue)
        {
            var shiftSlots = GenerateTimeSlots(openingTime2.Value, closingTime2.Value, slotDuration);
            slots.AddRange(shiftSlots.Select(time => new AvailableSlotDto
            {
                Date = date,
                SlotTime = time,
                TotalCapacity = maxCapacity ?? service.MaxCapacityPerSlot ?? int.MaxValue,
                AvailableCapacity = 0
            }));
        }

        // Calculate available capacity for each slot
        foreach (var slot in slots)
        {
            var bookedCount = await GetBookedCountForSlot(service.Id, date, slot.SlotTime);
            slot.AvailableCapacity = Math.Max(0, slot.TotalCapacity - bookedCount);
        }

        return slots.OrderBy(s => s.SlotTime);
    }

    private List<TimeSpan> GenerateTimeSlots(TimeSpan startTime, TimeSpan endTime, int slotDurationMinutes)
    {
        var slots = new List<TimeSpan>();
        var currentTime = startTime;

        while (currentTime < endTime)
        {
            slots.Add(currentTime);
            currentTime = currentTime.Add(TimeSpan.FromMinutes(slotDurationMinutes));
        }

        return slots;
    }

    private async Task<int> GetBookedCountForSlot(int serviceId, DateTime date, TimeSpan slotTime)
    {
        var slotDateTime = date.Date.Add(slotTime);

        var bookedCount = await _context.Bookings
            .Where(b => b.ServiceId == serviceId &&
                       b.BookingDate.Date == date.Date &&
                       b.StartTime.TimeOfDay == slotTime &&
                       (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .SumAsync(b => b.NumberOfPeople);

        return bookedCount;
    }

    private void ValidateAvailabilityRequest(CreateAvailabilityRequest request)
    {
        if (request.IsRecurring && !request.DayOfWeek.HasValue)
            throw new ArgumentException("DayOfWeek is required for recurring availabilities");

        if (!request.IsRecurring && !request.SpecificDate.HasValue)
            throw new ArgumentException("SpecificDate is required for non-recurring availabilities");

        if (request.StartTime >= request.EndTime)
            throw new ArgumentException("EndTime must be after StartTime");
    }

    private AvailabilityDto MapToDto(Availability availability)
    {
        return new AvailabilityDto
        {
            Id = availability.Id,
            ServiceId = availability.ServiceId,
            DayOfWeek = availability.DayOfWeek,
            SpecificDate = availability.SpecificDate,
            StartTime = availability.StartTime,
            EndTime = availability.EndTime,
            IsRecurring = availability.IsRecurring,
            MaxCapacity = availability.MaxCapacity,
            IsActive = availability.IsActive,
            CreatedAt = availability.CreatedAt,
            Slots = availability.Slots?.Select(s => new AvailabilitySlotDto
            {
                Id = s.Id,
                SlotTime = s.SlotTime,
                MaxCapacity = s.MaxCapacity
            }).OrderBy(s => s.SlotTime).ToList() ?? new List<AvailabilitySlotDto>()
        };
    }
}
