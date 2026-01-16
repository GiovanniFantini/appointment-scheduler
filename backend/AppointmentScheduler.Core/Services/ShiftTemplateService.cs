using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio per la gestione dei template di turni
/// </summary>
public class ShiftTemplateService : IShiftTemplateService
{
    private readonly ApplicationDbContext _context;

    public ShiftTemplateService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ShiftTemplateDto>> GetMerchantShiftTemplatesAsync(int merchantId)
    {
        var templates = await _context.ShiftTemplates
            .Where(st => st.MerchantId == merchantId && st.IsActive)
            .OrderBy(st => st.Name)
            .ToListAsync();

        return templates.Select(MapToDto);
    }

    public async Task<ShiftTemplateDto?> GetShiftTemplateByIdAsync(int id)
    {
        var template = await _context.ShiftTemplates
            .FirstOrDefaultAsync(st => st.Id == id);

        return template == null ? null : MapToDto(template);
    }

    public async Task<ShiftTemplateDto> CreateShiftTemplateAsync(int merchantId, CreateShiftTemplateRequest request)
    {
        var template = new ShiftTemplate
        {
            MerchantId = merchantId,
            Name = request.Name,
            Description = request.Description,
            ShiftType = request.ShiftType,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            BreakDurationMinutes = request.BreakDurationMinutes,
            RecurrencePattern = request.RecurrencePattern,
            RecurrenceDays = request.RecurrenceDays,
            Color = request.Color,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ShiftTemplates.Add(template);
        await _context.SaveChangesAsync();

        return MapToDto(template);
    }

    public async Task<ShiftTemplateDto?> UpdateShiftTemplateAsync(int templateId, int merchantId, UpdateShiftTemplateRequest request)
    {
        var template = await _context.ShiftTemplates
            .FirstOrDefaultAsync(st => st.Id == templateId && st.MerchantId == merchantId);

        if (template == null)
            return null;

        template.Name = request.Name;
        template.Description = request.Description;
        template.ShiftType = request.ShiftType;
        template.StartTime = request.StartTime;
        template.EndTime = request.EndTime;
        template.BreakDurationMinutes = request.BreakDurationMinutes;
        template.RecurrencePattern = request.RecurrencePattern;
        template.RecurrenceDays = request.RecurrenceDays;
        template.Color = request.Color;
        template.IsActive = request.IsActive;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(template);
    }

    public async Task<bool> DeleteShiftTemplateAsync(int templateId, int merchantId)
    {
        var template = await _context.ShiftTemplates
            .FirstOrDefaultAsync(st => st.Id == templateId && st.MerchantId == merchantId);

        if (template == null)
            return false;

        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    private static ShiftTemplateDto MapToDto(ShiftTemplate template)
    {
        var totalHours = CalculateTotalHours(template.StartTime, template.EndTime, template.BreakDurationMinutes);

        return new ShiftTemplateDto
        {
            Id = template.Id,
            MerchantId = template.MerchantId,
            Name = template.Name,
            Description = template.Description,
            ShiftType = template.ShiftType,
            ShiftTypeName = template.ShiftType.ToString(),
            StartTime = template.StartTime,
            EndTime = template.EndTime,
            BreakDurationMinutes = template.BreakDurationMinutes,
            TotalHours = totalHours,
            RecurrencePattern = template.RecurrencePattern,
            RecurrenceDays = template.RecurrenceDays,
            Color = template.Color,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    private static decimal CalculateTotalHours(TimeSpan startTime, TimeSpan endTime, int breakMinutes)
    {
        var duration = endTime - startTime;
        if (duration.TotalMinutes < 0) // Turno attraversa mezzanotte
            duration = duration.Add(TimeSpan.FromDays(1));

        var workMinutes = duration.TotalMinutes - breakMinutes;
        return (decimal)(workMinutes / 60.0);
    }
}
