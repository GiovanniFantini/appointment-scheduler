namespace AppointmentScheduler.Shared.DTOs;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // Multi-role system: un utente può avere multipli ruoli
    public List<string> Roles { get; set; } = new List<string>();

    // Flags per accesso rapido (backward compatibility)
    public bool IsAdmin { get; set; }
    public bool IsConsumer { get; set; }
    public bool IsMerchant { get; set; }
    public bool IsEmployee { get; set; }

    public int? MerchantId { get; set; } // Se è un merchant (one-to-one)
    // Note: EmployeeId rimosso perché un employee può lavorare per multipli merchant
    // Frontend employee-app userà un endpoint dedicato per ottenere la lista merchant
}
