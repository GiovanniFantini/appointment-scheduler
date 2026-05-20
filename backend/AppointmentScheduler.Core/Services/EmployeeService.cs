using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio per la gestione dei dipendenti tramite EmployeeMembership
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _context;

    public EmployeeService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Recupera tutti i dipendenti di un merchant tramite le membership attive
    /// </summary>
    public async Task<List<EmployeeDto>> GetMerchantEmployeesAsync(int merchantId)
    {
        var memberships = await _context.EmployeeMemberships
            .Include(m => m.Employee)
                .ThenInclude(e => e.Skills)
                    .ThenInclude(es => es.Skill)
            .Include(m => m.Role)
                .ThenInclude(r => r.Features)
            .Include(m => m.HomeBranch)
            .Include(m => m.HomeDepartment)
            .Include(m => m.BranchAccess)
            .Where(m => m.MerchantId == merchantId && m.IsActive)
            .OrderBy(m => m.Employee.LastName)
            .ThenBy(m => m.Employee.FirstName)
            .ToListAsync();

        return memberships.Select(m => MapToDto(m.Employee, m, merchantId)).ToList();
    }

    /// <summary>
    /// Recupera un dipendente per ID, verificando la membership al merchant
    /// </summary>
    public async Task<EmployeeDto?> GetByIdAsync(int employeeId, int merchantId)
    {
        var membership = await _context.EmployeeMemberships
            .Include(m => m.Employee)
                .ThenInclude(e => e.Skills)
                    .ThenInclude(es => es.Skill)
            .Include(m => m.Role)
                .ThenInclude(r => r.Features)
            .Include(m => m.HomeBranch)
            .Include(m => m.HomeDepartment)
            .Include(m => m.BranchAccess)
            .FirstOrDefaultAsync(m => m.EmployeeId == employeeId && m.MerchantId == merchantId && m.IsActive);

        if (membership == null)
            return null;

        return MapToDto(membership.Employee, membership, merchantId);
    }

    /// <summary>
    /// Crea un nuovo dipendente e la relativa membership.
    /// Se esiste già un Employee con questa email, riusa il record esistente.
    /// </summary>
    public async Task<EmployeeDto> CreateAsync(int merchantId, CreateEmployeeRequest request)
    {
        var normalizedEmail = request.Email.ToLower();

        // Check if the Employee already exists by email
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Email == normalizedEmail);

        if (employee == null)
        {
            // Try to link with an existing User account of type Employee
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail
                    && u.AccountType == AccountType.Employee
                    && u.IsActive);

            employee = new Employee
            {
                UserId = existingUser?.Id,
                Email = normalizedEmail,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        var homeBranchId = await ResolveHomeBranchIdAsync(merchantId, request.HomeBranchId);
        var homeDepartmentId = await ValidateDepartmentForBranchAsync(homeBranchId, request.HomeDepartmentId);

        // Check for existing membership (even inactive)
        var existingMembership = await _context.EmployeeMemberships
            .FirstOrDefaultAsync(m => m.EmployeeId == employee.Id && m.MerchantId == merchantId);

        EmployeeMembership membershipEntity;
        if (existingMembership != null)
        {
            // Reactivate existing membership
            existingMembership.IsActive = true;
            existingMembership.RoleId = request.RoleId;
            existingMembership.HomeBranchId = homeBranchId;
            existingMembership.HomeDepartmentId = homeDepartmentId;
            membershipEntity = existingMembership;
        }
        else
        {
            membershipEntity = new EmployeeMembership
            {
                EmployeeId = employee.Id,
                MerchantId = merchantId,
                RoleId = request.RoleId,
                HomeBranchId = homeBranchId,
                HomeDepartmentId = homeDepartmentId,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };
            _context.EmployeeMemberships.Add(membershipEntity);
        }

        await _context.SaveChangesAsync();

        await SyncEmployeeSkillsAsync(employee.Id, merchantId, request.SkillIds);
        await SyncBranchAccessAsync(membershipEntity.Id, merchantId, homeBranchId, request.AllowedBranchIds);

        // Return with full membership context
        return (await GetByIdAsync(employee.Id, merchantId))!;
    }

    /// <summary>
    /// Aggiorna i dati di un dipendente e/o la sua membership nel merchant
    /// </summary>
    public async Task<EmployeeDto?> UpdateAsync(int employeeId, int merchantId, UpdateEmployeeRequest request)
    {
        var membership = await _context.EmployeeMemberships
            .Include(m => m.Employee)
            .Include(m => m.Role)
                .ThenInclude(r => r.Features)
            .Include(m => m.BranchAccess)
            .FirstOrDefaultAsync(m => m.EmployeeId == employeeId && m.MerchantId == merchantId && m.IsActive);

        if (membership == null)
            return null;

        var homeBranchId = await ResolveHomeBranchIdAsync(merchantId, request.HomeBranchId);
        var homeDepartmentId = await ValidateDepartmentForBranchAsync(homeBranchId, request.HomeDepartmentId);

        var employee = membership.Employee;
        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.PhoneNumber = request.PhoneNumber;
        employee.IsActive = request.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;

        membership.RoleId = request.RoleId;
        membership.IsActive = request.IsActive;
        membership.HomeBranchId = homeBranchId;
        membership.HomeDepartmentId = homeDepartmentId;

        await _context.SaveChangesAsync();

        await SyncEmployeeSkillsAsync(employee.Id, merchantId, request.SkillIds);
        await SyncBranchAccessAsync(membership.Id, merchantId, homeBranchId, request.AllowedBranchIds);

        // Reload role with features
        await _context.Entry(membership).Reference(m => m.Role).LoadAsync();
        await _context.Entry(membership.Role).Collection(r => r.Features).LoadAsync();
        await _context.Entry(membership).Reference(m => m.HomeBranch).LoadAsync();
        if (membership.HomeDepartmentId.HasValue)
            await _context.Entry(membership).Reference(m => m.HomeDepartment).LoadAsync();
        await _context.Entry(membership).Collection(m => m.BranchAccess).LoadAsync();
        await _context.Entry(employee).Collection(e => e.Skills).LoadAsync();
        foreach (var es in employee.Skills)
            await _context.Entry(es).Reference(x => x.Skill).LoadAsync();

        return MapToDto(employee, membership, merchantId);
    }

    /// <summary>
    /// Rimuove un dipendente dal merchant disattivando la membership
    /// </summary>
    public async Task<bool> RemoveFromMerchantAsync(int employeeId, int merchantId)
    {
        var membership = await _context.EmployeeMemberships
            .FirstOrDefaultAsync(m => m.EmployeeId == employeeId && m.MerchantId == merchantId && m.IsActive);

        if (membership == null)
            return false;

        membership.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Sincronizza l'elenco delle mansioni di un employee con quelle passate (additive + remove).
    /// Considera solo Skill che appartengono al merchant corrente per evitare assegnazioni cross-tenant.
    /// </summary>
    private async Task SyncEmployeeSkillsAsync(int employeeId, int merchantId, List<int> skillIds)
    {
        var validSkillIds = await _context.Skills
            .Where(s => s.MerchantId == merchantId && skillIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();
        var desired = new HashSet<int>(validSkillIds);

        var existing = await _context.EmployeeSkills
            .Where(es => es.EmployeeId == employeeId
                         && _context.Skills.Any(s => s.Id == es.SkillId && s.MerchantId == merchantId))
            .ToListAsync();

        var existingSet = existing.Select(e => e.SkillId).ToHashSet();

        // Remove
        var toRemove = existing.Where(e => !desired.Contains(e.SkillId)).ToList();
        if (toRemove.Count > 0)
            _context.EmployeeSkills.RemoveRange(toRemove);

        // Add
        foreach (var sid in desired.Where(s => !existingSet.Contains(s)))
        {
            _context.EmployeeSkills.Add(new EmployeeSkill
            {
                EmployeeId = employeeId,
                SkillId = sid,
                AssignedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Risolve la filiale primaria: se non specificata/non valida, usa la HQ del merchant.
    /// </summary>
    private async Task<int> ResolveHomeBranchIdAsync(int merchantId, int? requestedBranchId)
    {
        if (requestedBranchId.HasValue && requestedBranchId.Value > 0)
        {
            var ok = await _context.MerchantBranches
                .AnyAsync(b => b.Id == requestedBranchId.Value && b.MerchantId == merchantId);
            if (ok)
                return requestedBranchId.Value;
            throw new InvalidOperationException("Filiale non valida per questo merchant.");
        }

        var hq = await _context.MerchantBranches
            .Where(b => b.MerchantId == merchantId)
            .OrderByDescending(b => b.IsHeadquarters)
            .ThenBy(b => b.Id)
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync();

        if (hq == null)
            throw new InvalidOperationException("Il merchant non ha filiali configurate.");

        return hq.Value;
    }

    /// <summary>
    /// Verifica che il reparto (se valorizzato) appartenga alla filiale indicata.
    /// Ritorna il departmentId valido oppure null.
    /// </summary>
    private async Task<int?> ValidateDepartmentForBranchAsync(int branchId, int? departmentId)
    {
        if (!departmentId.HasValue || departmentId.Value <= 0)
            return null;

        var ok = await _context.Departments
            .AnyAsync(d => d.Id == departmentId.Value && d.BranchId == branchId);
        if (!ok)
            throw new InvalidOperationException("Il reparto selezionato non appartiene alla filiale primaria.");

        return departmentId.Value;
    }

    /// <summary>
    /// Sincronizza le filiali aggiuntive consentite del dipendente. La HomeBranch è
    /// sempre implicitamente consentita e viene esclusa dalla tabella EmployeeBranchAccess.
    /// Considera solo filiali appartenenti al merchant (anti cross-tenant).
    /// </summary>
    private async Task SyncBranchAccessAsync(int membershipId, int merchantId, int homeBranchId, List<int> allowedBranchIds)
    {
        var validBranchIds = await _context.MerchantBranches
            .Where(b => b.MerchantId == merchantId && allowedBranchIds.Contains(b.Id))
            .Select(b => b.Id)
            .ToListAsync();
        // La HomeBranch è già consentita: non duplicarla in EmployeeBranchAccess.
        var desired = new HashSet<int>(validBranchIds.Where(id => id != homeBranchId));

        var existing = await _context.EmployeeBranchAccess
            .Where(a => a.MembershipId == membershipId)
            .ToListAsync();
        var existingSet = existing.Select(a => a.BranchId).ToHashSet();

        var toRemove = existing.Where(a => !desired.Contains(a.BranchId)).ToList();
        if (toRemove.Count > 0)
            _context.EmployeeBranchAccess.RemoveRange(toRemove);

        foreach (var bid in desired.Where(b => !existingSet.Contains(b)))
        {
            _context.EmployeeBranchAccess.Add(new EmployeeBranchAccess
            {
                MembershipId = membershipId,
                BranchId = bid
            });
        }

        await _context.SaveChangesAsync();
    }

    private static EmployeeDto MapToDto(Employee employee, EmployeeMembership membership, int merchantId)
    {
        var activeFeatures = membership.Role?.Features
            .Where(f => f.IsEnabled)
            .Select(f => f.Feature.ToString())
            .ToList() ?? new List<string>();

        var skills = employee.Skills
            .Where(es => es.Skill != null && es.Skill.MerchantId == merchantId)
            .Select(es => new EmployeeSkillDto
            {
                SkillId = es.SkillId,
                SkillName = es.Skill!.Name,
                SkillColor = es.Skill!.Color
            })
            .ToList();

        return new EmployeeDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            IsActive = employee.IsActive,
            HasUserAccount = employee.UserId.HasValue,
            CreatedAt = employee.CreatedAt,
            RoleId = membership.RoleId,
            RoleName = membership.Role?.Name,
            ActiveFeatures = activeFeatures,
            Skills = skills,
            HomeBranchId = membership.HomeBranchId,
            HomeBranchName = membership.HomeBranch?.Name,
            HomeDepartmentId = membership.HomeDepartmentId,
            HomeDepartmentName = membership.HomeDepartment?.Name,
            HomeDepartmentColor = membership.HomeDepartment?.Color,
            AllowedBranchIds = membership.BranchAccess?.Select(a => a.BranchId).ToList() ?? new List<int>()
        };
    }
}
