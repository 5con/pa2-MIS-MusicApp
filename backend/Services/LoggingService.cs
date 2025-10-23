using System.Diagnostics;

namespace FreelanceMusicPlatform.Services
{
    public class LoggingService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoggingService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void LogInfo(string message, object? context = null)
        {
            var correlationId = GetCorrelationId();
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"[{timestamp}] ");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("[INFO] ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"[CorrelationId: {correlationId}] ");
            Console.ResetColor();
            Console.WriteLine(message);

            if (context != null)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  Context: {System.Text.Json.JsonSerializer.Serialize(context)}");
                Console.ResetColor();
            }
        }

        public void LogWarning(string message, object? context = null)
        {
            var correlationId = GetCorrelationId();
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"[{timestamp}] ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[WARNING] ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"[CorrelationId: {correlationId}] ");
            Console.ResetColor();
            Console.WriteLine(message);

            if (context != null)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  Context: {System.Text.Json.JsonSerializer.Serialize(context)}");
                Console.ResetColor();
            }
        }

        public void LogError(string message, Exception? exception = null, object? context = null)
        {
            var correlationId = GetCorrelationId();
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"[{timestamp}] ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[ERROR] ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"[CorrelationId: {correlationId}] ");
            Console.ResetColor();
            Console.WriteLine(message);

            if (context != null)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  Context: {System.Text.Json.JsonSerializer.Serialize(context)}");
                Console.ResetColor();
            }

            if (exception != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Exception: {exception.GetType().Name}");
                Console.WriteLine($"  Message: {exception.Message}");
                Console.WriteLine($"  Stack Trace:");
                Console.WriteLine(exception.StackTrace);

                if (exception.InnerException != null)
                {
                    Console.WriteLine($"  Inner Exception: {exception.InnerException.GetType().Name}");
                    Console.WriteLine($"  Inner Message: {exception.InnerException.Message}");
                }
                Console.ResetColor();
            }
        }

        public void LogDatabaseOperation(string operation, string tableName, object? parameters = null)
        {
            var correlationId = GetCorrelationId();
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"[{timestamp}] ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("[DATABASE] ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"[CorrelationId: {correlationId}] ");
            Console.ResetColor();
            Console.WriteLine($"{operation} on {tableName}");

            if (parameters != null)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  Parameters: {System.Text.Json.JsonSerializer.Serialize(parameters)}");
                Console.ResetColor();
            }
        }

        private string GetCorrelationId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Items.ContainsKey("CorrelationId") == true)
            {
                return httpContext.Items["CorrelationId"]?.ToString() ?? "N/A";
            }
            return "N/A";
        }
    }
}

