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

    // Rileva se siamo in design-time mode (EF Core Tools)
    // Questo check evita che l'applicazione tenti di avviarsi quando EF Tools cerca il DbContext
    var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
    var isEfCommand = processName.Contains("ef", StringComparison.OrdinalIgnoreCase) ||
                      args.Any(arg => arg.Contains("ef", StringComparison.OrdinalIgnoreCase)) ||
                      Environment.GetCommandLineArgs().Any(arg => arg.Contains("migrations", StringComparison.OrdinalIgnoreCase));

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
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"Connection string loaded: {!string.IsNullOrEmpty(connectionString)}");

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
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("MerchantOnly", policy => policy.RequireRole("Merchant", "Admin"));
        options.AddPolicy("UserOnly", policy => policy.RequireRole("User", "Merchant", "Admin"));
    });
    Console.WriteLine("Authorization configured");

    // CORS per frontend
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(
                "http://localhost:5173",  // Consumer app
                "http://localhost:5174",  // Merchant app
                "http://localhost:5175",  // Admin app
                "https://localhost:5173",
                "https://localhost:5174",
                "https://localhost:5175"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
    });
    Console.WriteLine("CORS configured");

    Console.WriteLine("Building application...");
    var app = builder.Build();
    Console.WriteLine("Application built successfully");

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        Console.WriteLine("Swagger UI enabled");
    }
    else
    {
        app.UseHttpsRedirection();
    }

    app.UseCors("AllowFrontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
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
