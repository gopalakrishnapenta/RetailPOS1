using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using RetailPOS.Common.Logging;

var builder = WebApplication.CreateBuilder(args);

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

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Add configuration sources
builder.Configuration.AddEnvironmentVariables();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

// Configure Authentication
var jwtSecret = builder.Configuration["Jwt:Key"] ?? "super_secret_key_1234567890_pos_system";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "RetailPOS",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "RetailPOS_Clients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

var app = builder.Build();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
