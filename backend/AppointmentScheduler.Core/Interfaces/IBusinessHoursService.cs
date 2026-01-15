using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Interfaces;

public interface IBusinessHoursService
{
    Task<IEnumerable<BusinessHoursDto>> GetMerchantBusinessHoursAsync(int merchantId);
    Task<BusinessHoursDto?> GetBusinessHoursByIdAsync(int id);
    Task<BusinessHoursDto> CreateBusinessHoursAsync(int merchantId, CreateBusinessHoursDto dto);
    Task<BusinessHoursDto> UpdateBusinessHoursAsync(int id, int merchantId, UpdateBusinessHoursDto dto);
    Task DeleteBusinessHoursAsync(int id, int merchantId);
    Task<IEnumerable<BusinessHoursDto>> SetupDefaultWeekAsync(int merchantId, CreateBusinessHoursDto[] weekDays);
}
