using Microsoft.EntityFrameworkCore;
using HabitRPG.Api.Data;
using HabitRPG.Api.Models;

namespace HabitRPG.Api.Repositories
{
    public class HabitRepository : Repository<Habit>, IHabitRepository
    {
        public HabitRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Habit>> GetByUserIdAsync(int userId, bool includeInactive = false)
        {
            var query = _dbSet.Where(h => h.UserId == userId);

            if (!includeInactive)
                query = query.Where(h => h.IsActive);

            return await query
                .OrderByDescending(h => h.CreatedAt)
                .Take(100)
                .ToListAsync();
        }

        public async Task<IEnumerable<Habit>> GetActiveByUserIdAsync(int userId)
        {
            return await _dbSet
                .Where(h => h.UserId == userId && h.IsActive)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Habit>> GetInactiveByUserIdAsync(int userId)
        {
            return await _dbSet
                .Where(h => h.UserId == userId && !h.IsActive)
                .OrderByDescending(h => h.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        public async Task<Habit?> GetByIdWithUserAsync(int habitId)
        {
            return await _dbSet
                .Include(h => h.User)
                .FirstOrDefaultAsync(h => h.Id == habitId);
        }

        public async Task<Habit?> GetByIdWithUserAndCompletionsAsync(int habitId)
        {
            return await _dbSet
                .Include(h => h.User)
                .Include(h => h.CompletionLogs)
                .FirstOrDefaultAsync(h => h.Id == habitId);
        }

        public async Task<bool> ExistsForUserAsync(int habitId, int userId)
        {
            return await _dbSet
                .AnyAsync(h => h.Id == habitId && h.UserId == userId);
        }

        public async Task<bool> IsActiveForUserAsync(int habitId, int userId)
        {
            return await _dbSet
                .AnyAsync(h => h.Id == habitId && h.UserId == userId && h.IsActive);
        }

        public async Task<bool> TitleExistsForUserAsync(int userId, string title, int? excludeHabitId = null)
        {
            var query = _dbSet
                .Where(h => h.UserId == userId &&
                            h.IsActive &&
                            h.Title.ToLower() == title.Trim().ToLower());

            if (excludeHabitId.HasValue)
                query = query.Where(h => h.Id != excludeHabitId.Value);

            return await query.AnyAsync();
        }

        public async Task<int> GetActiveCountByUserIdAsync(int userId)
        {
            return await _dbSet
                .CountAsync(h => h.UserId == userId && h.IsActive);
        }

        public async Task<IEnumerable<Habit>> GetByIdsForUserAsync(int userId, IEnumerable<int> habitIds)
        {
            return await _dbSet
                .Where(h => habitIds.Contains(h.Id) && h.UserId == userId)
                .ToListAsync();
        }
    }
}