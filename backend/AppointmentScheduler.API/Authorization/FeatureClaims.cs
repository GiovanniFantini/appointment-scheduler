using System.Security.Claims;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Authorization;

/// <summary>
/// Helper centralizzato per leggere i claim di feature dal JWT.
/// Sostituisce gli helper <c>HasMagazzinoFeature()</c> / <c>HasTimbraturaFeature()</c>
/// che erano duplicati nei controller.
///
/// Due claim distinti, additivi e indipendenti:
///  - <c>Feature</c>      → presenza binaria della feature (es. "Magazzino"). Invariato.
///  - <c>FeatureLevel</c> → livello di accesso nel formato "&lt;Feature&gt;:&lt;Level&gt;"
///                          (es. "Magazzino:Manager"). Emesso solo per le feature
///                          che usano i livelli.
/// </summary>
public static class FeatureClaims
{
    /// <summary>
    /// True se l'utente ha la feature abilitata (claim "Feature").
    /// Equivalente agli helper booleani precedenti — nessun cambio di semantica.
    /// </summary>
    public static bool HasFeature(this ClaimsPrincipal user, MerchantFeature feature)
        => user.FindAll("Feature").Any(c => c.Value == feature.ToString());

    /// <summary>
    /// Livello di accesso dell'utente alla feature, oppure null se la feature
    /// non ha un livello associato nel token.
    /// </summary>
    public static FeatureAccessLevel? GetFeatureLevel(this ClaimsPrincipal user, MerchantFeature feature)
    {
        var prefix = feature.ToString() + ":";
        var claim = user.FindAll("FeatureLevel")
            .FirstOrDefault(c => c.Value.StartsWith(prefix, StringComparison.Ordinal));

        if (claim == null)
            return null;

        var levelText = claim.Value[prefix.Length..];
        return Enum.TryParse<FeatureAccessLevel>(levelText, out var level) ? level : null;
    }

    /// <summary>
    /// True se l'utente ha la feature e un livello di accesso almeno pari a
    /// <paramref name="minimumLevel"/>. Usare nei controller per gli endpoint
    /// operativi: se restituisce false il controller deve rispondere 403 Forbidden.
    /// </summary>
    public static bool RequireFeatureLevel(this ClaimsPrincipal user, MerchantFeature feature, FeatureAccessLevel minimumLevel)
    {
        if (!user.HasFeature(feature))
            return false;

        var level = user.GetFeatureLevel(feature);
        return level.HasValue && level.Value >= minimumLevel;
    }
}
