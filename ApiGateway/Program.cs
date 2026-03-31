using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using RetailPOS.Common.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.ConfigureSerilog("ApiGateway");

builder.Configuration.AddJsonFile("ocelot.json", optional: true, reloadOnChange: true);

// Add Ocelot
builder.Services.AddOcelot(builder.Configuration);

// Add CORS rules if needed for Angular SPA
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        policy => policy.WithOrigins("http://localhost:4200")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

var app = builder.Build();

app.UseCors("CorsPolicy");

app.UseRouting();
app.UseEndpoints(endpoints => {
    endpoints.MapGet("/", async context => {
        await context.Response.WriteAsync("Ocelot API Gateway is running.");
    });
});

await app.UseOcelot();

app.Run();
