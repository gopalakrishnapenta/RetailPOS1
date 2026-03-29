using System.Net;
using System.Text.Json;
using CatalogService.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/problem+json";
            
            var statusCode = (int)HttpStatusCode.InternalServerError;
            var title = "An unexpected error occurred.";
            var detail = exception.Message;

            if (exception is DomainException domainEx)
            {
                statusCode = (int)domainEx.StatusCode;
                title = "Domain Rule Violation";
            }
            else if (exception is UnauthorizedAccessException)
            {
                statusCode = (int)HttpStatusCode.Unauthorized;
                title = "Unauthorized Access";
            }

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = _env.IsDevelopment() ? detail : "Contact support for more information.",
                Instance = context.Request.Path
            };

            // Add trace ID/RequestId?
            problem.Extensions.Add("traceId", context.TraceIdentifier);

            var json = JsonSerializer.Serialize(problem);
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync(json);
        }
    }

    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
