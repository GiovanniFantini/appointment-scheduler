using System.Runtime.InteropServices;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Fuso orario "di parete" usato per la timbratura. Gli orari di turno
/// (<c>TimeOnly</c>) sono espressi nell'ora locale dell'azienda; per confrontarli
/// con l'istante reale (salvato in UTC) serve sapere questo fuso.
///
/// Default: <c>Europe/Rome</c>. <see cref="TimeZoneInfo"/> applica ora legale e
/// solare in automatico, quindi non serve alcuna configurazione manuale nel caso
/// comune. Per un deploy fuori dall'Italia basta valorizzare <see cref="Id"/> in
/// appsettings con un id IANA (es. <c>Europe/Madrid</c>) o Windows.
/// </summary>
public sealed class TimeClockTimeZoneOptions
{
    public const string DefaultId = "Europe/Rome";

    /// <summary>Id del fuso (IANA o Windows). Vuoto = default italiano.</summary>
    public string? Id { get; set; }

    /// <summary>
    /// Risolve l'<see cref="TimeZoneInfo"/> tollerando il formato del SO: su .NET 8
    /// gli id IANA funzionano anche su Windows via ICU, ma se la risoluzione
    /// fallisce si ripiega sull'id Windows equivalente (e viceversa). Se nemmeno
    /// quello esiste si usa l'ora italiana come ultima spiaggia, mai UTC: meglio
    /// un fuso plausibile che deviazioni di 1-2 ore silenziose.
    /// </summary>
    public TimeZoneInfo Resolve()
    {
        var id = string.IsNullOrWhiteSpace(Id) ? DefaultId : Id.Trim();

        if (TryFind(id, out var tz)) return tz;

        // Prova a tradurre tra IANA e Windows a seconda di cosa supporta il SO.
        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var winId)
            && TryFind(winId, out tz)) return tz;
        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out var ianaId)
            && TryFind(ianaId, out tz)) return tz;

        // Ultima spiaggia: l'equivalente italiano nel formato del SO corrente.
        var fallback = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "W. Europe Standard Time"
            : DefaultId;
        return TimeZoneInfo.FindSystemTimeZoneById(fallback);
    }

    private static bool TryFind(string id, out TimeZoneInfo tz)
    {
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            tz = TimeZoneInfo.Utc;
            return false;
        }
    }
}
