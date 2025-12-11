using Microsoft.EntityFrameworkCore;
using HabitRPG.Api.Data;
using HabitRPG.Api.Models;
using HabitRPG.Api.DTOs;

namespace HabitRPG.Api.Services
{
    public class GameService : IGameService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<GameService> _logger;
        private const int XP_PER_LEVEL = 100;
        private const int MAX_LEVEL = 1000;
        private const int MAX_XP = 100_000_000;

        public GameService(ApplicationDbContext db, ILogger<GameService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<GameReward> CompleteHabitAsync(int userId, int habitId)
        {
            if (userId <= 0)
            {
                return new GameReward { Success = false, Message = "Invalid user ID" };
            }

            if (habitId <= 0)
            {
                return new GameReward { Success = false, Message = "Invalid habit ID" };
            }

            var strategy = _db.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync();
                
                try
                {
                    var habit = await _db.Habits
                        .Include(h => h.User)
                        .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId && h.IsActive);

                    if (habit == null)
                    {
                        return new GameReward { Success = false, Message = "Habit not found or inactive" };
                    }

                    if (!await CanCompleteHabitTodayAsync(habitId))
                    {
                        return new GameReward { Success = false, Message = "Habit already completed today" };
                    }

                    if (habit.User.Level < 1)
                    {
                        _logger.LogWarning("User {UserId} has invalid level {Level}, resetting to 1", userId, habit.User.Level);
                        habit.User.Level = 1;
                    }

                    if (habit.User.TotalXP < 0)
                    {
                        _logger.LogWarning("User {UserId} has negative total XP {TotalXP}, resetting to 0", userId, habit.User.TotalXP);
                        habit.User.TotalXP = 0;
                    }

                    var xpGained = CalculateXpForHabit(habit.Difficulty);
                    var oldLevel = habit.User.Level;

                    if (habit.User.TotalXP > MAX_XP - xpGained)
                    {
                        return new GameReward { Success = false, Message = "Maximum XP limit reached" };
                    }

                    habit.User.TotalXP += xpGained;
                    var newLevel = CalculateLevelFromTotalXp(habit.User.TotalXP);

                    if (newLevel > MAX_LEVEL)
                    {
                        newLevel = MAX_LEVEL;
                        habit.User.TotalXP = GetXpRequiredForLevel(MAX_LEVEL);
                    }

                    var leveledUp = newLevel > oldLevel;
                    habit.User.Level = newLevel;

                    var currentLevelXpRequired = GetXpRequiredForLevel(habit.User.Level);
                    habit.User.XP = habit.User.TotalXP - currentLevelXpRequired;

                    var streakResult = await UpdateHabitStreakAsync(habit);
                    if (!streakResult.Success)
                    {
                        await transaction.RollbackAsync();
                        return new GameReward { Success = false, Message = streakResult.Message };
                    }

                    var utcNow = DateTime.UtcNow;
                    var today = utcNow.Date;
                    var tomorrow = today.AddDays(1);

                    var existingLog = await _db.CompletionLogs
                        .FirstOrDefaultAsync(cl => cl.HabitId == habitId &&
                                                cl.CompletedAt >= today &&
                                                cl.CompletedAt < tomorrow);

                    if (existingLog != null)
                    {
                        await transaction.RollbackAsync();
                        return new GameReward { Success = false, Message = "Habit already completed today" };
                    }

                    var completionLog = new CompletionLog
                    {
                        HabitId = habitId,
                        CompletedAt = DateTime.UtcNow
                    };

                    _db.CompletionLogs.Add(completionLog);
                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("User {UserId} completed habit {HabitId}, gained {XP} XP", userId, habitId, xpGained);

                    return new GameReward
                    {
                        Success = true,
                        Message = leveledUp ?
                            $"Incredible! You gained {xpGained} XP and reached level {newLevel}!" :
                            $"Well done! You gained {xpGained} XP!",
                        XpGained = xpGained,
                        LeveledUp = leveledUp,
                        NewLevel = habit.User.Level,
                        NewXp = habit.User.XP,
                        NewTotalXp = habit.User.TotalXP,
                        NewStreak = habit.CurrentStreak,
                        UpdatedHabit = MapToHabitDto(habit, false)
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error completing habit {HabitId} for user {UserId}", habitId, userId);
                    return new GameReward
                    {
                        Success = false,
                        Message = "An error occurred while completing the habit. Please try again."
                    };
                }
            });
        }

        public int CalculateXpForHabit(HabitDifficulty difficulty)
        {
            return difficulty switch
            {
                HabitDifficulty.Easy => 5,
                HabitDifficulty.Medium => 10,
                HabitDifficulty.Hard => 20,
                _ => 10
            };
        }

        public int CalculateLevelFromTotalXp(int totalXp)
        {
            if (totalXp < 0)
            {
                _logger.LogWarning("Negative total XP provided: {TotalXP}", totalXp);
                return 1;
            }

            var level = (totalXp / XP_PER_LEVEL) + 1;
            return Math.Min(level, MAX_LEVEL);
        }

        public int GetXpRequiredForLevel(int level)
        {
            if (level < 1)
            {
                _logger.LogWarning("Invalid level provided: {Level}", level);
                return XP_PER_LEVEL;
            }

            if (level > MAX_LEVEL)
            {
                level = MAX_LEVEL;
            }

            return (level - 1) * XP_PER_LEVEL;
        }

        public async Task<bool> CanCompleteHabitTodayAsync(int habitId)
        {
            if (habitId <= 0)
            {
                return false;
            }

            try
            {
                var today = DateTime.UtcNow.Date;
                var todayEnd = today.AddDays(1).AddTicks(-1);

                var completedToday = await _db.CompletionLogs
                    .AnyAsync(cl => cl.HabitId == habitId &&
                                   cl.CompletedAt >= today &&
                                   cl.CompletedAt <= todayEnd);

                return !completedToday;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if habit {HabitId} can be completed today", habitId);
                return false;
            }
        }

        private async Task<(bool Success, string Message)> UpdateHabitStreakAsync(Habit habit)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var yesterday = today.AddDays(-1);

                var lastCompletion = await _db.CompletionLogs
                    .Where(cl => cl.HabitId == habit.Id && cl.CompletedAt.Date < today)
                    .OrderByDescending(cl => cl.CompletedAt)
                    .FirstOrDefaultAsync();

                if (lastCompletion == null)
                    habit.CurrentStreak = 1;
                else if (lastCompletion.CompletedAt.Date == yesterday)
                {
                    if (habit.CurrentStreak < 0)
                    {
                        _logger.LogWarning("Habit {HabitId} had negative streak {Streak}, resetting",
                            habit.Id, habit.CurrentStreak);
                        habit.CurrentStreak = 1;
                    }
                    else if (habit.CurrentStreak > 10000)
                    {
                        _logger.LogWarning("Habit {HabitId} has extremely high streak {Streak}",
                            habit.Id, habit.CurrentStreak);
                        habit.CurrentStreak = Math.Min(habit.CurrentStreak + 1, 10000);
                    }
                    else
                        habit.CurrentStreak++;
                }
                else
                    habit.CurrentStreak = 1;

                if (habit.CurrentStreak > habit.BestStreak)
                    habit.BestStreak = habit.CurrentStreak;

                habit.LastCompletedAt = DateTime.UtcNow;

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating streak for habit {HabitId}", habit.Id);
                return (false, "Error updating habit streak");
            }
        }
        
        private static HabitDto MapToHabitDto(Habit habit, bool canCompleteToday)
        {
            return new HabitDto
            {
                Id = habit.Id,
                Title = habit.Title ?? string.Empty,
                Description = habit.Description,
                Frequency = habit.Frequency,
                Difficulty = habit.Difficulty,
                CurrentStreak = Math.Max(0, habit.CurrentStreak),
                BestStreak = Math.Max(0, habit.BestStreak),
                LastCompletedAt = habit.LastCompletedAt,
                IsActive = habit.IsActive,
                CanCompleteToday = canCompleteToday,
                CreatedAt = habit.CreatedAt
            };
        }
    }
}