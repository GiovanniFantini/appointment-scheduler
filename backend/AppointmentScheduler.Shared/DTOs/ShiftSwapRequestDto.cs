using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO per la creazione di una richiesta di scambio turno
/// </summary>
public class CreateShiftSwapRequest
{
    public int ShiftId { get; set; }
    public int? TargetEmployeeId { get; set; }
    public int? OfferedShiftId { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// DTO per rispondere a una richiesta di scambio turno
/// </summary>
public class RespondToShiftSwapRequest
{
    public ShiftSwapStatus Status { get; set; }
    public string? ResponseMessage { get; set; }
}

/// <summary>
/// DTO per la risposta con i dati della richiesta di scambio turno
/// </summary>
public class ShiftSwapRequestDto
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public ShiftDto Shift { get; set; } = null!;
    public int RequestingEmployeeId { get; set; }
    public string RequestingEmployeeName { get; set; } = string.Empty;
    public int? TargetEmployeeId { get; set; }
    public string? TargetEmployeeName { get; set; }
    public int? OfferedShiftId { get; set; }
    public ShiftDto? OfferedShift { get; set; }
    public ShiftSwapStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? ResponseMessage { get; set; }
    public bool RequiresMerchantApproval { get; set; }
    public DateTime? MerchantResponseAt { get; set; }
    public int? ApprovedByMerchantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
