using System.Net;
using System.Text.Json;

namespace FreelanceMusicPlatform.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
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

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "N/A";
            var timestamp = DateTime.UtcNow;
            var path = context.Request.Path;
            var method = context.Request.Method;

            // Log the exception with full details
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n!!! UNHANDLED EXCEPTION !!!");
            Console.ResetColor();
            Console.WriteLine($"CorrelationId: {correlationId}");
            Console.WriteLine($"Timestamp: {timestamp:yyyy-MM-dd HH:mm:ss.fff}");
            Console.WriteLine($"Path: {method} {path}");
            Console.WriteLine($"Exception Type: {exception.GetType().Name}");
            Console.WriteLine($"Message: {exception.Message}");
            Console.WriteLine($"Stack Trace:");
            Console.WriteLine(exception.StackTrace);

            if (exception.InnerException != null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\nInner Exception:");
                Console.ResetColor();
                Console.WriteLine($"Type: {exception.InnerException.GetType().Name}");
                Console.WriteLine($"Message: {exception.InnerException.Message}");
                Console.WriteLine($"Stack Trace:");
                Console.WriteLine(exception.InnerException.StackTrace);
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"!!! END EXCEPTION !!!\n");
            Console.ResetColor();

            // Determine status code based on exception type
            var statusCode = exception switch
            {
                ArgumentException => HttpStatusCode.BadRequest,
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                KeyNotFoundException => HttpStatusCode.NotFound,
                InvalidOperationException => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            // Create error response
            var response = new
            {
                success = false,
                message = GetUserFriendlyMessage(exception),
                error = exception.Message,
                errorType = exception.GetType().Name,
                correlationId = correlationId,
                timestamp = timestamp.ToString("o"),
                path = path.ToString(),
                statusCode = (int)statusCode,
                stackTrace = exception.StackTrace,
                innerException = exception.InnerException != null ? new
                {
                    message = exception.InnerException.Message,
                    type = exception.InnerException.GetType().Name
                } : null
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            return context.Response.WriteAsync(json);
        }

        private static string GetUserFriendlyMessage(Exception exception)
        {
            return exception switch
            {
                ArgumentException => "Invalid input provided. Please check your data and try again.",
                UnauthorizedAccessException => "You are not authorized to perform this action.",
                KeyNotFoundException => "The requested resource was not found.",
                InvalidOperationException => "The operation could not be completed. Please try again.",
                _ => "An unexpected error occurred. Please contact support with the correlation ID."
            };
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}

