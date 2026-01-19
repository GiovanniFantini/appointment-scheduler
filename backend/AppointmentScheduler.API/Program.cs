using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using AppointmentScheduler.Data;
using AppointmentScheduler.Core.Services;

try
{
    Console.WriteLine("Starting application...");
    Console.WriteLine($"at time {DateTime.Now}");

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
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Appointment Scheduler API",
            Version = "v1",
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
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = Environment.GetEnvironmentVariable("POSTGRESQLCONNSTR_DefaultConnection");

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
    builder.Services.AddScoped<IEmployeeService, EmployeeService>();
    builder.Services.AddScoped<AppointmentScheduler.Core.Interfaces.IBusinessHoursService, BusinessHoursService>();
    builder.Services.AddScoped<AppointmentScheduler.Core.Interfaces.IClosurePeriodService, ClosurePeriodService>();

    // Shift Management Services
    builder.Services.AddScoped<IShiftTemplateService, ShiftTemplateService>();
    builder.Services.AddScoped<IShiftService, ShiftService>();
    builder.Services.AddScoped<IShiftSwapService, ShiftSwapService>();
    builder.Services.AddScoped<IEmployeeWorkingHoursLimitService, EmployeeWorkingHoursLimitService>();

    // Smart Timbratura System
    builder.Services.AddScoped<ITimbratureService, TimbratureService>();

    Console.WriteLine("Application services registered");

    // JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
    Console.WriteLine("JWT settings loaded");

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
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });
    Console.WriteLine("Authentication configured");

    builder.Services.AddAuthorization(options =>
    {
        // Admin bypassa tutti i controlli (ha tutti i ruoli)
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("MerchantOnly", policy => policy.RequireRole("Merchant", "Admin"));
        options.AddPolicy("EmployeeOnly", policy => policy.RequireRole("Employee", "Admin"));
        options.AddPolicy("ConsumerOnly", policy => policy.RequireRole("Consumer", "Admin"));
    });
    Console.WriteLine("Authorization configured");

    // CORS per frontend
    var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? Array.Empty<string>();

    // Se non configurato, usa gli URL di produzione conosciuti come fallback sicuro
    if (corsOrigins.Length == 0)
    {
        Console.WriteLine("WARNING: No CORS origins configured in appsettings. Using production frontend URLs as fallback.");
        corsOrigins = new[]
        {
            "https://appointment-consumer-app.azurewebsites.net",
            "https://appointment-merchant-app.azurewebsites.net",
            "https://appointment-employee-app.azurewebsites.net",
            "https://appointment-admin-app.azurewebsites.net"
        };
    }

    Console.WriteLine($"CORS Origins configured: {string.Join(", ", corsOrigins)}");

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
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
                Console.WriteLine($"Connection String: {connectionString}");

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Appointment Scheduler API v1");
        c.RoutePrefix = "swagger"; // Serve Swagger UI at /swagger
    });
    Console.WriteLine("Swagger UI enabled");

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseCors("AllowFrontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Map health check endpoints
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready");
    app.MapHealthChecks("/health/live");
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
