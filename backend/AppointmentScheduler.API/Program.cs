using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using AppointmentScheduler.Data;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.API.Constants;

try
{
    Console.WriteLine("Starting application...");

    // Rileva se siamo in design-time mode (EF Core Tools)
    // Questo check evita che l'applicazione tenti di avviarsi quando EF Tools cerca il DbContext
    var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
    var commandLineArgs = Environment.GetCommandLineArgs();
    var isEfCommand = processName.Contains("ef", StringComparison.OrdinalIgnoreCase) ||
                      args.Any(arg => arg.Contains("ef", StringComparison.OrdinalIgnoreCase)) ||
                      commandLineArgs.Any(arg => arg.Contains("migrations", StringComparison.OrdinalIgnoreCase)) ||
                      commandLineArgs.Any(arg => arg.Contains("database", StringComparison.OrdinalIgnoreCase)) ||
                      commandLineArgs.Any(arg => arg.Contains("dbcontext", StringComparison.OrdinalIgnoreCase));

    if (isEfCommand)
    {
        Console.WriteLine($"EF Core Tools detected (process: {processName}).");
        Console.WriteLine("The DesignTimeDbContextFactory should handle DbContext creation.");
        Console.WriteLine("If you see this message, ensure ApplicationDbContextFactory exists in AppointmentScheduler.Data project.");
        return;
    }

    var builder = WebApplication.CreateBuilder(args);
    Console.WriteLine("Builder created successfully");

    // Add services to the container.
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
    builder.Services.AddEndpointsApiExplorer();
    Console.WriteLine("Controllers registered");

    // Add health checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("database");
    Console.WriteLine("Health checks configured");

    // Swagger con supporto JWT
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc(ApiEndpoints.Swagger.Version, new OpenApiInfo
        {
            Title = ApiEndpoints.Swagger.Title,
            Version = ApiEndpoints.Swagger.Version,
            Description = "API per la gestione di prenotazioni multi-verticale (B2C e B2B)"
        });

        // Configurazione JWT in Swagger
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header usando Bearer scheme. Esempio: \"Bearer {token}\"",
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
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });
    Console.WriteLine("Swagger configured");

    // Database Configuration
    var testSetting = builder.Configuration.GetValue<string>("TestSetting");
    Console.WriteLine($"TestSetting: {testSetting}");

    // Prova prima la variabile d'ambiente, poi il file di configurazione
    var connectionString = Environment.GetEnvironmentVariable(ConfigKeys.Environment.PostgresConnectionString)
        ?? builder.Configuration.GetConnectionString(ConfigKeys.ConnectionStrings.DefaultConnection);

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.WriteLine("WARNING: No database connection string found in environment variables or configuration.");
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        // Usa PostgreSQL (più economico per cloud)
        // Per usare SQL Server, decommenta la riga sotto e commenta quella PostgreSQL
        options.UseNpgsql(connectionString);
        // options.UseSqlServer(connectionString);
    });
    Console.WriteLine("DbContext registered");

    // Register Services
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IMerchantService, MerchantService>();
    builder.Services.AddScoped<IServiceManagementService, ServiceManagementService>();
    builder.Services.AddScoped<IBookingService, BookingService>();
    builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
    Console.WriteLine("Application services registered");

    // JWT Authentication
    // Prova prima le variabili d'ambiente, poi la configurazione, infine i valori di default
    var secretKey = Environment.GetEnvironmentVariable(ConfigKeys.Environment.JwtSecretKey)
        ?? builder.Configuration.GetSection(ConfigKeys.Jwt.SectionName)[ConfigKeys.Jwt.SecretKey]
        ?? throw new InvalidOperationException("JWT SecretKey not configured. Set JWT_SECRET_KEY environment variable.");

    var issuer = Environment.GetEnvironmentVariable(ConfigKeys.Environment.JwtIssuer)
        ?? builder.Configuration.GetSection(ConfigKeys.Jwt.SectionName)[ConfigKeys.Jwt.Issuer]
        ?? Defaults.Jwt.Issuer;

    var audience = Environment.GetEnvironmentVariable(ConfigKeys.Environment.JwtAudience)
        ?? builder.Configuration.GetSection(ConfigKeys.Jwt.SectionName)[ConfigKeys.Jwt.Audience]
        ?? Defaults.Jwt.Audience;

    Console.WriteLine($"JWT settings loaded - Issuer: {issuer}, Audience: {audience}");

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
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });
    Console.WriteLine("Authentication configured");

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthConstants.Policies.AdminOnly,
            policy => policy.RequireRole(AuthConstants.Roles.Admin));
        options.AddPolicy(AuthConstants.Policies.MerchantOnly,
            policy => policy.RequireRole(AuthConstants.Roles.Merchant, AuthConstants.Roles.Admin));
        options.AddPolicy(AuthConstants.Policies.UserOnly,
            policy => policy.RequireRole(AuthConstants.Roles.User, AuthConstants.Roles.Merchant, AuthConstants.Roles.Admin));
    });
    Console.WriteLine("Authorization configured");

    // CORS per frontend
    // Prova prima la variabile d'ambiente (separata da virgole), poi la configurazione
    var corsOriginsEnv = Environment.GetEnvironmentVariable(ConfigKeys.Environment.CorsOrigins);
    var corsOrigins = !string.IsNullOrWhiteSpace(corsOriginsEnv)
        ? corsOriginsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        : builder.Configuration.GetSection(ConfigKeys.Cors.AllowedOrigins).Get<string[]>() ?? Array.Empty<string>();

    Console.WriteLine($"CORS Origins configured: {string.Join(", ", corsOrigins)}");

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(ConfigKeys.Cors.PolicyName, policy =>
        {
            if (corsOrigins.Length > 0)
            {
                policy.WithOrigins(corsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
            else
            {
                Console.WriteLine("WARNING: No CORS origins configured. CORS will block all requests.");
            }
        });
    });
    Console.WriteLine("CORS configured");

    Console.WriteLine("Building application...");
    var app = builder.Build();
    Console.WriteLine("Application built successfully");

    // Applica le migrazioni al database all'avvio
    // ATTENZIONE: Questo viene eseguito anche in PRODUZIONE!
    // Per disabilitare, impostare la variabile d'ambiente: RUN_MIGRATIONS=false
    var runMigrations = builder.Configuration.GetValue<bool?>("RUN_MIGRATIONS") ?? true;

    // Verifica che la connection string sia configurata prima di tentare le migrazioni
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.WriteLine("WARNING: Database connection string is not configured.");
        Console.WriteLine("Skipping database migration. Configure ConnectionStrings:DefaultConnection to enable database features.");
        runMigrations = false;
    }

    if (runMigrations)
    {
        Console.WriteLine("Database migration is enabled (RUN_MIGRATIONS=true or not set)");

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();

                // Controlla se creare i dati di seed
                // In produzione, di default NON crea i seed a meno che non sia esplicitamente richiesto
                // Variabile d'ambiente: SEED_DATABASE=true|false
                bool seedData;

                if (app.Environment.IsDevelopment())
                {
                    // In Development, seed è true di default
                    seedData = builder.Configuration.GetValue<bool?>("SEED_DATABASE") ?? true;
                }
                else
                {
                    // In Production, seed è false di default (per sicurezza)
                    seedData = builder.Configuration.GetValue<bool?>("SEED_DATABASE") ?? true;
                }

                Console.WriteLine($"Database seeding is {(seedData ? "enabled" : "disabled")} (SEED_DATABASE={(seedData ? "true" : "false")})");

                DbInitializer.Initialize(context, seedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL ERROR during database initialization:");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                // In produzione, un errore di migrazione dovrebbe bloccare l'avvio
                if (!app.Environment.IsDevelopment())
                {
                    Console.WriteLine("Database initialization failed in production. Stopping application.");
                    throw;
                }
            }
        }
    }
    else
    {
        Console.WriteLine("Database migration is DISABLED (RUN_MIGRATIONS=false or connection string not configured)");
    }

    // Configure the HTTP request pipeline.
    // Enable Swagger in all environments
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint(ApiEndpoints.Swagger.JsonEndpoint, $"{ApiEndpoints.Swagger.Title} {ApiEndpoints.Swagger.Version}");
        c.RoutePrefix = ApiEndpoints.Swagger.RoutePrefix; // Serve Swagger UI at /swagger
    });
    Console.WriteLine("Swagger UI enabled");

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseCors(ConfigKeys.Cors.PolicyName);
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Map health check endpoints
    app.MapHealthChecks(ApiEndpoints.HealthChecks.Health);
    app.MapHealthChecks(ApiEndpoints.HealthChecks.Ready);
    app.MapHealthChecks(ApiEndpoints.HealthChecks.Live);
    Console.WriteLine("Health check endpoints mapped");

    Console.WriteLine("Middleware pipeline configured");

    Console.WriteLine("Starting web server...");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"FATAL ERROR during startup:");
    Console.WriteLine($"Type: {ex.GetType().Name}");
    Console.WriteLine($"Message: {ex.Message}");
    Console.WriteLine($"StackTrace: {ex.StackTrace}");

    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
        Console.WriteLine($"Inner StackTrace: {ex.InnerException.StackTrace}");
    }

    Environment.Exit(-1);
}
