namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// Request per il check-out da un turno
/// </summary>
public class CheckOutRequest
{
    /// <summary>
    /// ID del turno
    /// </summary>
    public int ShiftId { get; set; }

    /// <summary>
    /// Posizione GPS al check-out (opzionale, formato JSON: {lat, lng})
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Note opzionali
    /// </summary>
    public string? Notes { get; set; }
}
