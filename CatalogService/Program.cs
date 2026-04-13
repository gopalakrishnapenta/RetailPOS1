using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using CatalogService.Data;
using CatalogService.Interfaces;
using CatalogService.Services;
using CatalogService.Repositories;
using MassTransit;
using CatalogService.Consumers;
using CatalogService.Middleware;
using RetailPOS.Common.Authorization;
using RetailPOS.Common.Logging;
using Serilog;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.ConfigureSerilog("CatalogService");

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CatalogService.Consumers.CategoryConsumer>();
    x.AddConsumer<CatalogService.Consumers.OrderReturnedConsumer>();
    x.AddConsumer<CatalogService.Consumers.StockAdjustedConsumer>();
    x.AddConsumer<CatalogService.Consumers.SagaDeductStockConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h => {
            h.Username("guest");
            h.Password("guest");
        });


        cfg.ReceiveEndpoint("catalog-category", e =>
        {
            e.ConfigureConsumer<CategoryConsumer>(context);
        });

        cfg.ReceiveEndpoint("catalog-order-returned", e =>
        {
            e.ConfigureConsumer<OrderReturnedConsumer>(context);
        });

        cfg.ReceiveEndpoint("catalog-inventory-adjusted", e =>
        {
            e.ConfigureConsumer<StockAdjustedConsumer>(context);
        });

        cfg.ReceiveEndpoint("catalog-saga-commands", e =>
        {
            e.ConfigureConsumer<SagaDeductStockConsumer>(context);
        });
    });
});

builder.Services.AddControllers().AddJsonOptions(x => {
    x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    x.JsonSerializerOptions.AllowTrailingCommas = true;
});
builder.Services.AddEndpointsApiExplorer();

// ── Swagger with Bearer token support ────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog Service API", Version = "v1" });
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


// builder.Services.AddCors(...) removed to centralize CORS in ApiGateway

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, 
            ValidateAudience = true, 
            ValidateLifetime = true, 
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudiences = builder.Configuration.GetSection("Jwt:Audiences").Get<string[]>() ?? new[] { builder.Configuration["Jwt:Audience"] },
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "super_secret_key_1234567890_pos_system"))
        };
    });

// Project-Wide Granular Authorization (Enterprise RBAC)
builder.Services.AddRetailPOSAuthorization();

var app = builder.Build();

try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Applying Migrations and Seeding Catalog Database...");
        await CatalogService.Data.SeedData.Initialize(services);
        logger.LogInformation("Catalog Database initialized successfully.");
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An ERROR occurred while seeding the Catalog database. The application may be in an unstable state.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog Service v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseExceptionMiddleware();
// app.UseCors(); removed to centralize CORS in ApiGateway
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
