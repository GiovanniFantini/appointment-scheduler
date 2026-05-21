using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

public interface ISupplierService
{
    Task<List<SupplierDto>> GetSuppliersAsync(int merchantId, bool includeInactive = true);
    Task<SupplierDto?> GetSupplierByIdAsync(int supplierId, int merchantId);
    Task<SupplierDto> CreateSupplierAsync(int merchantId, CreateSupplierRequest request);
    Task<SupplierDto?> UpdateSupplierAsync(int supplierId, int merchantId, UpdateSupplierRequest request);
}