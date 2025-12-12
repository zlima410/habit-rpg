using Microsoft.EntityFrameworkCore;
using HabitRPG.Api.Data;
using HabitRPG.Api.Models;

namespace HabitRPG.Api.Repositories
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
    }

    public async Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername)
    {
        var lower = emailOrUsername.ToLower();
        return await _dbSet
            .FirstOrDefaultAsync(u => 
                u.Email.ToLower() == lower ||
                u.Username.ToLower() == lower);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet
            .AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _dbSet
            .AnyAsync(u => u.Username.ToLower() == username.ToLower());
    }

    public async Task<User?> GetUserWithHabitsAsync(int userId, bool includeInactive = false)
    {
        var query = _dbSet
            .Include(u => u.Habits.Where(h => includeInactive || h.IsActive))
            .Where(u => u.Id == userId);

        return await query.FirstOrDefaultAsync();
    }

    public async Task<User?> GetUserWithActiveHabitsAsync(int userId)
    {
        return await _dbSet
            .Include(u => u.Habits.Where(h => h.IsActive))
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}