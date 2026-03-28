using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: true, reloadOnChange: true);

// Add Ocelot
builder.Services.AddOcelot(builder.Configuration);

// Add CORS rules if needed for Angular SPA
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
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
