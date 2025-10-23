using BCrypt.Net;
using FreelanceMusicPlatform.Models;

namespace FreelanceMusicPlatform.Services
{
    public class AuthenticationService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggingService _loggingService;

        public AuthenticationService(DatabaseService databaseService, LoggingService loggingService)
        {
            _databaseService = databaseService;
            _loggingService = loggingService;
        }

        public User? AuthenticateUser(string email, string password)
        {
            try
            {
                _loggingService.LogInfo($"Authenticating user: {email}");

                var user = _databaseService.GetUserByEmail(email);
                if (user == null)
                {
                    _loggingService.LogWarning($"User not found for email: {email}");
                    return null;
                }

                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    _loggingService.LogWarning($"Password verification failed for email: {email}");
                    return null;
                }

                _loggingService.LogInfo($"User authenticated successfully: {email} (ID: {user.UserId})");
                return user;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error authenticating user: {email}", ex);
                throw;
            }
        }

        public bool RegisterUser(string name, string email, string password, string role, string? contactInfo,
            string? instrument, string? bio, string? referralSource)
        {
            try
            {
                _loggingService.LogInfo($"Registering user: {email}, role: {role}");

                // Check if user already exists
                if (_databaseService.GetUserByEmail(email) != null)
                {
                    _loggingService.LogWarning($"Registration failed: Email already exists: {email}");
                    return false;
                }

                // Hash password using BCrypt
                _loggingService.LogInfo($"Hashing password for user: {email}");
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                // Create user
                var user = new User
                {
                    Role = role,
                    Name = name,
                    Email = email,
                    PasswordHash = passwordHash,
                    ContactInfo = contactInfo,
                    CreatedAt = DateTime.UtcNow
                };

                _databaseService.InsertUser(user);
                _loggingService.LogInfo($"User inserted: {email}");

                // Get the created user
                var createdUser = _databaseService.GetUserByEmail(email);
                if (createdUser == null)
                {
                    _loggingService.LogError($"Failed to retrieve created user: {email}");
                    return false;
                }

                // Create profile based on role
                if (role == "Teacher")
                {
                    var teacherProfile = new TeacherProfile
                    {
                        UserId = createdUser.UserId,
                        InstrumentTaught = instrument ?? "Guitar",
                        Bio = bio,
                        CustomLessonRate = null // Use default rate initially
                    };
                    _databaseService.InsertTeacherProfile(teacherProfile);
                    _loggingService.LogInfo($"Teacher profile created for user ID: {createdUser.UserId}");
                }
                else if (role == "Student")
                {
                    var studentProfile = new StudentProfile
                    {
                        UserId = createdUser.UserId,
                        InstrumentInterest = instrument ?? "Guitar",
                        ReferralSource = referralSource ?? "Other"
                    };
                    _databaseService.InsertStudentProfile(studentProfile);
                    _loggingService.LogInfo($"Student profile created for user ID: {createdUser.UserId}");
                }

                _loggingService.LogInfo($"User registered successfully: {email} (ID: {createdUser.UserId})");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error registering user: {email}", ex, new { role = role });
                throw;
            }
        }
    }
}
