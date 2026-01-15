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
            .Include(s => s.Merchant)
            .FirstOrDefaultAsync(s => s.Id == serviceId);

        if (service == null)
            return Enumerable.Empty<AvailableSlotDto>();

        var dayOfWeek = (int)date.DayOfWeek;
        var dateOnly = date.Date;

        // STEP 1: Check for closure periods (highest priority)
        var isClosed = await _context.ClosurePeriods
            .AnyAsync(cp => cp.MerchantId == service.MerchantId
                && cp.IsActive
                && cp.StartDate <= dateOnly
                && cp.EndDate >= dateOnly);

        if (isClosed)
            return Enumerable.Empty<AvailableSlotDto>(); // Business is closed

        // STEP 2: Check for service-specific availability (overrides business hours)
        var availability = await _context.Availabilities
            .Include(a => a.Slots)
            .Where(a => a.ServiceId == serviceId && a.IsActive)
            .Where(a =>
                (a.IsRecurring == false && a.SpecificDate == dateOnly) ||
                (a.IsRecurring == true && a.DayOfWeek == dayOfWeek))
            .OrderBy(a => a.IsRecurring) // Specific dates first
            .FirstOrDefaultAsync();

        // STEP 3: If no service-specific availability, check merchant business hours
        if (availability == null)
        {
            var businessHours = await _context.BusinessHours
                .FirstOrDefaultAsync(bh => bh.MerchantId == service.MerchantId
                    && bh.DayOfWeek == dayOfWeek
                    && bh.IsActive);

            // If no business hours or closed on this day, return empty
            if (businessHours == null || !businessHours.OpeningTime.HasValue)
                return Enumerable.Empty<AvailableSlotDto>();

            // Create virtual availability from business hours
            availability = CreateVirtualAvailabilityFromBusinessHours(businessHours, service);
        }

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
    /// Creates a virtual availability object from merchant business hours
    /// This allows services to inherit the merchant's standard operating hours
    /// </summary>
    private Availability CreateVirtualAvailabilityFromBusinessHours(BusinessHours businessHours, Service service)
    {
        var virtualAvailability = new Availability
        {
            ServiceId = service.Id,
            StartTime = businessHours.OpeningTime!.Value,
            EndTime = businessHours.ClosingTime!.Value,
            IsRecurring = true,
            DayOfWeek = businessHours.DayOfWeek,
            MaxCapacity = service.MaxCapacityPerSlot,
            IsActive = true,
            Slots = new List<AvailabilitySlot>()
        };

        // For TimeSlot mode, generate slots based on business hours and slot duration
        if (service.BookingMode == BookingMode.TimeSlot && service.SlotDurationMinutes.HasValue)
        {
            var slots = new List<AvailabilitySlot>();
            var slotDuration = TimeSpan.FromMinutes(service.SlotDurationMinutes.Value);

            // Generate slots for first shift
            var currentTime = businessHours.OpeningTime!.Value;
            var endTime = businessHours.ClosingTime!.Value;

            while (currentTime.Add(slotDuration) <= endTime)
            {
                slots.Add(new AvailabilitySlot
                {
                    SlotTime = currentTime,
                    MaxCapacity = service.MaxCapacityPerSlot
                });
                currentTime = currentTime.Add(slotDuration);
            }

            // Generate slots for second shift if exists
            if (businessHours.SecondOpeningTime.HasValue && businessHours.SecondClosingTime.HasValue)
            {
                currentTime = businessHours.SecondOpeningTime.Value;
                endTime = businessHours.SecondClosingTime.Value;

                while (currentTime.Add(slotDuration) <= endTime)
                {
                    slots.Add(new AvailabilitySlot
                    {
                        SlotTime = currentTime,
                        MaxCapacity = service.MaxCapacityPerSlot
                    });
                    currentTime = currentTime.Add(slotDuration);
                }
            }

            virtualAvailability.Slots = slots;
        }

        return virtualAvailability;
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
