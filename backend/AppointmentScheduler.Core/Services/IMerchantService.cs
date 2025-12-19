using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Interfaccia per la gestione dei merchant
/// </summary>
public interface IMerchantService
{
    /// <summary>
    /// Recupera tutti i merchant in attesa di approvazione
    /// </summary>
    /// <returns>Lista di merchant non approvati</returns>
    Task<IEnumerable<MerchantDto>> GetPendingMerchantsAsync();

    /// <summary>
    /// Recupera tutti i merchant
    /// </summary>
    /// <returns>Lista di tutti i merchant</returns>
    Task<IEnumerable<MerchantDto>> GetAllMerchantsAsync();

    /// <summary>
    /// Recupera un merchant per ID
    /// </summary>
    /// <param name="id">ID del merchant</param>
    /// <returns>Dati del merchant o null se non trovato</returns>
    Task<MerchantDto?> GetMerchantByIdAsync(int id);

    /// <summary>
    /// Approva un merchant
    /// </summary>
    /// <param name="merchantId">ID del merchant da approvare</param>
    /// <returns>True se approvato con successo</returns>
    Task<bool> ApproveMerchantAsync(int merchantId);

    /// <summary>
    /// Rifiuta o disabilita un merchant
    /// </summary>
    /// <param name="merchantId">ID del merchant da rifiutare</param>
    /// <returns>True se operazione completata con successo</returns>
    Task<bool> RejectMerchantAsync(int merchantId);
}
