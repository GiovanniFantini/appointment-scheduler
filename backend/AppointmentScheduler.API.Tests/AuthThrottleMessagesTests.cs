using AppointmentScheduler.API.Authorization;

namespace AppointmentScheduler.API.Tests;

public class AuthThrottleMessagesTests
{
    [Theory]
    [InlineData(0, "Operazione non disponibile al momento. Riprova tra qualche minuto.")]
    [InlineData(-5, "Operazione non disponibile al momento. Riprova tra qualche minuto.")]
    [InlineData(1, "Operazione non disponibile al momento. Riprova tra circa 1 minuto.")]
    [InlineData(2, "Operazione non disponibile al momento. Riprova tra circa 2 minuti.")]
    [InlineData(15, "Operazione non disponibile al momento. Riprova tra circa 15 minuti.")]
    public void GenericRetry_ReturnsExpectedCopy(int minutes, string expected)
    {
        AuthThrottleMessages.GenericRetry(minutes).Should().Be(expected);
    }

    [Fact]
    public void GenericRetry_NeverLeaksThrottlingTerminology()
    {
        // Il vero invariante: nessuna parola che permetta a un bot di capire
        // QUALE soglia ha colpito (IP rate-limit, lockout, ecc.). La copy deve
        // restare neutra a prescindere dal parametro.
        var forbiddenTerms = new[] { "rate", "limit", "blocco", "lockout", "tentativi", "account" };
        foreach (var m in new[] { -1, 0, 1, 5, 30, 240 })
        {
            var msg = AuthThrottleMessages.GenericRetry(m).ToLowerInvariant();
            foreach (var term in forbiddenTerms)
                msg.Should().NotContain(term, $"il messaggio per {m} minuti non deve contenere '{term}'");
        }
    }

    [Theory]
    [InlineData(0, 0)]              // durata nulla → 0 minuti
    [InlineData(-30, 0)]            // durata negativa → 0 minuti
    [InlineData(1, 1)]              // 1s → 1 minuto (arrotondamento verso l'alto)
    [InlineData(59, 1)]             // 59s → 1 minuto
    [InlineData(60, 1)]             // esattamente 1min → 1
    [InlineData(61, 2)]             // 1min 1s → 2 (sempre verso l'alto)
    [InlineData(120, 2)]            // 2min → 2
    [InlineData(125, 3)]            // 2min 5s → 3
    public void CeilToMinutes_RoundsUp(int seconds, int expectedMinutes)
    {
        AuthThrottleMessages.CeilToMinutes(TimeSpan.FromSeconds(seconds)).Should().Be(expectedMinutes);
    }
}
