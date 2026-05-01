using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using RetailPOS.Common.Logging;
using RetailPOS.Common.Middleware;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
// Newtonsoft support is provided by Microsoft.AspNetCore.Mvc.NewtonsoftJson

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
builder.ConfigureSerilog("ApiGateway");

// Add Ocelot with Polly and Newtonsoft support
var ocelotConfig = "ocelot.json";
if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
{
    ocelotConfig = "ocelot.docker.json";
}
builder.Configuration.AddJsonFile(ocelotConfig, optional: false, reloadOnChange: true);

builder.Services.AddOcelot(builder.Configuration)
    .AddPolly();
    
// Add standard Newtonsoft support if needed by Ocelot internals
builder.Services.AddControllers().AddNewtonsoftJson();


// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("[SECURITY] Fatal Error: Jwt:Key is missing from configuration. Gateway cannot validate tokens.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "RetailPOS",
            ValidAudiences = builder.Configuration.GetSection("Jwt:Audiences").Get<string[]>() ?? 
                             new[] { builder.Configuration["Jwt:Audience"] ?? "RetailPOS_Clients", "RetailPOS_Services" },
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ── Hardened CORS Policy ──────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                         ?? new[] { "http://localhost:4200", "http://localhost:8080" };

    options.AddPolicy("CorsPolicy", policy => 
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowCredentials()
              .WithHeaders("Authorization", "Content-Type", "X-Store-Id", "Accept", "Origin")
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors("CorsPolicy");
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Security Headers for Google Login Popups
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin-allow-popups");
    context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "credentialless");
    await next();
});

app.UseEndpoints(endpoints => {
    endpoints.MapGet("/", async context => {
        await context.Response.WriteAsync("Ocelot API Gateway is running.");
    });
});

await app.UseOcelot();

app.Run();

