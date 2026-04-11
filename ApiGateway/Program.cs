using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using RetailPOS.Common.Logging;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
// Newtonsoft support is provided by Microsoft.AspNetCore.Mvc.NewtonsoftJson

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.ConfigureSerilog("ApiGateway");

builder.Configuration.AddJsonFile("ocelot.json", optional: true, reloadOnChange: true);

// Add Ocelot with Newtonsoft support
builder.Services.AddOcelotUsingBuilder((mvcBuilder, assembly) =>
{
    return mvcBuilder.AddNewtonsoftJson();
});

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudiences = builder.Configuration.GetSection("Jwt:Audiences").Get<string[]>() ?? new[] { builder.Configuration["Jwt:Audience"] ?? "ApiGateway" },
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "super_secret_key_1234567890_pos_system"))
        };
    });

builder.Services.AddAuthorization();

// Add CORS rules if needed for Angular SPA
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        policy => policy.AllowAnyOrigin()
        .AllowAnyMethod()
        .WithHeaders("Authorization", "Content-Type", "X-Store-Id", "Accept", "Origin")
        .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
});

var app = builder.Build();

app.UseCors("CorsPolicy");
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints => {
    endpoints.MapGet("/", async context => {
        await context.Response.WriteAsync("Ocelot API Gateway is running.");
    });
});

await app.UseOcelot();

app.Run();
