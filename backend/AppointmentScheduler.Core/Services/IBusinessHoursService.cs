using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Service interface for managing business hours and exceptions
/// </summary>
public interface IBusinessHoursService
{
    // Business Hours
    Task<IEnumerable<BusinessHoursDto>> GetServiceBusinessHoursAsync(int serviceId);
    Task<BusinessHoursDto?> GetBusinessHoursByIdAsync(int id);
    Task<BusinessHoursDto> CreateBusinessHoursAsync(int merchantId, CreateBusinessHoursDto dto);
    Task<BusinessHoursDto?> UpdateBusinessHoursAsync(int id, int merchantId, UpdateBusinessHoursDto dto);
    Task<bool> DeleteBusinessHoursAsync(int id, int merchantId);
    Task<IEnumerable<BusinessHoursDto>> SetupWeeklyHoursAsync(int merchantId, int serviceId, List<CreateBusinessHoursDto> weeklyHours);

    // Business Hours Exceptions
    Task<IEnumerable<BusinessHoursExceptionDto>> GetServiceExceptionsAsync(int serviceId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<BusinessHoursExceptionDto?> GetExceptionByIdAsync(int id);
    Task<BusinessHoursExceptionDto> CreateExceptionAsync(int merchantId, CreateBusinessHoursExceptionDto dto);
    Task<BusinessHoursExceptionDto?> UpdateExceptionAsync(int id, int merchantId, UpdateBusinessHoursExceptionDto dto);
    Task<bool> DeleteExceptionAsync(int id, int merchantId);

    // Availability calculation based on business hours
    Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsFromBusinessHoursAsync(int serviceId, DateTime date);
}
