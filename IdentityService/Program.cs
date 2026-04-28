using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using IdentityService.Data;
using IdentityService.Interfaces;
using IdentityService.Services;
using IdentityService.Repositories;
using MassTransit;
using IdentityService.Consumers;
using IdentityService.Middleware;
using RetailPOS.Common.Logging;
using Serilog;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Load the .env file robustly from current or parent directory
var currentDir = Directory.GetCurrentDirectory();
var envPath = Path.Combine(currentDir, ".env");
if (!File.Exists(envPath))
{
    envPath = Path.Combine(currentDir, "..", ".env");
}

if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
    Console.WriteLine($"[CONFIG] Loaded .env from: {Path.GetFullPath(envPath)}");
}
else
{
    Console.WriteLine("[CONFIG] WARNING: .env file not found.");
}

var builder = WebApplication.CreateBuilder(args);
// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

// Configure Serilog
builder.ConfigureSerilog("IdentityService");

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StoreCreatedConsumer>();
    x.AddConsumer<StaffAssignedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host(rabbitHost, "/", h => {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ReceiveEndpoint("identity-store-created", e =>
        {
            e.ConfigureConsumer<StoreCreatedConsumer>(context);
        });

        cfg.ReceiveEndpoint("identity-staff-assigned", e =>
        {
            e.ConfigureConsumer<StaffAssignedConsumer>(context);
        });
    });
});

builder.Services.AddControllers().AddJsonOptions(x => {
    x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    x.JsonSerializerOptions.AllowTrailingCommas = true;
});
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// ── Swagger with Bearer token support ────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Identity Service API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token below. Example: eyJhbGci..."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// ── JWT Authentication ────────────────────────────────────────────────────
var jwtKey    = builder.Configuration["Jwt:Key"]      ?? "super_secret_key_1234567890_pos_system";
var jwtIssuer = builder.Configuration["Jwt:Issuer"]   ?? "RetailPOS";
var jwtAud    = builder.Configuration["Jwt:Audience"] ?? "RetailPOS_Clients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudiences           = builder.Configuration.GetSection("Jwt:Audiences").Get<string[]>() ?? new[] { jwtAud, "RetailPOS_Services" },
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connString = builder.Configuration.GetConnectionString("IdentityConnection") ?? builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connString);
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStoreRepository, StoreRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// ── DATABASE INITIALIZATION (Standardized) ──────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        logger.LogInformation("🚀 [IdentityService] Starting Database Initialization...");

        // Apply Migrations
        logger.LogInformation("Applying Identity Database migrations...");
        try 
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning("Identity migration semi-failed or skipped (checking if DB is still usable): {Message}", ex.Message);
        }

        // Initialize/Seed (RBAC, etc.)
        logger.LogInformation("Starting Identity Data Seeding (RBAC)...");
        await DbInitializer.InitAsync(context, logger);
        
        logger.LogInformation("✅ [IdentityService] Database & RBAC initialized successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "❌ [IdentityService] Fatal error during database initialization.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity Service v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseExceptionMiddleware();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

