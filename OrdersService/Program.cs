using Microsoft.EntityFrameworkCore;
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
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.ConfigureSerilog("OrdersService");

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrdersService.Consumers.PaymentProcessedConsumer>();
    x.AddConsumer<OrdersService.Consumers.ReturnInitiatedConsumer>();
    x.AddConsumer<OrdersService.Consumers.OrderReturnedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h => {
            h.Username("guest");
            h.Password("guest");
        });
        
        cfg.ReceiveEndpoint("orders-payment-queue", e => {
            e.ConfigureConsumer<OrdersService.Consumers.PaymentProcessedConsumer>(context);
        });
        
        cfg.ReceiveEndpoint("orders-return-init-queue", e => {
            e.ConfigureConsumer<OrdersService.Consumers.ReturnInitiatedConsumer>(context);
        });

        cfg.ReceiveEndpoint("orders-return-complete-queue", e => {
            e.ConfigureConsumer<OrdersService.Consumers.OrderReturnedConsumer>(context);
        });
    });
});

builder.Services.AddControllers().AddJsonOptions(x => {
    x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
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
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true, 
            ValidateAudience = true, 
            ValidateLifetime = true, 
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudiences = builder.Configuration.GetSection("Jwt:Audiences").Get<string[]>() ?? new[] { builder.Configuration["Jwt:Audience"] ?? "OrdersService" },
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "super_secret_key_1234567890_pos_system"))
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
        logger.LogInformation("Orders Database initialized successfully.");
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

app.UseExceptionMiddleware();
// app.UseCors(); removed to centralize CORS in ApiGateway
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();