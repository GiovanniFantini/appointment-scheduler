using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using AppointmentScheduler.API.Authorization;
using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit.Abstractions;

namespace AppointmentScheduler.API.Tests;

public sealed class ApiEndpointRegressionTests(ITestOutputHelper output)
{
    public sealed class MatrixAuth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        private static int requestId;
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Ogni caso rappresenta un client distinto; il throttling ha test dedicati.
            Context.Connection.RemoteIpAddress = new IPAddress(Interlocked.Increment(ref requestId));
            var role = Request.Headers["X-Test-Role"].ToString();
            if (role.Length == 0) return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new List<Claim> { new(ClaimTypes.Role, role), new(ClaimTypes.NameIdentifier, "1"), new("EmployeeId", "1"), new("MerchantApproved", "true") };
            if (!Request.Headers.ContainsKey("X-No-Company")) claims.Add(new("MerchantId", "1"));
            foreach (var feature in Enum.GetNames<MerchantFeature>())
            {
                claims.Add(new("Feature", feature));
                claims.Add(new("FeatureLevel", feature + ":Manager"));
            }
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }

    public sealed class AuthorizationProbe : IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            // Misura l'accesso dopo middleware/policy, senza eseguire mutazioni o inviare email.
            if (context.HttpContext.Request.Headers.ContainsKey("X-Authorization-Probe")) context.Result = new NoContentResult();
        }
        public void OnResourceExecuted(ResourceExecutedContext context) { }
    }

    [Fact]
    public async Task EveryEndpoint_PreservesAccessAndEnforcesSubscription()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await RunMatrix(options);
    }

    [LocalPostgresFact]
    public async Task EveryEndpoint_OnPostgres()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SCHEDULER_TEST_POSTGRES"));
        if (connection.Host is not ("localhost" or "127.0.0.1" or "::1")) throw new InvalidOperationException("Il test accetta solo un server locale.");
        connection.Database = "scheduler_api_test_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection.ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        try
        {
            await db.Database.MigrateAsync();
            await RunMatrix(options);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    private async Task RunMatrix(DbContextOptions<ApplicationDbContext> options)
    {
        await using var db = new ApplicationDbContext(options);
        var plan = await db.SubscriptionPlans.SingleOrDefaultAsync() ?? new SubscriptionPlan { Id = 1, Name = "Completo", Features = Enumerable.Range(1, 10).ToArray() };
        var user = new User { Id = 1, Email = "matrix@example.test", AccountType = AccountType.Merchant };
        var merchant = new Merchant { Id = 1, User = user, CompanyName = "Matrix", IsApproved = true, SubscriptionPlan = plan };
        var role = new MerchantRole { Id = 1, Merchant = merchant, Name = "Manager", Features = Enum.GetValues<MerchantFeature>()
            .Select(f => new RoleFeature { Feature = f, IsEnabled = true, AccessLevel = FeatureAccessLevel.Manager }).ToList() };
        db.EmployeeMemberships.Add(new EmployeeMembership { Id = 1, Merchant = merchant, Role = role,
            Employee = new Employee { Id = 1, User = user, Email = user.Email }, HomeBranch = new MerchantBranch { Id = 1, Merchant = merchant, Name = "Sede" } });
        await db.SaveChangesAsync();
        using var factory = new ApiWebApplicationFactory();
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.AddScoped(_ => new ApplicationDbContext(options));
            services.Configure<MvcOptions>(o => o.Filters.Add<AuthorizationProbe>());
            services.AddAuthentication("Matrix").AddScheme<AuthenticationSchemeOptions, MatrixAuth>("Matrix", _ => { });
            services.PostConfigure<AuthenticationOptions>(o => { o.DefaultAuthenticateScheme = "Matrix"; o.DefaultChallengeScheme = "Matrix"; o.DefaultScheme = "Matrix"; });
            services.AddLogging(logging => logging.ClearProviders());
        }));
        using var client = app.CreateClient();
        var actions = app.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items.OfType<ControllerActionDescriptor>().ToArray();
        var failures = new List<string>();
        var requests = 0;
        foreach (var action in actions)
        {
            var route = "/" + Regex.Replace(action.AttributeRouteInfo!.Template!, "\\{[^}]+\\}", "1");
            var method = action.ActionConstraints!.OfType<Microsoft.AspNetCore.Mvc.ActionConstraints.HttpMethodActionConstraint>().Single().HttpMethods.Single();
            var auth = action.EndpointMetadata.OfType<IAuthorizeData>().ToArray();
            var publicRoute = auth.Length == 0 || action.EndpointMetadata.OfType<IAllowAnonymous>().Any();
            var account = route.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase) || route.StartsWith("/api/account", StringComparison.OrdinalIgnoreCase)
                || route.StartsWith("/api/subscription", StringComparison.OrdinalIgnoreCase);
            var authorizedRole = auth.Any(a => a.Policy == "AdminOnly") ? "Admin"
                : auth.Any(a => a.Policy is "MerchantOnly" or "ApprovedMerchantOnly") ? "Merchant" : "Employee";
            var feature = action.EndpointMetadata.OfType<RequiresPlanFeatureAttribute>().LastOrDefault();
            MerchantFeature? expectedFeature = action.ControllerName switch
            {
                "Events" => MerchantFeature.Calendario,
                "EmployeeRequests" => MerchantFeature.Richieste,
                "EmployeeResources" or "EmployeeRoles" => MerchantFeature.Risorse,
                "MerchantRoles" => MerchantFeature.Ruoli,
                "EmployeeDocuments" => MerchantFeature.Documenti,
                "Skills" or "EmployeeSkills" => MerchantFeature.Mansioni,
                "EmployeeProfile" when action.ActionName == "GetMySkills" => MerchantFeature.Mansioni,
                "EmployeeBranches" => MerchantFeature.Filiali,
                "EmployeeTimeClock" or "EmployeeTimeClockManagement" => MerchantFeature.Timbratura,
                "EmployeeInventory" => MerchantFeature.Magazzino,
                _ => null
            };
            feature?.Feature.Should().Be(expectedFeature, $"contratto del modulo per {method} {route}");
            async Task Check(string label, string? caller, HttpStatusCode expected, bool probe = true, bool noCompany = false)
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), route);
                if (caller != null) request.Headers.Add("X-Test-Role", caller);
                if (probe) request.Headers.Add("X-Authorization-Probe", "1");
                if (noCompany) request.Headers.Add("X-No-Company", "1");
                using var response = await client.SendAsync(request);
                requests++;
                if (response.StatusCode != expected) failures.Add($"{label}: {method} {route}: expected {(int)expected}, got {(int)response.StatusCode}");
            }
            await Check("anonymous", null, publicRoute ? HttpStatusCode.NoContent : HttpStatusCode.Unauthorized);
            await Check("complete", authorizedRole, HttpStatusCode.NoContent);
            await Check("admin", "Admin", HttpStatusCode.NoContent);
            if (auth.Any(a => a.Policy != null))
                await Check("wrong-role", authorizedRole == "Employee" ? "Merchant" : "Employee", publicRoute ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden);
            await Check("no-company", authorizedRole, publicRoute || account || authorizedRole == "Admin" ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden, noCompany: true);
            plan.Features = [];
            await db.SaveChangesAsync();
            await Check("empty-plan", authorizedRole, feature != null && authorizedRole != "Admin" && !publicRoute ? HttpStatusCode.Forbidden : HttpStatusCode.NoContent);
            plan.Features = Enumerable.Range(1, 10).ToArray();
            merchant.TrialEndsAt = DateTime.UtcNow.AddDays(-1);
            await db.SaveChangesAsync();
            await Check("expired", authorizedRole, !publicRoute && !account && authorizedRole != "Admin" ? HttpStatusCode.Forbidden : HttpStatusCode.NoContent);
            merchant.TrialEndsAt = null;
            await db.SaveChangesAsync();
            if (method == "GET")
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, route);
                request.Headers.Add("X-Test-Role", authorizedRole);
                using var response = await client.SendAsync(request);
                requests++;
                // Il download di un documento inesistente usa 403 per non rivelarne l'esistenza.
                var deniedMissingDocument = route == "/api/employee/documents/1/download" && response.StatusCode == HttpStatusCode.Forbidden;
                if (!deniedMissingDocument && response.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.BadRequest or HttpStatusCode.NotFound))
                    failures.Add($"actual-read: GET {route}: {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            }
            else
            {
                var missingRoute = "/" + Regex.Replace(action.AttributeRouteInfo.Template!, "\\{[^}]+\\}", "2147483647");
                using var request = new HttpRequestMessage(new HttpMethod(method), missingRoute)
                {
                    Content = new StringContent("{", Encoding.UTF8, "application/json")
                };
                request.Headers.Add("X-Test-Role", authorizedRole);
                using var response = await client.SendAsync(request);
                requests++;
                // Senza il probe: verifica binding, validazioni e risorse mancanti sui metodi di scrittura.
                if (!response.IsSuccessStatusCode && response.StatusCode is not (HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.Conflict))
                    failures.Add($"invalid-write: {method} {missingRoute}: {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            }
        }
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        var created = await client.PostAsJsonAsync("/api/admin/subscription-plans", new { name = "Solo calendario", features = new[] { 1 }, maxEmployees = 3 });
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = (await created.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetInt32();
        (await client.PutAsJsonAsync($"/api/admin/subscription-plans/{id}", new { name = "Calendario aggiornato", features = new[] { 1, 2 }, maxEmployees = 2 })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PutAsJsonAsync("/api/admin/subscription-plans/merchants/1", new { planId = id, trialDays = 14 })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsJsonAsync("/api/admin/subscription-plans/merchants/1/extend-trial", new { days = 7 })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        db.ChangeTracker.Clear();
        (await db.Merchants.SingleAsync()).SubscriptionPlanId.Should().Be(id);
        (await db.Merchants.SingleAsync()).TrialEndsAt.Should().BeAfter(DateTime.UtcNow.AddDays(20));
        (await client.PostAsJsonAsync("/api/admin/subscription-plans", new { name = "Invalido", features = new[] { 999 } })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/api/admin/subscription-plans", new { name = "Limite invalido", features = new[] { 1 }, maxEmployees = -1 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/api/admin/subscription-plans", new { name = "Calendario aggiornato", features = new[] { 1 } })).StatusCode.Should().Be(HttpStatusCode.Conflict);
        output.WriteLine($"{actions.Length} azioni API, {requests + 7} richieste HTTP; probe autorizzazione, letture reali, scritture non valide e flusso admin pacchetti completo.");
        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
        foreach (var action in actions.Where(a => !(a.ControllerName == "Activity" && a.ActionName == "Collect")))
            (await db.ActivityEvents.AnyAsync(e => e.Category == "request" && e.Action == action.ControllerName + "." + action.ActionName))
                .Should().BeTrue($"l'endpoint {action.ControllerName}.{action.ActionName} deve produrre audit anche senza modifiche");
    }
}
