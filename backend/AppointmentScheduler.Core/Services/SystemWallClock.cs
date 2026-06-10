using AppointmentScheduler.Core.Interfaces;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// "Adesso" come ora di parete dell'azienda. Deriva da <see cref="DateTime.UtcNow"/>
/// convertito nel fuso configurato (default <c>Europe/Rome</c>), così la deviazione
/// rispetto agli orari di turno è corretta a prescindere dal fuso del server — che
/// in produzione è tipicamente UTC. Restituisce un <see cref="DateTime"/> con
/// <c>Kind=Unspecified</c>, da confrontare solo con altri valori wall-clock.
/// </summary>
public sealed class SystemWallClock : IWallClock
{
    private readonly TimeZoneInfo _timeZone;

    public SystemWallClock(TimeClockTimeZoneOptions options)
    {
        _timeZone = options.Resolve();
    }

    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
}
