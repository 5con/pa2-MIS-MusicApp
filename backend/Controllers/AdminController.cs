using Microsoft.AspNetCore.Mvc;
using FreelanceMusicPlatform.Services;
using FreelanceMusicPlatform.Models;

namespace FreelanceMusicPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DatabaseService _databaseService;
        private readonly DummyDataService _dummyDataService;
        private readonly LoggingService _loggingService;

        public AdminController(DatabaseService databaseService, DummyDataService dummyDataService, LoggingService loggingService)
        {
            _databaseService = databaseService;
            _dummyDataService = dummyDataService;
            _loggingService = loggingService;
        }

        [HttpGet("dashboard")]
        public IActionResult GetDashboard()
        {
            try
            {
                _loggingService.LogInfo("Fetching admin dashboard data");

                // Get dashboard data
                var totalLessons = _databaseService.GetAllLessons().Count;
                var totalTeachers = _databaseService.GetAllLessons()
                    .Select(l => l.TeacherId)
                    .Distinct()
                    .Count();
                var totalStudents = _databaseService.GetAllLessons()
                    .Select(l => l.StudentId)
                    .Distinct()
                    .Count();

                // Get quarterly revenue
                var quarterlyRevenue = _databaseService.GetQuarterlyRevenue();

                // Get popular instruments
                var popularInstruments = _databaseService.GetPopularInstruments();

                _loggingService.LogInfo($"Admin dashboard loaded successfully - Lessons: {totalLessons}, Teachers: {totalTeachers}, Students: {totalStudents}");

                return Ok(new
                {
                    success = true,
                    dashboard = new
                    {
                        totalLessons,
                        totalTeachers,
                        totalStudents,
                        quarterlyRevenue,
                        popularInstruments
                    }
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error fetching admin dashboard", ex);
                return StatusCode(500, CreateErrorResponse("Failed to load admin dashboard.", "DASHBOARD_ERROR", ex.Message));
            }
        }

        [HttpGet("lessons")]
        public IActionResult GetLessons(string sortBy = "date", string filterBy = "", string filterValue = "")
        {
            var lessons = _databaseService.GetAllLessons();

            // Apply filters
            if (!string.IsNullOrEmpty(filterBy) && !string.IsNullOrEmpty(filterValue))
            {
                lessons = filterBy switch
                {
                    "teacher" => lessons.Where(l => l.Teacher.Name.Contains(filterValue, StringComparison.OrdinalIgnoreCase)).ToList(),
                    "student" => lessons.Where(l => l.Student.Name.Contains(filterValue, StringComparison.OrdinalIgnoreCase)).ToList(),
                    "instrument" => lessons.Where(l => l.Instrument.Contains(filterValue, StringComparison.OrdinalIgnoreCase)).ToList(),
                    _ => lessons
                };
            }

            // Apply sorting
            lessons = sortBy switch
            {
                "date" => lessons.OrderBy(l => l.StartDateTime).ToList(),
                "date_desc" => lessons.OrderByDescending(l => l.StartDateTime).ToList(),
                "teacher" => lessons.OrderBy(l => l.Teacher.Name).ToList(),
                "student" => lessons.OrderBy(l => l.Student.Name).ToList(),
                "instrument" => lessons.OrderBy(l => l.Instrument).ToList(),
                _ => lessons.OrderBy(l => l.StartDateTime).ToList()
            };

            // Get unique values for filter dropdowns
            var teachers = lessons.Select(l => l.Teacher.Name).Distinct().OrderBy(n => n).ToList();
            var students = lessons.Select(l => l.Student.Name).Distinct().OrderBy(n => n).ToList();
            var instruments = lessons.Select(l => l.Instrument).Distinct().OrderBy(i => i).ToList();

            return Ok(new
            {
                success = true,
                lessons,
                filters = new
                {
                    teachers,
                    students,
                    instruments
                },
                pagination = new
                {
                    currentSort = sortBy,
                    currentFilterBy = filterBy,
                    currentFilterValue = filterValue,
                    totalCount = lessons.Count
                }
            });
        }

        [HttpGet("reports")]
        public IActionResult GetReports()
        {
            // Get quarterly revenue
            var quarterlyRevenue = _databaseService.GetQuarterlyRevenue();

            // Get referral breakdown
            var referralBreakdown = _databaseService.GetReferralBreakdown();

            // Get popular instruments
            var popularInstruments = _databaseService.GetPopularInstruments();

            // Get user metrics
            var userMetrics = CalculateUserMetrics();

            // Get repeat booking rate
            var repeatBookingRate = CalculateRepeatBookingRate();

            return Ok(new
            {
                success = true,
                quarterlyRevenue,
                referralBreakdown,
                popularInstruments,
                userMetrics,
                repeatBookingRate
            });
        }

        [HttpGet("user-metrics")]
        public IActionResult GetUserMetrics()
        {
            // Count teachers and students
            var allLessons = _databaseService.GetAllLessons();
            var totalTeachers = allLessons.Select(l => l.TeacherId).Distinct().Count();
            var totalStudents = allLessons.Select(l => l.StudentId).Distinct().Count();

            return Ok(new
            {
                success = true,
                totalTeachers,
                totalStudents,
                totalUsers = totalTeachers + totalStudents
            });
        }

        [HttpGet("repeat-booking-rate")]
        public IActionResult GetRepeatBookingRate()
        {
            // Get all lessons grouped by student
            var lessonsByStudent = _databaseService.GetAllLessons()
                .GroupBy(l => l.StudentId)
                .ToList();

            var totalStudentsWithLessons = lessonsByStudent.Count();
            var studentsWithMultipleLessons = lessonsByStudent.Count(g => g.Count() > 1);

            var repeatRate = totalStudentsWithLessons > 0
                ? (double)studentsWithMultipleLessons / totalStudentsWithLessons * 100
                : 0;

            return Ok(new
            {
                success = true,
                totalStudentsWithLessons,
                studentsWithMultipleLessons,
                repeatRate = Math.Round(repeatRate, 2)
            });
        }

        [HttpGet("calendar-events")]
        public IActionResult GetLessonsForCalendar(DateTime? startDate, DateTime? endDate)
        {
            var lessons = _databaseService.GetAllLessons();

            // Filter by date range if provided
            if (startDate.HasValue)
                lessons = lessons.Where(l => l.StartDateTime >= startDate.Value).ToList();

            if (endDate.HasValue)
                lessons = lessons.Where(l => l.StartDateTime <= endDate.Value).ToList();

            // Format for calendar display
            var calendarEvents = lessons.Select(l => new
            {
                id = l.LessonId,
                title = $"{l.Student.Name} ({l.Instrument} with {l.Teacher.Name})",
                start = l.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = l.StartDateTime.AddMinutes(l.Duration).ToString("yyyy-MM-ddTHH:mm:ss"),
                backgroundColor = l.Mode == "Virtual" ? "#007bff" : "#28a745",
                textColor = "white",
                extendedProps = new
                {
                    mode = l.Mode,
                    duration = l.Duration,
                    price = l.Price,
                    status = l.Status
                }
            });

            return Ok(new { success = true, events = calendarEvents });
        }

        [HttpGet("revenue-distribution")]
        public IActionResult GetRevenueDistribution()
        {
            var lessons = _databaseService.GetAllLessons()
                .Where(l => l.Status != "Cancelled")
                .ToList();

            // Revenue by instrument
            var instrumentRevenue = lessons
                .GroupBy(l => l.Instrument)
                .Select(g => new
                {
                    Instrument = g.Key,
                    Revenue = g.Sum(l => l.Price),
                    LessonCount = g.Count()
                })
                .OrderByDescending(r => r.Revenue)
                .ToList();

            // Revenue by student
            var studentRevenue = lessons
                .GroupBy(l => l.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    StudentName = g.First().Student.Name,
                    Revenue = g.Sum(l => l.Price),
                    LessonCount = g.Count()
                })
                .OrderByDescending(r => r.Revenue)
                .ToList();

            // Calculate 50% cutoffs
            var totalRevenue = lessons.Sum(l => l.Price);

            var instrumentsFor50Percent = new List<string>();
            var instrumentsRevenueSum = 0m;
            foreach (var item in instrumentRevenue)
            {
                instrumentsRevenueSum += item.Revenue;
                instrumentsFor50Percent.Add(item.Instrument);
                if (instrumentsRevenueSum >= totalRevenue * 0.5m)
                    break;
            }

            var studentsFor50Percent = new List<string>();
            var studentsRevenueSum = 0m;
            foreach (var item in studentRevenue)
            {
                studentsRevenueSum += item.Revenue;
                studentsFor50Percent.Add(item.StudentName);
                if (studentsRevenueSum >= totalRevenue * 0.5m)
                    break;
            }

            return Ok(new
            {
                success = true,
                totalRevenue,
                instrumentDistribution = new
                {
                    instruments = instrumentRevenue,
                    instrumentsFor50Percent,
                    instrumentsCount = instrumentsFor50Percent.Count,
                    instrumentsRevenue = instrumentsRevenueSum
                },
                studentDistribution = new
                {
                    students = studentRevenue,
                    studentsFor50Percent,
                    studentsCount = studentsFor50Percent.Count,
                    studentsRevenue = studentsRevenueSum
                }
            });
        }

        [HttpPost("seed")]
        public IActionResult Seed([FromQuery] int teachers = 5, [FromQuery] int students = 20, [FromQuery] int lessons = 120, [FromQuery] bool clear = false)
        {
            if (clear)
            {
                _databaseService.ClearAllData(preserveAdmin: true);
                return Ok(new { success = true, message = "Database cleared" });
            }

            var seeded = _databaseService.SeedDummyData(teachers, students, lessons);
            return Ok(new { success = true, teachers = seeded.Teachers, students = seeded.Students, lessons = seeded.Lessons });
        }

        [HttpPost("dummy-data")]
        public IActionResult GenerateDummyData([FromQuery] int teachers = 5, [FromQuery] int students = 20, [FromQuery] int lessons = 120, [FromQuery] bool clear = false)
        {
            if (clear)
            {
                _dummyDataService.Clear(preserveAdmin: true);
                return Ok(new { success = true, message = "Database cleared" });
            }

            var seeded = _dummyDataService.Generate(teachers, students, lessons);
            return Ok(new { success = true, teachers = seeded.Teachers, students = seeded.Students, lessons = seeded.Lessons });
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

        private object CalculateUserMetrics()
        {
            var allLessons = _databaseService.GetAllLessons();
            var totalTeachers = allLessons.Select(l => l.TeacherId).Distinct().Count();
            var totalStudents = allLessons.Select(l => l.StudentId).Distinct().Count();

            return new
            {
                totalTeachers,
                totalStudents,
                totalUsers = totalTeachers + totalStudents
            };
        }

        private object CalculateRepeatBookingRate()
        {
            var lessonsByStudent = _databaseService.GetAllLessons()
                .GroupBy(l => l.StudentId)
                .ToList();

            var totalStudentsWithLessons = lessonsByStudent.Count();
            var studentsWithMultipleLessons = lessonsByStudent.Count(g => g.Count() > 1);

            var repeatRate = totalStudentsWithLessons > 0
                ? (double)studentsWithMultipleLessons / totalStudentsWithLessons * 100
                : 0;

            return new
            {
                totalStudentsWithLessons,
                studentsWithMultipleLessons,
                repeatRate = Math.Round(repeatRate, 2)
            };
        }
    }
}
