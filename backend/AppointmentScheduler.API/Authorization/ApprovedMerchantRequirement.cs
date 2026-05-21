using Microsoft.AspNetCore.Authorization;

namespace AppointmentScheduler.API.Authorization;

/// <summary>
/// Requisito della policy <c>ApprovedMerchantOnly</c>: l'azienda dietro a un
/// account Merchant deve essere stata approvata dall'admin.
///
/// Regola applicata dall'<see cref="ApprovedMerchantHandler"/>:
///  - Admin            → sempre consentito (gestisce qualsiasi merchant).
///  - Merchant         → consentito solo se il token contiene il claim
///                       <c>MerchantApproved=true</c> (emesso da AuthService
///                       solo quando <c>Merchant.IsApproved</c> è true).
///  - Altri ruoli      → negato.
///
/// Un merchant non approvato può autenticarsi e ottenere un token, ma quel
/// token non contiene il claim: le rotte operative gated da questa policy
/// rispondono 403 finché l'admin non approva e l'utente non rifà login.
/// </summary>
public sealed class ApprovedMerchantRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Handler di <see cref="ApprovedMerchantRequirement"/>. Vedi il requirement
/// per la regola di autorizzazione.
/// </summary>
public sealed class ApprovedMerchantHandler : AuthorizationHandler<ApprovedMerchantRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, ApprovedMerchantRequirement requirement)
    {
        // L'admin opera su qualsiasi merchant, a prescindere dall'approvazione.
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Per i merchant è richiesto il claim di approvazione.
        var isMerchant = context.User.IsInRole("Merchant");
        var isApproved = context.User.HasClaim("MerchantApproved", "true");
        if (isMerchant && isApproved)
            context.Succeed(requirement);

        // Negli altri casi non chiamiamo Fail(): la policy non viene soddisfatta
        // e il framework risponde 403 (l'utente è autenticato ma non autorizzato).
        return Task.CompletedTask;
    }
}
