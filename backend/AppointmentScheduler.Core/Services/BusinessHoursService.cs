using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Service for managing business hours, exceptions, and generating available slots
/// </summary>
public class BusinessHoursService : IBusinessHoursService
{
    private readonly ApplicationDbContext _context;

    public BusinessHoursService(ApplicationDbContext context)
    {
        _context = context;
    }

    #region Business Hours

    public async Task<IEnumerable<BusinessHoursDto>> GetServiceBusinessHoursAsync(int serviceId)
    {
        var businessHours = await _context.BusinessHours
            .Where(bh => bh.ServiceId == serviceId)
            .OrderBy(bh => bh.DayOfWeek)
            .ToListAsync();

        return businessHours.Select(MapToDto);
    }

    public async Task<BusinessHoursDto?> GetBusinessHoursByIdAsync(int id)
    {
        var businessHours = await _context.BusinessHours.FindAsync(id);
        return businessHours == null ? null : MapToDto(businessHours);
    }

    public async Task<BusinessHoursDto> CreateBusinessHoursAsync(int merchantId, CreateBusinessHoursDto dto)
    {
        // Verify service belongs to merchant
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == dto.ServiceId && s.MerchantId == merchantId);

        if (service == null)
            throw new UnauthorizedAccessException("Service not found or doesn't belong to merchant");

        // Validate
        ValidateBusinessHours(dto);

        // Check if business hours for this day already exist
        var existing = await _context.BusinessHours
            .FirstOrDefaultAsync(bh => bh.ServiceId == dto.ServiceId && bh.DayOfWeek == dto.DayOfWeek);

        if (existing != null)
            throw new InvalidOperationException($"Business hours for day {dto.DayOfWeek} already exist. Use update instead.");

        var businessHours = new BusinessHours
        {
            ServiceId = dto.ServiceId,
            DayOfWeek = dto.DayOfWeek,
            IsClosed = dto.IsClosed,
            OpeningTime1 = dto.OpeningTime1,
            ClosingTime1 = dto.ClosingTime1,
            OpeningTime2 = dto.OpeningTime2,
            ClosingTime2 = dto.ClosingTime2,
            MaxCapacity = dto.MaxCapacity,
            CreatedAt = DateTime.UtcNow
        };

        _context.BusinessHours.Add(businessHours);
        await _context.SaveChangesAsync();

        return MapToDto(businessHours);
    }

    public async Task<BusinessHoursDto?> UpdateBusinessHoursAsync(int id, int merchantId, UpdateBusinessHoursDto dto)
    {
        var businessHours = await _context.BusinessHours
            .Include(bh => bh.Service)
            .FirstOrDefaultAsync(bh => bh.Id == id);

        if (businessHours == null)
            return null;

        if (businessHours.Service.MerchantId != merchantId)
            throw new UnauthorizedAccessException("Business hours don't belong to merchant");

        // Validate
        ValidateBusinessHoursUpdate(dto);

        businessHours.IsClosed = dto.IsClosed;
        businessHours.OpeningTime1 = dto.OpeningTime1;
        businessHours.ClosingTime1 = dto.ClosingTime1;
        businessHours.OpeningTime2 = dto.OpeningTime2;
        businessHours.ClosingTime2 = dto.ClosingTime2;
        businessHours.MaxCapacity = dto.MaxCapacity;
        businessHours.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(businessHours);
    }

    public async Task<bool> DeleteBusinessHoursAsync(int id, int merchantId)
    {
        var businessHours = await _context.BusinessHours
            .Include(bh => bh.Service)
            .FirstOrDefaultAsync(bh => bh.Id == id);

        if (businessHours == null)
            return false;

        if (businessHours.Service.MerchantId != merchantId)
            throw new UnauthorizedAccessException("Business hours don't belong to merchant");

        _context.BusinessHours.Remove(businessHours);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<BusinessHoursDto>> SetupWeeklyHoursAsync(int merchantId, int serviceId, List<CreateBusinessHoursDto> weeklyHours)
    {
        // Verify service belongs to merchant
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.MerchantId == merchantId);

        if (service == null)
            throw new UnauthorizedAccessException("Service not found or doesn't belong to merchant");

        // Delete existing business hours
        var existing = await _context.BusinessHours
            .Where(bh => bh.ServiceId == serviceId)
            .ToListAsync();

        _context.BusinessHours.RemoveRange(existing);

        // Create new business hours
        var newBusinessHours = weeklyHours.Select(dto =>
        {
            ValidateBusinessHours(dto);
            return new BusinessHours
            {
                ServiceId = serviceId,
                DayOfWeek = dto.DayOfWeek,
                IsClosed = dto.IsClosed,
                OpeningTime1 = dto.OpeningTime1,
                ClosingTime1 = dto.ClosingTime1,
                OpeningTime2 = dto.OpeningTime2,
                ClosingTime2 = dto.ClosingTime2,
                MaxCapacity = dto.MaxCapacity,
                CreatedAt = DateTime.UtcNow
            };
        }).ToList();

        _context.BusinessHours.AddRange(newBusinessHours);
        await _context.SaveChangesAsync();

        return newBusinessHours.Select(MapToDto);
    }

    #endregion

    #region Business Hours Exceptions

    public async Task<IEnumerable<BusinessHoursExceptionDto>> GetServiceExceptionsAsync(int serviceId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.BusinessHoursExceptions
            .Where(ex => ex.ServiceId == serviceId);

        if (fromDate.HasValue)
            query = query.Where(ex => ex.Date >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(ex => ex.Date <= toDate.Value.Date);

        var exceptions = await query
            .OrderBy(ex => ex.Date)
            .ToListAsync();

        return exceptions.Select(MapToExceptionDto);
    }

    public async Task<BusinessHoursExceptionDto?> GetExceptionByIdAsync(int id)
    {
        var exception = await _context.BusinessHoursExceptions.FindAsync(id);
        return exception == null ? null : MapToExceptionDto(exception);
    }

    public async Task<BusinessHoursExceptionDto> CreateExceptionAsync(int merchantId, CreateBusinessHoursExceptionDto dto)
    {
        // Verify service belongs to merchant
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == dto.ServiceId && s.MerchantId == merchantId);

        if (service == null)
            throw new UnauthorizedAccessException("Service not found or doesn't belong to merchant");

        // Validate
        ValidateException(dto);

        // Check if exception for this date already exists
        var existing = await _context.BusinessHoursExceptions
            .FirstOrDefaultAsync(ex => ex.ServiceId == dto.ServiceId && ex.Date.Date == dto.Date.Date);

        if (existing != null)
            throw new InvalidOperationException($"Exception for date {dto.Date:yyyy-MM-dd} already exists. Use update instead.");

        var exception = new BusinessHoursException
        {
            ServiceId = dto.ServiceId,
            Date = dto.Date.Date,
            IsClosed = dto.IsClosed,
            Reason = dto.Reason,
            OpeningTime1 = dto.OpeningTime1,
            ClosingTime1 = dto.ClosingTime1,
            OpeningTime2 = dto.OpeningTime2,
            ClosingTime2 = dto.ClosingTime2,
            MaxCapacity = dto.MaxCapacity,
            CreatedAt = DateTime.UtcNow
        };

        _context.BusinessHoursExceptions.Add(exception);
        await _context.SaveChangesAsync();

        return MapToExceptionDto(exception);
    }

    public async Task<BusinessHoursExceptionDto?> UpdateExceptionAsync(int id, int merchantId, UpdateBusinessHoursExceptionDto dto)
    {
        var exception = await _context.BusinessHoursExceptions
            .Include(ex => ex.Service)
            .FirstOrDefaultAsync(ex => ex.Id == id);

        if (exception == null)
            return null;

        if (exception.Service.MerchantId != merchantId)
            throw new UnauthorizedAccessException("Exception doesn't belong to merchant");

        // Validate
        ValidateExceptionUpdate(dto);

        exception.IsClosed = dto.IsClosed;
        exception.Reason = dto.Reason;
        exception.OpeningTime1 = dto.OpeningTime1;
        exception.ClosingTime1 = dto.ClosingTime1;
        exception.OpeningTime2 = dto.OpeningTime2;
        exception.ClosingTime2 = dto.ClosingTime2;
        exception.MaxCapacity = dto.MaxCapacity;
        exception.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToExceptionDto(exception);
    }

    public async Task<bool> DeleteExceptionAsync(int id, int merchantId)
    {
        var exception = await _context.BusinessHoursExceptions
            .Include(ex => ex.Service)
            .FirstOrDefaultAsync(ex => ex.Id == id);

        if (exception == null)
            return false;

        if (exception.Service.MerchantId != merchantId)
            throw new UnauthorizedAccessException("Exception doesn't belong to merchant");

        _context.BusinessHoursExceptions.Remove(exception);
        await _context.SaveChangesAsync();

        return true;
    }

    #endregion

    #region Availability Calculation

    public async Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsFromBusinessHoursAsync(int serviceId, DateTime date)
    {
        var service = await _context.Services.FindAsync(serviceId);
        if (service == null || !service.IsActive)
            return Array.Empty<AvailableSlotDto>();

        // First, check for exceptions on this specific date
        var exception = await _context.BusinessHoursExceptions
            .FirstOrDefaultAsync(ex => ex.ServiceId == serviceId && ex.Date.Date == date.Date);

        if (exception != null)
        {
            if (exception.IsClosed)
                return Array.Empty<AvailableSlotDto>();

            return await GenerateSlotsFromHours(service, date, exception.OpeningTime1, exception.ClosingTime1,
                exception.OpeningTime2, exception.ClosingTime2, exception.MaxCapacity);
        }

        // No exception, check business hours for this day of week
        // DayOfWeek: Monday = 0, Sunday = 6
        var dayOfWeek = (int)date.DayOfWeek;
        // Convert .NET DayOfWeek (Sunday = 0) to our format (Monday = 0)
        dayOfWeek = dayOfWeek == 0 ? 6 : dayOfWeek - 1;

        var businessHours = await _context.BusinessHours
            .FirstOrDefaultAsync(bh => bh.ServiceId == serviceId && bh.DayOfWeek == dayOfWeek);

        if (businessHours == null || businessHours.IsClosed)
            return Array.Empty<AvailableSlotDto>();

        return await GenerateSlotsFromHours(service, date, businessHours.OpeningTime1, businessHours.ClosingTime1,
            businessHours.OpeningTime2, businessHours.ClosingTime2, businessHours.MaxCapacity);
    }

    private async Task<IEnumerable<AvailableSlotDto>> GenerateSlotsFromHours(
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

    #endregion

    #region Validation

    private void ValidateBusinessHours(CreateBusinessHoursDto dto)
    {
        if (dto.DayOfWeek < 0 || dto.DayOfWeek > 6)
            throw new ArgumentException("DayOfWeek must be between 0 (Monday) and 6 (Sunday)");

        if (dto.IsClosed)
            return;

        if (!dto.OpeningTime1.HasValue || !dto.ClosingTime1.HasValue)
            throw new ArgumentException("Opening and closing times are required when not closed");

        if (dto.OpeningTime1 >= dto.ClosingTime1)
            throw new ArgumentException("Opening time must be before closing time (shift 1)");

        if (dto.OpeningTime2.HasValue && dto.ClosingTime2.HasValue)
        {
            if (dto.OpeningTime2 >= dto.ClosingTime2)
                throw new ArgumentException("Opening time must be before closing time (shift 2)");

            if (dto.OpeningTime2 < dto.ClosingTime1)
                throw new ArgumentException("Second shift must start after first shift ends");
        }
    }

    private void ValidateBusinessHoursUpdate(UpdateBusinessHoursDto dto)
    {
        if (dto.IsClosed)
            return;

        if (!dto.OpeningTime1.HasValue || !dto.ClosingTime1.HasValue)
            throw new ArgumentException("Opening and closing times are required when not closed");

        if (dto.OpeningTime1 >= dto.ClosingTime1)
            throw new ArgumentException("Opening time must be before closing time (shift 1)");

        if (dto.OpeningTime2.HasValue && dto.ClosingTime2.HasValue)
        {
            if (dto.OpeningTime2 >= dto.ClosingTime2)
                throw new ArgumentException("Opening time must be before closing time (shift 2)");

            if (dto.OpeningTime2 < dto.ClosingTime1)
                throw new ArgumentException("Second shift must start after first shift ends");
        }
    }

    private void ValidateException(CreateBusinessHoursExceptionDto dto)
    {
        if (dto.IsClosed)
            return;

        if (!dto.OpeningTime1.HasValue || !dto.ClosingTime1.HasValue)
            throw new ArgumentException("Opening and closing times are required when not closed");

        if (dto.OpeningTime1 >= dto.ClosingTime1)
            throw new ArgumentException("Opening time must be before closing time (shift 1)");

        if (dto.OpeningTime2.HasValue && dto.ClosingTime2.HasValue)
        {
            if (dto.OpeningTime2 >= dto.ClosingTime2)
                throw new ArgumentException("Opening time must be before closing time (shift 2)");

            if (dto.OpeningTime2 < dto.ClosingTime1)
                throw new ArgumentException("Second shift must start after first shift ends");
        }
    }

    private void ValidateExceptionUpdate(UpdateBusinessHoursExceptionDto dto)
    {
        if (dto.IsClosed)
            return;

        if (!dto.OpeningTime1.HasValue || !dto.ClosingTime1.HasValue)
            throw new ArgumentException("Opening and closing times are required when not closed");

        if (dto.OpeningTime1 >= dto.ClosingTime1)
            throw new ArgumentException("Opening time must be before closing time (shift 1)");

        if (dto.OpeningTime2.HasValue && dto.ClosingTime2.HasValue)
        {
            if (dto.OpeningTime2 >= dto.ClosingTime2)
                throw new ArgumentException("Opening time must be before closing time (shift 2)");

            if (dto.OpeningTime2 < dto.ClosingTime1)
                throw new ArgumentException("Second shift must start after first shift ends");
        }
    }

    #endregion

    #region Mapping

    private BusinessHoursDto MapToDto(BusinessHours businessHours)
    {
        return new BusinessHoursDto
        {
            Id = businessHours.Id,
            ServiceId = businessHours.ServiceId,
            DayOfWeek = businessHours.DayOfWeek,
            IsClosed = businessHours.IsClosed,
            OpeningTime1 = businessHours.OpeningTime1,
            ClosingTime1 = businessHours.ClosingTime1,
            OpeningTime2 = businessHours.OpeningTime2,
            ClosingTime2 = businessHours.ClosingTime2,
            MaxCapacity = businessHours.MaxCapacity
        };
    }

    private BusinessHoursExceptionDto MapToExceptionDto(BusinessHoursException exception)
    {
        return new BusinessHoursExceptionDto
        {
            Id = exception.Id,
            ServiceId = exception.ServiceId,
            Date = exception.Date,
            IsClosed = exception.IsClosed,
            Reason = exception.Reason,
            OpeningTime1 = exception.OpeningTime1,
            ClosingTime1 = exception.ClosingTime1,
            OpeningTime2 = exception.OpeningTime2,
            ClosingTime2 = exception.ClosingTime2,
            MaxCapacity = exception.MaxCapacity
        };
    }

    #endregion
}
