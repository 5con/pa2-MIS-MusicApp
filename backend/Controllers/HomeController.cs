using Microsoft.AspNetCore.Mvc;
using FreelanceMusicPlatform.Services;
using FreelanceMusicPlatform.Models;

namespace FreelanceMusicPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly DatabaseService _databaseService;
        private readonly AuthenticationService _authenticationService;
        private readonly LoggingService _loggingService;

        public HomeController(DatabaseService databaseService, AuthenticationService authenticationService, LoggingService loggingService)
        {
            _databaseService = databaseService;
            _authenticationService = authenticationService;
            _loggingService = loggingService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                _loggingService.LogInfo($"Login attempt for email: {request.Email}");

                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    _loggingService.LogWarning("Login failed: Missing email or password", new { email = request.Email });
                    return BadRequest(CreateErrorResponse("Email and password are required.", "MISSING_CREDENTIALS"));
                }

                var user = _authenticationService.AuthenticateUser(request.Email, request.Password);

                if (user == null)
                {
                    _loggingService.LogWarning($"Login failed: Invalid credentials for email: {request.Email}");
                    return Unauthorized(CreateErrorResponse("Invalid email or password.", "INVALID_CREDENTIALS"));
                }

                _loggingService.LogInfo($"Login successful for user: {user.Name} (ID: {user.UserId}, Role: {user.Role})");

                return Ok(new
                {
                    success = true,
                    user = new
                    {
                        id = user.UserId,
                        name = user.Name,
                        role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Unexpected error during login for email: {request.Email}", ex);
                return StatusCode(500, CreateErrorResponse("An unexpected error occurred during login.", "LOGIN_ERROR", ex.Message));
            }
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            try
            {
                _loggingService.LogInfo($"Registration attempt for email: {request.Email}, role: {request.Role}");

                if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    _loggingService.LogWarning("Registration failed: Missing required fields", new { email = request.Email });
                    return BadRequest(CreateErrorResponse("Name, email, and password are required.", "MISSING_FIELDS"));
                }

                if (request.Password.Length < 6)
                {
                    _loggingService.LogWarning($"Registration failed: Password too short for email: {request.Email}");
                    return BadRequest(CreateErrorResponse("Password must be at least 6 characters long.", "PASSWORD_TOO_SHORT"));
                }

                if (string.IsNullOrWhiteSpace(request.Role) || (request.Role != "Teacher" && request.Role != "Student" && request.Role != "Admin"))
                {
                    _loggingService.LogWarning($"Registration failed: Invalid role '{request.Role}' for email: {request.Email}");
                    return BadRequest(CreateErrorResponse("Role must be 'Teacher', 'Student', or 'Admin'.", "INVALID_ROLE"));
                }

                if (string.IsNullOrWhiteSpace(request.Instrument))
                {
                    _loggingService.LogWarning($"Registration failed: Missing instrument for email: {request.Email}");
                    return BadRequest(CreateErrorResponse("Instrument is required.", "MISSING_INSTRUMENT"));
                }

                if (request.Role == "Student" && string.IsNullOrWhiteSpace(request.ReferralSource))
                {
                    _loggingService.LogWarning($"Registration failed: Missing referral source for student: {request.Email}");
                    return BadRequest(CreateErrorResponse("Referral source is required for students.", "MISSING_REFERRAL_SOURCE"));
                }

                var registered = _authenticationService.RegisterUser(request.Name, request.Email, request.Password, request.Role,
                    request.ContactInfo, request.Instrument, request.Bio, request.ReferralSource);

                if (!registered)
                {
                    _loggingService.LogWarning($"Registration failed: Email already exists: {request.Email}");
                    return Conflict(CreateErrorResponse("This email is already registered. Please log in instead.", "EMAIL_EXISTS"));
                }

                _loggingService.LogInfo($"Registration successful for email: {request.Email}, role: {request.Role}");
                return Ok(new { success = true, message = "Account created successfully! Please log in." });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Unexpected error during registration for email: {request.Email}", ex, new { role = request.Role });
                return StatusCode(500, CreateErrorResponse("An unexpected error occurred during registration.", "REGISTRATION_ERROR", ex.Message));
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            try
            {
                _loggingService.LogInfo("User logged out");
                return Ok(new { success = true, message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error during logout", ex);
                return StatusCode(500, CreateErrorResponse("An unexpected error occurred during logout.", "LOGOUT_ERROR", ex.Message));
            }
        }

        private object CreateErrorResponse(string message, string errorCode, string? technicalDetails = null)
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? "N/A";
            return new
            {
                success = false,
                message = message,
                errorCode = errorCode,
                error = technicalDetails ?? message,
                correlationId = correlationId,
                timestamp = DateTime.UtcNow.ToString("o"),
                path = $"{HttpContext.Request.Method} {HttpContext.Request.Path}"
            };
        }
    }

    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class RegisterRequest
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Role { get; set; }
        public string? ContactInfo { get; set; }
        public required string Instrument { get; set; }
        public string? Bio { get; set; }
        public string? ReferralSource { get; set; }
    }
}
