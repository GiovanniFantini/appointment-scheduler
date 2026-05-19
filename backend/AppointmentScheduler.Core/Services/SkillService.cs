using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

public class SkillService : ISkillService
{
    private readonly ApplicationDbContext _context;

    public SkillService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SkillDto>> GetAllByMerchantAsync(int merchantId)
    {
        var skills = await _context.Skills
            .Where(s => s.MerchantId == merchantId)
            .OrderBy(s => s.Name)
            .Select(s => new SkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Color = s.Color,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                EmployeeCount = s.EmployeeSkills.Count
            })
            .ToListAsync();

        return skills;
    }

    public async Task<SkillDto?> GetByIdAsync(int id, int merchantId)
    {
        var skill = await _context.Skills
            .Where(s => s.Id == id && s.MerchantId == merchantId)
            .Select(s => new SkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Color = s.Color,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                EmployeeCount = s.EmployeeSkills.Count
            })
            .FirstOrDefaultAsync();

        return skill;
    }

    public async Task<SkillDto> CreateAsync(int merchantId, CreateSkillRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Il nome della mansione è obbligatorio.");

        var exists = await _context.Skills.AnyAsync(s => s.MerchantId == merchantId && s.Name == name);
        if (exists)
            throw new InvalidOperationException($"Esiste già una mansione con nome '{name}'.");

        var skill = new Skill
        {
            MerchantId = merchantId,
            Name = name,
            Color = string.IsNullOrWhiteSpace(request.Color) ? "#3b82f6" : request.Color,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();

        return new SkillDto
        {
            Id = skill.Id,
            Name = skill.Name,
            Color = skill.Color,
            IsActive = skill.IsActive,
            CreatedAt = skill.CreatedAt,
            EmployeeCount = 0
        };
    }

    public async Task<SkillDto?> UpdateAsync(int id, int merchantId, UpdateSkillRequest request)
    {
        var skill = await _context.Skills
            .FirstOrDefaultAsync(s => s.Id == id && s.MerchantId == merchantId);

        if (skill == null)
            return null;

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Il nome della mansione è obbligatorio.");

        if (!string.Equals(skill.Name, name, StringComparison.Ordinal))
        {
            var conflict = await _context.Skills.AnyAsync(s =>
                s.MerchantId == merchantId && s.Id != id && s.Name == name);
            if (conflict)
                throw new InvalidOperationException($"Esiste già una mansione con nome '{name}'.");
        }

        skill.Name = name;
        skill.Color = string.IsNullOrWhiteSpace(request.Color) ? skill.Color : request.Color;
        skill.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        var count = await _context.EmployeeSkills.CountAsync(es => es.SkillId == skill.Id);

        return new SkillDto
        {
            Id = skill.Id,
            Name = skill.Name,
            Color = skill.Color,
            IsActive = skill.IsActive,
            CreatedAt = skill.CreatedAt,
            EmployeeCount = count
        };
    }

    /// <summary>
    /// Elimina la mansione. Soft delete (IsActive=false) se ci sono riferimenti,
    /// hard delete altrimenti.
    /// </summary>
    public async Task<bool> DeleteAsync(int id, int merchantId)
    {
        var skill = await _context.Skills
            .FirstOrDefaultAsync(s => s.Id == id && s.MerchantId == merchantId);

        if (skill == null)
            return false;

        var hasReferences = await _context.EmployeeSkills.AnyAsync(es => es.SkillId == id)
            || await _context.EventRequiredSkills.AnyAsync(rs => rs.SkillId == id)
            || await _context.EventParticipants.AnyAsync(p => p.SkillId == id);

        if (hasReferences)
        {
            skill.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        _context.Skills.Remove(skill);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<EmployeeDto>> GetEmployeesBySkillAsync(int skillId, int merchantId)
    {
        // Verifica che la skill appartenga al merchant
        var skillExists = await _context.Skills.AnyAsync(s => s.Id == skillId && s.MerchantId == merchantId);
        if (!skillExists)
            return new List<EmployeeDto>();

        var memberships = await _context.EmployeeMemberships
            .Include(m => m.Employee)
                .ThenInclude(e => e.Skills)
                    .ThenInclude(es => es.Skill)
            .Include(m => m.Role)
                .ThenInclude(r => r.Features)
            .Where(m => m.MerchantId == merchantId
                        && m.IsActive
                        && m.Employee.Skills.Any(es => es.SkillId == skillId))
            .OrderBy(m => m.Employee.LastName)
            .ThenBy(m => m.Employee.FirstName)
            .ToListAsync();

        return memberships.Select(m => new EmployeeDto
        {
            Id = m.Employee.Id,
            FirstName = m.Employee.FirstName,
            LastName = m.Employee.LastName,
            Email = m.Employee.Email,
            PhoneNumber = m.Employee.PhoneNumber,
            IsActive = m.Employee.IsActive,
            HasUserAccount = m.Employee.UserId.HasValue,
            CreatedAt = m.Employee.CreatedAt,
            RoleId = m.RoleId,
            RoleName = m.Role?.Name,
            Skills = m.Employee.Skills
                .Where(es => es.Skill != null && es.Skill.MerchantId == merchantId)
                .Select(es => new EmployeeSkillDto
                {
                    SkillId = es.SkillId,
                    SkillName = es.Skill!.Name,
                    SkillColor = es.Skill!.Color
                }).ToList()
        }).ToList();
    }

    public async Task<List<SuggestedEmployeeDto>> GetSuggestedEmployeesAsync(
        int merchantId,
        int skillId,
        DateOnly date,
        TimeOnly? startTime,
        TimeOnly? endTime,
        int? excludeEventId = null)
    {
        // 1. Tutti i dipendenti del merchant che possiedono la skill
        var employees = await _context.EmployeeMemberships
            .Include(m => m.Employee)
                .ThenInclude(e => e.Skills)
            .Where(m => m.MerchantId == merchantId
                        && m.IsActive
                        && m.Employee.IsActive
                        && m.Employee.Skills.Any(es => es.SkillId == skillId))
            .Select(m => m.Employee)
            .Distinct()
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync();

        if (employees.Count == 0)
            return new List<SuggestedEmployeeDto>();

        var employeeIds = employees.Select(e => e.Id).ToList();

        // 2. Ferie/permessi/malattia approvati che includono la data
        var leaveTypes = new[] { EmployeeRequestType.Ferie, EmployeeRequestType.Malattia, EmployeeRequestType.Permessi };
        var leaves = await _context.EmployeeRequests
            .Where(r => r.MerchantId == merchantId
                        && r.Status == RequestStatus.Approved
                        && leaveTypes.Contains(r.Type)
                        && employeeIds.Contains(r.EmployeeId)
                        && r.StartDate <= date
                        && (r.EndDate == null || r.EndDate >= date))
            .ToListAsync();

        // 3. Altri turni nella stessa data che potrebbero sovrapporsi
        var otherShifts = await _context.Events
            .Include(e => e.Participants)
            .Where(e => e.MerchantId == merchantId
                        && e.EventType == EventType.Turno
                        && e.StartDate <= date
                        && (e.EndDate == null || e.EndDate >= date)
                        && (excludeEventId == null || e.Id != excludeEventId.Value)
                        && e.Participants.Any(p => employeeIds.Contains(p.EmployeeId)))
            .ToListAsync();

        var result = new List<SuggestedEmployeeDto>();
        foreach (var emp in employees)
        {
            string? reason = null;

            // Check leaves
            var empLeaves = leaves.Where(l => l.EmployeeId == emp.Id).ToList();
            foreach (var l in empLeaves)
            {
                bool fullDay = !l.StartTime.HasValue || !l.EndTime.HasValue;
                if (fullDay)
                {
                    reason = l.Type switch
                    {
                        EmployeeRequestType.Ferie => "In ferie",
                        EmployeeRequestType.Malattia => "In malattia",
                        EmployeeRequestType.Permessi => "In permesso",
                        _ => "Non disponibile"
                    };
                    break;
                }
                if (startTime.HasValue && endTime.HasValue
                    && IntervalsOverlap(startTime.Value, endTime.Value, l.StartTime!.Value, l.EndTime!.Value))
                {
                    reason = "Permesso orario sovrapposto";
                    break;
                }
            }

            // Check shift overlap (only if not already unavailable)
            if (reason == null)
            {
                foreach (var s in otherShifts.Where(s => s.Participants.Any(p => p.EmployeeId == emp.Id)))
                {
                    if (!startTime.HasValue || !endTime.HasValue || !s.StartTime.HasValue || !s.EndTime.HasValue)
                    {
                        reason = $"Già su turno '{s.Title}'";
                        break;
                    }
                    if (IntervalsOverlap(startTime.Value, endTime.Value, s.StartTime.Value, s.EndTime.Value))
                    {
                        reason = $"Già su turno '{s.Title}'";
                        break;
                    }
                }
            }

            result.Add(new SuggestedEmployeeDto
            {
                EmployeeId = emp.Id,
                FullName = $"{emp.FirstName} {emp.LastName}",
                IsAvailable = reason == null,
                UnavailableReason = reason
            });
        }

        // Disponibili prima
        return result.OrderByDescending(r => r.IsAvailable)
                     .ThenBy(r => r.FullName)
                     .ToList();
    }

    private static bool IntervalsOverlap(TimeOnly aStart, TimeOnly aEnd, TimeOnly bStart, TimeOnly bEnd)
        => aStart < bEnd && bStart < aEnd;
}
