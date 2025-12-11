using System.ComponentModel.DataAnnotations;

namespace HabitRPG.Api.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Level { get; set; }
        public int XP { get; set; }
        public int TotalXP { get; set; }
        public int XpToNextLevel { get; set; }
        public int XpProgress { get; set; }
        public int XpRequiredForNextLevel { get; set; }
        public int ActiveHabitsCount { get; set; }
        public int TotalCompletions { get; set; }
        public int LongestStreak { get; set; }
        public int CurrentActiveStreaks { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserStatsDto
    {
        public int TotalCompletions { get; set; }
        public double CompletionRate { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreakInPeriod { get; set; }
        public double AverageCompletionsPerDay { get; set; }
        public Dictionary<string, int> DailyCompletions { get; set; } = new();
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    public class UpdateProfileRequest
    {
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Username can only contain letters, numbers, hyphens, and underscores")]
        public string? Username { get; set; }
    }
}