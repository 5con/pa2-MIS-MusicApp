using System.ComponentModel.DataAnnotations;

namespace FreelanceMusicPlatform.Models
{
    public enum UserRole
    {
        Admin,
        Teacher,
        Student
    }

    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ContactInfo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
