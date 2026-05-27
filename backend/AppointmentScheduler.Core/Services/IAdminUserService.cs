using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio sys-admin per la gestione globale degli utenti della piattaforma.
/// Tutti i metodi presuppongono autorizzazione AdminOnly a monte (controller).
/// </summary>
public interface IAdminUserService
{
    /// <summary>Lista paginata degli utenti con filtri opzionali.</summary>
    Task<AdminUserListResponse> GetUsersAsync(AdminUserListQuery query);

    /// <summary>Dettaglio utente; null se non trovato.</summary>
    Task<AdminUserDetailDto?> GetByIdAsync(int id);

    /// <summary>Imposta IsActive=true. Ritorna false se utente non trovato.</summary>
    Task<bool> ActivateAsync(int id);

    /// <summary>
    /// Imposta IsActive=false. Ritorna esito; non consente self-deactivate
    /// (il chiamante deve passare il proprio id corrente e gestire il caso).
    /// </summary>
    Task<DeactivateResult> DeactivateAsync(int id, int currentAdminUserId);
}

public enum DeactivateResult
{
    Success,
    NotFound,
    CannotDeactivateSelf
}
