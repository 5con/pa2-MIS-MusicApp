using System.ComponentModel.DataAnnotations;

namespace FreelanceMusicPlatform.Models
{
    public enum LessonMode
    {
        InPerson,
        Virtual
    }

    public enum LessonStatus
    {
        Scheduled,
        Completed,
        Cancelled
    }

    public class Lesson
    {
        [Key]
        public int LessonId { get; set; }

        [Required]
        public int TeacherId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        [StringLength(100)]
        public string Instrument { get; set; } = string.Empty;

        [Required]
        public DateTime StartDateTime { get; set; }

        [Required]
        [Range(1, 480)]
        public int Duration { get; set; } = 30;

        [Required]
        public string Mode { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        public string Status { get; set; } = LessonStatus.Scheduled.ToString();

        public int? RecurringSeriesId { get; set; }

        public string? SheetMusicPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User Teacher { get; set; } = null!;
        public User Student { get; set; } = null!;
    }
}
