using Microsoft.AspNetCore.Mvc;
using FreelanceMusicPlatform.Services;
using FreelanceMusicPlatform.Models;

namespace FreelanceMusicPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherController : ControllerBase
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggingService _loggingService;

        public TeacherController(DatabaseService databaseService, LoggingService loggingService)
        {
            _databaseService = databaseService;
            _loggingService = loggingService;
        }

        [HttpGet("dashboard/{teacherId}")]
        public IActionResult GetDashboard(int teacherId)
        {
            var teacherProfile = _databaseService.GetTeacherProfile(teacherId);
            if (teacherProfile == null)
                return NotFound(new { success = false, message = "Teacher profile not found." });

            var upcomingLessons = _databaseService.GetLessonsByTeacher(teacherId)
                .Where(l => l.StartDateTime > DateTime.Now)
                .OrderBy(l => l.StartDateTime)
                .Take(5)
                .ToList();

            return Ok(new
            {
                success = true,
                dashboard = new
                {
                    teacherProfile,
                    upcomingLessons
                }
            });
        }

        [HttpGet("availability/{teacherId}")]
        public IActionResult GetAvailability(int teacherId)
        {
            var availabilities = _databaseService.GetAvailabilitiesByTeacher(teacherId);
            return Ok(new { success = true, availabilities });
        }

        [HttpPost("availability")]
        public IActionResult AddAvailability([FromBody] AddAvailabilityRequest request)
        {
            try
            {
                _loggingService.LogInfo($"Adding availability for teacher ID: {request.TeacherId}", new { startDateTime = request.StartDateTime, duration = request.Duration });

                // Validate duration (must be between 15 and 480 minutes)
                if (request.Duration < 15 || request.Duration > 480)
                {
                    _loggingService.LogWarning($"Invalid duration for teacher ID: {request.TeacherId}, duration: {request.Duration}");
                    return BadRequest(CreateErrorResponse("Duration must be between 15 and 480 minutes.", "INVALID_DURATION"));
                }

                // Check for conflicts
                var existingAvailabilities = _databaseService.GetAvailabilitiesByTeacher(request.TeacherId);
                var endTime = request.StartDateTime.AddMinutes(request.Duration);

                foreach (var existingAvailability in existingAvailabilities)
                {
                    var existingEndTime = existingAvailability.StartDateTime.AddMinutes(existingAvailability.Duration);
                    if (request.StartDateTime < existingEndTime && endTime > existingAvailability.StartDateTime)
                    {
                        _loggingService.LogWarning($"Time slot conflict for teacher ID: {request.TeacherId}", new { requestedTime = request.StartDateTime, conflictingAvailability = existingAvailability.AvailabilityId });
                        return BadRequest(CreateErrorResponse("This time slot conflicts with an existing availability.", "TIME_CONFLICT"));
                    }
                }

                // Check if time is in the past
                if (request.StartDateTime <= DateTime.Now)
                {
                    _loggingService.LogWarning($"Attempted to schedule in the past for teacher ID: {request.TeacherId}");
                    return BadRequest(CreateErrorResponse("Cannot schedule availability in the past.", "PAST_TIME"));
                }

                var availability = new Availability
                {
                    TeacherId = request.TeacherId,
                    StartDateTime = request.StartDateTime,
                    Duration = request.Duration,
                    CreatedAt = DateTime.UtcNow
                };

                _databaseService.InsertAvailability(availability);
                _loggingService.LogInfo($"Availability added successfully - ID: {availability.AvailabilityId}, Teacher: {request.TeacherId}");

                return Ok(new
                {
                    success = true,
                    message = "Availability added successfully.",
                    availability = new
                    {
                        id = availability.AvailabilityId,
                        startDateTime = availability.StartDateTime,
                        duration = availability.Duration
                    }
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error adding availability for teacher ID: {request.TeacherId}", ex);
                return StatusCode(500, CreateErrorResponse("Failed to add availability.", "ADD_AVAILABILITY_ERROR", ex.Message));
            }
        }

        [HttpDelete("availability/{availabilityId}")]
        public IActionResult RemoveAvailability(int availabilityId, int teacherId)
        {
            var existingAvailability = _databaseService.GetAvailabilitiesByTeacher(teacherId)
                .FirstOrDefault(a => a.AvailabilityId == availabilityId);

            if (existingAvailability == null)
            {
                return NotFound(new { success = false, message = "Availability not found." });
            }

            _databaseService.DeleteAvailability(availabilityId);
            return Ok(new { success = true, message = "Availability removed successfully." });
        }

        [HttpGet("lessons/{teacherId}")]
        public IActionResult GetLessons(int teacherId, string dateFilter = "")
        {
            var lessons = _databaseService.GetLessonsByTeacher(teacherId);

            // Apply date filter if provided
            if (!string.IsNullOrEmpty(dateFilter))
            {
                if (DateTime.TryParse(dateFilter, out var filterDate))
                {
                    lessons = lessons.Where(l => l.StartDateTime.Date == filterDate.Date).ToList();
                }
            }

            return Ok(new { success = true, lessons });
        }

        [HttpGet("profile/{userId}")]
        public IActionResult GetProfile(int userId)
        {
            var profile = _databaseService.GetTeacherProfile(userId);
            if (profile == null)
                return NotFound(new { success = false, message = "Profile not found." });

            return Ok(new
            {
                success = true,
                profile = new
                {
                    profile.TeacherProfileId,
                    profile.UserId,
                    profile.InstrumentTaught,
                    profile.Bio,
                    profile.CustomLessonRate,
                    effectiveRate = _databaseService.GetEffectiveLessonRate(userId)
                }
            });
        }

        [HttpPut("profile/{userId}")]
        public IActionResult UpdateProfile(int userId, [FromBody] UpdateProfileRequest request)
        {
            var profile = _databaseService.GetTeacherProfile(userId);
            if (profile == null)
                return NotFound(new { success = false, message = "Profile not found." });

            // Convert array of instruments to comma-separated string
            if (request.InstrumentsTaught != null && request.InstrumentsTaught.Length > 0)
            {
                profile.InstrumentTaught = string.Join(",", request.InstrumentsTaught);
            }
            else if (request.InstrumentTaught != null)
            {
                // Fallback for backward compatibility with single instrument
                profile.InstrumentTaught = request.InstrumentTaught;
            }

            profile.Bio = request.Bio;
            profile.CustomLessonRate = request.CustomLessonRate;

            _databaseService.UpdateTeacherProfile(profile);
            return Ok(new { success = true, message = "Profile updated successfully." });
        }

        [HttpDelete("lesson/{lessonId}")]
        public IActionResult CancelLesson(int lessonId, int teacherId)
        {
            try
            {
                _loggingService.LogInfo($"Teacher {teacherId} cancelling lesson {lessonId}");

                // Verify the lesson belongs to this teacher
                var lessons = _databaseService.GetLessonsByTeacher(teacherId);
                var lesson = lessons.FirstOrDefault(l => l.LessonId == lessonId);

                if (lesson == null)
                {
                    _loggingService.LogWarning($"Lesson {lessonId} not found for teacher {teacherId}");
                    return NotFound(CreateErrorResponse("Lesson not found or you don't have permission to cancel it.", "LESSON_NOT_FOUND"));
                }

                if (lesson.Status == "Cancelled")
                {
                    _loggingService.LogWarning($"Lesson {lessonId} is already cancelled");
                    return BadRequest(CreateErrorResponse("This lesson is already cancelled.", "ALREADY_CANCELLED"));
                }

                _databaseService.CancelLesson(lessonId);
                _loggingService.LogInfo($"Lesson {lessonId} cancelled successfully by teacher {teacherId}");

                return Ok(new { success = true, message = "Lesson cancelled successfully." });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error cancelling lesson {lessonId} for teacher {teacherId}", ex);
                return StatusCode(500, CreateErrorResponse("Failed to cancel lesson.", "CANCEL_ERROR", ex.Message));
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

    public class AddAvailabilityRequest
    {
        public int TeacherId { get; set; }
        public DateTime StartDateTime { get; set; }
        public int Duration { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string[]? InstrumentsTaught { get; set; }
        public string? InstrumentTaught { get; set; } // Backward compatibility
        public string? Bio { get; set; }
        public decimal? CustomLessonRate { get; set; }
    }
}
