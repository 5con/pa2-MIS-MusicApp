using System.ComponentModel.DataAnnotations;

namespace FreelanceMusicPlatform.Models
{
    public class Availability
    {
        [Key]
        public int AvailabilityId { get; set; }

        [Required]
        public int TeacherId { get; set; }

        [Required]
        public DateTime StartDateTime { get; set; }

        [Required]
        [Range(1, 480)] // Max 8 hours
        public int Duration { get; set; } = 30; // Default 30 minutes for Phase 1

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User Teacher { get; set; } = null!;
    }
}
