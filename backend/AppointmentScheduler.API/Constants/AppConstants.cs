// =============================================================================
// APPOINTMENT SCHEDULER - BACKEND CONSTANTS
// =============================================================================
// Costanti centralizzate per evitare valori hardcodati nel codice

namespace AppointmentScheduler.API.Constants;

/// <summary>
/// Costanti relative all'autenticazione e autorizzazione
/// </summary>
public static class AuthConstants
{
    /// <summary>
    /// Nomi delle policy di autorizzazione
    /// </summary>
    public static class Policies
    {
        public const string AdminOnly = "AdminOnly";
        public const string MerchantOnly = "MerchantOnly";
        public const string UserOnly = "UserOnly";
    }

    /// <summary>
    /// Ruoli utente
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Merchant = "Merchant";
        public const string User = "User";
    }

    /// <summary>
    /// Claim types personalizzati
    /// </summary>
    public static class Claims
    {
        public const string Role = "role";
        public const string UserId = "userId";
        public const string Email = "email";
    }
}

/// <summary>
/// Costanti relative alle configurazioni
/// </summary>
public static class ConfigKeys
{
    /// <summary>
    /// Chiavi per le connection strings
    /// </summary>
    public static class ConnectionStrings
    {
        public const string DefaultConnection = "DefaultConnection";
    }

    /// <summary>
    /// Chiavi per la configurazione JWT
    /// </summary>
    public static class Jwt
    {
        public const string SectionName = "Jwt";
        public const string SecretKey = "SecretKey";
        public const string Issuer = "Issuer";
        public const string Audience = "Audience";
        public const string ExpirationMinutes = "ExpirationMinutes";
    }

    /// <summary>
    /// Chiavi per la configurazione CORS
    /// </summary>
    public static class Cors
    {
        public const string PolicyName = "AllowedOrigins";
        public const string AllowedOrigins = "AllowedOrigins";
    }

    /// <summary>
    /// Chiavi per le variabili d'ambiente
    /// </summary>
    public static class Environment
    {
        // Database
        public const string PostgresConnectionString = "POSTGRESQLCONNSTR_DefaultConnection";

        // JWT
        public const string JwtSecretKey = "JWT_SECRET_KEY";
        public const string JwtIssuer = "JWT_ISSUER";
        public const string JwtAudience = "JWT_AUDIENCE";
        public const string JwtExpirationMinutes = "JWT_EXPIRATION_MINUTES";

        // Admin Default
        public const string AdminDefaultEmail = "ADMIN_DEFAULT_EMAIL";
        public const string AdminDefaultPassword = "ADMIN_DEFAULT_PASSWORD";
        public const string AdminDefaultFirstName = "ADMIN_DEFAULT_FIRSTNAME";
        public const string AdminDefaultLastName = "ADMIN_DEFAULT_LASTNAME";

        // CORS
        public const string CorsOrigins = "CORS_ORIGINS";

        // Logging
        public const string LogLevelDefault = "LOGGING_LOGLEVEL_DEFAULT";
        public const string LogLevelAspNetCore = "LOGGING_LOGLEVEL_MICROSOFT_ASPNETCORE";
    }
}

/// <summary>
/// Costanti relative agli endpoint API
/// </summary>
public static class ApiEndpoints
{
    public const string BaseRoute = "api/[controller]";

    /// <summary>
    /// Endpoint per gli health checks
    /// </summary>
    public static class HealthChecks
    {
        public const string Health = "/health";
        public const string Ready = "/health/ready";
        public const string Live = "/health/live";
    }

    /// <summary>
    /// Endpoint Swagger
    /// </summary>
    public static class Swagger
    {
        public const string JsonEndpoint = "/swagger/v1/swagger.json";
        public const string RoutePrefix = "swagger";
        public const string Title = "Appointment Scheduler API";
        public const string Version = "v1";
    }
}

/// <summary>
/// Costanti relative al database
/// </summary>
public static class DatabaseConstants
{
    /// <summary>
    /// Lunghezze massime dei campi
    /// </summary>
    public static class MaxLengths
    {
        public const int Email = 256;
        public const int FirstName = 100;
        public const int LastName = 100;
        public const int BusinessName = 200;
        public const int ServiceName = 200;
        public const int Address = 500;
        public const int Description = 1000;
    }

    /// <summary>
    /// Precisione per campi numerici
    /// </summary>
    public static class Precision
    {
        public const string Price = "decimal(18,2)";
        public const int PricePrecision = 18;
        public const int PriceScale = 2;
    }

    /// <summary>
    /// Timeout per i comandi database (in secondi)
    /// </summary>
    public const int CommandTimeout = 30;
}

/// <summary>
/// Costanti relative alle validazioni
/// </summary>
public static class ValidationConstants
{
    public const int MinPasswordLength = 6;
    public const int MaxLoginAttempts = 5;
    public const int AccountLockoutMinutes = 15;
}

/// <summary>
/// Costanti relative ai servizi
/// </summary>
public static class ServiceConstants
{
    /// <summary>
    /// Tipi di servizio
    /// </summary>
    public enum ServiceType
    {
        Restaurant = 1,
        Sport = 2,
        Health = 3,
        Beauty = 4,
        Other = 5
    }

    /// <summary>
    /// Modi di prenotazione
    /// </summary>
    public enum BookingMode
    {
        TimeSlot = 1,
        WholeDay = 2
    }

    /// <summary>
    /// Valori di default per i servizi
    /// </summary>
    public const int DefaultDuration = 60; // minuti
    public const int DefaultMaxCapacity = int.MaxValue;
    public const int MinCapacity = 1;
}

/// <summary>
/// Costanti relative alle prenotazioni
/// </summary>
public static class BookingConstants
{
    /// <summary>
    /// Stati delle prenotazioni
    /// </summary>
    public enum BookingStatus
    {
        Pending = 0,
        Confirmed = 1,
        Cancelled = 2,
        Completed = 3
    }

    /// <summary>
    /// Nomi degli stati per l'UI
    /// </summary>
    public static readonly Dictionary<BookingStatus, string> StatusNames = new()
    {
        { BookingStatus.Pending, "In attesa" },
        { BookingStatus.Confirmed, "Confermata" },
        { BookingStatus.Cancelled, "Cancellata" },
        { BookingStatus.Completed, "Completata" }
    };

    public const int MinPeoplePerBooking = 1;
    public const int MaxPeoplePerBooking = 100;
}

/// <summary>
/// Costanti relative ai messaggi di errore
/// </summary>
public static class ErrorMessages
{
    public const string InvalidEmailOrPassword = "Email o password non validi";
    public const string EmailAlreadyRegistered = "Email già registrata";
    public const string UserNotFound = "Utente non trovato";
    public const string ServiceNotFound = "Servizio non trovato";
    public const string BookingNotFound = "Prenotazione non trovata";
    public const string Unauthorized = "Non autorizzato";
    public const string InsufficientCapacity = "Capacità insufficiente";
    public const string InvalidTimeSlot = "Fascia oraria non valida";
}

/// <summary>
/// Costanti relative ai valori di default
/// </summary>
public static class Defaults
{
    /// <summary>
    /// Credenziali admin di default
    /// </summary>
    public static class Admin
    {
        public const string Email = "admin@admin.com";
        public const string Password = "password";
        public const string FirstName = "Admin";
        public const string LastName = "User";
    }

    /// <summary>
    /// Configurazione JWT di default
    /// </summary>
    public static class Jwt
    {
        public const string Issuer = "AppointmentScheduler.API";
        public const string Audience = "AppointmentScheduler.Client";
        public const int ExpirationMinutes = 1440; // 24 ore
    }
}
