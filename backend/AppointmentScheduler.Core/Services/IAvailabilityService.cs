using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Service interface for managing service availabilities
/// </summary>
public interface IAvailabilityService
{
    /// <summary>
    /// Get all availabilities for a merchant's services
    /// </summary>
    Task<IEnumerable<AvailabilityDto>> GetMerchantAvailabilitiesAsync(int merchantId);

    /// <summary>
    /// Get availabilities for a specific service
    /// </summary>
    Task<IEnumerable<AvailabilityDto>> GetServiceAvailabilitiesAsync(int serviceId);

    /// <summary>
    /// Get a single availability by ID
    /// </summary>
    Task<AvailabilityDto?> GetAvailabilityByIdAsync(int id);

    /// <summary>
    /// Create a new availability for a service
    /// </summary>
    Task<AvailabilityDto> CreateAvailabilityAsync(int merchantId, CreateAvailabilityRequest request);

    /// <summary>
    /// Update an existing availability
    /// </summary>
    Task<AvailabilityDto?> UpdateAvailabilityAsync(int id, int merchantId, UpdateAvailabilityRequest request);

    /// <summary>
    /// Delete an availability
    /// </summary>
    Task<bool> DeleteAvailabilityAsync(int id, int merchantId);

    /// <summary>
    /// Get available time slots for a service on a specific date
    /// </summary>
    Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(int serviceId, DateTime date);

    /// <summary>
    /// Add slots to an availability (for TimeSlot mode)
    /// </summary>
    Task<AvailabilityDto?> AddSlotsToAvailabilityAsync(int availabilityId, int merchantId, List<CreateAvailabilitySlotRequest> slots);
}
