using System.ComponentModel.DataAnnotations;

namespace HabitRPG.Api.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public int Level { get; set; } = 0;
        public int XP { get; set; } = 0;
        public int TotalXP { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Habit> Habits { get; set; } = new();
    }
}