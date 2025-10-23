namespace FreelanceMusicPlatform.Middleware
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private const string CorrelationIdHeader = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Generate or retrieve correlation ID
            var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                ?? Guid.NewGuid().ToString();

            // Store in HttpContext for access throughout the request pipeline
            context.Items["CorrelationId"] = correlationId;

            // Add to response headers
            context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);

            // Log request details
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n=== Request Started ===");
            Console.ResetColor();
            Console.WriteLine($"CorrelationId: {correlationId}");
            Console.WriteLine($"Method: {context.Request.Method}");
            Console.WriteLine($"Path: {context.Request.Path}");
            Console.WriteLine($"QueryString: {context.Request.QueryString}");
            Console.WriteLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}");

            // Get user info if available
            if (context.Request.Headers.ContainsKey("X-User-Id"))
            {
                Console.WriteLine($"User-ID: {context.Request.Headers["X-User-Id"]}");
            }

            await _next(context);

            // Log response
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"=== Request Completed ===");
            Console.ResetColor();
            Console.WriteLine($"CorrelationId: {correlationId}");
            Console.WriteLine($"Status Code: {context.Response.StatusCode}");
            Console.WriteLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}\n");
        }
    }

    public static class CorrelationIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
}

