using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PaymentService.Data;
using PaymentService.Interfaces;
using PaymentService.Services;
using PaymentService.Repositories;
using MassTransit;
using Microsoft.OpenApi.Models;
using RetailPOS.Common.Authorization;
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
builder.ConfigureSerilog("PaymentService");

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(x => {
    x.JsonSerializerOptions.AllowTrailingCommas = true;
});
builder.Services.AddEndpointsApiExplorer();

// builder.Services.AddCors(...) removed to centralize CORS in ApiGateway
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PaymentService API", Version = "v1" });
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

builder.Services.AddDbContext<PaymentDbContext>(options =>
{
    var connString = builder.Configuration.GetConnectionString("PaymentConnection") ?? builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connString, sqlOptions => 
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

// Register Repositories and Services
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService.Services.PaymentService>();
builder.Services.AddScoped<IRazorpayService, RazorpayService>();

// MassTransit config
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentService.Consumers.CheckoutSagaCommandsConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });
        cfg.ConfigureEndpoints(context);
    });
});

// Auth config
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudiences = builder.Configuration.GetSection("Jwt:Audiences").Get<string[]>() ?? new[] { builder.Configuration["Jwt:Audience"] ?? "PaymentService" },
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "super_secret_key_1234567890_pos_system"))
        };
    });

// Project-Wide Granular Authorization (Enterprise RBAC)
builder.Services.AddRetailPOSAuthorization();

var app = builder.Build();

// Automatically apply migrations on startup
try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<PaymentDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Applying Migrations for PaymentService...");
        await context.Database.MigrateAsync();
        logger.LogInformation("PaymentService Database initialized successfully.");
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while applying migrations to the Payment database.");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseCors(); removed to centralize CORS in ApiGateway
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

