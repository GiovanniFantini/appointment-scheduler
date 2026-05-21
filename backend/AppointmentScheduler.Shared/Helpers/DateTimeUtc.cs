namespace AppointmentScheduler.Shared.Helpers;

/// <summary>
/// Helper per garantire che i <see cref="DateTime"/> destinati a colonne
/// PostgreSQL 'timestamp with time zone' abbiano sempre Kind=Utc.
/// Npgsql rifiuta valori con Kind=Unspecified o Local.
/// </summary>
public static class DateTimeUtc
{
    /// <summary>
    /// Coerce un DateTime arrivato dall'esterno (request del client) a Kind=Utc.
    /// I valori deserializzati da JSON senza offset hanno Kind=Unspecified e
    /// vengono interpretati come già espressi in UTC; i valori Local vengono convertiti.
    /// </summary>
    public static DateTime Coerce(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    /// <inheritdoc cref="Coerce(DateTime)"/>
    public static DateTime? Coerce(DateTime? value)
        => value.HasValue ? Coerce(value.Value) : null;
}
