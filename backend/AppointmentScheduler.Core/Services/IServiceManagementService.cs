using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Interfaccia per la gestione dei servizi offerti dai merchant
/// </summary>
public interface IServiceManagementService
{
    /// <summary>
    /// Recupera tutti i servizi di un merchant
    /// </summary>
    /// <param name="merchantId">ID del merchant</param>
    /// <returns>Lista di servizi del merchant</returns>
    Task<IEnumerable<ServiceDto>> GetMerchantServicesAsync(int merchantId);

    /// <summary>
    /// Recupera tutti i servizi attivi per gli utenti
    /// </summary>
    /// <param name="serviceType">Filtro opzionale per tipo di servizio</param>
    /// <returns>Lista di servizi attivi</returns>
    Task<IEnumerable<ServiceDto>> GetActiveServicesAsync(int? serviceType = null);

    /// <summary>
    /// Recupera un servizio per ID
    /// </summary>
    /// <param name="id">ID del servizio</param>
    /// <returns>Dati del servizio o null se non trovato</returns>
    Task<ServiceDto?> GetServiceByIdAsync(int id);

    /// <summary>
    /// Crea un nuovo servizio
    /// </summary>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <param name="request">Dati del servizio da creare</param>
    /// <returns>Servizio creato</returns>
    Task<ServiceDto> CreateServiceAsync(int merchantId, CreateServiceRequest request);

    /// <summary>
    /// Aggiorna un servizio esistente
    /// </summary>
    /// <param name="serviceId">ID del servizio da aggiornare</param>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <param name="request">Nuovi dati del servizio</param>
    /// <returns>Servizio aggiornato o null se non trovato o non autorizzato</returns>
    Task<ServiceDto?> UpdateServiceAsync(int serviceId, int merchantId, UpdateServiceRequest request);

    /// <summary>
    /// Elimina un servizio
    /// </summary>
    /// <param name="serviceId">ID del servizio da eliminare</param>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <returns>True se eliminato con successo</returns>
    Task<bool> DeleteServiceAsync(int serviceId, int merchantId);
}
