namespace HabitRPG.Api.DTOs
{
    public class GameReward
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int XpGained { get; set; }
        public bool LeveledUp { get; set; }
        public int NewLevel { get; set; }
        public int NewXp { get; set; }
        public int NewTotalXp { get; set; }
        public int NewStreak { get; set; }
        public HabitDto? UpdatedHabit { get; set; }
    }
}