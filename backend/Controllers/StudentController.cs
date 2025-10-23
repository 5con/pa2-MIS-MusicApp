using Microsoft.AspNetCore.Mvc;
using FreelanceMusicPlatform.Services;
using FreelanceMusicPlatform.Models;

namespace FreelanceMusicPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentController : ControllerBase
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggingService _loggingService;

        public StudentController(DatabaseService databaseService, LoggingService loggingService)
        {
            _databaseService = databaseService;
            _loggingService = loggingService;
        }

        [HttpGet("dashboard/{studentId}")]
        public IActionResult GetDashboard(int studentId)
        {
            try
            {
                _loggingService.LogInfo($"Fetching dashboard for student ID: {studentId}");

                var studentProfile = _databaseService.GetStudentProfile(studentId);
                if (studentProfile == null)
                {
                    _loggingService.LogWarning($"Student profile not found for ID: {studentId}");
                    return NotFound(CreateErrorResponse("Student profile not found.", "PROFILE_NOT_FOUND"));
                }

                var upcomingLessons = _databaseService.GetAllLessons()
                    .Where(l => l.StudentId == studentId && l.StartDateTime > DateTime.Now)
                    .OrderBy(l => l.StartDateTime)
                    .Take(5)
                    .ToList();

                _loggingService.LogInfo($"Dashboard loaded for student ID: {studentId}, {upcomingLessons.Count} upcoming lessons");

                return Ok(new
                {
                    success = true,
                    dashboard = new
                    {
                        studentProfile,
                        upcomingLessons
                    }
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error fetching dashboard for student ID: {studentId}", ex);
                return StatusCode(500, CreateErrorResponse("Failed to load dashboard.", "DASHBOARD_ERROR", ex.Message));
            }
        }

        [HttpGet("availabilities")]
        public IActionResult GetAvailabilities(int? teacherId)
        {
            List<Availability> availabilities;

            if (teacherId.HasValue)
            {
                availabilities = _databaseService.GetAvailabilitiesByTeacher(teacherId.Value);
            }
            else
            {
                availabilities = _databaseService.GetAllAvailabilities();
            }

            // Group by teacher for easier frontend handling
            var availabilitiesByTeacher = availabilities
                .GroupBy(a => a.TeacherId)
                .Select(g =>
                {
                    var teacherProfile = _databaseService.GetTeacherProfile(g.Key);
                    var teacher = g.First().Teacher;

                    return new
                    {
                        Teacher = new
                        {
                            UserId = teacher.UserId,
                            Name = teacher.Name,
                            Email = teacher.Email,
                            InstrumentTaught = teacherProfile?.InstrumentTaught ?? "Unknown"
                        },
                        Availabilities = g.OrderBy(a => a.StartDateTime).ToList()
                    };
                })
                .ToList();

            return Ok(new { success = true, availabilitiesByTeacher });
        }

        [HttpPost("book")]
        public IActionResult BookLesson([FromBody] BookLessonRequest request)
        {
            try
            {
                _loggingService.LogInfo($"Booking lesson - Student ID: {request.StudentId}, Availability ID: {request.AvailabilityId}", new { mode = request.Mode });

                var availability = _databaseService.GetAllAvailabilities()
                    .FirstOrDefault(a => a.AvailabilityId == request.AvailabilityId);

                if (availability == null)
                {
                    _loggingService.LogWarning($"Availability not found: {request.AvailabilityId}");
                    return BadRequest(CreateErrorResponse("Availability not found or no longer available.", "AVAILABILITY_NOT_FOUND"));
                }

                var teacherProfile = _databaseService.GetTeacherProfile(availability.TeacherId);
                if (teacherProfile == null)
                {
                    _loggingService.LogError($"Teacher profile not found for teacher ID: {availability.TeacherId}");
                    return BadRequest(CreateErrorResponse("Teacher profile not found.", "TEACHER_NOT_FOUND"));
                }

                // Get the effective lesson rate (custom or default)
                var lessonRate = _databaseService.GetEffectiveLessonRate(availability.TeacherId);

                // Create lesson
                var lesson = new Lesson
                {
                    TeacherId = availability.TeacherId,
                    StudentId = request.StudentId,
                    Instrument = teacherProfile.InstrumentTaught,
                    StartDateTime = availability.StartDateTime,
                    Duration = availability.Duration,
                    Mode = request.Mode,
                    Price = lessonRate,
                    Status = "Scheduled",
                    SheetMusicPath = request.SheetMusicPath,
                    CreatedAt = DateTime.UtcNow
                };

                _databaseService.InsertLesson(lesson);
                _loggingService.LogInfo($"Lesson created with ID: {lesson.LessonId}");

                // Remove availability
                _databaseService.DeleteAvailability(request.AvailabilityId);
                _loggingService.LogInfo($"Availability {request.AvailabilityId} removed after booking");

                _loggingService.LogInfo($"Lesson booked successfully - Lesson ID: {lesson.LessonId}, Student: {request.StudentId}, Teacher: {availability.TeacherId}");

                return Ok(new
                {
                    success = true,
                    message = $"Lesson booked successfully for {availability.StartDateTime:g}!",
                    lesson = new
                    {
                        id = lesson.LessonId,
                        date = lesson.StartDateTime,
                        duration = lesson.Duration,
                        price = lesson.Price,
                        mode = lesson.Mode
                    }
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error booking lesson for student ID: {request.StudentId}", ex, new { availabilityId = request.AvailabilityId });
                return StatusCode(500, CreateErrorResponse("Failed to book lesson.", "BOOKING_ERROR", ex.Message));
            }
        }

        [HttpGet("calendar/{teacherId}")]
        public IActionResult GetAvailabilitiesForCalendar(int teacherId, DateTime? startDate, DateTime? endDate)
        {
            var availabilities = _databaseService.GetAvailabilitiesByTeacher(teacherId);

            // Filter by date range if provided
            if (startDate.HasValue)
                availabilities = availabilities.Where(a => a.StartDateTime >= startDate.Value).ToList();

            if (endDate.HasValue)
                availabilities = availabilities.Where(a => a.StartDateTime <= endDate.Value).ToList();

            // Format for calendar display
            var calendarEvents = availabilities.Select(a => new
            {
                id = a.AvailabilityId,
                title = $"Available - {a.Duration}min",
                start = a.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = a.StartDateTime.AddMinutes(a.Duration).ToString("yyyy-MM-ddTHH:mm:ss"),
                backgroundColor = "#ffc107",
                textColor = "black",
                extendedProps = new
                {
                    availabilityId = a.AvailabilityId,
                    duration = a.Duration,
                    teacherId = a.TeacherId
                }
            });

            return Ok(new { success = true, events = calendarEvents });
        }

        [HttpGet("search")]
        public IActionResult SearchByInstrument(string instrument)
        {
            var availabilities = _databaseService.GetAllAvailabilities()
                .Where(a => a.Teacher != null && _databaseService.GetTeacherProfile(a.TeacherId)?.InstrumentTaught?.Equals(instrument, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            // Group by teacher for easier display
            var results = availabilities
                .GroupBy(a => a.TeacherId)
                .Select(g => new
                {
                    Teacher = g.First().Teacher,
                    Availabilities = g.OrderBy(a => a.StartDateTime).ToList()
                })
                .ToList();

            return Ok(new { success = true, results });
        }

        [HttpPost("book-recurring")]
        public IActionResult BookRecurringLessons([FromBody] BookRecurringRequest request)
        {
            var availability = _databaseService.GetAllAvailabilities()
                .FirstOrDefault(a => a.AvailabilityId == request.AvailabilityId);

            if (availability == null)
            {
                return BadRequest(new { success = false, message = "Availability not found or no longer available." });
            }

            var teacherProfile = _databaseService.GetTeacherProfile(availability.TeacherId);
            if (teacherProfile == null)
            {
                return BadRequest(new { success = false, message = "Teacher profile not found." });
            }

            var lessonRate = _databaseService.GetEffectiveLessonRate(availability.TeacherId);
            var createdLessons = new List<Lesson>();

            // Create recurring series ID
            var seriesId = new Random().Next(100000, 999999);

            for (int i = 0; i < request.Occurrences; i++)
            {
                var lessonDateTime = availability.StartDateTime.AddDays(i * 7 * request.IntervalWeeks);

                // Check if this time slot conflicts with existing lessons
                var existingLessons = _databaseService.GetLessonsByTeacher(availability.TeacherId)
                    .Where(l => l.StartDateTime == lessonDateTime && l.Duration == availability.Duration)
                    .ToList();

                if (existingLessons.Any())
                {
                    return BadRequest(new { success = false, message = $"Conflict detected for occurrence {i + 1}. Series creation stopped." });
                }

                var lesson = new Lesson
                {
                    TeacherId = availability.TeacherId,
                    StudentId = request.StudentId,
                    Instrument = teacherProfile.InstrumentTaught,
                    StartDateTime = lessonDateTime,
                    Duration = availability.Duration,
                    Mode = request.Mode,
                    Price = lessonRate,
                    Status = "Scheduled",
                    RecurringSeriesId = seriesId,
                    CreatedAt = DateTime.UtcNow
                };

                _databaseService.InsertLesson(lesson);
                createdLessons.Add(lesson);
            }

            // Remove the original availability
            _databaseService.DeleteAvailability(request.AvailabilityId);

            return Ok(new
            {
                success = true,
                message = $"Created {request.Occurrences} recurring lessons successfully!",
                seriesId,
                lessons = createdLessons.Select(l => new
                {
                    id = l.LessonId,
                    date = l.StartDateTime.ToString("yyyy-MM-dd HH:mm"),
                    price = l.Price
                })
            });
        }

        [HttpPost("validate-payment")]
        public IActionResult ValidatePayment([FromBody] PaymentRequest request)
        {
            // Basic card validation
            var isValid = ValidateCreditCard(request.CardNumber, request.ExpiryDate, request.Cvv);

            return Ok(new
            {
                success = isValid,
                message = isValid ? "Payment method validated successfully." : "Invalid payment method. Please check your card details."
            });
        }

        private bool ValidateCreditCard(string cardNumber, string expiryDate, string cvv)
        {
            // Remove spaces and dashes from card number
            cardNumber = cardNumber.Replace(" ", "").Replace("-", "");

            // Basic format checks
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 13 || cardNumber.Length > 19)
                return false;

            if (string.IsNullOrEmpty(expiryDate) || expiryDate.Length != 5 || !expiryDate.Contains("/"))
                return false;

            if (string.IsNullOrEmpty(cvv) || cvv.Length < 3 || cvv.Length > 4)
                return false;

            // Luhn algorithm for card number validation
            if (!IsValidLuhn(cardNumber))
                return false;

            // Check expiry date (MM/YY format)
            var parts = expiryDate.Split('/');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int month) || !int.TryParse(parts[1], out int year))
                return false;

            if (month < 1 || month > 12)
                return false;

            // Convert 2-digit year to 4-digit year
            year += year >= 50 ? 1900 : 2000;

            var expiry = new DateTime(year, month, 1);
            if (expiry < DateTime.Now)
                return false;

            // CVV validation (basic length check)
            if (cvv.Length != 3 && cvv.Length != 4)
                return false;

            return true;
        }

        private bool IsValidLuhn(string cardNumber)
        {
            var digits = cardNumber.Reverse().Select(c => c - '0').ToArray();
            var sum = 0;

            for (var i = 0; i < digits.Length; i++)
            {
                if (i % 2 == 1)
                {
                    digits[i] *= 2;
                    if (digits[i] > 9)
                        digits[i] -= 9;
                }
                sum += digits[i];
            }

            return sum % 10 == 0;
        }

        [HttpGet("lessons/{studentId}")]
        public IActionResult GetLessons(int studentId, string? status = null)
        {
            try
            {
                _loggingService.LogInfo($"Fetching lessons for student ID: {studentId}, status filter: {status ?? "All"}");

                var lessons = _databaseService.GetLessonsByStudent(studentId, status);

                _loggingService.LogInfo($"Retrieved {lessons.Count} lessons for student ID: {studentId}");

                return Ok(new { success = true, lessons });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error fetching lessons for student ID: {studentId}", ex);
                return StatusCode(500, CreateErrorResponse("Failed to load lessons.", "LESSONS_ERROR", ex.Message));
            }
        }

        [HttpDelete("lesson/{lessonId}")]
        public IActionResult CancelLesson(int lessonId, int studentId)
        {
            try
            {
                _loggingService.LogInfo($"Student {studentId} cancelling lesson {lessonId}");

                // Verify the lesson belongs to this student
                var lessons = _databaseService.GetLessonsByStudent(studentId);
                var lesson = lessons.FirstOrDefault(l => l.LessonId == lessonId);

                if (lesson == null)
                {
                    _loggingService.LogWarning($"Lesson {lessonId} not found for student {studentId}");
                    return NotFound(CreateErrorResponse("Lesson not found or you don't have permission to cancel it.", "LESSON_NOT_FOUND"));
                }

                if (lesson.Status == "Cancelled")
                {
                    _loggingService.LogWarning($"Lesson {lessonId} is already cancelled");
                    return BadRequest(CreateErrorResponse("This lesson is already cancelled.", "ALREADY_CANCELLED"));
                }

                _databaseService.CancelLesson(lessonId);
                _loggingService.LogInfo($"Lesson {lessonId} cancelled successfully by student {studentId}");

                return Ok(new { success = true, message = "Lesson cancelled successfully." });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error cancelling lesson {lessonId} for student {studentId}", ex);
                return StatusCode(500, CreateErrorResponse("Failed to cancel lesson.", "CANCEL_ERROR", ex.Message));
            }
        }

        [HttpPost("sheet-music")]
        public async Task<IActionResult> UploadSheetMusic([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    _loggingService.LogWarning("Sheet music upload attempted with no file");
                    return BadRequest(CreateErrorResponse("No file uploaded.", "NO_FILE"));
                }

                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    _loggingService.LogWarning($"Invalid file type uploaded: {extension}");
                    return BadRequest(CreateErrorResponse("Invalid file type. Only PDF and image files are allowed.", "INVALID_FILE_TYPE"));
                }

                // Create directory if it doesn't exist
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "sheet-music");
                Directory.CreateDirectory(uploadsPath);

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _loggingService.LogInfo($"Sheet music uploaded successfully: {fileName}");

                return Ok(new
                {
                    success = true,
                    message = "Sheet music uploaded successfully.",
                    filePath = $"uploads/sheet-music/{fileName}",
                    fileName = file.FileName
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error uploading sheet music", ex);
                return StatusCode(500, CreateErrorResponse("Failed to upload sheet music.", "UPLOAD_ERROR", ex.Message));
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

    public class BookLessonRequest
    {
        public int AvailabilityId { get; set; }
        public int StudentId { get; set; }
        public required string Mode { get; set; }
        public string? SheetMusicPath { get; set; }
    }

    public class BookRecurringRequest
    {
        public int AvailabilityId { get; set; }
        public int StudentId { get; set; }
        public required string Mode { get; set; }
        public int Occurrences { get; set; }
        public int IntervalWeeks { get; set; }
    }

    public class PaymentRequest
    {
        public required string CardNumber { get; set; }
        public required string ExpiryDate { get; set; }
        public required string Cvv { get; set; }
    }
}
