using System.ComponentModel.DataAnnotations;

namespace FreelanceMusicPlatform.Models
{
    public class TeacherProfile
    {
        [Key]
        public int TeacherProfileId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string InstrumentTaught { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Bio { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CustomLessonRate { get; set; } // Custom rate, null means use default

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User User { get; set; } = null!;
    }
}
