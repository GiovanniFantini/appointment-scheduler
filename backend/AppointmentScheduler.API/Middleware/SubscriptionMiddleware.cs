using System.Security.Claims;
using AppointmentScheduler.API.Authorization;
using AppointmentScheduler.Shared.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace AppointmentScheduler.API.Middleware;

public sealed class SubscriptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, SubscriptionAccess access)
    {
        var endpoint = context.GetEndpoint();
        // Le rotte pubbliche e quelle inesistenti non dipendono dal pacchetto dell'utente eventualmente autenticato.
        var publicRoute = endpoint == null || endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null
            || endpoint.Metadata.GetMetadata<IAuthorizeData>() == null && endpoint.Metadata.GetMetadata<RequiresPlanFeatureAttribute>() == null;
        var authenticated = context.User.Identity?.IsAuthenticated == true;
        var path = context.Request.Path;
        var accountRoute = path.StartsWithSegments("/api/subscription") || path.StartsWithSegments("/api/auth")
            || path.StartsWithSegments("/api/account") || path.Equals(new PathString("/api/merchants/me"));
        if (authenticated && !context.User.IsInRole("Admin") && !accountRoute
            && !publicRoute
            && !int.TryParse(context.User.FindFirstValue("MerchantId"), out _))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { code = "COMPANY_REQUIRED", message = "Seleziona un’azienda." });
            return;
        }
        if (authenticated && !context.User.IsInRole("Admin")
            && !publicRoute
            && int.TryParse(context.User.FindFirstValue("MerchantId"), out var merchantId))
        {
            var state = await access.ResolveAsync(merchantId, context.User, context.RequestAborted);
            context.Items[typeof(Shared.DTOs.SubscriptionAccessDto)] = state;
            // I claim nel JWT possono precedere un cambio pacchetto o ruolo: non sono la fonte corrente dei diritti.
            foreach (var identity in context.User.Identities)
                foreach (var claim in identity.FindAll("Feature").Concat(identity.FindAll("FeatureLevel")).ToArray())
                    identity.RemoveClaim(claim);
            var currentIdentity = (ClaimsIdentity)context.User.Identity!;
            currentIdentity.AddClaims(state.ActiveFeatures.Select(f => new Claim("Feature", f)));
            currentIdentity.AddClaims(state.FeatureLevels.Select(f => new Claim("FeatureLevel", $"{f.Key}:{f.Value}")));

            if (!accountRoute && state.Status is not ("Active" or "Trial"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { code = "SUBSCRIPTION_" + state.Status.ToUpperInvariant(),
                    message = state.Status == "Expired" ? "Prova terminata. Contatta l’amministratore." : "Pacchetto non attivo. Contatta l’amministratore." });
                return;
            }
            var feature = endpoint?.Metadata.GetMetadata<RequiresPlanFeatureAttribute>();
            if (feature != null && !state.ActiveFeatures.Contains(feature.Feature.ToString()))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { code = "FEATURE_DISABLED", message = "Funzione non disponibile." });
                return;
            }
        }
        try
        {
            await next(context);
        }
        catch (SubscriptionLimitException ex) when (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new { code = "SUBSCRIPTION_LIMIT", message = ex.Message });
        }
    }
}
