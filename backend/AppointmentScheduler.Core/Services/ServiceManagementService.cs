using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio per la gestione dei servizi offerti dai merchant
/// </summary>
public class ServiceManagementService : IServiceManagementService
{
    private readonly ApplicationDbContext _context;

    public ServiceManagementService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Recupera tutti i servizi di un merchant
    /// </summary>
    public async Task<IEnumerable<ServiceDto>> GetMerchantServicesAsync(int merchantId)
    {
        var services = await _context.Services
            .Include(s => s.Merchant)
            .Where(s => s.MerchantId == merchantId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return services.Select(MapToDto);
    }

    /// <summary>
    /// Recupera tutti i servizi attivi per gli utenti
    /// </summary>
    public async Task<IEnumerable<ServiceDto>> GetActiveServicesAsync(int? serviceType = null)
    {
        var query = _context.Services
            .Include(s => s.Merchant)
            .Where(s => s.IsActive && s.Merchant.IsApproved);

        if (serviceType.HasValue)
        {
            query = query.Where(s => (int)s.ServiceType == serviceType.Value);
        }

        var services = await query
            .OrderBy(s => s.Name)
            .ToListAsync();

        return services.Select(MapToDto);
    }

    /// <summary>
    /// Recupera un servizio per ID
    /// </summary>
    public async Task<ServiceDto?> GetServiceByIdAsync(int id)
    {
        var service = await _context.Services
            .Include(s => s.Merchant)
            .FirstOrDefaultAsync(s => s.Id == id);

        return service == null ? null : MapToDto(service);
    }

    /// <summary>
    /// Crea un nuovo servizio
    /// </summary>
    public async Task<ServiceDto> CreateServiceAsync(int merchantId, CreateServiceRequest request)
    {
        var service = new Service
        {
            MerchantId = merchantId,
            Name = request.Name,
            Description = request.Description,
            ServiceType = request.ServiceType,
            Price = request.Price,
            DurationMinutes = request.DurationMinutes,
            Configuration = request.Configuration,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        await _context.Entry(service).Reference(s => s.Merchant).LoadAsync();

        return MapToDto(service);
    }

    /// <summary>
    /// Aggiorna un servizio esistente
    /// </summary>
    public async Task<ServiceDto?> UpdateServiceAsync(int serviceId, int merchantId, UpdateServiceRequest request)
    {
        var service = await _context.Services
            .Include(s => s.Merchant)
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.MerchantId == merchantId);

        if (service == null)
            return null;

        service.Name = request.Name;
        service.Description = request.Description;
        service.ServiceType = request.ServiceType;
        service.Price = request.Price;
        service.DurationMinutes = request.DurationMinutes;
        service.IsActive = request.IsActive;
        service.Configuration = request.Configuration;

        await _context.SaveChangesAsync();

        return MapToDto(service);
    }

    /// <summary>
    /// Elimina un servizio
    /// </summary>
    public async Task<bool> DeleteServiceAsync(int serviceId, int merchantId)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.MerchantId == merchantId);

        if (service == null)
            return false;

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();

        return true;
    }

    private ServiceDto MapToDto(Service service)
    {
        return new ServiceDto
        {
            Id = service.Id,
            MerchantId = service.MerchantId,
            MerchantName = service.Merchant.BusinessName,
            Name = service.Name,
            Description = service.Description,
            ServiceType = service.ServiceType,
            ServiceTypeName = service.ServiceType.ToString(),
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            IsActive = service.IsActive,
            Configuration = service.Configuration,
            CreatedAt = service.CreatedAt
        };
    }
}
