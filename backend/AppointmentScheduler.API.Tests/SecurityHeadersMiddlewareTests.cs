using AppointmentScheduler.API.Middleware;
using Microsoft.AspNetCore.Http.Features;

namespace AppointmentScheduler.API.Tests;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task Invoke_AddsBaselineSecurityHeaders_OnAllResponses()
    {
        var context = await RunAndFireResponseStartAsync(isDevelopment: false);

        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
        context.Response.Headers["Content-Security-Policy"].ToString().Should().Contain("default-src 'none'");
    }

    [Fact]
    public async Task Invoke_AddsHsts_InProduction()
    {
        var context = await RunAndFireResponseStartAsync(isDevelopment: false);

        context.Response.Headers["Strict-Transport-Security"].ToString()
            .Should().Be("max-age=31536000; includeSubDomains");
    }

    [Fact]
    public async Task Invoke_DoesNotAddHsts_InDevelopment()
    {
        var context = await RunAndFireResponseStartAsync(isDevelopment: true);

        // In sviluppo l'app gira su http: HSTS bloccherebbe i caricamenti futuri.
        context.Response.Headers.ContainsKey("Strict-Transport-Security").Should().BeFalse();
        // Gli altri header sono comunque presenti.
        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
    }

    [Fact]
    public async Task Invoke_CallsNext()
    {
        // Sanity check: il middleware non short-circuita la pipeline.
        var context = new DefaultHttpContext();
        var nextCalled = false;
        var middleware = new SecurityHeadersMiddleware(
            next: _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            env: new TestEnv(isDevelopment: false));

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Invoca il middleware su un HttpContext che usa un IHttpResponseFeature
    /// custom in grado di registrare ed eseguire manualmente le callback
    /// OnStarting (DefaultHttpContext non le esegue mai da solo).
    /// </summary>
    private static async Task<HttpContext> RunAndFireResponseStartAsync(bool isDevelopment)
    {
        var context = new DefaultHttpContext();
        var feature = new RecordingResponseFeature();
        context.Features.Set<IHttpResponseFeature>(feature);

        var middleware = new SecurityHeadersMiddleware(
            next: _ => Task.CompletedTask,
            env: new TestEnv(isDevelopment));

        await middleware.InvokeAsync(context);
        await feature.FireOnStartingAsync();

        return context;
    }

    private sealed class TestEnv : IWebHostEnvironment
    {
        public TestEnv(bool isDevelopment)
        {
            EnvironmentName = isDevelopment ? "Development" : "Production";
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
    }

    /// <summary>
    /// IHttpResponseFeature minimale che colleziona le callback OnStarting e le
    /// esegue al momento giusto (chiamata esplicita da test). Necessario perché
    /// DefaultHttpContext non triggera OnStarting al primo write.
    /// </summary>
    private sealed class RecordingResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _onStartingCallbacks = new();

        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted { get; private set; }

        public void OnStarting(Func<object, Task> callback, object state)
            => _onStartingCallbacks.Add((callback, state));

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            // Non rilevante per i test sui security headers.
        }

        public async Task FireOnStartingAsync()
        {
            HasStarted = true;
            foreach (var (callback, state) in _onStartingCallbacks)
                await callback(state);
        }
    }
}
