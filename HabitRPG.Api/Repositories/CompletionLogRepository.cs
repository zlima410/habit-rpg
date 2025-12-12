using Microsoft.EntityFrameworkCore;
using HabitRPG.Api.Data;
using HabitRPG.Api.Models;

namespace HabitRPG.Api.Repositories
{
    public class CompletionLogRepository : Repository<CompletionLog>, ICompletionLogRepository
    {
        public CompletionLogRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<bool> IsCompletedTodayAsync(int habitId)
        {
            var today = DateTime.UtcNow.Date;
            var todayEnd = today.AddDays(1).AddTicks(-1);

            return await _dbSet
                .AnyAsync(cl => cl.HabitId == habitId &&
                                cl.CompletedAt >= today &&
                                cl.CompletedAt <= todayEnd);
        }

        public async Task<CompletionLog?> GetLastCompletionBeforeTodayAsync(int habitId)
        {
            var today = DateTime.UtcNow.Date;

            return await _dbSet
                .Where(cl => cl.HabitId == habitId && cl.CompletedAt.Date < today)
                .OrderByDescending(cl => cl.CompletedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CompletionLog>> GetCompletionsByDateRangeAsync(int habitId, DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(cl => cl.HabitId == habitId &&
                            cl.CompletedAt >= startDate &&
                            cl.CompletedAt <= endDate)
                .OrderBy(cl => cl.CompletedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<CompletionLog>> GetCompletionsByHabitIdsAsync(IEnumerable<int> habitIds, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbSet.Where(cl => habitIds.Contains(cl.HabitId));

            if (startDate.HasValue)
                query = query.Where(cl => cl.CompletedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(cl => cl.CompletedAt <= endDate.Value);

            return await query.ToListAsync();
        }

        public async Task<int> GetCompletionCountByHabitIdsAsync(IEnumerable<int> habitIds)
        {
            return await _dbSet
                .CountAsync(cl => habitIds.Contains(cl.HabitId));
        }

        public async Task<IEnumerable<DateTime>> GetCompletionDatesByHabitIdsAsync(IEnumerable<int> habitIds, DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(cl => habitIds.Contains(cl.HabitId) &&
                            cl.CompletedAt >= startDate &&
                            cl.CompletedAt <= endDate)
                .Select(cl => cl.CompletedAt.Date)
                .Distinct()
                .ToListAsync();
        }
    }
}