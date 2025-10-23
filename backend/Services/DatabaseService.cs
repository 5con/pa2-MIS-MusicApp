using System.Data;
using Microsoft.Data.Sqlite;
using BCrypt.Net;
using FreelanceMusicPlatform.Models;

namespace FreelanceMusicPlatform.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private LoggingService? _loggingService;

        public DatabaseService(IConfiguration? configuration = null)
        {
            _connectionString = configuration?.GetConnectionString("DefaultConnection") ?? "Data Source=freelance_music.db";
            InitializeDatabase();
        }

        public void SetLoggingService(LoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Create Users table
            var createUsersTable = @"
                CREATE TABLE IF NOT EXISTS Users (
                    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Role TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Email TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    ContactInfo TEXT,
                    CreatedAt TEXT NOT NULL
                )";

            // Create TeacherProfiles table
            var createTeacherProfilesTable = @"
                CREATE TABLE IF NOT EXISTS TeacherProfiles (
                    TeacherProfileId INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    InstrumentTaught TEXT NOT NULL,
                    Bio TEXT,
                    CustomLessonRate REAL,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(UserId)
                )";

            // Create StudentProfiles table
            var createStudentProfilesTable = @"
                CREATE TABLE IF NOT EXISTS StudentProfiles (
                    StudentProfileId INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    InstrumentInterest TEXT NOT NULL,
                    ReferralSource TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(UserId)
                )";

            // Create Availabilities table
            var createAvailabilitiesTable = @"
                CREATE TABLE IF NOT EXISTS Availabilities (
                    AvailabilityId INTEGER PRIMARY KEY AUTOINCREMENT,
                    TeacherId INTEGER NOT NULL,
                    StartDateTime TEXT NOT NULL,
                    Duration INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY (TeacherId) REFERENCES Users(UserId)
                )";

            // Create Lessons table
            var createLessonsTable = @"
                CREATE TABLE IF NOT EXISTS Lessons (
                    LessonId INTEGER PRIMARY KEY AUTOINCREMENT,
                    TeacherId INTEGER NOT NULL,
                    StudentId INTEGER NOT NULL,
                    Instrument TEXT NOT NULL,
                    StartDateTime TEXT NOT NULL,
                    Duration INTEGER NOT NULL,
                    Mode TEXT NOT NULL,
                    Price REAL NOT NULL,
                    Status TEXT NOT NULL,
                    RecurringSeriesId INTEGER,
                    SheetMusicPath TEXT,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY (TeacherId) REFERENCES Users(UserId),
                    FOREIGN KEY (StudentId) REFERENCES Users(UserId)
                )";

            using var command = connection.CreateCommand();
            command.CommandText = createUsersTable;
            command.ExecuteNonQuery();

            command.CommandText = createTeacherProfilesTable;
            command.ExecuteNonQuery();

            // Update existing TeacherProfiles to set CustomLessonRate to null
            var updateTeacherProfiles = @"
                ALTER TABLE TeacherProfiles ADD COLUMN CustomLessonRate REAL";

            try
            {
                command.CommandText = updateTeacherProfiles;
                command.ExecuteNonQuery();
            }
            catch (Exception)
            {
                // Column might already exist, ignore error
            }

            command.CommandText = createStudentProfilesTable;
            command.ExecuteNonQuery();

            command.CommandText = createAvailabilitiesTable;
            command.ExecuteNonQuery();

            command.CommandText = createLessonsTable;
            command.ExecuteNonQuery();

            // Insert default admin user if it doesn't exist
            var insertAdmin = @"
                INSERT OR IGNORE INTO Users (Role, Name, Email, PasswordHash, ContactInfo, CreatedAt)
                VALUES ('Admin', 'Admin User', 'admin@freelancemusic.com', @passwordHash, 'admin@freelancemusic.com', datetime('now'))";

            command.CommandText = insertAdmin;
            command.Parameters.Add(new SqliteParameter("@passwordHash", BCrypt.Net.BCrypt.HashPassword("admin123")));
            command.ExecuteNonQuery();
        }

        public IDbConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        // User operations
        public User? GetUserByEmail(string email)
        {
            try
            {
                _loggingService?.LogDatabaseOperation("SELECT", "Users", new { email = email });

                using var connection = GetConnection();
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Users WHERE Email = @Email";
                command.Parameters.Add(new SqliteParameter("@Email", email));

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var user = new User
                    {
                        UserId = reader.GetInt32(0),
                        Role = reader.GetString(1),
                        Name = reader.GetString(2),
                        Email = reader.GetString(3),
                        PasswordHash = reader.GetString(4),
                        ContactInfo = reader.IsDBNull(5) ? null : reader.GetString(5),
                        CreatedAt = DateTime.Parse(reader.GetString(6))
                    };
                    _loggingService?.LogInfo($"User found: {email} (ID: {user.UserId})");
                    return user;
                }

                _loggingService?.LogInfo($"User not found: {email}");
                return null;
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Error getting user by email: {email}", ex);
                throw;
            }
        }

        public User? GetUserById(int userId)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Users WHERE UserId = @UserId";
            command.Parameters.Add(new SqliteParameter("@UserId", userId));

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    UserId = reader.GetInt32(0),
                    Role = reader.GetString(1),
                    Name = reader.GetString(2),
                    Email = reader.GetString(3),
                    PasswordHash = reader.GetString(4),
                    ContactInfo = reader.IsDBNull(5) ? null : reader.GetString(5),
                    CreatedAt = DateTime.Parse(reader.GetString(6))
                };
            }
            return null;
        }

        public void InsertUser(User user)
        {
            try
            {
                _loggingService?.LogDatabaseOperation("INSERT", "Users", new { email = user.Email, role = user.Role });

                using var connection = GetConnection();
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Users (Role, Name, Email, PasswordHash, ContactInfo, CreatedAt)
                    VALUES (@Role, @Name, @Email, @PasswordHash, @ContactInfo, @CreatedAt)";

                command.Parameters.Add(new SqliteParameter("@Role", user.Role));
                command.Parameters.Add(new SqliteParameter("@Name", user.Name));
                command.Parameters.Add(new SqliteParameter("@Email", user.Email));
                command.Parameters.Add(new SqliteParameter("@PasswordHash", user.PasswordHash));
                command.Parameters.Add(new SqliteParameter("@ContactInfo", user.ContactInfo ?? (object)DBNull.Value));
                command.Parameters.Add(new SqliteParameter("@CreatedAt", user.CreatedAt.ToString("o")));

                command.ExecuteNonQuery();
                _loggingService?.LogInfo($"User inserted successfully: {user.Email}");
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Error inserting user: {user.Email}", ex);
                throw;
            }
        }

        // Teacher Profile operations
        public TeacherProfile? GetTeacherProfile(int userId)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT tp.*, u.Name, u.Email
                FROM TeacherProfiles tp
                INNER JOIN Users u ON tp.UserId = u.UserId
                WHERE tp.UserId = @UserId";

            command.Parameters.Add(new SqliteParameter("@UserId", userId));

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new TeacherProfile
                {
                    TeacherProfileId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    InstrumentTaught = reader.GetString(2),
                    Bio = reader.IsDBNull(3) ? null : reader.GetString(3),
                    CustomLessonRate = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                    CreatedAt = DateTime.Parse(reader.GetString(5)),
                    User = new User
                    {
                        UserId = reader.GetInt32(1),
                        Name = reader.GetString(6),
                        Email = reader.GetString(7)
                    }
                };
            }
            return null;
        }

        public void InsertTeacherProfile(TeacherProfile profile)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO TeacherProfiles (UserId, InstrumentTaught, Bio, CustomLessonRate, CreatedAt)
                VALUES (@UserId, @InstrumentTaught, @Bio, @CustomLessonRate, @CreatedAt)";

            command.Parameters.Add(new SqliteParameter("@UserId", profile.UserId));
            command.Parameters.Add(new SqliteParameter("@InstrumentTaught", profile.InstrumentTaught));
            command.Parameters.Add(new SqliteParameter("@Bio", profile.Bio ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@CustomLessonRate", profile.CustomLessonRate.HasValue ? (object)profile.CustomLessonRate.Value : DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@CreatedAt", profile.CreatedAt.ToString("o")));

            command.ExecuteNonQuery();
        }

        public void UpdateTeacherProfile(TeacherProfile profile)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE TeacherProfiles
                SET InstrumentTaught = @InstrumentTaught, Bio = @Bio, CustomLessonRate = @CustomLessonRate
                WHERE UserId = @UserId";

            command.Parameters.Add(new SqliteParameter("@UserId", profile.UserId));
            command.Parameters.Add(new SqliteParameter("@InstrumentTaught", profile.InstrumentTaught));
            command.Parameters.Add(new SqliteParameter("@Bio", profile.Bio ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@CustomLessonRate", profile.CustomLessonRate.HasValue ? (object)profile.CustomLessonRate.Value : DBNull.Value));

            command.ExecuteNonQuery();
        }

        public decimal GetEffectiveLessonRate(int teacherId)
        {
            var profile = GetTeacherProfile(teacherId);
            if (profile?.CustomLessonRate.HasValue == true)
            {
                return profile.CustomLessonRate.Value;
            }
            return 30.00m; // Default rate
        }

        // Student Profile operations
        public StudentProfile? GetStudentProfile(int userId)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT sp.*, u.Name, u.Email
                FROM StudentProfiles sp
                INNER JOIN Users u ON sp.UserId = u.UserId
                WHERE sp.UserId = @UserId";

            command.Parameters.Add(new SqliteParameter("@UserId", userId));

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new StudentProfile
                {
                    StudentProfileId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    InstrumentInterest = reader.GetString(2),
                    ReferralSource = reader.GetString(3),
                    CreatedAt = DateTime.Parse(reader.GetString(4)),
                    User = new User
                    {
                        UserId = reader.GetInt32(1),
                        Name = reader.GetString(5),
                        Email = reader.GetString(6)
                    }
                };
            }
            return null;
        }

        public void InsertStudentProfile(StudentProfile profile)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO StudentProfiles (UserId, InstrumentInterest, ReferralSource, CreatedAt)
                VALUES (@UserId, @InstrumentInterest, @ReferralSource, @CreatedAt)";

            command.Parameters.Add(new SqliteParameter("@UserId", profile.UserId));
            command.Parameters.Add(new SqliteParameter("@InstrumentInterest", profile.InstrumentInterest));
            command.Parameters.Add(new SqliteParameter("@ReferralSource", profile.ReferralSource));
            command.Parameters.Add(new SqliteParameter("@CreatedAt", profile.CreatedAt.ToString("o")));

            command.ExecuteNonQuery();
        }

        // Availability operations
        public List<Availability> GetAvailabilitiesByTeacher(int teacherId)
        {
            var availabilities = new List<Availability>();

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT a.*, u.Name, u.Email, tp.InstrumentTaught
                FROM Availabilities a
                INNER JOIN Users u ON a.TeacherId = u.UserId
                INNER JOIN TeacherProfiles tp ON a.TeacherId = tp.UserId
                WHERE a.TeacherId = @TeacherId AND a.StartDateTime > datetime('now')
                ORDER BY a.StartDateTime";

            command.Parameters.Add(new SqliteParameter("@TeacherId", teacherId));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                availabilities.Add(new Availability
                {
                    AvailabilityId = reader.GetInt32(0),
                    TeacherId = reader.GetInt32(1),
                    StartDateTime = DateTime.Parse(reader.GetString(2)),
                    Duration = reader.GetInt32(3),
                    CreatedAt = DateTime.Parse(reader.GetString(4)),
                    Teacher = new User
                    {
                        UserId = reader.GetInt32(1),
                        Name = reader.GetString(5),
                        Email = reader.GetString(6)
                    }
                });
            }

            return availabilities;
        }

        public List<Availability> GetAllAvailabilities()
        {
            var availabilities = new List<Availability>();

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT a.*, u.Name as TeacherName, u.Email as TeacherEmail, tp.InstrumentTaught
                FROM Availabilities a
                INNER JOIN Users u ON a.TeacherId = u.UserId
                INNER JOIN TeacherProfiles tp ON a.TeacherId = tp.UserId
                WHERE a.StartDateTime > datetime('now')
                ORDER BY a.StartDateTime";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                availabilities.Add(new Availability
                {
                    AvailabilityId = reader.GetInt32(0),
                    TeacherId = reader.GetInt32(1),
                    StartDateTime = DateTime.Parse(reader.GetString(2)),
                    Duration = reader.GetInt32(3),
                    CreatedAt = DateTime.Parse(reader.GetString(4)),
                    Teacher = new User
                    {
                        UserId = reader.GetInt32(1),
                        Name = reader.GetString(5),
                        Email = reader.GetString(6)
                    }
                });
            }

            return availabilities;
        }

        public void InsertAvailability(Availability availability)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Availabilities (TeacherId, StartDateTime, Duration, CreatedAt)
                VALUES (@TeacherId, @StartDateTime, @Duration, @CreatedAt)";

            command.Parameters.Add(new SqliteParameter("@TeacherId", availability.TeacherId));
            command.Parameters.Add(new SqliteParameter("@StartDateTime", availability.StartDateTime.ToString("o")));
            command.Parameters.Add(new SqliteParameter("@Duration", availability.Duration));
            command.Parameters.Add(new SqliteParameter("@CreatedAt", availability.CreatedAt.ToString("o")));

            command.ExecuteNonQuery();
        }

        public void DeleteAvailability(int availabilityId)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Availabilities WHERE AvailabilityId = @AvailabilityId";
            command.Parameters.Add(new SqliteParameter("@AvailabilityId", availabilityId));

            command.ExecuteNonQuery();
        }

        // Lesson operations
        public List<Lesson> GetLessonsByTeacher(int teacherId)
        {
            var lessons = new List<Lesson>();

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT l.*, t.Name as TeacherName, t.Email as TeacherEmail,
                       s.Name as StudentName, s.Email as StudentEmail
                FROM Lessons l
                INNER JOIN Users t ON l.TeacherId = t.UserId
                INNER JOIN Users s ON l.StudentId = s.UserId
                WHERE l.TeacherId = @TeacherId
                ORDER BY l.StartDateTime";

            command.Parameters.Add(new SqliteParameter("@TeacherId", teacherId));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lessons.Add(new Lesson
                {
                    LessonId = reader.GetInt32(0),
                    TeacherId = reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    Instrument = reader.GetString(3),
                    StartDateTime = DateTime.Parse(reader.GetString(4)),
                    Duration = reader.GetInt32(5),
                    Mode = reader.GetString(6),
                    Price = reader.GetDecimal(7),
                    Status = reader.GetString(8),
                    RecurringSeriesId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    SheetMusicPath = reader.IsDBNull(10) ? null : reader.GetString(10),
                    CreatedAt = DateTime.Parse(reader.GetString(11)),
                    Teacher = new User
                    {
                        UserId = reader.GetInt32(1),
                        Name = reader.GetString(12),
                        Email = reader.GetString(13)
                    },
                    Student = new User
                    {
                        UserId = reader.GetInt32(2),
                        Name = reader.GetString(14),
                        Email = reader.GetString(15)
                    }
                });
            }

            return lessons;
        }

        public List<Lesson> GetAllLessons()
        {
            var lessons = new List<Lesson>();

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT l.*, t.Name as TeacherName, t.Email as TeacherEmail,
                       s.Name as StudentName, s.Email as StudentEmail
                FROM Lessons l
                INNER JOIN Users t ON l.TeacherId = t.UserId
                INNER JOIN Users s ON l.StudentId = s.UserId
                ORDER BY l.StartDateTime";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lessons.Add(new Lesson
                {
                    LessonId = reader.GetInt32(0),
                    TeacherId = reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    Instrument = reader.GetString(3),
                    StartDateTime = DateTime.Parse(reader.GetString(4)),
                    Duration = reader.GetInt32(5),
                    Mode = reader.GetString(6),
                    Price = reader.GetDecimal(7),
                    Status = reader.GetString(8),
                    RecurringSeriesId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    SheetMusicPath = reader.IsDBNull(10) ? null : reader.GetString(10),
                    CreatedAt = DateTime.Parse(reader.GetString(11)),
                    Teacher = new User
                    {
                        UserId = reader.GetInt32(1),
                        Name = reader.GetString(12),
                        Email = reader.GetString(13)
                    },
                    Student = new User
                    {
                        UserId = reader.GetInt32(2),
                        Name = reader.GetString(14),
                        Email = reader.GetString(15)
                    }
                });
            }

            return lessons;
        }

        public void InsertLesson(Lesson lesson)
        {
            try
            {
                _loggingService?.LogDatabaseOperation("INSERT", "Lessons", new
                {
                    teacherId = lesson.TeacherId,
                    studentId = lesson.StudentId,
                    startDateTime = lesson.StartDateTime
                });

                using var connection = GetConnection();
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Lessons (TeacherId, StudentId, Instrument, StartDateTime, Duration, Mode, Price, Status, RecurringSeriesId, SheetMusicPath, CreatedAt)
                    VALUES (@TeacherId, @StudentId, @Instrument, @StartDateTime, @Duration, @Mode, @Price, @Status, @RecurringSeriesId, @SheetMusicPath, @CreatedAt)";

                command.Parameters.Add(new SqliteParameter("@TeacherId", lesson.TeacherId));
                command.Parameters.Add(new SqliteParameter("@StudentId", lesson.StudentId));
                command.Parameters.Add(new SqliteParameter("@Instrument", lesson.Instrument));
                command.Parameters.Add(new SqliteParameter("@StartDateTime", lesson.StartDateTime.ToString("o")));
                command.Parameters.Add(new SqliteParameter("@Duration", lesson.Duration));
                command.Parameters.Add(new SqliteParameter("@Mode", lesson.Mode));
                command.Parameters.Add(new SqliteParameter("@Price", lesson.Price));
                command.Parameters.Add(new SqliteParameter("@Status", lesson.Status));
                command.Parameters.Add(new SqliteParameter("@RecurringSeriesId", (object?)lesson.RecurringSeriesId ?? DBNull.Value));
                command.Parameters.Add(new SqliteParameter("@SheetMusicPath", (object?)lesson.SheetMusicPath ?? DBNull.Value));
                command.Parameters.Add(new SqliteParameter("@CreatedAt", lesson.CreatedAt.ToString("o")));

                command.ExecuteNonQuery();

                // Get the last inserted ID
                command.CommandText = "SELECT last_insert_rowid()";
                lesson.LessonId = Convert.ToInt32(command.ExecuteScalar());

                _loggingService?.LogInfo($"Lesson inserted successfully: ID {lesson.LessonId}, Teacher: {lesson.TeacherId}, Student: {lesson.StudentId}");
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Error inserting lesson for Teacher: {lesson.TeacherId}, Student: {lesson.StudentId}", ex);
                throw;
            }
        }

        public void UpdateLessonStatus(int lessonId, string status)
        {
            try
            {
                _loggingService?.LogInfo($"Updating lesson status: Lesson ID {lessonId}, New Status: {status}");

                using var connection = GetConnection();
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE Lessons 
                    SET Status = @Status 
                    WHERE LessonId = @LessonId";

                command.Parameters.Add(new SqliteParameter("@Status", status));
                command.Parameters.Add(new SqliteParameter("@LessonId", lessonId));

                command.ExecuteNonQuery();

                _loggingService?.LogInfo($"Lesson status updated successfully: ID {lessonId}");
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Error updating lesson status for Lesson ID: {lessonId}", ex);
                throw;
            }
        }

        public void CancelLesson(int lessonId)
        {
            UpdateLessonStatus(lessonId, "Cancelled");
        }

        public List<Lesson> GetLessonsByStudent(int studentId, string? statusFilter = null)
        {
            var lessons = new List<Lesson>();

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            var sql = @"
                SELECT l.*, t.Name as TeacherName, t.Email as TeacherEmail,
                       s.Name as StudentName, s.Email as StudentEmail
                FROM Lessons l
                INNER JOIN Users t ON l.TeacherId = t.UserId
                INNER JOIN Users s ON l.StudentId = s.UserId
                WHERE l.StudentId = @StudentId";

            if (!string.IsNullOrEmpty(statusFilter))
            {
                sql += " AND l.Status = @Status";
            }

            sql += " ORDER BY l.StartDateTime DESC";

            command.CommandText = sql;
            command.Parameters.Add(new SqliteParameter("@StudentId", studentId));

            if (!string.IsNullOrEmpty(statusFilter))
            {
                command.Parameters.Add(new SqliteParameter("@Status", statusFilter));
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lessons.Add(new Lesson
                {
                    LessonId = reader.GetInt32(0),
                    TeacherId = reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    Instrument = reader.GetString(3),
                    StartDateTime = DateTime.Parse(reader.GetString(4)),
                    Duration = reader.GetInt32(5),
                    Mode = reader.GetString(6),
                    Price = reader.GetDecimal(7),
                    Status = reader.GetString(8),
                    RecurringSeriesId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    SheetMusicPath = reader.IsDBNull(10) ? null : reader.GetString(10),
                    CreatedAt = DateTime.Parse(reader.GetString(11)),
                    Teacher = new User
                    {
                        UserId = reader.GetInt32(1),
                        Name = reader.GetString(12),
                        Email = reader.GetString(13)
                    },
                    Student = new User
                    {
                        UserId = reader.GetInt32(2),
                        Name = reader.GetString(14),
                        Email = reader.GetString(15)
                    }
                });
            }

            return lessons;
        }

        public void UpdateLessonSheetMusic(int lessonId, string sheetMusicPath)
        {
            try
            {
                _loggingService?.LogInfo($"Updating sheet music for lesson ID {lessonId}");

                using var connection = GetConnection();
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE Lessons 
                    SET SheetMusicPath = @SheetMusicPath 
                    WHERE LessonId = @LessonId";

                command.Parameters.Add(new SqliteParameter("@SheetMusicPath", sheetMusicPath));
                command.Parameters.Add(new SqliteParameter("@LessonId", lessonId));

                command.ExecuteNonQuery();

                _loggingService?.LogInfo($"Sheet music updated successfully for lesson ID {lessonId}");
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Error updating sheet music for Lesson ID: {lessonId}", ex);
                throw;
            }
        }

        // Reporting operations
        public List<object> GetQuarterlyRevenue()
        {
            var revenue = new List<object>();

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    CASE
                        WHEN strftime('%m', StartDateTime) IN ('01', '02', '03') THEN strftime('%Y', StartDateTime) || ' Q1'
                        WHEN strftime('%m', StartDateTime) IN ('04', '05', '06') THEN strftime('%Y', StartDateTime) || ' Q2'
                        WHEN strftime('%m', StartDateTime) IN ('07', '08', '09') THEN strftime('%Y', StartDateTime) || ' Q3'
                        WHEN strftime('%m', StartDateTime) IN ('10', '11', '12') THEN strftime('%Y', StartDateTime) || ' Q4'
                    END as Quarter,
                    SUM(Price) as Revenue,
                    COUNT(*) as LessonCount
                FROM Lessons
                WHERE Status != 'Cancelled'
                GROUP BY strftime('%Y', StartDateTime),
                         CASE
                             WHEN strftime('%m', StartDateTime) IN ('01', '02', '03') THEN 1
                             WHEN strftime('%m', StartDateTime) IN ('04', '05', '06') THEN 2
                             WHEN strftime('%m', StartDateTime) IN ('07', '08', '09') THEN 3
                             WHEN strftime('%m', StartDateTime) IN ('10', '11', '12') THEN 4
                         END
                ORDER BY strftime('%Y', StartDateTime), Quarter";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (!reader.IsDBNull(0) && !reader.IsDBNull(1) && !reader.IsDBNull(2))
                {
                    revenue.Add(new { quarter = reader.GetString(0), revenue = reader.GetDecimal(1), lessonCount = reader.GetInt32(2) });
                }
            }

            return revenue;
        }

        public List<object> GetReferralBreakdown()
        {
            var referrals = new List<object>();

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT ReferralSource, COUNT(*) as Count
                FROM StudentProfiles
                GROUP BY ReferralSource";

            var totalStudents = 0;
            var referralCounts = new Dictionary<string, int>();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var source = reader.GetString(0);
                var count = reader.GetInt32(1);
                referralCounts[source] = count;
                totalStudents += count;
            }

            foreach (var referral in referralCounts)
            {
                var percentage = totalStudents > 0 ? (decimal)referral.Value / totalStudents * 100 : 0;
                referrals.Add(new { referralSource = referral.Key, count = referral.Value, percentage = Math.Round(percentage, 2) });
            }

            return referrals.OrderByDescending(r => ((dynamic)r).count).ToList();
        }

        public List<object> GetPopularInstruments()
        {
            var instruments = new List<object>();

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Instrument, COUNT(*) as LessonCount
                FROM Lessons
                GROUP BY Instrument
                ORDER BY LessonCount DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                instruments.Add(new { instrument = reader.GetString(0), count = reader.GetInt32(1) });
            }

            return instruments;
        }

        public void ClearAllData(bool preserveAdmin)
        {
            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText = "DELETE FROM Lessons; DELETE FROM Availabilities; DELETE FROM TeacherProfiles; DELETE FROM StudentProfiles;";
            command.ExecuteNonQuery();

            if (!preserveAdmin)
            {
                command.CommandText = "DELETE FROM Users;";
                command.ExecuteNonQuery();
            }
            else
            {
                command.CommandText = "DELETE FROM Users WHERE Role <> 'Admin';";
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        public (int Teachers, int Students, int Lessons) SeedDummyData(int teachers, int students, int lessons)
        {
            var random = new Random();

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            int InsertUser(string role, string name, string email, string? contact)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"INSERT INTO Users (Role, Name, Email, PasswordHash, ContactInfo, CreatedAt)
                                    VALUES (@Role, @Name, @Email, @PasswordHash, @ContactInfo, @CreatedAt);
                                    SELECT last_insert_rowid();";
                cmd.Parameters.Add(new SqliteParameter("@Role", role));
                cmd.Parameters.Add(new SqliteParameter("@Name", name));
                cmd.Parameters.Add(new SqliteParameter("@Email", email));
                cmd.Parameters.Add(new SqliteParameter("@PasswordHash", BCrypt.Net.BCrypt.HashPassword("password")));
                cmd.Parameters.Add(new SqliteParameter("@ContactInfo", (object?)contact ?? DBNull.Value));
                cmd.Parameters.Add(new SqliteParameter("@CreatedAt", DateTime.UtcNow.ToString("o")));
                return Convert.ToInt32(cmd.ExecuteScalar());
            }

            void InsertTeacherProfile(int userId, string instrument, string? bio, decimal? rate)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"INSERT INTO TeacherProfiles (UserId, InstrumentTaught, Bio, CustomLessonRate, CreatedAt)
                                    VALUES (@UserId, @InstrumentTaught, @Bio, @CustomLessonRate, @CreatedAt)";
                cmd.Parameters.Add(new SqliteParameter("@UserId", userId));
                cmd.Parameters.Add(new SqliteParameter("@InstrumentTaught", instrument));
                cmd.Parameters.Add(new SqliteParameter("@Bio", (object?)bio ?? DBNull.Value));
                cmd.Parameters.Add(new SqliteParameter("@CustomLessonRate", (object?)rate ?? DBNull.Value));
                cmd.Parameters.Add(new SqliteParameter("@CreatedAt", DateTime.UtcNow.ToString("o")));
                cmd.ExecuteNonQuery();
            }

            void InsertStudentProfile(int userId, string interest, string referral)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"INSERT INTO StudentProfiles (UserId, InstrumentInterest, ReferralSource, CreatedAt)
                                    VALUES (@UserId, @InstrumentInterest, @ReferralSource, @CreatedAt)";
                cmd.Parameters.Add(new SqliteParameter("@UserId", userId));
                cmd.Parameters.Add(new SqliteParameter("@InstrumentInterest", interest));
                cmd.Parameters.Add(new SqliteParameter("@ReferralSource", referral));
                cmd.Parameters.Add(new SqliteParameter("@CreatedAt", DateTime.UtcNow.ToString("o")));
                cmd.ExecuteNonQuery();
            }

            void InsertLesson(int teacherId, int studentId, string instrument, DateTime start, int duration, string mode, decimal price, string status)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"INSERT INTO Lessons (TeacherId, StudentId, Instrument, StartDateTime, Duration, Mode, Price, Status, CreatedAt)
                                    VALUES (@TeacherId, @StudentId, @Instrument, @StartDateTime, @Duration, @Mode, @Price, @Status, @CreatedAt)";
                cmd.Parameters.Add(new SqliteParameter("@TeacherId", teacherId));
                cmd.Parameters.Add(new SqliteParameter("@StudentId", studentId));
                cmd.Parameters.Add(new SqliteParameter("@Instrument", instrument));
                cmd.Parameters.Add(new SqliteParameter("@StartDateTime", start.ToString("o")));
                cmd.Parameters.Add(new SqliteParameter("@Duration", duration));
                cmd.Parameters.Add(new SqliteParameter("@Mode", mode));
                cmd.Parameters.Add(new SqliteParameter("@Price", price));
                cmd.Parameters.Add(new SqliteParameter("@Status", status));
                cmd.Parameters.Add(new SqliteParameter("@CreatedAt", DateTime.UtcNow.ToString("o")));
                cmd.ExecuteNonQuery();
            }

            void InsertAvailability(int teacherId, DateTime start, int duration)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"INSERT INTO Availabilities (TeacherId, StartDateTime, Duration, CreatedAt)
                                    VALUES (@TeacherId, @StartDateTime, @Duration, @CreatedAt)";
                cmd.Parameters.Add(new SqliteParameter("@TeacherId", teacherId));
                cmd.Parameters.Add(new SqliteParameter("@StartDateTime", start.ToString("o")));
                cmd.Parameters.Add(new SqliteParameter("@Duration", duration));
                cmd.Parameters.Add(new SqliteParameter("@CreatedAt", DateTime.UtcNow.ToString("o")));
                cmd.ExecuteNonQuery();
            }

            var instrumentOptions = new[] { "Piano", "Guitar", "Violin", "Drums", "Saxophone", "Flute" };
            var teacherIds = new List<(int Id, string Instrument, decimal Rate)>();
            for (int i = 0; i < teachers; i++)
            {
                var instrument = instrumentOptions[random.Next(instrumentOptions.Length)];
                var name = $"Teacher {i + 1}";
                var email = $"teacher{i + 1}@example.com";
                var id = InsertUser("Teacher", name, email, null);
                var rate = 25 + random.Next(0, 21);
                InsertTeacherProfile(id, instrument, $"Experienced {instrument} teacher.", rate);
                teacherIds.Add((id, instrument, rate));
            }

            // Create 3-5 availability slots per teacher
            foreach (var teacher in teacherIds)
            {
                var numSlots = random.Next(3, 6);
                for (int j = 0; j < numSlots; j++)
                {
                    var availDate = DateTime.UtcNow.Date
                        .AddDays(random.Next(1, 30))
                        .AddHours(random.Next(9, 18))
                        .AddMinutes(new[] { 0, 30 }[random.Next(2)]);
                    var duration = new[] { 30, 45, 60 }[random.Next(3)];
                    InsertAvailability(teacher.Id, availDate, duration);
                }
            }

            var referralOptions = new[] { "Social Media", "Friend", "Flyer", "Search", "Other" };
            var studentIds = new List<int>();
            for (int i = 0; i < students; i++)
            {
                var interest = instrumentOptions[random.Next(instrumentOptions.Length)];
                var name = $"Student {i + 1}";
                var email = $"student{i + 1}@example.com";
                var id = InsertUser("Student", name, email, null);
                InsertStudentProfile(id, interest, referralOptions[random.Next(referralOptions.Length)]);
                studentIds.Add(id);
            }

            for (int i = 0; i < lessons; i++)
            {
                var teacher = teacherIds[random.Next(teacherIds.Count)];
                var studentId = studentIds[random.Next(studentIds.Count)];
                var duration = new[] { 30, 45, 60 }[random.Next(3)];
                var mode = random.NextDouble() < 0.5 ? "Virtual" : "In-Person";
                var status = random.NextDouble() < 0.1 ? "Cancelled" : (random.NextDouble() < 0.6 ? "Completed" : "Scheduled");
                var when = DateTime.UtcNow.Date
                    .AddDays(random.Next(-120, 60))
                    .AddHours(random.Next(9, 20))
                    .AddMinutes(new[] { 0, 15, 30, 45 }[random.Next(4)]);
                var price = Math.Round((decimal)duration / 60m * teacher.Rate, 2);
                InsertLesson(teacher.Id, studentId, teacher.Instrument, when, duration, mode, price, status);
            }

            transaction.Commit();
            return (teacherIds.Count, studentIds.Count, lessons);
        }
    }
}
