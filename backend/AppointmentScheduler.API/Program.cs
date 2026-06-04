using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using AppointmentScheduler.Data;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.API.Authorization;
using AppointmentScheduler.API.Middleware;
using Microsoft.AspNetCore.Authorization;

try
{
    Console.WriteLine("Starting application...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseDefaultServiceProvider((context, options) =>
    {
        var validateContainer = context.HostingEnvironment.IsDevelopment() ||
                                context.HostingEnvironment.IsEnvironment("Testing");

        options.ValidateScopes = validateContainer;
        options.ValidateOnBuild = validateContainer;
    });

    // Controllers
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // Normalizza la stringa vuota a null per i campi TimeOnly? (es. orari
            // di un permesso): senza questo l'invio di "" da un input time svuotato
            // genererebbe un 400 con messaggio tecnico incomprensibile.
            options.JsonSerializerOptions.Converters.Add(
                new AppointmentScheduler.API.Converters.NullableTimeOnlyJsonConverter());
        });

    // Validazione automatica del model (DataAnnotations): la risposta resta il
    // ProblemDetails standard (400 con `errors`), ma logghiamo i campi falliti
    // lato server. Senza questo un 400 di validazione appariva nei log solo come
    // "status 400" senza dire QUALE campo: ci ha nascosto il caso reale in cui
    // una password troppo corta veniva poi mostrata all'utente come "link scaduto".
    builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    {
        var defaultFactory = options.InvalidModelStateResponseFactory;
        options.InvalidModelStateResponseFactory = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("ModelValidation");

            var fields = context.ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .Select(kvp => kvp.Key);

            logger.LogWarning(
                "Validazione fallita su {Path}: campi non validi = {Fields}",
                context.HttpContext.Request.Path,
                string.Join(", ", fields));

            return defaultFactory(context);
        };
    });

    builder.Services.AddEndpointsApiExplorer();

    // Health checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("database");

    // Swagger + JWT
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Gestionale Aziendale API",
            Version = "v1",
            Description = "API per la gestione eventi, turni e risorse aziendali"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization. Inserisci: Bearer {token}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    // Database
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
        connectionString = Environment.GetEnvironmentVariable("POSTGRESQLCONNSTR_DefaultConnection");

    var maskedCs = connectionString is not null && connectionString.Length > 20
        ? connectionString[..20] + "***"
        : "(null or empty)";
    Console.WriteLine($"Connection string source resolved. Preview: {maskedCs}");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
    builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

    var jwtTokenOptions = builder.Configuration.GetSection("JwtSettings").Get<JwtTokenOptions>()
        ?? new JwtTokenOptions();
    if (string.IsNullOrWhiteSpace(jwtTokenOptions.SecretKey))
        throw new InvalidOperationException("JWT SecretKey not configured");

    var frontendUrlOptions = builder.Configuration
        .GetSection("AzureCommunicationServices:FrontendBaseUrls")
        .Get<FrontendUrlOptions>()
        ?? new FrontendUrlOptions();

    var azureBlobStorageOptions = builder.Configuration.GetSection("AzureBlobStorage").Get<AzureBlobStorageOptions>()
        ?? new AzureBlobStorageOptions();
    azureBlobStorageOptions.ConnectionString = builder.Configuration.GetConnectionString("AzureBlobStorage")
        ?? azureBlobStorageOptions.ConnectionString;

    var azureEmailOptions = builder.Configuration.GetSection("AzureCommunicationServices").Get<AzureEmailOptions>()
        ?? new AzureEmailOptions();

    builder.Services.AddSingleton(jwtTokenOptions);
    builder.Services.AddSingleton(frontendUrlOptions);
    builder.Services.AddSingleton(azureBlobStorageOptions);
    builder.Services.AddSingleton(azureEmailOptions);
    builder.Services.AddSingleton<IUtcClock, SystemUtcClock>();
    builder.Services.AddSingleton<IWallClock, SystemWallClock>();
    builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
    builder.Services.AddSingleton<IPasswordResetTokenGenerator, PasswordResetTokenGenerator>();

    // ── Application Services ───────────────────────────────────────────────
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<IShiftConflictValidator, ShiftConflictValidator>();
    builder.Services.AddScoped<IEventService, EventService>();
    builder.Services.AddScoped<IMerchantRoleService, MerchantRoleService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IMerchantService, MerchantService>();
    builder.Services.AddScoped<IAdminUserService, AdminUserService>();
    builder.Services.AddScoped<IAdminEmployeeService, AdminEmployeeService>();
    builder.Services.AddScoped<IEmployeeService, EmployeeService>();
    builder.Services.AddScoped<IEmployeeRequestService, EmployeeRequestService>();
    builder.Services.AddScoped<ISkillService, SkillService>();
    builder.Services.AddScoped<IBranchService, BranchService>();
    builder.Services.AddScoped<ITimeClockService, TimeClockService>();
    builder.Services.AddScoped<IInventoryService, InventoryService>();
    builder.Services.AddScoped<IEmployeeInventoryService, EmployeeInventoryService>();
    builder.Services.AddScoped<ISupplierService, SupplierService>();
    builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
    builder.Services.AddScoped<IInventoryReportingService, InventoryReportingService>();

    // HR Documents (Azure Blob)
    builder.Services.AddScoped<IFileStorageService, AzureBlobStorageService>();
    builder.Services.AddScoped<IHRDocumentService, HRDocumentService>();

    // Email + Password Reset
    builder.Services.AddScoped<IEmailService, AzureEmailService>();
    builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

    // ── JWT Authentication ─────────────────────────────────────────────────
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtTokenOptions.Issuer,
            ValidAudience = jwtTokenOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtTokenOptions.SecretKey))
        };
    });

    // ── Authorization Policies ─────────────────────────────────────────────
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("MerchantOnly", policy => policy.RequireRole("Merchant", "Admin"));
        options.AddPolicy("EmployeeOnly", policy => policy.RequireRole("Employee", "Admin"));

        // Come MerchantOnly, ma il merchant deve anche essere approvato dall'admin.
        // Usata sulle rotte operative: un merchant non approvato riceve 403.
        options.AddPolicy("ApprovedMerchantOnly", policy =>
            policy.Requirements.Add(new ApprovedMerchantRequirement()));
    });

    // Handler della policy ApprovedMerchantOnly.
    builder.Services.AddSingleton<IAuthorizationHandler, ApprovedMerchantHandler>();

    // Necessario per leggere IP / User-Agent dai servizi (AuthService userà
    // l'IP per audit log e per il rate limiting per-email applicativo).
    builder.Services.AddHttpContextAccessor();

    // ── Rate Limiting (endpoint auth) ──────────────────────────────────────
    // Politica per IP applicata a tutti gli endpoint di /api/auth/* tramite
    // attributo [EnableRateLimiting("auth-ip")] sul controller. Il rate limit
    // per-email è invece applicato lato AuthService perché richiede l'email
    // già deserializzata dal binder (leggere il body dal limiter è fragile).
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;

        options.AddPolicy("auth-ip", httpContext =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
        });

        options.OnRejected = async (context, cancellationToken) =>
        {
            // Calcola i minuti residui dalla metadata del lease (RetryAfter è
            // TimeSpan). Se il limiter non la espone usiamo il fallback vago.
            var retryAfterSeconds = 0;
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
                context.HttpContext.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
            }

            var minutes = AuthThrottleMessages.CeilToMinutes(TimeSpan.FromSeconds(retryAfterSeconds));
            var body = JsonSerializer.Serialize(new { message = AuthThrottleMessages.GenericRetry(minutes) });
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsync(body, cancellationToken);
        };
    });

    // ── CORS ───────────────────────────────────────────────────────────────
    var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>()
        ?? Array.Empty<string>();

    if (corsOrigins.Length == 0)
    {
        corsOrigins = new[]
        {
            "https://appointment-consumer-app.azurewebsites.net",
            "https://appointment-merchant-app.azurewebsites.net",
            "https://appointment-employee-app.azurewebsites.net",
            "https://appointment-admin-app.azurewebsites.net",
            "https://gestione.turnis.it",
            "https://mio.turnis.it"
        };
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
    });

    var app = builder.Build();

    // ── Database Init ──────────────────────────────────────────────────────
    var runMigrations = builder.Configuration.GetValue<bool?>("RUN_MIGRATIONS") ?? true;

    Console.WriteLine($"Database initialization check: ConnectionString {(string.IsNullOrWhiteSpace(connectionString) ? "NOT set" : "set")}, RUN_MIGRATIONS={runMigrations}");

    if (!string.IsNullOrWhiteSpace(connectionString) && runMigrations)
    {
        using var scope = app.Services.CreateScope();
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var seedData = builder.Configuration.GetValue<bool?>("SEED_DATABASE") ?? app.Environment.IsDevelopment();
            DbInitializer.Initialize(context, seedData);

            Console.WriteLine("Database initialization completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization error: {ex.Message}");
            if (!app.Environment.IsDevelopment()) throw;
        }
    }

    // ── Pipeline ───────────────────────────────────────────────────────────
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gestionale Aziendale API v1");
        c.RoutePrefix = "swagger";
    });

    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    // Security headers su tutte le risposte (X-Frame-Options, CSP, HSTS in prod).
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // Log every API error status (4xx/5xx) and unhandled exceptions in one place.
    app.UseMiddleware<ApiErrorLoggingMiddleware>();

    app.UseCors("AllowFrontend");
    app.UseAuthentication();
    app.UseAuthorization();

    // Rate limiter è dopo l'autenticazione così che eventuali politiche
    // per-utente possano usare l'identità in claim, ma prima di MapControllers
    // per intercettare le richieste prima del binding del DTO.
    app.UseRateLimiter();

    app.MapControllers();

    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready");
    app.MapHealthChecks("/health/live");

    Console.WriteLine("Application started.");
    app.Run();
}
catch (HostAbortedException)
{
    // Expected during EF Core design-time operations (migrations)
}
catch (Exception ex)
{
    Console.WriteLine($"FATAL ERROR: {ex.GetType().Name}: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Environment.Exit(-1);
}

public partial class Program
{
}
