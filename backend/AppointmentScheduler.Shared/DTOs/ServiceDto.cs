using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO per la creazione di un nuovo servizio
/// </summary>
public class CreateServiceRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ServiceType ServiceType { get; set; }
    public decimal? Price { get; set; }
    public int DurationMinutes { get; set; }
    public string? Configuration { get; set; }
}

/// <summary>
/// DTO per l'aggiornamento di un servizio esistente
/// </summary>
public class UpdateServiceRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ServiceType ServiceType { get; set; }
    public decimal? Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public string? Configuration { get; set; }
}

/// <summary>
/// DTO per la risposta con i dati del servizio
/// </summary>
public class ServiceDto
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ServiceType ServiceType { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public string? Configuration { get; set; }
    public DateTime CreatedAt { get; set; }
}
