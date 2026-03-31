using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Interfaccia per la gestione dei merchant
/// </summary>
public interface IMerchantService
{
    /// <summary>
    /// Recupera tutti i merchant
    /// </summary>
    Task<List<MerchantDto>> GetAllAsync();

    /// <summary>
    /// Recupera i merchant in attesa di approvazione
    /// </summary>
    Task<List<MerchantDto>> GetPendingAsync();

    /// <summary>
    /// Recupera un merchant per ID
    /// </summary>
    Task<MerchantDto?> GetByIdAsync(int id);

    /// <summary>
    /// Approva un merchant
    /// </summary>
    Task<bool> ApproveAsync(int id);

    /// <summary>
    /// Rifiuta o disabilita un merchant
    /// </summary>
    Task<bool> RejectAsync(int id);

    /// <summary>
    /// Aggiorna i dati di un merchant
    /// </summary>
    Task<MerchantDto?> UpdateAsync(int id, UpdateMerchantRequest request);
}
