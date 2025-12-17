using HabitRPG.Api.Models;
using HabitRPG.Api.DTOs;

namespace HabitRPG.Api.Services
{
    public interface IGameService
    {
        Task<GameReward> CompleteHabitAsync(Guid userId, int habitId);
        int CalculateXpForHabit(HabitDifficulty difficulty);
        int CalculateLevelFromTotalXp(int totalXp);
        int GetXpRequiredForLevel(int level);
        Task<bool> CanCompleteHabitTodayAsync(int habitId);
    }
}