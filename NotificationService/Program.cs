using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using NotificationService.Data;
using NotificationService.Interfaces;
using NotificationService.Services;
using NotificationService.Repositories;
using NotificationService.Hubs;
using NotificationService.Consumers;
using MassTransit;
using RetailPOS.Common.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog (Common Extension)
builder.ConfigureSerilog("NotificationService");

// Add Database Context
builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repository Pattern
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, InternalNotificationService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        policy => policy.WithOrigins("http://localhost:4200", "http://localhost:5000") // UI and Gateway
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

// Configure SignalR
builder.Services.AddSignalR();

// Configure MassTransit with Consumers
builder.Services.AddMassTransit(x =>
{
    // Add all consumers for Phase 4
    x.AddConsumer<UserRegisteredConsumer>();
    x.AddConsumer<StockAdjustedConsumer>();
    x.AddConsumer<OrderPlacedConsumer>();
    x.AddConsumer<ReturnInitiatedConsumer>();
    x.AddConsumer<OrderReturnedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });
        
        cfg.ReceiveEndpoint("notification-service-queue", e =>
        {
            e.ConfigureConsumer<UserRegisteredConsumer>(context);
            e.ConfigureConsumer<StockAdjustedConsumer>(context);
            e.ConfigureConsumer<OrderPlacedConsumer>(context);
            e.ConfigureConsumer<ReturnInitiatedConsumer>(context);
            e.ConfigureConsumer<OrderReturnedConsumer>(context);
        });
    });
});

// Configure Authentication (for secure SignalR connections)
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
            ValidAudiences = new[] { builder.Configuration["Jwt:Audience"] ?? "NotificationService" },
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "super_secret_key_1234567890_pos_system"))
        };
        
        // Custom logic to handle token in SignalR query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddControllers().AddJsonOptions(x => {
    x.JsonSerializerOptions.AllowTrailingCommas = true;
});

// Configure Swagger for API visibility
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Notification Service API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token. Example: eyJhbGci..."
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSerilogRequestLogging();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR Hub
app.MapHub<NotificationHub>("/notificationHub");

// Enable Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification Service v1");
        c.RoutePrefix = "swagger";
    });
}

app.Run();
