using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using OrdersService.Data;
using OrdersService.Interfaces;
using OrdersService.Repositories;
using OrdersService.Services;
using MassTransit;
using OrdersService.Middleware;
using RetailPOS.Common.Authorization;
using Microsoft.OpenApi.Models;
using RetailPOS.Common.Logging;
using RetailPOS.Common.Middleware;
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
builder.ConfigureSerilog("OrdersService");

builder.Services.AddMassTransit(x =>
{
    /* 
    // Configure EF Core Outbox
    x.AddEntityFrameworkOutbox<OrdersDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();
    });
    */

    x.AddConsumer<OrdersService.Consumers.ReturnInitiatedConsumer>();
    x.AddConsumer<OrdersService.Consumers.OrderReturnedConsumer>();
    x.AddConsumer<OrdersService.Consumers.SagaOrderCommandsConsumer>();

    x.AddSagaStateMachine<OrdersService.Sagas.CheckoutStateMachine, OrdersService.Sagas.CheckoutSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<OrdersService.Data.OrdersSagaDbContext>();
            r.UseSqlServer();
        });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h => {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });
        
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddControllers().AddJsonOptions(x => {
    x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    x.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    x.JsonSerializerOptions.AllowTrailingCommas = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();

// builder.Services.AddCors(...) removed to centralize CORS in ApiGateway
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "OrdersService API", Version = "v1" });
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

builder.Services.AddDbContext<OrdersDbContext>(options =>
{
    var connString = builder.Configuration.GetConnectionString("OrdersConnection") ?? builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connString);
});

builder.Services.AddDbContext<OrdersService.Data.OrdersSagaDbContext>(options =>
{
    var connString = builder.Configuration.GetConnectionString("OrdersConnection") ?? builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connString);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        var jwtKey = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("[SECURITY] Fatal Error: Jwt:Key is missing from configuration.");
        }

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true, 
            ValidateAudience = true, 
            ValidateLifetime = true, 
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "RetailPOS", 
            ValidAudiences = builder.Configuration.GetSection("Jwt:Audiences").Get<string[]>() ?? new[] { builder.Configuration["Jwt:Audience"] ?? "OrdersService" },
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Project-Wide Granular Authorization (Enterprise RBAC)
builder.Services.AddRetailPOSAuthorization();

builder.Services.AddScoped<IBillRepository, BillRepository>();
builder.Services.AddScoped<IBillItemRepository, BillItemRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

var app = builder.Build();

try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Applying Migrations and Seeding Orders Database...");
        var context = scope.ServiceProvider.GetRequiredService<OrdersService.Data.OrdersDbContext>();
        await context.Database.MigrateAsync();

        var sagaContext = scope.ServiceProvider.GetRequiredService<OrdersService.Data.OrdersSagaDbContext>();
        await sagaContext.Database.MigrateAsync();
        
        logger.LogInformation("Orders & Saga Databases initialized successfully.");
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An ERROR occurred while seeding the Orders database. The application may be in an unstable state.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionMiddleware();
// app.UseCors(); removed to centralize CORS in ApiGateway
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
