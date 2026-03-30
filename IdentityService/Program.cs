using Microsoft.EntityFrameworkCore;
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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StoreCreatedConsumer>();
    x.AddConsumer<StaffAssignedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h => {
            h.Username("guest");
            h.Password("guest");
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

builder.Services.AddControllers().AddJsonOptions(x =>
    x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();

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
var jwtAud    = builder.Configuration["Jwt:Audience"] ?? "RetailPOSClients";

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
            ValidAudiences           = builder.Configuration.GetSection("Jwt:Audiences").Get<string[]>() ?? new[] { jwtAud },
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStoreRepository, StoreRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// ── [UPGRADE] Dynamic RBAC & Database Initialization ────────────────────
try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        var context = services.GetRequiredService<AppDbContext>();

        logger.LogInformation("Attempting to apply Identity Database migrations...");
        
        try 
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning("Identity migration skipped (Database may be dirty): {Message}", ex.Message);
        }

        // [PRO UPGRADE] This MUST run even if migration skipped, to ensure Permissions are synced!
        await DbInitializer.InitAsync(context, logger);
        
        logger.LogInformation("Identity Database & RBAC initialized successfully.");
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "CRITICAL ERROR: Failed to initialize Identity Database/RBAC.");
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
