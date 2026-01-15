using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Service for managing business hours with multiple shifts, exceptions, and generating available slots
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
            .Include(bh => bh.Shifts.OrderBy(s => s.SortOrder))
            .Where(bh => bh.ServiceId == serviceId)
            .OrderBy(bh => bh.DayOfWeek)
            .ToListAsync();

        return businessHours.Select(MapToDto);
    }

    public async Task<BusinessHoursDto?> GetBusinessHoursByIdAsync(int id)
    {
        var businessHours = await _context.BusinessHours
            .Include(bh => bh.Shifts.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync(bh => bh.Id == id);

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
            MaxCapacity = dto.MaxCapacity,
            CreatedAt = DateTime.UtcNow
        };

        _context.BusinessHours.Add(businessHours);
        await _context.SaveChangesAsync();

        // Add shifts if not closed
        if (!dto.IsClosed && dto.Shifts?.Any() == true)
        {
            var shifts = dto.Shifts.Select(s => new BusinessHoursShift
            {
                BusinessHoursId = businessHours.Id,
                OpeningTime = s.OpeningTime,
                ClosingTime = s.ClosingTime,
                Label = s.Label,
                MaxCapacity = s.MaxCapacity,
                SortOrder = s.SortOrder
            }).ToList();

            _context.BusinessHoursShifts.AddRange(shifts);
            await _context.SaveChangesAsync();

            businessHours.Shifts = shifts;
        }

        return MapToDto(businessHours);
    }

    public async Task<BusinessHoursDto?> UpdateBusinessHoursAsync(int id, int merchantId, UpdateBusinessHoursDto dto)
    {
        var businessHours = await _context.BusinessHours
            .Include(bh => bh.Service)
            .Include(bh => bh.Shifts)
            .FirstOrDefaultAsync(bh => bh.Id == id);

        if (businessHours == null)
            return null;

        if (businessHours.Service.MerchantId != merchantId)
            throw new UnauthorizedAccessException("Business hours don't belong to merchant");

        // Validate
        ValidateBusinessHoursUpdate(dto);

        businessHours.IsClosed = dto.IsClosed;
        businessHours.MaxCapacity = dto.MaxCapacity;
        businessHours.UpdatedAt = DateTime.UtcNow;

        // Remove old shifts
        _context.BusinessHoursShifts.RemoveRange(businessHours.Shifts);

        // Add new shifts if not closed
        if (!dto.IsClosed && dto.Shifts?.Any() == true)
        {
            var newShifts = dto.Shifts.Select(s => new BusinessHoursShift
            {
                BusinessHoursId = businessHours.Id,
                OpeningTime = s.OpeningTime,
                ClosingTime = s.ClosingTime,
                Label = s.Label,
                MaxCapacity = s.MaxCapacity,
                SortOrder = s.SortOrder
            }).ToList();

            _context.BusinessHoursShifts.AddRange(newShifts);
            businessHours.Shifts = newShifts;
        }
        else
        {
            businessHours.Shifts.Clear();
        }

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

        // Delete existing business hours and their shifts (cascade will handle shifts)
        var existing = await _context.BusinessHours
            .Where(bh => bh.ServiceId == serviceId)
            .ToListAsync();

        _context.BusinessHours.RemoveRange(existing);
        await _context.SaveChangesAsync();

        // Create new business hours
        var newBusinessHours = new List<BusinessHours>();

        foreach (var dto in weeklyHours)
        {
            ValidateBusinessHours(dto);

            var businessHours = new BusinessHours
            {
                ServiceId = serviceId,
                DayOfWeek = dto.DayOfWeek,
                IsClosed = dto.IsClosed,
                MaxCapacity = dto.MaxCapacity,
                CreatedAt = DateTime.UtcNow
            };

            newBusinessHours.Add(businessHours);
        }

        _context.BusinessHours.AddRange(newBusinessHours);
        await _context.SaveChangesAsync();

        // Add shifts for each day
        foreach (var (businessHours, dto) in newBusinessHours.Zip(weeklyHours))
        {
            if (!dto.IsClosed && dto.Shifts?.Any() == true)
            {
                var shifts = dto.Shifts.Select(s => new BusinessHoursShift
                {
                    BusinessHoursId = businessHours.Id,
                    OpeningTime = s.OpeningTime,
                    ClosingTime = s.ClosingTime,
                    Label = s.Label,
                    MaxCapacity = s.MaxCapacity,
                    SortOrder = s.SortOrder
                }).ToList();

                _context.BusinessHoursShifts.AddRange(shifts);
                businessHours.Shifts = shifts;
            }
        }

        await _context.SaveChangesAsync();

        return newBusinessHours.Select(MapToDto);
    }

    #endregion

    #region Business Hours Exceptions

    public async Task<IEnumerable<BusinessHoursExceptionDto>> GetServiceExceptionsAsync(int serviceId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.BusinessHoursExceptions
            .Include(ex => ex.Shifts.OrderBy(s => s.SortOrder))
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
        var exception = await _context.BusinessHoursExceptions
            .Include(ex => ex.Shifts.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync(ex => ex.Id == id);

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
            MaxCapacity = dto.MaxCapacity,
            CreatedAt = DateTime.UtcNow
        };

        _context.BusinessHoursExceptions.Add(exception);
        await _context.SaveChangesAsync();

        // Add shifts if not closed
        if (!dto.IsClosed && dto.Shifts?.Any() == true)
        {
            var shifts = dto.Shifts.Select(s => new BusinessHoursShift
            {
                BusinessHoursExceptionId = exception.Id,
                OpeningTime = s.OpeningTime,
                ClosingTime = s.ClosingTime,
                Label = s.Label,
                MaxCapacity = s.MaxCapacity,
                SortOrder = s.SortOrder
            }).ToList();

            _context.BusinessHoursShifts.AddRange(shifts);
            await _context.SaveChangesAsync();

            exception.Shifts = shifts;
        }

        return MapToExceptionDto(exception);
    }

    public async Task<BusinessHoursExceptionDto?> UpdateExceptionAsync(int id, int merchantId, UpdateBusinessHoursExceptionDto dto)
    {
        var exception = await _context.BusinessHoursExceptions
            .Include(ex => ex.Service)
            .Include(ex => ex.Shifts)
            .FirstOrDefaultAsync(ex => ex.Id == id);

        if (exception == null)
            return null;

        if (exception.Service.MerchantId != merchantId)
            throw new UnauthorizedAccessException("Exception doesn't belong to merchant");

        // Validate
        ValidateExceptionUpdate(dto);

        exception.IsClosed = dto.IsClosed;
        exception.Reason = dto.Reason;
        exception.MaxCapacity = dto.MaxCapacity;
        exception.UpdatedAt = DateTime.UtcNow;

        // Remove old shifts
        _context.BusinessHoursShifts.RemoveRange(exception.Shifts);

        // Add new shifts if not closed
        if (!dto.IsClosed && dto.Shifts?.Any() == true)
        {
            var newShifts = dto.Shifts.Select(s => new BusinessHoursShift
            {
                BusinessHoursExceptionId = exception.Id,
                OpeningTime = s.OpeningTime,
                ClosingTime = s.ClosingTime,
                Label = s.Label,
                MaxCapacity = s.MaxCapacity,
                SortOrder = s.SortOrder
            }).ToList();

            _context.BusinessHoursShifts.AddRange(newShifts);
            exception.Shifts = newShifts;
        }
        else
        {
            exception.Shifts.Clear();
        }

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
            .Include(ex => ex.Shifts.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync(ex => ex.ServiceId == serviceId && ex.Date.Date == date.Date);

        if (exception != null)
        {
            if (exception.IsClosed)
                return Array.Empty<AvailableSlotDto>();

            return await GenerateSlotsFromShifts(service, date, exception.Shifts, exception.MaxCapacity);
        }

        // No exception, check business hours for this day of week
        // DayOfWeek: Monday = 0, Sunday = 6
        var dayOfWeek = (int)date.DayOfWeek;
        // Convert .NET DayOfWeek (Sunday = 0) to our format (Monday = 0)
        dayOfWeek = dayOfWeek == 0 ? 6 : dayOfWeek - 1;

        var businessHours = await _context.BusinessHours
            .Include(bh => bh.Shifts.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync(bh => bh.ServiceId == serviceId && bh.DayOfWeek == dayOfWeek);

        if (businessHours == null || businessHours.IsClosed)
            return Array.Empty<AvailableSlotDto>();

        return await GenerateSlotsFromShifts(service, date, businessHours.Shifts, businessHours.MaxCapacity);
    }

    private async Task<IEnumerable<AvailableSlotDto>> GenerateSlotsFromShifts(
        Service service, DateTime date,
        IEnumerable<BusinessHoursShift> shifts,
        int? defaultMaxCapacity)
    {
        var allSlots = new List<AvailableSlotDto>();

        if (!shifts.Any())
            return allSlots;

        foreach (var shift in shifts.OrderBy(s => s.SortOrder))
        {
            if (service.BookingMode == BookingMode.TimeSlot)
            {
                // Generate individual slots for this shift
                var slotDuration = service.SlotDurationMinutes ?? service.DurationMinutes;
                var timeSlots = GenerateTimeSlots(shift.OpeningTime, shift.ClosingTime, slotDuration);

                var shiftSlots = timeSlots.Select(time => new AvailableSlotDto
                {
                    Date = date,
                    SlotTime = time,
                    TotalCapacity = shift.MaxCapacity ?? defaultMaxCapacity ?? service.MaxCapacityPerSlot ?? int.MaxValue,
                    AvailableCapacity = 0 // Will be calculated
                }).ToList();

                allSlots.AddRange(shiftSlots);
            }
            else
            {
                // For TimeRange/DayOnly modes, return shift as availability window
                allSlots.Add(new AvailableSlotDto
                {
                    Date = date,
                    SlotTime = shift.OpeningTime,
                    TotalCapacity = shift.MaxCapacity ?? defaultMaxCapacity ?? service.MaxCapacityPerSlot ?? int.MaxValue,
                    AvailableCapacity = 0 // Will be calculated
                });
            }
        }

        // Calculate available capacity for each slot
        foreach (var slot in allSlots)
        {
            var bookedCount = await GetBookedCountForSlot(service.Id, date, slot.SlotTime);
            slot.AvailableCapacity = Math.Max(0, slot.TotalCapacity - bookedCount);
        }

        return allSlots.OrderBy(s => s.SlotTime);
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

        if (dto.Shifts == null || !dto.Shifts.Any())
            throw new ArgumentException("At least one shift is required when not closed");

        foreach (var shift in dto.Shifts)
        {
            if (shift.OpeningTime >= shift.ClosingTime)
                throw new ArgumentException("Opening time must be before closing time for all shifts");
        }
    }

    private void ValidateBusinessHoursUpdate(UpdateBusinessHoursDto dto)
    {
        if (dto.IsClosed)
            return;

        if (dto.Shifts == null || !dto.Shifts.Any())
            throw new ArgumentException("At least one shift is required when not closed");

        foreach (var shift in dto.Shifts)
        {
            if (shift.OpeningTime >= shift.ClosingTime)
                throw new ArgumentException("Opening time must be before closing time for all shifts");
        }
    }

    private void ValidateException(CreateBusinessHoursExceptionDto dto)
    {
        if (dto.IsClosed)
            return;

        if (dto.Shifts == null || !dto.Shifts.Any())
            throw new ArgumentException("At least one shift is required when not closed");

        foreach (var shift in dto.Shifts)
        {
            if (shift.OpeningTime >= shift.ClosingTime)
                throw new ArgumentException("Opening time must be before closing time for all shifts");
        }
    }

    private void ValidateExceptionUpdate(UpdateBusinessHoursExceptionDto dto)
    {
        if (dto.IsClosed)
            return;

        if (dto.Shifts == null || !dto.Shifts.Any())
            throw new ArgumentException("At least one shift is required when not closed");

        foreach (var shift in dto.Shifts)
        {
            if (shift.OpeningTime >= shift.ClosingTime)
                throw new ArgumentException("Opening time must be before closing time for all shifts");
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
            MaxCapacity = businessHours.MaxCapacity,
            Shifts = businessHours.Shifts?.OrderBy(s => s.SortOrder).Select(s => new BusinessHoursShiftDto
            {
                Id = s.Id,
                OpeningTime = s.OpeningTime,
                ClosingTime = s.ClosingTime,
                Label = s.Label,
                MaxCapacity = s.MaxCapacity,
                SortOrder = s.SortOrder
            }).ToList() ?? new List<BusinessHoursShiftDto>()
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
            MaxCapacity = exception.MaxCapacity,
            Shifts = exception.Shifts?.OrderBy(s => s.SortOrder).Select(s => new BusinessHoursShiftDto
            {
                Id = s.Id,
                OpeningTime = s.OpeningTime,
                ClosingTime = s.ClosingTime,
                Label = s.Label,
                MaxCapacity = s.MaxCapacity,
                SortOrder = s.SortOrder
            }).ToList() ?? new List<BusinessHoursShiftDto>()
        };
    }

    #endregion
}
