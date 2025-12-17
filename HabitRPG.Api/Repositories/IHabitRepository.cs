using HabitRPG.Api.Models;

namespace HabitRPG.Api.Repositories
{
    public interface IHabitRepository : IRepository<Habit>
    {
        Task<IEnumerable<Habit>> GetByUserIdAsync(Guid userId, bool includeInactive = false);
        Task<IEnumerable<Habit>> GetActiveByUserIdAsync(Guid userId);
        Task<IEnumerable<Habit>> GetInactiveByUserIdAsync(Guid userId);
        Task<Habit?> GetByIdWithUserAsync(int habitId);
        Task<Habit?> GetByIdWithUserAndCompletionsAsync(int habitId);
        Task<bool> ExistsForUserAsync(int habitId, Guid userId);
        Task<bool> IsActiveForUserAsync(int habitId, Guid userId);
        Task<bool> TitleExistsForUserAsync(Guid userId, string title, int? excludeHabitId = null);
        Task<int> GetActiveCountByUserIdAsync(Guid userId);
        Task<IEnumerable<Habit>> GetByIdsForUserAsync(Guid userId, IEnumerable<int> habitIds);
    }
        
}