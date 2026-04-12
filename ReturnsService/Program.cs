using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ReturnsService.Data;
using ReturnsService.Interfaces;
using ReturnsService.Services;
using ReturnsService.Repositories;
using MassTransit;
using Microsoft.OpenApi.Models;
using RetailPOS.Common.Authorization;
using RetailPOS.Common.Logging;
using Serilog;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.ConfigureSerilog("ReturnsService");

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(x => {
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
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ReturnsService API", Version = "v1" });
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

builder.Services.AddDbContext<ReturnsService.Data.ReturnsSagaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<ReturnsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

// Register Repositories and Services
builder.Services.AddScoped<IReturnRepository, ReturnRepository>();
builder.Services.AddScoped<IReturnService, ReturnsService.Services.ReturnService>();

// MassTransit config
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ReturnsService.Consumers.SagaReturnCommandsConsumer>();
    x.AddSagaStateMachine<ReturnsService.Sagas.ReturnStateMachine, ReturnsService.Sagas.ReturnSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<ReturnsService.Data.ReturnsSagaDbContext>();
            r.UseSqlServer();
        });
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
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
            ValidAudiences = builder.Configuration.GetSection("Jwt:Audiences").Get<string[]>() ?? new[] { builder.Configuration["Jwt:Audience"] ?? "RetailPOSClients" },
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "super_secret_key_1234567890_pos_system"))
        };
    });

// Project-Wide Granular Authorization (Enterprise RBAC)
builder.Services.AddRetailPOSAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
// Migration and Seeding logic
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ReturnsDbContext>();
        await context.Database.MigrateAsync();

        var sagaContext = services.GetRequiredService<ReturnsService.Data.ReturnsSagaDbContext>();
        await sagaContext.Database.MigrateAsync();
        
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Returns & Saga Databases initialized successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.MapControllers();
app.Run();
