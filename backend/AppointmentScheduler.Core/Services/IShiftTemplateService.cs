using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Interfaccia per la gestione dei template di turni
/// </summary>
public interface IShiftTemplateService
{
    /// <summary>
    /// Recupera tutti i template di turni di un merchant
    /// </summary>
    /// <param name="merchantId">ID del merchant</param>
    /// <returns>Lista di template turni del merchant</returns>
    Task<IEnumerable<ShiftTemplateDto>> GetMerchantShiftTemplatesAsync(int merchantId);

    /// <summary>
    /// Recupera un template turno per ID
    /// </summary>
    /// <param name="id">ID del template</param>
    /// <returns>Dati del template o null se non trovato</returns>
    Task<ShiftTemplateDto?> GetShiftTemplateByIdAsync(int id);

    /// <summary>
    /// Crea un nuovo template turno
    /// </summary>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <param name="request">Dati del template da creare</param>
    /// <returns>Template creato</returns>
    Task<ShiftTemplateDto> CreateShiftTemplateAsync(int merchantId, CreateShiftTemplateRequest request);

    /// <summary>
    /// Aggiorna un template turno esistente
    /// </summary>
    /// <param name="templateId">ID del template da aggiornare</param>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <param name="request">Nuovi dati del template</param>
    /// <returns>Template aggiornato o null se non trovato o non autorizzato</returns>
    Task<ShiftTemplateDto?> UpdateShiftTemplateAsync(int templateId, int merchantId, UpdateShiftTemplateRequest request);

    /// <summary>
    /// Elimina un template turno
    /// </summary>
    /// <param name="templateId">ID del template da eliminare</param>
    /// <param name="merchantId">ID del merchant proprietario</param>
    /// <returns>True se eliminato con successo</returns>
    Task<bool> DeleteShiftTemplateAsync(int templateId, int merchantId);
}
