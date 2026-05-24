using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio per la gestione dei ruoli merchant
/// </summary>
public class MerchantRoleService : IMerchantRoleService
{
    private readonly IApplicationDbContext _context;
    private readonly IUtcClock _clock;

    public MerchantRoleService(IApplicationDbContext context, IUtcClock clock)
    {
        _context = context;
        _clock = clock;
    }

    /// <summary>
    /// Recupera tutti i ruoli di un merchant con feature e conteggio membri
    /// </summary>
    public async Task<List<MerchantRoleDto>> GetRolesAsync(int merchantId)
    {
        var roles = await _context.MerchantRoles
            .Include(r => r.Features)
            .Include(r => r.Memberships)
            .Where(r => r.MerchantId == merchantId)
            .OrderBy(r => r.IsDefault ? 0 : 1)
            .ThenBy(r => r.Name)
            .ToListAsync();

        return roles.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Recupera un ruolo per ID, verificando l'appartenenza al merchant
    /// </summary>
    public async Task<MerchantRoleDto?> GetByIdAsync(int id, int merchantId)
    {
        var role = await _context.MerchantRoles
            .Include(r => r.Features)
            .Include(r => r.Memberships)
            .FirstOrDefaultAsync(r => r.Id == id && r.MerchantId == merchantId);

        return role == null ? null : MapToDto(role);
    }

    /// <summary>
    /// Crea un nuovo ruolo per il merchant con le feature specificate
    /// </summary>
    public async Task<MerchantRoleDto> CreateAsync(int merchantId, CreateMerchantRoleRequest request)
    {
        var role = new MerchantRole
        {
            MerchantId = merchantId,
            Name = request.Name,
            IsDefault = false,
            CreatedAt = _clock.UtcNow
        };

        foreach (var featureRequest in request.Features)
        {
            role.Features.Add(new RoleFeature
            {
                Feature = featureRequest.Feature,
                IsEnabled = featureRequest.IsEnabled,
                AccessLevel = ResolveAccessLevel(featureRequest)
            });
        }

        _context.MerchantRoles.Add(role);
        await _context.SaveChangesAsync();

        await _context.Entry(role).Collection(r => r.Features).LoadAsync();
        await _context.Entry(role).Collection(r => r.Memberships).LoadAsync();

        return MapToDto(role);
    }

    /// <summary>
    /// Aggiorna nome e feature di un ruolo esistente
    /// </summary>
    public async Task<MerchantRoleDto?> UpdateAsync(int id, int merchantId, UpdateMerchantRoleRequest request)
    {
        var role = await _context.MerchantRoles
            .Include(r => r.Features)
            .Include(r => r.Memberships)
            .FirstOrDefaultAsync(r => r.Id == id && r.MerchantId == merchantId);

        if (role == null)
            return null;

        role.Name = request.Name;

        // Replace features
        _context.RoleFeatures.RemoveRange(role.Features);
        role.Features.Clear();

        foreach (var featureRequest in request.Features)
        {
            role.Features.Add(new RoleFeature
            {
                RoleId = role.Id,
                Feature = featureRequest.Feature,
                IsEnabled = featureRequest.IsEnabled,
                AccessLevel = ResolveAccessLevel(featureRequest)
            });
        }

        await _context.SaveChangesAsync();

        return MapToDto(role);
    }

    /// <summary>
    /// Elimina un ruolo. Non è consentito eliminare ruoli di default o con membri attivi.
    /// </summary>
    public async Task<bool> DeleteAsync(int id, int merchantId)
    {
        var role = await _context.MerchantRoles
            .Include(r => r.Memberships)
            .FirstOrDefaultAsync(r => r.Id == id && r.MerchantId == merchantId);

        if (role == null)
            return false;

        if (role.IsDefault)
            throw new InvalidOperationException("Non è possibile eliminare il ruolo predefinito del merchant.");

        var hasActiveMembers = role.Memberships.Any(m => m.IsActive);
        if (hasActiveMembers)
            throw new InvalidOperationException("Non è possibile eliminare un ruolo con dipendenti assegnati.");

        _context.MerchantRoles.Remove(role);
        await _context.SaveChangesAsync();
        return true;
    }

    private static MerchantRoleDto MapToDto(MerchantRole role)
    {
        return new MerchantRoleDto
        {
            Id = role.Id,
            MerchantId = role.MerchantId,
            Name = role.Name,
            IsDefault = role.IsDefault,
            Features = role.Features.Select(f => new RoleFeatureDto
            {
                Feature = f.Feature,
                IsEnabled = f.IsEnabled,
                AccessLevel = f.AccessLevel
            }).ToList(),
            MemberCount = role.Memberships.Count(m => m.IsActive)
        };
    }

    /// <summary>
    /// Determina il livello di accesso da persistere per una feature.
    /// Il livello è significativo per Calendario, Richieste, Magazzino, Documenti e Timbratura: se la feature è
    /// abilitata senza livello esplicito, il default è ReadOnly. Per le altre
    /// feature il livello resta null.
    /// </summary>
    private static FeatureAccessLevel? ResolveAccessLevel(MerchantFeatureRequest request)
    {
        if (request.Feature != MerchantFeature.Calendario
            && request.Feature != MerchantFeature.Richieste
            && request.Feature != MerchantFeature.Magazzino
            && request.Feature != MerchantFeature.Documenti
            && request.Feature != MerchantFeature.Timbratura)
            return null;
        if (!request.IsEnabled)
            return null;
        return request.AccessLevel ?? FeatureAccessLevel.ReadOnly;
    }
}
