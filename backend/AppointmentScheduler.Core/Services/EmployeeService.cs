using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Helpers;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio per la gestione dei dipendenti tramite EmployeeMembership
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly IApplicationDbContext _context;
    private readonly IUtcClock _clock;

    /// <summary>
    /// Dominio delle email tecniche generate per le risorse esterne prive di email
    /// reale. Un indirizzo con questo dominio non  un contatto valido.
    /// </summary>
    private const string TechnicalEmailDomain = "@noemail.local";

    public EmployeeService(IApplicationDbContext context, IUtcClock clock)
    {
        _context = context;
        _clock = clock;
    }

    /// <summary>
    /// Recupera i dipendenti di un merchant tramite le membership attive.
    /// </summary>
    /// <param name="kind">
    /// Filtro opzionale per tipo di risorsa: null = tutti (default, comportamento
    /// storico), Internal = solo dipendenti, External = solo risorse esterne.
    /// </param>
    public async Task<List<EmployeeDto>> GetMerchantEmployeesAsync(int merchantId, EmployeeKind? kind = null)
    {
        var query = _context.EmployeeMemberships
            .Include(m => m.Employee)
                .ThenInclude(e => e.Skills)
                    .ThenInclude(es => es.Skill)
            .Include(m => m.Role)
                .ThenInclude(r => r.Features)
            .Include(m => m.HomeBranch)
            .Include(m => m.HomeDepartment)
            .Include(m => m.BranchAccess)
            .Where(m => m.MerchantId == merchantId && m.IsActive);

        if (kind.HasValue)
            query = query.Where(m => m.Employee.Kind == kind.Value);

        var memberships = await query
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
    /// Crea un nuovo dipendente (o risorsa esterna) e la relativa membership.
    /// Per gli interni, se esiste già un Employee con la stessa email lo riusa.
    /// Per gli esterni privi di email viene generato un indirizzo tecnico univoco
    /// e non si applica la deduplica.
    /// </summary>
    public async Task<EmployeeDto> CreateAsync(int merchantId, CreateEmployeeRequest request)
    {
        var isExternal = request.Kind == EmployeeKind.External;
        var providedEmail = request.Email?.Trim();
        var hasEmail = !string.IsNullOrWhiteSpace(providedEmail);

        // I dipendenti interni devono sempre avere un'email reale.
        if (!isExternal && !hasEmail)
            throw new InvalidOperationException("L'email è obbligatoria per i dipendenti interni.");

        Employee? employee = null;

        if (hasEmail)
        {
            var normalizedEmail = providedEmail!.ToLower();

            // Deduplica per email: riusa l'Employee esistente (vale per interni e
            // per esterni che hanno comunque fornito un'email).
            employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email == normalizedEmail);

            if (employee == null)
            {
                // Try to link with an existing User account of type Employee
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == normalizedEmail
                        && u.AccountType == AccountType.Employee
                        && u.IsActive);

                var createdAt = _clock.UtcNow;

                employee = new Employee
                {
                    UserId = existingUser?.Id,
                    Email = normalizedEmail,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    Kind = request.Kind,
                    IsActive = true,
                    CreatedAt = createdAt
                };
                ApplyExternalFields(employee, request.Kind, request.ContractType,
                    request.AgencyName, request.HourlyRate, request.ExternalNotes);

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            // Esterno senza email: nessuna deduplica, email tecnica univoca (GUID).
            var createdAt = _clock.UtcNow;
            employee = new Employee
            {
                UserId = null,
                Email = $"esterno-{Guid.NewGuid():N}{TechnicalEmailDomain}",
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                Kind = EmployeeKind.External,
                IsActive = true,
                CreatedAt = createdAt
            };
            ApplyExternalFields(employee, EmployeeKind.External, request.ContractType,
                request.AgencyName, request.HourlyRate, request.ExternalNotes);

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        var homeBranchId = await ResolveHomeBranchIdAsync(merchantId, request.HomeBranchId);
        var homeDepartmentId = await ValidateDepartmentForBranchAsync(homeBranchId, request.HomeDepartmentId);

        // Se RoleId manca/non è valido, usa il ruolo base coerente col tipo risorsa.
        var roleId = await ResolveRoleIdAsync(merchantId, request.RoleId, request.Kind);

        var existingMembership = await _context.EmployeeMemberships
            .FirstOrDefaultAsync(m => m.EmployeeId == employee.Id && m.MerchantId == merchantId);

        EmployeeMembership membershipEntity;
        if (existingMembership != null)
        {
            existingMembership.IsActive = true;
            existingMembership.RoleId = roleId;
            existingMembership.HomeBranchId = homeBranchId;
            existingMembership.HomeDepartmentId = homeDepartmentId;
            membershipEntity = existingMembership;
        }
        else
        {
            var joinedAt = _clock.UtcNow;
            membershipEntity = new EmployeeMembership
            {
                EmployeeId = employee.Id,
                MerchantId = merchantId,
                RoleId = roleId,
                HomeBranchId = homeBranchId,
                HomeDepartmentId = homeDepartmentId,
                IsActive = true,
                JoinedAt = joinedAt
            };
            _context.EmployeeMemberships.Add(membershipEntity);
        }

        await _context.SaveChangesAsync();

        await SyncEmployeeSkillsAsync(employee.Id, merchantId, request.SkillIds);
        await SyncBranchAccessAsync(membershipEntity.Id, merchantId, homeBranchId, request.AllowedBranchIds);

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
        var providedEmail = request.Email?.Trim();
        var hasRealEmail = !string.IsNullOrWhiteSpace(providedEmail)
                           && !IsTechnicalEmail(providedEmail!);

        // Un dipendente interno deve sempre avere un'email reale. Questo copre anche
        // la conversione esterno→interno: se l'esterno aveva un'email tecnica, va
        // sostituita con un indirizzo reale prima di poterlo rendere interno.
        if (request.Kind == EmployeeKind.Internal && !hasRealEmail)
        {
            throw new InvalidOperationException(
                "Un dipendente interno richiede un'email reale: indicane una valida.");
        }

        // Aggiornamento email: si applica solo se ne è stata fornita una reale.
        // Per gli esterni un'email vuota lascia invariato l'eventuale indirizzo
        // tecnico esistente (non lo si tocca).
        if (hasRealEmail)
        {
            var normalizedEmail = providedEmail!.ToLower();
            if (normalizedEmail != employee.Email)
            {
                var emailTaken = await _context.Employees
                    .AnyAsync(e => e.Email == normalizedEmail && e.Id != employee.Id);
                if (emailTaken)
                    throw new InvalidOperationException("Esiste già un'anagrafica con questa email.");
                employee.Email = normalizedEmail;
            }
        }

        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.PhoneNumber = request.PhoneNumber;
        employee.IsActive = request.IsActive;
        employee.Kind = request.Kind;
        ApplyExternalFields(employee, request.Kind, request.ContractType,
            request.AgencyName, request.HourlyRate, request.ExternalNotes);
        employee.UpdatedAt = _clock.UtcNow;

        membership.RoleId = await ResolveRoleIdAsync(merchantId, request.RoleId, request.Kind);
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

        var toRemove = existing.Where(e => !desired.Contains(e.SkillId)).ToList();
        if (toRemove.Count > 0)
            _context.EmployeeSkills.RemoveRange(toRemove);

        foreach (var sid in desired.Where(s => !existingSet.Contains(s)))
        {
            _context.EmployeeSkills.Add(new EmployeeSkill
            {
                EmployeeId = employeeId,
                SkillId = sid,
                AssignedAt = _clock.UtcNow
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
    /// Risolve il ruolo da assegnare alla membership. Se il chiamante fornisce un
    /// RoleId valido del merchant lo usa; altrimenti ripiega sul ruolo base per tipo
    /// risorsa (Interno Base / Esterno Base).
    /// </summary>
    private async Task<int> ResolveRoleIdAsync(int merchantId, int requestedRoleId, EmployeeKind kind)
    {
        if (requestedRoleId > 0)
        {
            var ok = await _context.MerchantRoles
                .AnyAsync(r => r.Id == requestedRoleId && r.MerchantId == merchantId);
            if (ok)
                return requestedRoleId;
        }

        var baseRoleName = kind == EmployeeKind.External
            ? SystemRoleNames.ExternalBase
            : SystemRoleNames.InternalBase;

        var baseRoleId = await _context.MerchantRoles
            .Where(r => r.MerchantId == merchantId && r.Name == baseRoleName)
            .OrderBy(r => r.Id)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync();

        if (baseRoleId == null)
        {
            throw new InvalidOperationException(
                $"Il merchant non ha il ruolo base richiesto ({baseRoleName}).");
        }

        return baseRoleId.Value;
    }

    /// <summary>
    /// Allinea i campi anagrafici della risorsa esterna al tipo: per un dipendente
    /// interno vengono azzerati (non devono sopravvivere a una conversione
    /// esterno→interno), per un esterno vengono valorizzati con quanto richiesto.
    /// </summary>
    private static void ApplyExternalFields(
        Employee employee,
        EmployeeKind kind,
        ExternalContractType? contractType,
        string? agencyName,
        decimal? hourlyRate,
        string? externalNotes)
    {
        if (kind == EmployeeKind.External)
        {
            employee.ContractType = contractType;
            employee.AgencyName = string.IsNullOrWhiteSpace(agencyName) ? null : agencyName.Trim();
            employee.HourlyRate = hourlyRate;
            employee.ExternalNotes = string.IsNullOrWhiteSpace(externalNotes) ? null : externalNotes.Trim();
        }
        else
        {
            employee.ContractType = null;
            employee.AgencyName = null;
            employee.HourlyRate = null;
            employee.ExternalNotes = null;
        }
    }

    /// <summary>True se l'email è un indirizzo tecnico generato e non un contatto reale.</summary>
    private static bool IsTechnicalEmail(string? email)
        => !string.IsNullOrWhiteSpace(email)
           && email.EndsWith(TechnicalEmailDomain, StringComparison.OrdinalIgnoreCase);

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

        var isTechnicalEmail = IsTechnicalEmail(employee.Email);

        return new EmployeeDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            // L'email tecnica è un dettaglio interno: non va esposta come contatto.
            Email = isTechnicalEmail ? string.Empty : employee.Email,
            HasTechnicalEmail = isTechnicalEmail,
            PhoneNumber = employee.PhoneNumber,
            IsActive = employee.IsActive,
            HasUserAccount = employee.UserId.HasValue,
            CreatedAt = employee.CreatedAt,
            Kind = employee.Kind,
            ContractType = employee.ContractType,
            AgencyName = employee.AgencyName,
            HourlyRate = employee.HourlyRate,
            ExternalNotes = employee.ExternalNotes,
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
