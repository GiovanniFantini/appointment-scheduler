using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;
using AppointmentScheduler.Core.Interfaces;

namespace AppointmentScheduler.Core.Services;

public class BusinessHoursService : IBusinessHoursService
{
    private readonly ApplicationDbContext _context;

    public BusinessHoursService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BusinessHoursDto>> GetMerchantBusinessHoursAsync(int merchantId)
    {
        var businessHours = await _context.BusinessHours
            .Where(bh => bh.MerchantId == merchantId && bh.IsActive)
            .OrderBy(bh => bh.DayOfWeek)
            .ToListAsync();

        return businessHours.Select(MapToDto);
    }

    public async Task<BusinessHoursDto?> GetBusinessHoursByIdAsync(int id)
    {
        var businessHours = await _context.BusinessHours
            .FirstOrDefaultAsync(bh => bh.Id == id);

        return businessHours == null ? null : MapToDto(businessHours);
    }

    public async Task<BusinessHoursDto> CreateBusinessHoursAsync(int merchantId, CreateBusinessHoursDto dto)
    {
        // Verify merchant exists
        var merchant = await _context.Merchants.FindAsync(merchantId);
        if (merchant == null)
            throw new ArgumentException("Merchant not found");

        // Check if business hours already exist for this day
        var existing = await _context.BusinessHours
            .FirstOrDefaultAsync(bh => bh.MerchantId == merchantId && bh.DayOfWeek == dto.DayOfWeek && bh.IsActive);

        if (existing != null)
            throw new InvalidOperationException($"Business hours already exist for day {dto.DayOfWeek}. Use update instead.");

        // Validate times
        ValidateBusinessHours(dto);

        var businessHours = new BusinessHours
        {
            MerchantId = merchantId,
            DayOfWeek = dto.DayOfWeek,
            OpeningTime = dto.OpeningTime,
            ClosingTime = dto.ClosingTime,
            SecondOpeningTime = dto.SecondOpeningTime,
            SecondClosingTime = dto.SecondClosingTime,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.BusinessHours.Add(businessHours);
        await _context.SaveChangesAsync();

        return MapToDto(businessHours);
    }

    public async Task<BusinessHoursDto> UpdateBusinessHoursAsync(int id, int merchantId, UpdateBusinessHoursDto dto)
    {
        var businessHours = await _context.BusinessHours
            .FirstOrDefaultAsync(bh => bh.Id == id && bh.MerchantId == merchantId);

        if (businessHours == null)
            throw new ArgumentException("Business hours not found or doesn't belong to merchant");

        // Update only provided fields
        if (dto.OpeningTime.HasValue)
            businessHours.OpeningTime = dto.OpeningTime.Value;

        if (dto.ClosingTime.HasValue)
            businessHours.ClosingTime = dto.ClosingTime.Value;

        if (dto.SecondOpeningTime.HasValue)
            businessHours.SecondOpeningTime = dto.SecondOpeningTime.Value;

        if (dto.SecondClosingTime.HasValue)
            businessHours.SecondClosingTime = dto.SecondClosingTime.Value;

        if (dto.IsActive.HasValue)
            businessHours.IsActive = dto.IsActive.Value;

        businessHours.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(businessHours);
    }

    public async Task DeleteBusinessHoursAsync(int id, int merchantId)
    {
        var businessHours = await _context.BusinessHours
            .FirstOrDefaultAsync(bh => bh.Id == id && bh.MerchantId == merchantId);

        if (businessHours == null)
            throw new ArgumentException("Business hours not found or doesn't belong to merchant");

        _context.BusinessHours.Remove(businessHours);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<BusinessHoursDto>> SetupDefaultWeekAsync(int merchantId, CreateBusinessHoursDto[] weekDays)
    {
        if (weekDays.Length != 7)
            throw new ArgumentException("Must provide exactly 7 days (0=Sunday to 6=Saturday)");

        // Verify merchant exists
        var merchant = await _context.Merchants.FindAsync(merchantId);
        if (merchant == null)
            throw new ArgumentException("Merchant not found");

        // Delete existing business hours
        var existing = await _context.BusinessHours
            .Where(bh => bh.MerchantId == merchantId)
            .ToListAsync();

        _context.BusinessHours.RemoveRange(existing);

        // Create new business hours
        var newBusinessHours = new List<BusinessHours>();

        for (int i = 0; i < weekDays.Length; i++)
        {
            var dto = weekDays[i];
            ValidateBusinessHours(dto);

            var businessHours = new BusinessHours
            {
                MerchantId = merchantId,
                DayOfWeek = i, // Use index as day of week
                OpeningTime = dto.OpeningTime,
                ClosingTime = dto.ClosingTime,
                SecondOpeningTime = dto.SecondOpeningTime,
                SecondClosingTime = dto.SecondClosingTime,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            newBusinessHours.Add(businessHours);
        }

        _context.BusinessHours.AddRange(newBusinessHours);
        await _context.SaveChangesAsync();

        return newBusinessHours.Select(MapToDto);
    }

    private void ValidateBusinessHours(CreateBusinessHoursDto dto)
    {
        if (dto.DayOfWeek < 0 || dto.DayOfWeek > 6)
            throw new ArgumentException("DayOfWeek must be between 0 (Sunday) and 6 (Saturday)");

        // If closed (no opening time), ensure no other times are set
        if (!dto.OpeningTime.HasValue)
        {
            if (dto.ClosingTime.HasValue || dto.SecondOpeningTime.HasValue || dto.SecondClosingTime.HasValue)
                throw new ArgumentException("If OpeningTime is null (closed), all other times must be null");
            return;
        }

        // If open, must have closing time
        if (!dto.ClosingTime.HasValue)
            throw new ArgumentException("If OpeningTime is set, ClosingTime must also be set");

        // Validate first shift
        if (dto.OpeningTime.Value >= dto.ClosingTime.Value)
            throw new ArgumentException("OpeningTime must be before ClosingTime");

        // Validate second shift if provided
        if (dto.SecondOpeningTime.HasValue || dto.SecondClosingTime.HasValue)
        {
            if (!dto.SecondOpeningTime.HasValue || !dto.SecondClosingTime.HasValue)
                throw new ArgumentException("Both SecondOpeningTime and SecondClosingTime must be set for second shift");

            if (dto.SecondOpeningTime.Value >= dto.SecondClosingTime.Value)
                throw new ArgumentException("SecondOpeningTime must be before SecondClosingTime");

            // Ensure no overlap between shifts
            if (dto.SecondOpeningTime.Value < dto.ClosingTime.Value)
                throw new ArgumentException("Second shift must start after first shift ends");
        }
    }

    private BusinessHoursDto MapToDto(BusinessHours businessHours)
    {
        return new BusinessHoursDto
        {
            Id = businessHours.Id,
            MerchantId = businessHours.MerchantId,
            DayOfWeek = businessHours.DayOfWeek,
            OpeningTime = businessHours.OpeningTime,
            ClosingTime = businessHours.ClosingTime,
            SecondOpeningTime = businessHours.SecondOpeningTime,
            SecondClosingTime = businessHours.SecondClosingTime,
            IsActive = businessHours.IsActive
        };
    }
}
