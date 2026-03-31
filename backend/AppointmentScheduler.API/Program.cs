using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using AppointmentScheduler.Data;
using AppointmentScheduler.Core.Services;
using AppointmentScheduler.Core.Interfaces;

try
{
    Console.WriteLine("Starting application...");

    var builder = WebApplication.CreateBuilder(args);

    // Controllers
    builder.Services.AddControllers();
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
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? Environment.GetEnvironmentVariable("POSTGRESQLCONNSTR_DefaultConnection");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    // ── Application Services ───────────────────────────────────────────────
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IEventService, EventService>();
    builder.Services.AddScoped<IMerchantRoleService, MerchantRoleService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IMerchantService, MerchantService>();
    builder.Services.AddScoped<IEmployeeService, EmployeeService>();

    // HR Documents (Azure Blob)
    builder.Services.AddScoped<IFileStorageService, AzureBlobStorageService>();
    builder.Services.AddScoped<IHRDocumentService, HRDocumentService>();

    // Email + Password Reset
    builder.Services.AddScoped<IEmailService, AzureEmailService>();
    builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

    // ── JWT Authentication ─────────────────────────────────────────────────
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"]
        ?? throw new InvalidOperationException("JWT SecretKey not configured");

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

    // ── Authorization Policies ─────────────────────────────────────────────
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("MerchantOnly", policy => policy.RequireRole("Merchant", "Admin"));
        options.AddPolicy("EmployeeOnly", policy => policy.RequireRole("Employee", "Admin"));
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
            "https://appointment-admin-app.azurewebsites.net"
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

    if (!string.IsNullOrWhiteSpace(connectionString) && runMigrations)
    {
        using var scope = app.Services.CreateScope();
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var seedData = builder.Configuration.GetValue<bool?>("SEED_DATABASE") ?? app.Environment.IsDevelopment();
            DbInitializer.Initialize(context, seedData);
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

    app.UseCors("AllowFrontend");
    app.UseAuthentication();
    app.UseAuthorization();
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
