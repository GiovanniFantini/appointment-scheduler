namespace AppointmentScheduler.API.Middleware;

/// <summary>
/// Aggiunge gli header di sicurezza standard a tutte le risposte HTTP.
///
/// Gli header vengono impostati su <c>OnStarting</c> in modo che siano presenti
/// anche per risposte generate dopo il middleware (controller, RateLimiter,
/// auth challenge): se li scrivessimo subito qui un endpoint downstream
/// potrebbe sovrascriverli.
///
/// Strict-Transport-Security viene emesso solo in produzione: in sviluppo il
/// dev server gira su http e l'header rifiuterebbe i futuri caricamenti.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _includeHsts;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _includeHsts = !env.IsDevelopment();
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var ctx = (HttpContext)state;
            var headers = ctx.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // CSP minimale: l'API non serve HTML, blocchiamo qualsiasi origine.
            // Se in futuro il backend servirà view, va rilassata di conseguenza.
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

            return Task.CompletedTask;
        }, context);

        if (_includeHsts)
        {
            context.Response.OnStarting(static state =>
            {
                var ctx = (HttpContext)state;
                ctx.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
                return Task.CompletedTask;
            }, context);
        }

        return _next(context);
    }
}
