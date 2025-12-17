using System.ComponentModel.DataAnnotations;

namespace HabitRPG.Api.Models
{
    public class Habit
    {
        public int Id { get; set; }

        public Guid UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        public string? Description { get; set; }

        public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;
        public HabitDifficulty Difficulty { get; set; } = HabitDifficulty.Medium;

        public int CurrentStreak { get; set; } = 0;
        public int BestStreak { get; set; }

        public DateTime? LastCompletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public User User { get; set; }
        public List<CompletionLog> CompletionLogs { get; set; } = new();
    }

    public enum HabitFrequency
    {
        Daily = 1,
        Weekly = 2
    }

    public enum HabitDifficulty
    {
        Easy = 1,   // +5 XP
        Medium = 2, // +10 XP
        Hard = 3    // +20 XP
    }
}