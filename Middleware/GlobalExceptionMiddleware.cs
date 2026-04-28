using System.Text.Json;
using RestaurantApp.Exceptions;

namespace RestaurantApp.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, title) = exception switch
            {
                NotFoundException          => (StatusCodes.Status404NotFound,          "Not Found"),
                ConflictException          => (StatusCodes.Status409Conflict,          "Conflict"),
                UnauthorizedException      => (StatusCodes.Status401Unauthorized,      "Unauthorized"),
                ForbiddenException         => (StatusCodes.Status403Forbidden,         "Forbidden"),
                AppValidationException     => (StatusCodes.Status400BadRequest,        "Validation Error"),
                AccountLockedException     => (StatusCodes.Status423Locked,            "Account Locked"),
                _                          => (StatusCodes.Status500InternalServerError, "Internal Server Error")
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
            }
            else
            {
                _logger.LogWarning("Handled exception [{StatusCode}]: {Message}", statusCode, exception.Message);
            }

            var problem = new
            {
                type = $"https://httpstatuses.com/{statusCode}",
                title,
                status = statusCode,
                detail = exception.Message,
                traceId = context.TraceIdentifier
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            );
        }
    }
}
