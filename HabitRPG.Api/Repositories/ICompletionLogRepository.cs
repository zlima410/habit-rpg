using HabitRPG.Api.Models;

namespace HabitRPG.Api.Repositories
{
    public interface ICompletionLogRepository : IRepository<CompletionLog>
    {
        Task<bool> IsCompletedTodayAsync(int habitId);
        Task<CompletionLog?> GetLastCompletionBeforeTodayAsync(int habitId);
        Task<IEnumerable<CompletionLog>> GetCompletionsByDateRangeAsync(int habitId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<CompletionLog>> GetCompletionsByHabitIdsAsync(IEnumerable<int> habitIds, DateTime? startDate = null, DateTime? endDate = null);
        Task<int> GetCompletionCountByHabitIdsAsync(IEnumerable<int> habitIds);
        Task<IEnumerable<DateTime>> GetCompletionDatesByHabitIdsAsync(IEnumerable<int> habitIds, DateTime startDate, DateTime endDate);
    }
}