using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text;
using AdminService.Data;
using AdminService.Interfaces;
using AdminService.Services;
using AdminService.Repositories;
using MassTransit;
using AdminService.Middleware;
using AdminService.Consumers;
using RetailPOS.Common.Authorization;
using RetailPOS.Common.Logging;
using Serilog;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.ConfigureSerilog("AdminService");

builder.Services.AddControllers().AddJsonOptions(x =>
    x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();

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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Admin Service API", Version = "v1" });
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

// Project-Wide Granular Authorization (Enterprise RBAC)
builder.Services.AddRetailPOSAuthorization();

builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<AdminService.Data.AdminSagaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<AdminService.Sagas.OnboardingStateMachine, AdminService.Sagas.OnboardingSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<AdminService.Data.AdminSagaDbContext>();
            r.UseSqlServer();
        });

    x.AddConsumer<DashboardEventsConsumer>();
    x.AddConsumer<SagaUserOnboardingConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h => {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);

    });
});

builder.Services.AddScoped<IInventoryAdjustmentRepository, InventoryAdjustmentRepository>();
builder.Services.AddScoped<IStoreRepository, StoreRepository>();

builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IReportService, ReportService>();

var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<AdminService.Data.AdminDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        try 
        {
            logger.LogInformation("Attempting to apply Admin Service migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Admin Service migrations applied successfully.");

            logger.LogInformation("🚀 Starting Admin Data Seeding...");
            await AdminService.Data.DbInitializer.InitAsync(context, logger);
            logger.LogInformation("Admin Data Seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Admin Service initialization failed.");
        }
    }

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Admin Service v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseExceptionMiddleware();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
// Migration and Initial Setup logic
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AdminDbContext>();
        await context.Database.MigrateAsync();

        var sagaContext = services.GetRequiredService<AdminService.Data.AdminSagaDbContext>();
        await sagaContext.Database.MigrateAsync();
        
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Admin & Saga Databases initialized successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.MapControllers();
app.Run();
