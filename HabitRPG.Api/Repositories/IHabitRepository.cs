using HabitRPG.Api.Models;

namespace HabitRPG.Api.Repositories
{
    public interface IHabitRepository : IRepository<Habit>
    {
        Task<IEnumerable<Habit>> GetByUserIdAsync(int userId, bool includeInactive = false);
        Task<IEnumerable<Habit>> GetActiveByUserIdAsync(int userId);
        Task<IEnumerable<Habit>> GetInactiveByUserIdAsync(int userId);
        Task<Habit?> GetByIdWithUserAsync(int habitId);
        Task<Habit?> GetByIdWithUserAndCompletionsAsync(int habitId);
        Task<bool> ExistsForUserAsync(int habitId, int userId);
        Task<bool> IsActiveForUserAsync(int habitId, int userId);
        Task<bool> TitleExistsForUserAsync(int userId, string title, int? excludeHabitId = null);
        Task<int> GetActiveCountByUserIdAsync(int userId);
        Task<IEnumerable<Habit>> GetByIdsForUserAsync(int userId, IEnumerable<int> habitIds);
    }
        
}