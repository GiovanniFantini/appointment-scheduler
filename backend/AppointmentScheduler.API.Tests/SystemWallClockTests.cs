using AppointmentScheduler.Core.Services;

namespace AppointmentScheduler.API.Tests;

public class SystemWallClockTests
{
    [Fact]
    public void Resolve_DefaultsToItalianTimeZone_WhenIdIsEmpty()
    {
        var tz = new TimeClockTimeZoneOptions().Resolve();

        // L'Italia è UTC+1 in inverno, UTC+2 in estate (ora legale).
        var winter = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var summer = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        tz.GetUtcOffset(winter).Should().Be(TimeSpan.FromHours(1));
        tz.GetUtcOffset(summer).Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void Resolve_AcceptsWindowsIdForItaly()
    {
        var tz = new TimeClockTimeZoneOptions { Id = "W. Europe Standard Time" }.Resolve();

        var summer = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        tz.GetUtcOffset(summer).Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void Now_ConvertsUtcToItalianWallClock_AndAppliesDst()
    {
        var clock = new SystemWallClock(new TimeClockTimeZoneOptions());

        var beforeUtc = DateTime.UtcNow;
        var wall = clock.Now;
        var afterUtc = DateTime.UtcNow;

        // In estate l'ora di parete italiana è UTC+2: il fix che elimina il bug
        // dei -107 minuti. Verifichiamo che lo scarto da UTC sia 1h o 2h (DST),
        // mai zero come faceva DateTime.Now su un server UTC.
        var offsetFromUtc = wall - afterUtc;
        var isItalianOffset =
            (offsetFromUtc >= TimeSpan.FromMinutes(59) && offsetFromUtc <= TimeSpan.FromMinutes(61)) ||
            (offsetFromUtc >= TimeSpan.FromMinutes(119) && offsetFromUtc <= TimeSpan.FromMinutes(121));

        isItalianOffset.Should().BeTrue(
            "l'ora di parete italiana deve essere UTC+1 o UTC+2, non l'ora del processo");
        wall.Should().BeOnOrAfter(beforeUtc.AddHours(1).AddSeconds(-5));
    }
}
