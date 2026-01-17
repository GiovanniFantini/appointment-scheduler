namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// Request per il check-in su un turno
/// </summary>
public class CheckInRequest
{
    /// <summary>
    /// ID del turno
    /// </summary>
    public int ShiftId { get; set; }

    /// <summary>
    /// Posizione GPS al check-in (opzionale, formato JSON: {lat, lng})
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Note opzionali
    /// </summary>
    public string? Notes { get; set; }
}
