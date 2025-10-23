using System.ComponentModel.DataAnnotations;

namespace FreelanceMusicPlatform.Models
{
    public enum ReferralSource
    {
        SocialMedia,
        Friend,
        Advertisement,
        SearchEngine,
        Other
    }

    public class StudentProfile
    {
        [Key]
        public int StudentProfileId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string InstrumentInterest { get; set; } = string.Empty;

        [Required]
        public string ReferralSource { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User User { get; set; } = null!;
    }
}
