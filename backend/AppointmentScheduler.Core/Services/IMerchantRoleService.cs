using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Interfaccia per la gestione dei ruoli merchant
/// </summary>
public interface IMerchantRoleService
{
    /// <summary>
    /// Recupera tutti i ruoli di un merchant
    /// </summary>
    Task<List<MerchantRoleDto>> GetRolesAsync(int merchantId);

    /// <summary>
    /// Recupera un ruolo per ID, verificando l'appartenenza al merchant
    /// </summary>
    Task<MerchantRoleDto?> GetByIdAsync(int id, int merchantId);

    /// <summary>
    /// Crea un nuovo ruolo per il merchant
    /// </summary>
    Task<MerchantRoleDto> CreateAsync(int merchantId, CreateMerchantRoleRequest request);

    /// <summary>
    /// Aggiorna un ruolo esistente
    /// </summary>
    Task<MerchantRoleDto?> UpdateAsync(int id, int merchantId, UpdateMerchantRoleRequest request);

    /// <summary>
    /// Elimina un ruolo. Non è possibile eliminare ruoli di default o con membri attivi.
    /// </summary>
    Task<bool> DeleteAsync(int id, int merchantId);

    /// <summary>
    /// Assegna un ruolo a un dipendente del merchant
    /// </summary>
    Task<bool> AssignRoleAsync(int merchantId, AssignRoleRequest request);
}
