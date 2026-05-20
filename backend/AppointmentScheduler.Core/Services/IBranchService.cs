using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Gestione di filiali (MerchantBranch) e reparti (Department) di un merchant.
/// </summary>
public interface IBranchService
{
    // ── Filiali ────────────────────────────────────────────────────────────
    Task<List<MerchantBranchDto>> GetBranchesAsync(int merchantId, bool includeInactive = true);
    Task<MerchantBranchDto?> GetBranchByIdAsync(int branchId, int merchantId);
    Task<MerchantBranchDto> CreateBranchAsync(int merchantId, CreateBranchRequest request);
    Task<MerchantBranchDto?> UpdateBranchAsync(int branchId, int merchantId, UpdateBranchRequest request);
    Task<bool> DeleteBranchAsync(int branchId, int merchantId);

    /// <summary>Promuove una filiale a sede principale (sgancia la HQ precedente).</summary>
    Task<bool> SetHeadquartersAsync(int branchId, int merchantId);

    // ── Reparti ────────────────────────────────────────────────────────────
    Task<DepartmentDto> CreateDepartmentAsync(int branchId, int merchantId, CreateDepartmentRequest request);
    Task<DepartmentDto?> UpdateDepartmentAsync(int departmentId, int merchantId, UpdateDepartmentRequest request);
    Task<bool> DeleteDepartmentAsync(int departmentId, int merchantId);
}
