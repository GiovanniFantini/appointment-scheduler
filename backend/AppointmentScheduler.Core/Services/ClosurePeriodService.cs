using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;
using AppointmentScheduler.Core.Interfaces;

namespace AppointmentScheduler.Core.Services;

public class ClosurePeriodService : IClosurePeriodService
{
    private readonly ApplicationDbContext _context;

    public ClosurePeriodService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ClosurePeriodDto>> GetMerchantClosurePeriodsAsync(int merchantId)
    {
        var closurePeriods = await _context.ClosurePeriods
            .Where(cp => cp.MerchantId == merchantId && cp.IsActive)
            .OrderBy(cp => cp.StartDate)
            .ToListAsync();

        return closurePeriods.Select(MapToDto);
    }

    public async Task<ClosurePeriodDto?> GetClosurePeriodByIdAsync(int id)
    {
        var closurePeriod = await _context.ClosurePeriods
            .FirstOrDefaultAsync(cp => cp.Id == id);

        return closurePeriod == null ? null : MapToDto(closurePeriod);
    }

    public async Task<ClosurePeriodDto> CreateClosurePeriodAsync(int merchantId, CreateClosurePeriodDto dto)
    {
        // Verify merchant exists
        var merchant = await _context.Merchants.FindAsync(merchantId);
        if (merchant == null)
            throw new ArgumentException("Merchant not found");

        // Validate dates
        ValidateClosurePeriod(dto);

        // Check for overlapping periods
        var hasOverlap = await _context.ClosurePeriods
            .AnyAsync(cp => cp.MerchantId == merchantId
                && cp.IsActive
                && cp.StartDate <= dto.EndDate
                && cp.EndDate >= dto.StartDate);

        if (hasOverlap)
            throw new InvalidOperationException("Closure period overlaps with existing closure");

        var closurePeriod = new ClosurePeriod
        {
            MerchantId = merchantId,
            StartDate = dto.StartDate.Date, // Ensure we store only the date part
            EndDate = dto.EndDate.Date,
            Reason = dto.Reason,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ClosurePeriods.Add(closurePeriod);
        await _context.SaveChangesAsync();

        return MapToDto(closurePeriod);
    }

    public async Task<ClosurePeriodDto> UpdateClosurePeriodAsync(int id, int merchantId, UpdateClosurePeriodDto dto)
    {
        var closurePeriod = await _context.ClosurePeriods
            .FirstOrDefaultAsync(cp => cp.Id == id && cp.MerchantId == merchantId);

        if (closurePeriod == null)
            throw new ArgumentException("Closure period not found or doesn't belong to merchant");

        // Update only provided fields
        if (dto.StartDate.HasValue)
            closurePeriod.StartDate = dto.StartDate.Value.Date;

        if (dto.EndDate.HasValue)
            closurePeriod.EndDate = dto.EndDate.Value.Date;

        if (dto.Reason != null)
            closurePeriod.Reason = dto.Reason;

        if (dto.Description != null)
            closurePeriod.Description = dto.Description;

        if (dto.IsActive.HasValue)
            closurePeriod.IsActive = dto.IsActive.Value;

        // Validate updated dates
        if (closurePeriod.StartDate > closurePeriod.EndDate)
            throw new ArgumentException("StartDate must be before or equal to EndDate");

        // Check for overlapping periods (excluding this one)
        var hasOverlap = await _context.ClosurePeriods
            .AnyAsync(cp => cp.MerchantId == merchantId
                && cp.Id != id
                && cp.IsActive
                && cp.StartDate <= closurePeriod.EndDate
                && cp.EndDate >= closurePeriod.StartDate);

        if (hasOverlap)
            throw new InvalidOperationException("Updated closure period overlaps with existing closure");

        closurePeriod.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(closurePeriod);
    }

    public async Task DeleteClosurePeriodAsync(int id, int merchantId)
    {
        var closurePeriod = await _context.ClosurePeriods
            .FirstOrDefaultAsync(cp => cp.Id == id && cp.MerchantId == merchantId);

        if (closurePeriod == null)
            throw new ArgumentException("Closure period not found or doesn't belong to merchant");

        _context.ClosurePeriods.Remove(closurePeriod);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsClosedOnDateAsync(int merchantId, DateTime date)
    {
        var dateOnly = date.Date;

        return await _context.ClosurePeriods
            .AnyAsync(cp => cp.MerchantId == merchantId
                && cp.IsActive
                && cp.StartDate <= dateOnly
                && cp.EndDate >= dateOnly);
    }

    private void ValidateClosurePeriod(CreateClosurePeriodDto dto)
    {
        if (dto.StartDate > dto.EndDate)
            throw new ArgumentException("StartDate must be before or equal to EndDate");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new ArgumentException("Reason is required");

        if (dto.EndDate.Date < DateTime.UtcNow.Date)
            throw new ArgumentException("Cannot create closure period that ends in the past");
    }

    private ClosurePeriodDto MapToDto(ClosurePeriod closurePeriod)
    {
        return new ClosurePeriodDto
        {
            Id = closurePeriod.Id,
            MerchantId = closurePeriod.MerchantId,
            StartDate = closurePeriod.StartDate,
            EndDate = closurePeriod.EndDate,
            Reason = closurePeriod.Reason,
            Description = closurePeriod.Description,
            IsActive = closurePeriod.IsActive,
            CreatedAt = closurePeriod.CreatedAt
        };
    }
}
